using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;

namespace Assets.Core
{
    /// <summary>
    /// Controlling Star System interactions while the matching StarSystemData class
    /// holds key info on status and for save game
    /// </summary>
    public class StarSysController : MonoBehaviour
    {
        //Fields
        private StarSysManager _manager;
        private StarSysData starSysData;
        public StarSysData StarSysData { get { return starSysData; } set { starSysData = value; } }
        [SerializeField]
        private GameObject starSysUIGameObject; //The instantiated system UI for this system. a prefab clone, not a class but a game object
        // instantiated by StarSysManager from a prefab and added to StarSysController
        public GameObject StarSystUIGameObject { get { return starSysUIGameObject; } set { starSysUIGameObject = value; } }
        private Camera galaxyEventCamera;
        [SerializeField]
        private Canvas canvasToolTip;
        public static event Action<TrekRandomEventSO> TrekEventDisasters;
        public GridLayoutGroup buildListGridLayoutGroup;
        public GridLayoutGroup shipListGridLayoutGroup;
        [SerializeField]
        private List<Transform> sysBuildQueueList;
        private int lastBuildQueueCount = 0;
        private Transform lastBuildingItem;
        private Transform buildingItem;
        private bool building = false;
        private bool starTimer = true;
        public Slider SliderBuildProgress;
        private float starDateOfCompletion = 1f;
        private int currentProgress = 1;
        private int startDate = 1;
        public int TimeToBuild = 1;
        [SerializeField]
        private List<Transform> shipBuildQueueList;
        private int lastShipBuildQueueCount = 0;
        private Transform lastShipBuildingItem;
        private Transform shipBuildingItem;
        private bool shipBuilding = false;
        private bool shipStartTimer = true;
        public Slider ShipSliderBuildProgress;
        private float shipStarDateOfCompletion = 1f;
        private int shipCurrentProgress = 1;
        private int shipStartDate = 1;
        public int ShipTimeToBuild = 1;

