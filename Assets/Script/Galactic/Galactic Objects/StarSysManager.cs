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
    /// This is a type of galactic object that is a 'StarSystem' class (Manager/Controller/Data and can have a habitable 'planet') 
    /// with a real star or a nebula or a complex as in the Borg Unicomplex)
    /// Other galactic objects not described by StarSys (will have their own classes (ToDo: Managers/Controllers/Data) for stations (one class),
    /// and blackholes/wormholes (one class.) 
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
        private GameObject sysPrefab;
        [SerializeField]
        private GameObject shipSliderPrefab;

        [SerializeField]
        private GameObject sysUIPrefab;
        public List<StarSysController> StarSysControllerList;
        public GameObject PowerPlantPrefab;
        public GameObject FactoryPrefab;
        public GameObject ShipyardPrefab;
        public GameObject ShieldGeneratorPrefab;
        public GameObject OrbitalBatteryPrefab;
        public GameObject ResearchCenterPrefab;
        //public StarSysController currentActiveSysCon;

        private GameObject powerPlantInventorySlot;
        private GameObject factoryInventorySlot;
        private GameObject shipyardInventorySlot;
        private GameObject shieldGenInventorySlot;
        private GameObject orbitalBatteryInventorySlot;
        private GameObject researchCenterInventorySlot;
        [SerializeField]
        private GameObject fleetPrefab;
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
        private GameObject contentFolderParent;
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
        public void SetShipBuildPerfabs(CivEnum localCiv)
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
                    var shipBuildScript = scoutBluePrintPrefab.GetComponent<ShipInFleetItem>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    scoutBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.Destroyer)
                {
                    var shipBuildScript = destroyerBluePrintPrefab.GetComponent<ShipInFleetItem>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    destroyerBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.Cruiser)
                {
                    var shipBuildScript = cruiserBluePrintPrefab.GetComponent<ShipInFleetItem>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    cruiserBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.LtCruiser)
                {
                    var shipBuildScript = ltCruiserBluePrintPrefab.GetComponent<ShipInFleetItem>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    ltCruiserBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.HvyCruiser)
                {
                    var shipBuildScript = hvyCruiserBluePrintPrefab.GetComponent<ShipInFleetItem>();
                    shipBuildScript.BuildDuration = shipSOList[i].BuildDuration;
                    shipBuildScript.ShipSprite = shipSOList[i].shipSprite;
                    hvyCruiserBluePrintPrefab.GetComponent<Image>().sprite = shipSOList[i].shipSprite;
                }
                else if (shipSOList[i].ShipType == ShipType.Transport)
                {
                    var shipBuildScript = transportBluePrintPrefab.GetComponent<ShipInFleetItem>();
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

                InstantiateSystem(SysData, civSOList[i]);
                //if (civSOList[i].HasWarp)
                //    FleetManager.Instance.FleetDataFromSO(, false);
                //if (SysData.CurrentCivController != null)
                //    starSysDatas.Add(SysData);
            }
            starSysDatas.Remove(starSysDatas[0]); // pull out the null
        }
        public void InstantiateSystem(StarSysData sysData, CivSO civSO)
        {

            if (MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType == GalaxyMapType.RANDOM)
            { // do something random with sys and fleetData.position
            }
            else if (MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType == GalaxyMapType.RING)
            {
                // do something in a ring with sys and fleetData.position
            }
            else
            {
                // Help please, this looks like bad coding practice to me.
                // I think I should be getting the StarSysController directly from the prefab instead of instantiating it as a new object first.
                // When I do that the prefab is null at runtime.
                GameObject starSysGO = (GameObject)Instantiate(sysPrefab, new Vector3(0, 0, 0),
                    Quaternion.identity);
                StarSysController starSysCon = starSysGO.GetComponent<StarSysController>();
                //StarSysController starSysCon = Instantiate(sysPrefab, new Vector3(0, 0, 0),
                //     Quaternion.identity);
                starSysCon.Init(this);
                starSysCon.gameObject.layer = 4; // water layer (also used by fog of war for obsticles with shows to line of sight
                starSysCon.transform.Translate(new Vector3(sysData.GetPosition().x,
                    sysData.GetPosition().y, sysData.GetPosition().z));
                starSysCon.transform.SetParent(galaxyCenter.transform, true);
                starSysCon.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                
                Transform fogObsticleTransform = starSysCon.transform.Find("FogObstacle");
                fogObsticleTransform.SetParent(galaxyCenter.transform, false);
                fogObsticleTransform.Translate(new Vector3(sysData.GetPosition().x, -55f, sysData.GetPosition().z));
                starSysCon.name = sysData.GetSysName();

                sysData.SysGameObject = starSysCon.gameObject;

                TextMeshProUGUI[] TheText = starSysCon.GetComponentsInChildren<TextMeshProUGUI>();
                for (int i = 0; i < TheText.Length; i++)
                {
                    TheText[i].enabled = true;
                    if (TheText[i] != null && TheText[i].name == "SysName")
                    {
                        if (!GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum)) // != CivManager.current.LocalPlayerCivEnum)
                        {
                            TheText[i].text = "UNKNOWN";
                        }
                        else
                        {
                            TheText[i].text = sysData.GetSysName();
                           //var sysThingy = fleetData
                        }
                    }
                    else if (TheText[i] != null && TheText[i].name == "SysDescription (TMP)")
                        TheText[i].text = sysData.Description;

                }

                MapLineFixed ourDropLine = starSysCon.GetComponentInChildren<MapLineFixed>();

                ourDropLine.GetLineRenderer();

                Vector3 galaxyPlanePoint = new Vector3(starSysCon.transform.position.x,
                    galaxyImage.transform.position.y, starSysCon.transform.position.z);
                Vector3[] points = { starSysCon.transform.position, galaxyPlanePoint };
                ourDropLine.SetUpLine(points);

                var Renderers = starSysCon.GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0;i < Renderers.Length; i++)
                {
                    if (Renderers[i] != null)
                    {
                        if (Renderers[i].name == "OwnerInsignia")
                        {
                            Renderers[i].sprite = civSO.Insignia;
                            Renderers[i].gameObject.transform.position =
                                new Vector3(starSysCon.transform.position.x, galaxyPlanePoint.y + 1f, starSysCon.transform.position.z);
                            Renderers[i].gameObject.layer = 4; // water layer (also used by fog of war for obsticles with shows to line of sight
                            if (!GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum))
                            {
                                Renderers[i].sortingOrder = 0;
                                Renderers[i].enabled = false;
                            }

                        } // ToDo: random map with random sprites on nebula, wormholes
                        else if (Renderers[i].name == "StarSprite")
                        {
                            Renderers[i].sprite = sysData.StarSprit;
                            Renderers[i].sortingOrder = 1;
                        }
                    }
                }


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

                List<StarSysController> listStarSysCon = new List<StarSysController> { starSysCon };
                CivManager.Instance.AddSystemToOwnSystemListAndHomeSys(listStarSysCon);
                var canvases = starSysCon.GetComponentsInChildren<Canvas>();
                starSystemCounter++;
                if (starSystemCounter == CivManager.Instance.CivControllersInGame.Count)
                {
                    csFogWar.Instance.RunFogOfWar(); // star systems are in place so time to scan for the fog

                }
                if (civSO.HasWarp)
                     FleetManager.Instance.BuildFirstFleets(starSysCon, false); // fleet for first ships as game loads, not for ships instatiated by working shipyard in system
                if (true) //(GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum)) 
                {
                    StarSysSO starSysSO = GetStarSObyInt(civSO.CivInt);
                    sysData.PowerPlants = AddSystemFacilities(starSysSO.PowerStations, PowerPlantPrefab, civSO.CivInt, sysData, 1);
                    sysData.Factories = AddSystemFacilities(starSysSO.Factories, FactoryPrefab, civSO.CivInt, sysData, 1);
                    sysData.Shipyards = AddSystemFacilities(starSysSO.Shipyards, ShipyardPrefab, civSO.CivInt, sysData,1);
                    sysData.ShieldGenerators = AddSystemFacilities(starSysSO.ShieldGenerators, ShieldGeneratorPrefab, civSO.CivInt, sysData,1);
                    sysData.OrbitalBatteries = AddSystemFacilities(starSysSO.OrbitalBatteries, OrbitalBatteryPrefab, civSO.CivInt, sysData,1);
                    sysData.ResearchCenters = AddSystemFacilities(starSysSO.ResearchCenters, ResearchCenterPrefab, civSO.CivInt, sysData,1);
                    SetParentForFacilities(starSysCon.gameObject, sysData);

                }
                if (GameController.Instance.AreWeLocalPlayer(sysData.CurrentOwnerCivEnum))
                {
                    localPlayerTheme = ThemeManager.Instance.GetLocalPlayerTheme();
                }
                InstantiateSysUIGameObject(starSysCon);
                //***** This is temporary so we can test a multi-starsystem civ
                //******* before diplomacy will alow civs/systems to join another civ
                //if (systemCount == 8)
                //{
                //    CivManager.current.nowCivsCanJoinTheFederation = true;
                //}
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

        public List<GameObject> AddSystemFacilities(int numOf, GameObject prefab, int civInt, StarSysData sysData, int onOff)
        {
            List<GameObject> list = new List<GameObject>();
            TechLevel techLevel = GameController.Instance.GameData.StartingTechLevel;
            int startingStarDate = TimeManager.Instance.StaringStardate;
            switch (prefab.name)
            {
                // "SysName" not done here. See in each system ribbon is set in GalaxyMenuUIController without a facility game object needed
                case "PowerPlantData":
                    {
                        PowerPlantData powerPlantData = new PowerPlantData("null");
                        var powerPlantSO = GetPowrPlantSObyCivInt(civInt);
                        powerPlantData.CivInt = civInt;
                        powerPlantData.TechLevel = techLevel;
                        powerPlantData.FacilitiesEnumType = StarSysFacilities.PowerPlanet;
                        powerPlantData.Name = powerPlantSO.Name;                       
                        powerPlantData.StartStarDate = startingStarDate;
                        powerPlantData.BuildDuration = powerPlantSO.BuildDuration;
                        powerPlantData.PowerOutput = powerPlantSO.PowerOutput;  
                        powerPlantData.PowerPlantSprite = powerPlantSO.PowerPlantSprite;
                        powerPlantData.Description = powerPlantSO.Description;
                        sysData.PowerPlantData = powerPlantData;
                        
                        List<PowerPlantData> powerPlantDatas = new List<PowerPlantData>();
                        for (int i = 0; i < numOf; i++)
                        {
                            powerPlantDatas.Add(powerPlantData);
                            GameObject newFacilityGO = (GameObject)Instantiate(prefab, new Vector3(0, 0, 0),
                                Quaternion.identity);
                            newFacilityGO.layer = 5;
                            GetPowerPlantText(newFacilityGO, powerPlantData, numOf);
                            newFacilityGO.SetActive(false);
                            
                            powerPlantData.SysGameObject = newFacilityGO;
                            list.Add(newFacilityGO);
                        }  
                        
                        break;
                    }
                case "FactoryData":
                    {
                        FactoryData factoryData = new FactoryData("null");
                        var factorySO = GetFactorySObyCivInt(civInt);
                        factoryData.CivInt = civInt;
                        factoryData.TechLevel = techLevel;
                        factoryData.FacilitiesEnumType = StarSysFacilities.Factory;
                        factoryData.Name = factorySO.Name;
                        factoryData.StartStarDate = startingStarDate;
                        factoryData.PowerLoad = factorySO.PowerLoad;
                        factoryData.BuildDuration = factorySO.BuildDuration;
                        factoryData.FactorySprite = factorySO.FactorySprite;
                        factoryData.Description = factorySO.Description;
                        sysData.FactoryData = factoryData;
                        List<FactoryData> fDatas = new List<FactoryData>();
                        for (int i = 0; i < numOf; i++)
                            fDatas.Add(factoryData);
                        for (int i = 0; i < fDatas.Count; i++)
                        {
                            GameObject newFacilityGO = (GameObject)Instantiate(prefab, new Vector3(0, 0, 0),
                                Quaternion.identity);
                            newFacilityGO.layer = 5;
                            TextMeshProUGUI On = newFacilityGO.AddComponent<TextMeshProUGUI>();
                            On.text = onOff.ToString(); // 0 = off, 1 = on.
                            GetFactoryText(newFacilityGO, factoryData, numOf);
                            newFacilityGO.SetActive(false);
                            factoryData.SysGameObject = newFacilityGO;
                            list.Add(newFacilityGO);
                            sysData.TotalSysPowerLoad += factoryData.PowerLoad;
                        }
                        break;
                    }
                case "ShipyardData":
                    {
                        ShipyardData syData = new ShipyardData("null");
                        var sSO = GetShipyardSObyCivInt(civInt);
                        syData.CivInt = civInt;
                        syData.TechLevel = techLevel;
                        syData.FacilitiesEnumType = StarSysFacilities.Shipyard;
                        syData.Name = sSO.Name;
                        syData.StartStarDate = startingStarDate;
                        syData.BuildDuration = sSO.BuildDuration;
                        syData.PowerLoad = sSO.PowerLoad;
                        syData.ShipyardSprite = sSO.ShipyardSprite;
                        syData.Description = sSO.Description;
                        sysData.ShipyardData = syData;
                        List<ShipyardData> syDatas = new List<ShipyardData>();
                        for (int i = 0; i < numOf; i++)
                            syDatas.Add(syData);
                        for (int i = 0; i < syDatas.Count; i++)
                        {
                            GameObject newFacilityGO = (GameObject)Instantiate(prefab, new Vector3(0, 0, 0),
                                Quaternion.identity);
                            newFacilityGO.layer = 5;
                            TextMeshProUGUI On = newFacilityGO.AddComponent<TextMeshProUGUI>();
                            On.text = onOff.ToString();
                            GetShipyardText(newFacilityGO, syData, numOf);
                            newFacilityGO.SetActive(false);
                            syData.SysGameObject = newFacilityGO;
                            list.Add(newFacilityGO);
                            sysData.TotalSysPowerLoad += syData.PowerLoad;
                        }
                        break;
                    }
                case "ShieldGeneratorData":
                    {
                        ShieldGeneratorData sgData = new ShieldGeneratorData("null");
                        var sgSO = GetShieldGeneratorSObyCivInt(civInt);
                        sgData.CivInt = civInt;
                        sgData.TechLevel = techLevel;
                        sgData.FacilitiesEnumType = StarSysFacilities.ShieldGenerator;
                        sgData.Name = sgSO.Name;
                        sgData.StartStarDate = startingStarDate;
                        sgData.BuildDuration = sgSO.BuildDuration;
                        sgData.PowerLoad = sgSO.PowerLoad;
                        sgData.ShieldGeneratorSprite = sgSO.ShieldGeneratorSprite;
                        sgData.Description = sgSO.Description;
                        sysData.ShieldGeneratorData = sgData;
                        List<ShieldGeneratorData> sGDatas = new List<ShieldGeneratorData>();
                        for (int i = 0; i < numOf; i++)
                            sGDatas.Add(sgData);
                        for (int i = 0; i < sGDatas.Count; i++)
                        {
                            GameObject newFacilityGO = (GameObject)Instantiate(prefab, new Vector3(0, 0, 0),
                                Quaternion.identity);
                            newFacilityGO.layer = 5;
                            TextMeshProUGUI On = newFacilityGO.AddComponent<TextMeshProUGUI>();
                            On.text = onOff.ToString();
                            GetShieldGText(newFacilityGO, sgData, numOf);
                            newFacilityGO.SetActive(false);
                            sgData.SysGameObject = newFacilityGO;
                            list.Add(newFacilityGO);
                            sysData.TotalSysPowerLoad += sgData.PowerLoad;
                        }
                        break;
                    }
                case "OrbitalBatteryData":
                    {
                        OrbitalBatteryData obData = new OrbitalBatteryData("null");
                        var sgSO = GetOrbitalBatterySObyCivInt(civInt);
                        obData.CivInt = civInt;
                        obData.TechLevel = techLevel;
                        obData.FacilitiesEnumType = StarSysFacilities.OrbitalBattery;
                        obData.Name = sgSO.Name;
                        obData.StartStarDate = startingStarDate;
                        obData.BuildDuration = sgSO.BuildDuration;
                        obData.PowerLoad = sgSO.PowerLoad;
                        obData.OrbitalBatterySprite = sgSO.OrbitalBatterySprite;
                        obData.Description = sgSO.Description;
                        sysData.OrbitalBatteryData = obData;
                        List<OrbitalBatteryData> oBDatas = new List<OrbitalBatteryData>();
                        for (int i = 0; i < numOf; i++)
                            oBDatas.Add(obData);
                        for (int i = 0; i < oBDatas.Count; i++)
                        {
                            GameObject newFacilityGO = (GameObject)Instantiate(prefab, new Vector3(0, 0, 0),
                                Quaternion.identity);
                            newFacilityGO.layer = 5;
                            TextMeshProUGUI On = newFacilityGO.AddComponent<TextMeshProUGUI>();
                            On.text = onOff.ToString();
                            GetOBText(newFacilityGO, obData, numOf);
                            newFacilityGO.SetActive(false);
                            obData.SysGameObject = newFacilityGO;
                            list.Add(newFacilityGO);
                            sysData.TotalSysPowerLoad += obData.PowerLoad;
                        }
                        break;
                    }
                case "ResearchCenterData":
                    {
                        ResearchCenterData researchData = new ResearchCenterData("null");
                        var sgSO = GetResearchCenterSObyCivInt(civInt);
                        researchData.CivInt = civInt;
                        researchData.TechLevel = techLevel;
                        researchData.FacilitiesEnumType = StarSysFacilities.ResearchCenter;
                        researchData.Name = sgSO.Name;
                        researchData.StartStarDate = startingStarDate;
                        researchData.BuildDuration = sgSO.BuildDuration;
                        researchData.PowerLoad = sgSO.PowerLoad;
                        researchData.ResearchCenterSprite = sgSO.ResearchCenterSprite;
                        researchData.Description = sgSO.Description;
                        sysData.ResearchCenterData = researchData;
                        List<ResearchCenterData> reDatas = new List<ResearchCenterData>();
                        for (int i = 0; i < numOf; i++)
                            reDatas.Add(researchData);
                        for (int i = 0; i < reDatas.Count; i++)
                        {
                            GameObject newFacilityGO = (GameObject)Instantiate(prefab, new Vector3(0, 0, 0),
                                Quaternion.identity);
                            newFacilityGO.layer = 5;
                            TextMeshProUGUI On = newFacilityGO.AddComponent<TextMeshProUGUI>();
                            On.text = onOff.ToString();
                            GetResearchCenterText(newFacilityGO, researchData, numOf);
                            newFacilityGO.SetActive(false);
                            researchData.SysGameObject = newFacilityGO;
                            list.Add(newFacilityGO);
                            sysData.TotalSysPowerLoad += researchData.PowerLoad;
                        }
                        break;
                    }
                default:
                    break;
            }
            return list;
        }

        private void GetPowerPlantText(GameObject go, PowerPlantData plantData, int numOf)
        {
            TextMeshProUGUI[] TheText = go.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0;i < TheText.Length;i++) 
            {
                TheText[i].enabled = true;
                if (TheText[i].name == "NameText (TMP)")
                    TheText[i].text = plantData.Name;
                else if (TheText[i].name == "NumTotalUnits (TMP)")
                    TheText[i].text = numOf.ToString();
                else if (TheText[i].name == "NumTotalEOut (TMP)")
                    TheText[i].text = plantData.PowerOutput.ToString();
                else if (TheText[i].name == "DescriptionText (TMP)")
                    TheText[i].text = plantData.Description;
                //Doing the system power load in SysData/ GalaxyMenuUIController //else if (OneTmp.name == "NumP Load")

                // image here

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
            for (int i = 0; i< starSysSOList.Count; i++)
            {
                if (starSysSOList[i].StarSysInt == sysInt)
                {
                    result = starSysSOList[i];
                    break;
                }
            }
            return result;

        }
        private PowerPlantSO GetPowrPlantSObyCivInt(int  civInt)
        {
            PowerPlantSO result = null;
            if (civInt <= 6)
            {
                result = powerPlantSOList[civInt];
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
            if(civInt <= 6)
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
            if ( civInt <= 6)
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
            for (int i = 0;i< StarSysControllerList.Count; i++)
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
                //currentActiveSysCon = sysController;
                if (sysController.StarSystUIGameObject == null)
                {
                    GameObject thisStarSysUIGameObject = (GameObject)Instantiate(sysUIPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    thisStarSysUIGameObject.layer = 5;
                    sysController.StarSystUIGameObject = thisStarSysUIGameObject;
                    sysController.StarSystUIGameObject.SetActive(true);
                    thisStarSysUIGameObject.transform.SetParent(contentFolderParent.transform, false); // load into List of systems
                }
            }    
        }

        public void InstantiateSysBuildListUI(StarSysController sysCon) // open the build queue UI
        {
            GameObject sysBuildListInstance = (GameObject)Instantiate(sysBuildUIListPrefab, new Vector3(0, -70, 0),
                Quaternion.identity);
            GalaxyMenuUIController.Instance.SetActiveBuildMenu(sysBuildListInstance);
            sysBuildListInstance.layer = 5; //UI layer

            canvasBuildList.SetActive(true);

            // getting the FactoryBuildableItems code, set StarSysController/Data for them so they can send image endDrags back.
            sysBuildListInstance.transform.SetParent(canvasBuildList.transform, false);
            FactoryBuildableItem[] buildable = sysBuildListInstance.GetComponentsInChildren<FactoryBuildableItem>();


            for (int m = 0; m < buildable.Length; m++)
            {
                buildable[m].StarSysController = sysCon;
                if (buildable[m].name == "ItemPowerPlant")
                {
                    buildable[m].FacilityType = StarSysFacilities.PowerPlanet;
                }
                else if (buildable[m].name == "ItemFactory")
                    buildable[m].FacilityType = StarSysFacilities.Factory;
                else if (buildable[m].name == "ItemShipyard")
                    buildable[m].FacilityType = StarSysFacilities.Shipyard;
                else if (buildable[m].name == "ItemShieldGenerator")
                    buildable[m].FacilityType = StarSysFacilities.ShieldGenerator;
                else if (buildable[m].name == "ItemOrbitalBattery")
                    buildable[m].FacilityType = StarSysFacilities.OrbitalBattery;
                else if (buildable[m].name == "ItemResearchCenter")
                    buildable[m].FacilityType = StarSysFacilities.ResearchCenter;
            }
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
                    sysCon.buildListGridLayoutGroup = theGrids[k];
                    sysCon.GridFactoryQueueUpdate();
                }
                else if (theGrids[k].name == "QueueHoldingBuildableShips")
                {
                    sysCon.shipListGridLayoutGroup = theGrids[k];
                    sysCon.GridShipQueueUpdate();
                    break;
                }
            }

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
                            researchCenterInventorySlot = theSlots[l].gameObject;
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
                            sysCon.SliderBuildProgress = theSlots[l].gameObject.GetComponent<Slider>();
                            sysCon.SliderBuildProgress.gameObject.transform.SetParent(theSlots[l]);
                            break;
                        }
                    case "Cruiser (TMP)":
                        {
                            if (sysCon.StarSysData.CurrentCivController.CivData.TechLevel == TechLevel.EARLY || sysCon.StarSysData.CurrentCivController.CivData.TechLevel == TechLevel.SUPREME)
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
                                    itemScoutImage[i].sprite = scoutBluePrintPrefab.GetComponent<ShipInFleetItem>().ShipSprite;
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
                                    itemDestroyerImage[i].sprite = destroyerBluePrintPrefab.GetComponent<ShipInFleetItem>().ShipSprite;
                                }
                            }
                            break;
                        }
                    case "ItemSlotCruiser":
                        {
                            if (sysCon.StarSysData.CurrentCivController.CivData.TechLevel == TechLevel.EARLY || sysCon.StarSysData.CurrentCivController.CivData.TechLevel == TechLevel.SUPREME)
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
                                    itemCruiserImage[i].sprite = cruiserBluePrintPrefab.GetComponent<ShipInFleetItem>().ShipSprite;
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
                                    itemCruiserImage[i].sprite = ltCruiserBluePrintPrefab.GetComponent<ShipInFleetItem>().ShipSprite;
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
                                    itemCruiserImage[i].sprite = hvyCruiserBluePrintPrefab.GetComponent<ShipInFleetItem>().ShipSprite;
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
                                    itemCruiserImage[i].sprite = transportBluePrintPrefab.GetComponent<ShipInFleetItem>().ShipSprite;
                                }
                            }
                            break;
                        }

                    default:
                        break;
                }
            }
            GameObject shipSliderGO = (GameObject)Instantiate(shipSliderPrefab, new Vector3(0, 0, 0),
                Quaternion.identity);
            shipSliderGO.transform.SetParent(sysBuildListInstance.transform);
            sysCon.ShipSliderBuildProgress = shipSliderGO.GetComponentInChildren<Slider>();
            shipSliderGO.layer = 5; //UI layer

        }
        
        public void NewImageInEmptyBuildableInventory(GameObject prefab, StarSysController sysCon) 
        {
            //if (sysCon == null)
            //    sysCon = currentActiveSysCon;
            switch (prefab.name)
            {
                case "PowerPlantData":
                    GameObject imageObPower = (GameObject)Instantiate(powerPlantInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var powerPlantSO = GetPowrPlantSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObPower.GetComponentInChildren<Image>().sprite = powerPlantSO.PowerPlantSprite;
                    imageObPower.transform.SetParent(powerPlantInventorySlot.transform, false);
                    break;
                case "FactoryData":
                    GameObject imageObFactory = (GameObject)Instantiate(factoryInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var factorySO = GetFactorySObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObFactory.GetComponentInChildren<Image>().sprite = factorySO.FactorySprite;
                    imageObFactory.transform.SetParent(factoryInventorySlot.transform, false);
                    break;
                case "ShipyardData":
                    GameObject imageObShipyard = (GameObject)Instantiate(shipyardInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var shipyardSO = GetShipyardSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObShipyard.GetComponentInChildren<Image>().sprite = shipyardSO.ShipyardSprite;
                    imageObShipyard.transform.SetParent(shipyardInventorySlot.transform, false);
                    break;
                case "ShieldGeneratorData":
                    GameObject imageObShield = (GameObject)Instantiate(shieldGenInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var shieldSO = GetShieldGeneratorSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObShield.GetComponentInChildren<Image>().sprite = shieldSO.ShieldGeneratorSprite;
                    imageObShield.transform.SetParent(shieldGenInventorySlot.transform, false);
                    break;
                case "OrbitalBatteryData":
                    GameObject imageObOB = (GameObject)Instantiate(orbitalBatteryInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var orbitalSO = GetOrbitalBatterySObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObOB.GetComponentInChildren<Image>().sprite = orbitalSO.OrbitalBatterySprite;
                    imageObOB.transform.SetParent(orbitalBatteryInventorySlot.transform, false);
                    break;
                case "ResearchCenterData":
                    GameObject imageObRC = (GameObject)Instantiate(researchCenterInventorySlotPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    var researchSO = GetResearchCenterSObyCivInt((int)sysCon.StarSysData.CurrentOwnerCivEnum);
                    imageObRC.GetComponentInChildren<Image>().sprite = researchSO.ResearchCenterSprite;
                    imageObRC.transform.SetParent(researchCenterInventorySlot.transform, false);
                    break;
                default:
                    break;
            }
        }
        public void NewImageInEmptyShipBuildableInventory(ShipType shiptype)
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
                    // Not changing a sys sprite for now
                    //var Renderers = starSysController.gameObject.GetComponentsInChildren<SpriteRenderer>();
                    //for (int i = 0; i < Renderers.Length; i++)
                    //{
                    //    if (Renderers[i] != null)
                    //    {
                    //        if (Renderers[i].name == "StarSprite")
                    //        {
                    //            Renderers[i].gameObject.SetActive(true);
                    //            //var fog = starSysController.gameObject.GetComponent<csFogVisibilityAgent>();
                    //            //if (fog != null)
                    //            //    fog.spriteRenderers.Add(Renderers[i]);
                    //        }
                    //        else if (Renderers[i].name == "InsigniaUnknown")
                    //            Renderers[i].gameObject.SetActive(false);
                    //    }
                    //}
                }
            }
        }
    }   
}

