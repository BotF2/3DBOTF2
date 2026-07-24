// Ignore Spelling: shiptype Sys hvy BOTF
using BOTF3D.Civilization;
using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.UI;
using FischlWorks_FogWar;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Instantiates the star system (a StarSysController and a StarSysData) using StarSysSO.
    /// Manages Star Systems, their initialization, facilities, and UI.
    /// </summary>
    public class StarSysManager : MonoBehaviour, IManager
    {
        public void Initialize() { }
        public void Cleanup() { }
        public static StarSysManager Instance;

        [Header("Scene References")]
        [SerializeField] private GameObject galaxyCenter; // Assign in Inspector
        [SerializeField] private Camera galaxyCamera; // Assign MainCamera in Inspector        
        [SerializeField]
        private List<StarSysSO> starSysSOList; // get StarSysSO for civ by int
        [SerializeField]
        private GameObject sysBuildUIListPrefab;
        [SerializeField]
        private List<PowerPlantSO> powerPlantSOList; // get PowerPlantSO for civ by int
        [SerializeField]
        private List<FactorySO> factorySOList; // get factorySO for civ by int
        [SerializeField]
        private List<ShipyardSO> shipyardSOList; // get shipyardSO for civ by int
        [SerializeField]
        private List<ShieldGeneratorSO> shieldGeneratorSOList; // get shieldGeneratorSO for civ by int
        [SerializeField]
        private List<OrbitalBatterySO> orbitalBatterySOList; // get OrbitalBatterySO for civ by int
        [SerializeField]
        private List<ResearchCenterSO> researchCenterSOList; // get factorySO for civ by int
        [SerializeField]
        private StarSysController sysPrefab;

        [SerializeField]
        private GameObject sysUIPrefab;
        [SerializeField]
        private GameObject shipBuildUIPrefab;
        public List<StarSysController> StarSysControllerList { get; private set; } = new List<StarSysController>();
        public GameObject PowerPlantPrefab;
        public GameObject FactoryPrefab;
        public GameObject ShipyardPrefab;
        public GameObject ShieldGeneratorPrefab;
        public GameObject OrbitalBatteryPrefab;
        public GameObject ResearchCenterPrefab;

        private GameObject powerPlantInventorySlot;
        private GameObject factoryInventorySlot;
        private GameObject shipyardInventorySlot;
        private GameObject shieldGenInventorySlot;
        private GameObject orbitalBatteryInventorySlot;
        private GameObject researchCenterInventory_slot;

        public GameObject scoutBluePrintPrefab;
        public GameObject destroyerBluePrintPrefab;
        public GameObject cruiserBluePrintPrefab;
        public GameObject ltCruiserBluePrintPrefab;
        public GameObject hvyCruiserBluePrintPrefab;
        public GameObject transportBluePrintPrefab;
        private GameObject scoutInventorySlot;
        private GameObject destroyerInventorySlot;
        private GameObject cruiserInventorySlot;
        private GameObject ltCruiserInventorySlot;
        private GameObject hvyCruiserInventorySlot;
        private GameObject transportInventorySlot;

        [SerializeField]
        private GameObject factoryBuildItemPrefab;
        [SerializeField]
        private GameObject powerPlantInventorySlotPrefab;
        [SerializeField]
        private GameObject factoryInventorySlotPrefab;
        [SerializeField]
        private GameObject shipyardInventorySlotPrefab;
        [SerializeField]
        private GameObject shieldGenInventorySlotPrefab;
        [SerializeField]
        private GameObject orbitalBatteryInventorySlotPrefab;
        [SerializeField]
        private GameObject researchCenterInventorySlotPrefab;
        [SerializeField]
        private GameObject sysUIGOContentParent;
        [SerializeField]
        private GameObject sysShipsContentFolderParent;
        [SerializeField]
        private ThemeSO localPlayerTheme;
        [SerializeField]
        private GameObject galaxyImage;
        [SerializeField]
        private GameObject canvasBuildList;
        private StarSysController currentBuildUISysCon;
        public StarSysController CurrentBuildUISysCon => currentBuildUISysCon;
        [SerializeField]
        private Sprite unknowSystem;
        private int starSystemCounter = 0;
        private List<CivEnum> localPlayerCanSeeMyNameList = new List<CivEnum>();
        // StarSysUI_ListContainer removed — system UIs live in SysListContainer (SystemsMenuView/Viewport/SysListContainer)

        // ✅ NEW: Random position pool for RANDOM galaxy type
        private List<Vector3> randomPositionPool;
        private int randomPositionIndex = 0;

        private void Awake()
        {
            ServiceLocator.Register<StarSysManager>(this);
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                // No DontDestroyOnLoad - dies with GalaxyScene

                Debug.Log("StarSysManager: Awake - Instance created");

                // Load all StarSysSO assets
                LoadStarSystemScriptableObjects();

                // Find critical references EARLY
                FindGalaxyReferences();
            }
        }

        private void LoadStarSystemScriptableObjects()
        {
            // ✅ Check if list needs loading: null, empty, or contains null entries
            bool needsLoading = starSysSOList == null ||
                               starSysSOList.Count == 0 ||
                               starSysSOList.Any(s => s == null);

            if (needsLoading)
            {
                Debug.Log("Loading StarSysSO assets from AssetDatabase...");

#if UNITY_EDITOR
                // In Editor, use AssetDatabase
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StarSysSO", new[] { "Assets/SO/StarSysSO" });
                starSysSOList = new List<StarSysSO>();

                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    StarSysSO starSO = UnityEditor.AssetDatabase.LoadAssetAtPath<StarSysSO>(path);
                    if (starSO != null)
                    {
                        starSysSOList.Add(starSO);
                    }
                }

                // ✅ Sort by StarSysInt for consistent ordering
                starSysSOList = starSysSOList.OrderBy(s => s.StarSysInt).ToList();

                Debug.Log($"✅ Loaded {starSysSOList.Count} StarSysSO assets from AssetDatabase");

                // ✅ Log first few entries for verification
                if (starSysSOList.Count > 0)
                {
                    Debug.Log($"   First system: {starSysSOList[0].SysName} (StarSysInt={starSysSOList[0].StarSysInt})");
                    if (starSysSOList.Count > 1)
                    {
                        Debug.Log($"   Second system: {starSysSOList[1].SysName} (StarSysInt={starSysSOList[1].StarSysInt})");
                    }
                }
#else
                // In builds the list must be populated in the GalaxyScene Inspector.
                // Run  BOTF > Fix > Populate StarSysSO List in GalaxyScene  once in the
                // Unity editor to assign all StarSysSO assets and save the scene.
                // That bakes them into the build via scene serialization (no Resources folder needed).
                starSysSOList = new List<StarSysSO>();
                Debug.LogError(
                    "❌ StarSysManager: starSysSOList is empty in a build!\n" +
                    "   FIX: In the Unity Editor run  BOTF > Fix > Populate StarSysSO List in GalaxyScene\n" +
                    "   then rebuild the game. Galaxy map cannot be created without these assets.");
#endif
            }
            else
            {
                Debug.Log($"StarSysSO list already populated with {starSysSOList.Count} valid entries");
            }
        }

        public void SetGalaxyReferences(GameObject center, GameObject systemContainer)
        {
            galaxyCenter = center;
            // Store systemContainer if there's a corresponding field
            // Add any other initialization needed with these references

            Debug.Log("StarSysManager: Galaxy references set.");
        }

        public void FindGalaxyReferences()
        {
            // Find galaxyCenter if not assigned
            if (galaxyCenter == null)
            {
                galaxyCenter = GameObject.Find("GalaxyCenter");
                Debug.Log($"StarSysManager: Found galaxyCenter: {galaxyCenter != null}");
            }

            // Find galaxyCamera if not assigned
            if (galaxyCamera == null)
            {
                var mainCameraGO = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCameraGO != null)
                {
                    galaxyCamera = mainCameraGO.GetComponent<Camera>();
                    Debug.Log($"StarSysManager: Found galaxyCamera: {galaxyCamera != null}");
                }
            }

            // Find sysUIGOContentParent if not assigned
            if (sysUIGOContentParent == null)
            {
                var systemMenuView = GameObject.Find("SystemMenuView");
                if (systemMenuView != null)
                {
                    // Look for SysListContainer or similar
                    var listContainer = systemMenuView.transform.Find("SysListContainer");
                    if (listContainer != null)
                    {
                        sysUIGOContentParent = listContainer.gameObject;
                        Debug.Log($"StarSysManager: Found sysUIGOContentParent: {sysUIGOContentParent.name}");
                    }
                }

                if (sysUIGOContentParent == null)
                {
                    Debug.LogWarning("StarSysManager: sysUIGOContentParent not found - assign in Inspector!");
                }
            }

        }

        private GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name)
                return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject found = FindInHierarchy(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void InitializeFogOfWar()
        {
            var fogOfWar = FischlWorks_FogWar.csFogWar.Instance;

            if (fogOfWar != null && galaxyCenter != null)
            {
                // Use reflection to set levelMidPoint if not public
                var fogType = fogOfWar.GetType();
                var levelMidPointField = fogType.GetField("levelMidPoint",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                if (levelMidPointField != null)
                {
                    levelMidPointField.SetValue(fogOfWar, galaxyCenter.transform);
                    Debug.Log("StarSysManager: Set FogOfWar levelMidPoint to GalaxyCenter");
                }
                else
                {
                    Debug.LogError("StarSysManager: Could not find levelMidPoint field in csFogWar - assign manually in Inspector!");
                }
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<StarSysManager>();
            // Clean up singleton when scene unloads
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetShipBuildPrefabs(CivEnum localCiv)
        {
            TechLevel techLevel = GameController.Instance.GameData.StartingTechLevel;

            // ✅ NEW: Use civ-specific list
            List<ShipSO> shipSOList = ShipManager.Instance.GetShipSOsForCivAndTech(localCiv, techLevel);

            Debug.Log($"SetShipBuildPrefabs: Found {shipSOList.Count} ships for {localCiv} at {techLevel}");

            foreach (var shipSO in shipSOList)
            {
                GameObject prefab = GetShipPrefabByType(shipSO.ShipType);
                if (prefab == null) continue;

                var shipBuildScript = prefab.GetComponent<ShipBuildDrag>();
                if (shipBuildScript != null)
                {
                    int q = CivManager.Instance?.GetCivDataByCivEnum(localCiv)?.QualityScore ?? 5;
                    shipBuildScript.BuildDuration = BOTF3D.Combat.ShipStatCalculator.Calculate(
                        shipSO.ShipType, shipSO.TechLevel, localCiv, q).BuildDuration;
                    shipBuildScript.ShipSprite = shipSO.shipSprite;
                    prefab.GetComponent<Image>().sprite = shipSO.shipSprite;

                    Debug.Log($"  ✅ Set prefab for {shipSO.ShipType}");
                }
            }
        }

        private GameObject GetShipPrefabByType(ShipType type)
        {
            switch (type)
            {
                case ShipType.Scout: return scoutBluePrintPrefab;
                case ShipType.Destroyer: return destroyerBluePrintPrefab;
                case ShipType.Cruiser: return cruiserBluePrintPrefab;
                case ShipType.LtCruiser: return ltCruiserBluePrintPrefab;
                case ShipType.HvyCruiser: return hvyCruiserBluePrintPrefab;
                case ShipType.Transport: return transportBluePrintPrefab;
                default: return null;
            }
        }

        /// <summary>
        /// ✅ NEW: Initialize random position pool from all 211 star positions
        /// </summary>
        private void InitializeRandomPositionPool()
        {
            randomPositionPool = new List<Vector3>
            {
                new Vector3(-7.37f, 1.07f, -39.37f), new Vector3(33.65f, 0.34f, -16.79f), new Vector3(18.22f, -1.48f, -67.02f),
                new Vector3(-42.61f, 1.44f, -34.3f), new Vector3(-57.44f, 2.53f, 62.14f), new Vector3(61.97f, 1.07f, 84.35f),
                new Vector3(-45.4f, 1.29f, -70.41f), new Vector3(-2.04f, 0.34f, -71.75f), new Vector3(-15.91f, -0.39f, -33.27f),
                new Vector3(19.08f, -1.19f, 39.03f), new Vector3(0.09f, 2.07f, -40.49f), new Vector3(-45.92f, 0.34f, -48.75f),
                new Vector3(41.68f, -0.07f, 61.66f), new Vector3(-6.32f, 1.18f, -49.87f), new Vector3(4.35f, -1.23f, -48.75f),
                new Vector3(50.91f, 2.07f, 32.74f), new Vector3(-14.86f, 0.29f, -79.74f), new Vector3(-4.08f, -0.07f, -68.39f),
                new Vector3(-12.7f, 1.18f, -18.85f), new Vector3(6.47f, -1.23f, -62.21f), new Vector3(-28.72f, 2.07f, 68.9f),
                new Vector3(17.12f, 0.29f, -88f), new Vector3(-6.32f, -0.07f, -80.76f), new Vector3(8.61f, 1.18f, -53.93f),
                new Vector3(-42.61f, -1.2f, -32.24f), new Vector3(8.61f, -1.12f, -86.97f), new Vector3(6.47f, -1.23f, -83.9f),
                new Vector3(34.2f, 2.07f, 76.1f), new Vector3(12.89f, 0.29f, -84.93f), new Vector3(26.73f, -0.26f, -82.93f),
                new Vector3(-27.63f, 1.18f, -71.49f), new Vector3(-22.33f, 1.6f, -86.97f), new Vector3(-16.94f, 1.88f, -67.2f),
                new Vector3(52.49f, -0.83f, 47.16f), new Vector3(-14.86f, 0.7f, -28.46f), new Vector3(49.8f, -1.63f, 5.02f),
                new Vector3(-24.47f, -1.44f, -49.87f), new Vector3(-35.12f, -1.08f, -23.66f), new Vector3(13.66f, 1.14f, -84.42f),
                new Vector3(-64f, 0.34f, -73.56f), new Vector3(2.21f, 0.73f, -20.63f), new Vector3(18.22f, -0.55f, -81.9f),
                new Vector3(-6.32f, -0.56f, -62.39f), new Vector3(5.42f, -1.23f, -82.93f), new Vector3(0.09f, 2.07f, -51.87f),
                new Vector3(3.91f, 0.34f, -82.93f), new Vector3(-7.37f, -0.07f, 12.1f), new Vector3(-44.73f, 1.14f, -78.25f),
                new Vector3(0.09f, 1.01f, -32.24f), new Vector3(37.07f, -0.58f, 38.52f), new Vector3(-12.7f, 2.27f, 1.28f),
                new Vector3(-43.76f, -1.63f, 53.19f), new Vector3(-19.62f, -1.34f, 76.1f), new Vector3(-21.21f, -0.46f, -84.42f),
                new Vector3(-39.42f, 2.4f, -85.94f), new Vector3(45.59f, -1.66f, -1.28f), new Vector3(-56.91f, 0.27f, -38.48f),
                new Vector3(33.55f, -1.66f, 51.48f), new Vector3(32.08f, 1.69f, -85.94f), new Vector3(57.31f, -0.21f, -36.3f),
                new Vector3(-31.89f, 2.09f, -36.99f), new Vector3(45.59f, -1.18f, -35.27f), new Vector3(38.2f, 1.66f, -28.46f),
                new Vector3(-29.77f, -0.12f, -80.94f), new Vector3(20.23f, -0.46f, 76.1f), new Vector3(-44.73f, -1.01f, -13.48f),
                new Vector3(56.24f, 1.8f, 37.14f), new Vector3(-41.54f, -1.41f, -85.94f), new Vector3(18.22f, -0.99f, 48.98f),
                new Vector3(-5.25f, 0.56f, -58.74f), new Vector3(18.22f, -1.48f, -85.94f), new Vector3(-26.58f, 2.02f, -85.94f),
                new Vector3(-6.32f, 1.42f, -82.93f), new Vector3(-36.21f, -2.08f, 60.09f), new Vector3(50.66f, 0.58f, 55.28f),
                new Vector3(-53.28f, -1.61f, -78.02f), new Vector3(22.37f, 0.94f, -79.74f), new Vector3(-40.47f, 2.53f, -66.17f),
                new Vector3(22.37f, -1.63f, 59.06f), new Vector3(-7.37f, 0.34f, -8.51f), new Vector3(19.16f, 1.84f, -59.77f),
                new Vector3(-53.28f, 2.36f, -75.96f), new Vector3(21.28f, 2.47f, 5.41f), new Vector3(-29.77f, 2.51f, -9.36f),
                new Vector3(22.37f, -2.42f, 19.93f), new Vector3(-28.72f, -2.39f, -30.01f), new Vector3(-0.98f, -1.12f, -58.08f),
                new Vector3(49.8f, 0.83f, 44.64f), new Vector3(-32.94f, 0.8f, -44.09f), new Vector3(-7.37f, 0.2f, -75.96f),
                new Vector3(-60.71f, -2.36f, 78.28f), new Vector3(8.61f, -0.58f, -37.97f), new Vector3(-20.17f, -1.8f, -20.11f),
                new Vector3(35.29f, -0.99f, -82.93f), new Vector3(-15.99f, 1.68f, 63.92f), new Vector3(46.66f, 2.09f, 40.58f),
                new Vector3(-5.25f, -2.61f, -55.47f), new Vector3(61.57f, 1.31f, 68.9f), new Vector3(54.48f, -2.08f, 80.8f),
                new Vector3(28.7f, 0.42f, 16.37f), new Vector3(31.87f, 1.31f, 71.12f), new Vector3(11.82f, -1.83f, 16.37f),
                new Vector3(16.41f, 0.49f, 66.49f), new Vector3(1.14f, 2.02f, 73.81f), new Vector3(-31.16f, 2.16f, -66.86f),
                new Vector3(-2.04f, 0.17f, -74.93f), new Vector3(-0.98f, -1.17f, -26.92f), new Vector3(-50.05f, 1.27f, 69.08f),
                new Vector3(-2.04f, -0.81f, 19.93f), new Vector3(-46.86f, 1.44f, 78.28f), new Vector3(57.31f, -0.79f, 66.15f),
                new Vector3(5.63f, 2.09f, -75.73f), new Vector3(-44.73f, 2.09f, -59.77f), new Vector3(-13.77f, 0.16f, -60.69f),
                new Vector3(-21.76f, 1.68f, -44.89f), new Vector3(-16.96f, -2.61f, -85.94f), new Vector3(19.16f, 1.67f, -86.46f),
                new Vector3(45.98f, 0.05f, 73.81f), new Vector3(-28.72f, 0.05f, 21.58f), new Vector3(1.46f, -2.31f, 88f),
                new Vector3(7.54f, 2.43f, -80.25f), new Vector3(-55.48f, 0.47f, -25.79f), new Vector3(63.75f, 3f, 75.07f),
                new Vector3(38.98f, -2.89f, 20.79f), new Vector3(-27.63f, -1.52f, -80.76f), new Vector3(3.18f, 2.57f, 67.87f),
                new Vector3(-39.42f, 1.01f, -1.28f), new Vector3(-37.28f, -2.63f, -69.56f), new Vector3(-11.73f, -2.22f, -82.93f),
                new Vector3(-15.91f, -2.35f, -43.06f), new Vector3(-59.18f, 0.76f, 58.03f), new Vector3(43.07f, -0.84f, -52.55f),
                new Vector3(15.05f, -2.33f, -84.93f), new Vector3(-48.98f, -2.89f, -49.87f), new Vector3(38.98f, -1.62f, 87.31f),
                new Vector3(-24.47f, 2.16f, -63.93f), new Vector3(-60.71f, 2.22f, 50.47f), new Vector3(49.8f, 0.56f, -23.66f),
                new Vector3(-35.12f, -2.82f, -13.48f), new Vector3(39.15f, -2.87f, 29.6f), new Vector3(-60.71f, -0.19f, -11.42f),
                new Vector3(40.2f, 2.22f, 69.75f), new Vector3(64f, 0.42f, 44.46f), new Vector3(17.12f, -1.34f, -14.51f),
                new Vector3(49.8f, 2.18f, -52.73f), new Vector3(62.57f, 2.05f, 61.66f), new Vector3(-2.04f, 1.07f, -44.09f),
                new Vector3(-19.1f, -1.7f, 45.15f), new Vector3(29.16f, -2.08f, -73.56f), new Vector3(-50.05f, -2.4f, -0.76f),
                new Vector3(-11.25f, 2.53f, -20.29f), new Vector3(8.61f, -1.8f, -42.55f), new Vector3(-50.05f, -2.58f, 21.58f),
                new Vector3(-19.1f, 0.49f, -57.74f), new Vector3(1.46f, -0.14f, 59.06f), new Vector3(26.73f, -1.84f, -25.09f),
                new Vector3(56.24f, 1.94f, -66.86f), new Vector3(40.57f, 1.69f, -83.9f), new Vector3(6.47f, 0.7f, -25.09f),
                new Vector3(44.91f, 1.98f, -48.75f), new Vector3(-50.05f, 0.53f, -16.79f), new Vector3(-2.04f, -0.46f, -36.3f),
                new Vector3(17.12f, -0.14f, -53.93f), new Vector3(11.82f, -0.42f, -47.74f), new Vector3(13.94f, -0.53f, -52.9f),
                new Vector3(39.15f, -1.59f, 12.1f), new Vector3(33.15f, 0.79f, -47.74f), new Vector3(-13.77f, 0.93f, 12.1f),
                new Vector3(-39.42f, 1.17f, 32.74f), new Vector3(19.26f, 1.57f, -36.3f), new Vector3(33.15f, 2.02f, 2.82f),
                new Vector3(22.37f, -1.63f, -51.87f), new Vector3(50.91f, -0.46f, -15.71f), new Vector3(-50.05f, 1.8f, -65.32f),
                new Vector3(-27.63f, -1.98f, 12.1f), new Vector3(59.43f, -2.08f, -9.7f), new Vector3(-41.54f, 0.8f, 12.1f),
                new Vector3(-30.82f, -2.08f, 41.73f), new Vector3(21.38f, 1.07f, -8.51f), new Vector3(23.52f, -2.23f, -31.21f),
                new Vector3(-48.96f, 2.15f, -58.08f), new Vector3(-29.77f, -3f, 6.95f), new Vector3(-59.57f, 1.18f, -55.47f),
                new Vector3(38.37f, 1.44f, -41.06f), new Vector3(9.49f, 0.2f, 33.25f), new Vector3(28.89f, 0.16f, -66.17f),
                new Vector3(31.03f, 1.44f, -42.08f), new Vector3(12.89f, 2.09f, -37.46f), new Vector3(13.94f, 2.31f, 50.3f),
                new Vector3(-4.23f, 1.68f, 47.16f), new Vector3(7.54f, 1.62f, 51.32f), new Vector3(22.37f, 0.43f, -43.57f),
                new Vector3(22.37f, -0.29f, 30.5f), new Vector3(19.26f, 1.17f, -62.21f), new Vector3(9.68f, 2.4f, 76.97f),
                new Vector3(23.52f, -1.44f, -58.08f), new Vector3(-6.32f, 2.22f, 82.34f), new Vector3(34.07f, 1.53f, -34.3f),
                new Vector3(61.57f, 0.18f, 1.79f), new Vector3(28.89f, 0.16f, -4.38f), new Vector3(13.94f, 0.42f, -25.09f),
                new Vector3(-18.05f, 2.09f, 22.95f), new Vector3(15.01f, 2.31f, -11.42f), new Vector3(-4.96f, 1.66f, 58.37f),
                new Vector3(-42.78f, 1.59f, 25.24f), new Vector3(-7.37f, 0.43f, 28.68f), new Vector3(-23.4f, -0.29f, 30.5f),
                new Vector3(-15.3f, 1.68f, 35.13f), new Vector3(-32.94f, 2.49f, 35.13f), new Vector3(-13.39f, -1.7f, 55.62f),
                new Vector3(-10.9f, 2.16f, 71.12f)
            };

            // ✅ Shuffle the pool using Fisher-Yates algorithm
            for (int i = randomPositionPool.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                Vector3 temp = randomPositionPool[i];
                randomPositionPool[i] = randomPositionPool[randomIndex];
                randomPositionPool[randomIndex] = temp;
            }

            randomPositionIndex = 0;
            Debug.Log($"✅ Initialized random position pool with {randomPositionPool.Count} positions (shuffled)");
        }

        /// <summary>
        /// ✅ NEW: Get next random position from shuffled pool
        /// </summary>
        private Vector3 GetNextRandomPosition()
        {
            if (randomPositionPool == null || randomPositionPool.Count == 0)
            {
                Debug.LogError("Random position pool is empty! Falling back to (0,0,0)");
                return Vector3.zero;
            }

            if (randomPositionIndex >= randomPositionPool.Count)
            {
                Debug.LogWarning("Ran out of random positions! Wrapping around to start of pool.");
                randomPositionIndex = 0;
            }

            Vector3 position = randomPositionPool[randomPositionIndex];
            randomPositionIndex++;
            return position;
        }

        public void SysDataFromSO(List<CivSO> civSOList)
        {
            Debug.Log($"=== StarSysManager.SysDataFromSO: Creating systems for {civSOList.Count} civs ===");

            // Ensure we have required references
            if (galaxyCenter == null)
            {
                Debug.LogError("StarSysManager: galaxyCenter is NULL! Cannot create systems.");
                return;
            }

            // ✅ NEW: Initialize random positions if using RANDOM galaxy type
            if (MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType == GalaxyMapType.RANDOM)
            {
                InitializeRandomPositionPool();
            }

            StarSysData SysData = new StarSysData("null");
            List<StarSysData> starSysDatas = new List<StarSysData>();
            starSysDatas.Add(SysData);

            for (int i = 0; i < civSOList.Count; i++)
            {
                StarSysSO starSysSO = GetStarSObyInt(civSOList[i].CivInt);

                if (starSysSO == null)
                {
                    Debug.LogWarning($"  ⚠️ No StarSysSO found for civ {civSOList[i].CivShortName} (int={civSOList[i].CivInt})");
                    continue;
                }

                // ✅ DIAGNOSTIC: Validate star sprite before creating system
                if (starSysSO.StarSprit == null)
                {
                    Debug.LogError($"  ❌ MISSING STAR SPRITE for system '{starSysSO.SysName}' (CivInt={civSOList[i].CivInt}, StarType={starSysSO.StarType})");
                    Debug.LogError($"     This system will appear without a visible star! Check CSV import and sprite assets.");
                }
                else
                {
                    Debug.Log($"  ✅ Star sprite found for '{starSysSO.SysName}': {starSysSO.StarSprit.name} (StarType={starSysSO.StarType})");
                }

                SysData = new StarSysData(starSysSO);
                SysData.CurrentOwnerCivEnum = starSysSO.FirstOwner;
                SysData.SystemType = starSysSO.StarType;
                SysData.StarSprit = starSysSO.StarSprit;
                SysData.Description = starSysSO.Description;

                Debug.Log($"  Creating system: {SysData.SysName} for {civSOList[i].CivShortName}");

                InstantiateSystem(SysData, civSOList[i], starSysSO);
            }

            starSysDatas.Remove(starSysDatas[0]); // pull out the null

            Debug.Log($"=== StarSysManager: Created {StarSysControllerList.Count} total systems ===");
        }

        public StarSysController InstantiateEmptyStarSysController()
        {
            StarSysController starSysCon = Instantiate(sysPrefab, new Vector3(0, 0, 0),
              Quaternion.identity);
            return starSysCon;
        }

        public void InstantiateSystem(StarSysData sysData, CivSO civSO, StarSysSO starSysSO)
        {
            // ✅ Determine position based on galaxy type
            Vector3 systemLocalPosition;

            if (MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType == GalaxyMapType.RANDOM)
            {
                // ✅ RANDOM: Get next shuffled position from pool
                systemLocalPosition = GetNextRandomPosition();
                Debug.Log($"InstantiateSystem: RANDOM mode - Assigned '{sysData.SysName}' to position {systemLocalPosition}");
            }
            else if (MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType == GalaxyMapType.RING)
            {
                // TODO: Implement ring galaxy layout
                systemLocalPosition = sysData.GetPosition();
                Debug.Log($"InstantiateSystem: RING mode - Using position {systemLocalPosition} for '{sysData.SysName}'");
            }
            else // CLASSIC/DEFAULT
            {
                // ✅ Use position from StarSysSO
                systemLocalPosition = sysData.GetPosition();
                Debug.Log($"InstantiateSystem: CLASSIC mode - Using SO position {systemLocalPosition} for '{sysData.SysName}'");
            }

            // ✅ Instantiate as child of galaxyCenter
            StarSysController starSysCon = Instantiate(sysPrefab, galaxyCenter.transform);

            // ✅ Set LOCAL position directly (no scale interference)
            starSysCon.transform.localPosition = systemLocalPosition;
            starSysCon.transform.localRotation = Quaternion.identity;
            sysData.SetPosition(systemLocalPosition);
            starSysCon.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            StarSysBuildManager buildManager = new StarSysBuildManager(starSysCon);
            buildManager.RegisterStarSysController(starSysCon);
            starSysCon.StarSysData = sysData;
            starSysCon.gameObject.layer = 4; // water layer (also used by fog of war for obstacles with shows to line of sight

            starSysCon.GalaxyEventCamera = galaxyCamera;

            Transform fogObsticleTransform = starSysCon.transform.Find("FogObstacle");
            if (fogObsticleTransform != null)
            {
                // ✅ Parent to galaxyCenter with local positioning
                fogObsticleTransform.SetParent(galaxyCenter.transform, false);
                fogObsticleTransform.localPosition = new Vector3(systemLocalPosition.x, -55f, systemLocalPosition.z);
            }

            starSysCon.name = sysData.GetSysName();

            // ✅ Log world position for verification
            Debug.Log($"  System world position: {starSysCon.transform.position}");

            // ✅ Set Dilithium Capacity based on system type
            sysData.DilithiumCapacity = DetermineDilithiumCapacity(civSO, starSysSO);
            sysData.TotalSysPowerLoad = 0;
            sysData.TotalSysPowerOutput = starSysSO.PowerStations * sysData.BasePowerPerPlant;
            sysData.CurrentPowerPlantCount = starSysSO.PowerStations;
            starSysCon.StarSysData.ShipsList.Clear();
            sysData.SysGameObject = starSysCon.gameObject;

            StarSysChildFields starSysFields = starSysCon.GetComponent<StarSysChildFields>();
            if (!GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum))
            {
                starSysFields.SysName.text = "UNKNOWN";
            }
            else
            {
                starSysFields.SysName.text = sysData.GetSysName();
            }

            MapLineFixed ourDropLine = starSysCon.GetComponentInChildren<MapLineFixed>();
            ourDropLine.GetLineRenderer();

            // ✅ Use WORLD position for galaxy plane calculations
            Vector3 systemWorldPos = starSysCon.transform.position;
            Vector3 galaxyPlanePoint = new Vector3(systemWorldPos.x, galaxyImage.transform.position.y, systemWorldPos.z);
            Vector3[] points = { systemWorldPos, galaxyPlanePoint };
            ourDropLine.SetUpLine(points);

            StarSysChildFields starSysField = starSysCon.GetComponent<StarSysChildFields>();
            SpriteRenderer srInsignia = starSysField.OwnerInsigniaGO.GetComponent<SpriteRenderer>();
            srInsignia.sprite = civSO.Insignia;
            if (!GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum))
            {
                srInsignia.sortingOrder = 0;
                srInsignia.enabled = false;
            }
            srInsignia.gameObject.transform.position =
                new Vector3(systemWorldPos.x, galaxyPlanePoint.y + 1f, systemWorldPos.z);
            srInsignia.gameObject.layer = 4;

            SpriteRenderer srStar = starSysField.StarSpriteGO.GetComponent<SpriteRenderer>();
            srStar.sprite = sysData.StarSprit;
            srStar.sortingOrder = 1;
            starSysCon.name = sysData.GetSysName();
            starSysCon.StarSysData = sysData;

            CivController[] controllers = CivManager.Instance.CivControllersInGame.ToArray();
            for (int i = 0; controllers.Length > 0; i++)
            {
                if (controllers[i].CivData.CivEnum == starSysCon.StarSysData.GetFirstOwner())
                {
                    starSysCon.StarSysData.CurrentCivController = controllers[i];
                    break;
                }
            }

            starSysCon.gameObject.SetActive(true);
            StarSysControllerList.Add(starSysCon);

            Debug.Log($"  ✅ System created: {starSysCon.name}, systems total: {StarSysControllerList.Count}");

            if (GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum))
            {
                InstantiateStarSysUI(starSysCon);
            }

            List<StarSysController> listStarSysCon = new List<StarSysController> { starSysCon };
            CivManager.Instance.AddSystemToOwnSystemListAndHomeSys(listStarSysCon);

            starSystemCounter++;
            if (starSystemCounter == CivManager.Instance.CivControllersInGame.Count)
            {
                csFogWar.Instance.RunFogOfWar();
            }

            if (civSO.HasWarp)
            {
                FleetManager.Instance.BuildFirstFleetsNearSyst(starSysCon);
            }

            // Home system defense: majors always get their own lone Destroyer regardless of warp
            // status; minors only get one once they've researched warp (HasWarp=true) - mirrors the
            // major/minor split already used for starting-fleet composition
            // (ShipSOProvider.GetStartingFleetShips) and for majority/minority checks elsewhere
            // (DiplomacyController.cs uses the same CivEnum <= TERRAN cutoff).
            bool isMajorCiv = starSysCon.StarSysData.CurrentOwnerCivEnum <= CivEnum.TERRAN;
            if (isMajorCiv || civSO.HasWarp)
            {
                ShipManager.Instance.BuildShipInSystem(ShipType.Destroyer, starSysCon);
            }

            if (true)
            {
                // Shared by orbital battery and population starting scale-down below so a fresh
                // EARLY-era game doesn't spawn every system already fully built/settled.
                TechLevel startingTechLevel = sysData.CurrentCivController?.CivData?.CurrentTechLevel ?? TechLevel.EARLY;

                int startingPowerPlants = DetermineStartingPowerPlants(civSO, starSysSO, sysData.DilithiumCapacity);

                sysData.PowerPlants = AddSystemFacilities(startingPowerPlants, PowerPlantPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                sysData.CurrentPowerPlantCount = sysData.PowerPlants.Count;
                sysData.Factories = AddSystemFacilities(starSysSO.Factories, FactoryPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                sysData.Shipyards = AddSystemFacilities(starSysSO.Shipyards, ShipyardPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                sysData.ShieldGenerators = AddSystemFacilities(starSysSO.ShieldGenerators, ShieldGeneratorPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                int startingOrbitalBatteries = DetermineStartingOrbitalBatteries(civSO, startingTechLevel, starSysSO.OrbitalBatteries);
                sysData.OrbitalBatteries = AddSystemFacilities(startingOrbitalBatteries, OrbitalBatteryPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                sysData.ResearchCenters = AddSystemFacilities(starSysSO.ResearchCenters, ResearchCenterPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);

                sysData.MaxPopulation = DetermineMaxPopulation(civSO, starSysSO);
                sysData.MaxGroundForceUnits = DetermineMaxGroundForceUnits(civSO, starSysSO, sysData.MaxPopulation);
                // Homeworlds start partially settled, scaled by the civ's starting TechLevel (see
                // DetermineStartingPopulationFraction) - a fresh EARLY-era game no longer spawns every
                // homeworld already at its population cap. Every other owned system still starts at 0
                // and grows toward its cap over time via PopulationManager (see that class for the
                // per-stardate growth/conversion tied to active Factories/ResearchCenters).
                if (starSysSO.IsHomeworld)
                {
                    float startingFraction = DetermineStartingPopulationFraction(civSO, startingTechLevel);
                    int startingTotalPopulation = Mathf.RoundToInt(sysData.MaxPopulation * startingFraction);

                    // 10% of the starting population is already fielded as ground forces rather than
                    // civilians - carved out of, not added on top of, the total so Population +
                    // GroundForces.Count always equals the system's true total population.
                    int initialGroundForces = Mathf.Clamp(
                        Mathf.RoundToInt(startingTotalPopulation * 0.10f), 0, sysData.MaxGroundForceUnits);
                    sysData.Population = startingTotalPopulation - initialGroundForces;

                    for (int i = 0; i < initialGroundForces; i++)
                        AddGroundForceUnit(starSysCon);
                }

                SetParentForFacilities(starSysCon.gameObject, sysData);

                if (StarSysMenuUIController.Instance != null)
                {
                    StarSysMenuUIController.Instance.UpdateSystemPowerBalance(starSysCon);
                    Debug.Log($"  ✅ Initial power balance: Load={sysData.TotalSysPowerLoad}, Output={sysData.TotalSysPowerOutput}");
                }

                if (starSysCon.StarSysUIGameObject != null)
                {
                    var uiElement = starSysCon.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                    if (uiElement != null)
                    {
                        uiElement.InitializeFromStarSysData(sysData);
                    }
                }
            }

            InitializeDilithiumStockpile(starSysCon);

            if (GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum))
            {
                localPlayerTheme = ThemeManager.Instance.GetLocalPlayerTheme();
            }
        }

        private void InitializeDilithiumStockpile(StarSysController sysCon)
        {
            var sysData = sysCon.StarSysData;
            bool isPlayable = sysData.CurrentCivController?.CivData?.Playable == true;

            if (!isPlayable)
            {
                sysData.DilithiumStockpile = 5;
                return;
            }

            CivEnum civ    = sysData.CurrentOwnerCivEnum;
            TechLevel tech = sysData.CurrentCivController.CivData.CurrentTechLevel;
            int quality    = sysData.CurrentCivController.CivData.QualityScore;

            // Cost of the power plants that were pre-built at game start
            int ppLi2 = ShipStatCalculator.GetPowerPlantDilithiumCost(civ) * sysData.CurrentPowerPlantCount;

            // Cost of the Destroyer placed in-system at game start
            int shipLi2 = ShipStatCalculator.Calculate(ShipType.Destroyer, tech, civ, quality).DilithiumCost;

            // Base buffer of 20 + 3 per power plant (more plants = more infrastructure to maintain)
            int buffer = 20 + sysData.CurrentPowerPlantCount * 3;

            sysData.DilithiumStockpile = ppLi2 + shipLi2 + buffer;
            Debug.Log($"[Dilithium] {sysCon.name} ({civ}): pp={ppLi2}, ship={shipLi2}, buffer={buffer}, stockpile={sysData.DilithiumStockpile}");
        }

        // ... (rest of the methods remain the same - DetermineDilithiumCapacity, DetermineStartingPowerPlants, etc.)
        /// <summary>
        ///
        /// </summary>
        private int DetermineDilithiumCapacity(CivSO civSO, StarSysSO starSysSO)
        {
            // ✅ Major race homeworlds
            if (civSO.Playable && starSysSO.IsHomeworld)
            {
                return starSysSO.Dilitium; // set per-civ by CivBalanceCalculator
            }

            // ✅ Minor race systems
            if (!civSO.Playable)
            {
                //// 70% get capacity 1-2, 30% get capacity 3
                //float roll = UnityEngine.Random.value;
                //if (roll < 0.40f) return 1;
                //if (roll < 0.70f) return 2;
                return 1;
            }

            // ✅ Colonizable/Terraformable systems
            if (starSysSO.IsHabitable || starSysSO.IsTerraformable)
            {
                //// Based on planet quality (you can expand this)
                //float roll = UnityEngine.Random.value;
                //if (roll < 0.50f) return 1;
                //if (roll < 0.85f) return 2;
                return 1;
            }

            // ✅ Non-habitable systems, black hole....
            //#nullable enable
            return 0;
        }

        /// <summary>
        /// Determine the Population cap for a system. Homeworlds use the civ's authored lore value
        /// (CivSO.Population) if set. Major homeworlds otherwise use MaxHomePopulation directly (every
        /// major is authored with the same fixed 80, so majors reliably reach the top GroundForce tier).
        /// Minor homeworlds without a lore value instead roll randomly within Min/MaxHomePopulation,
        /// where MaxHomePopulation itself was authored per-civ (randomized up to 40) so minor caps vary
        /// across races. Non-home systems get smaller fixed starting caps until colonization
        /// (transport-delivered population) is built out.
        /// </summary>
        private int DetermineMaxPopulation(CivSO civSO, StarSysSO starSysSO)
        {
            if (starSysSO.IsHomeworld)
            {
                if (civSO.Population > 0)
                    return civSO.Population;

                if (civSO.Playable)
                    return civSO.MaxHomePopulation > 0 ? civSO.MaxHomePopulation : 80;

                int min = civSO.MinHomePopulation > 0 ? civSO.MinHomePopulation : 10;
                int max = civSO.MaxHomePopulation > min ? civSO.MaxHomePopulation : min + 20;
                return UnityEngine.Random.Range(min, max + 1);
            }

            if (civSO.Playable)
                return (starSysSO.IsHabitable || starSysSO.IsTerraformable) ? 20 : 0;

            return 8; // minor-owned non-home system
        }

        /// <summary>
        /// Fraction of MaxPopulation a homeworld starts with, based on the civ's TechLevel at creation
        /// time. Majors get a fixed fraction per tier so they're predictable; minors roll within a
        /// per-tier range so same-tech minors still vary. Population climbs from here toward
        /// MaxPopulation over time via PopulationManager - it's no longer set to the cap outright.
        /// </summary>
        private float DetermineStartingPopulationFraction(CivSO civSO, TechLevel techLevel)
        {
            if (civSO.Playable)
            {
                switch (techLevel)
                {
                    case TechLevel.EARLY: return 0.25f;
                    case TechLevel.DEVELOPED: return 0.50f;
                    case TechLevel.ADVANCED: return 0.75f;
                    default: return 1.0f; // SUPREME
                }
            }

            switch (techLevel)
            {
                case TechLevel.EARLY: return UnityEngine.Random.Range(0.10f, 0.30f);
                case TechLevel.DEVELOPED: return UnityEngine.Random.Range(0.30f, 0.55f);
                case TechLevel.ADVANCED: return UnityEngine.Random.Range(0.55f, 0.80f);
                default: return UnityEngine.Random.Range(0.80f, 1.0f); // SUPREME
            }
        }

        /// <summary>
        /// Number of orbital batteries a system starts with, scaled down from the designer-authored
        /// starSysSO.OrbitalBatteries count by TechLevel so a fresh EARLY-era game doesn't spawn every
        /// system already fully defended - the rest can be built up over time via StarSysBuildManager.
        /// Majors use the same fixed 25/50/75/100% curve as starting population. Minors use the same
        /// randomized per-tier range, then shift up or down per point of WarLikeEnum/XenophobiaEnum:
        /// warlike and xenophobic civs field more batteries at any given tech tier, peaceful/open ones
        /// field fewer.
        /// </summary>
        private int DetermineStartingOrbitalBatteries(CivSO civSO, TechLevel techLevel, int authoredCount)
        {
            if (authoredCount <= 0) return 0;

            float fraction;
            if (civSO.Playable)
            {
                switch (techLevel)
                {
                    case TechLevel.EARLY: fraction = 0.25f; break;
                    case TechLevel.DEVELOPED: fraction = 0.50f; break;
                    case TechLevel.ADVANCED: fraction = 0.75f; break;
                    default: fraction = 1.0f; break; // SUPREME
                }
            }
            else
            {
                switch (techLevel)
                {
                    case TechLevel.EARLY: fraction = UnityEngine.Random.Range(0.10f, 0.30f); break;
                    case TechLevel.DEVELOPED: fraction = UnityEngine.Random.Range(0.30f, 0.55f); break;
                    case TechLevel.ADVANCED: fraction = UnityEngine.Random.Range(0.55f, 0.80f); break;
                    default: fraction = UnityEngine.Random.Range(0.80f, 1.0f); break; // SUPREME
                }

                // WarLikeEnum/XenophobiaEnum run -2 (Warlike/Xenophobia) to +2 (Pacifist/Compassion), so
                // negating and summing gives a -4..4 "defensiveness" score. Each point shifts the
                // fraction by 10% - a Warlike+Xenophobia minor fields noticeably more batteries than an
                // equivalent-tech Pacifist+Compassion one.
                int defensiveness = -(int)civSO.WarLikeEnum - (int)civSO.XenophbiaEnum;
                fraction = Mathf.Clamp01(fraction + defensiveness * 0.10f);
            }

            return Mathf.Clamp(Mathf.RoundToInt(authoredCount * fraction), 0, authoredCount);
        }

        /// <summary>
        /// Determine the GroundForces cap for a system. Home systems of major civs are the only
        /// systems that can reach the absolute upper limit of 11 (matches groundForceGridLimit in
        /// StarSysUI_Fields). Every other tier is scaled down from there so majors always field more
        /// troops than minors, and homeworlds always field more than colonies.
        /// </summary>
        private int DetermineMaxGroundForceUnits(CivSO civSO, StarSysSO starSysSO, int maxPopulation)
        {
            if (maxPopulation <= 0) return 0;

            int tierCeiling;
            if (civSO.Playable && starSysSO.IsHomeworld) tierCeiling = 11;
            else if (civSO.Playable) tierCeiling = 6;
            else if (starSysSO.IsHomeworld) tierCeiling = 5;
            else tierCeiling = 2;

            return Mathf.Clamp(maxPopulation / GroundForceData.PopulationPerUnit, 1, tierCeiling);
        }

        /// <summary>
        /// Determine starting power plants (always 1 for warp-capable, 0 otherwise)
        /// </summary>
        private int DetermineStartingPowerPlants(CivSO civSO, StarSysSO starSysSO, int dilithiumCapacity)
        {
            if (dilithiumCapacity == 0)
                return 0;

            // ✅ Read from ScriptableObject if available
            if (starSysSO != null && starSysSO.PowerStations > 0)
            {
                // Respect the SO's configured value, but cap at dilithium capacity
                return Mathf.Min(starSysSO.PowerStations, dilithiumCapacity);
            }

            // ✅ Fallback: Warp-capable civs start with 1 power plant
            if (civSO.CivInt <= 6)
                return 2;
            if (civSO.HasWarp)
                return 1;

            return 0; // Non-warp systems start with 0
        }
        private void SetParentForFacilities(GameObject parent, StarSysData starSysData)
        {
            foreach (var go in starSysData.PowerPlants)
            {
                go.transform.SetParent(parent.transform, false);
            }
            foreach (var go in starSysData.Factories)
            {
                go.transform.SetParent(parent.transform, false);
            }
            foreach (var go in starSysData.Shipyards)
            {
                go.transform.SetParent(parent.transform, false);
            }
            foreach (var go in starSysData.ShieldGenerators)
            {
                go.transform.SetParent(parent.transform, false);
            }
            foreach (var go in starSysData.OrbitalBatteries)
            {
                go.transform.SetParent(parent.transform, false);
            }
            foreach (var go in starSysData.ResearchCenters)
            {
                go.transform.SetParent(parent.transform, false);
            }
            foreach (var go in starSysData.GroundForces)
            {
                go.transform.SetParent(parent.transform, false);
            }
        }

        /// <summary>
        /// Instantiates one ground force unit placeholder for the system and adds it to
        /// StarSysData.GroundForces. Unlike other facilities, ground forces have no power load and
        /// no on/off state (see GroundForceIconUI), so the placeholder needs no TextMeshProUGUI toggle -
        /// it exists purely as a unique GameObject identity for the UI grid (StarSysUI_Fields.SyncGroundForceGrid)
        /// to key off of. Called at homeworld creation and by PopulationManager as population grows.
        /// </summary>
        public GameObject AddGroundForceUnit(StarSysController sysController)
        {
            var sysData = sysController.StarSysData;

            if (sysData.GroundForceData == null)
            {
                sysData.GroundForceData = new GroundForceData("null")
                {
                    CivEnum = sysData.CurrentOwnerCivEnum,
                    FacilitiesEnumType = StarSysFacilityType.GroundForce
                };
            }

            var newFacilityGO = new GameObject("GroundForceUnit");
            newFacilityGO.layer = 5;
            newFacilityGO.transform.SetParent(sysController.transform, false);
            newFacilityGO.SetActive(false);
            sysData.GroundForces.Add(newFacilityGO);
            return newFacilityGO;
        }
        public List<GameObject> AddSystemFacilities(int numOf, GameObject prefab, int civInt, int onOff, StarSysController sysController)
        {
            List<GameObject> returnList = new List<GameObject>();
            TechLevel techLevel = GameController.Instance.GameData.StartingTechLevel;
            var civ = (CivEnum)civInt;
            ;
            int startingStarDate = TimeManager.Instance.StaringStardate;

            // Use prefab reference comparisons for switch
            if (prefab == PowerPlantPrefab)
            {
                PowerPlantData powerPlantData = new PowerPlantData("null");
                var powerPlantSO = GetPowrPlantSObyCivEnum(civ);
                powerPlantData.CivEnum = civ;
                powerPlantData.TechLevel = techLevel;
                powerPlantData.FacilitiesEnumType = StarSysFacilityType.PowerPlanet;
                powerPlantData.Name = powerPlantSO.Name;
                powerPlantData.StartStarDate = startingStarDate;
                powerPlantData.BuildDuration = powerPlantSO.BuildDuration;
                powerPlantData.BasePowerOutput = powerPlantSO.PowerOutput;
                // ✅ Sprite comes from ThemeSO (per-civ), not the facility SO — ThemeSO is the
                // single source of truth for facility art so editing a theme updates the icon live.
                powerPlantData.PowerPlantSprite = ThemeManager.Instance.GetThemeByCivEnum(civ).PowerPlantImage;
                powerPlantData.Description = powerPlantSO.Description;
                sysController.StarSysData.PowerPlantData = powerPlantData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;

                    // ✅ Parent to the star system controller to keep hierarchy clean
                    newFacilityGO.transform.SetParent(sysController.transform);

                    GetPowerPlantText(sysController, newFacilityGO, numOf);
                    newFacilityGO.SetActive(false);
                    powerPlantData.SysGameObject = newFacilityGO;
                    returnList.Add(newFacilityGO);
                }
            }
            else if (prefab == FactoryPrefab)
            {
                FactoryData factoryData = new FactoryData("null");
                var factorySO = GetFactorySObyCivInt((int)civ);
                factoryData.CivEnum = civ;
                factoryData.TechLevel = techLevel;
                factoryData.FacilitiesEnumType = StarSysFacilityType.Factory;
                factoryData.Name = factorySO.Name;
                factoryData.StartStarDate = startingStarDate;
                factoryData.PowerLoad = factorySO.PowerLoad;
                factoryData.BuildDuration = factorySO.BuildDuration;
                factoryData.FactorySprite = ThemeManager.Instance.GetThemeByCivEnum(civ).FactoryImage;
                factoryData.Description = factorySO.Description;
                sysController.StarSysData.FactoryData = factoryData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
                    newFacilityGO.transform.SetParent(sysController.transform);
                    TextMeshProUGUI onTmp = newFacilityGO.AddComponent<TextMeshProUGUI>();
                    onTmp.text = onOff.ToString(); // or "0" to be explicit
                    onTmp.name = "OnOffTMP";
                    GetFactoryText(newFacilityGO, factoryData, numOf);
                    newFacilityGO.SetActive(false);
                    factoryData.SysGameObject = newFacilityGO;
                    returnList.Add(newFacilityGO);
                    //sysController.StarSysData.TotalSysPowerLoad += factoryData.PowerLoad;
                }
            }
            else if (prefab == ShipyardPrefab)
            {
                ShipyardData syData = new ShipyardData("null");
                var sSO = GetShipyardSObyCivInt((int)civ);
                syData.CivEnum = civ;
                syData.TechLevel = techLevel;
                syData.FacilitiesEnumType = StarSysFacilityType.Shipyard;
                syData.Name = sSO.Name;
                syData.StartStarDate = startingStarDate;
                syData.BuildDuration = sSO.BuildDuration;
                syData.PowerLoad = sSO.PowerLoad;
                syData.ShipyardSprite = ThemeManager.Instance.GetThemeByCivEnum(civ).ShipyardImage;
                syData.Description = sSO.Description;
                sysController.StarSysData.ShipyardData = syData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
                    newFacilityGO.transform.SetParent(sysController.transform);
                    TextMeshProUGUI On = newFacilityGO.AddComponent<TextMeshProUGUI>();
                    On.text = onOff.ToString();
                    GetShipyardText(newFacilityGO, syData, numOf);
                    newFacilityGO.SetActive(false);
                    syData.SysGameObject = newFacilityGO;
                    returnList.Add(newFacilityGO);
                    //sysController.StarSysData.TotalSysPowerLoad += syData.PowerLoad;
                }
            }
            else if (prefab == ShieldGeneratorPrefab)
            {
                ShieldGeneratorData sgData = new ShieldGeneratorData("null");
                var sgSO = GetShieldGeneratorSObyCivInt((int)civ);
                sgData.CivEnum = civ;
                sgData.TechLevel = techLevel;
                sgData.FacilitiesEnumType = StarSysFacilityType.ShieldGenerator;
                sgData.Name = sgSO.Name;
                sgData.StartStarDate = startingStarDate;
                sgData.BuildDuration = sgSO.BuildDuration;
                sgData.PowerLoad = sgSO.PowerLoad;
                sgData.ShieldGeneratorSprite = ThemeManager.Instance.GetThemeByCivEnum(civ).ShieldImage;
                sgData.Description = sgSO.Description;
                sysController.StarSysData.ShieldGeneratorData = sgData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
                    newFacilityGO.transform.SetParent(sysController.transform);
                    TextMeshProUGUI On = newFacilityGO.AddComponent<TextMeshProUGUI>();
                    On.text = onOff.ToString();
                    GetShieldGText(newFacilityGO, sgData, numOf);
                    newFacilityGO.SetActive(false);
                    sgData.SysGameObject = newFacilityGO;
                    returnList.Add(newFacilityGO);
                    //sysController.StarSysData.TotalSysPowerLoad += sgData.PowerLoad;
                }
            }
            else if (prefab == OrbitalBatteryPrefab)
            {
                OrbitalBatteryData obData = new OrbitalBatteryData("null");
                var obSO = GetOrbitalBatterySObyCivInt((int)civ);
                obData.CivEnum = civ;
                obData.TechLevel = techLevel;
                obData.FacilitiesEnumType = StarSysFacilityType.OrbitalBattery;
                obData.Name = obSO.Name;
                obData.StartStarDate = startingStarDate;
                obData.BuildDuration = obSO.BuildDuration;
                obData.PowerLoad = obSO.PowerLoad;
                obData.OrbitalBatterySprite = ThemeManager.Instance.GetThemeByCivEnum(civ).OrbitalBatteriesImage;
                obData.Description = obSO.Description;
                sysController.StarSysData.OrbitalBatteryData = obData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
                    newFacilityGO.transform.SetParent(sysController.transform);
                    TextMeshProUGUI On = newFacilityGO.AddComponent<TextMeshProUGUI>();
                    On.text = onOff.ToString();
                    GetOBText(newFacilityGO, obData, numOf);
                    newFacilityGO.SetActive(false);
                    obData.SysGameObject = newFacilityGO;
                    returnList.Add(newFacilityGO);
                    //sysController.StarSysData.TotalSysPowerLoad += obData.PowerLoad;
                }
            }
            else if (prefab == ResearchCenterPrefab)
            {
                ResearchCenterData researchData = new ResearchCenterData("null");
                var rSO = GetResearchCenterSObyCivInt((int)civ);
                researchData.CivEnum = civ;
                researchData.TechLevel = techLevel;
                researchData.FacilitiesEnumType = StarSysFacilityType.ResearchCenter;
                researchData.Name = rSO.Name;
                researchData.StartStarDate = startingStarDate;
                researchData.BuildDuration = rSO.BuildDuration;
                researchData.PowerLoad = rSO.PowerLoad;
                researchData.ResearchCenterSprite = ThemeManager.Instance.GetThemeByCivEnum(civ).ResearchCenterImage;
                researchData.Description = rSO.Description;
                sysController.StarSysData.ResearchCenterData = researchData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
                    newFacilityGO.transform.SetParent(sysController.transform);
                    TextMeshProUGUI On = newFacilityGO.AddComponent<TextMeshProUGUI>();
                    On.text = onOff.ToString();
                    GetResearchCenterText(newFacilityGO, researchData, numOf);
                    newFacilityGO.SetActive(false);
                    researchData.SysGameObject = newFacilityGO;
                    returnList.Add(newFacilityGO);
                    //sysController.StarSysData.TotalSysPowerLoad += researchData.PowerLoad;
                }
            }
            return returnList;
        }

        private void GetPowerPlantText(StarSysController sysCon, GameObject newFacilityGo, int numOf)
        {
            int plants = 0;
            int powerOut = sysCon.StarSysData.PowerPlantData.BasePowerOutput;
            string description = sysCon.StarSysData.PowerPlantData.Description;
            string name = sysCon.StarSysData.PowerPlantData.Name;
            if (sysCon.StarSysData.PowerPlants != null)
                plants += sysCon.StarSysData.PowerPlants.Count();
            TextMeshProUGUI[] TheText = newFacilityGo.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < TheText.Length; i++)
            {
                TheText[i].enabled = true;
                if (TheText[i].name == "NameText (TMP)")
                    TheText[i].text = name;
                else if (TheText[i].name == "NumTotalUnits (TMP)")
                {
                    TheText[i].text = (numOf + plants).ToString();
                }
                else if (TheText[i].name == "NumTotalEOut (TMP)")
                {
                    int numPower = powerOut;
                    if (sysCon.StarSysData.PowerPlants != null)
                    {
                        numPower = sysCon.StarSysData.PowerPlants.Count() * powerOut;
                    }
                    TheText[i].text = numPower.ToString();
                }
                else if (TheText[i].name == "DescriptionText (TMP)")
                    TheText[i].text = description;
                //Doing the system power load in SysData/ GalaxyMenuUIController //else if (OneTmp.name == "NumP Load")
            }
        }
        private void GetFactoryText(GameObject go, FactoryData factoryData, int numOf)
        {
            TextMeshProUGUI[] TheText = go.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < TheText.Length; i++)
            {
                TheText[i].enabled = true;
                if (TheText[i].name == "NameFactory")
                    TheText[i].text = factoryData.Name;
                else if (TheText[i].name == "NumFactoryRatio")
                    TheText[i].text = numOf.ToString();
                else if (TheText[i].name == "FactoryLoad")
                    TheText[i].text = factoryData.PowerLoad.ToString();
                else if (TheText[i].name == "DescriptionFactory")
                    TheText[i].text = factoryData.Description;
                // image here
            }
        }

        private void GetShipyardText(GameObject go, ShipyardData factoryData, int numOf)
        {
            TextMeshProUGUI[] TheText = go.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < TheText.Length; i++)
            {
                TheText[i].enabled = true;
                if (TheText[i].name == "NameShipyard")
                    TheText[i].text = factoryData.Name;
                else if (TheText[i].name == "NumShipyardRatio")
                    TheText[i].text = numOf.ToString();
                else if (TheText[i].name == "ShipyardLoad")
                    TheText[i].text = factoryData.PowerLoad.ToString();
                else if (TheText[i].name == "DescriptionShipyard")
                    TheText[i].text = factoryData.Description;
                // image here

            }
        }
        private void GetShieldGText(GameObject go, ShieldGeneratorData shieldData, int numOf)
        {
            TextMeshProUGUI[] TheText = go.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < TheText.Length; i++)
            {
                TheText[i].enabled = true;
                if (TheText[i].name == "NameShieldG")
                    TheText[i].text = shieldData.Name;
                else if (TheText[i].name == "NumShieldGRatio")
                    TheText[i].text = numOf.ToString();
                else if (TheText[i].name == "ShieldGLoad")
                    TheText[i].text = shieldData.PowerLoad.ToString();
                else if (TheText[i].name == "DescriptionShieldG")
                    TheText[i].text = shieldData.Description;
                // image here

            }
        }
        private void GetOBText(GameObject go, OrbitalBatteryData oBData, int numOf)
        {
            TextMeshProUGUI[] TheText = go.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < TheText.Length; i++)
            {
                TheText[i].enabled = true;
                if (TheText[i].name == "NameOB")
                    TheText[i].text = oBData.Name;
                else if (TheText[i].name == "NumOBRatio")
                    TheText[i].text = numOf.ToString();
                else if (TheText[i].name == "OBLoad")
                    TheText[i].text = oBData.PowerLoad.ToString();
                else if (TheText[i].name == "DescriptionOB")
                    TheText[i].text = oBData.Description;
                // image here

            }
        }
        private void GetResearchCenterText(GameObject go, ResearchCenterData resData, int numOf)
        {
            TextMeshProUGUI[] TheText = go.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < TheText.Length; i++)
            {
                TheText[i].enabled = true;
                if (TheText[i].name == "NameResearchCenter")
                    TheText[i].text = resData.Name;
                else if (TheText[i].name == "NumResearchCenterRatio")
                    TheText[i].text = numOf.ToString();
                else if (TheText[i].name == "ResearchCenterLoad")
                    TheText[i].text = resData.PowerLoad.ToString();
                else if (TheText[i].name == "DescriptionResearchCenter")
                    TheText[i].text = resData.Description;
                // image here

            }
        }
        private StarSysSO GetStarSObyInt(int sysInt)
        {
            if (starSysSOList == null || starSysSOList.Count == 0)
            {
                Debug.LogError("❌ starSysSOList is null or empty! Cannot find star system.");
                return null;
            }

            StarSysSO result = null;
            for (int i = 0; i < starSysSOList.Count; i++)
            {
                // Add null check
                if (starSysSOList[i] == null)
                {
                    Debug.LogWarning($"⚠️ starSysSOList[{i}] is null, skipping");
                    continue;
                }

                if (starSysSOList[i].StarSysInt == sysInt)
                {
                    result = starSysSOList[i];
                    break;
                }
            }

            if (result == null)
            {
                Debug.LogWarning($"⚠️ No StarSysSO found with StarSysInt={sysInt}");
            }

            return result;
        }
        private PowerPlantSO GetPowrPlantSObyCivEnum(CivEnum civ)
        {
            PowerPlantSO result = null;
            if ((int)civ <= 6)
            {
                result = powerPlantSOList[(int)civ];
            }
            else
            {
                result = powerPlantSOList[0];
            }
            return result;
        }
        private FactorySO GetFactorySObyCivInt(int civInt)
        {
            FactorySO result = null;
            if (civInt <= 6)
            {
                result = factorySOList[civInt];
            }
            else
            {
                result = factorySOList[0];
            }
            return result;
        }
        private ShipyardSO GetShipyardSObyCivInt(int civInt)
        {
            ShipyardSO result = null;

            if (civInt <= 6)
            {
                result = shipyardSOList[civInt];
            }
            else
            {
                result = shipyardSOList[0];
            }
            return result;
        }
        private ShieldGeneratorSO GetShieldGeneratorSObyCivInt(int civInt)
        {
            ShieldGeneratorSO result = null;
            if (civInt <= 6)
            {
                result = shieldGeneratorSOList[civInt];
            }
            else
            {
                result = shieldGeneratorSOList[0];
            }
            return result;
        }
        private OrbitalBatterySO GetOrbitalBatterySObyCivInt(int civInt)
        {
            OrbitalBatterySO result = null;
            if (civInt <= 6)
            {
                result = orbitalBatterySOList[civInt];
            }
            else
            {
                result = orbitalBatterySOList[0];
            }
            return result;
        }
        private ResearchCenterSO GetResearchCenterSObyCivInt(int civInt)
        {
            ResearchCenterSO result = null;
            if (civInt <= 6)
            {
                result = researchCenterSOList[civInt];
            }
            else
            {
                result = researchCenterSOList[0];
            }
            return result;
        }
        public StarSysData GetStarSysDataByName(string name)
        {

            StarSysData result = null;
            for (int i = 0; i < StarSysControllerList.Count; i++)
            {

                if (StarSysControllerList[i].StarSysData.GetSysName().Equals(name))
                {
                    result = StarSysControllerList[i].StarSysData;
                    break;
                }
            }
            return result;

        }

        // StarSysController has no NetworkIdentity (it's a plain MonoBehaviour, not networked), so a
        // fleet destination Command can't take one as a parameter directly - it sends the system name
        // instead and this does the server-side lookup back to the actual GameObject.
        public StarSysController GetStarSysControllerByName(string name)
        {
            for (int i = 0; i < StarSysControllerList.Count; i++)
            {
                if (StarSysControllerList[i].StarSysData.GetSysName().Equals(name))
                    return StarSysControllerList[i];
            }
            return null;
        }

        public void UpdateStarSystemOwner(CivEnum civCurrent, CivEnum civNew)
        {
            foreach (var sysCon in StarSysControllerList)
            {
                if (sysCon.StarSysData.GetFirstOwner() == civCurrent)
                    sysCon.StarSysData.CurrentOwnerCivEnum = civNew;
            }
        }
        /// <summary>
        /// Enables/disables ship build items based on the system owner's tech level
        /// Called when opening the build UI
        /// </summary>
        /// <summary>
        /// Enables/disables ship build items based on the system owner's tech level
        /// Called when opening the build UI
        /// </summary>
        public void UpdateAvailableShipsByTechLevel(StarSysController sysCon, GameObject buildUIInstance)
        {
            if (sysCon == null || buildUIInstance == null)
            {
                Debug.LogError("UpdateAvailableShipsByTechLevel: sysCon or buildUIInstance is null");
                return;
            }

            CivEnum ownerCiv = sysCon.StarSysData.CurrentOwnerCivEnum;
            TechLevel currentTechLevel = sysCon.StarSysData.CurrentCivController.CivData.CurrentTechLevel;

            Debug.Log($"=== UpdateAvailableShipsByTechLevel: {sysCon.name} ({ownerCiv}) at {currentTechLevel} ===");

            // ✅ Find all ship build drag items in the build UI
            ShipBuildDrag[] shipBuildItems = buildUIInstance.GetComponentsInChildren<ShipBuildDrag>(true);

            foreach (var shipItem in shipBuildItems)
            {
                // ✅ CRITICAL: Use GetShipSOAtBestTechLevel instead of GetShipSO
                // GetShipSO has fallback logic that returns Scout when ship not found
                // GetShipSOAtBestTechLevel returns null when ship not available
                ShipSO shipAtCurrentTech = ShipManager.Instance.GetShipSOAtBestTechLevel(
                    shipItem.ShipType,
                    currentTechLevel,
                    ownerCiv);

                bool isAvailable = (shipAtCurrentTech != null);

                if (isAvailable)
                {
                    // ✅ Ship available - enable and set normal appearance
                    shipItem.gameObject.SetActive(true);

                    var canvasGroup = shipItem.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = 1.0f;
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    }

                    Debug.Log($"  ✅ Enabled: {shipItem.ShipType} (found {shipAtCurrentTech.ShipName} at {shipAtCurrentTech.TechLevel})");
                }
                else
                {
                    // ❌ Ship not available at current tech - grey out and disable
                    shipItem.gameObject.SetActive(true);

                    var canvasGroup = shipItem.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = shipItem.gameObject.AddComponent<CanvasGroup>();
                    }

                    canvasGroup.alpha = 0.3f; // Greyed out
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;

                    Debug.Log($"  🔒 Locked: {shipItem.ShipType} (not available at {currentTechLevel})");
                }
            }

            // Refresh turn/Li_2 costs now that tech level has changed
            PopulateBuildCostTexts(sysCon, buildUIInstance);

            Debug.Log("=== UpdateAvailableShipsByTechLevel: Complete ===");
        }

        private void EnsureSystemShipUIs(StarSysController sysCon)
        {
            if (sysCon == null || sysCon.StarSysData == null) return;

            var shipManager = ShipManager.Instance;
            if (shipManager == null) return;

            // Preferred parent for ship UI items created by this fleet UI
            GameObject shipListParent = sysCon.StarSysData.ShipListUIParent;

            // If there's no parent yet, give ShipManager a chance to reparent pending UIs and return
            if (shipListParent == null)
            {
                shipManager.ProcessPendingShipUIs();
                return;
            }

            // Iterate fleet ship list and ensure UI exists and is parented to the fleet UI's ShipList container
            var ships = sysCon.StarSysData.ShipsList;
            if (ships == null || ships.Count == 0) return;

            for (int i = 0; i < ships.Count; i++)
            {
                var shipCon = ships[i];
                if (shipCon == null) continue;

                // If UI doesn't exist yet, instantiate it (ShipManager handles queuing/reparenting)
                if (shipCon.ShipListUIGameObject == null)
                {
                    shipManager.InstantiateShipListUIGameObject(shipCon, sysCon.gameObject);
                }

                // If UI exists but not parented correctly, set the correct parent
                if (shipCon.ShipListUIGameObject != null)
                {
                    var currentParent = shipCon.ShipListUIGameObject.transform.parent;
                    if (currentParent == null || currentParent.gameObject != shipListParent)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(shipListParent.transform, false);
                    }
                }
            }

            // Final pass to process any items that were queued by InstantiateShipListUIGameObject
            shipManager.ProcessPendingShipUIs();
        }
        public void HideBuildUI()
        {
            var buildUIFields = Object.FindFirstObjectByType<BuildUIFields>(FindObjectsInactive.Exclude);
            if (buildUIFields != null)
                buildUIFields.gameObject.SetActive(false);
            // Do NOT deactivate canvasBuildList — ASystemMenuView may be a child of it,
            // and SetActive(false) on a parent hides children even after SetActive(true) on the child.
        }

        public void InstantiateSysBuildUI(StarSysController sysCon) // open the build queue UI
        {
            Debug.Log($"InstantiateSysBuildUI: Opening for system '{sysCon.name}'");

            // Search inactive objects too — HideBuildUI deactivates rather than destroys
            var existingBuildUIFields = Object.FindFirstObjectByType<BuildUIFields>(FindObjectsInactive.Include);
            if (existingBuildUIFields != null)
            {
                if (currentBuildUISysCon == sysCon)
                {
                    // Same system reopened — just show the existing UI so the queue is preserved
                    Debug.Log("  Reusing existing build UI for same system");
                    existingBuildUIFields.gameObject.SetActive(true);
                    canvasBuildList.SetActive(true);
                    return;
                }
                Debug.Log("  Destroying previous build UI (different system): " + existingBuildUIFields.gameObject.name);
                Destroy(existingBuildUIFields.gameObject);
                currentBuildUISysCon = null;
            }
            currentBuildUISysCon = sysCon;

            GameObject sysBuildListInstance = Instantiate(sysBuildUIListPrefab, new Vector3(0, -70, 0), Quaternion.identity);
            sysBuildListInstance.layer = 5;

            // Populate system name header
            var buildUIFields = sysBuildListInstance.GetComponent<BuildUIFields>();
            if (buildUIFields?.systemNameTMP != null)
                buildUIFields.systemNameTMP.text = sysCon.StarSysData.SysName;

            // ✅ Set civ-specific images FIRST (before anything else)
            SetFacilityBuildImages(sysCon, sysBuildListInstance);
            SetShipBuildImages(sysCon, sysBuildListInstance);
            PopulateBuildCostTexts(sysCon, sysBuildListInstance);

            // Find GridLayoutGroups
            GridLayoutGroup[] grids = sysBuildListInstance.GetComponentsInChildren<GridLayoutGroup>();
            for (int i = 0; i < grids.Length; i++)
            {
                grids[i].enabled = true;
                if (grids[i].name == "QueueHoldingBuildables")
                    sysCon.BuildListGridLayoutGroup = grids[i];
                else if (grids[i].name == "QueueHoldingBuildableShips")
                    sysCon.ShipListGridLayoutGroup = grids[i];
            }

            // Initialize watchers explicitly
            foreach (var watcher in sysBuildListInstance.GetComponentsInChildren<BuildQueueWatcher>(true))
            {
                watcher.Initialize(sysCon);
            }

            foreach (var watcher in sysBuildListInstance.GetComponentsInChildren<ShipQueueWatcher>(true))
            {
                watcher.Initialize(sysCon);
            }

            GalaxyMenuUIController.Instance.SetActiveBuildMenu(sysBuildListInstance);
            canvasBuildList.SetActive(true);
            sysBuildListInstance.transform.SetParent(canvasBuildList.transform, false);

            // ✅ CRITICAL: Find and wire inventory slot references AND sliders
            Transform[] allTransforms = sysBuildListInstance.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                switch (t.name)
                {
                    case "PowerPlantInventorySlot":
                        powerPlantInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found powerPlantInventorySlot");
                        break;
                    case "FactoryInventorySlot":
                        factoryInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found factoryInventorySlot");
                        break;
                    case "ShipyardInventorySlot":
                        shipyardInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found shipyardInventorySlot");
                        break;
                    case "ShieldGenInventorySlot":
                        shieldGenInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found shieldGenInventorySlot");
                        break;
                    case "OrbitalBatteryInventorySlot":
                        orbitalBatteryInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found orbitalBatteryInventorySlot");
                        break;
                    case "ResearchCenterInventorySlot":
                        researchCenterInventory_slot = t.gameObject;
                        Debug.Log("  ✅ Found researchCenterInventory_slot");
                        break;
                    // ✅ Ship blueprint slots
                    case "ScoutInventorySlot":
                        scoutInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found scoutInventorySlot");
                        break;
                    case "DestroyerInventorySlot":
                        destroyerInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found destroyerInventorySlot");
                        break;
                    case "CruiserInventorySlot":
                        cruiserInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found cruiserInventorySlot");
                        break;
                    case "LtCruiserInventorySlot":
                        ltCruiserInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found ltCruiserInventorySlot");
                        break;
                    case "HvyCruiserInventorySlot":
                        hvyCruiserInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found hvyCruiserInventorySlot");
                        break;
                    case "TransportInventorySlot":
                        transportInventorySlot = t.gameObject;
                        Debug.Log("  ✅ Found transportInventorySlot");
                        break;
                }
            }

            // ✅ NEW: Find and wire progress sliders
            Slider[] allSliders = sysBuildListInstance.GetComponentsInChildren<Slider>(true);
            Debug.Log($"  Found {allSliders.Length} sliders in build UI");

            foreach (Slider slider in allSliders)
            {
                if (slider.name.Contains("FactoryProgressBar"))
                {
                    if (StarSysMenuUIController.Instance != null)
                    {
                        StarSysMenuUIController.Instance.SliderBuildProgress = slider;
                        Debug.Log($"  ✅ Wired facility build slider: '{slider.name}'");
                    }
                }
                else if (slider.name.Contains("ShipyardProgressBar"))
                {
                    if (StarSysMenuUIController.Instance != null)
                    {
                        StarSysMenuUIController.Instance.ShipSliderBuildProgress = slider;
                        Debug.Log($"  ✅ Wired ship build slider: '{slider.name}'");
                    }
                }
            }
            SetShipBuildImages(sysCon, sysBuildListInstance);
            SetFacilityBuildImages(sysCon, sysBuildListInstance);
            PopulateBuildCostTexts(sysCon, sysBuildListInstance);

            Debug.Log($"InstantiateStarSysBuildListUI: Complete for '{sysCon.name}'");
            // ✅ NEW: Find and wire CloseBuilding button
            Button[] allButtons = sysBuildListInstance.GetComponentsInChildren<Button>(true);
            Debug.Log($"  Found {allButtons.Length} buttons in build UI");

            foreach (Button button in allButtons)
            {
                if (button.name == "CloseBuilding") //close the build queues menu
                {
                    button.onClick.RemoveAllListeners(); // Clear any existing listeners
                    button.onClick.AddListener(() =>
                    {
                        Debug.Log("CloseBuilding button clicked");
                        if (StarSysMenuUIController.Instance != null)
                        {
                            StarSysMenuUIController.Instance.CloseBuildingQueues();
                        }
                    });
                    Debug.Log($"  ✅ Wired CloseBuilding button: '{button.name}'");
                }
            }

            // ✅ Validate that critical slots were found
            if (powerPlantInventorySlot == null)
                Debug.LogError("  ❌ powerPlantInventorySlot NOT FOUND in build UI prefab!");
            if (factoryInventorySlot == null)
                Debug.LogError("  ❌ factoryInventorySlot NOT FOUND in build UI prefab!");
            if (shipyardInventorySlot == null)
                Debug.LogError("  ❌ shipyardInventorySlot NOT FOUND in build UI prefab!");
            if (shieldGenInventorySlot == null)
                Debug.LogError("  ❌ shieldGenInventorySlot NOT FOUND in build UI prefab!");
            if (orbitalBatteryInventorySlot == null)
                Debug.LogError("  ❌ orbitalBatteryInventorySlot NOT FOUND in build UI prefab!");
            if (researchCenterInventory_slot == null)
                Debug.LogError("  ❌ researchCenterInventory_slot NOT FOUND in build UI prefab!");

            if (StarSysMenuUIController.Instance?.SliderBuildProgress == null)
                Debug.LogWarning("  ⚠️ Facility build slider not found!");
            if (StarSysMenuUIController.Instance?.ShipSliderBuildProgress == null)
                Debug.LogWarning("  ⚠️ Ship build slider not found!");

            // ✅ CRITICAL: Set StarSysController reference on build-able items
            FactoryBuildItemDrag[] buildable = sysBuildListInstance.GetComponentsInChildren<FactoryBuildItemDrag>(true);
            Debug.Log($"  Found {buildable.Length} FactoryBuildItemDrag components");

            for (int m = 0; m < buildable.Length; m++)
            {
                buildable[m].StarSysController = sysCon; // ✅ Wire the reference!

                if (buildable[m].name == "ItemPowerPlant")
                    buildable[m].FacilityType = StarSysFacilityType.PowerPlanet;
                else if (buildable[m].name == "ItemFactory")
                    buildable[m].FacilityType = StarSysFacilityType.Factory;
                else if (buildable[m].name == "ItemShipyard")
                    buildable[m].FacilityType = StarSysFacilityType.Shipyard;
                else if (buildable[m].name == "ItemShieldGenerator")
                    buildable[m].FacilityType = StarSysFacilityType.ShieldGenerator;
                else if (buildable[m].name == "ItemOrbitalBattery")
                    buildable[m].FacilityType = StarSysFacilityType.OrbitalBattery;
                else if (buildable[m].name == "ItemResearchCenter")
                    buildable[m].FacilityType = StarSysFacilityType.ResearchCenter;

                Debug.Log($"    Wired '{buildable[m].name}' to system '{sysCon.name}'");
            }

            // ✅ Also wire ship drag items
            ShipBuildDrag[] shipDragItems = sysBuildListInstance.GetComponentsInChildren<ShipBuildDrag>(true);
            Debug.Log($"  Found {shipDragItems.Length} ShipBuildDrag components");

            foreach (var shipDrag in shipDragItems)
            {
                shipDrag.StarSysController = sysCon;
                shipDrag.ShipType = shipDrag.gameObject.name switch
                {
                    "ItemScout" => ShipType.Scout,
                    "ItemDestroyer" => ShipType.Destroyer,
                    "ItemCruiser" => ShipType.Cruiser,
                    "ItemLtCruiser" => ShipType.LtCruiser,
                    "ItemHvyCruiser" => ShipType.HvyCruiser,
                    "ItemTransport" => ShipType.Transport,
                    _ => shipDrag.ShipType
                };
                Debug.Log($"    Wired ship drag '{shipDrag.name}' (type={shipDrag.ShipType}) to system '{sysCon.name}'");
            }

            // ✅ NEW: Filter ship build items based on tech level
            UpdateAvailableShipsByTechLevel(sysCon, sysBuildListInstance);
            ApplyDilithiumAvailability(sysCon, sysBuildListInstance);

            Debug.Log($"InstantiateStarSysBuildListUI: Complete for '{sysCon.name}'");
        }
        public void NewImageInEmptyBuildAbleInventory(StarSysFacilityType type, StarSysController sysCon)
        {
            Debug.Log($"NewImageInEmptyBuildAbleInventory: type={type}, sysCon={sysCon?.name}");

            if (sysCon == null)
            {
                Debug.LogError("NewImageInEmptyBuildAbleInventory: sysCon is null!");
                return;
            }

            switch (type)
            {
                case StarSysFacilityType.PowerPlanet:
                    if (powerPlantInventorySlot == null)
                    {
                        Debug.LogError($"NewImageInEmptyBuildAbleInventory: powerPlantInventorySlot is NULL! Open the build menu first for system '{sysCon.name}'");
                        return;
                    }
                    if (powerPlantInventorySlotPrefab == null)
                    {
                        Debug.LogError("NewImageInEmptyBuildAbleInventory: powerPlantInventorySlotPrefab not assigned!");
                        return;
                    }

                    GameObject imageObPower = Instantiate(powerPlantInventorySlotPrefab, Vector3.zero, Quaternion.identity);
                    if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
                        imageObPower.GetComponentInChildren<Image>().sprite = ThemeManager.Instance.CurrentTheme.PowerPlantImage;
                    imageObPower.transform.SetParent(powerPlantInventorySlot.transform, false);
                    { var d = imageObPower.GetComponent<FactoryBuildItemDrag>(); if (d != null) d.StarSysController = sysCon; }
                    break;

                case StarSysFacilityType.Factory:
                    if (factoryInventorySlot == null)
                    {
                        Debug.LogError($"NewImageInEmptyBuildAbleInventory: factoryInventorySlot is NULL! Open the build menu first for system '{sysCon.name}'");
                        return;
                    }
                    if (factoryInventorySlotPrefab == null)
                    {
                        Debug.LogError("NewImageInEmptyBuildAbleInventory: factoryInventorySlotPrefab not assigned!");
                        return;
                    }

                    GameObject imageObFactory = Instantiate(factoryInventorySlotPrefab, Vector3.zero, Quaternion.identity);
                    if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
                        imageObFactory.GetComponentInChildren<Image>().sprite = ThemeManager.Instance.CurrentTheme.FactoryImage;
                    imageObFactory.transform.SetParent(factoryInventorySlot.transform, false);
                    { var d = imageObFactory.GetComponent<FactoryBuildItemDrag>(); if (d != null) d.StarSysController = sysCon; }
                    break;

                case StarSysFacilityType.Shipyard:
                    if (shipyardInventorySlot == null)
                    {
                        Debug.LogError($"NewImageInEmptyBuildAbleInventory: shipyardInventorySlot is NULL! Open the build menu first for system '{sysCon.name}'");
                        return;
                    }
                    if (shipyardInventorySlotPrefab == null)
                    {
                        Debug.LogError("NewImageInEmptyBuildAbleInventory: shipyardInventorySlotPrefab not assigned!");
                        return;
                    }

                    GameObject imageObShipyard = Instantiate(shipyardInventorySlotPrefab, Vector3.zero, Quaternion.identity);
                    if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
                        imageObShipyard.GetComponentInChildren<Image>().sprite = ThemeManager.Instance.CurrentTheme.ShipyardImage;
                    imageObShipyard.transform.SetParent(shipyardInventorySlot.transform, false);
                    { var d = imageObShipyard.GetComponent<FactoryBuildItemDrag>(); if (d != null) d.StarSysController = sysCon; }
                    break;

                case StarSysFacilityType.ShieldGenerator:
                    if (shieldGenInventorySlot == null)
                    {
                        Debug.LogError($"NewImageInEmptyBuildAbleInventory: shieldGenInventorySlot is NULL! Open the build menu first for system '{sysCon.name}'");
                        return;
                    }
                    if (shieldGenInventorySlotPrefab == null)
                    {
                        Debug.LogError("NewImageInEmptyBuildAbleInventory: shieldGenInventorySlotPrefab not assigned!");
                        return;
                    }

                    GameObject imageObShield = Instantiate(shieldGenInventorySlotPrefab, Vector3.zero, Quaternion.identity);
                    if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
                        imageObShield.GetComponentInChildren<Image>().sprite = ThemeManager.Instance.CurrentTheme.ShieldImage;
                    imageObShield.transform.SetParent(shieldGenInventorySlot.transform, false);
                    { var d = imageObShield.GetComponent<FactoryBuildItemDrag>(); if (d != null) d.StarSysController = sysCon; }
                    break;

                case StarSysFacilityType.OrbitalBattery:
                    if (orbitalBatteryInventorySlot == null)
                    {
                        Debug.LogError($"NewImageInEmptyBuildAbleInventory: orbitalBatteryInventorySlot is NULL! Open the build menu first for system '{sysCon.name}'");
                        return;
                    }
                    if (orbitalBatteryInventorySlotPrefab == null)
                    {
                        Debug.LogError("NewImageInEmptyBuildAbleInventory: orbitalBatteryInventorySlotPrefab not assigned!");
                        return;
                    }

                    GameObject imageObOB = Instantiate(orbitalBatteryInventorySlotPrefab, Vector3.zero, Quaternion.identity);
                    if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
                        imageObOB.GetComponentInChildren<Image>().sprite = ThemeManager.Instance.CurrentTheme.OrbitalBatteriesImage;
                    imageObOB.transform.SetParent(orbitalBatteryInventorySlot.transform, false);
                    { var d = imageObOB.GetComponent<FactoryBuildItemDrag>(); if (d != null) d.StarSysController = sysCon; }
                    break;

                case StarSysFacilityType.ResearchCenter:
                    if (researchCenterInventory_slot == null)
                    {
                        Debug.LogError($"NewImageInEmptyBuildAbleInventory: researchCenterInventory_slot is NULL! Open the build menu first for system '{sysCon.name}'");
                        return;
                    }
                    if (researchCenterInventorySlotPrefab == null)
                    {
                        Debug.LogError("NewImageInEmptyBuildAbleInventory: researchCenterInventorySlotPrefab not assigned!");
                        return;
                    }

                    GameObject imageObRC = Instantiate(researchCenterInventorySlotPrefab, Vector3.zero, Quaternion.identity);
                    if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
                        imageObRC.GetComponentInChildren<Image>().sprite = ThemeManager.Instance.CurrentTheme.ResearchCenterImage;
                    imageObRC.transform.SetParent(researchCenterInventory_slot.transform, false);
                    { var d = imageObRC.GetComponent<FactoryBuildItemDrag>(); if (d != null) d.StarSysController = sysCon; }
                    break;

                default:
                    Debug.LogWarning($"NewImageInEmptyBuildAbleInventory: Unknown facility type {type}");
                    break;
            }
        }

        public void NewImageInShipInventory(ShipType shiptype)
        {
            switch (shiptype)
            {
                case ShipType.Scout:
                    GameObject ItemSGO = Instantiate(scoutBluePrintPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    ItemSGO.transform.SetParent(scoutInventorySlot.transform, false);
                    break;

                case ShipType.Destroyer:
                    GameObject ItemDGO = Instantiate(destroyerBluePrintPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    ItemDGO.transform.SetParent(destroyerInventorySlot.transform, false);
                    break;

                case ShipType.Cruiser:
                    GameObject cruiserItemGO = Instantiate(cruiserBluePrintPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    cruiserItemGO.transform.SetParent(cruiserInventorySlot.transform, false);
                    break;

                case ShipType.LtCruiser:
                    GameObject ltCruiserItemGO = Instantiate(ltCruiserBluePrintPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    ltCruiserItemGO.transform.SetParent(ltCruiserInventorySlot.transform, false);
                    break;

                case ShipType.HvyCruiser:
                    GameObject hvyCruiserItemGO = Instantiate(hvyCruiserBluePrintPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    hvyCruiserItemGO.transform.SetParent(hvyCruiserInventorySlot.transform, false);
                    break;

                case ShipType.Transport:
                    GameObject transportItemGO = Instantiate(transportBluePrintPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    transportItemGO.transform.SetParent(transportInventorySlot.transform, false);
                    break;

                default:
                    Debug.LogWarning($"NewImageInShipInventory: Unknown ship type {shiptype}");
                    break;
            }
        }

        /// <summary>
        /// Makes all star system names visible for a specific civilization
        /// Called when first contact is made or when a civ is discovered
        /// </summary>
        public void ExposeAllSystemName(CivEnum civEnum)
        {
            Debug.Log($"ExposeAllSystemName: Revealing system names for {civEnum}");

            if (!localPlayerCanSeeMyNameList.Contains(civEnum))
            {
                localPlayerCanSeeMyNameList.Add(civEnum);
            }

            foreach (var starSysController in StarSysControllerList)
            {
                if (starSysController == null) continue;

                if (starSysController.StarSysData.CurrentOwnerCivEnum == civEnum)
                {
                    StarSysChildFields starSysFields = starSysController.GetComponent<StarSysChildFields>();
                    if (starSysFields != null)
                    {
                        // Reveal system name
                        if (starSysFields.SysName != null)
                        {
                            starSysFields.SysName.text = starSysController.StarSysData.SysName;
                            starSysFields.SysName.gameObject.SetActive(true);
                        }

                        // Reveal owner insignia
                        if (starSysFields.OwnerInsigniaGO != null)
                        {
                            starSysFields.OwnerInsigniaGO.SetActive(true);
                            var sr = starSysFields.OwnerInsigniaGO.GetComponent<SpriteRenderer>();
                            if (sr != null) sr.enabled = true;
                        }
                    }
                }
            }

            Debug.Log($"ExposeAllSystemName: Complete for {civEnum}");
        }

        /// <summary>
        /// Moves a ship from a star system to a fleet or another system
        /// </summary>
        public void MoveShipOutOfStarSys(ShipController shipCon, FleetController targetFleet, StarSysController targetSys)
        {
            if (shipCon == null)
            {
                Debug.LogWarning("MoveShipOutOfStarSys: shipCon is null");
                return;
            }

            // Remove from current star system
            if (shipCon.ShipData.CurrentStarSysController != null)
            {
                shipCon.ShipData.CurrentStarSysController.StarSysData.ShipsList.Remove(shipCon);
                Debug.Log($"Removed ship '{shipCon.ShipData.ShipName}' from system '{shipCon.ShipData.CurrentStarSysController.name}'");
            }

            // Add to target fleet or system
            if (targetFleet != null)
            {
                targetFleet.FleetData.ShipsList.Add(shipCon);
                shipCon.ShipData.CurrentFleetController = targetFleet;
                shipCon.ShipData.CurrentStarSysController = null;
                Debug.Log($"Added ship '{shipCon.ShipData.ShipName}' to fleet '{targetFleet.name}'");
            }
            else if (targetSys != null)
            {
                targetSys.StarSysData.ShipsList.Add(shipCon);
                shipCon.ShipData.CurrentStarSysController = targetSys;
                shipCon.ShipData.CurrentFleetController = null;
                Debug.Log($"Added ship '{shipCon.ShipData.ShipName}' to system '{targetSys.name}'");
            }
        }

        [ContextMenu("Debug: List All Facility SO Names")]
        private void DebugListFacilitySONames()
        {
            Debug.Log("=== Facility SO Names by Civilization ===");

            for (int i = 0; i < 7; i++) // 7 civs (FED=0 to TERRAN=6)
            {
                CivEnum civ = (CivEnum)i;
                Debug.Log($"\n--- {civ} (index {i}) ---");

                if (i < powerPlantSOList.Count)
                    Debug.Log($"  PowerPlant: {powerPlantSOList[i]?.Name ?? "NULL"}");

                if (i < factorySOList.Count)
                    Debug.Log($"  Factory: {factorySOList[i]?.Name ?? "NULL"}");

                if (i < shipyardSOList.Count)
                    Debug.Log($"  Shipyard: {shipyardSOList[i]?.Name ?? "NULL"}");

                if (i < shieldGeneratorSOList.Count)
                    Debug.Log($"  Shield: {shieldGeneratorSOList[i]?.Name ?? "NULL"}");

                if (i < orbitalBatterySOList.Count)
                    Debug.Log($"  Orbital Battery: {orbitalBatterySOList[i]?.Name ?? "NULL"}");

                if (i < researchCenterSOList.Count)
                    Debug.Log($"  Research: {researchCenterSOList[i]?.Name ?? "NULL"}");
            }

            Debug.Log("=== End Facility SO Names ===");
        }

        /// <summary>
        ///
        /// </summary>
        public GameObject InstantiateStarSysUI(StarSysController sysCon)
        {
            Debug.Log($"InstantiateStarSysUI: Creating UI for system '{sysCon.name}'");

            // Create the UI from prefab
            GameObject newUI = Instantiate(sysUIPrefab, Vector3.zero, Quaternion.identity);
            newUI.layer = 5; // UI layer
            newUI.name = $"SystemUI ({sysCon.StarSysData.SysName})";


            // Store reference on the controller
            sysCon.StarSysUIGameObject = newUI;

            // Parent inside SysListContainer (SystemsMenuView/Viewport/SysListContainer) so the
            // RectTransform is already in the right canvas subtree. The UI starts inactive and
            // is activated when the Systems menu is opened.
            var sysListContainer = StarSysMenuUIController.Instance?.SysListContainer;
            if (sysListContainer != null)
            {
                newUI.transform.SetParent(sysListContainer.transform, false);
                Debug.Log($"  ✅ Parented to SysListContainer");
            }
            else
            {
                Debug.LogError("  ❌ StarSysMenuUIController.SysListContainer is null — UI may render incorrectly. Check Inspector assignment on StarSysMenuUIController.");
            }

            // ✅ Set up ShipContent for ship UIs
            var uiFields = newUI.GetComponent<StarSysUI_Fields>();
            if (uiFields != null && uiFields.shipContent != null)
            {
                sysCon.StarSysData.ShipListUIParent = uiFields.shipContent.gameObject;
                Debug.Log($"  ✅ Set ShipListUIParent");
            }

            // ✅ Initially inactive - will be shown when menu opens
            newUI.SetActive(false);

            // ✅ Process any pending ship UIs
            if (ShipManager.Instance != null)
            {
                ShipManager.Instance.ProcessPendingShipUIs(sysCon);
            }

            Debug.Log($"InstantiateStarSysUI: Complete for '{sysCon.name}'");
            return newUI;
        }

        /// <summary>
        /// Sets the facility build item images based on local player's civilization
        /// Called when opening build UI
        /// </summary>
        public void SetFacilityBuildImages(StarSysController sysCon, GameObject buildUIInstance)
        {
            if (sysCon == null || buildUIInstance == null) return;

            CivEnum localCiv = sysCon.StarSysData.CurrentOwnerCivEnum;
            Debug.Log($"SetFacilityBuildImages: Setting for {localCiv}");

            // ✅ Use the theme for the system's owning civ, not whichever theme happens to be
            // globally "current" (e.g. the local player's main-menu selection) - otherwise a
            // system owned by another civ would show the wrong civ's facility art.
            ThemeSO theme = ThemeManager.Instance?.GetThemeByCivEnum(localCiv);
            if (theme == null) return;

            // Find all buildable items
            FactoryBuildItemDrag[] buildableItems = buildUIInstance.GetComponentsInChildren<FactoryBuildItemDrag>(true);

            foreach (var item in buildableItems)
            {
                Sprite sprite = item.name switch
                {
                    "ItemPowerPlant" => theme.PowerPlantImage,
                    "ItemFactory" => theme.FactoryImage,
                    "ItemShipyard" => theme.ShipyardImage,
                    "ItemShieldGenerator" => theme.ShieldImage,
                    "ItemOrbitalBattery" => theme.OrbitalBatteriesImage,
                    "ItemResearchCenter" => theme.ResearchCenterImage,
                    _ => null
                };
                if (sprite == null) continue;

                // ✅ Some item prefabs (e.g. ItemShipyard) render their icon via a nested child
                // "Image" GameObject rather than the root's own Image component - update every
                // Image under this item so the visible one always gets the themed sprite.
                foreach (var img in item.GetComponentsInChildren<Image>(true))
                    img.sprite = sprite;

                Debug.Log($"  ✅ Set {item.name} sprite for {localCiv}");
            }
        }

        /// <summary>
        /// Sets ship build images based on civ and tech level
        /// Only shows ships available for current tech level
        /// </summary>
        public void SetShipBuildImages(StarSysController sysCon, GameObject buildUIInstance)
        {
            if (sysCon == null || buildUIInstance == null) return;

            CivEnum localCiv = sysCon.StarSysData.CurrentOwnerCivEnum;

            // ✅ FIX: Use CURRENT tech level, not starting!
            TechLevel techLevel = sysCon.StarSysData.CurrentCivController.CivData.CurrentTechLevel;

            Debug.Log($"SetShipBuildImages: Civ={localCiv}, TechLevel={techLevel}");

            // ✅ Get ships available at or below current tech level
            List<ShipSO> availableShips = ShipManager.Instance.GetAvailableShipsForCiv(localCiv, techLevel);

            if (availableShips.Count == 0)
            {
                Debug.LogWarning($"  ⚠️ No ships found for {localCiv} at or below {techLevel}!");
                return;
            }

            Debug.Log($"  Found {availableShips.Count} ships: {string.Join(", ", availableShips.Select(s => s.ShipType))}");

            // Find all ship drag items in build UI
            ShipBuildDrag[] shipDragItems = buildUIInstance.GetComponentsInChildren<ShipBuildDrag>(true);

            foreach (var dragItem in shipDragItems)
            {
                // ✅ Find matching ShipSO by type
                ShipSO shipSO = availableShips.FirstOrDefault(s => s.ShipType == dragItem.ShipType);

                // ✅ The per-slot background image (e.g. "ImageScoutBackground") sits alongside
                // the draggable item under the same InventorySlot - mirror the item's sprite onto it.
                Image bgImage = FindShipInventoryBackgroundImage(dragItem);
                Image itemImage = dragItem.GetComponent<Image>();

                if (shipSO != null)
                {
                    // Set image and data
                    if (itemImage != null && shipSO.shipSprite != null)
                    {
                        itemImage.sprite = shipSO.shipSprite;
                        dragItem.ShipSprite = shipSO.shipSprite;
                        int q2 = sysCon.StarSysData.CurrentCivController.CivData.QualityScore;
                        dragItem.BuildDuration = BOTF3D.Combat.ShipStatCalculator.Calculate(
                            shipSO.ShipType, shipSO.TechLevel, localCiv, q2).BuildDuration;
                        dragItem.ShipType = shipSO.ShipType;

                        Debug.Log($"  ✅ Set {shipSO.ShipType} sprite for {localCiv}");
                    }

                    // ✅ Ship is buildable - show the real item image, hide the locked preview
                    if (itemImage != null)
                        itemImage.enabled = true;

                    // ✅ Show this ship type - make it fully interactable
                    dragItem.gameObject.SetActive(true);

                    var canvasGroup = dragItem.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = 1.0f;
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    }

                    if (bgImage != null && shipSO.shipSprite != null)
                    {
                        bgImage.sprite = shipSO.shipSprite;
                        bgImage.enabled = true;
                        bgImage.transform.SetAsLastSibling();
                        Color c = bgImage.color;
                        bgImage.color = new Color(c.r, c.g, c.b, 1f);
                    }
                }
                else
                {
                    // ❌ Ship not available at current tech level

                    // Option A: Hide completely
                    // dragItem.gameObject.SetActive(false);

                    // Option B: Show greyed out (better UX - shows what's coming)
                    dragItem.gameObject.SetActive(true);

                    var canvasGroup = dragItem.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = dragItem.gameObject.AddComponent<CanvasGroup>();
                    }

                    canvasGroup.alpha = 0.3f; // Greyed out
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;

                    // ✅ Preview the ship's eventual look (this civ + type) even though it's
                    // not buildable yet - the background shows what it'll be once unlocked,
                    // dimmed to 30% alpha to read as inactive. Hide the item's default/stale
                    // image underneath so it doesn't show through the preview.
                    if (bgImage != null)
                    {
                        ShipSO futureShipSO = ShipManager.Instance.GetShipSO(localCiv, dragItem.ShipType);
                        if (futureShipSO != null && futureShipSO.shipSprite != null)
                        {
                            bgImage.sprite = futureShipSO.shipSprite;
                            bgImage.enabled = true;
                            bgImage.transform.SetAsLastSibling();
                            Color c = bgImage.color;
                            bgImage.color = new Color(c.r, c.g, c.b, 0.3f);

                            if (itemImage != null)
                                itemImage.enabled = false;
                        }
                        else
                        {
                            bgImage.enabled = false;

                            if (itemImage != null)
                                itemImage.enabled = true;
                        }
                    }

                    Debug.Log($"  🔒 {dragItem.ShipType} locked (requires higher tech level)");
                }
            }
        }

        /// <summary>
        /// Locates the "Image{ShipType}Background" sibling that sits next to a ship's
        /// draggable inventory item (e.g. "ImageScoutBackground" next to "ItemScout"),
        /// mirroring how each facility's InventorySlot pairs a background image with its item.
        /// </summary>
        private Image FindShipInventoryBackgroundImage(ShipBuildDrag dragItem)
        {
            Transform slot = dragItem.transform.parent;
            if (slot == null) return null;

            string bgName = GetShipInventoryBackgroundName(dragItem.ShipType);
            if (bgName == null) return null;

            Transform bg = slot.Find(bgName);
            return bg != null ? bg.GetComponent<Image>() : null;
        }

        private string GetShipInventoryBackgroundName(ShipType type)
        {
            switch (type)
            {
                case ShipType.Scout: return "ImageScoutBackground";
                case ShipType.Destroyer: return "ImageDestroyerBackground";
                case ShipType.Cruiser: return "ImageCruiserBackground";
                case ShipType.LtCruiser: return "ImageLtCruiserBackground";
                case ShipType.HvyCruiser: return "ImageHvyCruiserBackground";
                case ShipType.Transport: return "ImageTransportBackground";
                default: return null;
            }
        }

        /// <summary>
        /// Populates the turn-count and Li_2 text fields in the build UI.
        /// Called on initial open and whenever tech level advances.
        /// </summary>
        public void PopulateBuildCostTexts(StarSysController sysCon, GameObject buildUIInstance)
        {
            if (sysCon == null || buildUIInstance == null) return;
            if (sysCon.StarSysData?.CurrentCivController?.CivData == null) return;

            CivEnum civ = sysCon.StarSysData.CurrentOwnerCivEnum;
            TechLevel tech = sysCon.StarSysData.CurrentCivController.CivData.CurrentTechLevel;
            int quality = sysCon.StarSysData.CurrentCivController.CivData.QualityScore;

            // Build a name → TMP lookup so we can set each field in O(1)
            var tmps = new Dictionary<string, TextMeshProUGUI>();
            foreach (var t in buildUIInstance.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (!tmps.ContainsKey(t.gameObject.name))
                    tmps[t.gameObject.name] = t;

            // ── Facilities ────────────────────────────────────────────────────────────
            SetCostText(tmps, "F_PowerTurns", "F_PowerLi_2",
                GetFacilityTurns(sysCon, StarSysFacilityType.PowerPlanet),
                ShipStatCalculator.GetPowerPlantDilithiumCost(civ));

            SetCostText(tmps, "F_FactoryTurns", "F_FactoryLi_2",
                GetFacilityTurns(sysCon, StarSysFacilityType.Factory),
                GetFactorySObyCivInt((int)civ)?.DilithiumCost ?? 0);

            SetCostText(tmps, "F_YardTurns", "F_YardLi_2",
                GetFacilityTurns(sysCon, StarSysFacilityType.Shipyard),
                GetShipyardSObyCivInt((int)civ)?.DilithiumCost ?? 0);

            SetCostText(tmps, "F_ShieldTurns", "F_ShieldLi_2",
                GetFacilityTurns(sysCon, StarSysFacilityType.ShieldGenerator),
                GetShieldGeneratorSObyCivInt((int)civ)?.DilithiumCost ?? 0);

            SetCostText(tmps, "F_ResearchTurns", "F_ResearchLi_2",
                GetFacilityTurns(sysCon, StarSysFacilityType.ResearchCenter),
                GetResearchCenterSObyCivInt((int)civ)?.DilithiumCost ?? 0);

            SetCostText(tmps, "F_OBTurns", "F_OBLi_2",
                GetFacilityTurns(sysCon, StarSysFacilityType.OrbitalBattery),
                GetOrbitalBatterySObyCivInt((int)civ)?.DilithiumCost ?? 0);

            // ── Ships ──────────────────────────────────────────────────────────────────
            SetShipCostText(tmps, "ScoutTurns", "ScoutLi_2", ShipType.Scout, tech, civ, quality);
            SetShipCostText(tmps, "DestroyerTurns", "DestroyerLi_2", ShipType.Destroyer, tech, civ, quality);
            SetShipCostText(tmps, "CruiserTurns", "CruiserLi_2", ShipType.Cruiser, tech, civ, quality);
            SetShipCostText(tmps, "LtCruiserTurns", "LtCruiserLi_2", ShipType.LtCruiser, tech, civ, quality);
            SetShipCostText(tmps, "HvyCruiserTurns", "HvyCruiserLi_2", ShipType.HvyCruiser, tech, civ, quality);
            SetShipCostText(tmps, "TransTurns", "TransLi_2", ShipType.Transport, tech, civ, quality);

            // ── Dilithium Stockpile ────────────────────────────────────────────────────
            // The value text is named "DilithiumText" inside the "Ditlithium Stockpile" container
            var stockpileRoot = buildUIInstance.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Dilithium Stockpile");
            if (stockpileRoot != null)
            {
                var valueText = stockpileRoot.GetComponentInChildren<TextMeshProUGUI>(true);
                if (valueText != null)
                    valueText.text = sysCon.StarSysData.DilithiumStockpile.ToString();
            }
        }

        private void SetCostText(Dictionary<string, TextMeshProUGUI> tmps,
            string turnsKey, string li2Key, int turns, int dilithium)
        {
            if (tmps.TryGetValue(turnsKey, out var t)) t.text = turns.ToString();
            if (tmps.TryGetValue(li2Key, out var l)) l.text = dilithium.ToString();
        }

        private void SetShipCostText(Dictionary<string, TextMeshProUGUI> tmps,
            string turnsKey, string li2Key,
            ShipType shipType, TechLevel tech, CivEnum civ, int quality)
        {
            var stats = ShipStatCalculator.Calculate(shipType, tech, civ, quality);
            if (tmps.TryGetValue(turnsKey, out var t)) t.text = stats.BuildDuration.ToString();
            if (tmps.TryGetValue(li2Key, out var l)) l.text = stats.DilithiumCost.ToString();
        }

        private int GetFacilityTurns(StarSysController sysCon, StarSysFacilityType facilityType)
        {
            if (sysCon.StarSysBuildManager != null)
                return sysCon.StarSysBuildManager.GetBuildTimeDuration(facilityType);

            // Fallback: raw duration without tech-speed adjustment
            return facilityType switch
            {
                StarSysFacilityType.PowerPlanet => sysCon.StarSysData.PowerPlantData?.BuildDuration ?? 5,
                StarSysFacilityType.Factory => sysCon.StarSysData.FactoryData?.BuildDuration ?? 5,
                StarSysFacilityType.Shipyard => sysCon.StarSysData.ShipyardData?.BuildDuration ?? 8,
                StarSysFacilityType.ShieldGenerator => sysCon.StarSysData.ShieldGeneratorData?.BuildDuration ?? 6,
                StarSysFacilityType.ResearchCenter => sysCon.StarSysData.ResearchCenterData?.BuildDuration ?? 6,
                StarSysFacilityType.OrbitalBattery => sysCon.StarSysData.OrbitalBatteryData?.BuildDuration ?? 6,
                _ => 5,
            };
        }

        /// <summary>
        /// Grays out and blocks drag for any build item whose dilithium cost exceeds the system stockpile.
        /// Called after UpdateAvailableShipsByTechLevel so tech-locked items keep their existing state.
        /// </summary>
        private void ApplyDilithiumAvailability(StarSysController sysCon, GameObject buildUIInstance)
        {
            if (sysCon?.StarSysData?.CurrentCivController?.CivData == null) return;

            int stockpile = sysCon.StarSysData.DilithiumStockpile;
            CivEnum civ = sysCon.StarSysData.CurrentOwnerCivEnum;
            TechLevel tech = sysCon.StarSysData.CurrentCivController.CivData.CurrentTechLevel;
            int quality = sysCon.StarSysData.CurrentCivController.CivData.QualityScore;

            foreach (var drag in buildUIInstance.GetComponentsInChildren<ShipBuildDrag>(true))
            {
                int cost = ShipStatCalculator.Calculate(drag.ShipType, tech, civ, quality).DilithiumCost;
                if (cost > 0 && stockpile < cost)
                    SetBuildableUnavailable(drag.gameObject);
            }

            foreach (var drag in buildUIInstance.GetComponentsInChildren<FactoryBuildItemDrag>(true))
            {
                int cost = drag.FacilityType switch
                {
                    StarSysFacilityType.PowerPlanet => ShipStatCalculator.GetPowerPlantDilithiumCost(civ),
                    StarSysFacilityType.Factory => GetFactorySObyCivInt((int)civ)?.DilithiumCost ?? 0,
                    StarSysFacilityType.Shipyard => GetShipyardSObyCivInt((int)civ)?.DilithiumCost ?? 0,
                    StarSysFacilityType.ShieldGenerator => GetShieldGeneratorSObyCivInt((int)civ)?.DilithiumCost ?? 0,
                    StarSysFacilityType.OrbitalBattery => GetOrbitalBatterySObyCivInt((int)civ)?.DilithiumCost ?? 0,
                    StarSysFacilityType.ResearchCenter => GetResearchCenterSObyCivInt((int)civ)?.DilithiumCost ?? 0,
                    _ => 0
                };
                if (cost > 0 && stockpile < cost)
                    SetBuildableUnavailable(drag.gameObject);
            }
        }

        private static void SetBuildableUnavailable(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            cg.alpha = 0.4f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }
}


