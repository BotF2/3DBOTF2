using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        public int PlayerID; // network player ID, not used in single player
        public StarSysData StarSysData { get { return starSysData; } set { starSysData = value; } }
        [SerializeField]
        private GameObject starSysUIGameObject; //The instantiated system UI for this system. a prefab clone, not a class but a game object

        private GameObject starSysShipUIGameObject;// instantiated by StarSysManager from the prefab and added to StarSysController
        public GameObject StarSysRightSideShipsUIGameObject { get { return starSysShipUIGameObject; } set { starSysShipUIGameObject = value; } }
        public GameObject StarSysUIGameObject { get { return starSysUIGameObject; } set { starSysUIGameObject = value; } }
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

        private void Start()
        {
            galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
            canvasToolTip.worldCamera = galaxyEventCamera;
            TimeManager.Instance.OnRandomSpecialEvent += DoDisaster;
            OnOffSysFacilityEvents.current.FacilityOnClick += FacilityOnClick;// subscribe method to the event += () => Debug.Log("Action Invoked!");
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
            // Are we building anything 
            if (building && TimeToBuild > 0)
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
                    switch (sysBuildQueueList[0].gameObject.GetComponentInChildren<FactoryBuildItemDrag>().FacilityType)
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
                    imageTransform.SetParent(imageTransform.GetComponent<FactoryBuildItemDrag>().originalParent, false);
                    if (imageTransform.parent.childCount > 1)
                    {
                        Destroy(imageTransform.gameObject);
                    }
                    sysBuildQueueList.Remove(sysBuildQueueList[0]);
                    StarSysMenuUIController.Instance.UpdateSystemPowerLoad(this);
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

                    switch (shipBuildQueueList[0].gameObject.GetComponentInChildren<SystemBuildShipDrag>().ShipType)
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
                    ShipManager.Instance.BuildShipInSystem(shipType, this);

                    var imageTransform = shipBuildQueueList[0];
                    imageTransform.SetParent(imageTransform.GetComponent<SystemBuildShipDrag>().originalParent, false);
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
            // update star system controller sysBuildQueue list to match buildListBridLayoutGroup.tranform children
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
                    TimeToBuild = GetBuildTimeDuration(buildingItem.gameObject.GetComponentInChildren<FactoryBuildItemDrag>().FacilityType);
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
            // update star system controller list to match buildShipListBridLayoutGroup.tranform children
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
                    var shipBuildableItem = shipBuildingItem.gameObject.GetComponentInChildren<SystemBuildShipDrag>();
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
                StarSysMenuUIController.Instance.UpdateSystemPowerLoad(this);
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
            //ToD use tech level to set features of system production, defense....
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

            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            GameObject sysGO = hit.collider.gameObject;
            if (sysGO.CompareTag("GalaxyImage")) return;

            StarSysController clickedSystemCon = sysGO.GetComponentInChildren<StarSysController>();
            var galaxyUI = GalaxyMenuUIController.Instance;

            switch (galaxyUI.CurrentClickMode)
            {
                case GalaxyClickMode.Normal:
                    HandleNormalClick(clickedSystemCon);
                    break;
                case GalaxyClickMode.SetDestination:
                    HandleDestinationClick(clickedSystemCon);
                    break;
                case GalaxyClickMode.SelectForShipExchange:
                    HandleShipExchangeSelection(clickedSystemCon);
                    break;
            }
        }

        private void HandleDestinationClick(StarSysController clickedSystemCon)
        {
            var fleetLookingForDestination = GalaxyMenuUIController.Instance.FleetLookingForDestination.GetComponent<FleetController>();
            if (fleetLookingForDestination != null)
            {
                fleetLookingForDestination.FleetData.Destination = clickedSystemCon.gameObject;
                fleetLookingForDestination.SetAsDestinationInUI(clickedSystemCon.gameObject);
            }
        }

        private void HandleShipExchangeSelection(StarSysController clickedSystemCon)
        {
            if (clickedSystemCon != this) return;
            MousePointerChanger.Instance.ResetCursor();
            FleetMenuUIController.Instance.AFleetMenuView.gameObject.SetActive(true);
            var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
            this.starSysUIGameObject.transform.Translate(new Vector3(0, -200, 0));
            this.starSysUIGameObject.transform.SetParent(aFleetView.transform, false);

        }

        private void HandleNormalClick(StarSysController clickedSystemCon)
        {
            GalaxyMenuUIController.Instance.CloseButtonPressed();
            if (clickedSystemCon == null) return;
            if (clickedSystemCon == this)
            {
                if (GameController.Instance.AreWeLocalPlayer(clickedSystemCon.StarSysData.CurrentOwnerCivEnum))
                {
                    var starSysUI = StarSysMenuUIController.Instance;
                    starSysUI.SetActiveSetParentUIGO(this);
                    starSysUI.UpdateFacilityUI(this, 0, "FactoryLoad", "NumFactoryRatio", StarSysFacilities.Factory);
                    starSysUI.UpdateFacilityUI(this, 0, "YardLoad", "NumYardsOnRatio", StarSysFacilities.Shipyard);
                    starSysUI.UpdateFacilityUI(this, 0, "ShieldLoad", "NumShieldRatio", StarSysFacilities.ShieldGenerator);
                    starSysUI.UpdateFacilityUI(this, 0, "OBLoad", "NumOBRatio", StarSysFacilities.OrbitalBattery);
                    starSysUI.UpdateFacilityUI(this, 0, "ResearchLoad", "NumResearchRatio", StarSysFacilities.ResearchCenter);
                    starSysUI.UpdateSystemPowerLoad(this);
                    GalaxyMenuUIController.Instance.OpenMenu(Menu.ASystemMenu, this.gameObject); // set the system UI to this system
                }
                else if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivContoller, this.StarSysData.CurrentCivController))
                { // this is a system local player does not own but we know them
                    DiplomacyManager.Instance.ResolveDiplomacyForClickSystemWeKnow(CivManager.Instance.LocalPlayerCivContoller, this);
                }
            }
        }
        void OnTriggerEnter(Collider collider) // Not using OnCollisionEnter....
        {
            //bool weAreLocalPlayer = false;
            //if (this.StarSysData.CurrentCivController != null)
            //    weAreLocalPlayer = GameController.Instance.AreWeLocalPlayer(this.StarSysData.CurrentOwnerCivEnum);


            //if (collider.gameObject.TryGetComponent(out FleetController hitFleetCon))
            //{

            ////    if (StarSysData.CurrentOwnerCivEnum != hitFleetCon.FleetData.CivEnum)//if not one of ours
            ////    {
            ////        EncounterManager.Instance.ResolveEncounterWithOtherCiv(this, hitFleetCon);

            ////        EncounterUnknownFleetGetNameAndSprite(collider.gameObject); // set active sprite and name

            ////        if (hitFleetCon.FleetData.Destination == this.gameObject) // they are coming for us
            ////        {
            ////            ClickCancelDestinationButton(hitFleetCon); // they stop

            ////            CloseUnLoadFleetUI(); // need more code to handle this encounter 
            ////        }

            ////    }
            ////    else //our fleet
            ////    {
            ////        // do ships?
            ////        OnADestinationThatIsOurOtherFleet(hitFleetCon); // we are the same civ fleets, do ships?
            ////    }

            ////}
            ////else if (collider.gameObject.TryGetComponent(out StarSysController sysCon)) // only the fleetController reports a collision for now, not the system
            ///
            ////{
            ////    if (isOurDestination)
            ////    {
            ////        ClickCancelDestinationButton(this); // we stop, cancel destination

            ////        if (this.FleetData.CivEnum != sysCon.StarSysData.CurrentOwnerCivEnum)
            ////        {
            ////            if (weAreLocalPlayer)
            ////            {
            ////                EncounterUnknownSystemShowName(collider.gameObject); // update Galaxy view to expose insignia/name
            ////            }
            ////            OnEnterForeignStarSystem(); // ToDo
            ////            EncounterManager.Instance.ResolveEncounter(this, sysCon);

            ////        }
            ////        else // ToDo: enter our system
            ////        {

            ////        }
            ////    }
            ////}
            ////else if (collider.gameObject.TryGetComponent(out PlayerDefinedTargetController Freddy))
            ////{
            ////    if (isOurDestination)
            ////    {
            ////        ClickCancelDestinationButton(this); // we stop, cancel destination
            ////        Destroy(collider.gameObject); // remove the player defined target
            ////    }
            //}
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
            var starSysUI = StarSysMenuUIController.Instance;
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
                                StarSysMenuUIController.Instance.FlashPowerOverload();
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
                                        StarSysMenuUIController.Instance.UpdateFacilityUI(this, 1, "FactoryLoad", "NumFactoryRatio", StarSysFacilities.Factory);
                                        break;
                                    }
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
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
                                    StarSysMenuUIController.Instance.UpdateFacilityUI(this, -1, "FactoryLoad", "NumFactoryRatio", StarSysFacilities.Factory);
                                    break;
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
                        }
                        break;
                    case "YardButtonOn":
                        {
                            if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShipyardData.PowerLoad >
                                    this.StarSysData.TotalSysPowerOutput)
                            {
                                StarSysMenuUIController.Instance.FlashPowerOverload();
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
                                        StarSysMenuUIController.Instance.UpdateFacilityUI(this, 1, "YardLoad", "NumYardsOnRatio", StarSysFacilities.Shipyard);
                                        break;
                                    }
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
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
                                    StarSysMenuUIController.Instance.UpdateFacilityUI(this, -1, "YardLoad", "NumYardsOnRatio", StarSysFacilities.Shipyard);
                                    break;
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
                        }
                        break;
                    case "ShieldButtonOn":
                        {
                            if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShieldGeneratorData.PowerLoad >
                                    this.StarSysData.TotalSysPowerOutput)
                            {
                                StarSysMenuUIController.Instance.FlashPowerOverload();
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
                                        StarSysMenuUIController.Instance.UpdateFacilityUI(this, 1, "ShieldLoad", "NumShieldRatio", StarSysFacilities.ShieldGenerator);
                                        break;
                                    }
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
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
                                    StarSysMenuUIController.Instance.UpdateFacilityUI(this, -1, "ShieldLoad", "NumShieldRatio", StarSysFacilities.ShieldGenerator);
                                    break;
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
                        }
                        break;
                    case "OBButtonOn":
                        {
                            if (this.StarSysData.TotalSysPowerLoad + StarSysData.OrbitalBatteryData.PowerLoad >
                                       this.StarSysData.TotalSysPowerOutput)
                            {
                                StarSysMenuUIController.Instance.FlashPowerOverload();
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
                                        StarSysMenuUIController.Instance.UpdateFacilityUI(this, 1, "OBLoad", "NumOBRatio", StarSysFacilities.OrbitalBattery);
                                        break;
                                    }
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
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
                                    StarSysMenuUIController.Instance.UpdateFacilityUI(this, -1, "OBLoad", "NumOBRatio", StarSysFacilities.OrbitalBattery);
                                    break;
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
                        }
                        break;
                    case "ResearchButtonOn":
                        {
                            if (this.StarSysData.TotalSysPowerLoad + StarSysData.ResearchCenterData.PowerLoad >
                                     this.StarSysData.TotalSysPowerOutput)
                            {
                                StarSysMenuUIController.Instance.FlashPowerOverload();
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
                                        StarSysMenuUIController.Instance.UpdateFacilityUI(this, 1, "ResearchLoad", "NumResearchRatio", StarSysFacilities.ResearchCenter);
                                        break;
                                    }
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
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
                                    StarSysMenuUIController.Instance.UpdateFacilityUI(this, -1, "ResearchLoad", "NumResearchRatio", StarSysFacilities.ResearchCenter);
                                    break;
                                }
                            }
                            starSysUI.UpdateSystemPowerLoad(this);
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
            SliderBuildProgress.value = progress;
        }
        public void SetShipBuildProgress(float shipProgress)
        {
            ShipSliderBuildProgress.value = shipProgress;
        }
        public void SelectedShipManageCursor(StarSysController starSysCon)
        {
            GalaxyMenuUIController.Instance.BeginShipExchange(this);
            GalaxyMenuUIController.Instance.SetClickMode(GalaxyClickMode.SelectForShipExchange);
            MousePointerChanger.Instance.SetShipExchangeCursor(this);

        }
        public void ClickCancelShipManageButton()
        {
            GalaxyMenuUIController.Instance.ClickCancelShipManageButton();
            GalaxyMenuUIController.Instance.ResetClickMode();
            //GalaxyMenuUIController.Instance.activeFleetOrSystemControllerForShipExchange = null;
            MousePointerChanger.Instance.ResetCursor();
        }

        internal void SelectedOtherForShips(StarSysController sysController)
        {
            //Implement ship transfer between sysController looking and selected system or fleet
            // GalaxyMenuUIController.Instance.TransferShipsBetweenSystemsForShipExchange(this, sysController);
        }

        internal void ClickCancelForShipsButton()
        {
            GalaxyMenuUIController.Instance.ClickCancelShipManageButton();
            GalaxyMenuUIController.Instance.ResetClickMode();
            MousePointerChanger.Instance.ResetCursor();
        }

        public void CleanupStarSysUIs()
        {
            foreach (var starSysCon in StarSysManager.Instance.StarSysControllerList)
            {
                if (starSysCon.StarSysUIGameObject == null)
                    continue;

                if (!starSysCon.StarSysUIGameObject.activeInHierarchy)
                {
                    starSysCon.StarSysUIGameObject = null;
                }
            }
        }
    }
}
