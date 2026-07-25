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

        public int StaringStardate = 1010; // the starting stardate

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

        /// <summary>
        /// Called by GalaxyEncounterQueue (or diplomacy panel close) when all queued
        /// encounters for this turn have been resolved.
        /// </summary>
        public void OnEncounterQueueEmpty()
        {
            if (TurnPhase == TurnPhase.EncounterResolution)
            {
                SetTurnPhase(TurnPhase.InterTurn);
                Debug.Log("⏰ TimeManager: All encounters resolved — InterTurn");
            }
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
        private void RefreshBuildUIsForCiv(CivEnum civEnum)
        {
            if (StarSysManager.Instance == null) return;

            var civController = CivManager.Instance?.GetCivControllerByCivEnum(civEnum);
            if (civController?.CivData?.StarSysWeOwn == null) return;

            Debug.Log($"  Refreshing build UIs for {civController.CivData.StarSysWeOwn.Count} systems owned by {civEnum}");

            foreach (var system in civController.CivData.StarSysWeOwn)
            {
                if (system != null)
                {
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
                    }
                }
            }
        }
        /// <summary>
        /// Process all turn-based events (research, production, etc.) then pause the clock.
        /// Encounters queued during TurnProgression are drained before returning to InterTurn.
        /// </summary>
        private void ProcessTurnEvents()
        {
            if (TechManager.Instance != null)
                TechManager.Instance.ProcessResearchForAllCivs();

            // TODO: population growth, credits/income, random events

            var queue = BOTF3D.Galaxy.GalaxyEncounterQueue.Instance;
            if (queue != null && queue.HasPending)
            {
                SetTurnPhase(TurnPhase.EncounterResolution);
                PauseTime();
                queue.DrainAll();
                // DrainAll() resolves every queued encounter synchronously (opening any diplomacy
                // panels non-blocking) and returns immediately - it does not wait on player input.
                // Nothing else calls back into TimeManager when it's done, so we must return to
                // InterTurn here ourselves or turn advancement soft-locks forever.
                OnEncounterQueueEmpty();
            }
            else
            {
                SetTurnPhase(TurnPhase.InterTurn);
                PauseTime();
            }
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