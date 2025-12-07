// Ignore Spelling: Sys Habitalbe

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
        public StarSysBuildManager SysBuildManager;
        private StarSysData starSysData;
        public int PlayerID; // network player ID, not used in single player
        public StarSysData StarSysData { get { return starSysData; } set { starSysData = value; } }
        [SerializeField]
        private GameObject starSysUIGameObject; //The instantiated system UI for this system. a prefab clone, not a class but a game object
        public GameObject StarSysUIGameObject { get { return starSysUIGameObject; } set { starSysUIGameObject = value; } }
        public bool SettingUpNewFleet = false;
        private Camera galaxyEventCamera;
        [SerializeField]
        private Canvas canvasToolTip;
        public static event Action<TrekRandomEventSO> TrekEventDisasters;
        public GridLayoutGroup BuildListGridLayoutGroup;
        private BuildQueueWatcher buildQueueWatcher;
        public GridLayoutGroup ShipListGridLayoutGroup;
        private ShipQueueWatcher shipQueueWatcher;
        [SerializeField]
        internal List<Transform> sysBuildQueueList;
        private Transform buildingItem;
        [SerializeField]
        internal List<Transform> shipBuildQueueList;
        private Transform shipBuildingItem;

        public GameObject ShipListUIParent
        {
            get => StarSysData?.ShipListUIParent;
            set
            {
                if (StarSysData != null)
                    StarSysData.ShipListUIParent = value;
                ShipManager.Instance?.ProcessPendingShipUIs();
            }
        }
        //ToDo, next steps:
        //Pause / resume building
        //Speed modifiers(tech, civ traits)
        //Save/load coroutine state
        //Replace Update() with OnTransformChildrenChanged()
        private void Awake()
        {
            if (sysBuildQueueList == null)
                sysBuildQueueList = new List<Transform>();

            if (shipBuildQueueList == null)
                shipBuildQueueList = new List<Transform>();
        }
        private void Start()
        {
            galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
            canvasToolTip.worldCamera = galaxyEventCamera;
            OnOffSysFacilityEvents.current.FacilityOnClick += FacilityOnClick;// subscribe method to the event += () => Debug.Log("Action Invoked!");
        }
        private void OnTransformChildrenChanged() //Unity automatically invokes when the transform hierarchy of the Controller GameObject changes.
        {  // This UI Queue is in the SysBuildUIListPanel prefab and not a child of StarSysController, BuildQueueWatcher helps call OnTransformChildrenChanged()
            if (BuildListGridLayoutGroup != null)
                GridFactoryQueueUpdate();

            if (ShipListGridLayoutGroup != null)
                GridShipQueueUpdate();
        }
        public void RegisterBuildQueueWatcher(BuildQueueWatcher watcher)
        {
            buildQueueWatcher = watcher;
        }

        public void UnregisterBuildQueueWatcher(BuildQueueWatcher watcher)
        {
            if (buildQueueWatcher == watcher)
                buildQueueWatcher = null;
        }

        public void RegisterShipQueueWatcher(ShipQueueWatcher watcher)
        {
            shipQueueWatcher = watcher;
        }

        public void UnregisterShipQueueWatcher(ShipQueueWatcher watcher)
        {
            if (shipQueueWatcher == watcher)
                shipQueueWatcher = null;
        }

        public void GridFactoryQueueUpdate()
        {
            if (BuildListGridLayoutGroup == null)
                return;
            // 1️⃣ Sync queue list FIRST
            foreach (Transform child in BuildListGridLayoutGroup.transform)
            {
                if (!sysBuildQueueList.Contains(child))
                    sysBuildQueueList.Add(child);
            }

            sysBuildQueueList.RemoveAll(t => t == null || t.parent != BuildListGridLayoutGroup.transform);

            sysBuildQueueList = sysBuildQueueList
                .OrderByDescending(t => t.localPosition.y)
                .ThenBy(t => t.localPosition.x)
                .ToList();

            // 2️⃣ THEN maybe start coroutine
            if (!SysBuildManager.IsBuildingFacility && sysBuildQueueList.Count > 0)
            {
                SysBuildManager.StartNextFacilityBuildIfAny();
            }
        }

        public void GridShipQueueUpdate()
        {
            if (ShipListGridLayoutGroup == null)
                return;
            foreach (Transform child in ShipListGridLayoutGroup.transform)
            {
                if (!shipBuildQueueList.Contains(child))
                    shipBuildQueueList.Add(child);
            }

            shipBuildQueueList.RemoveAll(t => t == null || t.parent != ShipListGridLayoutGroup.transform);

            shipBuildQueueList = shipBuildQueueList
                .OrderByDescending(t => t.localPosition.y)
                .ThenBy(t => t.localPosition.x)
                .ToList();

            if (!SysBuildManager.IsBuildingShip && shipBuildQueueList.Count > 0)
            {
                SysBuildManager.StartNextShipBuildIfAny();
            }
        }

        internal void AddSysFacility(GameObject faciltyGO, string loadName, string ratioName, StarSysFacilities facilityType)
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
                    if (GameController.Instance.AreWeLocalPlayer(this.StarSysData.CurrentOwnerCivEnum))
                        HandleShipExchangeSelection(this);
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
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatSystemIsSelectedForShipDeploy(this);
            var fleetLooking = galaxyUI.FleetLookingForShipDeploy;
            var starSysLooking = galaxyUI.StarSystLookingForShipDeploy;
            if (fleetLooking == null)
            {
                var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
                aSysView.SetActive(true);
                this.starSysUIGameObject.transform.SetParent(aSysView.transform, false);
                starSysUIGameObject.transform.SetAsLastSibling();
            }
            else if (starSysLooking == null)
            {
                var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
                aFleetView.SetActive(true);
                this.starSysUIGameObject.transform.SetParent(aFleetView.transform, false);
                starSysUIGameObject.transform.SetAsLastSibling();

            }
            ShipDeployMenuUIController.Instance.SetUpTopShipLists();
            ShipDeployMenuUIController.Instance.SetUpBottomShipLists(this);
            ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();
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

        internal void InitNewFleet()
        {
            // ToDo: implement fleet creation in system
        }

        internal void RemoveFromShipList(ShipController shipController)
        {
            // Remove from model list
            if (shipController == null) return;
            StarSysData.RemoveFromShipList(shipController);

            // If the ship controller GO is parented to this system (under the GalaxyCenter go), unparent it to scene root.
            if (shipController.transform.IsChildOf(transform))
                shipController.transform.SetParent(null, worldPositionStays: true);
        }

        // New: add ship to this star system (gameplay object + model + UI)
        public void AddToShipList(ShipController shipController)
        {
            if (shipController == null) return;

            // Reparent gameplay ship under this star system in the scene
            shipController.transform.SetParent(transform, worldPositionStays: true);

            // Add to model list
            if (!StarSysData.ShipsList.Contains(shipController))
                StarSysData.AddToShipList(shipController);

            // Move UI element under system UI parent if available
            if (shipController.ShipListUIGameObject != null && StarSysData.ShipListUIParent != null)
                shipController.ShipListUIGameObject.transform.SetParent(StarSysData.ShipListUIParent.transform, false);
        }
    }
}