        public StarSysController(string name)
        {
            StarSysData = new StarSysData(name);
            starDateOfCompletion = 0f;
        }
        private void Start()
        {
            galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
            canvasToolTip.worldCamera = galaxyEventCamera;
            TimeManager.Instance.OnRandomSpecialEvent += DoDisaster;
            OnOffSysFacilityEvents.current.FacilityOnClick += FacilityOnClick;// subscribe methode to the event += () => Debug.Log("Action Invoked!");
            starDateOfCompletion = 0f;
        }
        private void Update()
        {
            if (buildListGridLayoutGroup != null)
            {
                if (lastBuildQueueCount != buildListGridLayoutGroup.transform.childCount)
                {
                    GridFactoryQueueUpdate();
                }
                else
                {
                    int counter = 0;
                    foreach (var item in buildListGridLayoutGroup.transform)
                    {
                        if (sysBuildQueueList[counter] != null && (Transform)item != sysBuildQueueList[counter])
                        {
                            GridFactoryQueueUpdate();
                            break;
                        }
                        else
                            counter++;
                    }
                }
            }
            if (shipListGridLayoutGroup != null)
            {
                if (lastShipBuildQueueCount != shipListGridLayoutGroup.transform.childCount)
                {
                    GridShipQueueUpdate();
                }
                else
                {
                    int counter = 0;
                    foreach (var item in shipListGridLayoutGroup.transform)
                    {
                        if (shipBuildQueueList[counter] != null && (Transform)item != shipBuildQueueList[counter])
                        {
                            GridShipQueueUpdate();
                            break;
                        }
                        else
                            counter++;
                    }
                }
            }
            //}
            // Are we building anything
            // 
            if (building && TimeToBuild > 0) //&& GameController.Instance.AreWeLocalPlayer(this.StarSysData.CurrentOwnerCivEnum)
            {

                if (starTimer)
                {
                    startDate = TimeManager.Instance.CurrentStarDate();
                    starDateOfCompletion = TimeManager.Instance.CurrentStarDate() + TimeToBuild;
                    starTimer = false;
                }
                else if (TimeManager.Instance.CurrentStarDate() <= starDateOfCompletion)
                {
                    currentProgress = (int)(TimeManager.Instance.CurrentStarDate() - startDate);
                    if (TimeToBuild <= 0)
                        TimeToBuild = 1;
                    SetBuildProgress((float)currentProgress / (float)TimeToBuild);
                }
                else if (TimeManager.Instance.CurrentStarDate() >= starDateOfCompletion)
                {
                    building = false;
                    SetBuildProgress(0);
                    starTimer = true;
                    TimeToBuild = 0;
                    buildingItem = null;
                    switch (sysBuildQueueList[0].gameObject.GetComponentInChildren<FactoryBuildableItem>().FacilityType)
                    {
                        case StarSysFacilities.PowerPlanet:
                            this.StarSysData.PowerPlants.Add(StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.PowerPlantPrefab, (int)this.StarSysData.CurrentOwnerCivEnum, this.StarSysData, 0)[0]);
                            if (starSysUIGameObject != null)
                            {
                                TextMeshProUGUI[] theTextItems = starSysUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
                                for (int j = 0; j < theTextItems.Length; j++)
                                {
                                    theTextItems[j].enabled = true;
                                    if (theTextItems[j].name == "NumPUnits")
                                        theTextItems[j].text = this.StarSysData.PowerPlants.Count.ToString();
                                    else if (theTextItems[j].name == "NumTotalEOut")
                                    {
                                        this.starSysData.TotalSysPowerOutput = (this.StarSysData.PowerPlants.Count) * (this.StarSysData.PowerPlantData.PowerOutput);
                                        theTextItems[j].text = this.starSysData.TotalSysPowerOutput.ToString();
                                    }
                                }
                            }
                            GalaxyMenuUIController.Instance.UpdateSystemPowerLoad(this);
                            break;

                        case StarSysFacilities.Factory:
                            var factory = (StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.FactoryPrefab, (int)this.StarSysData.CurrentOwnerCivEnum, this.StarSysData, 0)[0]);
                            AddSysFacility(factory, "FactoryLoad", "NumFactoryRatio", StarSysFacilities.Factory);
                            break;
                        case StarSysFacilities.Shipyard:
                            var shipyard = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.ShipyardPrefab, (int)this.StarSysData.CurrentOwnerCivEnum, this.StarSysData, 0)[0];
                            AddSysFacility(shipyard, "YardLoad", "NumYardsOnRatio", StarSysFacilities.Shipyard);
                            break;
                        case StarSysFacilities.ShieldGenerator:
                            var shieldGenerator = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.ShieldGeneratorPrefab, (int)this.StarSysData.CurrentOwnerCivEnum, this.StarSysData, 0)[0];
                            AddSysFacility(shieldGenerator, "ShieldLoad", "NumShieldRatio", StarSysFacilities.ShieldGenerator);
                            break;
                        case StarSysFacilities.OrbitalBattery:
                            var orbitalBatterie = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.OrbitalBatteryPrefab, (int)this.StarSysData.CurrentOwnerCivEnum, this.StarSysData, 0)[0];
                            AddSysFacility(orbitalBatterie, "OBLoad", "NumOBRatio", StarSysFacilities.OrbitalBattery);
                            break;
                        case StarSysFacilities.ResearchCenter:
                            var researchCenter = StarSysManager.Instance.AddSystemFacilities(1, StarSysManager.Instance.ResearchCenterPrefab, (int)this.StarSysData.CurrentOwnerCivEnum, this.StarSysData, 0)[0];
                            AddSysFacility(researchCenter, "ResearchLoad", "NumResearchRatio", StarSysFacilities.ResearchCenter);

