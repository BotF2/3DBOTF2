using BOTF3D.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    public class TimeManager : NetworkBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        public static TimeManager Instance;

        public event Action<TrekRandomEventSO> onRandomSpecialEvent; // 
        public Action<TrekRandomEventSO> OnRandomSpecialEvent; // current of the delegate Action 
        public event Action<TrekStardateEventSO> onStardateSpecialEvent; // 
        public Action<TrekStardateEventSO> OnStardateSpecialEvent;
        public event Action OnStardateChanged; //StardateUIController subscribes the UpdateDateText() function
        public event Action<CivEnum, TechLevel, TechLevel> OnTechLevelAdvanced;
        public event Action OnTurnAdvanced; // fires every StarDatesPerTurn stardates — strategic resolution tick
        public event Action<TurnPhase> OnTurnPhaseChanged; // UI can subscribe to update Advance Turn button

        // These three were plain auto-properties before turn-phase networking. TimeManager used to
        // run as an unnetworked MonoBehaviour, so OnGameStartReceived (called on every client via
        // PlayerManager.RpcStartGame) had each machine start its own independent TimeProgression()
        // coroutine, and AdvanceTurn() only ever mutated that local machine's own copy - a non-host
        // client's turn click never reached the server, so server-authoritative fleet movement (which
        // gates on TurnPhase == TurnProgression) never actually triggered for non-host clients. Now
        // that TimeManager is a NetworkBehaviour (scene object, see PersistentScene.unity), these are
        // SyncVars so the server is the sole simulator and every client's UI/gating logic reads the
        // same replicated state.
        [SyncVar(hook = nameof(OnStardateSynced))]
        private int syncedStardate;
        public int currentStardate => syncedStardate;

        [SyncVar(hook = nameof(OnCurrentTurnSynced))]
        private int syncedCurrentTurn = 0;
        public int CurrentTurn => syncedCurrentTurn;

        [SerializeField] public int StarDatesPerTurn = 10;

        [SyncVar(hook = nameof(OnTurnPhaseSynced))]
        private TurnPhase syncedTurnPhase = TurnPhase.InterTurn;
        public TurnPhase TurnPhase => syncedTurnPhase;

        // Server-authoritative set of civs that have marked themselves done giving orders for the
        // current InterTurn. Advancing to TurnProgression now requires every civ in
        // PlayerManager.Roster to be present here (see TryAutoAdvanceIfAllReady) instead of
        // advancing on the first player's click. Cleared and re-seeded with any AI civs (see
        // BeginNewInterTurnReadyState) every time we (re-)enter InterTurn. SyncList so every
        // client's UI can show live "waiting on X" state via the Callback event.
        public readonly SyncList<CivEnum> ReadyCivs = new SyncList<CivEnum>();

        public bool timeRunning = true; // ✅ Change from false to true
        public bool IsPaused { get; private set; } = false; // Already correct
        private Coroutine timeCoroutine;
        private float currentTimeSpeed = 10f; // stardates/sec — controls turn progression speed
        private float unityTimeScale = 1f; // ✅ Add this for Unity's Time.timeScale
        private bool isPausing = false;
        public List<TrekRandomEventSO> RandomEvents;
        public List<TrekStardateEventSO> StardateEvents;

        public int StaringStardate = 1010; // starting stardate for TechLevel.EARLY, and the app-launch default before a game is created
        public int StardateDeveloped = 1510; // starting stardate for TechLevel.DEVELOPED
        public int StardateAdvanced = 3010;  // starting stardate for TechLevel.ADVANCED
        public int StardateSupreme = 4010;   // starting stardate for TechLevel.SUPREME

        public int GetStartingStardateFor(TechLevel level)
        {
            switch (level)
            {
                case TechLevel.DEVELOPED: return StardateDeveloped;
                case TechLevel.ADVANCED: return StardateAdvanced;
                case TechLevel.SUPREME: return StardateSupreme;
                default: return StaringStardate;
            }
        }

        // Called once per new game (CivManager.CreateNewGameBySelections) once the player's chosen
        // StartingTechLevel is known, so the clock starts at the right point for that era instead of
        // always EARLY's 1010 - covers every play mode (SP host, MP host) since it just re-assigns the
        // same SyncVar Start() seeds at app launch. Server-only, same reasoning as Start()'s guard below.
        [Server]
        public void ApplyStartingStardate(TechLevel level)
        {
            syncedStardate = GetStartingStardateFor(level);
        }

        void Awake()
        {
            ServiceLocator.Register<TimeManager>(this);
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject); // ✅ Destroy duplicate, don't replace Instance
                return;
            }

            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Start paused — player must click Advance Turn to begin first turn
            IsPaused = true;
            timeRunning = false;

            Debug.Log($"⏰ TimeManager: Initialized - IsPaused={IsPaused}, timeRunning={timeRunning}");
        }
        private void Start()
        {
            // Only the server owns the authoritative stardate/phase - a joining client receives
            // these via the SyncVars themselves (initial spawn sync), so setting them here too
            // would just get immediately overwritten and would trip Mirror's "SyncVar set on
            // client" warning for no benefit.
            if (NetworkServer.active)
            {
                syncedStardate = StaringStardate;

                // Fire the event so any already-subscribed UI components initialize their state
                SetTurnPhase(TurnPhase.InterTurn);
            }

            // Remain in InterTurn until player clicks Advance Turn for the first time
            IsPaused = true;
            timeRunning = false;

            Debug.Log($"⏰ TimeManager: Started - currentStardate={currentStardate}, waiting for first Advance Turn click");
        }
        void Update()
        {

        }
        public void StartTime()
        {
            // TimeProgression is the sole tick of stardate/turn simulation (research, special
            // events, encounter queueing). Running it on every client independently is what
            // caused non-host clients' turn clicks to never reach the server-authoritative fleet
            // movement gate (see FleetController.FixedUpdate) - only the server may run it now.
            if (!isServer) return;

            if (timeCoroutine != null)
                StopCoroutine(timeCoroutine);
            timeRunning = true;
            IsPaused = false;
            Time.timeScale = 1f;
            timeCoroutine = StartCoroutine(TimeProgression());
            Debug.Log("⏰ TimeManager: Time started via StartTime()");
        }

        /// <summary>
        /// Actually starts TurnProgression. Server-only - reached either via
        /// TryAutoAdvanceIfAllReady() once every civ is ready, or via ForceAdvanceTurn().
        /// Restarts the clock for one full turn cycle, then the system auto-pauses again.
        /// </summary>
        [Server]
        public void AdvanceTurn()
        {
            if (syncedTurnPhase == TurnPhase.TurnProgression)
            {
                Debug.Log("⏰ TimeManager: AdvanceTurn ignored — already in TurnProgression");
                return; // already running
            }
            SetTurnPhase(TurnPhase.TurnProgression);
            StartTime(); // restarts the coroutine (PauseTime killed it) and sets timeScale = 1
            Debug.Log("⏰ TimeManager: Turn advanced — TurnProgression started");
        }

        /// <summary>
        /// Entry point for the "Force Turn" button (GameControlOverlay) - advances the turn
        /// immediately, skipping ReadyCivs. Shown/enabled for every connected client (testing aid,
        /// including on a true dedicated server where no client has NetworkServer.active); non-host
        /// clients relay through a requiresAuthority = false Command, same pattern as
        /// RequestSetCivReady/CmdSetCivReady below.
        /// </summary>
        public void RequestForceAdvanceTurn()
        {
            if (isServer)
                ForceAdvanceTurn();
            else
            {
                Debug.Log("⏰ TimeManager: RequestForceAdvanceTurn - relaying via CmdForceAdvanceTurn (non-host client).");
                CmdForceAdvanceTurn();
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdForceAdvanceTurn(NetworkConnectionToClient sender = null)
        {
            Debug.Log($"⏰ TimeManager: CmdForceAdvanceTurn received from connection {sender?.connectionId}");
            ForceAdvanceTurn();
        }

        [Server]
        private void ForceAdvanceTurn()
        {
            Debug.LogWarning("⏰ TimeManager: ForceAdvanceTurn — advancing without waiting for all civs ready.");
            AdvanceTurn();
        }

        /// <summary>
        /// Entry point for the "Advance Turn" button (GameControlOverlay) toggling this civ's ready
        /// state, and for AI to mark itself ready. TimeManager is a scene-singleton NetworkBehaviour
        /// with no owning connection (same as FleetController's order relays), so non-host clients
        /// relay through a requiresAuthority = false Command rather than mutating ReadyCivs directly.
        /// </summary>
        public void RequestSetCivReady(CivEnum civ, bool ready)
        {
            if (isServer)
                SetCivReady(civ, ready);
            else
            {
                Debug.Log($"⏰ TimeManager: RequestSetCivReady({civ}, {ready}) - relaying via CmdSetCivReady (non-host client).");
                CmdSetCivReady(civ, ready);
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdSetCivReady(CivEnum civ, bool ready, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"⏰ TimeManager: CmdSetCivReady({civ}, {ready}) received from connection {sender?.connectionId}");
            SetCivReady(civ, ready);
        }

        [Server]
        private void SetCivReady(CivEnum civ, bool ready)
        {
            if (syncedTurnPhase != TurnPhase.InterTurn)
            {
                Debug.LogWarning($"⏰ TimeManager: SetCivReady({civ}, {ready}) ignored — not InterTurn (phase={syncedTurnPhase}).");
                return;
            }

            if (ready)
            {
                if (!ReadyCivs.Contains(civ))
                    ReadyCivs.Add(civ);
            }
            else
            {
                ReadyCivs.Remove(civ);
            }

            Debug.Log($"⏰ TimeManager: {civ} ready={ready} ({ReadyCivs.Count} ready).");
            TryAutoAdvanceIfAllReady();
        }

        [Server]
        private void TryAutoAdvanceIfAllReady()
        {
            if (syncedTurnPhase != TurnPhase.InterTurn) return;
            if (PlayerManager.Instance == null || PlayerManager.Instance.Roster.Count == 0) return;

            foreach (var entry in PlayerManager.Instance.Roster)
            {
                // AI civs are always treated as ready (stub - see BeginNewInterTurnReadyState),
                // checked here directly rather than relying solely on ReadyCivs already containing
                // them - an AI registering mid-InterTurn (e.g. joining after this InterTurn's seed
                // already ran) would otherwise block the group forever with nothing to un-ready it.
                if (entry.PlayerType == PlayerType.AI) continue;
                if (!ReadyCivs.Contains(entry.PlayerCiv))
                    return; // still waiting on someone
            }

            Debug.Log("⏰ TimeManager: All civs ready — auto-advancing turn.");
            AdvanceTurn();
        }

        /// <summary>
        /// Human (non-AI) civs registered in PlayerManager.Roster that haven't marked themselves
        /// ready yet this InterTurn. Used by the turn UI to render a "waiting on X, Y" notice.
        /// </summary>
        public List<CivEnum> GetHumanCivsNotReady()
        {
            var result = new List<CivEnum>();
            if (PlayerManager.Instance == null) return result;

            foreach (var entry in PlayerManager.Instance.Roster)
            {
                if (entry.PlayerType == PlayerType.AI) continue;
                if (!ReadyCivs.Contains(entry.PlayerCiv))
                    result.Add(entry.PlayerCiv);
            }
            return result;
        }

        private void SetTurnPhase(TurnPhase phase)
        {
            // Assigning the SyncVar replicates to every client; OnTurnPhaseSynced fires the
            // OnTurnPhaseChanged event both here on the server/host and on each remote client
            // once the new value arrives (same hook-fires-everywhere pattern already established
            // by LocalHumanPlayerController.OnPlayerCivChanged / FleetController.OnCivEnumChanged).
            syncedTurnPhase = phase;

            // ReadyCivs is server-authoritative (SyncList) - only the server may mutate it. This
            // method is also reached from EnsureInterTurn, which OnSceneLoaded calls unconditionally
            // on every client (see below), so the NetworkServer.active guard is required here even
            // though the syncedTurnPhase assignment above is already effectively inert off-server.
            if (phase == TurnPhase.InterTurn && NetworkServer.active)
                BeginNewInterTurnReadyState();
        }

        [Server]
        private void BeginNewInterTurnReadyState()
        {
            ReadyCivs.Clear();

            if (PlayerManager.Instance == null) return;

            foreach (var entry in PlayerManager.Instance.Roster)
            {
                // Stub AI turn logic: there's no galaxy-map AI decision-making yet, so an AI civ has
                // nothing to plan and is ready the instant a new InterTurn begins. Real AI turn
                // planning can call RequestSetCivReady(civ, true) itself once it exists instead of
                // this unconditional add, without touching the ready-sync mechanism.
                if (entry.PlayerType == PlayerType.AI)
                    ReadyCivs.Add(entry.PlayerCiv);
            }
        }

        private void OnTurnPhaseSynced(TurnPhase oldPhase, TurnPhase newPhase)
        {
            OnTurnPhaseChanged?.Invoke(newPhase);
            Debug.Log($"⏰ TimeManager: TurnPhase → {newPhase}");
        }

        private void OnStardateSynced(int oldStardate, int newStardate)
        {
            OnStardateChanged?.Invoke();
        }

        private void OnCurrentTurnSynced(int oldTurn, int newTurn)
        {
        }

        private System.Collections.IEnumerator TimeProgression()
        {

            while (timeRunning)
            {
                yield return new WaitForSeconds(10f / currentTimeSpeed);
                syncedStardate++;
                CheckSpecialEvents();

                if (syncedStardate % StarDatesPerTurn == 0)
                {
                    syncedCurrentTurn++;
                    ProcessTurnEvents();
                    OnTurnAdvanced?.Invoke();
                }
            }
        }

        /// <summary>
        /// Refresh build UIs for all systems owned by a civilization
        /// Call this when tech level changes to update available ships
        /// </summary>
        public void RefreshBuildUIsForCiv(CivEnum civEnum)
        {
            if (StarSysManager.Instance == null) return;

            var civController = CivManager.Instance?.GetCivControllerByCivEnum(civEnum);
            if (civController?.CivData?.StarSysWeOwn == null) return;

            Debug.Log($"  Refreshing build UIs for {civController.CivData.StarSysWeOwn.Count} systems owned by {civEnum}");

            // Re-point already-built shipyards at their new tier's art - RefreshSystemBuildUI below
            // only touches the (not-yet-built) build menu, not a facility that already exists.
            var theme = ThemeManager.Instance?.GetThemeByCivEnum(civEnum);
            TechLevel currentTechLevel = civController.CivData.CurrentTechLevel;

            foreach (var system in civController.CivData.StarSysWeOwn)
            {
                if (system != null)
                {
                    if (theme != null && system.StarSysData?.ShipyardData != null)
                        system.StarSysData.ShipyardData.ShipyardSprite = theme.GetShipyardImage(currentTechLevel);

                    // If the build UI is currently open for this system, refresh it
                    RefreshSystemBuildUI(system);
                }
            }
        }

        /// <summary>
        /// Refresh the build UI for a specific system
        /// </summary>
        private void RefreshSystemBuildUI(StarSysController sysCon)
{
            // Check if this system's build UI is currently open
            if (StarSysMenuUIController.Instance != null &&
                StarSysMenuUIController.Instance.ActiveStarSysController == sysCon)
            {
                // Find the active build UI instance
                GameObject buildUI = GameObject.Find("SysBuildUIList(Clone)");
                if (buildUI != null)
                {
                    Debug.Log($"    ✅ Refreshing build UI for system '{sysCon.name}'");

                    // Re-run the tech level filter
                    if (StarSysManager.Instance != null)
                    {
                        StarSysManager.Instance.UpdateAvailableShipsByTechLevel(sysCon, buildUI);

                        // ✅ Also refresh item/background sprites so newly-unlocked ships get their
                        // real art (and previously-locked ones drop their "coming soon" preview)
                        StarSysManager.Instance.SetShipBuildImages(sysCon, buildUI);

                        // ✅ Facility icons (shipyard in particular) are also tech-tiered art now
                        StarSysManager.Instance.SetFacilityBuildImages(sysCon, buildUI);
                    }
                }
            }
        }
        /// <summary>
        /// Process all turn-based events (research, production, etc.) then pause the clock.
        /// Encounters are now resolved per-fleet as they happen (see GalaxyEncounterQueue.
        /// ProcessPendingForThisTick), not deferred to this turn boundary, so fleets still
        /// awaiting an encounter decision simply stay paused via FleetController's own
        /// per-fleet gate while everyone else keeps moving next turn.
        /// </summary>
        private void ProcessTurnEvents()
        {
            if (TechManager.Instance != null)
                TechManager.Instance.ProcessResearchForAllCivs();

            StarSysManager.Instance?.ProcessDilithiumMining();
            StarSysManager.Instance?.ProcessRepairs();

            // TODO: population growth, credits/income, random events

            SetTurnPhase(TurnPhase.InterTurn);
            PauseTime();
        }

        // Check for special events and trigger corresponding actions
        private void CheckSpecialEvents()
        {
            foreach (var specialEvent in RandomEvents)
            {
                if (specialEvent != null)
                {
                    if (1 == UnityEngine.Random.Range(1, specialEvent.oneInXChance))
                    {
                        // Trigger special event
                        onRandomSpecialEvent?.Invoke(specialEvent);
                    }
                }
            }
            foreach (var specialEvent in StardateEvents)
            {
                if (specialEvent != null && currentStardate == specialEvent.stardate)
                {
                    // Trigger special event
                    OnStardateSpecialEvent?.Invoke(specialEvent);
                }
            }
        }

        // Method to set time speed multiplier
        public void SetTimeSpeedMultiplier(float multiplier)
        {
            if (multiplier > 0)
                currentTimeSpeed = multiplier;

            // Restart time progression coroutine with new speed multiplier
            if (timeCoroutine != null)
            {
                StopCoroutine(timeCoroutine);
                timeCoroutine = StartCoroutine(TimeProgression());
            }
        }

        // Method to pause time progression
        public void PauseTime()
        {
            timeRunning = false;
            IsPaused = true;
            // Do NOT set Time.timeScale here — that would freeze FixedUpdate and stop
            // all galaxy fleet movement. Fleet physics must keep running between turns.
            // Only PauseForMessageCoroutine (and combat) should freeze Unity time.
            Debug.Log("⏸ TimeManager: Time PAUSED");
        }

        public void ResumeTime()
        {
            timeRunning = true;
            IsPaused = false;
            // Restore timeScale in case a message pause or combat froze it.
            Time.timeScale = 1f;
            Debug.Log($"▶️ TimeManager: Time RESUMED (timeScale=1.0, coroutineSpeed={currentTimeSpeed})");
        }

        // Fallback combat-end broadcast channel, used by CombatController.EndCombat() only when no
        // FleetController survived to anchor its own RpcCombatEnded (see FleetController.cs and
        // CombatController.EndCombat's comment on combatEndedAnchor). That normal channel depends on
        // CombatController._involvedFleets containing at least one still-alive fleet, which fails for
        // a FleetVsSystem/SystemVsFleet combat where the defending system's own ships never belong to
        // a fleet at all, if the one attacking fleet that WAS in the list is itself wiped out ending
        // the fight. TimeManager is a persistent scene NetworkBehaviour (see Awake/DontDestroyOnLoad,
        // PersistentScene.unity) - alive for the whole game session regardless of what happens to any
        // specific combat's ships or fleets - so it works as an always-reachable relay for this one
        // broadcast when the normal channel has nothing left to send it through.
        [Server]
        public void ServerNotifyCombatEnded(CivEnum civA, CivEnum civB)
        {
            RpcCombatEndedFallback(civA, civB);
        }

        [ClientRpc]
        private void RpcCombatEndedFallback(CivEnum civA, CivEnum civB)
        {
            // Unconditional, first line - if missing from a peer's log, this fallback Rpc never
            // arrived/executed on that peer at all.
            Debug.Log($"📩[RpcCombatEndedFallbackDiag] RpcCombatEndedFallback RECEIVED for {civA} vs {civB} (isServer={isServer}, isClient={isClient}).");

            CombatPausedNoticeUI.Instance?.Hide();

            // Matches by civ pair, not fleet identity, since this only ever runs when no fleet
            // anchor was available to identify the combat unambiguously (see
            // FleetController.RpcCombatEnded's comment on why civ-pair matching alone is ambiguous
            // under back-to-back same-civ-pair test battles). That narrow ambiguity risk is
            // acceptable here: CombatController.EndCombat()'s own idempotency guard (the
            // `combatEnded` flag) makes it harmless if this fallback and the normal fleet-anchored
            // broadcast both end up reaching the same client for the same combat.
            CombatController combatCon = CombatManager.Instance?.GetActiveCombatControllerForCivs(civA, civB);
            if (combatCon == null)
            {
                Debug.LogWarning($"⚠️[RpcCombatEndedFallbackDiag] Fallback RpcCombatEnded received for {civA} vs {civB} but no matching active CombatController found - this client's Combat scene will NOT be torn down.");
            }
            combatCon?.EndCombat();
        }

        // ---------------------------------------------------------------------------------------
        // Star-system ship roster replication. StarSysController has no NetworkIdentity (see
        // StarSysManager.GetStarSysControllerByInt's comment), so it can't host its own [Command]/
        // [ClientRpc] pair the way FleetController.RequestSyncShipRoster does. TimeManager is a
        // persistent scene NetworkBehaviour reachable by every client for the whole session (see
        // RpcCombatEndedFallback's comment above for the same reasoning), so it acts as the relay
        // channel instead. Called server-side, right after any [Server]-context mutation of a
        // StarSysData.ShipsList (see FleetManager.ServerTransferShipToSystem/FromSystem/
        // BetweenSystems and LocalHumanPlayerController.CmdSyncStarSysRoster).
        // ---------------------------------------------------------------------------------------
        [Server]
        public void ServerSyncStarSysRoster(int starSysInt, List<int> shipIDs)
        {
            RpcSyncStarSysRoster(starSysInt, shipIDs);
        }

        [ClientRpc]
        private void RpcSyncStarSysRoster(int starSysInt, List<int> shipIDs)
        {
            StarSysController sysCon = StarSysManager.Instance?.GetStarSysControllerByInt(starSysInt);
            if (sysCon == null)
            {
                Debug.LogWarning($"RpcSyncStarSysRoster: no local StarSysController found for starSysInt={starSysInt} - roster sync dropped on this peer.");
                return;
            }

            List<ShipController> resolved = new List<ShipController>();
            foreach (int shipID in shipIDs)
            {
                ShipController shipCon = ShipManager.Instance?.GetShipControllerByShipID(shipID);
                if (shipCon != null)
                    resolved.Add(shipCon);
                else
                    Debug.LogWarning($"RpcSyncStarSysRoster: system '{sysCon.name}' could not resolve ShipID={shipID} to a local ShipController - this peer doesn't know about that ship yet, its roster will be short by one.");
            }

            sysCon.StarSysData.ShipsList = resolved;
            Debug.Log($"RpcSyncStarSysRoster: system '{sysCon.name}' roster synced - {resolved.Count}/{shipIDs.Count} ship(s) resolved locally.");
        }

        // ---------------------------------------------------------------------------------------
        // Claim/Terraform/Colonize replication. Same reasoning and relay channel as the roster sync
        // above - StarSysController has no NetworkIdentity, so this persistent scene NetworkBehaviour
        // is what every peer can reach. Unlike the roster sync (a pure resync of already-networked
        // data), StarSysController.ClaimSystem/TerraformSystem/ColonizeWithTransport are also called
        // directly, client-locally, by the acting player's own UI first (see
        // FleetMenuUIController.ClickClaimSystemButton/ClickTerraformButton/ClickColonizeButton) for
        // instant feedback - these Rpcs are what make that same mutation land on every OTHER peer too.
        // Each Rpc checks whether its own local StarSysData already reflects the change before
        // re-running the mutation, so it no-ops harmlessly on the initiating peer's own echo instead
        // of re-entering (and warning inside) an already-applied StarSysController method.
        // ---------------------------------------------------------------------------------------
        [Server]
        public void ServerClaimSystem(int starSysInt, CivEnum claimingCiv)
        {
            RpcClaimSystem(starSysInt, claimingCiv);
        }

        [ClientRpc]
        private void RpcClaimSystem(int starSysInt, CivEnum claimingCiv)
        {
            StarSysController sysCon = StarSysManager.Instance?.GetStarSysControllerByInt(starSysInt);
            if (sysCon == null)
            {
                Debug.LogWarning($"RpcClaimSystem: no local StarSysController found for starSysInt={starSysInt} - claim dropped on this peer.");
                return;
            }
            if (sysCon.StarSysData.CurrentOwnerCivEnum == claimingCiv)
                return; // already applied locally - see comment above

            CivController civCon = CivManager.Instance?.GetCivControllerByCivEnum(claimingCiv);
            if (civCon == null)
            {
                Debug.LogWarning($"RpcClaimSystem: could not resolve CivController for {claimingCiv} - claim dropped on this peer.");
                return;
            }
            sysCon.ClaimSystem(civCon);
        }

        [Server]
        public void ServerTerraformSystem(int starSysInt, int transportShipID)
        {
            RpcTerraformSystem(starSysInt, transportShipID);
        }

        [ClientRpc]
        private void RpcTerraformSystem(int starSysInt, int transportShipID)
        {
            StarSysController sysCon = StarSysManager.Instance?.GetStarSysControllerByInt(starSysInt);
            if (sysCon == null)
            {
                Debug.LogWarning($"RpcTerraformSystem: no local StarSysController found for starSysInt={starSysInt} - terraform dropped on this peer.");
                return;
            }
            if (sysCon.StarSysData.IsTerraforming)
                return; // already applied locally - see comment above

            ShipController transportShip = ShipManager.Instance?.GetShipControllerByShipID(transportShipID);
            if (transportShip == null)
            {
                Debug.LogWarning($"RpcTerraformSystem: could not resolve transport ShipID={transportShipID} on this peer - terraform dropped.");
                return;
            }
            sysCon.TerraformSystem(transportShip);
        }

        [Server]
        public void ServerColonizeSystem(int starSysInt, int transportShipID)
        {
            RpcColonizeSystem(starSysInt, transportShipID);
        }

        [ClientRpc]
        private void RpcColonizeSystem(int starSysInt, int transportShipID)
        {
            StarSysController sysCon = StarSysManager.Instance?.GetStarSysControllerByInt(starSysInt);
            if (sysCon == null)
            {
                Debug.LogWarning($"RpcColonizeSystem: no local StarSysController found for starSysInt={starSysInt} - colonize dropped on this peer.");
                return;
            }
            if (sysCon.StarSysData.IsColonizing)
                return; // already applied locally - see comment above

            ShipController transportShip = ShipManager.Instance?.GetShipControllerByShipID(transportShipID);
            if (transportShip == null)
            {
                Debug.LogWarning($"RpcColonizeSystem: could not resolve transport ShipID={transportShipID} on this peer - colonize dropped.");
                return;
            }
            sysCon.ColonizeWithTransport(transportShip);
        }

        // Method to get current oneInXChance
        public int CurrentStarDate()
        {
            return currentStardate;
        }
        public IEnumerator DelayedAction(float delay)
        {
            Debug.Log("Action before delay.");

            // Wait for 1/2 second
            yield return new WaitForSeconds(delay);

            Debug.Log("Action after delay.");
        }



        public void PauseForMessage(float delay)
        {
            if (!isPausing)
                StartCoroutine(PauseForMessageCoroutine(delay));
        }

        private IEnumerator PauseForMessageCoroutine(float delay)
        {
            isPausing = true;

            Time.timeScale = 0f;
            PauseTime();

            yield return new WaitForSecondsRealtime(delay);

            ResumeTime();
            Time.timeScale = 1f;

            isPausing = false;
        }
    

        /// <summary>
        /// Reset to InterTurn state — stops any running coroutine and fires the phase event.
        /// Called on scene load so that a DontDestroyOnLoad TimeManager never carries
        /// a stale TurnProgression state into a fresh GalaxyScene session.
        /// </summary>
        public void EnsureInterTurn()
        {
            if (timeCoroutine != null)
            {
                StopCoroutine(timeCoroutine);
                timeCoroutine = null;
            }
            timeRunning = false;
            IsPaused = true;
            Time.timeScale = 1f;
            SetTurnPhase(TurnPhase.InterTurn);
            Debug.Log("⏰ TimeManager: EnsureInterTurn — reset to InterTurn");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // When the galaxy scene is loaded (additively or directly) make sure we
            // start in InterTurn so the player controls are active from frame one.
            if (scene.name.Contains("Galaxy"))
                EnsureInterTurn();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ServiceLocator.Unregister<TimeManager>();
        }
}
}