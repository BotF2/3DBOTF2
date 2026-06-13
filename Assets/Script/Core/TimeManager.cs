using BOTF3D.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    public class TimeManager : MonoBehaviour, IManager
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

        public int currentStardate { get; private set; }

        public bool timeRunning = true; // ✅ Change from false to true
        public bool IsPaused { get; private set; } = false; // Already correct
        private Coroutine timeCoroutine;
        private float currentTimeSpeed = 10f; // This controls YOUR coroutine delay
        private float unityTimeScale = 1f; // ✅ Add this for Unity's Time.timeScale
        private bool isPausing = false;
        public List<TrekRandomEventSO> RandomEvents;
        public List<TrekStardateEventSO> StardateEvents;

        public int StaringStardate = 1010; // the starting stardate
        //private float currentTimeSpeed;

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

            DontDestroyOnLoad(gameObject); // ✅ Move here

            // ✅ Initialize to running state
            IsPaused = false;
            timeRunning = true;

            Debug.Log($"⏰ TimeManager: Initialized - IsPaused={IsPaused}, timeRunning={timeRunning}");
        }
        private void Start()
        {
            // timer = currentTimeSpeed;
            timeCoroutine = StartCoroutine(TimeProgression());
            currentStardate = StaringStardate;

            // ✅ Ensure running state
            IsPaused = false;
            timeRunning = true;

            Debug.Log($"⏰ TimeManager: Started - currentStardate={currentStardate}");
        }
        void Update()
        {

        }
        public void StartTime()
        {
            if (timeCoroutine != null)
            {
                StopCoroutine(timeCoroutine);
            }
            timeCoroutine = StartCoroutine(TimeProgression());
            IsPaused = false; // ✅ FIX: Starting time means NOT paused
            timeRunning = true; // ✅ Also set this
            Debug.Log("⏰ TimeManager: Time started via StartTime()");
        }
        private System.Collections.IEnumerator TimeProgression()
        {

            while (timeRunning)
            {
                yield return new WaitForSeconds(10f / currentTimeSpeed); // 10 seconds in game = 1 turn
                currentStardate++;
                OnStardateChanged?.Invoke();

                // ✅ NEW: Process research for all civs each turn
                ProcessTurnEvents();

                CheckSpecialEvents();
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
                    }
                }
            }
        }
        /// <summary>
        /// Process all turn-based events (research, production, etc.)
        /// </summary>
        private void ProcessTurnEvents()
        {
            // Process research for all civilizations
            if (TechManager.Instance != null)
            {
                TechManager.Instance.ProcessResearchForAllCivs();
            }

            // Process espionage intel generation and project advancement
            if (IntelligenceManager.Instance != null && CivManager.Instance != null)
            {
                foreach (var civCon in CivManager.Instance.CivControllersInGame)
                {
                    IntelligenceManager.Instance.ProcessEspionageTurn(civCon);
                }
            }

            // TODO: Add other turn-based processing here
            // - Population growth
            // - Credits/income
            // - Random events
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
            Time.timeScale = 0f; // ✅ Correct - freeze Unity time
            Debug.Log("⏸ TimeManager: Time PAUSED");
        }

        public void ResumeTime()
        {
            timeRunning = true;
            IsPaused = false;
            Time.timeScale = 1f; // ✅ FIX: Use 1.0, not currentTimeSpeed (which is 10)
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
    

        private void OnDestroy()
        {
            ServiceLocator.Unregister<TimeManager>();
        }
}
}