using BOTF3D.Audio;
using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Main combat manager - orchestrates combat flow using specialized sub-managers.
    /// Refactored to delegate responsibilities to focused manager classes.
    /// </summary>
    public class CombatManager : MonoBehaviour, IManager
    {
        public static CombatManager Instance { get; private set; }

        [Header("Configuration")]
        public Config.GameConfig gameConfig;

        [Header("Combat Prefabs & Assets")]
public GameObject HealthbarPrefab;
        [SerializeField] private CombatController combatConPrefab;
        [SerializeField] private SoundData dropOutOfWarpSoundData;

        [Header("Weapon Prefabs")]
        public List<GameObject> TorpedoPrefabs;
        public List<GameObject> BeamPrefabs;

        [Header("Weapon Audio Clips")]
        public AudioClip[] BeamFireClips;
        public AudioClip[] TorpedoFireClips;

        // Specialized managers
        private CombatQueueManager queueManager;
        private CombatSceneLoader sceneLoader;
        private WeaponAssetProvider weaponProvider;
        private CombatInstantiator combatInstantiator;

        // Cached prefab reference
        private CombatController _cachedCombatConPrefab;

        // Public accessors for backwards compatibility
        public CombatController ActiveCombatController => queueManager?.ActiveCombatController;
        public GameObject CombatUICanvas => sceneLoader?.CombatUICanvas;
        public GameObject Combat3DCanvas => sceneLoader?.Combat3DCanvas;
        public GameObject GameOverCanvas => sceneLoader?.GameOverCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Combat, "Duplicate CombatManager found! Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            gameConfig?.ApplyLogSettings();

            // Cache the prefab BEFORE DontDestroyOnLoad
            _cachedCombatConPrefab = combatConPrefab;

            if (_cachedCombatConPrefab == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ combatConPrefab is NULL in Awake! Check Inspector assignment.", this);
            }
            else
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, $"✅ combatConPrefab cached successfully: {_cachedCombatConPrefab.name}", this);
            }

            DontDestroyOnLoad(gameObject);

            // Verify it survived the move
            if (combatConPrefab == null && _cachedCombatConPrefab != null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Combat, "⚠️ combatConPrefab was cleared by DontDestroyOnLoad - restoring from cache", this);
                combatConPrefab = _cachedCombatConPrefab;
            }

            // Register with ServiceLocator
            ServiceLocator.Register<CombatManager>(this);

            // Initialize
            Initialize();
        }

        public void Initialize()
        {
            // Initialize specialized managers
            InitializeManagers();

            GameLogger.Log(GameLogger.LogCategory.Combat, "✅ CombatManager initialized with specialized sub-managers.", this);
        }

        public void Cleanup()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "🧹 CombatManager: Cleaning up...", this);
            CleanupCombat();
        }

        private void OnDestroy()
        {
            Cleanup();
            ServiceLocator.Unregister<CombatManager>();
        }

        /// <summary>
        /// Initialize all specialized manager classes
        /// </summary>
        private void InitializeManagers()
        {
            // Queue manager for sequential combat processing
            queueManager = new CombatQueueManager(this);
            queueManager.OnCombatControllerRequested += HandleCombatControllerRequest;
            queueManager.OnCombatSetupNeeded += SetUpLocalPlayer;

            // Scene loader for finding combat scene objects
            sceneLoader = new CombatSceneLoader();

            // Weapon asset provider for civ-specific weapons
            weaponProvider = new WeaponAssetProvider(
                TorpedoPrefabs,
                BeamPrefabs,
                BeamFireClips,
                TorpedoFireClips
            );

            // Combat instantiator for creating combat controllers
            combatInstantiator = new CombatInstantiator(
                combatConPrefab,
                transform,
                dropOutOfWarpSoundData,
                sceneLoader,
                weaponProvider
            );

            GameLogger.Log(GameLogger.LogCategory.Combat, "✅ Specialized managers initialized", this);
        }

        /// <summary>
        /// Request a combat - will be queued if another is active
        /// </summary>
        public void RequestCombat(List<ShipController> sideOneShips, List<ShipController> sideTwoShips, CombatType combatType, StarSysController starSysCon = null)
        {
            queueManager.RequestCombat(sideOneShips, sideTwoShips, combatType, starSysCon);
        }

        /// <summary>
        /// Handle combat controller creation request from queue manager
        /// </summary>
        private void HandleCombatControllerRequest(
            List<ShipController> sideOneShips,
            List<ShipController> sideTwoShips,
            StarSysController starSysCon,
            System.Action<CombatController> callback)
        {
            // Find scene references
            sceneLoader.FindCombatSceneReferences();

            // Create combat controller
            CombatController controller = combatInstantiator.InstantiateCombatController(
                sideOneShips,
                sideTwoShips,
                starSysCon
            );

            callback?.Invoke(controller);
        }

        /// <summary>
        /// Called when a combat ends
        /// </summary>
        public void OnCombatEnded(CombatController controller)
        {
            queueManager.OnCombatEnded(controller);
            combatInstantiator.RemoveCombatController(controller);
            Destroy(controller.gameObject);
        }

        /// <summary>
        /// End combat time pause
        /// </summary>
        public void EndCombatTimePause()
        {
            TimeManager.Instance.ResumeTime();
        }

        /// <summary>
        /// Setup local player UI after combat starts
        /// </summary>
        public void SetUpLocalPlayer()
        {
            StartCoroutine(SetUpLocalPlayerAfterSceneLoad());
        }

        /// <summary>
        /// Setup local player UI after CombatScene fully loads
        /// </summary>
        private IEnumerator SetUpLocalPlayerAfterSceneLoad()
        {
            // Wait two frames per copilot-instructions.md
            yield return null;
            yield return null;

            GameObject thisCombatUIGameObject = sceneLoader.CombatUICanvas;

            if (thisCombatUIGameObject == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ CombatUICanvas is null - cannot setup UI!", this);
                yield break;
            }

            // CombatUIManager and ActiveCombatController may not be ready yet when
            // loading directly via the Scenario Editor. Retry for up to 3 seconds.
            float timeout = 3f;
            while ((CombatUIManager.Instance == null || ActiveCombatController == null) && timeout > 0f)
            {
                yield return null;
                timeout -= Time.unscaledDeltaTime;
            }

            if (CombatUIManager.Instance != null && ActiveCombatController != null)
            {
                CombatUIManager.Instance.SetupForCombat(
                    ActiveCombatController,
                    thisCombatUIGameObject,
                    sceneLoader.Combat3DCanvas,
                    sceneLoader.GameOverCanvas
                );
                GameLogger.Log(GameLogger.LogCategory.Combat, "✅ CombatUIManager configured for local player", this);
            }
            else
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ CombatUIManager.Instance or ActiveCombatController is null after retry!", this);
            }
        }

        /// <summary>
        /// Set diplomacy controller (legacy method for diplomacy-triggered combat)
        /// </summary>
        internal void SetDiplomacyController(DiplomacyController diplomacyController)
        {
            var sideOneShips = new List<ShipController>();
            var sideTwoShips = new List<ShipController>();
            
            // Note: IntelligenceManager should ideally be accessed via ServiceLocator
            var intelManager = ServiceLocator.Get<IntelligenceManager>();
            if (intelManager == null)
            {
                 // Fallback to singleton if not yet migrated
                 intelManager = IntelligenceManager.Instance;
            }

            var intelCon = intelManager?.ReturnAnIntelligenceController(
                diplomacyController.DiplomacyData.CivEnumSideOne,
                diplomacyController.DiplomacyData.CivEnumSideTwo
            );

            if (intelCon != null)
            {
                if (intelCon.IntelligenceData.LastSeenFleetOfSideOne != null)
                {
                    sideOneShips = intelCon.IntelligenceData.LastSeenFleetOfSideOne.FleetData.ShipsList;
                    if (intelCon.IntelligenceData.LastSeenFleetOfSideTwo != null)
                    {
                        sideTwoShips = intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData.ShipsList;
                        if (sideOneShips.Count > 0 && sideTwoShips.Count > 0)
                        {
                            RequestCombat(sideOneShips, sideTwoShips, CombatType.FleetVsFleet);
                        }
                    }
                    else if (intelCon.IntelligenceData.LastSeenStarSysController != null)
                    {
                        sideTwoShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;
                        if (sideOneShips.Count > 0 && sideTwoShips.Count > 0)
                        {
                            RequestCombat(sideOneShips, sideTwoShips, CombatType.FleetVsSystem, intelCon.IntelligenceData.LastSeenStarSysController);
                        }
                    }
                }
                else if (intelCon.IntelligenceData.LastSeenFleetOfSideTwo != null && intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData != null)
                {
                    sideTwoShips = intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData.ShipsList;
                    sideOneShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;

                    if (sideOneShips.Count > 0 && sideTwoShips.Count > 0)
                    {
                        RequestCombat(sideOneShips, sideTwoShips, CombatType.SystemVsFleet, intelCon.IntelligenceData.LastSeenStarSysController);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the active (not-yet-ended) combat between the two given civs, for use by
        /// networked order RPCs that only have civ identities to work with (not a local instance ID).
        /// </summary>
        public CombatController GetActiveCombatControllerForCivs(CivEnum civA, CivEnum civB)
        {
            var allCombatControllers = combatInstantiator.GetAllCombatControllers();
            for (int i = 0; i < allCombatControllers.Count; i++)
            {
                CombatController controller = allCombatControllers[i];
                if (controller.CombatEnded || controller.CombatData == null) continue;

                bool matchesForward = controller.CombatData.CivEnumSideOne == civA && controller.CombatData.CivEnumSideTwo == civB;
                bool matchesReverse = controller.CombatData.CivEnumSideOne == civB && controller.CombatData.CivEnumSideTwo == civA;
                if (matchesForward || matchesReverse)
                    return controller;
            }
            return null;
        }

        /// <summary>
        /// Same as <see cref="GetActiveCombatControllerForCivs"/> but for callers (e.g. an order-submission
        /// Command) that only have the submitting side's civ, not the opponent's.
        /// </summary>
        public CombatController GetActiveCombatControllerForCiv(CivEnum civ)
        {
            var allCombatControllers = combatInstantiator.GetAllCombatControllers();
            for (int i = 0; i < allCombatControllers.Count; i++)
            {
                CombatController controller = allCombatControllers[i];
                if (controller.CombatEnded || controller.CombatData == null) continue;

                if (controller.CombatData.CivEnumSideOne == civ || controller.CombatData.CivEnumSideTwo == civ)
                    return controller;
            }
            return null;
        }

        /// <summary>
        /// Remove a ship controller from all combat tracking (legacy method)
        /// </summary>
        internal void RemoveThisShipController(ShipController shipController)
        {
            var allCombatControllers = combatInstantiator.GetAllCombatControllers();

            for (int i = 0; i < allCombatControllers.Count; i++)
            {
                for (int j = 0; j < allCombatControllers[i].CombatData.SideOneShipCons.Count; j++)
                {
                    if (allCombatControllers[i].CombatData.SideOneShipCons[j] == shipController)
                    {
                        bool removed = allCombatControllers[i].CombatData.SideOneShipCons.Remove(shipController);
                        if (removed)
                        {
                            GameLogger.Log(GameLogger.LogCategory.Combat, $"Removed {shipController.ShipData.ShipName} from Side One", this);
                        }
                        break;
                    }
                }

                for (int j = 0; j < allCombatControllers[i].CombatData.SideTwoShipCons.Count; j++)
                {
                    if (allCombatControllers[i].CombatData.SideTwoShipCons[j] == shipController)
                    {
                        bool removed = allCombatControllers[i].CombatData.SideTwoShipCons.Remove(shipController);
                        if (removed)
                        {
                            GameLogger.Log(GameLogger.LogCategory.Combat, $"Removed {shipController.ShipData.ShipName} from Side Two", this);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Clean up after combat ends
        /// </summary>
        private void CleanupCombat()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "🧹 CombatManager: Cleaning up combat...", this);

            // Clear Unity Editor selection to prevent MissingReferenceException
#if UNITY_EDITOR
            UnityEditor.Selection.activeObject = null;
#endif

            // Clear scene loader references
            sceneLoader?.ClearReferences();

            GameLogger.Log(GameLogger.LogCategory.Combat, "✅ Combat cleanup complete", this);
        }
    }
}

