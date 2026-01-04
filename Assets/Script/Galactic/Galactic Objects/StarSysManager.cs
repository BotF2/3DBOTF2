// Ignore Spelling: shiptype Sys hvy

using FischlWorks_FogWar;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Core
{
    /// <summary>
    /// Instantiates the star system (a StarSysController and a StarSysData) using StarSysSO.
    /// Manages Star Systems, their initialization, facilities, and UI.
    /// </summary>
    public class StarSysManager : MonoBehaviour
    {

        public static StarSysManager Instance;
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
        private GameObject shipBuildSliderPrefab;

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
        [SerializeField]
        private GameObject galaxyCenter;
        private Camera galaxyEventCamera;
        private int starSystemCounter = 0;
        private List<CivEnum> localPlayerCanSeeMyNameList = new List<CivEnum>();
        internal GameObject sysShipUIGOContentParent;

        //private int systemCount = -1; // Used only in testing multiple systems in Federation
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        public void Start()
        {
            galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
        }
        public void SetShipBuildPrefabs(CivEnum localCiv)
        {

            TechLevel techLevel = GameController.Instance.GameData.StartingTechLevel;// to do GameDate to know staring tech level
            List<ShipSO> shipSOList = new List<ShipSO>();
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    shipSOList = ShipManager.Instance.ShipSOListTech0.Where(x => x.CivEnum == localCiv && x.TechLevel == TechLevel.EARLY).ToList();
                    break;
                case TechLevel.DEVELOPED:
                    shipSOList = ShipManager.Instance.ShipSOListTech1.Where(x => x.CivEnum == localCiv && x.TechLevel == TechLevel.DEVELOPED).ToList();
                    break;
                case TechLevel.ADVANCED:
                    shipSOList = ShipManager.Instance.ShipSOListTech2.Where(x => x.CivEnum == localCiv && x.TechLevel == TechLevel.ADVANCED).ToList();
                    break;
                case TechLevel.SUPREME:
                    shipSOList = ShipManager.Instance.ShipSOListTech3.Where(x => x.CivEnum == localCiv && x.TechLevel == TechLevel.SUPREME).ToList();
                    break;
                default:
                    break;
            }
            for (int i = 0; i < shipSOList.Count; i++)
            {
                if (shipSOList[i].ShipType == ShipType.Scout)
                {
                    var shipBuildScript = scoutBluePrintPrefab.GetComponent<ShipBuildDrag>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    scoutBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.Destroyer)
                {
                    var shipBuildScript = destroyerBluePrintPrefab.GetComponent<ShipBuildDrag>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    destroyerBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.Cruiser)
                {
                    var shipBuildScript = cruiserBluePrintPrefab.GetComponent<ShipBuildDrag>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    cruiserBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.LtCruiser)
                {
                    var shipBuildScript = ltCruiserBluePrintPrefab.GetComponent<ShipBuildDrag>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    ltCruiserBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.HvyCruiser)
                {
                    var shipBuildScript = hvyCruiserBluePrintPrefab.GetComponent<ShipBuildDrag>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    hvyCruiserBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.Transport)
                {
                    var shipBuildScript = transportBluePrintPrefab.GetComponent<ShipBuildDrag>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    transportBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
            }
        }
        public void SysDataFromSO(List<CivSO> civSOList)
        {
            StarSysData SysData = new StarSysData("null");
            List<StarSysData> starSysDatas = new List<StarSysData>();
            starSysDatas.Add(SysData);
            for (int i = 0; i < civSOList.Count; i++)
            {
                StarSysSO starSysSO = GetStarSObyInt(civSOList[i].CivInt);
                SysData = new StarSysData(starSysSO);

                SysData.CurrentOwnerCivEnum = starSysSO.FirstOwner;
                SysData.SystemType = starSysSO.StarType;
                SysData.StarSprit = starSysSO.StarSprit;
                SysData.Description = starSysSO.Description;
                InstantiateSystem(SysData, civSOList[i], starSysSO);
                //if (civSOList[i].HasWarp)
                //    FleetManager.Instance.FleetDataFromSO(, false);
                //if (SysData.CurrentCivController != null)
                //    starSysDatas.Add(SysData);
            }
            starSysDatas.Remove(starSysDatas[0]); // pull out the null
        }
        public StarSysController InstantiateEmptyStarSysController()
        {
            StarSysController starSysCon = Instantiate(sysPrefab, new Vector3(0, 0, 0),
              Quaternion.identity);
            return starSysCon;
        }
        public void InstantiateSystem(StarSysData sysData, CivSO civSO, StarSysSO starSysSO)
        {

            if (MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType == GalaxyMapType.RANDOM)
            { // do something random with system and fleetData.position
            }
            else if (MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType == GalaxyMapType.RING)
            {
                // ?do something in a ring with system and fleetData.position
            }
            else
            {
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

                Transform fogObsticleTransform = starSysCon.transform.Find("FogObstacle");
                fogObsticleTransform.SetParent(galaxyCenter.transform, false);
                fogObsticleTransform.Translate(new Vector3(sysData.GetPosition().x, -55f, sysData.GetPosition().z));
                starSysCon.name = sysData.GetSysName();

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

                // Ensure the system UI is instantiated early so ShipListUIParent is available
                // before any code that creates ship UI items or builds ships/fleets.
                InstantiateSysUIGameObject(starSysCon);

                List<StarSysController> listStarSysCon = new List<StarSysController> { starSysCon };
                CivManager.Instance.AddSystemToOwnSystemListAndHomeSys(listStarSysCon);
                //var canvases = starSysCon.GetComponentsInChildren<Canvas>();
                starSystemCounter++;
                if (starSystemCounter == CivManager.Instance.CivControllersInGame.Count)
                {
                    csFogWar.Instance.RunFogOfWar(); // star systems are in place so time to scan for the fog
                                                     // instantiate and wire the system UI now (so ShipListUIParent is available
                }
                if (civSO.HasWarp)
                {
                    FleetManager.Instance.BuildFleetsNearSyst(starSysCon); // fleet for first ships as game loads, not for ships instantiated by working shipyard in system
                    ShipManager.Instance.BuildShipInSystem(ShipType.Destroyer, starSysCon);
                }
                if (true) //(GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum)) 
                {
                    sysData.PowerPlants = AddSystemFacilities(starSysSO.PowerStations, PowerPlantPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                    sysData.Factories = AddSystemFacilities(starSysSO.Factories, FactoryPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                    sysData.Shipyards = AddSystemFacilities(starSysSO.Shipyards, ShipyardPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                    sysData.ShieldGenerators = AddSystemFacilities(starSysSO.ShieldGenerators, ShieldGeneratorPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                    sysData.OrbitalBatteries = AddSystemFacilities(starSysSO.OrbitalBatteries, OrbitalBatteryPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                    sysData.ResearchCenters = AddSystemFacilities(starSysSO.ResearchCenters, ResearchCenterPrefab, (int)starSysCon.StarSysData.CurrentOwnerCivEnum, 1, starSysCon);
                    SetParentForFacilities(starSysCon.gameObject, sysData);

                    // initialize/star-wire the system UI from StarSysData (new helper on StarSysUIElement)
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

            GameObject[] allGO = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            //clean up game object not in use, ToDo: find and remove the creation of these game object at the source
            foreach (GameObject obj in allGO)
            {
                if (obj.name == "New Game Object")
                    Destroy(obj);
            }
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
                    sysController.StarSysData.TotalSysPowerLoad += factoryData.PowerLoad;
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
                    sysController.StarSysData.TotalSysPowerLoad += syData.PowerLoad;
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
                    sysController.StarSysData.TotalSysPowerLoad += sgData.PowerLoad;
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
                    sysController.StarSysData.TotalSysPowerLoad += obData.PowerLoad;
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
                    sysController.StarSysData.TotalSysPowerLoad += researchData.PowerLoad;
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

        public void InstantiateSysUIGameObject(StarSysController sysController)
        {
            if (sysController.StarSysData.CurrentOwnerCivEnum == GameController.Instance.GameData.LocalPlayerCivEnum)
            {
                if (sysController.StarSysUIGameObject == null)
                {
                    GameObject thisStarSysUIGameObject = (GameObject)Instantiate(sysUIPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    thisStarSysUIGameObject.layer = 5;
                    sysController.StarSysUIGameObject = thisStarSysUIGameObject;
                    sysController.StarSysUIGameObject.SetActive(true);

                    // Find the UI container that will hold ship UI items (include inactive children)
                    var shipContent = thisStarSysUIGameObject.GetComponentsInChildren<Transform>(true)
                                                    .FirstOrDefault(t => t.name == "ShipContent");
                    if (shipContent != null)
                    {
                        sysController.StarSysData.ShipListUIParent = shipContent.gameObject;
                    }
                    else
                    {
                        Debug.LogWarning($"InstantiateSysUIGameObject: ShipContent not found in UI prefab for system {sysController.name}");
                    }

                    // existing code to wire other UI child references...
                    var transforms = thisStarSysUIGameObject.transform.GetComponentsInChildren<Transform>();
                    for (int j = 0; j < transforms.Length; j++)
                    {
                        if (transforms[j].gameObject.name == "ShipContent")
                        {
                            sysController.StarSysData.ShipListUIParent = transforms[j].gameObject;
                            var shipManager = ShipManager.Instance;
                            if (shipManager != null)
                            {
                                shipManager.ProcessPendingShipUIs();
                            }
                            return;
                        }
                    }
                    thisStarSysUIGameObject.transform.SetParent(sysUIGOContentParent.transform, false);
                }
            }
        }

        public void InstantiateSysBuildListUI(StarSysController sysCon) // open the build queue UI
        {
            GameObject sysBuildListInstance = (GameObject)Instantiate(sysBuildUIListPrefab, new Vector3(0, -70, 0),
                Quaternion.identity);
            sysBuildListInstance.layer = 5; // UI layer

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

            // Parent under canvas
            sysBuildListInstance.transform.SetParent(canvasBuildList.transform, false);

            // set StarSysController reference on buildable items
            FactoryBuildItemDrag[] buildable = sysBuildListInstance.GetComponentsInChildren<FactoryBuildItemDrag>();
            for (int m = 0; m < buildable.Length; m++)
            {
                buildable[m].StarSysController = sysCon;
                if (buildable[m].name == "ItemPowerPlant") buildable[m].FacilityType = StarSysFacilityType.PowerPlanet;
                else if (buildable[m].name == "ItemFactory") buildable[m].FacilityType = StarSysFacilityType.Factory;
                else if (buildable[m].name == "ItemShipyard") buildable[m].FacilityType = StarSysFacilityType.Shipyard;
                else if (buildable[m].name == "ItemShieldGenerator") buildable[m].FacilityType = StarSysFacilityType.ShieldGenerator;
                else if (buildable[m].name == "ItemOrbitalBattery") buildable[m].FacilityType = StarSysFacilityType.OrbitalBattery;
                else if (buildable[m].name == "ItemResearchCenter") buildable[m].FacilityType = StarSysFacilityType.ResearchCenter;
            }

            // Prefer the explicit prefab helper component
            var buildUI = sysBuildListInstance.GetComponent<BuildUIFields>();
            if (buildUI != null)
            {
                // text
                if (buildUI.systemNameTMP != null)
                    buildUI.systemNameTMP.text = sysCon.StarSysData.SysName;

                // grid layouts -> assign to controller and refresh its queues
                if (buildUI.queueHoldingBuildables != null)
                {
                    sysCon.BuildListGridLayoutGroup = buildUI.queueHoldingBuildables;
                    sysCon.GridFactoryQueueUpdate();
                }
                if (buildUI.queueHoldingBuildableShips != null)
                {
                    sysCon.ShipListGridLayoutGroup = buildUI.queueHoldingBuildableShips;
                    sysCon.GridShipQueueUpdate();
                }

                // sliders -> menu controller
                if (buildUI.factoryBuildProgress != null)
                {
                    StarSysMenuUIController.Instance.SliderBuildProgress = buildUI.factoryBuildProgress;
                    StarSysMenuUIController.Instance.SliderBuildProgress.value = 0f;
                }
                if (buildUI.shipBuildProgress != null)
                {
                    StarSysMenuUIController.Instance.ShipSliderBuildProgress = buildUI.shipBuildProgress;
                    StarSysMenuUIController.Instance.ShipSliderBuildProgress.value = 0f;
                }

                // inventory slot parents for later use
                powerPlantInventorySlot =
                    buildUI.powerPlantInventorySlot != null
                        ? buildUI.powerPlantInventorySlot
                        : powerPlantInventorySlot;
                factoryInventorySlot = buildUI.factoryInventorySlot != null ? buildUI.factoryInventorySlot : factoryInventorySlot;
                shipyardInventorySlot = buildUI.shipyardInventorySlot != null ? buildUI.shipyardInventorySlot : shipyardInventorySlot;
                shieldGenInventorySlot = buildUI.shieldGenInventorySlot != null ? buildUI.shieldGenInventorySlot : shieldGenInventorySlot;
                orbitalBatteryInventorySlot = buildUI.orbitalBatteryInventorySlot != null ? buildUI.orbitalBatteryInventorySlot : orbitalBatteryInventorySlot;
                researchCenterInventory_slot = buildUI.researchCenterInventorySlot != null ? researchCenterInventory_slot : researchCenterInventory_slot;
                scoutInventorySlot = buildUI.scoutInventorySlot != null ? buildUI.scoutInventorySlot : scoutInventorySlot;
                destroyerInventorySlot = buildUI.destroyerInventorySlot != null ? buildUI.destroyerInventorySlot : destroyerInventorySlot;
                cruiserInventorySlot = buildUI.cruiserInventorySlot != null ? buildUI.cruiserInventorySlot : cruiserInventorySlot;
                ltCruiserInventorySlot = buildUI.ltCruiserInventorySlot != null ? buildUI.ltCruiserInventorySlot : ltCruiserInventorySlot;
                hvyCruiserInventorySlot = buildUI.hvyCruiserInventorySlot != null ? buildUI.hvyCruiserInventorySlot : hvyCruiserInventorySlot;
                transportInventorySlot = buildUI.transportInventorySlot != null ? buildUI.transportInventorySlot : transportInventorySlot;
                var sysUIElement = sysCon.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                // populate images from StarSysData into known image fields (if present)
                if (sysUIElement.powerUnitImage != null && sysCon.StarSysData.PowerPlantData != null)
                    sysUIElement.powerUnitImage.sprite = sysCon.StarSysData.PowerPlantData.PowerPlantSprite;
                if (sysUIElement.factoryImage != null && sysCon.StarSysData.FactoryData != null)
                    sysUIElement.factoryImage.sprite = sysCon.StarSysData.FactoryData.FactorySprite;
                if (sysUIElement.shipyardImage != null && sysCon.StarSysData.ShipyardData != null)
                    sysUIElement.shipyardImage.sprite = sysCon.StarSysData.ShipyardData.ShipyardSprite;
                if (sysUIElement.shieldPlantImage != null && sysCon.StarSysData.ShieldGeneratorData != null)
                    sysUIElement.shieldPlantImage.sprite = sysCon.StarSysData.ShieldGeneratorData.ShieldGeneratorSprite;
                if (sysUIElement.orbitalBatteriesImage != null && sysCon.StarSysData.OrbitalBatteryData != null)
                    sysUIElement.orbitalBatteriesImage.sprite = sysCon.StarSysData.OrbitalBatteryData.OrbitalBatterySprite;
                if (sysUIElement.researchImage != null && sysCon.StarSysData.ResearchCenterData != null)
                    sysUIElement.researchImage.sprite = sysCon.StarSysData.ResearchCenterData.ResearchCenterSprite;

                // wire action buttons (if present)
                if (sysUIElement.buildButton != null)
                {
                    sysUIElement.buildButton.onClick.RemoveAllListeners();
                    sysUIElement.buildButton.onClick.AddListener(() => sysCon.BuildClick(sysCon));
                }
                if (sysUIElement.shipButton != null)
                {
                    sysUIElement.shipButton.onClick.RemoveAllListeners();
                    sysUIElement.shipButton.onClick.AddListener(() => sysCon.ShipClick(sysCon));
                }

                // wire facility on/off buttons to StarSysController's handlers if present
                if (sysUIElement.factoryButtonOn != null)
                {
                    sysUIElement.factoryButtonOn.onClick.RemoveAllListeners();
                    sysUIElement.factoryButtonOn.onClick.AddListener(() => sysCon.FactoryButtonOnClicked(sysCon));
                }
                if (sysUIElement.factoryButtonOff != null)
                {
                    sysUIElement.factoryButtonOff.onClick.RemoveAllListeners();
                    sysUIElement.factoryButtonOff.onClick.AddListener(() => sysCon.FactoryButtonOffClicked(sysCon));
                }
                if (sysUIElement.yardButtonOn != null)
                {
                    sysUIElement.yardButtonOn.onClick.RemoveAllListeners();
                    sysUIElement.yardButtonOn.onClick.AddListener(() => sysCon.YardButtonOnClicked(sysCon));
                }
                if (sysUIElement.yardButtonOff != null)
                {
                    sysUIElement.yardButtonOff.onClick.RemoveAllListeners();
                    sysUIElement.yardButtonOff.onClick.AddListener(() => sysCon.YardButtonOffClicked(sysCon));
                }
                if (sysUIElement.shieldButtonOn != null)
                {
                    sysUIElement.shieldButtonOn.onClick.RemoveAllListeners();
                    sysUIElement.shieldButtonOn.onClick.AddListener(() => sysCon.ShieldButtonOnClicked(sysCon));
                }
                if (sysUIElement.shieldButtonOff != null)
                {
                    sysUIElement.shieldButtonOff.onClick.RemoveAllListeners();
                    sysUIElement.shieldButtonOff.onClick.AddListener(() => sysCon.ShieldButtonOffClicked(sysCon));
                }
                if (sysUIElement.oBButtonOn != null)
                {
                    sysUIElement.oBButtonOn.onClick.RemoveAllListeners();
                    sysUIElement.oBButtonOn.onClick.AddListener(() => sysCon.OBButtonOnClicked(sysCon));
                }
                if (sysUIElement.oBButtonOff != null)
                {
                    sysUIElement.oBButtonOff.onClick.RemoveAllListeners();
                    sysUIElement.oBButtonOff.onClick.AddListener(() => sysCon.OBButtonOffClicked(sysCon));
                }
                if (sysUIElement.researchButtonOn != null)
                {
                    sysUIElement.researchButtonOn.onClick.RemoveAllListeners();
                    sysUIElement.researchButtonOn.onClick.AddListener(() => sysCon.ResearchButtonOnClicked(sysCon));
                }
                if (sysUIElement.researchButtonOff != null)
                {
                    sysUIElement.researchButtonOff.onClick.RemoveAllListeners();
                    sysUIElement.researchButtonOff.onClick.AddListener(() => sysCon.ResearchButtonOffClicked(sysCon));
                }

                // wire close buttons array
                if (buildUI.closeButtons != null)
                {
                    foreach (var btn in buildUI.closeButtons)
                    {
                        if (btn == null) continue;
                        btn.gameObject.SetActive(true);
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => StarSysMenuUIController.Instance.CloseBuildingQueues());
                    }
                }

                // ensure inventory slot images inside assigned parents are correct
                if (powerPlantInventorySlot != null && sysCon.StarSysData.PowerPlantData != null)
                {
                    foreach (var img in powerPlantInventorySlot.GetComponentsInChildren<Image>(true))
                        if (img.name == "ItemPowerPlant" || img.name == "ImagePowerBackground")
                            img.sprite = sysCon.StarSysData.PowerPlantData.PowerPlantSprite;
                }

                // ship inventory images
                if (scoutInventorySlot != null && scoutBluePrintPrefab != null)
                {
                    foreach (var img in scoutInventorySlot.GetComponentsInChildren<Image>(true))
                        if (img.name == "ItemScout" || img.name == "ImageScoutBackground")
                            img.sprite = scoutBluePrintPrefab.GetComponent<ShipBuildDrag>().ShipSprite;
                }

                //return;
            }

            // Fallback: existing legacy traversal (keeps compatibility for prefabs missing BuildUISliders)
            TextMeshProUGUI[] theTextItems = sysBuildListInstance.GetComponentsInChildren<TextMeshProUGUI>();
            for (int j = 0; j < theTextItems.Length; j++)
            {
                theTextItems[j].enabled = true;
                if (theTextItems[j].name == "SystemNameTMP")
                {
                    theTextItems[j].text = sysCon.StarSysData.SysName;
                    break;
                }
            }
            GridLayoutGroup[] theGrids = sysBuildListInstance.GetComponentsInChildren<GridLayoutGroup>();
            for (int k = 0; k < theGrids.Length; k++)
            {
                theGrids[k].enabled = true;
                if (theGrids[k].name == "QueueHoldingBuildables")
                {
                    sysCon.BuildListGridLayoutGroup = theGrids[k];
                    sysCon.GridFactoryQueueUpdate();
                }
                else if (theGrids[k].name == "QueueHoldingBuildableShips")
                {
                    sysCon.ShipListGridLayoutGroup = theGrids[k];
                    sysCon.GridShipQueueUpdate();
                    break;
                }
            }

            // Original per-slot traversal preserved below (unchanged)
            Transform[] theSlots = sysBuildListInstance.GetComponentsInChildren<Transform>();
            for (int l = 0; (l < theSlots.Length); l++)
            {
                theSlots[l].gameObject.SetActive(true);
                switch (theSlots[l].gameObject.name)
                {
                    case "ItemSlotPower":
                        {
                            powerPlantInventorySlot = theSlots[l].gameObject;
                            Image[] itemPowerPlantImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemPowerPlantImage.Length; i++)
                            {
                                if (itemPowerPlantImage[i].name == "ItemPowerPlant" || itemPowerPlantImage[i].name == "ImagePowerBackground")
                                {
                                    itemPowerPlantImage[i].sprite = sysCon.StarSysData.PowerPlantData.PowerPlantSprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotFactory":
                        {
                            factoryInventorySlot = theSlots[l].gameObject;
                            Image[] itemFactoryImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemFactoryImage.Length; i++)
                            {
                                if (itemFactoryImage[i].name == "ItemFactory" || itemFactoryImage[i].name == "ImageFactoryBackground")
                                {
                                    itemFactoryImage[i].sprite = sysCon.StarSysData.FactoryData.FactorySprite;
                                }
                            }
                            break;

                        }
                    case "ItemSlotShipyard":
                        {
                            shipyardInventorySlot = theSlots[l].gameObject;
                            Image[] itemShipyardImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemShipyardImage.Length; i++)
                            {
                                if (itemShipyardImage[i].name == "ItemShipyard" || itemShipyardImage[i].name == "ImageShipyardBackground")
                                {
                                    itemShipyardImage[i].sprite = sysCon.StarSysData.ShipyardData.ShipyardSprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotShields":
                        {
                            shieldGenInventorySlot = theSlots[l].gameObject;
                            Image[] itemShieldGenImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemShieldGenImage.Length; i++)
                            {
                                if (itemShieldGenImage[i].name == "ItemShieldGenerator" || itemShieldGenImage[i].name == "ImageShieldBackground")
                                {
                                    itemShieldGenImage[i].sprite = sysCon.StarSysData.ShieldGeneratorData.ShieldGeneratorSprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotOrbitalBattery":
                        {
                            orbitalBatteryInventorySlot = theSlots[l].gameObject;
                            Image[] itemOBImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemOBImage.Length; i++)
                            {
                                if (itemOBImage[i].name == "ItemOrbitalBattery" || itemOBImage[i].name == "ImageOrbitalBatteryBackground")
                                {
                                    itemOBImage[i].sprite = sysCon.StarSysData.OrbitalBatteryData.OrbitalBatterySprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotResearchCnt":
                        {
                            researchCenterInventory_slot = theSlots[l].gameObject;
                            Image[] itemResearchCenterImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemResearchCenterImage.Length; i++)
                            {
                                if (itemResearchCenterImage[i].name == "ItemResearchCenter" || itemResearchCenterImage[i].name == "ImageResearchBackground")
                                {
                                    itemResearchCenterImage[i].sprite = sysCon.StarSysData.ResearchCenterData.ResearchCenterSprite;
                                }
                            }
                            break;
                        }
                    case "FactoryProgressBar":
                        {
                            StarSysMenuUIController.Instance.SliderBuildProgress = theSlots[l].gameObject.GetComponent<Slider>();
                            StarSysMenuUIController.Instance.gameObject.transform.SetParent(theSlots[l]);
                            break;
                        }
                    case "Scout (TMP)":
                        {
                            // always available
                            theSlots[l].gameObject.SetActive(true);
                            break;
                        }
                    case "Destroyer (TMP)":
                        {
                            // always available
                            theSlots[l].gameObject.SetActive(true);
                            break;
                        }
                    case "Transport (TMP)":
                        {
                            // always available
                            theSlots[l].gameObject.SetActive(true);
                            break;
                        }
                    case "Cruiser (TMP)":
                        {
                            if (sysCon.StarSysData.CurrentCivController.CivData.TechLevel == TechLevel.EARLY ||
                                sysCon.StarSysData.CurrentCivController.CivData.TechLevel == TechLevel.SUPREME)
                            {
                                theSlots[l].gameObject.SetActive(false);
                                break;
                            }
                            else theSlots[l].gameObject.SetActive(true);
                            break;
                        }
                    case "Lt Cruiser (TMP)":
                    case "Hv Cruiser (TMP)":
                        {
                            if (sysCon.StarSysData.CurrentCivController.CivData.TechLevel != TechLevel.SUPREME)
                            {
                                theSlots[l].gameObject.SetActive(false);
                                break;
                            }
                            else theSlots[l].gameObject.SetActive(true);
                            break;
                        }
                    case "ItemSlotScout":
                        {
                            string localPlayer = GameController.Instance.GameData.LocalPlayerCivEnum.ToString();
                            scoutInventorySlot = theSlots[l].gameObject;
                            Image[] itemScoutImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemScoutImage.Length; i++)
                            {
                                if (itemScoutImage[i].name == "ItemScout" || itemScoutImage[i].name == "ImageScoutBackground")
                                {
                                    itemScoutImage[i].sprite = scoutBluePrintPrefab.GetComponent<ShipBuildDrag>().ShipSprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotDestroyer":
                        {
                            string localPlayer = GameController.Instance.GameData.LocalPlayerCivEnum.ToString();
                            destroyerInventorySlot = theSlots[l].gameObject;
                            Image[] itemDestroyerImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemDestroyerImage.Length; i++)
                            {
                                if (itemDestroyerImage[i].name == "ItemDestroyer" || itemDestroyerImage[i].name == "ImageDestroyerBackground")
                                {
                                    itemDestroyerImage[i].sprite = destroyerBluePrintPrefab.GetComponent<ShipBuildDrag>().ShipSprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotCruiser":
                        {
                            if (sysCon.StarSysData.CurrentCivController.CivData.TechLevel == TechLevel.EARLY ||
                                sysCon.StarSysData.CurrentCivController.CivData.TechLevel == TechLevel.SUPREME)
                            {
                                theSlots[l].gameObject.SetActive(false);
                                break;
                            }
                            else theSlots[l].gameObject.SetActive(true);
                            string localPlayer = GameController.Instance.GameData.LocalPlayerCivEnum.ToString();
                            cruiserInventorySlot = theSlots[l].gameObject;
                            Image[] itemCruiserImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemCruiserImage.Length; i++)
                            {
                                if (itemCruiserImage[i].name == "ItemCruiser" || itemCruiserImage[i].name == "ImageCruiserBackground")
                                {
                                    itemCruiserImage[i].sprite = cruiserBluePrintPrefab.GetComponent<ShipBuildDrag>().ShipSprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotLtCruiser":
                        {
                            if (sysCon.StarSysData.CurrentCivController.CivData.TechLevel != TechLevel.SUPREME)
                            {
                                theSlots[l].gameObject.SetActive(false);
                                break;
                            }
                            else theSlots[l].gameObject.SetActive(true);
                            string localPlayer = GameController.Instance.GameData.LocalPlayerCivEnum.ToString();
                            ltCruiserInventorySlot = theSlots[l].gameObject;
                            Image[] itemCruiserImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemCruiserImage.Length; i++)
                            {
                                if (itemCruiserImage[i].name == "ItemLtCruiser" || itemCruiserImage[i].name == "ImageLtCruiserBackground")
                                {
                                    itemCruiserImage[i].sprite = ltCruiserBluePrintPrefab.GetComponent<ShipBuildDrag>().ShipSprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotHvyCruiser":
                        {
                            if (sysCon.StarSysData.CurrentCivController.CivData.TechLevel != TechLevel.SUPREME)
                            {
                                theSlots[l].gameObject.SetActive(false);
                                break;
                            }
                            else theSlots[l].gameObject.SetActive(true);
                            hvyCruiserInventorySlot = theSlots[l].gameObject;
                            Image[] itemCruiserImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemCruiserImage.Length; i++)
                            {
                                if (itemCruiserImage[i].name == "ItemHvyCruiser" || itemCruiserImage[i].name == "ImageHvyCruiserBackground")
                                {
                                    itemCruiserImage[i].sprite = hvyCruiserBluePrintPrefab.GetComponent<ShipBuildDrag>().ShipSprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotTransport":
                        {
                            string localPlayer = GameController.Instance.GameData.LocalPlayerCivEnum.ToString();
                            transportInventorySlot = theSlots[l].gameObject;
                            Image[] itemCruiserImage = theSlots[l].gameObject.GetComponentsInChildren<Image>();
                            for (int i = 0; i < itemCruiserImage.Length; i++)
                            {
                                if (itemCruiserImage[i].name == "ItemTransport" || itemCruiserImage[i].name == "ImageTransportBackground")
                                {
                                    itemCruiserImage[i].sprite = transportBluePrintPrefab.GetComponent<ShipBuildDrag>().ShipSprite;
                                }
                            }
                            break;
                        }

                    default:
                        break;
                }
            }
            Button[] closeButton = sysBuildListInstance.GetComponentsInChildren<Button>();
            for (int l = 0; (l < closeButton.Length); l++)
            {
                closeButton[l].gameObject.SetActive(true);
                switch (closeButton[l].gameObject.name)
                {
                    case "CloseBuilding":
                        {
                            closeButton[l].onClick.RemoveAllListeners();
                            closeButton[l].onClick.AddListener(() => StarSysMenuUIController.Instance.CloseBuildingQueues());
                            break;
                        }
                }
            }
            GameObject shipSliderGO = (GameObject)Instantiate(shipBuildSliderPrefab, new Vector3(0, 0, 0),
                Quaternion.identity);// ship building progress bar as prefab
            shipSliderGO.transform.SetParent(sysBuildListInstance.transform);
            StarSysMenuUIController.Instance.ShipSliderBuildProgress = shipSliderGO.GetComponentInChildren<Slider>();
            shipSliderGO.layer = 5; //UI layer
        }

        public void NewImageInEmptyBuildAbleInventory(StarSysFacilityType type, StarSysController sysCon)
        {
            //prefab.GetComponent<>
            //    sysCon = currentActiveSysCon;
            switch (type)
            {
                case StarSysFacilityType.PowerPlanet:
                    GameObject imageObPower = (GameObject)Instantiate(powerPlantInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var powerPlantSO = GetPowrPlantSObyCivEnum(sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObPower.GetComponentInChildren<Image>().sprite = powerPlantSO.PowerPlantSprite;
                    imageObPower.transform.SetParent(powerPlantInventorySlot.transform, false);
                    break;
                case StarSysFacilityType.Factory:
                    GameObject imageObFactory = (GameObject)Instantiate(factoryInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var factorySO = GetFactorySObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObFactory.GetComponentInChildren<Image>().sprite = factorySO.FactorySprite;
                    imageObFactory.transform.SetParent(factoryInventorySlot.transform, false);
                    break;
                case StarSysFacilityType.Shipyard:
                    GameObject imageObShipyard = (GameObject)Instantiate(shipyardInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var shipyardSO = GetShipyardSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObShipyard.GetComponentInChildren<Image>().sprite = shipyardSO.ShipyardSprite;
                    imageObShipyard.transform.SetParent(shipyardInventorySlot.transform, false);
                    break;
                case StarSysFacilityType.ShieldGenerator:
                    GameObject imageObShield = (GameObject)Instantiate(shieldGenInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var shieldSO = GetShieldGeneratorSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObShield.GetComponentInChildren<Image>().sprite = shieldSO.ShieldGeneratorSprite;
                    imageObShield.transform.SetParent(shieldGenInventorySlot.transform, false);
                    break;
                case StarSysFacilityType.OrbitalBattery:
                    GameObject imageObOB = (GameObject)Instantiate(orbitalBatteryInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var orbitalSO = GetOrbitalBatterySObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObOB.GetComponentInChildren<Image>().sprite = orbitalSO.OrbitalBatterySprite;
                    imageObOB.transform.SetParent(orbitalBatteryInventorySlot.transform, false);
                    break;
                case StarSysFacilityType.ResearchCenter:
                    GameObject imageObRC = (GameObject)Instantiate(researchCenterInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var researchSO = GetResearchCenterSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObRC.GetComponentInChildren<Image>().sprite = researchSO.ResearchCenterSprite;
                    imageObRC.transform.SetParent(researchCenterInventory_slot.transform, false);
                    break;
                default:
                    break;
            }
        }
        public void NewImageInShipInventory(ShipType shiptype)
        {
            switch (shiptype)
            {
                case ShipType.Scout:
                    GameObject ItemSGO = (GameObject)Instantiate(scoutBluePrintPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    ItemSGO.transform.SetParent(scoutInventorySlot.transform, false);
                    break;
                case ShipType.Destroyer:
                    GameObject ItemDGO = (GameObject)Instantiate(destroyerBluePrintPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    ItemDGO.transform.SetParent(destroyerInventorySlot.transform, false);
                    break;
                case ShipType.Cruiser:
                    GameObject cruiserItemGO = (GameObject)Instantiate(cruiserBluePrintPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    cruiserItemGO.transform.SetParent(cruiserInventorySlot.transform, false);
                    break;
                case ShipType.LtCruiser:
                    GameObject ltCruiserItemGO = (GameObject)Instantiate(ltCruiserBluePrintPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    ltCruiserItemGO.transform.SetParent(ltCruiserInventorySlot.transform, false);
                    break;
                case ShipType.HvyCruiser:
                    GameObject hvyCruiserItemGO = (GameObject)Instantiate(hvyCruiserBluePrintPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    hvyCruiserItemGO.transform.SetParent(hvyCruiserInventorySlot.transform, false);
                    break;
                case ShipType.Transport:
                    GameObject transportItemGO = (GameObject)Instantiate(transportBluePrintPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    transportItemGO.transform.SetParent(transportInventorySlot.transform, false);
                    break;
                default:
                    break;
            }
        }
        public void ExposeAllSystemName(CivEnum civEnum)
        {
            localPlayerCanSeeMyNameList.Add(civEnum);
            foreach (var starSysController in StarSysControllerList)
            {
                if (starSysController.StarSysData.CurrentOwnerCivEnum == civEnum)
                {
                    Transform[] transforms = starSysController.gameObject.GetComponentsInChildren<Transform>();
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        GameObject ourGO = transforms[i].gameObject;
                        bool oneDown = false;
                        bool oneMoreDown = false;
                        if (ourGO.name == "SysName")
                        {
                            ourGO.SetActive(true);
                            ourGO.GetComponentInChildren<TextMeshProUGUI>().text = starSysController.StarSysData.SysName;
                            oneDown = true;

                        }
                        if (ourGO.name == "OwnerInsignia")
                        {
                            ourGO.SetActive(true);
                            ourGO.GetComponent<SpriteRenderer>().enabled = true;
                            ourGO.GetComponent<SpriteRenderer>().sortingOrder = 0;
                            oneMoreDown = true;

                        }
                        if (oneDown && oneMoreDown)
                        {
                            return;
                        }
                    }
                }
            }
        }
        public void MoveShipOutOfStarSys(ShipController shipCon, FleetController targetFleet, StarSysController targetSys)
        {
            if (shipCon.ShipData.CurrentStarSysController != null)
            {
                shipCon.ShipData.CurrentStarSysController.StarSysData.ShipsList.Remove(shipCon);
            }
            if (targetFleet != null)
            {
                targetFleet.FleetData.ShipsList.Add(shipCon);
                shipCon.ShipData.CurrentFleetController = targetFleet;
                shipCon.ShipData.CurrentStarSysController = null;
            }
            else if (targetSys != null)
            {
                targetSys.StarSysData.ShipsList.Add(shipCon);
                shipCon.ShipData.CurrentStarSysController = targetSys;
                shipCon.ShipData.CurrentFleetController = null;
            }

        }
    }
}

