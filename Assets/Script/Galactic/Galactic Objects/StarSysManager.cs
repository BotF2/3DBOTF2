// Ignore Spelling: shiptype Sys hvy BOTF
using BOTF3D.GamePlay;
using BOTF3D.UI;
using FischlWorks_FogWar;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.Core
{
    /// <summary>
    /// Instantiates the star system (a StarSysController and a StarSysData) using StarSysSO.
    /// Manages Star Systems, their initialization, facilities, and UI.
    /// </summary>
    public class StarSysManager : MonoBehaviour
    {
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
        [SerializeField]
        private Sprite unknowSystem;
        private int starSystemCounter = 0;
        private List<CivEnum> localPlayerCanSeeMyNameList = new List<CivEnum>();
        [SerializeField]
        public GameObject StarSysUI_ListContainer;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                // No DontDestroyOnLoad - dies with GalaxyScene

                Debug.Log("StarSysManager: Awake - Instance created");

                // Find critical references EARLY
                FindGalaxyReferences();
            }
        }

        private void Start()
        {
            Debug.Log("StarSysManager: Start called");

            // Double-check references
            if (galaxyCenter == null || galaxyCamera == null)
            {
                FindGalaxyReferences();
            }

            // Initialize Fog of War
            InitializeFogOfWar();

            Debug.Log("StarSysManager: Ready to create systems");
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

            // ✅ NEW: Find your StarSysUI_ListContainer
            if (StarSysUI_ListContainer == null)
            {
                var canvasGalaxy = GameObject.Find("CanvasGalaxy");
                if (canvasGalaxy != null)
                {
                    StarSysUI_ListContainer = FindInHierarchy(canvasGalaxy.transform, "StarSysUI_ListContainer");

                    if (StarSysUI_ListContainer != null)
                    {
                        Debug.Log($"StarSysManager: ✅ Found StarSysUI_ListContainer");
                    }
                    else
                    {
                        Debug.LogWarning("StarSysManager: ⚠️ StarSysUI_ListContainer not found - create it in CanvasGalaxy!");
                    }
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
                    shipBuildScript.BuildDuration = shipSO.BuildDuration;
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
        public void SysDataFromSO(List<CivSO> civSOList)
        {
            Debug.Log($"=== StarSysManager.SysDataFromSO: Creating systems for {civSOList.Count} civs ===");

            // Ensure we have required references
            if (galaxyCenter == null)
            {
                Debug.LogError("StarSysManager: galaxyCenter is NULL! Cannot create systems.");
                return;
            }

            StarSysData SysData = new StarSysData("null");
            List<StarSysData> starSysDatas = new List<StarSysData>();
            starSysDatas.Add(SysData);

            for (int i = 0; i < civSOList.Count; i++)
            {
                StarSysSO starSysSO = GetStarSObyInt(civSOList[i].CivInt);

                if (starSysSO == null)
                {
                    Debug.LogWarning($"  No StarSysSO found for civ {civSOList[i].CivShortName} (int={civSOList[i].CivInt})");
                    continue;
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
            Debug.Log($"InstantiateSystem: Creating {sysData.SysName} for {civSO.CivShortName}");
            if (MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType == GalaxyMapType.RANDOM)
            { // do something random with system and fleetData.position
            }
            else if (MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType == GalaxyMapType.RING)
            {
                // do something ring or whatever with system and fleetData.position
            }

            StarSysController starSysCon = Instantiate(sysPrefab, new Vector3(0, 0, 0),
            Quaternion.identity);

            StarSysBuildManager buildManager = new StarSysBuildManager(starSysCon);
            buildManager.RegisterStarSysController(starSysCon);
            starSysCon.StarSysData = sysData;
            starSysCon.gameObject.layer = 4; // water layer (also used by fog of war for obstacles with shows to line of sight
            starSysCon.transform.Translate(new Vector3(sysData.GetPosition().x,
                sysData.GetPosition().y, sysData.GetPosition().z));
            starSysCon.transform.SetParent(galaxyCenter.transform, true);
            starSysCon.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            starSysCon.GalaxyEventCamera = galaxyCamera;
            Transform fogObsticleTransform = starSysCon.transform.Find("FogObstacle");
            fogObsticleTransform.SetParent(galaxyCenter.transform, false);
            fogObsticleTransform.Translate(new Vector3(sysData.GetPosition().x, -55f, sysData.GetPosition().z));
            starSysCon.name = sysData.GetSysName();
            // ✅ Set Dilithium Capacity based on system type
            sysData.DilithiumCapacity = DetermineDilithiumCapacity(civSO, starSysSO);
            sysData.TotalSysPowerLoad = 0; // Will be updated as facilities are added
            sysData.TotalSysPowerOutput = 0;
            sysData.CurrentPowerPlantCount = 0; // Will be set when power plants are added
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
                //var sysThingy = fleetData
            }
            // starSysFields.SysDescription.text = sysData.Description;// null just now but available for a hover tooltip later      
            MapLineFixed ourDropLine = starSysCon.GetComponentInChildren<MapLineFixed>();
            ourDropLine.GetLineRenderer();

            // Drop line now shorter (star at -40, galaxy at -60)
            Vector3 galaxyPlanePoint = new Vector3(starSysCon.transform.position.x,
                        galaxyImage.transform.position.y, starSysCon.transform.position.z);
            Vector3[] points = { starSysCon.transform.position, galaxyPlanePoint };
            ourDropLine.SetUpLine(points);
            StarSysChildFields starSysField = starSysCon.GetComponent<StarSysChildFields>();
            SpriteRenderer srInsignia = starSysField.OwnerInsigniaGO.GetComponent<SpriteRenderer>();
            srInsignia.sprite = civSO.Insignia;
            if (!GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum))
            {
                srInsignia.sortingOrder = 0;
                srInsignia.enabled = false; // hide the insignia if not our system and no known systems yet
            }
            srInsignia.gameObject.transform.position =
                new Vector3(starSysCon.transform.position.x, galaxyPlanePoint.y + 1f, starSysCon.transform.position.z);
            srInsignia.gameObject.layer = 4; // water layer (also used by fog of war for obstacles with shows to line of sight

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

            Debug.Log($"  ✅ System created: {starSysCon.name}, total systems: {StarSysControllerList.Count}");

            if (GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum))
            {
                InstantiateStarSysUI(starSysCon); // ✅ Creates system UI panel with ship list
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
                FleetManager.Instance.BuildFirstFleetsNearSyst(starSysCon); // fleet for first ships as game loads, not for ships instantiated by working shipyard in system
                ShipManager.Instance.BuildShipInSystem(ShipType.Destroyer, starSysCon);
            }
            if (true) //(GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum)) 
            {
                // ✅ MODIFIED: Use Dilithium capacity to limit starting power plants
                int startingPowerPlants = DetermineStartingPowerPlants(civSO, sysData.DilithiumCapacity);

                sysData.PowerPlants = AddSystemFacilities(
                    startingPowerPlants,
                    PowerPlantPrefab,
                    (int)starSysCon.StarSysData.CurrentOwnerCivEnum,
                    1,
                    starSysCon);
                // ✅ Update count
                sysData.CurrentPowerPlantCount = sysData.PowerPlants.Count;
                sysData.Factories = AddSystemFacilities(starSysSO.Factories, FactoryPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                sysData.Shipyards = AddSystemFacilities(starSysSO.Shipyards, ShipyardPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                sysData.ShieldGenerators = AddSystemFacilities(starSysSO.ShieldGenerators, ShieldGeneratorPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                sysData.OrbitalBatteries = AddSystemFacilities(starSysSO.OrbitalBatteries, OrbitalBatteryPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                sysData.ResearchCenters = AddSystemFacilities(starSysSO.ResearchCenters, ResearchCenterPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                SetParentForFacilities(starSysCon.gameObject, sysData);

                // initialize/wire the system UI from StarSysData (new helper on StarSysUIElement)
                if (starSysCon.StarSysUIGameObject != null)
                {
                    var uiElement = starSysCon.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                    if (uiElement != null)
                    {
                        uiElement.InitializeFromStarSysData(sysData);
                    }
                }
            }
            if (GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum))
            {
                localPlayerTheme = ThemeManager.Instance.GetLocalPlayerTheme();
            }
        }
        /// <summary>
        /// Determine Dilithium capacity based on system type and owner
        /// </summary>
        private int DetermineDilithiumCapacity(CivSO civSO, StarSysSO starSysSO)
        {
            // ✅ Major race homeworlds
            if (civSO.Playable && starSysSO.IsHomeworld)
            {
                return 3; // Federation, Romulan, Klingon, etc. homeworlds
            }

            // ✅ Minor race systems
            if (!civSO.Playable && civSO.HasWarp)
            {
                // 70% get capacity 1-2, 30% get capacity 3
                float roll = UnityEngine.Random.value;
                if (roll < 0.40f) return 1;
                if (roll < 0.70f) return 2;
                return 3;
            }

            // ✅ Colonizable/Terraformable systems
            if (starSysSO.Habitable || starSysSO.Terraformable)
            {
                // Based on planet quality (you can expand this)
                float roll = UnityEngine.Random.value;
                if (roll < 0.50f) return 1;
                if (roll < 0.85f) return 2;
                return 3;
            }

            // ✅ Non-habitable systems
            return 0;
        }
        /// <summary>
        /// Determine starting power plants (always 1 for warp-capable, 0 otherwise)
        /// </summary>
        private int DetermineStartingPowerPlants(CivSO civSO, int dilithiumCapacity)
        {
            if (dilithiumCapacity == 0)
                return 0;

            if (civSO.HasWarp)
                return 1; // All warp-capable civs start with 1 power plant

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
                powerPlantData.PowerOutput = powerPlantSO.PowerOutput;
                powerPlantData.PowerPlantSprite = powerPlantSO.PowerPlantSprite;
                powerPlantData.Description = powerPlantSO.Description;
                sysController.StarSysData.PowerPlantData = powerPlantData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
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
                factoryData.FactorySprite = factorySO.FactorySprite;
                factoryData.Description = factorySO.Description;
                sysController.StarSysData.FactoryData = factoryData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
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
                syData.ShipyardSprite = sSO.ShipyardSprite;
                syData.Description = sSO.Description;
                sysController.StarSysData.ShipyardData = syData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
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
                sgData.ShieldGeneratorSprite = sgSO.ShieldGeneratorSprite;
                sgData.Description = sgSO.Description;
                sysController.StarSysData.ShieldGeneratorData = sgData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
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
                obData.OrbitalBatterySprite = obSO.OrbitalBatterySprite;
                obData.Description = obSO.Description;
                sysController.StarSysData.OrbitalBatteryData = obData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
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
                researchData.ResearchCenterSprite = rSO.ResearchCenterSprite;
                researchData.Description = rSO.Description;
                sysController.StarSysData.ResearchCenterData = researchData;

                for (int i = 0; i < numOf; i++)
                {
                    GameObject newFacilityGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    newFacilityGO.layer = 5;
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
            int powerOut = sysCon.StarSysData.PowerPlantData.PowerOutput;
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
            StarSysSO result = null;
            for (int i = 0; i < starSysSOList.Count; i++)
            {
                if (starSysSOList[i].StarSysInt == sysInt)
                {
                    result = starSysSOList[i];
                    break;
                }
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
        public void UpdateStarSystemOwner(CivEnum civCurrent, CivEnum civNew)
        {
            foreach (var sysCon in StarSysControllerList)
            {
                if (sysCon.StarSysData.GetFirstOwner() == civCurrent)
                    sysCon.StarSysData.CurrentOwnerCivEnum = civNew;
            }
        }

        //public void InstantiateSysUI(StarSysController sysController)
        //{
        //    var shipManager = ShipManager.Instance;
        //    if (sysController.StarSysData.CurrentOwnerCivEnum == GameController.Instance.GameData.LocalPlayerCivEnum)
        //    {
        //        if (sysController.StarSysUIGameObject == null)
        //        {
        //            GameObject thisStarSysUIGameObject = (GameObject)Instantiate(sysUIPrefab, new Vector3(0, 0, 0),
        //                Quaternion.identity);
        //            thisStarSysUIGameObject.layer = 5;
        //            sysController.StarSysUIGameObject = thisStarSysUIGameObject;
        //            sysController.StarSysUIGameObject.SetActive(true);

        //            // Find the UI container that will hold ship UI items (include inactive children)
        //            var shipContent = thisStarSysUIGameObject.GetComponentsInChildren<Transform>(true)
        //                                            .FirstOrDefault(t => t.name == "ShipContent");
        //            if (shipContent != null)
        //            {
        //                EnsureSystemShipUIs(sysController);
        //                sysController.StarSysData.ShipListUIParent = shipContent.gameObject;
        //            }
        //            else
        //            {
        //                Debug.LogWarning($"InstantiateSysUI: ShipContent not found in UI prefab for system {sysController.name}");
        //            }

        //            // existing code to wire other UI child references...
        //            var transforms = thisStarSysUIGameObject.transform.GetComponentsInChildren<Transform>();
        //            for (int j = 0; j < transforms.Length; j++)
        //            {
        //                if (transforms[j].gameObject.name == "ShipContent")
        //                {
        //                    sysController.StarSysData.ShipListUIParent = transforms[j].gameObject;
        //                    //var shipManager = ShipManager.Instance;
        //                    if (shipManager != null)
        //                    {
        //                        shipManager.ProcessPendingShipUIs();
        //                    }
        //                    return;
        //                }
        //            }
        //            thisStarSysUIGameObject.transform.SetParent(sysUIGOContentParent.transform, false);
        //        }
        //    }
        //    if (shipManager != null)
        //    {
        //        // Process any pending ship UIs (created earlier before parent existed)
        //        shipManager.ProcessPendingShipUIs();

        //        // Ensure each ship in the StarSysData has a UI item and that the UI is parented correctly
        //        EnsureSystemShipUIs(sysController);
        //    }
        //}
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
        public void InstantiateSysBuildListUI(StarSysController sysCon) // open the build queue UI
        {
            Debug.Log($"InstantiateSysBuildListUI: Opening for system '{sysCon.name}'");

            var existingBuildUI = GameObject.Find("SysBuildUIListPanel(Clone)");
            if (existingBuildUI != null)
            {
                Debug.Log("  Destroying previous build UI");
                Destroy(existingBuildUI);
            }

            GameObject sysBuildListInstance = Instantiate(sysBuildUIListPrefab, new Vector3(0, -70, 0), Quaternion.identity);
            sysBuildListInstance.layer = 5;

            // ✅ Set civ-specific images FIRST (before anything else)
            SetFacilityBuildImages(sysCon, sysBuildListInstance);
            SetShipBuildImages(sysCon, sysBuildListInstance);

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
                Debug.Log($"    Wired ship drag '{shipDrag.name}' to system '{sysCon.name}'");
            }

            Debug.Log($"InstantiateSysBuildListUI: Complete for '{sysCon.name}'");
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
                    var powerPlantSO = GetPowrPlantSObyCivEnum(sysCon.StarSysData.CurrentOwnerCivEnum);
                    if (powerPlantSO != null)
                        imageObPower.GetComponentInChildren<Image>().sprite = powerPlantSO.PowerPlantSprite;
                    imageObPower.transform.SetParent(powerPlantInventorySlot.transform, false);
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
                    var factorySO = GetFactorySObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    if (factorySO != null)
                        imageObFactory.GetComponentInChildren<Image>().sprite = factorySO.FactorySprite;
                    imageObFactory.transform.SetParent(factoryInventorySlot.transform, false);
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
                    var shipyardSO = GetShipyardSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    if (shipyardSO != null)
                        imageObShipyard.GetComponentInChildren<Image>().sprite = shipyardSO.ShipyardSprite;
                    imageObShipyard.transform.SetParent(shipyardInventorySlot.transform, false);
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
                    var shieldSO = GetShieldGeneratorSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    if (shieldSO != null)
                        imageObShield.GetComponentInChildren<Image>().sprite = shieldSO.ShieldGeneratorSprite;
                    imageObShield.transform.SetParent(shieldGenInventorySlot.transform, false);
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
                    var orbitalSO = GetOrbitalBatterySObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    if (orbitalSO != null)
                        imageObOB.GetComponentInChildren<Image>().sprite = orbitalSO.OrbitalBatterySprite;
                    imageObOB.transform.SetParent(orbitalBatteryInventorySlot.transform, false);
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
                    var researchSO = GetResearchCenterSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    if (researchSO != null)
                        imageObRC.GetComponentInChildren<Image>().sprite = researchSO.ResearchCenterSprite;
                    imageObRC.transform.SetParent(researchCenterInventory_slot.transform, false);
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
                    Transform[] transforms = starSysController.gameObject.GetComponentsInChildren<Transform>(true);
                    bool nameUpdated = false;
                    bool insigniaUpdated = false;

                    for (int i = 0; i < transforms.Length; i++)
                    {
                        GameObject ourGO = transforms[i].gameObject;

                        if (ourGO.name == "SysName")
                        {
                            ourGO.SetActive(true);
                            var textComponent = ourGO.GetComponent<TextMeshProUGUI>();
                            if (textComponent != null)
                            {
                                textComponent.text = starSysController.StarSysData.SysName;
                                nameUpdated = true;
                                Debug.Log($"  Revealed system name: {starSysController.StarSysData.SysName}");
                            }
                        }
                        else if (ourGO.name == "OwnerInsignia")
                        {
                            ourGO.SetActive(true);
                            var spriteRenderer = ourGO.GetComponent<SpriteRenderer>();
                            if (spriteRenderer != null)
                            {
                                spriteRenderer.enabled = true;
                                spriteRenderer.sortingOrder = 0;
                                insigniaUpdated = true;
                                Debug.Log($"  Revealed insignia for system: {starSysController.StarSysData.SysName}");
                            }
                        }

                        // Exit early if both updated
                        if (nameUpdated && insigniaUpdated)
                        {
                            break;
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
        /// Instantiates a star system UI and parents it to the scene's StarSysUI_ListContainer
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

            // ✅ CRITICAL: Parent to StarSysUI_ListContainer (home storage)
            if (StarSysUI_ListContainer != null)
            {
                newUI.transform.SetParent(StarSysUI_ListContainer.transform, false);
                Debug.Log($"  ✅ Parented to StarSysUI_ListContainer");
            }
            else
            {
                Debug.LogWarning("  ⚠️ StarSysUI_ListContainer is null! Trying to find it...");
                FindGalaxyReferences();

                if (StarSysUI_ListContainer != null)
                {
                    newUI.transform.SetParent(StarSysUI_ListContainer.transform, false);
                }
                else
                {
                    Debug.LogError("  ❌ Still can't find StarSysUI_ListContainer! UI will be orphaned!");
                }
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

            // Find all buildable items
            FactoryBuildItemDrag[] buildableItems = buildUIInstance.GetComponentsInChildren<FactoryBuildItemDrag>(true);

            foreach (var item in buildableItems)
            {
                Image itemImage = item.GetComponent<Image>();
                if (itemImage == null) continue;

                switch (item.name)
                {
                    case "ItemPowerPlant":
                        var powerPlantSO = GetPowrPlantSObyCivEnum(localCiv);
                        if (powerPlantSO != null && powerPlantSO.PowerPlantSprite != null)
                        {
                            itemImage.sprite = powerPlantSO.PowerPlantSprite;
                            Debug.Log($"  ✅ Set PowerPlant sprite for {localCiv}");
                        }
                        break;

                    case "ItemFactory":
                        var factorySO = GetFactorySObyCivInt((int)localCiv);
                        if (factorySO != null && factorySO.FactorySprite != null)
                        {
                            itemImage.sprite = factorySO.FactorySprite;
                            Debug.Log($"  ✅ Set Factory sprite for {localCiv}");
                        }
                        break;

                    case "ItemShipyard":
                        var shipyardSO = GetShipyardSObyCivInt((int)localCiv);
                        if (shipyardSO != null && shipyardSO.ShipyardSprite != null)
                        {
                            itemImage.sprite = shipyardSO.ShipyardSprite;
                            Debug.Log($"  ✅ Set Shipyard sprite for {localCiv}");
                        }
                        break;

                    case "ItemShieldGenerator":
                        var shieldSO = GetShieldGeneratorSObyCivInt((int)localCiv);
                        if (shieldSO != null && shieldSO.ShieldGeneratorSprite != null)
                        {
                            itemImage.sprite = shieldSO.ShieldGeneratorSprite;
                            Debug.Log($"  ✅ Set ShieldGenerator sprite for {localCiv}");
                        }
                        break;

                    case "ItemOrbitalBattery":
                        var orbitalSO = GetOrbitalBatterySObyCivInt((int)localCiv);
                        if (orbitalSO != null && orbitalSO.OrbitalBatterySprite != null)
                        {
                            itemImage.sprite = orbitalSO.OrbitalBatterySprite;
                            Debug.Log($"  ✅ Set OrbitalBattery sprite for {localCiv}");
                        }
                        break;

                    case "ItemResearchCenter":
                        var researchSO = GetResearchCenterSObyCivInt((int)localCiv);
                        if (researchSO != null && researchSO.ResearchCenterSprite != null)
                        {
                            itemImage.sprite = researchSO.ResearchCenterSprite;
                            Debug.Log($"  ✅ Set ResearchCenter sprite for {localCiv}");
                        }
                        break;
                }
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
            TechLevel techLevel = GameController.Instance.GameData.StartingTechLevel;

            Debug.Log($"SetShipBuildImages: Civ={localCiv}, TechLevel={techLevel}");

            // ✅ NEW: Get ships from civ-specific list, filtered by tech
            List<ShipSO> availableShips = ShipManager.Instance.GetShipSOsForCivAndTech(localCiv, techLevel);

            if (availableShips.Count == 0)
            {
                Debug.LogWarning($"  ⚠️ No ships found for {localCiv} at {techLevel}!");
                return;
            }

            Debug.Log($"  Found {availableShips.Count} ships: {string.Join(", ", availableShips.Select(s => s.ShipType))}");

            // Find all ship drag items in build UI
            ShipBuildDrag[] shipDragItems = buildUIInstance.GetComponentsInChildren<ShipBuildDrag>(true);

            foreach (var dragItem in shipDragItems)
            {
                // ✅ Find matching ShipSO by type
                ShipSO shipSO = availableShips.FirstOrDefault(s => s.ShipType == dragItem.ShipType);

                if (shipSO != null)
                {
                    // Set image and data
                    Image itemImage = dragItem.GetComponent<Image>();
                    if (itemImage != null && shipSO.shipSprite != null)
                    {
                        itemImage.sprite = shipSO.shipSprite;
                        dragItem.ShipSprite = shipSO.shipSprite;
                        dragItem.BuildDuration = shipSO.BuildDuration;
                        dragItem.ShipType = shipSO.ShipType;

                        Debug.Log($"  ✅ Set {shipSO.ShipType} sprite for {localCiv}");
                    }

                    // ✅ Show this ship type
                    dragItem.gameObject.SetActive(true);
                }
                else
                {
                    // ✅ Hide ships not available at this tech level
                    dragItem.gameObject.SetActive(false);
                    Debug.Log($"  ⚠️ {dragItem.ShipType} not available at {techLevel} - hiding");
                }
            }
        }
    }
}

