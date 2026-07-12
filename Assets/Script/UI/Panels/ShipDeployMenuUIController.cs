// Ignore Spelling: BOTF

using BOTF3D.Core;

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    public class ShipDeployMenuUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        public static ShipDeployMenuUIController Instance;
        public GameObject ShipDeployPanel;
        public GameObject TopSlot;
        public GameObject BottomSlot;
        public Button saveCloseButton;
        public FleetController TopFleet;
public FleetController BottomFleet;
        public StarSysController TopStarSyst;
        public StarSysController BottomStarSyst;

        private void Awake()
        {
            // ✅ Scene-based singleton
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("✅ ShipDeployMenuUIController: Instance assigned");
            }
            else if (Instance != this)
            {
                Debug.LogWarning($"❌ Duplicate ShipDeployMenuUIController found! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("ShipDeployMenuUIController: Instance cleared");
            }
        }
        /// <summary>
        /// Show the ship deploy menu scoped to a specific fleet
        /// Sets up the fleet's ships in the bottom slot for deployment
        /// </summary>
        public void ShowShipDeployMenu(FleetController fleet)
        {
            if (fleet == null)
            {
                Debug.LogError("ShowShipDeployMenu: fleet is null");
                return;
            }

            Debug.Log($"ShowShipDeployMenu: opening for fleet '{fleet.name}'");

            // Set up the bottom slot with the fleet's ships
            SetUpBottomShipLists(fleet, deployNotMerge: true);

            // Show the panel
            ShowShipDeployMenuView();
        }
        public void ShowShipDeployMenuView()
        {
            if (ShipDeployPanel == null)
            {
                Debug.LogError("ShowShipDeployMenuView: ShipDeployPanel reference is null.");
                return;
            }

            // Ensure any pending ship UI items are parented before we show the panel
            if (ShipManager.Instance != null)
            {
                ShipManager.Instance.ProcessPendingShipUIs();
            }

            ShipDeployPanel.SetActive(true);
            // Bring panel to front
            transform.SetAsLastSibling();

            // ✅ Fallback: Try to find the button if it's not assigned
            if (saveCloseButton == null)
            {
                saveCloseButton = ShipDeployPanel.transform.Find("SaveClose")?.GetComponent<Button>();
                if (saveCloseButton == null)
                {
                    saveCloseButton = ShipDeployPanel.transform.Find("SaveCloseButton")?.GetComponent<Button>();
                }
                
                if (saveCloseButton != null)
                {
                    Debug.Log($"  Found '{saveCloseButton.name}' via search in ShipDeployPanel");
                }
            }

            if (saveCloseButton != null)
            {
                saveCloseButton.onClick.RemoveAllListeners();
                saveCloseButton.onClick.AddListener(() => OnSaveCloseButtonClicked());
                Debug.Log($"✅ ShipDeployMenuUIController: '{saveCloseButton.name}' wired to OnSaveCloseButtonClicked()");
            }
            else
            {
                Debug.LogWarning("  ⚠️ Save/Close button not found or assigned! User won't be able to close Ship Deploy UI.");
            }
            Debug.Log($"ShowShipDeployMenuView: opened. TopSlot children={TopSlot?.transform.childCount ?? 0}, BottomSlot children={BottomSlot?.transform.childCount ?? 0}");
        }
        /// <summary>
        /// Called when Save + Close button is clicked
        /// </summary>
        public void OnSaveCloseButtonClicked()
        {
            // ✅ GalaxyMenuUIController.CloseCurrentMenu() calls this on EVERY menu
            // transition (any fleet/system click), not just when the deploy panel is
            // open. Bail out silently when there's nothing to save/close, otherwise
            // the "No active owner found" warning below fires on ordinary clicks.
            if (ShipDeployPanel == null || !ShipDeployPanel.activeSelf)
            {
                return;
            }

            Debug.Log("=== OnSaveCloseButtonClicked: User clicked Save + Close ===");

            // ✅ CRITICAL: Delegate to the active controller's commit logic!
            // This ensures ships are moved and temp fleets are finalized/cleaned up.
            
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI == null)
            {
                Debug.LogError("  ❌ GalaxyMenuUIController.Instance is NULL!");
                HideShipDeployMenuView();
                return;
            }

            // Determine which controller "owns" this session based on which one is looking for deploy/merge
            bool isFleetActive = galaxyUI.FleetLookingForShipDeploy != null || galaxyUI.FleetLookingForShipMerge != null;
            bool isSysActive = galaxyUI.StarSystLookingForShipDeploy != null || galaxyUI.StarSystLookingForShipMerge != null;

            if (isFleetActive && FleetMenuUIController.Instance != null)
            {
                Debug.Log("  Delegating to FleetMenuUIController.ClickCancelShipManageButton()");
                FleetMenuUIController.Instance.ClickCancelShipManageButton();
            }
            else if (isSysActive && StarSysMenuUIController.Instance != null)
            {
                Debug.Log("  Delegating to StarSysMenuUIController.ClickCancelShipManageButton()");
                StarSysMenuUIController.Instance.ClickCancelShipManageButton();
            }
            else
            {
                // Fallback: Just hide the UI if we can't find an owner (shouldn't happen in normal flow)
                Debug.LogWarning("  ⚠️ No active owner found for Ship Deploy. Just hiding menu.");
                galaxyUI.CloseShipDeployMenu();
            }
        }
        public void HideShipDeployMenuView()
        {
            ShipDeployPanel.SetActive(false);
        }

        internal void SetUpBottomShipLists(FleetController chosenFleet, bool deployNotMerge)
        {
            if (chosenFleet == null)
            {
                Debug.LogWarning("SetUpBottomShipLists: chosenFleet is null.");
                return;
            }

            // Make sure any ship UI created earlier is reparented to its owners.
            if (ShipManager.Instance != null)
            {
                ShipManager.Instance.ProcessPendingShipUIs();
                if (chosenFleet.FleetData != null)
                {
                    var ships = chosenFleet.FleetData.ShipsList;
                    if (ships.Count == 0)
                    {
                        //Debug.Log($"SetUpBottomShipLists: chosenFleet {chosenFleet?.name} has no ships.");
                    }
                    else
                    {
                        for (int i = 0; i < ships.Count; i++)
                        {
                            var ship = ships[i];
                            if (ship == null) continue;

                            // Ensure a ShipList UI exists for this ship
                            if (ship.ShipListUIGameObject == null && ShipManager.Instance != null)
                            {
                                ShipManager.Instance.InstantiateShipListUIGameObject(ship, chosenFleet.gameObject);
                            }

                            ShipManager.Instance.ProcessPendingShipUIs();

                            if (ship.ShipListUIGameObject != null)
                            {
                                ship.ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);

                                // Set IntendedSlotParent
                                var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                                if (shipUIItem != null)
                                {
                                    shipUIItem.IntendedSlotParent = BottomSlot.transform; // ? NEW
                                }
                            }
                            //else
                            //{
                            //    //Debug.LogWarning($"SetUpBottomShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                            //}
                        }
                    }
                }
            }
            BottomFleet = chosenFleet;
            BottomStarSyst = null;
        }

        internal void SetUpBottomShipLists(StarSysController StarSysLooking, bool deployNotMerge)
        {
            List<BOTF3D.Combat.ShipController> shipConList = null;
            var galaxyMenu = GalaxyMenuUIController.Instance;
            if (deployNotMerge)
            {
                if (galaxyMenu.StarSystSelectedForShipDeploy != null && galaxyMenu.StarSystSelectedForShipDeploy.StarSysData != null)
                {
                    shipConList = galaxyMenu.StarSystSelectedForShipDeploy.StarSysData.ShipsList;
                }
            }
            else if (galaxyMenu.StarSystSelectedForShipMerge != null && galaxyMenu.StarSystSelectedForShipMerge.StarSysData != null)
            {
                shipConList = galaxyMenu.StarSystSelectedForShipMerge.StarSysData.ShipsList;
            }
            if (shipConList != null && shipConList.Count > 0)
            {
                var ownerSys = deployNotMerge ? galaxyMenu.StarSystSelectedForShipDeploy : galaxyMenu.StarSystSelectedForShipMerge;

                for (int i = 0; shipConList.Count > i; i++)
                {
                    var ship = shipConList[i];
                    if (ship == null) continue;

                    // Ensure a ShipList UI exists for this ship before trying to parent it
                    // (same fix as SetUpTopShipLists — otherwise ships silently don't appear
                    // until the panel is closed and reopened).
                    if (ship.ShipListUIGameObject == null && ShipManager.Instance != null && ownerSys != null)
                    {
                        ShipManager.Instance.InstantiateShipListUIGameObject(ship, ownerSys.gameObject);
                        ShipManager.Instance.ProcessPendingShipUIs();
                    }

                    if (ship.ShipListUIGameObject != null)
                    {
                        ship.ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
                    }
                }
            }

            BottomStarSyst = StarSysLooking;
            BottomFleet = null;
        }

        public void SetUpTopShipLists(List<BOTF3D.Combat.ShipController> shipList)
        {
            // Need to know which owner these ships belong to
            FleetController ownerFleet = null;
            StarSysController ownerSys = null;

            if (shipList != null && shipList.Count > 0)
            {
                var firstShip = shipList[0];
                if (firstShip != null && firstShip.ShipData != null)
                {
                    ownerFleet = firstShip.ShipData.CurrentFleetController;
                    ownerSys = firstShip.ShipData.CurrentStarSysController;
                }
            }

            if (ownerFleet != null && ownerFleet.FleetData.ShipListUIParent == null)
            {
                //Debug.LogWarning($"SetUpTopShipLists(List): Fleet '{ownerFleet.name}' missing ShipListUIParent - setting it up now");

                var uiFields = ownerFleet.FleetUIGameObject != null ? ownerFleet.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
                if (uiFields != null && uiFields.FleetShipContentGO != null)
                {
                    ownerFleet.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;
                    //Debug.Log($"SetUpTopShipLists(List): Set ShipListUIParent for fleet '{ownerFleet.name}'");
                }
                //else
                //{
                //    // Debug.LogError($"SetUpTopShipLists(List): Cannot find FleetShipContentGO for fleet '{ownerFleet.name}'! uiFields={(uiFields != null ? "EXISTS" : "NULL")}, FleetShipContentGO={(uiFields != null && uiFields.FleetShipContentGO != null ? "EXISTS" : "NULL")}");
                //}
            }

            // Same check for star systems
            if (ownerSys != null && ownerSys.StarSysData.ShipListUIParent == null)
            {
                //Debug.LogWarning($"SetUpTopShipLists(List): System '{ownerSys.name}' missing ShipListUIParent - setting it up now");

                var uiFields = ownerSys.StarSysUIGameObject != null
                    ? ownerSys.StarSysUIGameObject.GetComponent<StarSysUI_Fields>()
                    : null;
                if (uiFields != null && uiFields.shipContent != null)
                {
                    ownerSys.StarSysData.ShipListUIParent = uiFields.shipContent.gameObject;
                    // Debug.Log($"SetUpTopShipLists(List): Set ShipListUIParent for system '{ownerSys.name}'");
                }
                else
                {
                    // Debug.LogError($"SetUpTopShipLists(List): Cannot find shipContent for system '{ownerSys.name}'!");
                }
            }

            for (int i = 0; i < shipList.Count; i++)
            {
                var ship = shipList[i];
                if (ship == null) continue;

                // Ensure a ShipList UI exists for this ship before trying to parent it.
                // Without this, ships whose UI hasn't been instantiated yet are silently
                // skipped on first open, and only appear after the panel is closed/reopened
                // once something else (e.g. opening the normal system menu) creates the UI.
                if (ship.ShipListUIGameObject == null && ShipManager.Instance != null)
                {
                    GameObject parentGO = ownerFleet != null ? ownerFleet.gameObject : ownerSys?.gameObject;
                    if (parentGO != null)
                    {
                        ShipManager.Instance.InstantiateShipListUIGameObject(ship, parentGO);
                        ShipManager.Instance.ProcessPendingShipUIs();
                    }
                }

                if (ship.ShipListUIGameObject != null)
                {
                    ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

                    // CRITICAL FIX: Set the CurrentFleet/CurrentStarSyst AND IntendedSlotParent
                    var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                    if (shipUIItem != null)
                    {
                        shipUIItem.CurrentFleet = ownerFleet;
                        shipUIItem.CurrentStarSyst = ownerSys;
                        shipUIItem.IntendedSlotParent = TopSlot.transform; // ? NEW: Store intended slot
                        Debug.Log($"SetUpTopShipLists(List): Set owner for {ship.ShipData?.ShipName} to {ownerFleet?.name ?? ownerSys?.name ?? "NULL"}");
                    }
                }
            }

            // Also set TopFleet/TopStarSyst so ReparentUIToOwners knows the context
            TopFleet = ownerFleet;
            TopStarSyst = ownerSys;

            // Final verification log
            // Debug.Log($"SetUpTopShipLists(List): TopFleet={(TopFleet?.name ?? "NULL")}, TopFleet.ShipListUIParent={(TopFleet?.FleetData?.ShipListUIParent != null ? "SET" : "NULL")}, TopStarSyst={(TopStarSyst?.name ?? "NULL")}");
        }

        //internal void SetUpTopShipLists(List<BOTF3D.Combat.ShipController> shipControllers)
        //{
        //    // Ensure any pending ship UI reparenting is attempted first
        //    if (ShipManager.Instance != null)
        //    {
        //        ShipManager.Instance.ProcessPendingShipUIs();

        //        var galaxyUI = GalaxyMenuUIController.Instance;
        //        if (galaxyUI == null)
        //        {
        //            // Debug.LogError("SetUpTopShipLists: GalaxyMenuUIController.Instance is null.");
        //            return;
        //        }

        //        if (galaxyUI.FleetLookingForShipDeploy != null)
        //        {
        //            var shipConList = galaxyUI.FleetLookingForShipDeploy.FleetData.ShipsList;
        //            if (shipConList == null || shipConList.Count == 0)
        //            {
        //                // Debug.Log($"SetUpTopShipLists: Fleet {galaxyUI.FleetLookingForShipDeploy.name} has no ships to show.");
        //            }
        //            else
        //            {
        //                for (int i = 0; i < shipConList.Count; i++)
        //                {
        //                    var ship = shipConList[i];
        //                    if (ship == null) continue;

        //                    if (ship.ShipListUIGameObject == null)
        //                    {
        //                        ShipManager.Instance.InstantiateShipListUIGameObject(ship, galaxyUI.FleetLookingForShipDeploy.gameObject);
        //                    }

        //                    ShipManager.Instance.ProcessPendingShipUIs();

        //                    if (ship.ShipListUIGameObject != null)
        //                    {
        //                        ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

        //                        // CRITICAL FIX: Set the CurrentFleet on the ShipListUI_Item
        //                        var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
        //                        if (shipUIItem != null)
        //                        {
        //                            shipUIItem.CurrentFleet = galaxyUI.FleetLookingForShipDeploy;
        //                            shipUIItem.CurrentStarSyst = null;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
        //                    }
        //                }
        //            }

        //            TopFleet = galaxyUI.FleetLookingForShipDeploy;
        //            TopStarSyst = null;
        //        }
        //        else if (galaxyUI.StarSystLookingForShipDeploy != null)
        //        {
        //            var shipConList = galaxyUI.StarSystLookingForShipDeploy.StarSysData.ShipsList;
        //            if (shipConList == null || shipConList.Count == 0)
        //            {
        //                // Debug.Log($"SetUpTopShipLists: StarSys {galaxyUI.StarSystLookingForShipDeploy.name} has no ships to show.");
        //            }
        //            else
        //            {
        //                for (int i = 0; i < shipConList.Count; i++)
        //                {
        //                    var ship = shipConList[i];
        //                    if (ship == null) continue;

        //                    if (ship.ShipListUIGameObject == null)
        //                    {
        //                        ShipManager.Instance.InstantiateShipListUIGameObject(ship, galaxyUI.StarSystLookingForShipDeploy.gameObject);
        //                    }

        //                    ShipManager.Instance.ProcessPendingShipUIs();

        //                    if (ship.ShipListUIGameObject != null)
        //                    {
        //                        ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

        //                        // CRITICAL FIX: Set the CurrentStarSyst on the ShipListUI_Item
        //                        var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
        //                        if (shipUIItem != null)
        //                        {
        //                            shipUIItem.CurrentStarSyst = galaxyUI.StarSystLookingForShipDeploy;
        //                            shipUIItem.CurrentFleet = null;
        //                            //Debug.Log($"SetUpTopShipLists: Set CurrentStarSyst for {ship.ShipData.ShipName} to {galaxyUI.StarSystLookingForShipDeploy.name}");
        //                        }
        //                    }
        //                    //else
        //                    //{
        //                    //    //Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
        //                    //}
        //                }
        //            }

        //            TopStarSyst = galaxyUI.StarSystLookingForShipDeploy;
        //            TopFleet = null;
        //        }
        //        if (galaxyUI.FleetLookingForShipMerge != null)
        //        {
        //            var shipConList = galaxyUI.FleetLookingForShipMerge.FleetData.ShipsList;
        //            if (shipConList == null || shipConList.Count == 0)
        //            {
        //                //Debug.Log($"SetUpTopShipLists: Fleet {galaxyUI.FleetLookingForShipMerge.name} has no ships to show.");
        //            }
        //            else
        //            {
        //                for (int i = 0; i < shipConList.Count; i++)
        //                {
        //                    var ship = shipConList[i];
        //                    if (ship == null) continue;

        //                    if (ship.ShipListUIGameObject == null)
        //                    {
        //                        ShipManager.Instance.InstantiateShipListUIGameObject(ship, galaxyUI.FleetLookingForShipMerge.gameObject);
        //                    }

        //                    ShipManager.Instance.ProcessPendingShipUIs();

        //                    if (ship.ShipListUIGameObject != null)
        //                    {
        //                        ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

        //                        var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
        //                        if (shipUIItem != null)
        //                        {
        //                            shipUIItem.CurrentFleet = galaxyUI.FleetLookingForShipMerge;
        //                            shipUIItem.CurrentStarSyst = null;
        //                            //Debug.Log($"SetUpTopShipLists: Set CurrentFleet for {ship.ShipData.ShipName} to {galaxyUI.FleetLookingForShipMerge.name}");
        //                        }
        //                    }
        //                    //else
        //                    //{
        //                    //    //Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
        //                    //}
        //                }
        //            }
        //            TopFleet = galaxyUI.FleetLookingForShipMerge;
        //            TopStarSyst = null;
        //        }
        //        else if (galaxyUI.StarSystLookingForShipMerge != null)
        //        {
        //            var shipConList = galaxyUI.StarSystLookingForShipMerge.StarSysData?.ShipsList;
        //            if (shipConList == null || shipConList.Count == 0)
        //            {
        //                //Debug.Log($"SetUpTopShipLists: StarSys {galaxyUI.StarSystLookingForShipMerge.name} has no ships to show.");
        //            }
        //            else
        //            {
        //                for (int i = 0; i < shipConList.Count; i++)
        //                {
        //                    var ship = shipConList[i];
        //                    if (ship == null) continue;

        //                    if (ship.ShipListUIGameObject == null)
        //                    {
        //                        ShipManager.Instance.InstantiateShipListUIGameObject(ship, galaxyUI.StarSystLookingForShipMerge.gameObject);
        //                    }

        //                    ShipManager.Instance.ProcessPendingShipUIs();

        //                    if (ship.ShipListUIGameObject != null)
        //                    {
        //                        ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

        //                        var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
        //                        if (shipUIItem != null)
        //                        {
        //                            shipUIItem.CurrentStarSyst = galaxyUI.StarSystLookingForShipMerge;
        //                            shipUIItem.CurrentFleet = null;
        //                            //Debug.Log($"SetUpTopShipLists: Set CurrentStarSyst for {ship.ShipData.ShipName} to {galaxyUI.StarSystLookingForShipMerge.name}");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
        //                    }
        //                }
        //            }
        //            TopStarSyst = galaxyUI.StarSystLookingForShipMerge;
        //            TopFleet = null;
        //        }
        //    }
        //}

        /// <summary>
        /// Sets up the bottom slot with a combined list of ships for merge operations.
        /// All ships from both sources appear together in the BottomSlot.
        /// </summary>
        public void SetUpBottomShipListsForMerge(
            List<BOTF3D.Combat.ShipController> allShips,
            FleetController targetFleet = null,
            FleetController sourceFleet = null,
            StarSysController sourceSystem = null,
            StarSysController targetSystem = null)
        {
            if (BottomSlot == null)
            {
                Debug.LogError("SetUpBottomShipListsForMerge: BottomSlot is null!");
                return;
            }

            // Clear any existing ships in the bottom slot
            for (int i = BottomSlot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = BottomSlot.transform.GetChild(i);
                Destroy(child.gameObject);
            }

            Debug.Log($"SetUpBottomShipListsForMerge: Adding {allShips.Count} ships to BottomSlot");

            // Parent all ship UI GameObjects to the BottomSlot
            foreach (var ship in allShips)
            {
                if (ship == null) continue;

                if (ship.ShipListUIGameObject != null)
                {
                    ship.ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
                    ship.ShipListUIGameObject.SetActive(true);

                    // Set the UI item to show it will belong to target
                    var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                    if (shipUIItem != null)
                    {
                        shipUIItem.CurrentFleet = targetFleet;
                        shipUIItem.CurrentStarSyst = targetSystem;
                        shipUIItem.IntendedSlotParent = BottomSlot.transform;
                    }

                    // Keep drag-drop ENABLED
                    var dragDrop = ship.ShipListUIGameObject.GetComponent<ShipDragDropItemSlot>();
                    if (dragDrop != null)
                    {
                        dragDrop.enabled = true;
                    }
                }
                else
                {
                    Debug.LogWarning($"SetUpBottomShipListsForMerge: Ship '{ship.name}' has no ShipListUIGameObject");
                }
            }

            // Store merge context
            BottomFleet = targetFleet;
            BottomStarSyst = targetSystem;

            // Store source for merge commit
            TopFleet = sourceFleet;
            TopStarSyst = sourceSystem;

            Debug.Log($"Merge context: TopFleet={sourceFleet?.name}, TopSystem={sourceSystem?.name}, BottomFleet={targetFleet?.name}, BottomSystem={targetSystem?.name}");
        }

        public GameObject[] GetTopSlotShipListUIGOs()
        {
            List<GameObject> shipListItems = new List<GameObject>();
            for (int i = 0; TopSlot.transform.childCount > i; i++)
            {
                shipListItems.Add(TopSlot.transform.GetChild(i).gameObject);
            }
            return shipListItems.ToArray();
        }

        public GameObject[] GetBottomSlotShipListUIGOs()
        {
            List<GameObject> shipListItems = new List<GameObject>();
            for (int i = 0; BottomSlot.transform.childCount > i; i++)
            {
                shipListItems.Add(BottomSlot.transform.GetChild(i).gameObject);
            }
            return shipListItems.ToArray();
        }

        internal void DeployShipsUIGOForFleetsOrSystem()
        {
            if (TopFleet != null && BottomFleet != null)
            {
                DeployShipUIgoBetweenFleets(TopFleet, BottomFleet);
            }
            else if (TopFleet != null && BottomStarSyst != null)
            {
                DeployShipUIgoFromFleetToStarSys(TopFleet, BottomStarSyst);
            }
            else if (TopStarSyst != null && BottomFleet != null)
            {
                DeployShipUIgoFromStarSysToFleet(TopStarSyst, BottomFleet);
            }
            else if (TopStarSyst != null && BottomStarSyst != null)
            {
                DeployShipUIgoBetweenStarSys(TopStarSyst, BottomStarSyst);
            }
        }

        private void DeployShipUIgoBetweenStarSys(StarSysController topStarSyst, StarSysController bottomStarSyst)
        {
            var topShipControllerList = topStarSyst.StarSysData.ShipsList;
            List<BOTF3D.Combat.ShipController> newTopShipControllerList = new List<BOTF3D.Combat.ShipController>();
            for (int i = 0; GetTopSlotShipListUIGOs().Length > i; i++)
            {
                var shipUIGOTop = GetTopSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOTop.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentStarSyst = topStarSyst;
                shipListUI_Item.CurrentFleet = null;
                shipUIGOTop.transform.SetParent(topStarSyst.StarSysData.ShipListUIParent.transform, false);
                for (int j = 0; j < topShipControllerList.Count; j++)
                {
                    if (topShipControllerList[j] == shipListUI_Item.ShipController)
                    {
                        newTopShipControllerList.Add(topShipControllerList[j]);
                    }
                }
            }

            // Defensive reconciliation
            ReconcileMissingShips(topShipControllerList, newTopShipControllerList, topStarSyst.StarSysData?.ShipListUIParent?.transform);

            topStarSyst.StarSysData.ShipsList = newTopShipControllerList;

            var bottomShipControllerList = bottomStarSyst.StarSysData.ShipsList;
            List<BOTF3D.Combat.ShipController> newBottomShipControllerList = new List<BOTF3D.Combat.ShipController>();
            for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
            {
                var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentStarSyst = bottomStarSyst;
                shipListUI_Item.CurrentFleet = null;
                shipUIGOBottom.transform.SetParent(bottomStarSyst.StarSysData.ShipListUIParent.transform, false);
                for (int j = 0; j < bottomShipControllerList.Count; j++)
                {
                    if (bottomShipControllerList[j] == shipListUI_Item.ShipController)
                    {
                        newBottomShipControllerList.Add(bottomShipControllerList[j]);
                    }
                }
            }

            // Defensive reconciliation for bottom
            ReconcileMissingShips(bottomShipControllerList, newBottomShipControllerList, bottomStarSyst.StarSysData?.ShipListUIParent?.transform);

            bottomStarSyst.StarSysData.ShipsList = newBottomShipControllerList;
        }

        private void DeployShipUIgoFromStarSysToFleet(StarSysController topStarSyst, FleetController bottomFleet)
        {
            var topShipControllerList = topStarSyst.StarSysData.ShipsList;
            List<BOTF3D.Combat.ShipController> newTopShipControllerList = new List<BOTF3D.Combat.ShipController>();
            for (int i = 0; GetTopSlotShipListUIGOs().Length > i; i++)
            {
                var shipUIGOTop = GetTopSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOTop.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentStarSyst = topStarSyst;
                shipListUI_Item.CurrentFleet = null;
                shipUIGOTop.transform.SetParent(topStarSyst.StarSysData.ShipListUIParent.transform, false);
                for (int j = 0; j < topShipControllerList.Count; j++)
                {
                    if (topShipControllerList[j] == shipListUI_Item.ShipController)
                    {
                        newTopShipControllerList.Add(topShipControllerList[j]);
                    }
                }
            }

            // Defensive reconciliation for top star system
            ReconcileMissingShips(topShipControllerList, newTopShipControllerList, topStarSyst.StarSysData?.ShipListUIParent?.transform);

            topStarSyst.StarSysData.ShipsList = newTopShipControllerList;

            var bottomShipControllerList = bottomFleet.FleetData.ShipsList;
            List<BOTF3D.Combat.ShipController> newBottomShipControllerList = new List<BOTF3D.Combat.ShipController>();
            for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
            {
                var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentFleet = bottomFleet;
                shipListUI_Item.CurrentStarSyst = null;
                shipUIGOBottom.transform.SetParent(bottomFleet.FleetData.ShipListUIParent.transform, false);
                // Trust the UI slot's ship reference directly rather than requiring it to already be a
                // member of bottomFleet's list — this target may be a brand-new, empty convoy fleet that
                // has never owned any of these ships yet.
                if (shipListUI_Item.ShipController != null && !newBottomShipControllerList.Contains(shipListUI_Item.ShipController))
                {
                    newBottomShipControllerList.Add(shipListUI_Item.ShipController);
                }
            }

            // Defensive reconciliation for bottom fleet
            ReconcileMissingShips(bottomShipControllerList, newBottomShipControllerList, bottomFleet.FleetData?.ShipListUIParent?.transform);

            bottomFleet.FleetData.ShipsList = newBottomShipControllerList;
        }

        private void DeployShipUIgoFromFleetToStarSys(FleetController topFleet, StarSysController bottomStarSyst)
        {
            var topShipControllerList = topFleet.FleetData.ShipsList;
            List<BOTF3D.Combat.ShipController> newTopShipControllerList = new List<BOTF3D.Combat.ShipController>();
            for (int i = 0; GetTopSlotShipListUIGOs().Length > i; i++)
            {
                var shipUIGOTop = GetTopSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOTop.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentFleet = topFleet;
                shipListUI_Item.CurrentStarSyst = null;
                shipUIGOTop.transform.SetParent(topFleet.FleetData.ShipListUIParent.transform, false);
                for (int j = 0; j < topShipControllerList.Count; j++)
                {
                    if (topShipControllerList[j] == shipListUI_Item.ShipController)
                    {
                        newTopShipControllerList.Add(topShipControllerList[j]);
                    }
                }
            }

            // Defensive reconciliation for top fleet
            ReconcileMissingShips(topShipControllerList, newTopShipControllerList, topFleet.FleetData?.ShipListUIParent?.transform);

            topFleet.FleetData.ShipsList = newTopShipControllerList;

            var bottomShipControllerList = bottomStarSyst.StarSysData.ShipsList;
            List<BOTF3D.Combat.ShipController> newBottomShipControllerList = new List<BOTF3D.Combat.ShipController>();
            for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
            {
                var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentStarSyst = bottomStarSyst;
                shipListUI_Item.CurrentFleet = null;
                shipUIGOBottom.transform.SetParent(bottomStarSyst.StarSysData.ShipListUIParent.transform, false);
                for (int j = 0; j < bottomShipControllerList.Count; j++)
                {
                    if (bottomShipControllerList[j] == shipListUI_Item.ShipController)
                    {
                        newBottomShipControllerList.Add(bottomShipControllerList[j]);
                    }
                }
            }

            // Defensive reconciliation for bottom star system
            ReconcileMissingShips(bottomShipControllerList, newBottomShipControllerList, bottomStarSyst.StarSysData?.ShipListUIParent?.transform);

            bottomStarSyst.StarSysData.ShipsList = newBottomShipControllerList;
        }

        private void DeployShipUIgoBetweenFleets(FleetController topFleet, FleetController bottomFleet)
        {
            var topShipControllerList = topFleet.FleetData.ShipsList;
            List<BOTF3D.Combat.ShipController> newTopShipControllerList = new List<BOTF3D.Combat.ShipController>();
            for (int i = 0; i < GetTopSlotShipListUIGOs().Length; i++)
            {
                var shipUIGOtop = GetTopSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOtop.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentFleet = topFleet;
                shipListUI_Item.CurrentStarSyst = null;
                shipUIGOtop.transform.SetParent(topFleet.FleetData.ShipListUIParent.transform, false);
                for (int j = 0; j < topShipControllerList.Count; j++)
                {
                    if (topShipControllerList[j] == shipListUI_Item.ShipController)
                    {
                        newTopShipControllerList.Add(topShipControllerList[j]);
                    }
                }
            }

            // Defensive reconciliation for top fleet
            ReconcileMissingShips(topShipControllerList, newTopShipControllerList, topFleet.FleetData?.ShipListUIParent?.transform);

            topFleet.FleetData.ShipsList = newTopShipControllerList;

            var bottomShipControllerList = bottomFleet.FleetData.ShipsList;
            List<BOTF3D.Combat.ShipController> newBottomShipControllerList = new List<BOTF3D.Combat.ShipController>();
            for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
            {
                var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentFleet = bottomFleet;
                shipListUI_Item.CurrentStarSyst = null;
                shipUIGOBottom.transform.SetParent(bottomFleet.FleetData.ShipListUIParent.transform, false);
                // Trust the UI slot's ship reference directly rather than requiring it to already be a
                // member of bottomFleet's list — this target may be a brand-new, empty convoy fleet that
                // has never owned any of these ships yet.
                if (shipListUI_Item.ShipController != null && !newBottomShipControllerList.Contains(shipListUI_Item.ShipController))
                {
                    newBottomShipControllerList.Add(shipListUI_Item.ShipController);
                }
            }

            // Defensive reconciliation for bottom fleet
            ReconcileMissingShips(bottomShipControllerList, newBottomShipControllerList, bottomFleet.FleetData?.ShipListUIParent?.transform);

            bottomFleet.FleetData.ShipsList = newBottomShipControllerList;
        }

        // Small helper: if any ships from the original owner list are missing from the rebuilt list,
        // add them back and reparent their UI under the provided parent. This is a defensive safety net
        // against transient UI state that could otherwise drop ships.
        private void ReconcileMissingShips(List<BOTF3D.Combat.ShipController> originalList, List<BOTF3D.Combat.ShipController> rebuiltList, Transform ownerUIParent)
        {
            if (originalList == null || rebuiltList == null) return;
            for (int i = 0; i < originalList.Count; i++)
            {
                var orig = originalList[i];
                if (orig == null) continue;
                if (!rebuiltList.Contains(orig))
                {
                    rebuiltList.Add(orig);
                    if (orig.ShipListUIGameObject != null && ownerUIParent != null)
                    {
                        orig.ShipListUIGameObject.transform.SetParent(ownerUIParent, false);
                    }
                    //Debug.Log($"ReconcileMissingShips: restored missing ship '{orig.name}' to owner UI parent.");
                }
            }
        }

        /// <summary>
        /// Called when committing ship deploy for new fleet operations
        /// </summary>
        public void CommitShipDeployForNewFleetAndClose(Action onCommitComplete)
        {
            Debug.Log($"=== CommitShipDeployForNewFleetAndClose START ===");
            Debug.Log($"TopFleet={TopFleet?.name}, BottomFleet={BottomFleet?.name}, TopStarSyst={TopStarSyst?.name}");

            // ✅ DON'T rebuild lists - drag/drop already moved ships correctly!
            // DeployShipsUIGOToNewFleetOrSystem(); // ❌ REMOVED - this loses ships!

            // ✅ Just update max warp for affected fleets
            if (TopFleet != null)
            {
                try { TopFleet.UpdateMaxWarp(); } catch { }
            }
            if (BottomFleet != null)
            {
                try { BottomFleet.UpdateMaxWarp(); } catch { }
            }

            Debug.Log($"=== CommitShipDeployForNewFleetAndClose COMPLETE - letting AfterCommitCleanup handle UI ===");

            // ✅ Callback (AfterCommitCleanup) will:
            // - Move fleet/system UIs to storage
            // - Close all menu views
            // - Refresh ship UIs in containers
            onCommitComplete?.Invoke();
        }

        /// <summary>
        /// Called when committing ship merge operations
        /// </summary>
        public void CommitMergeAndClose(Action onCommitComplete)
        {
            Debug.Log($"=== CommitMergeAndClose START ===");
            Debug.Log($"TopFleet={TopFleet?.name}, TopStarSyst={TopStarSyst?.name}, BottomFleet={BottomFleet?.name}, BottomStarSyst={BottomStarSyst?.name}");

            FleetController targetFleet = BottomFleet;
            StarSysController targetSystem = BottomStarSyst;

            // If the real target is far enough away, redirect the BottomSlot ships into a temporary
            // convoy fleet instead of teleporting them straight into it. The convoy travels at warp
            // (intercept-pursuing a fleet target so it tracks a target that may itself be moving, or a
            // plain destination for a stationary system target) and merges/deposits on arrival.
            FleetController convoyFleet = TryRouteBottomSlotThroughConvoy(
                TopFleet, TopStarSyst, targetFleet, targetSystem,
                out FleetController convoyMergeTargetFleet, out StarSysController convoyMergeTargetSystem);

            if (convoyFleet != null)
            {
                // Ships get merged into the convoy below instead of the real target.
                targetFleet = convoyFleet;
                targetSystem = null;
            }

            // ✅ CRITICAL: Process BOTH TopSlot AND BottomSlot
            // TopSlot = ships staying with original owner
            // BottomSlot = ships going to target

            // 1️⃣ Process TopSlot (ships staying with original owner)
            if (TopFleet != null || TopStarSyst != null)
            {
                Debug.Log($"  Processing TopSlot ({TopSlot.transform.childCount} ships) - staying with original owner");

                for (int i = TopSlot.transform.childCount - 1; i >= 0; i--)
                {
                    var shipUI = TopSlot.transform.GetChild(i).gameObject;
                    var shipUIItem = shipUI.GetComponent<ShipListUI_Item>();

                    if (shipUIItem != null && shipUIItem.ShipController != null)
                    {
                        var ship = shipUIItem.ShipController;

                        // Ensure ship stays with original owner
                        if (TopFleet != null)
                        {
                            if (!TopFleet.FleetData.ShipsList.Contains(ship))
                            {
                                TopFleet.AddToShipList(ship);
                            }
                            ship.ShipData.CurrentFleetController = TopFleet;
                            ship.ShipData.CurrentStarSysController = null;

                            // Move UI to TopFleet's container
                            if (TopFleet.FleetData.ShipListUIParent != null)
                            {
                                shipUI.transform.SetParent(TopFleet.FleetData.ShipListUIParent.transform, false);
                                shipUI.SetActive(true);
                                shipUIItem.CurrentFleet = TopFleet;
                                shipUIItem.CurrentStarSyst = null;
                                Debug.Log($"    Ship '{ship.ShipData.ShipName}' stays with TopFleet '{TopFleet.name}'");
                            }
                        }
                        else if (TopStarSyst != null)
                        {
                            if (!TopStarSyst.StarSysData.ShipsList.Contains(ship))
                            {
                                TopStarSyst.AddToShipList(ship);
                            }
                            ship.ShipData.CurrentStarSysController = TopStarSyst;
                            ship.ShipData.CurrentFleetController = null;

                            // Move UI to TopStarSyst's container
                            if (TopStarSyst.StarSysData.ShipListUIParent != null)
                            {
                                shipUI.transform.SetParent(TopStarSyst.StarSysData.ShipListUIParent.transform, false);
                                shipUI.SetActive(true);
                                shipUIItem.CurrentStarSyst = TopStarSyst;
                                shipUIItem.CurrentFleet = null;
                                Debug.Log($"    Ship '{ship.ShipData.ShipName}' stays with TopStarSyst '{TopStarSyst.name}'");
                            }
                        }
                    }
                }
            }

            // 2️⃣ Process BottomSlot (ships going to target)
            Debug.Log($"  Processing BottomSlot ({BottomSlot.transform.childCount} ships) - merging to target");

            for (int i = BottomSlot.transform.childCount - 1; i >= 0; i--)
            {
                var shipUI = BottomSlot.transform.GetChild(i).gameObject;
                var shipUIItem = shipUI.GetComponent<ShipListUI_Item>();

                if (shipUIItem != null && shipUIItem.ShipController != null)
                {
                    var ship = shipUIItem.ShipController;

                    // Remove from old owner (if different from target)
                    if (ship.ShipData.CurrentFleetController != null && ship.ShipData.CurrentFleetController != targetFleet)
                    {
                        ship.ShipData.CurrentFleetController.RemoveFromShipList(ship);
                        Debug.Log($"    Removed ship '{ship.ShipData.ShipName}' from old fleet '{ship.ShipData.CurrentFleetController.name}'");
                    }
                    else if (ship.ShipData.CurrentStarSysController != null && ship.ShipData.CurrentStarSysController != targetSystem)
                    {
                        ship.ShipData.CurrentStarSysController.RemoveFromShipList(ship);
                        Debug.Log($"    Removed ship '{ship.ShipData.ShipName}' from old system '{ship.ShipData.CurrentStarSysController.name}'");
                    }

                    // Add to target
                    if (targetFleet != null)
                    {
                        if (!targetFleet.FleetData.ShipsList.Contains(ship))
                        {
                            targetFleet.AddToShipList(ship);
                        }
                        ship.ShipData.CurrentFleetController = targetFleet;
                        ship.ShipData.CurrentStarSysController = null;

                        // Move UI to target's container
                        if (targetFleet.FleetData.ShipListUIParent != null)
                        {
                            shipUI.transform.SetParent(targetFleet.FleetData.ShipListUIParent.transform, false);
                            shipUI.SetActive(true);
                            shipUIItem.CurrentFleet = targetFleet;
                            shipUIItem.CurrentStarSyst = null;
                        }

                        Debug.Log($"    Merged ship '{ship.ShipData.ShipName}' to target fleet '{targetFleet.name}'");
                    }
                    else if (targetSystem != null)
                    {
                        if (!targetSystem.StarSysData.ShipsList.Contains(ship))
                        {
                            targetSystem.AddToShipList(ship);
                        }
                        ship.ShipData.CurrentStarSysController = targetSystem;
                        ship.ShipData.CurrentFleetController = null;

                        // Move UI to target's container
                        if (targetSystem.StarSysData.ShipListUIParent != null)
                        {
                            shipUI.transform.SetParent(targetSystem.StarSysData.ShipListUIParent.transform, false);
                            shipUI.SetActive(true);
                            shipUIItem.CurrentStarSyst = targetSystem;
                            shipUIItem.CurrentFleet = null;
                        }

                        Debug.Log($"    Merged ship '{ship.ShipData.ShipName}' to target system '{targetSystem.name}'");
                    }
                }
            }

            // 3️⃣ Update max warp
            if (targetFleet != null) targetFleet.UpdateMaxWarp();
            if (TopFleet != null && TopFleet != targetFleet) TopFleet.UpdateMaxWarp();

            // 4️⃣ Send the convoy on its way now that its final MaxWarpFactor (from the ships just
            // loaded aboard it) is known.
            SendConvoyOnItsWay(convoyFleet, convoyMergeTargetFleet, convoyMergeTargetSystem);

            Debug.Log($"=== CommitMergeAndClose COMPLETE ===");

            // ✅ Callback does the cleanup (AfterCommitCleanup from GalaxyMenuUIController)
            onCommitComplete?.Invoke();
        }

        /// <summary>
        /// Called when committing general ship deploy operations
        /// </summary>
        public void CommitShipDeployAndClose(Action onComplete = null)
        {
            Debug.Log($"=== CommitShipDeployAndClose START ===");
            Debug.Log($"TopFleet={TopFleet?.name}, BottomFleet={BottomFleet?.name}, TopStarSyst={TopStarSyst?.name}, BottomStarSyst={BottomStarSyst?.name}");

            // Same convoy redirection as CommitMergeAndClose: if the target is far enough away, the
            // BottomSlot ships get routed into a temporary convoy fleet that physically travels there,
            // rather than teleporting straight into the target's ship list.
            FleetController convoyFleet = TryRouteBottomSlotThroughConvoy(
                TopFleet, TopStarSyst, BottomFleet, BottomStarSyst,
                out FleetController convoyMergeTargetFleet, out StarSysController convoyMergeTargetSystem);

            if (convoyFleet != null)
            {
                if (TopFleet != null)
                    DeployShipUIgoBetweenFleets(TopFleet, convoyFleet);
                else if (TopStarSyst != null)
                    DeployShipUIgoFromStarSysToFleet(TopStarSyst, convoyFleet);
            }
            else
            {
                // ✅ CRITICAL: For regular deploy, we MUST reconcile ship lists based on where UIs ended up
                // This method looks at TopSlot/BottomSlot contents and rebuilds ship lists accordingly
                DeployShipsUIGOForFleetsOrSystem();
            }

            // ✅ Update max warp for affected fleets
            if (TopFleet != null)
            {
                try { TopFleet.UpdateMaxWarp(); } catch { }
            }
            if (BottomFleet != null)
            {
                try { BottomFleet.UpdateMaxWarp(); } catch { }
            }
            if (convoyFleet != null)
            {
                try { convoyFleet.UpdateMaxWarp(); } catch { }
            }

            // Send the convoy on its way now that its final MaxWarpFactor (from the ships just loaded
            // aboard it) is known.
            SendConvoyOnItsWay(convoyFleet, convoyMergeTargetFleet, convoyMergeTargetSystem);

            Debug.Log($"=== CommitShipDeployAndClose COMPLETE ===");

            // ✅ Callback does the cleanup
            onComplete?.Invoke();
        }

        // Shared by CommitShipDeployAndClose and CommitMergeAndClose: if the BottomSlot has ships and
        // the target is farther than FleetManager.ConvoyDistanceThreshold, spins up a convoy fleet to
        // carry them there instead of teleporting them straight into the target's ship list.
        private FleetController TryRouteBottomSlotThroughConvoy(
            FleetController topFleet, StarSysController topStarSyst,
            FleetController targetFleet, StarSysController targetSystem,
            out FleetController convoyMergeTargetFleet, out StarSysController convoyMergeTargetSystem)
        {
            convoyMergeTargetFleet = null;
            convoyMergeTargetSystem = null;

            if (BottomSlot.transform.childCount == 0) return null;

            Vector3 sourcePos = topFleet != null ? topFleet.transform.position :
                topStarSyst != null ? topStarSyst.transform.position : Vector3.zero;
            Vector3 targetPos = targetFleet != null ? targetFleet.transform.position :
                targetSystem != null ? targetSystem.transform.position : Vector3.zero;

            if (Vector3.Distance(sourcePos, targetPos) <= FleetManager.ConvoyDistanceThreshold) return null;

            CivEnum sourceCiv = topFleet != null ? topFleet.FleetData.CivEnum :
                topStarSyst != null ? topStarSyst.StarSysData.CurrentOwnerCivEnum :
                targetFleet != null ? targetFleet.FleetData.CivEnum :
                targetSystem.StarSysData.CurrentOwnerCivEnum;

            FleetController convoyFleet = FleetManager.Instance.CreateConvoyFleet(topFleet, topStarSyst, sourceCiv);
            if (convoyFleet == null) return null;

            convoyMergeTargetFleet = targetFleet;
            convoyMergeTargetSystem = targetSystem;
            convoyFleet.FleetData.ConvoyMergeTarget = targetFleet;
            convoyFleet.FleetData.ConvoyMergeSystem = targetSystem;

            Debug.Log($"TryRouteBottomSlotThroughConvoy: target is {Vector3.Distance(sourcePos, targetPos):F1} units away — routing ships through convoy '{convoyFleet.name}'");

            return convoyFleet;
        }

        // Shared by CommitShipDeployAndClose and CommitMergeAndClose: sends a just-loaded convoy fleet
        // off toward its merge target now that its final MaxWarpFactor is known.
        private void SendConvoyOnItsWay(FleetController convoyFleet, FleetController convoyMergeTargetFleet, StarSysController convoyMergeTargetSystem)
        {
            if (convoyFleet == null) return;

            string destinationName = "";

            if (convoyMergeTargetFleet != null)
            {
                convoyFleet.SetInterceptTarget(convoyMergeTargetFleet);
                destinationName = convoyMergeTargetFleet.FleetData.FleetName;
            }
            else if (convoyMergeTargetSystem != null)
            {
                convoyFleet.FleetData.Destination = convoyMergeTargetSystem.gameObject;
                convoyFleet.FleetData.CurrentWarpFactor = convoyFleet.FleetData.MaxWarpFactor;
                destinationName = convoyMergeTargetSystem.StarSysData.SysName;
            }
            else
            {
                return;
            }

            // Same existing destination UI used for a normal move order (see
            // FleetController.SetAsDestinationInUI / HandleShipMergeSelection) so the convoy shows a
            // destination name and its Cancel Destination button, letting the player abort the
            // deploy/merge in transit via FleetController.ClickCancelDestinationButton.
            var fields = convoyFleet.FleetUIGameObject != null ? convoyFleet.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
            if (fields != null)
            {
                if (fields.DestinationDragTarget != null)
                    fields.DestinationDragTarget.gameObject.SetActive(false);
                if (fields.CancelDestination != null)
                    fields.CancelDestination.gameObject.SetActive(true);
                if (fields.DestinationName != null)
                    fields.DestinationName.text = destinationName;
                if (fields.DestinationCoordinates != null)
                    fields.DestinationCoordinates.text = "";
            }
        }

        private void ReparentUIFromShipControllerHierarchy(FleetController fleet)
        {
            if (fleet == null) return;

            // Debug.Log($"ReparentUIFromShipControllerHierarchy: Processing fleet '{fleet.name}'");

            // Ensure ShipListUIParent exists
            if (fleet.FleetData?.ShipListUIParent == null)
            {
                var uiFields = fleet.FleetUIGameObject != null ? fleet.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
                if (uiFields != null && uiFields.FleetShipContentGO != null)
                {
                    fleet.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;
                    //Debug.Log($"  Set ShipListUIParent for fleet '{fleet.name}'");
                }
                else
                {
                    //Debug.LogError($"  Cannot set ShipListUIParent for fleet '{fleet.name}'");
                    return;
                }
            }

            // Iterate through all ShipController children of this fleet
            var shipControllers = fleet.GetComponentsInChildren<BOTF3D.Combat.ShipController>(true);
            //Debug.Log($"  Found {shipControllers.Length} ShipController children under fleet '{fleet.name}'");

            foreach (var shipController in shipControllers)
            {
                if (shipController.ShipListUIGameObject != null)
                {
                    var currentParent = shipController.ShipListUIGameObject.transform.parent;
                    var targetParent = fleet.FleetData.ShipListUIParent.transform;

                    if (currentParent != targetParent)
                    {
                        //Debug.Log($"  Reparenting UI for ship '{shipController.ShipData?.ShipName}' from '{currentParent?.name}' to '{targetParent.name}'");
                        shipController.ShipListUIGameObject.transform.SetParent(targetParent, false);

                        // Also update the ShipListUI_Item ownership
                        var shipUIItem = shipController.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                        if (shipUIItem != null)
                        {
                            shipUIItem.CurrentFleet = fleet;
                            shipUIItem.CurrentStarSyst = null;
                        }
                    }
                    else
                    {
                        //Debug.Log($"  Ship UI '{shipController.ShipData?.ShipName}' already correctly parented");
                    }
                }
            }
        }

        /// <summary>
        /// Overload for star systems
        /// </summary>
        private void ReparentUIFromShipControllerHierarchy(StarSysController starSys)
        {
            if (starSys == null) return;

            Debug.Log($"ReparentUIFromShipControllerHierarchy: Processing system '{starSys.name}'");

            // Ensure ShipListUIParent exists
            if (starSys.StarSysData?.ShipListUIParent == null)
            {
                var uiFields = starSys.StarSysUIGameObject != null
                    ? starSys.StarSysUIGameObject.GetComponent<StarSysUI_Fields>()
                    : null;
                if (uiFields != null && uiFields.shipContent != null)
                {
                    starSys.StarSysData.ShipListUIParent = uiFields.shipContent.gameObject;
                    Debug.Log($"  Set ShipListUIParent for system '{starSys.name}'");
                }
                else
                {
                    Debug.LogError($"  Cannot set ShipListUIParent for system '{starSys.name}'");
                    return;
                }
            }

            // Iterate through all ShipController children of this system
            var shipControllers = starSys.GetComponentsInChildren<BOTF3D.Combat.ShipController>(true);
            // Debug.Log($"  Found {shipControllers.Length} ShipController children under system '{starSys.name}'");

            foreach (var shipController in shipControllers)
            {
                if (shipController.ShipListUIGameObject != null)
                {
                    var currentParent = shipController.ShipListUIGameObject.transform.parent;
                    var targetParent = starSys.StarSysData.ShipListUIParent.transform;

                    if (currentParent != targetParent)
                    {
                        // Debug.Log($"  Reparenting UI for ship '{shipController.ShipData?.ShipName}' from '{currentParent?.name}' to '{targetParent.name}'");
                        shipController.ShipListUIGameObject.transform.SetParent(targetParent, false);

                        // Also update the ShipListUI_Item ownership
                        var shipUIItem = shipController.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                        if (shipUIItem != null)
                        {
                            shipUIItem.CurrentStarSyst = starSys;
                            shipUIItem.CurrentFleet = null;
                        }
                    }
                    else
                    {
                        //Debug.Log($"  Ship UI '{shipController.ShipData?.ShipName}' already correctly parented");
                    }
                }
            }
        }

        private void AssignOwnership(Transform parent, FleetController fleetOwner, StarSysController sysOwner)
        {
            if (parent == null) return;
            for (int i = 0; i < parent.childCount; i++)
            {
                var go = parent.GetChild(i).gameObject;
                var item = go.GetComponent<ShipListUI_Item>();
                if (item == null) continue;
                item.CurrentFleet = fleetOwner;
                item.CurrentStarSyst = sysOwner;
            }
        }

        private void DumpAllShipListUIs()
        {
            // Find all ShipListUI_Item instances (active and inactive)
            var all = UnityEngine.Object.FindObjectsByType<ShipListUI_Item>(UnityEngine.FindObjectsInactive.Include);
            // Debug.Log($"DumpAllShipListUIs: found {all.Length} ShipListUI_Item instances");
            for (int i = 0; i < all.Length; i++)
            {
                var item = all[i];
                var parentName = item.gameObject.transform.parent != null ? item.gameObject.transform.parent.name : "<null>";
                // Debug.Log($"ShipUI '{item.gameObject.name}': parent='{parentName}', CurrentFleet='{item.CurrentFleet?.name}', CurrentStarSyst='{item.CurrentStarSyst?.name}', ShipController='{item.ShipController?.name}'");
            }
        }

        /// <summary>
        /// Helper: Rebuilds layout for a UI GameObject
        /// </summary>
        private void RebuildLayout(GameObject uiGameObject)
        {
            if (uiGameObject != null)
            {
                var rt = uiGameObject.GetComponent<RectTransform>();
                if (rt != null)
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                }
            }
        }
    }
}
