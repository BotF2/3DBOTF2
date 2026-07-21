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
        /// Called by the "Advance Turn" button (or AI). Server-authoritative - clients must go
        /// through RequestAdvanceTurn() below, which relays here via Command.
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
        /// Entry point for the "Advance Turn" button (GameControlOverlay) and any AI caller.
        /// TimeManager is a scene-singleton NetworkBehaviour with no owning connection (same as
        /// FleetController's order relays), so non-host clients relay through a
        /// requiresAuthority = false Command rather than calling AdvanceTurn() directly.
        /// </summary>
        public void RequestAdvanceTurn()
        {
            if (isServer)
                AdvanceTurn();
            else
            {
                Debug.Log("⏰ TimeManager: RequestAdvanceTurn - relaying via CmdAdvanceTurn (non-host client).");
                CmdAdvanceTurn();
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdAdvanceTurn(NetworkConnectionToClient sender = null)
        {
            Debug.Log($"⏰ TimeManager: CmdAdvanceTurn received from connection {sender?.connectionId}");
            AdvanceTurn();
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