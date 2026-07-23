// Ignore Spelling: Sys Habitalbe Unregister
using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BOTF3D.Civilization;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Controlling Star System interactions while the matching StarSystemData class
    /// holds key info on status and for save game
    /// </summary>
    public class StarSysController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        //Fields
        public StarSysBuildManager StarSysBuildManager { get; set; }
        private StarSysData starSysData;
        public int PlayerID; // network player ID, not used in single player
        public StarSysData StarSysData { get { return starSysData; } set { starSysData = value; } }

        [SerializeField] // Make backing field serializable for Inspector
        private GameObject _starSysUIGameObject;

        public GameObject StarSysUIGameObject
        {
            get => _starSysUIGameObject;
            set
            {
                // ✅ Guard: never allow the 3D world object to be used as the UI object
                if (value != null && (value == this.gameObject || value.transform.IsChildOf(this.transform)))
                {
                    Debug.LogError($"❌ StarSysUIGameObject on '{name}' cannot be set to its own 3D GameObject or a child! Assignment blocked.");
                    return;
                }

                if (value != _starSysUIGameObject)
                {
                    // ... existing null-logging code stays here ...
                    _starSysUIGameObject = value;
                }
            }
        }


        private GameObject goForPowerOverload;
        public Camera GalaxyEventCamera { get; set; }
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
        internal List<Transform> sysShipBuildQueueList;
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

            if (sysShipBuildQueueList == null)
                sysShipBuildQueueList = new List<Transform>();
        }


        //************ToDo, next steps:***********
        //Pause / resume building
        //Speed modifiers(tech, civ traits)
        //Save/load coroutine state
        //Replace Update() with OnTransformChildrenChanged()
        //private void Awake()

        private void Start()
        {
            // ✅ Use assigned camera, fallback to find if not set
            if (GalaxyEventCamera == null)
            {
                GalaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
                Debug.LogWarning($"StarSysController {name}: Had to find camera in Start() - should be set by StarSysManager");
            }

            if (canvasToolTip != null && GalaxyEventCamera != null)
            {
                canvasToolTip.worldCamera = GalaxyEventCamera;
            }
            else if (canvasToolTip != null)
            {
                Debug.LogError($"StarSysController {name}: Cannot set tooltip camera - GalaxyEventCamera is NULL!");
            }

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

            // 2️⃣ Start build only when a turn is actively progressing
            if (!StarSysBuildManager.IsBuildingFacility && sysBuildQueueList.Count > 0 &&
                TimeManager.Instance?.TurnPhase == TurnPhase.TurnProgression)
            {
                StarSysBuildManager.StartNextFacilityBuildIfAny();
            }
        }


        public void GridShipQueueUpdate()
        {
            if (ShipListGridLayoutGroup == null)
                return;
            foreach (Transform child in ShipListGridLayoutGroup.transform)
            {
                if (!sysShipBuildQueueList.Contains(child))
                {
                    child.gameObject.SetActive(true);
                    sysShipBuildQueueList.Add(child);
                }
            }

            sysShipBuildQueueList.RemoveAll(t => t == null || t.parent != ShipListGridLayoutGroup.transform);

            sysShipBuildQueueList = sysShipBuildQueueList
                .OrderByDescending(t => t.localPosition.y)
                .ThenBy(t => t.localPosition.x)
                .ToList();

            if (!StarSysBuildManager.IsBuildingShip && sysShipBuildQueueList.Count > 0 &&
                TimeManager.Instance?.TurnPhase == TurnPhase.TurnProgression)
            {
                StarSysBuildManager.StartNextShipBuildIfAny();
            }
        }
        public void UpgradeShipToCurrentTech(ShipController ship)
        {
            var currentTech = this.StarSysData.CurrentCivController.CivData.CurrentTechLevel;

            if (ship.ShipData.TechLevel < currentTech)
            {
                // Add to upgrade queue
                // Cost: BuildDuration / 2, uses shipyard
                Debug.Log($"Upgrading {ship.ShipData.ShipName} from {ship.ShipData.TechLevel} to {currentTech}");
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
            // See matching comment in FleetController.OnMouseDown: this raw physics click fires
            // even when a UI button over this system's screen position was the actual click target.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var clickedSystemCon = GetComponentInParent<StarSysController>();
            if (clickedSystemCon == null) return;

            var galaxyUI = GalaxyMenuUIController.Instance;

            Debug.Log($"OnMouseDown: system '{clickedSystemCon.name}' clicked, CurrentClickMode={galaxyUI.CurrentClickMode}.");

            switch (galaxyUI.CurrentClickMode)
            {
                case GalaxyClickMode.Normal:
                    // ✅ Only close ship deploy if it's actually open!
                    if (ShipDeployMenuUIController.Instance != null &&
                        ShipDeployMenuUIController.Instance.ShipDeployPanel != null &&
                        ShipDeployMenuUIController.Instance.ShipDeployPanel.activeSelf)
                    {
                        galaxyUI.CloseShipDeployMenu();
                    }

                    // ✅ CRITICAL: Clean up BOTH star system UIs AND fleet UIs
                    if (StarSysMenuUIController.Instance != null)
                    {
                        StarSysMenuUIController.Instance.MoveBackAnyStarSysUIGO();
                    }
                    if (FleetMenuUIController.Instance != null)
                    {
                        FleetMenuUIController.Instance.MoveBackAnyaFleetUIGO();
                        Debug.Log("OnMouseDown: Cleaned up fleet UIs before opening star system");
                    }

                    HandleNormalClick(clickedSystemCon);
                    break;

                case GalaxyClickMode.SetDestination:
                    Debug.Log($"OnMouseDown: SetDestination click on system '{clickedSystemCon.name}'.");
                    HandleDestinationClick(clickedSystemCon);
                    break;

                case GalaxyClickMode.SelectForShipDeploy:
                    if (GameController.Instance.AreWeLocalPlayer(clickedSystemCon.StarSysData.CurrentOwnerCivEnum))
                        HandleShipDeploySelection(clickedSystemCon);
                    break;

                case GalaxyClickMode.SelectForShipMerge:
                    if (GameController.Instance.AreWeLocalPlayer(clickedSystemCon.StarSysData.CurrentOwnerCivEnum))
                        HandleMergeSelection(clickedSystemCon);
                    break;
            }
        }

        private void HandleMergeSelection(StarSysController clickedSystemCon)
        {
            if (clickedSystemCon != this) { return; }
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatSystemIsSelectedForShipMerge(clickedSystemCon);

            var fleetLooking = galaxyUI.FleetLookingForShipMerge;
            var starSysLooking = galaxyUI.StarSystLookingForShipMerge;
            var shipDeployUI = ShipDeployMenuUIController.Instance;

            if (fleetLooking != null) // Fleet-to-System merge
            {
                var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
                aSysView.gameObject.SetActive(true);

                // ✅ Add VerticalLayoutGroup if not present
                var layoutGroup = aSysView.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = aSysView.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    layoutGroup.spacing = 20f; // Space between fleet and system UI
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = false;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childControlWidth = false;
                }

                // Parent fleet UI to container (TOP position)
                if (fleetLooking.FleetUIGameObject != null)
                {
                    fleetLooking.FleetUIGameObject.transform.SetParent(aSysView.transform, false);
                    fleetLooking.FleetUIGameObject.transform.SetAsFirstSibling();
                    fleetLooking.FleetUIGameObject.SetActive(true);
                    Debug.Log($"✅ Fleet UI parented to ASystemMenuView (top)");
                }

                // Parent system UI to container (BOTTOM position)
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedSystemCon.StarSysUIGameObject.transform.SetAsLastSibling();
                clickedSystemCon.StarSysUIGameObject.SetActive(true);
                Debug.Log($"✅ System UI parented to ASystemMenuView (bottom)");

                // Update facility UI
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);

                // Create combined ship list
                var combinedShipsList = new List<BOTF3D.Combat.ShipController>();
                combinedShipsList.AddRange(fleetLooking.FleetData.ShipsList);
                combinedShipsList.AddRange(clickedSystemCon.StarSysData.ShipsList);

                Debug.Log($"Merge Fleet-to-System: {fleetLooking.FleetData.ShipsList.Count} + {clickedSystemCon.StarSysData.ShipsList.Count} = {combinedShipsList.Count} ships");

                shipDeployUI.SetUpTopShipLists(new List<BOTF3D.Combat.ShipController>());
                shipDeployUI.SetUpBottomShipListsForMerge(combinedShipsList, null, fleetLooking, null, clickedSystemCon);
            }
            else if (starSysLooking != null && starSysLooking != this)
            {
                // System-to-System merge - same approach
                var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
                aSysView.gameObject.SetActive(true);

                var layoutGroup = aSysView.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = aSysView.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    layoutGroup.spacing = 20f;
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = false;
                }

                // Source system at TOP
                starSysLooking.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                starSysLooking.StarSysUIGameObject.transform.SetAsFirstSibling();
                starSysLooking.StarSysUIGameObject.SetActive(true);

                // Target system at BOTTOM
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedSystemCon.StarSysUIGameObject.transform.SetAsLastSibling();
                clickedSystemCon.StarSysUIGameObject.SetActive(true);

                // Update both
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ResearchCenter);

                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);

                var combinedShipsList = new List<BOTF3D.Combat.ShipController>();
                combinedShipsList.AddRange(starSysLooking.StarSysData.ShipsList);
                combinedShipsList.AddRange(clickedSystemCon.StarSysData.ShipsList);

                Debug.Log($"Merge System-to-System: {starSysLooking.StarSysData.ShipsList.Count} + {clickedSystemCon.StarSysData.ShipsList.Count} = {combinedShipsList.Count} ships");

                shipDeployUI.SetUpTopShipLists(new List<BOTF3D.Combat.ShipController>());
                shipDeployUI.SetUpBottomShipListsForMerge(combinedShipsList, null, null, starSysLooking, clickedSystemCon);
            }

            shipDeployUI.ShowShipDeployMenuView();
        }
        private void HandleShipDeploySelection(StarSysController clickedSystemCon)
        {
            if (clickedSystemCon != this) return;
            deployNotMerge = true;
            MousePointerChanger.Instance.ResetCursor();
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatSystemIsSelectedForShipDeploy(clickedSystemCon);
            var fleetLooking = galaxyUI.FleetLookingForShipDeploy;
            var starSysLooking = galaxyUI.StarSystLookingForShipDeploy;

            if (fleetLooking == null && starSysLooking != null)
            {
                // Star system to star system deploy
                var aSysView = StarSysUI.ASystemMenuView.gameObject;
                aSysView.SetActive(true);

                // Parent the LOOKING star system UI (top)
                if (starSysLooking.StarSysUIGameObject != null)
                {
                    starSysLooking.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                    starSysLooking.StarSysUIGameObject.transform.SetAsFirstSibling();
                    starSysLooking.StarSysUIGameObject.SetActive(true);

                    // ✅ Update the LOOKING system's UI values
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Factory);
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.Shipyard);
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ShieldGenerator);
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.OrbitalBattery);
                    StarSysUI.UpdateFacilityUI(starSysLooking, 0, StarSysFacilityType.ResearchCenter);

                    // Update mini map position for LOOKING system
                    var lookingSysUIFields = starSysLooking.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                    if (lookingSysUIFields != null && lookingSysUIFields.redDot != null)
                    {
                        Vector3 lookingPos = starSysLooking.transform.localPosition;
                        lookingSysUIFields.redDot.anchoredPosition = GalaxyPositionBounds.ToMiniMapPosition(lookingPos);
                        Debug.Log($"Updated mini map for LOOKING system '{starSysLooking.name}'");
                    }
                }

                // Parent the CLICKED star system UI (bottom)
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedSystemCon.StarSysUIGameObject.transform.SetAsLastSibling();
                clickedSystemCon.StarSysUIGameObject.SetActive(true);

                // ✅ Update THIS (clicked) system's UI values
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);

                // Update mini map position for THIS system
                var thisSysUIFields = this.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (thisSysUIFields != null && thisSysUIFields.redDot != null)
                {
                    Vector3 thisPos = this.transform.localPosition;
                    thisSysUIFields.redDot.anchoredPosition = GalaxyPositionBounds.ToMiniMapPosition(thisPos);
                    Debug.Log($"Updated mini map for clicked system '{this.name}'");
                }
            }
            else if (fleetLooking != null && starSysLooking == null)
            {
                // Fleet to star system deploy
                // ✅ Use ASystemMenuView (not AFleetMenuView) to match the proven-working
                // "star system to star system deploy" branch above and HandleMergeSelection's
                // Fleet-to-System case — this is the container with the VerticalLayoutGroup
                // set up for stacking two prefab UIs on the left side of the deploy panel.
                var aSysView = StarSysUI.ASystemMenuView.gameObject;
                aSysView.SetActive(true);

                // Parent fleet UI (top)
                if (fleetLooking.FleetUIGameObject != null)
                {
                    fleetLooking.FleetUIGameObject.transform.SetParent(aSysView.transform, false);
                    fleetLooking.FleetUIGameObject.transform.SetAsFirstSibling();
                    fleetLooking.FleetUIGameObject.SetActive(true);
                }

                // Parent THIS system UI (bottom)
                clickedSystemCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                clickedSystemCon.StarSysUIGameObject.transform.SetAsLastSibling();
                clickedSystemCon.StarSysUIGameObject.SetActive(true);

                // ✅ Update THIS system's UI values
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);

                // Update mini map position
                var sysUIFields = this.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (sysUIFields != null && sysUIFields.redDot != null)
                {
                    Vector3 sysPos = this.transform.localPosition;
                    sysUIFields.redDot.anchoredPosition = GalaxyPositionBounds.ToMiniMapPosition(sysPos);
                    Debug.Log($"Updated mini map for system '{this.name}' in fleet-to-system deploy");
                }
            }

            if (fleetLooking != null)
            {
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(fleetLooking.FleetData.ShipsList);
            }
            else if (starSysLooking != null)
            {
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(starSysLooking.StarSysData.ShipsList);
            }
            else
            {
                ShipDeployMenuUIController.Instance.SetUpTopShipLists(clickedSystemCon.StarSysData.ShipsList);
            }

            ShipDeployMenuUIController.Instance.SetUpBottomShipLists(clickedSystemCon, deployNotMerge);
            ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();

            Debug.Log($"HandleShipDeploySelection: ShipDeploy opened for system '{this.name}'");
        }
        private void HandleDestinationClick(StarSysController clickedSystemCon)
        {
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI == null)
            {
                Debug.LogWarning("HandleDestinationClick: GalaxyMenuUIController.Instance is NULL - click ignored.");
                return;
            }

            FleetController theFleetConLookingForDestination = galaxyUI.FleetLookingForDestination;
            if (theFleetConLookingForDestination == null)
            {
                Debug.LogWarning($"HandleDestinationClick: galaxyUI.FleetLookingForDestination is NULL when clicking system '{clickedSystemCon.name}' - SetDestination mode was not armed for a fleet, click ignored.");
                return;
            }

            Debug.Log($"HandleDestinationClick: setting destination='{clickedSystemCon.name}' for fleet '{theFleetConLookingForDestination.name}'.");

            // ✅ Destroy any existing PlayerDefinedTarget before setting new destination
            if (theFleetConLookingForDestination.TargetController != null)
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(theFleetConLookingForDestination);
            }

            // Set the destination
            theFleetConLookingForDestination.FleetData.Destination = this.gameObject;
            theFleetConLookingForDestination.SetAsDestinationInUI(clickedSystemCon.gameObject);

            // Reset mode and cursor
            galaxyUI.CompleteSetDestination();
            MousePointerChanger.Instance?.ResetCursor();
        }


        public void LoadAStarSystem()
        {
            HandleNormalClick(this);
        }
        private void HandleNormalClick(StarSysController clickedSystemCon)
        {
            GalaxyUI.CloseShipDeployMenu();
            if (clickedSystemCon == null) return;
            if (clickedSystemCon == this)
            {
                if (gameController.AreWeLocalPlayer(clickedSystemCon.StarSysData.CurrentOwnerCivEnum))
                {
                    // Our own system - open system UI
                    StarSysUI.SetActiveSetParentUIGO(this);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Factory);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.Shipyard);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ShieldGenerator);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.OrbitalBattery);
                    StarSysUI.UpdateFacilityUI(this, 0, StarSysFacilityType.ResearchCenter);
                    GalaxyUI.OpenMenu(Menu.ASystemMenu, clickedSystemCon.gameObject);
                }
                else
                {
                    // ✅ Check if this is an uninhabited system (CivEnum >= 158)
                    int firstUninhabited = (int)CivEnum.ZZUNINHABITED1; // 158

                    if ((int)this.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
                    {
                        // ✅ Uninhabited system - show colonization UI (if habitable)
                        if (this.StarSysData.IsHabitable)
                        {
                            Debug.Log($"Clicked uninhabited habitable system '{this.StarSysData.SysName}' - showing colonization UI");

                            // Show habitable system UI for colonization
                            HabitableSysUIController.Instance?.LoadHabitableSysUI(this, CivManager.Instance.LocalPlayerCivController);
                        }
                        else
                        {
                            Debug.Log($"Clicked uninhabited non-habitable system '{this.StarSysData.SysName}' - no action");
                            // Could show a "System scanned - not habitable" message
                        }
                    }
                    else if (DiplomacyManager.Instance.FoundADiplomacyController(CivManager.Instance.LocalPlayerCivController, this.StarSysData.CurrentCivController))
                    {
                        // ✅ Foreign owned system (real civilization) - open diplomacy
                        Debug.Log($"Clicked known foreign system '{this.StarSysData.SysName}' owned by {this.StarSysData.CurrentOwnerCivEnum}");
                        DiplomacyManager.Instance.ResolveDiplomacyForClickSystemWeKnow(CivManager.Instance.LocalPlayerCivController, this);
                    }
                    else
                    {
                        Debug.Log($"Clicked unknown foreign system '{this.StarSysData.SysName}' owned by {this.StarSysData.CurrentOwnerCivEnum} - no diplomacy controller found");
                        // First contact should happen via fleet OnTriggerEnter, not clicking
                    }
                }
            }
        }
        void OnTriggerEnter(Collider collider) // Not using OnCollisionEnter....
        {

        }

        public void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnRandomSpecialEvent += DoDisaster;
                TimeManager.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
            }
        }
        public void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnRandomSpecialEvent -= DoDisaster;
                TimeManager.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
            }
        }

        private void OnTurnPhaseChanged(TurnPhase phase)
        {
            if (phase != TurnPhase.TurnProgression || StarSysBuildManager == null) return;
            if (!StarSysBuildManager.IsBuildingFacility && sysBuildQueueList.Count > 0)
                StarSysBuildManager.StartNextFacilityBuildIfAny();
            if (!StarSysBuildManager.IsBuildingShip && sysShipBuildQueueList.Count > 0)
                StarSysBuildManager.StartNextShipBuildIfAny();
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
            StarSysManager.Instance.InstantiateSysBuildUI(this);
            // Do NOT call GalaxyUI.OpenMenu(BuildMenu) — that triggers CloseCurrentMenu(),
            // which hides ASystemMenuView before the build queue has even opened.
        }
        public void ShipClick(StarSysController sysCon) // open build and ship build list UI
        {
            StarSysManager.Instance.InstantiateSysBuildUI(this);
        }
        public void FactoryButtonOnClicked(StarSysController starSysCon)
        {
            if (starSysCon != null && this == starSysCon)
            {
                // Get the power overload image for THIS specific system
                GameObject powerOverloadImg = GetPowerOverloadImage();

                // Do we have enough power to turn a factory on?
                if (this.StarSysData.TotalSysPowerLoad + StarSysData.FactoryData.PowerLoad >
                        this.StarSysData.TotalSysPowerOutput)
                {
                    if (powerOverloadImg != null)
                        CoroutineRunner.FlashPowerOverload(powerOverloadImg);
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

        // Add this helper method to StarSysController
        private GameObject GetPowerOverloadImage()
        {
            if (StarSysUIGameObject != null)
            {
                var sysUIFields = StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (sysUIFields != null && sysUIFields.PowerOverload != null)
                {
                    return sysUIFields.PowerOverload;
                }
            }
            return null;
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
            if (FischlWorks_FogWar.csFogWar.Instance != null && transform != null)
                FischlWorks_FogWar.csFogWar.Instance.RemoveRevealer(transform);

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnRandomSpecialEvent -= DoDisaster;
                TimeManager.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
            }
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
