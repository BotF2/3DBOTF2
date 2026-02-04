// Ignore Spelling: Sys Habitalbe Unregister

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
        private GameObject goForPowerOverload;
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
        private GalaxyMenuUIController galaxyUI;
        private GalaxyMenuUIController GalaxyUI
        {
            get
            {
                if (galaxyUI == null)
                    galaxyUI = GalaxyMenuUIController.Instance;
                return galaxyUI;
            }
        }
        private StarSysMenuUIController starSysUI;
        private StarSysMenuUIController StarSysUI
        {
            get
            {
                if (starSysUI == null)
                    starSysUI = StarSysMenuUIController.Instance;
                return starSysUI;
            }
        }

        private GameController gameController;
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
        private bool deployNotMerge = true; // true=deploy, false=merge
        private void Awake()
        {
            gameController = GameController.Instance;
            if (sysBuildQueueList == null)
                sysBuildQueueList = new List<Transform>();

            if (shipBuildQueueList == null)
                shipBuildQueueList = new List<Transform>();
        }


        //************ToDo, next steps:***********
        //Pause / resume building
        //Speed modifiers(tech, civ traits)
        //Save/load coroutine state
        //Replace Update() with OnTransformChildrenChanged()
        //private void Awake()

        private void Start()
        {
            galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
            canvasToolTip.worldCamera = galaxyEventCamera;
            if (StarSysUI != null)
                goForPowerOverload = StarSysUI.PowerOverloadImage;
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
            var clickedStarSysCon = GetComponentInParent<StarSysController>();

            if (clickedStarSysCon == null) return;

            switch (GalaxyUI.CurrentClickMode)
            {
                case GalaxyClickMode.Normal:
                    GalaxyMenuUIController.Instance.CloseShipDeploy();
                    HandleNormalClick(clickedStarSysCon);
                    break;
                case GalaxyClickMode.SetDestination:
                    HandleDestinationClick(clickedStarSysCon);
                    break;
                case GalaxyClickMode.SelectForShipDeploy:
                    if (gameController.AreWeLocalPlayer(clickedStarSysCon.StarSysData.CurrentOwnerCivEnum))
                        HandleShipDeploySelection(clickedStarSysCon);
                    break;
                case GalaxyClickMode.SelectForShipMerge:
                    if (gameController.AreWeLocalPlayer(this.StarSysData.CurrentOwnerCivEnum))
                        HandleShipMergeSelection(clickedStarSysCon);
                    break;
            }
        }

        private void HandleShipMergeSelection(StarSysController clickedStarSysCon)
        {
            if (clickedStarSysCon != this) return;
            deployNotMerge = false;
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatSystemIsSelectedForShipMerge(clickedStarSysCon);
            var fleetLooking = galaxyUI.FleetLookingForShipMerge;
            var starSysLooking = galaxyUI.StarSystLookingForShipMerge;
            if (fleetLooking == null)
            {
                var aSysView = StarSysUI.ASystemMenuView.gameObject;
                aSysView.SetActive(true);
                clickedStarSysCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                StarSysUIGameObject.transform.SetAsLastSibling();
            }
            else if (starSysLooking == null)
            {
                var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
                aFleetView.SetActive(true);
                clickedStarSysCon.StarSysUIGameObject.transform.SetParent(aFleetView.transform, false);
                starSysUIGameObject.transform.SetAsLastSibling();
            }
            //ShipDeployMenuUIController.Instance.SetUpTopShipLists();
            ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedStarSysCon, deployNotMerge);
            ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();
        }
        private void HandleShipDeploySelection(StarSysController clickedSystemCon)
        {
            if (clickedSystemCon != this) return;
            //SettingUpNewFleet = false;
            deployNotMerge = true;
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatSystemIsSelectedForShipDeploy(clickedSystemCon);
            var fleetLooking = galaxyUI.FleetLookingForShipDeploy;
            var starSysLooking = galaxyUI.StarSystLookingForShipDeploy;
            if (fleetLooking == null)
            {
                var aSysView = StarSysUI.ASystemMenuView.gameObject;
                aSysView.SetActive(true);
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                StarSysUIGameObject.transform.SetAsLastSibling();
            }
            else if (starSysLooking == null)
            {
                var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
                aFleetView.SetActive(true);
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aFleetView.transform, false);
                starSysUIGameObject.transform.SetAsLastSibling();

            }
            ShipDeployMenuUIController.Instance.SetUpTopShipLists();
            ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedSystemCon, deployNotMerge);
            ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();
        }
        private void HandleDestinationClick(StarSysController clickedSystemCon)
        {
            //SettingUpNewFleet = false;
            var fleetLookingForDestination = GalaxyUI.FleetLookingForDestination.GetComponent<FleetController>();
            if (fleetLookingForDestination != null)
            {
                fleetLookingForDestination.FleetData.Destination = clickedSystemCon.gameObject;
                fleetLookingForDestination.SetAsDestinationInUI(clickedSystemCon.gameObject);
            }
        }


        public void LoadAStarSystem()
        {
            HandleNormalClick(this);
        }
        private void HandleNormalClick(StarSysController clickedSystemCon)
        {
            //SettingUpNewFleet = false;
            GalaxyUI.CloseShipDeploy();
            if (clickedSystemCon == null) return;
            if (clickedSystemCon == this)
            {
                if (gameController.AreWeLocalPlayer(clickedSystemCon.StarSysData.CurrentOwnerCivEnum))
                {
                    StarSysUI.SetActiveSetParentUIGO(this);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);
                    //StarSysUI.UpdateSystemPowerBalance(this);
                    GalaxyUI.OpenMenu(Menu.ASystemMenu, clickedSystemCon.gameObject); // set the system UI to this system
                    //StarSysMenuUIController.Instance.lastStarSysController = this;
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
            GalaxyUI.OpenMenu(Menu.BuildMenu, null);

        }
        public void ShipClick(StarSysController sysCon) // open build and ship build list UI
        {
            StarSysManager.Instance.InstantiateSysBuildListUI(this);
            GalaxyUI.OpenMenu(Menu.BuildMenu, null);
        }
        public void FactoryButtonOnClicked(StarSysController starSysCon)
        {

            if (starSysCon != null && this == starSysCon)
            {
                if (starSysUI == null)
                    starSysUI = StarSysMenuUIController.Instance;
                if (goForPowerOverload == null)
                    goForPowerOverload = starSysUI.PowerOverloadImage;
                // Do we have enough power to turn a factory on?
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.FactoryData.PowerLoad >
                        this.StarSysData.TotalSysPowerOutput)
                {
                    CoroutineRunner.FlashPowerOverload(goForPowerOverload);
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
                            StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.Factory);
                            break;
                        }
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void FactoryButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.Factories.Count; i++)
                {
                    if (StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.FactoryData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.Factory);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void YardButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShipyardData.PowerLoad >
                        this.StarSysData.TotalSysPowerOutput)
                {
                    CoroutineRunner.FlashPowerOverload(goForPowerOverload);
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
                            StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.Shipyard);
                            break;
                        }
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void YardButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.Shipyards.Count; i++)
                {
                    if (StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.ShipyardData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.Shipyard);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void ShieldButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.ShieldGeneratorData.PowerLoad >
                        this.StarSysData.TotalSysPowerOutput)
                {
                    CoroutineRunner.FlashPowerOverload(goForPowerOverload);

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
                            StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.ShieldGenerator);
                            break;
                        }
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void ShieldButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.ShieldGenerators.Count; i++)
                {
                    if (StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.ShieldGeneratorData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.ShieldGenerator);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void OBButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.OrbitalBatteryData.PowerLoad >
                            this.StarSysData.TotalSysPowerOutput)
                {
                    CoroutineRunner.FlashPowerOverload(goForPowerOverload);
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
                        StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.OrbitalBattery);
                        break;
                    }
                }
            }
            StarSysUI.UpdateSystemPowerBalance(this);
        }
        public void OBButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.OrbitalBatteries.Count; i++)
                {
                    if (StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.OrbitalBatteryData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.OrbitalBattery);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void ResearchButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.ResearchCenterData.PowerLoad >
                         this.StarSysData.TotalSysPowerOutput)
                {
                    CoroutineRunner.FlashPowerOverload(goForPowerOverload);
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
                            StarSysUI.UpdateFacilityUI(this, 1, StarSysFacilityType.ResearchCenter);
                            break;
                        }
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }
        public void ResearchButtonOffClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                for (int i = 0; i < this.StarSysData.ResearchCenters.Count; i++)
                {
                    if (StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text == "1")
                    {
                        StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text = "0";
                        this.StarSysData.TotalSysPowerLoad -= StarSysData.ResearchCenterData.PowerLoad;
                        StarSysUI.UpdateFacilityUI(this, -1, StarSysFacilityType.ResearchCenter);
                        break;
                    }
                }
                StarSysUI.UpdateSystemPowerBalance(this);
            }
        }

        private void OnDestroy()
        {
            TimeManager.Instance.OnRandomSpecialEvent -= DoDisaster;
            // OnOffSysFacilityEvents.current.FacilityOnClick -= FacilityOnClick;
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


        internal void RemoveFromShipList(ShipController shipController)
        {
            // Remove from model list
            if (shipController == null) return;
            StarSysData.RemoveFromShipList(shipController);

            // If the ship controller GO is parented to this system (under the GalaxyCenter go), unparent it to scene root.
            if (shipController.transform.IsChildOf(transform))
                shipController.transform.SetParent(null, worldPositionStays: true);
        }

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