                            break;
                        default:
                            break;
                    }
                    var imageTransform = sysBuildQueueList[0];
                    imageTransform.SetParent(imageTransform.GetComponent<FactoryBuildableItem>().originalParent, false);
                    if (imageTransform.parent.childCount > 1)
                    {
                        Destroy(imageTransform.gameObject);
                    }
                    sysBuildQueueList.Remove(sysBuildQueueList[0]);
                }
            }
            else if (TimeToBuild < 0)
            {
                TimeToBuild = 0;

            }
            if (shipBuilding && ShipTimeToBuild > 0) //&& GameController.Instance.AreWeLocalPlayer(this.StarSysData.CurrentOwnerCivEnum)
            {
                if (shipStartTimer)
                {
                    shipStartDate = TimeManager.Instance.CurrentStarDate();
                    shipStarDateOfCompletion = TimeManager.Instance.CurrentStarDate() + ShipTimeToBuild;
                    shipStartTimer = false;
                }
                else if (TimeManager.Instance.CurrentStarDate() <= shipStarDateOfCompletion)
                {
                    shipCurrentProgress = (int)(TimeManager.Instance.CurrentStarDate() - shipStartDate);
                    if (ShipTimeToBuild <= 0)
                        ShipTimeToBuild = 1;
                    SetShipBuildProgress((float)shipCurrentProgress / (float)ShipTimeToBuild);
                }
                else if (TimeManager.Instance.CurrentStarDate() >= shipStarDateOfCompletion)
                {
                    ShipType shipType = new ShipType();
                    shipBuilding = false;
                    SetShipBuildProgress(0.02f);
                    shipStartTimer = true;
                    ShipTimeToBuild = 0;
                    shipBuildingItem = null;
                    CivEnum localPlayerCivEnum = CivManager.Instance.LocalPlayerCivContoller.CivData.CivEnum;

                    switch (shipBuildQueueList[0].gameObject.GetComponentInChildren<ShipInFleetItem>().ShipType)
                    {
                        case ShipType.Scout:
                            shipType = ShipType.Scout;
                            break;
                        case ShipType.Destroyer:
                            shipType = ShipType.Destroyer;
                            break;
                        case ShipType.Cruiser:
                            shipType = ShipType.Cruiser;
                            break;
                        case ShipType.LtCruiser:
                            shipType = ShipType.LtCruiser;
                            break;
                        case ShipType.HvyCruiser:
                            shipType = ShipType.HvyCruiser;
                            break;
                        case ShipType.Transport:
                            shipType = ShipType.Transport;
                            break;
                        default:
                            break;
                    }

                    if (this.StarSysData.FleetsInSystem.Count > 0)
                    {
                        ShipManager.Instance.BuildShipInOurFleet(shipType, this.StarSysData.FleetsInSystem[0], this); // put a ship in the fleet
                    }
                    var imageTransform = shipBuildQueueList[0];
                    imageTransform.SetParent(imageTransform.GetComponent<ShipInFleetItem>().originalParent, false);
                    if (imageTransform.parent.childCount > 1)
                    {
                        Destroy(imageTransform.gameObject);
                    }
                    shipBuildQueueList.Remove(shipBuildQueueList[0]);
                }
            }
            else if (ShipTimeToBuild < 0)
            {
                ShipTimeToBuild = 0;
            }
        }
        public void Init(StarSysManager manager)
        {
            _manager = manager;
        }
        public void GridFactoryQueueUpdate()
        {
            lastBuildQueueCount = this.buildListGridLayoutGroup.transform.childCount;
            Debug.Log("Grid layout has changed!");
            // update syscontroller sysBuildQueue list to match buildListBridLayoutGroup.tranform children
            foreach (Transform child in buildListGridLayoutGroup.transform)
            {
                if (!sysBuildQueueList.Contains(child))
                    sysBuildQueueList.Add(child);
            }

            //Does sysBuildQueueList have extra items not in buildListGridLayoutGroup children?
            foreach (Transform child in buildListGridLayoutGroup.transform)
            {
                if (!sysBuildQueueList.Contains(child))
                    sysBuildQueueList.Remove(child);
            }

            // Sort by Y position (top to bottom), then X position (left to right)
            sysBuildQueueList = sysBuildQueueList.OrderByDescending(t => t.localPosition.y)
                                    .ThenBy(t => t.localPosition.x)
                                    .ToList();
            if (sysBuildQueueList.Count > 0 && sysBuildQueueList[0] != null)
            {
                buildingItem = sysBuildQueueList[0];
                building = true;

                if (buildingItem != lastBuildingItem)
                {
                    TimeToBuild = GetBuildTimeDuration(buildingItem.gameObject.GetComponentInChildren<FactoryBuildableItem>().FacilityType);
                    lastBuildingItem = buildingItem;
                    starTimer = true;
                }
            }
            else { building = false; }
        }
        public void GridShipQueueUpdate()
        {
            lastShipBuildQueueCount = this.shipListGridLayoutGroup.transform.childCount;
            Debug.Log("Ship Grid layout has changed!");
            // update syscontroller list to match buildShipListBridLayoutGroup.tranform children
            foreach (Transform child in shipListGridLayoutGroup.transform)
            {
                if (!shipBuildQueueList.Contains(child))
                    shipBuildQueueList.Add(child);
            }

            //Does shipBuildQueueList have extra items not in buildListGridLayoutGroup children?
            foreach (Transform child in shipListGridLayoutGroup.transform)
            {
                if (!shipBuildQueueList.Contains(child))
                    shipBuildQueueList.Remove(child);
            }

            // Sort by Y position (top to bottom), then X position (left to right)
            shipBuildQueueList = shipBuildQueueList.OrderByDescending(t => t.localPosition.y)
                                    .ThenBy(t => t.localPosition.x)
                                    .ToList();
            if (shipBuildQueueList.Count > 0 && shipBuildQueueList[0] != null)
            {
                shipBuildingItem = shipBuildQueueList[0];
                shipBuilding = true;

                if (shipBuildingItem != lastShipBuildingItem)
                {
                    var shipBuildableItem = shipBuildingItem.gameObject.GetComponentInChildren<ShipInFleetItem>();
                    ShipTimeToBuild = ShipManager.Instance.GetShipBuildDuration(shipBuildableItem.ShipType, this.StarSysData.CurrentCivController.CivData.TechLevel, this.StarSysData.CurrentOwnerCivEnum);
                    lastShipBuildingItem = shipBuildingItem;
                    shipStartTimer = true;
                }
            }
            else { shipBuilding = false; }
        }


        private void AddSysFacility(GameObject faciltyGO, string loadName, string ratioName, StarSysFacilities facilityType)
        {
            if (GameController.Instance.AreWeLocalPlayer(this.StarSysData.CurrentOwnerCivEnum))
            {
                int newFacilityLoad = 0;
                List<GameObject> facilities = new List<GameObject>();
                switch (facilityType)
                {
                    case StarSysFacilities.Factory:
                        newFacilityLoad = StarSysData.FactoryData.PowerLoad;
                        this.StarSysData.Factories.Add(faciltyGO);
                        facilities = this.StarSysData.Factories;
                        break;
                    case StarSysFacilities.Shipyard:
                        newFacilityLoad = StarSysData.ShipyardData.PowerLoad;
                        this.StarSysData.Shipyards.Add(faciltyGO);
                        facilities = this.StarSysData.Shipyards;
                        break;
                    case StarSysFacilities.ShieldGenerator:
                        newFacilityLoad = StarSysData.ShieldGeneratorData.PowerLoad;
                        this.StarSysData.ShieldGenerators.Add(faciltyGO);
                        facilities = StarSysData.ShieldGenerators;
                        break;
                    case StarSysFacilities.OrbitalBattery:
                        newFacilityLoad = StarSysData.OrbitalBatteryData.PowerLoad;
                        this.StarSysData.OrbitalBatteries.Add(faciltyGO);
                        facilities = StarSysData.OrbitalBatteries;
                        break;
                    case StarSysFacilities.ResearchCenter:
                        newFacilityLoad = StarSysData.ResearchCenterData.PowerLoad;
                        this.StarSysData.ResearchCenters.Add(faciltyGO);
                        facilities = StarSysData.ResearchCenters;
                        break;
                    default:
                        break;
                }

                if (starSysUIGameObject != null)
                {
                    TextMeshProUGUI[] theTextItems = starSysUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
                    bool allDone = false;
                    for (int j = 0; j < theTextItems.Length; j++)
                    {
                        theTextItems[j].enabled = true;
                        int load = 0;
                        if (theTextItems[j].name == loadName)
                        {
                            for (int k = 0; k < facilities.Count; k++)
                            {
                                if (facilities[k].GetComponent<TextMeshProUGUI>().text == "1")
                                {
                                    load += newFacilityLoad;
                                }
                            }
                            theTextItems[j].text = load.ToString();
                        }
                        else if (theTextItems[j].name == ratioName)
                        {
                            int numOn = 0;
                            for (int i = 0; i < facilities.Count; i++)
                            {
                                TextMeshProUGUI TheText = facilities[i].GetComponent<TextMeshProUGUI>();
                                if (TheText.text == "1") // 1 = on and 0 = off
                                    numOn++;
                            }
                            theTextItems[j].text = numOn.ToString() + "/" + (facilities.Count).ToString();
                            allDone = true;
                        }
                        else if (allDone)
                            break;
                    }
                }
                GalaxyMenuUIController.Instance.UpdateSystemPowerLoad(this);
            }
        }

        public int GetBuildTimeDuration(StarSysFacilities starSysFacilities)
        {
            int timeDuration = 1;
            TechLevel ourTechLevel = this.StarSysData.CurrentCivController.CivData.TechLevel;
            switch (starSysFacilities)
            {
                case StarSysFacilities.PowerPlanet:
                    timeDuration = this.StarSysData.PowerPlantData.BuildDuration;
                    break;
                case StarSysFacilities.Factory:
                    timeDuration = this.StarSysData.FactoryData.BuildDuration;
                    break;
                case StarSysFacilities.Shipyard:
                    timeDuration = this.StarSysData.ShipyardData.BuildDuration;
                    break;
                case StarSysFacilities.ShieldGenerator:
                    timeDuration = this.StarSysData.ShieldGeneratorData.BuildDuration;
                    break;
                case StarSysFacilities.OrbitalBattery:
                    timeDuration = this.StarSysData.OrbitalBatteryData.BuildDuration;
                    break;
                case StarSysFacilities.ResearchCenter:
                    timeDuration = this.StarSysData.ResearchCenterData.BuildDuration;
                    break;
                default:
                    break;
            }
            return timeDuration;
            //ToD use tech level to set features of system production, defence....
        }
        public void DoHabitalbeSystemUI(CivController discoveringCiv)
        {
            if (discoveringCiv != null)
            {
                HabitableSysUIController.Instance.LoadHabitableSysUI(this, discoveringCiv);
            }
        }

        public void UpdateOwner(CivEnum newOwner) // system captured or colonized
        {
            starSysData.CurrentOwnerCivEnum = newOwner;
        }
        private void OnMouseDown()
        {
            Ray ray = galaxyEventCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                GameObject sysGO = hit.collider.gameObject;
                // what a Star System Controller does with a hit
                if (sysGO.tag != "GalaxyImage") // && GameController.Instance.AreWeLocalPlayer(this.StarSysData.CurrentOwnerCivEnum))
                {
                    if (GalaxyMenuUIController.Instance.MouseClickSetsDestination == true)
                    {
                        FleetController theFleetConLookingForDestination = MousePointerChanger.Instance.fleetConBehindGalaxyMapDestinationCursor;
                        theFleetConLookingForDestination.FleetData.Destination = sysGO;
                        theFleetConLookingForDestination.SetAsDestinationInUI(sysGO);
                    }
                    else if (this.StarSysData.CurrentCivController.CivData.CivEnum == GameController.Instance.GameData.LocalPlayerCivEnum)
                    {
                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 0, "FactoryLoad", "NumFactoryRatio", StarSysFacilities.Factory);
                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 0, "YardLoad", "NumYardsOnRatio", StarSysFacilities.Shipyard);
                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 0, "ShieldLoad", "NumShieldRatio", StarSysFacilities.ShieldGenerator);
                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 0, "OBLoad", "NumOBRatio", StarSysFacilities.OrbitalBattery);
                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 0, "ResearchLoad", "NumResearchRatio", StarSysFacilities.ResearchCenter);
                        GalaxyMenuUIController.Instance.UpdateSystemPowerLoad(this);
                        GalaxyMenuUIController.Instance.OpenMenu(Menu.ASystemMenu, sysGO); // set the system UI to this system
                    }
                    else if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivContoller, this.StarSysData.CurrentCivController))
                    { // this is a system local player does not own but we know them
                        EncounterManager.Instance.ClickOnTheirSystem(CivManager.Instance.LocalPlayerCivContoller, this);

                        //DiplomacyUIController.Instance.LoadDiplomacyUI(DiplomacyManager.Instance.ReturnADiplomacyController(this.StarSysData.CurrentCivController, CivManager.Instance.LocalPlayerCivContoller));
                        //var diplomacyController = DiplomacyManager.Instance.ReturnADiplomacyController(this.StarSysData.CurrentCivController, CivManager.Instance.LocalPlayerCivContoller);
                        //GalaxyMenuUIController.Instance.OpenMenu(Menu.ADiplomacyMenu, diplomacyController.DiplomacyUIGameObject);
                    }
                }
            }
        }


        public void OnEnable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnRandomSpecialEvent += DoDisaster;
        }
        public void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnRandomSpecialEvent -= DoDisaster;
        }
        private void DoDisaster(TrekRandomEventSO randomSpecialEvent)
        {
            if (randomSpecialEvent != null)
            {
                Debug.Log("Special event reached StarSystemController: " + randomSpecialEvent.eventName + " on oneInXChance " +
                    randomSpecialEvent.oneInXChance + " TrekRandomEvents: " + randomSpecialEvent.trekEventType +
                    " parameter: " + randomSpecialEvent.eventParameter);
                // Add your logic to handle the special event here
                switch (randomSpecialEvent.trekEventType)
                {
                    case TrekRandomEvents.AsteroidHit:
                        {
                            // ToDo: Do Disaster code for each disaster 
                            Debug.Log("******** Asteroid ***********"); ;
                            break;
                        }
                    case TrekRandomEvents.Pandemic:
                        {
                            Debug.Log("********** PANDEMIC **********");
                            break;
                        }
                    case TrekRandomEvents.SuperVolcano:
                        {
                            Debug.Log("********** SUPER VOLCANO **********");
                            break;
                        }
                    case TrekRandomEvents.GamaRayBurst:
                        {
                            Debug.Log("********** GAMERAY BURST **********");
                            break;
                        }

                    case TrekRandomEvents.SeismicEvent:
                        {
                            Debug.Log("********** SEISMEIC EVENT **********");
                            break;
                        }
                    case TrekRandomEvents.Teribals:
                        {
                            Debug.Log("********** TERIBAL TROUBLE **********");
                            break;
                        }
                    default:
                        break;
                }
            }
        }
        public void BuildClick(StarSysController sysCon) // open build and ship build list UI
        {
            StarSysManager.Instance.InstantiateSysBuildListUI(this);
            GalaxyMenuUIController.Instance.OpenMenu(Menu.BuildMenu, null);

        }
        public void ShipClick(StarSysController sysCon) // open build and ship build list UI
        {
            StarSysManager.Instance.InstantiateSysBuildListUI(this);
            GalaxyMenuUIController.Instance.OpenMenu(Menu.BuildMenu, null);
        }
        public void FacilityOnClick(StarSysController sysCon, string name)
        {
            if (this == sysCon)
            {
                switch (name)
                {
                    case "FactoryButtonOn":
                        {
                            // Do we have enough power to turn a factory on?
                            if (this.StarSysData.TotalSysPowerLoad + StarSysData.FactoryData.PowerLoad >
                                    this.StarSysData.TotalSysPowerOutput)
                            {
                                GalaxyMenuUIController.Instance.FlashPowerOverload();
                                break;
                            }
                            for (int i = 0; i < this.StarSysData.Factories.Count; i++)
                            {
                                if (StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text == "0")
                                {
                                    if (this.StarSysData.TotalSysPowerLoad + StarSysData.FactoryData.PowerLoad <=
                                        this.StarSysData.TotalSysPowerOutput)
                                    {
                                        this.StarSysData.TotalSysPowerLoad += StarSysData.FactoryData.PowerLoad;
                                        StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text = "1";
                                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 1, "FactoryLoad", "NumFactoryRatio", StarSysFacilities.Factory);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    case "FactoryButtonOff":
                        {
                            for (int i = 0; i < this.StarSysData.Factories.Count; i++)
                            {
                                if (StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text == "1")
                                {
                                    StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text = "0";
                                    this.StarSysData.TotalSysPowerLoad -= StarSysData.FactoryData.PowerLoad;
                                    GalaxyMenuUIController.Instance.UpdateFacilityUI(this, -1, "FactoryLoad", "NumFactoryRatio", StarSysFacilities.Factory);
                                    break;
                                }
                            }
                        }
                        break;
                    case "YardButtonOn":
                        {
                            if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShipyardData.PowerLoad >
                                    this.StarSysData.TotalSysPowerOutput)
                            {
                                GalaxyMenuUIController.Instance.FlashPowerOverload();
                                break;
                            }
                            for (int i = 0; i < this.StarSysData.Shipyards.Count; i++)
                            {
                                if (StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text == "0")
                                {
                                    if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShipyardData.PowerLoad <=
                                        this.StarSysData.TotalSysPowerOutput)
                                    {
                                        StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text = "1";
                                        this.StarSysData.TotalSysPowerLoad += StarSysData.ShipyardData.PowerLoad;
                                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 1, "YardLoad", "NumYardsOnRatio", StarSysFacilities.Shipyard);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    case "YardButtonOff":
                        {

                            for (int i = 0; i < this.StarSysData.Shipyards.Count; i++)
                            {
                                if (StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text == "1")
                                {
                                    StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text = "0";
                                    this.StarSysData.TotalSysPowerLoad -= StarSysData.ShipyardData.PowerLoad;
                                    GalaxyMenuUIController.Instance.UpdateFacilityUI(this, -1, "YardLoad", "NumYardsOnRatio", StarSysFacilities.Shipyard);
                                    break;
                                }
                            }
                        }
                        break;
                    case "ShieldButtonOn":
                        {
                            if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShieldGeneratorData.PowerLoad >
                                    this.StarSysData.TotalSysPowerOutput)
                            {
                                GalaxyMenuUIController.Instance.FlashPowerOverload();
                                break;
                            }
                            for (int i = 0; i < this.StarSysData.ShieldGenerators.Count; i++)
                            {
                                if (StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text == "0")
                                {
                                    if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShieldGeneratorData.PowerLoad <=
                                        this.StarSysData.TotalSysPowerOutput)
                                    {
                                        StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text = "1";
                                        this.StarSysData.TotalSysPowerLoad += StarSysData.ShieldGeneratorData.PowerLoad;
                                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 1, "ShieldLoad", "NumShieldRatio", StarSysFacilities.ShieldGenerator);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    case "ShieldButtonOff":
                        {
                            for (int i = 0; i < this.StarSysData.ShieldGenerators.Count; i++)
                            {
                                if (StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text == "1")
                                {
                                    StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text = "0";
                                    this.StarSysData.TotalSysPowerLoad -= StarSysData.ShieldGeneratorData.PowerLoad;
                                    GalaxyMenuUIController.Instance.UpdateFacilityUI(this, -1, "ShieldLoad", "NumShieldRatio", StarSysFacilities.ShieldGenerator);
                                    break;
                                }
                            }
                        }
                        break;
                    case "OBButtonOn":
                        {
                            if (this.StarSysData.TotalSysPowerLoad + StarSysData.OrbitalBatteryData.PowerLoad >
                                       this.StarSysData.TotalSysPowerOutput)
                            {
                                GalaxyMenuUIController.Instance.FlashPowerOverload();
                                break;
                            }
                            for (int i = 0; i < this.StarSysData.OrbitalBatteries.Count; i++)
                            {
                                if (StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text == "0")
                                {
                                    if (this.StarSysData.TotalSysPowerLoad + StarSysData.OrbitalBatteryData.PowerLoad <=
                                        this.StarSysData.TotalSysPowerOutput)
                                    {
                                        StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text = "1";
                                        this.StarSysData.TotalSysPowerLoad += StarSysData.OrbitalBatteryData.PowerLoad;
                                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 1, "OBLoad", "NumOBRatio", StarSysFacilities.OrbitalBattery);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    case "OBButtonOff":
                        {
                            for (int i = 0; i < this.StarSysData.OrbitalBatteries.Count; i++)
                            {
                                if (StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text == "1")
                                {
                                    StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text = "0";
                                    this.StarSysData.TotalSysPowerLoad -= StarSysData.OrbitalBatteryData.PowerLoad;
                                    GalaxyMenuUIController.Instance.UpdateFacilityUI(this, -1, "OBLoad", "NumOBRatio", StarSysFacilities.OrbitalBattery);
                                    break;
                                }
                            }
                        }
                        break;
                    case "ResearchButtonOn":
                        {
                            if (this.StarSysData.TotalSysPowerLoad + StarSysData.ResearchCenterData.PowerLoad >
                                     this.StarSysData.TotalSysPowerOutput)
                            {
                                GalaxyMenuUIController.Instance.FlashPowerOverload();
                                break;
                            }
                            for (int i = 0; i < this.StarSysData.ResearchCenters.Count; i++)
                            {
                                if (StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text == "0")
                                {
                                    if (this.StarSysData.TotalSysPowerLoad + StarSysData.ResearchCenterData.PowerLoad <=
                                        this.StarSysData.TotalSysPowerOutput)
                                    {
                                        StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text = "1";
                                        this.StarSysData.TotalSysPowerLoad += StarSysData.ResearchCenterData.PowerLoad;
                                        GalaxyMenuUIController.Instance.UpdateFacilityUI(this, 1, "ResearchLoad", "NumResearchRatio", StarSysFacilities.ResearchCenter);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    case "ResearchButtonOff":
                        {
                            for (int i = 0; i < this.StarSysData.ResearchCenters.Count; i++)
                            {
                                if (StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text == "1")
                                {
                                    StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text = "0";
                                    this.StarSysData.TotalSysPowerLoad -= StarSysData.ResearchCenterData.PowerLoad;
                                    GalaxyMenuUIController.Instance.UpdateFacilityUI(this, -1, "ResearchLoad", "NumResearchRatio", StarSysFacilities.ResearchCenter);
                                    break;
                                }
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
        }
        private void OnDestroy()
        {
            TimeManager.Instance.OnRandomSpecialEvent -= DoDisaster;
            OnOffSysFacilityEvents.current.FacilityOnClick -= FacilityOnClick;
        }
        public void SetBuildProgress(float progress)
        {
            //SliderBuildProgress.gameObject.SetActive(true);
            //SliderBuildProgress.enabled = true;
            //SliderBuildProgress.gameObject.transform.SetAsLastSibling();
            SliderBuildProgress.value = progress;

        }
        public void SetShipBuildProgress(float shipProgress)
        {
            //ShipSliderBuildProgress.gameObject.SetActive(true);
            //ShipSliderBuildProgress.enabled = true;
            //ShipSliderBuildProgress.gameObject.transform.SetAsLastSibling();
            ShipSliderBuildProgress.value = shipProgress;

        }
    }

}
