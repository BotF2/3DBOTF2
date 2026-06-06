using BOTF3D.Civilization;
using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BOTF3D.Combat.Testing
{
    /// <summary>
    /// Runs combat scenarios from the Combat Scenario Editor.
    /// Creates ships, loads combat scene, and starts combat with specified parameters.
    /// </summary>
    public class CombatScenarioRunner : MonoBehaviour
    {
        public static CombatScenarioRunner Instance { get; private set; }

        [Header("Test Configuration")]
        public bool AutoStartOnPlay = false;
        public CombatScenario TestScenario;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (AutoStartOnPlay && TestScenario != null)
            {
                StartCoroutine(RunScenarioDelayed(TestScenario, 1f));
            }
        }

        /// <summary>
        /// Run a combat scenario (called from Combat Scenario Editor)
        /// </summary>
        public void RunScenario(CombatScenario scenario)
        {
            if (scenario == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ CombatScenarioRunner: Cannot run null scenario");
                return;
            }

            if (!Application.isPlaying)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ CombatScenarioRunner: Must be in Play Mode to run scenarios");
                return;
            }

            StartCoroutine(RunScenarioCoroutine(scenario));
        }

        /// <summary>
        /// Run scenario with a delay (useful for Start() initialization)
        /// </summary>
        private IEnumerator RunScenarioDelayed(CombatScenario scenario, float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return RunScenarioCoroutine(scenario);
        }

        /// <summary>
        /// Main coroutine to setup and run a combat scenario
        /// </summary>
        private IEnumerator RunScenarioCoroutine(CombatScenario scenario)
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, $"🎬 CombatScenarioRunner: Starting scenario '{scenario.name}'");

            // Validate core managers (TimeManager, SceneController)
            if (!ValidateCoreManagers())
            {
                yield break;
            }

            // Pause game time during combat
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.PauseTime();
            }

            // STEP 1: Load CombatScene additively
            // CombatManager and ShipManager don't exist yet - they will be created when CombatScene loads
            GameLogger.Log(GameLogger.LogCategory.Combat, "📦 Loading CombatScene to initialize combat managers...");

            // Load CombatScene additively for testing scenarios
            var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("CombatScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);

            // Wait for scene load to complete
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, "✅ Combat scene loaded");

            // Get scene references
            UnityEngine.SceneManagement.Scene galaxyScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("GalaxyScene");
            UnityEngine.SceneManagement.Scene combatScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("CombatScene");

            // Disable all EventSystems first (before deactivating scenes)
            var allEventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>();
            foreach (var es in allEventSystems)
            {
                es.enabled = false;
            }

            // Deactivate galaxy scene objects (mimics normal combat flow)
            if (galaxyScene.isLoaded)
            {
                foreach (GameObject go in galaxyScene.GetRootGameObjects())
                {
                    go.SetActive(false);
                }
                GameLogger.Log(GameLogger.LogCategory.Combat, "✅ Galaxy scene deactivated");
            }

            // Set combat scene as active
            if (combatScene.isLoaded)
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(combatScene);
                GameLogger.Log(GameLogger.LogCategory.Combat, "✅ Combat scene set as active");
            }

            // Activate combat scene objects
            foreach (var obj in combatScene.GetRootGameObjects())
            {
                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                }
            }

            // Enable combat EventSystem only
            foreach (var obj in combatScene.GetRootGameObjects())
            {
                var es = obj.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true);
                if (es != null)
                {
                    es.enabled = true;
                    break; // Only enable the first one
                }
            }

            // Enable combat camera explicitly (mimics SceneController behavior)
            if (ShipCombatCameraController.Instance != null)
            {
                var camera = ShipCombatCameraController.Instance.GetComponentInChildren<Camera>();
                if (camera != null)
                {
                    camera.enabled = true;
                    GameLogger.Log(GameLogger.LogCategory.Combat, "✅ Combat camera enabled");
                }
            }

            // Wait for Awake() and Start() to run
            yield return null;
            yield return null;

            // STEP 2: Wait for combat managers to be created
            float timeout = 5f;
            float elapsed = 0f;
            while ((CombatManager.Instance == null || ShipManager.Instance == null) && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (!ValidateCombatManagers())
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ Combat managers failed to initialize after loading CombatScene");
                yield break;
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, "✅ Combat managers initialized");

            // STEP 3: Create ships for both sides (NOW that ShipManager exists)
            List<ShipController> sideOneShips = CreateShipsForSide(scenario, 1);
            List<ShipController> sideTwoShips = CreateShipsForSide(scenario, 2);

            if (sideOneShips.Count == 0 || sideTwoShips.Count == 0)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ CombatScenarioRunner: Failed to create ships");
                yield break;
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, $"✅ Created {sideOneShips.Count} ships for Side 1, {sideTwoShips.Count} for Side 2");

            // STEP 4: Request combat through CombatManager
            CombatManager.Instance.RequestCombat(sideOneShips, sideTwoShips, CombatType.FleetVsFleet);
            GameLogger.Log(GameLogger.LogCategory.Combat, $"✅ Combat requested: {scenario.sideOneCiv} vs {scenario.sideTwoCiv}");

            // STEP 5: Wait for combat controller to be created
            yield return new WaitUntil(() => CombatManager.Instance.ActiveCombatController != null);

            // STEP 6: Set initial orders on the controller (writes to CombatData and each ship's state machine)
            var combatController = CombatManager.Instance.ActiveCombatController;
            combatController.SetShipOrders(scenario.sideOneOrder, scenario.sideOneCiv, 1);
            combatController.SetShipOrders(scenario.sideTwoOrder, scenario.sideTwoCiv, 2);

            GameLogger.Log(GameLogger.LogCategory.Combat, $"✅ Orders set - Side 1: {scenario.sideOneOrder}, Side 2: {scenario.sideTwoOrder}");

            // STEP 7: Sync Combat UI — orders, civ names, and tech levels — from the scenario editor
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.PreloadScenarioData(
                    scenario.sideOneOrder, scenario.sideTwoOrder,
                    scenario.sideOneCiv,   scenario.sideOneTechLevel,
                    scenario.sideTwoCiv,   scenario.sideTwoTechLevel);
                GameLogger.Log(GameLogger.LogCategory.Combat, "✅ Combat UI synced from scenario editor");
            }
            else
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Combat, "⚠️ CombatUIManager.Instance not found - UI will not reflect scenario settings");
            }
        }

        /// <summary>
        /// Validate that core managers exist (these should exist in PersistentScene/GalaxyScene)
        /// </summary>
        private bool ValidateCoreManagers()
        {
            if (TimeManager.Instance == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ TimeManager.Instance is null - ensure PersistentScene is loaded");
                return false;
            }

            if (SceneController.Instance == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ SceneController.Instance is null - ensure PersistentScene is loaded");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate that combat managers exist (these are created when CombatScene loads)
        /// </summary>
        private bool ValidateCombatManagers()
        {
            if (ShipManager.Instance == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ ShipManager.Instance is null - CombatScene may not have loaded properly");
                return false;
            }

            if (CombatManager.Instance == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ CombatManager.Instance is null - CombatScene may not have loaded properly");
                return false;
            }

            if (CivManager.Instance == null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Combat, "⚠️ CivManager.Instance is null - civs may not be initialized");
                // Not fatal - we can still create ships
            }

            return true;
        }

        /// <summary>
        /// Create ships for one side based on scenario configuration
        /// </summary>
        private List<ShipController> CreateShipsForSide(CombatScenario scenario, int side)
        {
            List<ShipController> ships = new List<ShipController>();

            CivEnum civEnum;
            TechLevel techLevel;
            int scouts, destroyers, cruisers, hvyCruisers, ltCruiser, transports;

            if (side == 1)
            {
                civEnum = scenario.sideOneCiv;
                techLevel = scenario.sideOneTechLevel;
                scouts = scenario.s1Scouts;
                destroyers = scenario.s1Destroyers;
                cruisers = scenario.s1Cruisers;
                hvyCruisers = scenario.s1HvyCruisers;
                ltCruiser = scenario.s1LtCruisers;
                transports = scenario.s1Transports;
            }
            else
            {
                civEnum = scenario.sideTwoCiv;
                techLevel = scenario.sideTwoTechLevel;
                scouts = scenario.s2Scouts;
                destroyers = scenario.s2Destroyers;
                cruisers = scenario.s2Cruisers;
                hvyCruisers = scenario.s2HvyCruisers;
                ltCruiser = scenario.s2LtCruisers;
                transports = scenario.s2Transports;
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, $"  Side {side}: Creating {civEnum} ships at tech level {techLevel}");

            // Create ships by type
            CreateShipsOfType(ShipType.Scout, scouts, civEnum, techLevel, ships);
            CreateShipsOfType(ShipType.Destroyer, destroyers, civEnum, techLevel, ships);
            CreateShipsOfType(ShipType.Cruiser, cruisers, civEnum, techLevel, ships);
            CreateShipsOfType(ShipType.LtCruiser, ltCruiser, civEnum, techLevel, ships);
            CreateShipsOfType(ShipType.HvyCruiser, hvyCruisers, civEnum, techLevel, ships);
            CreateShipsOfType(ShipType.Transport, transports, civEnum, techLevel, ships);

            return ships;
        }

        /// <summary>
        /// Returns true if the ship type is available at the given tech level.
        /// EARLY     : Scout, Destroyer, Transport
        /// DEVELOPED : Scout, Destroyer, Cruiser, Transport
        /// ADVANCED  : Scout, Destroyer, Cruiser, Transport
        /// SUPREME   : Scout, Destroyer, LtCruiser, HvyCruiser, Transport
        /// </summary>
        public static bool IsShipTypeAvailableAtTechLevel(ShipType shipType, TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    return shipType == ShipType.Scout || shipType == ShipType.Destroyer || shipType == ShipType.Transport;
                case TechLevel.DEVELOPED:
                case TechLevel.ADVANCED:
                    return shipType == ShipType.Scout || shipType == ShipType.Destroyer ||
                           shipType == ShipType.Cruiser || shipType == ShipType.Transport;
                case TechLevel.SUPREME:
                    return shipType == ShipType.Scout || shipType == ShipType.Destroyer ||
                           shipType == ShipType.LtCruiser || shipType == ShipType.HvyCruiser ||
                           shipType == ShipType.Transport;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Create multiple ships of a specific type
        /// </summary>
        private void CreateShipsOfType(ShipType shipType, int count, CivEnum civEnum, TechLevel techLevel, List<ShipController> outputList)
        {
            if (count <= 0) return;

            if (!IsShipTypeAvailableAtTechLevel(shipType, techLevel))
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, $"❌ {shipType} is not available at TechLevel.{techLevel} — skipping {count} {civEnum} {shipType}(s). Check scenario configuration.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                ShipController ship = CreateSingleShip(shipType, civEnum, techLevel, i);
                if (ship != null)
                {
                    outputList.Add(ship);
                }
            }
        }

        /// <summary>
        /// Create a single ship using ShipManager's normal flow
        /// </summary>
        private ShipController CreateSingleShip(ShipType shipType, CivEnum civEnum, TechLevel techLevel, int index)
        {
            // Get ship template (ScriptableObject)
            ShipSO shipSO = ShipManager.Instance.GetShipSOAtBestTechLevel(shipType, techLevel, civEnum);

            if (shipSO == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, $"❌ No ShipSO found for {civEnum} {shipType} at {techLevel} — verify that {civEnum} has a {shipType} ShipSO configured in ShipManager.");
                return null;
            }

            // Unity's ?. operator does not prevent UnassignedReferenceException on serialized fields;
            // use != null instead so the check goes through Unity's overloaded equality operator.
            string fbxName = shipSO.ShipFBX_ModelAsGOPrefab != null ? shipSO.ShipFBX_ModelAsGOPrefab.name : "NULL";
            GameLogger.Log(GameLogger.LogCategory.Combat, $"  Creating {civEnum} {shipType} using ShipSO: {shipSO.ShipName} (FBX: {fbxName})");

            if (shipSO.ShipFBX_ModelAsGOPrefab == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat,
                    $"❌ {civEnum} {shipType} ShipSO '{shipSO.ShipName}' has no FBX model prefab assigned. " +
                    $"Open the ShipSO asset in the Inspector and assign ShipFBX_ModelAsGOPrefab.");
                return null;
            }

            // Create temporary parent (DontDestroyOnLoad prevents it from being in a scene, avoiding UI parent check)
            GameObject tempParent = new GameObject($"TestShipParent_{civEnum}_{shipType}_{index}");
            DontDestroyOnLoad(tempParent);

            // Use ShipManager's normal instantiation (creates ShipController properly with all data)
            // This WILL log UI parent errors, but we'll clean up the invalid UI afterward
            List<ShipSO> shipSOList = new List<ShipSO> { shipSO };
            List<ShipController> shipList = ShipManager.Instance.InstantiateShipControllersWithDataFromSO(shipSOList, tempParent);

            ShipController ship = shipList.Count > 0 ? shipList[0] : null;

            if (ship == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, $"❌ Failed to create ship: {shipType}");
                Destroy(tempParent);
                return null;
            }

            // Update ship name and enforce correct civ (SO may have been a fallback from a different civ)
            ship.ShipData.ShipName = $"{civEnum}_{shipType}_{index + 1}";
            ship.ShipData.CivEnum = civEnum;

            // Unparent from temp parent (CombatController will handle positioning)
            ship.transform.SetParent(null);
            Destroy(tempParent);

            // Clean up any invalid UI that was created
            if (ship.ShipListUIGameObject != null)
            {
                Destroy(ship.ShipListUIGameObject);
                ship.ShipListUIGameObject = null;
            }

            // Deactivate ship until combat starts (CombatController will activate it)
            ship.gameObject.SetActive(false);

            GameLogger.Log(GameLogger.LogCategory.Combat, $"  ✅ Created {ship.ShipData.ShipName} (HP: {ship.ShipData.HullHealth + ship.ShipData.ShieldHealth})");

            return ship;
        }

        /// <summary>
        /// Stop the current scenario (for cleanup)
        /// </summary>
        public void StopScenario()
        {
            StopAllCoroutines();
            GameLogger.Log(GameLogger.LogCategory.Combat, "🛑 CombatScenarioRunner: Scenario stopped");
        }
    }

    /// <summary>
    /// Data class for combat scenarios (serializable for editor)
    /// </summary>
    [System.Serializable]
    public class CombatScenario
    {
        public string name = "Test Scenario";

        [Header("Civilizations, Tech Levels & Orders")]
        public CivEnum sideOneCiv = CivEnum.FED;
        public TechLevel sideOneTechLevel = TechLevel.EARLY;
        public CombatOrders sideOneOrder = CombatOrders.Engage;

        public CivEnum sideTwoCiv = CivEnum.KLING;
        public TechLevel sideTwoTechLevel = TechLevel.EARLY;
        public CombatOrders sideTwoOrder = CombatOrders.Engage;

        [Header("Side One Fleet")] // number of each ship type for side one (these will be created in the scenario)
        public int s1Scouts = 1;
        public int s1Destroyers = 1;
        public int s1Cruisers = 0;
        public int s1LtCruisers = 0;
        public int s1HvyCruisers = 0;
        public int s1Transports = 0;

        [Header("Side Two Fleet")]
        public int s2Scouts = 1;
        public int s2Destroyers = 1;
        public int s2Cruisers = 0;
        public int s2LtCruisers = 0;
        public int s2HvyCruisers = 0;
        public int s2Transports = 0;
    }

    /// <summary>
    /// Wrapper for scenario list (for JSON serialization)
    /// </summary>
    [System.Serializable]
    public class ScenarioList
    {
        public List<CombatScenario> scenarios = new List<CombatScenario>();
    }
}
