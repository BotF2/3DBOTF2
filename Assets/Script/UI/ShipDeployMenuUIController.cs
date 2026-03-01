// Ignore Spelling: BOTF

using BOTF3D.Core;
using BOTF3D.GamePlay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    public class ShipDeployMenuUIController : MonoBehaviour
    {
        public static ShipDeployMenuUIController Instance;
        public GameObject ShipDeployPanel;
        public GameObject TopSlot;
        public GameObject BottomSlot;
        [SerializeField]
        private Button updateShipsLists;
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

            Debug.Log($"ShowShipDeployMenuView: opened. TopSlot children={TopSlot?.transform.childCount ?? 0}, BottomSlot children={BottomSlot?.transform.childCount ?? 0}");
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
                            else
                            {
                                //Debug.LogWarning($"SetUpBottomShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                            }
                        }
                    }
                }
            }
            BottomFleet = chosenFleet;
            BottomStarSyst = null;
        }

        internal void SetUpBottomShipLists(StarSysController StarSysLooking, bool deployNotMerge)
        {
            List<ShipController> shipConList = null;
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
                for (int i = 0; shipConList.Count > i; i++)
                {
                    shipConList[i].ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
                }
            }

            BottomStarSyst = StarSysLooking;
            BottomFleet = null;
        }

        public void SetUpTopShipLists(List<ShipController> shipList)
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

                var uiFields = ownerFleet.FleetUIGameObject?.GetComponent<FleetUI_Fields>();
                if (uiFields != null && uiFields.FleetShipContentGO != null)
                {
                    ownerFleet.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;
                    //Debug.Log($"SetUpTopShipLists(List): Set ShipListUIParent for fleet '{ownerFleet.name}'");
                }
                else
                {
                    // Debug.LogError($"SetUpTopShipLists(List): Cannot find FleetShipContentGO for fleet '{ownerFleet.name}'! uiFields={(uiFields != null ? "EXISTS" : "NULL")}, FleetShipContentGO={(uiFields != null && uiFields.FleetShipContentGO != null ? "EXISTS" : "NULL")}");
                }
            }

            // Same check for star systems
            if (ownerSys != null && ownerSys.StarSysData.ShipListUIParent == null)
            {
                //Debug.LogWarning($"SetUpTopShipLists(List): System '{ownerSys.name}' missing ShipListUIParent - setting it up now");

                var uiFields = ownerSys.StarSysUIGameObject?.GetComponent<StarSysUI_Fields>();
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
                if (shipList[i] != null && shipList[i].ShipListUIGameObject != null)
                {
                    shipList[i].ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

                    // CRITICAL FIX: Set the CurrentFleet/CurrentStarSyst AND IntendedSlotParent
                    var shipUIItem = shipList[i].ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                    if (shipUIItem != null)
                    {
                        shipUIItem.CurrentFleet = ownerFleet;
                        shipUIItem.CurrentStarSyst = ownerSys;
                        shipUIItem.IntendedSlotParent = TopSlot.transform; // ? NEW: Store intended slot
                        Debug.Log($"SetUpTopShipLists(List): Set owner for {shipList[i].ShipData?.ShipName} to {ownerFleet?.name ?? ownerSys?.name ?? "NULL"}");
                    }
                }
            }

            // Also set TopFleet/TopStarSyst so ReparentUIToOwners knows the context
            TopFleet = ownerFleet;
            TopStarSyst = ownerSys;

            // Final verification log
            // Debug.Log($"SetUpTopShipLists(List): TopFleet={(TopFleet?.name ?? "NULL")}, TopFleet.ShipListUIParent={(TopFleet?.FleetData?.ShipListUIParent != null ? "SET" : "NULL")}, TopStarSyst={(TopStarSyst?.name ?? "NULL")}");
        }

        internal void SetUpTopShipLists()
        {
            // Ensure any pending ship UI reparenting is attempted first
            if (ShipManager.Instance != null)
            {
                ShipManager.Instance.ProcessPendingShipUIs();

                var galaxyUI = GalaxyMenuUIController.Instance;
                if (galaxyUI == null)
                {
                    // Debug.LogError("SetUpTopShipLists: GalaxyMenuUIController.Instance is null.");
                    return;
                }

                if (galaxyUI.FleetLookingForShipDeploy != null)
                {
                    var shipConList = galaxyUI.FleetLookingForShipDeploy.FleetData.ShipsList;
                    if (shipConList == null || shipConList.Count == 0)
                    {
                        // Debug.Log($"SetUpTopShipLists: Fleet {galaxyUI.FleetLookingForShipDeploy.name} has no ships to show.");
                    }
                    else
                    {
                        for (int i = 0; i < shipConList.Count; i++)
                        {
                            var ship = shipConList[i];
                            if (ship == null) continue;

                            if (ship.ShipListUIGameObject == null)
                            {
                                ShipManager.Instance.InstantiateShipListUIGameObject(ship, galaxyUI.FleetLookingForShipDeploy.gameObject);
                            }

                            ShipManager.Instance.ProcessPendingShipUIs();

                            if (ship.ShipListUIGameObject != null)
                            {
                                ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

                                // CRITICAL FIX: Set the CurrentFleet on the ShipListUI_Item
                                var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                                if (shipUIItem != null)
                                {
                                    shipUIItem.CurrentFleet = galaxyUI.FleetLookingForShipDeploy;
                                    shipUIItem.CurrentStarSyst = null;
                                }
                            }
                            else
                            {
                                // Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                            }
                        }
                    }

                    TopFleet = galaxyUI.FleetLookingForShipDeploy;
                    TopStarSyst = null;
                }
                else if (galaxyUI.StarSystLookingForShipDeploy != null)
                {
                    var shipConList = galaxyUI.StarSystLookingForShipDeploy.StarSysData.ShipsList;
                    if (shipConList == null || shipConList.Count == 0)
                    {
                        // Debug.Log($"SetUpTopShipLists: StarSys {galaxyUI.StarSystLookingForShipDeploy.name} has no ships to show.");
                    }
                    else
                    {
                        for (int i = 0; i < shipConList.Count; i++)
                        {
                            var ship = shipConList[i];
                            if (ship == null) continue;

                            if (ship.ShipListUIGameObject == null)
                            {
                                ShipManager.Instance.InstantiateShipListUIGameObject(ship, galaxyUI.StarSystLookingForShipDeploy.gameObject);
                            }

                            ShipManager.Instance.ProcessPendingShipUIs();

                            if (ship.ShipListUIGameObject != null)
                            {
                                ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

                                // CRITICAL FIX: Set the CurrentStarSyst on the ShipListUI_Item
                                var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                                if (shipUIItem != null)
                                {
                                    shipUIItem.CurrentStarSyst = galaxyUI.StarSystLookingForShipDeploy;
                                    shipUIItem.CurrentFleet = null;
                                    //Debug.Log($"SetUpTopShipLists: Set CurrentStarSyst for {ship.ShipData.ShipName} to {galaxyUI.StarSystLookingForShipDeploy.name}");
                                }
                            }
                            else
                            {
                                //Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                            }
                        }
                    }

                    TopStarSyst = galaxyUI.StarSystLookingForShipDeploy;
                    TopFleet = null;
                }
                if (galaxyUI.FleetLookingForShipMerge != null)
                {
                    var shipConList = galaxyUI.FleetLookingForShipMerge.FleetData.ShipsList;
                    if (shipConList == null || shipConList.Count == 0)
                    {
                        //Debug.Log($"SetUpTopShipLists: Fleet {galaxyUI.FleetLookingForShipMerge.name} has no ships to show.");
                    }
                    else
                    {
                        for (int i = 0; i < shipConList.Count; i++)
                        {
                            var ship = shipConList[i];
                            if (ship == null) continue;

                            if (ship.ShipListUIGameObject == null)
                            {
                                ShipManager.Instance.InstantiateShipListUIGameObject(ship, galaxyUI.FleetLookingForShipMerge.gameObject);
                            }

                            ShipManager.Instance.ProcessPendingShipUIs();

                            if (ship.ShipListUIGameObject != null)
                            {
                                ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

                                var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                                if (shipUIItem != null)
                                {
                                    shipUIItem.CurrentFleet = galaxyUI.FleetLookingForShipMerge;
                                    shipUIItem.CurrentStarSyst = null;
                                    //Debug.Log($"SetUpTopShipLists: Set CurrentFleet for {ship.ShipData.ShipName} to {galaxyUI.FleetLookingForShipMerge.name}");
                                }
                            }
                            else
                            {
                                //Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                            }
                        }
                    }
                    TopFleet = galaxyUI.FleetLookingForShipMerge;
                    TopStarSyst = null;
                }
                else if (galaxyUI.StarSystLookingForShipMerge != null)
                {
                    var shipConList = galaxyUI.StarSystLookingForShipMerge.StarSysData?.ShipsList;
                    if (shipConList == null || shipConList.Count == 0)
                    {
                        //Debug.Log($"SetUpTopShipLists: StarSys {galaxyUI.StarSystLookingForShipMerge.name} has no ships to show.");
                    }
                    else
                    {
                        for (int i = 0; i < shipConList.Count; i++)
                        {
                            var ship = shipConList[i];
                            if (ship == null) continue;

                            if (ship.ShipListUIGameObject == null)
                            {
                                ShipManager.Instance.InstantiateShipListUIGameObject(ship, galaxyUI.StarSystLookingForShipMerge.gameObject);
                            }

                            ShipManager.Instance.ProcessPendingShipUIs();

                            if (ship.ShipListUIGameObject != null)
                            {
                                ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);

                                var shipUIItem = ship.ShipListUIGameObject.GetComponent<ShipListUI_Item>();
                                if (shipUIItem != null)
                                {
                                    shipUIItem.CurrentStarSyst = galaxyUI.StarSystLookingForShipMerge;
                                    shipUIItem.CurrentFleet = null;
                                    //Debug.Log($"SetUpTopShipLists: Set CurrentStarSyst for {ship.ShipData.ShipName} to {galaxyUI.StarSystLookingForShipMerge.name}");
                                }
                            }
                            else
                            {
                                // Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                            }
                        }
                    }
                    TopStarSyst = galaxyUI.StarSystLookingForShipMerge;
                    TopFleet = null;
                }
            }
        }

        /// <summary>
        /// Sets up the bottom slot with a combined list of ships for merge operations.
        /// All ships from both sources appear together in the BottomSlot.
        /// </summary>
        public void SetUpBottomShipListsForMerge(
            List<ShipController> allShips,
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

        internal void DeployShipsUIGOToNewFleetOrSystem()
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
            List<ShipController> newTopShipControllerList = new List<ShipController>();
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
            List<ShipController> newBottomShipControllerList = new List<ShipController>();
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
            List<ShipController> newTopShipControllerList = new List<ShipController>();
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
            List<ShipController> newBottomShipControllerList = new List<ShipController>();
            for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
            {
                var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentFleet = bottomFleet;
                shipListUI_Item.CurrentStarSyst = null;
                shipUIGOBottom.transform.SetParent(bottomFleet.FleetData.ShipListUIParent.transform, false);
                for (int j = 0, jmax = bottomShipControllerList.Count; j < jmax; j++)
                {
                    if (bottomShipControllerList[j] == shipListUI_Item.ShipController)
                    {
                        newBottomShipControllerList.Add(bottomShipControllerList[j]);
                    }
                }
            }

            // Defensive reconciliation for bottom fleet
            ReconcileMissingShips(bottomShipControllerList, newBottomShipControllerList, bottomFleet.FleetData?.ShipListUIParent?.transform);

            bottomFleet.FleetData.ShipsList = newBottomShipControllerList;
        }

        private void DeployShipUIgoFromFleetToStarSys(FleetController topFleet, StarSysController bottomStarSyst)
        {
            var topShipControllerList = topFleet.FleetData.ShipsList;
            List<ShipController> newTopShipControllerList = new List<ShipController>();
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
            List<ShipController> newBottomShipControllerList = new List<ShipController>();
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
            List<ShipController> newTopShipControllerList = new List<ShipController>();
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
            List<ShipController> newBottomShipControllerList = new List<ShipController>();
            for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
            {
                var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
                var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
                shipListUI_Item.CurrentFleet = bottomFleet;
                shipListUI_Item.CurrentStarSyst = null;
                shipUIGOBottom.transform.SetParent(bottomFleet.FleetData.ShipListUIParent.transform, false);
                for (int j = 0; j < bottomShipControllerList.Count; j++)
                {
                    if (bottomShipControllerList[j] == shipListUI_Item.ShipController)
                    {
                        newBottomShipControllerList.Add(bottomShipControllerList[j]);
                    }
                }
            }

            // Defensive reconciliation for bottom fleet
            ReconcileMissingShips(bottomShipControllerList, newBottomShipControllerList, this.BottomFleet.FleetData?.ShipListUIParent?.transform);

            BottomFleet.FleetData.ShipsList = newBottomShipControllerList;
        }

        // Small helper: if any ships from the original owner list are missing from the rebuilt list,
        // add them back and reparent their UI under the provided parent. This is a defensive safety net
        // against transient UI state that could otherwise drop ships.
        private void ReconcileMissingShips(List<ShipController> originalList, List<ShipController> rebuiltList, Transform ownerUIParent)
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

        public void CommitShipDeployAndClose(Action onComplete = null)
        {
            gameObject.SetActive(true);
            // Run async to let drag/drop & layout events finish
            StartCoroutine(CommitShipDeployCoroutine(onComplete));
        }

        private IEnumerator CommitShipDeployCoroutine(Action onComplete = null)
        {
            // 1) Ensure any pending ShipManager parenting is attempted first
            ShipManager.Instance?.ProcessPendingShipUIs();

            // 2) Force canvas/layout update so child counts are accurate
            Canvas.ForceUpdateCanvases();
            if (TopSlot != null)
            {
                var rt = TopSlot.GetComponent<RectTransform>();
                if (rt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            if (BottomSlot != null)
            {
                var rt = BottomSlot.GetComponent<RectTransform>();
                if (rt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            // 3) Wait one frame to allow EndDrag handlers and UI events to finish
            yield return null;

            // 4) Diagnostic dump to see where UI items are right now
            DumpAllShipListUIs();

            // 5) Assign ownership flags from the slot containers (and owner UI parents)
            AssignOwnership(TopSlot?.transform, TopFleet, TopStarSyst);
            AssignOwnership(BottomSlot?.transform, BottomFleet, BottomStarSyst);

            if (TopFleet?.FleetData?.ShipListUIParent != null) AssignOwnership(TopFleet.FleetData.ShipListUIParent.transform, TopFleet, null);
            if (BottomFleet?.FleetData?.ShipListUIParent != null) AssignOwnership(BottomFleet.FleetData.ShipListUIParent.transform, BottomFleet, null);
            if (TopStarSyst?.StarSysData?.ShipListUIParent != null) AssignOwnership(TopStarSyst.StarSysData.ShipListUIParent.transform, null, TopStarSyst);
            if (BottomStarSyst?.StarSysData?.ShipListUIParent != null) AssignOwnership(BottomStarSyst.StarSysData.ShipListUIParent.transform, null, BottomStarSyst);

            // 6) Now call your deploy/reconciliation code (idempotent)
            DeployShipsUIGOToNewFleetOrSystem();

            // ✅ 7) CRITICAL: Explicitly reparent ALL ship UIs from slots to their owner's ShipListUIParent
            ReparentShipUIsToOwners();

            // 8) Clean up and move fleet/system UIs back to their original parents
            FleetMenuUIController.Instance?.MoveBackAnyaFleetUIGO();
            StarSysMenuUIController.Instance?.MoveBackAnyStarSysUIGO();

            // 9) Final diagnostic + callback
            GalaxyMenuUIController.Instance?.CompleteShipExchange();
            Debug.Log($"CommitShipDeployAndClose: Ships reparented, closing panel");

            if (onComplete != null)
                onComplete.Invoke();

            // Close the panel
            HideShipDeployMenuView();

            yield break;
        }

        /// <summary>
        /// Ensures all ship UIs are moved from TopSlot/BottomSlot to their owner's ShipListUIParent
        /// </summary>
        private void ReparentShipUIsToOwners()
        {
            // Process TopSlot ships
            if (TopSlot != null && TopSlot.transform.childCount > 0)
            {
                for (int i = TopSlot.transform.childCount - 1; i >= 0; i--)
                {
                    var shipUI = TopSlot.transform.GetChild(i).gameObject;
                    var shipUIItem = shipUI.GetComponent<ShipListUI_Item>();
                    if (shipUIItem != null)
                    {
                        Transform targetParent = null;
                        if (shipUIItem.CurrentFleet != null && shipUIItem.CurrentFleet.FleetData.ShipListUIParent != null)
                        {
                            targetParent = shipUIItem.CurrentFleet.FleetData.ShipListUIParent.transform;
                        }
                        else if (shipUIItem.CurrentStarSyst != null && shipUIItem.CurrentStarSyst.StarSysData.ShipListUIParent != null)
                        {
                            targetParent = shipUIItem.CurrentStarSyst.StarSysData.ShipListUIParent.transform;
                        }

                        if (targetParent != null)
                        {
                            shipUI.transform.SetParent(targetParent, false);
                            shipUI.SetActive(true); // ✅ CRITICAL: Ensure ship UI is active after reparenting
                            Debug.Log($"Reparented ship UI '{shipUIItem.ShipController?.ShipData?.ShipName}' from TopSlot to owner and activated");
                        }
                        else
                        {
                            Debug.LogWarning($"❌ Cannot reparent ship UI '{shipUIItem.ShipController?.ShipData?.ShipName}' - no target parent found! CurrentFleet={(shipUIItem.CurrentFleet?.name ?? "NULL")}, CurrentStarSyst={(shipUIItem.CurrentStarSyst?.name ?? "NULL")}");
                        }
                    }
                }
            }

            // Process BottomSlot ships
            if (BottomSlot != null && BottomSlot.transform.childCount > 0)
            {
                for (int i = BottomSlot.transform.childCount - 1; i >= 0; i--)
                {
                    var shipUI = BottomSlot.transform.GetChild(i).gameObject;
                    var shipUIItem = shipUI.GetComponent<ShipListUI_Item>();
                    if (shipUIItem != null)
                    {
                        Transform targetParent = null;
                        if (shipUIItem.CurrentFleet != null && shipUIItem.CurrentFleet.FleetData.ShipListUIParent != null)
                        {
                            targetParent = shipUIItem.CurrentFleet.FleetData.ShipListUIParent.transform;
                        }
                        else if (shipUIItem.CurrentStarSyst != null && shipUIItem.CurrentStarSyst.StarSysData.ShipListUIParent != null)
                        {
                            targetParent = shipUIItem.CurrentStarSyst.StarSysData.ShipListUIParent.transform;
                        }

                        if (targetParent != null)
                        {
                            shipUI.transform.SetParent(targetParent, false);
                            shipUI.SetActive(true); // ✅ CRITICAL: Ensure ship UI is active after reparenting
                            Debug.Log($"Reparented ship UI '{shipUIItem.ShipController?.ShipData?.ShipName}' from BottomSlot to owner and activated");
                        }
                        else
                        {
                            Debug.LogWarning($"❌ Cannot reparent ship UI '{shipUIItem.ShipController?.ShipData?.ShipName}' - no target parent found! CurrentFleet={(shipUIItem.CurrentFleet?.name ?? "NULL")}, CurrentStarSyst={(shipUIItem.CurrentStarSyst?.name ?? "NULL")}");
                        }
                    }
                }
            }

            Debug.Log($"ReparentShipUIsToOwners: TopSlot remaining={TopSlot?.transform.childCount ?? 0}, BottomSlot remaining={BottomSlot?.transform.childCount ?? 0}");
        }

        public void CommitShipDeployForNewFleetAndClose(Action onCommitComplete)
        {
            Debug.Log($"=== COMMIT (Immediate) START ===");
            Debug.Log($"TopFleet={TopFleet?.name}, BottomFleet={BottomFleet?.name}");

            // Since ShipControllers are already correctly parented (via drag/drop AddToFleet/AddToStarSystem),
            // we can rebuild UI parenting from the ShipController hierarchy
            ReparentUIFromShipControllerHierarchy(TopFleet);
            ReparentUIFromShipControllerHierarchy(BottomFleet);
            ReparentUIFromShipControllerHierarchy(TopStarSyst);
            ReparentUIFromShipControllerHierarchy(BottomStarSyst);

            // ✅ Also ensure any ship UIs still in slots are moved to owners
            ReparentShipUIsToOwners();

            // Update fleet max warp
            if (TopFleet != null)
            {
                try { TopFleet.UpdateMaxWarp(); } catch { }
            }
            if (BottomFleet != null)
            {
                try { BottomFleet.UpdateMaxWarp(); } catch { }
            }

            // Clean up UIs before hiding panel
            FleetMenuUIController.Instance?.MoveBackAnyaFleetUIGO();
            StarSysMenuUIController.Instance?.MoveBackAnyStarSysUIGO();

            // Hide the deploy menu
            HideShipDeployMenuView();
            gameObject.SetActive(false);

            Debug.Log($"=== COMMIT (Immediate) END ===");

            // Callback for after-commit cleanup
            onCommitComplete?.Invoke();
        }

        public void CommitMergeAndClose(Action onCommitComplete)
        {
            Debug.Log($"=== COMMIT MERGE START ===");
            Debug.Log($"TopFleet={TopFleet?.name}, TopStarSyst={TopStarSyst?.name}, BottomFleet={BottomFleet?.name}, BottomStarSyst={BottomStarSyst?.name}");

            // Get all ships currently in BottomSlot
            List<ShipController> shipsToMerge = new List<ShipController>();
            for (int i = 0; i < BottomSlot.transform.childCount; i++)
            {
                var shipUI = BottomSlot.transform.GetChild(i).gameObject;
                var shipUIItem = shipUI.GetComponent<ShipListUI_Item>();
                if (shipUIItem != null && shipUIItem.ShipController != null)
                {
                    shipsToMerge.Add(shipUIItem.ShipController);
                }
            }

            Debug.Log($"Found {shipsToMerge.Count} ships in BottomSlot to merge");

            // Determine target (where ships will end up)
            FleetController targetFleet = BottomFleet;
            StarSysController targetSystem = BottomStarSyst;

            // Move all ships to target
            foreach (var ship in shipsToMerge)
            {
                if (ship == null) continue;

                // Remove from current owner
                if (ship.ShipData.CurrentFleetController != null && ship.ShipData.CurrentFleetController != targetFleet)
                {
                    ship.ShipData.CurrentFleetController.RemoveFromShipList(ship);
                    Debug.Log($"Removed ship '{ship.ShipData.ShipName}' from fleet '{ship.ShipData.CurrentFleetController.name}'");
                }
                else if (ship.ShipData.CurrentStarSysController != null && ship.ShipData.CurrentStarSysController != targetSystem)
                {
                    ship.ShipData.CurrentStarSysController.RemoveFromShipList(ship);
                    Debug.Log($"Removed ship '{ship.ShipData.ShipName}' from system '{ship.ShipData.CurrentStarSysController.name}'");
                }

                // Add to target
                if (targetFleet != null)
                {
                    targetFleet.AddToShipList(ship);
                    ship.ShipData.CurrentFleetController = targetFleet;
                    ship.ShipData.CurrentStarSysController = null;
                    Debug.Log($"Added ship '{ship.ShipData.ShipName}' to fleet '{targetFleet.name}'");
                }
                else if (targetSystem != null)
                {
                    targetSystem.AddToShipList(ship);
                    ship.ShipData.CurrentStarSysController = targetSystem;
                    ship.ShipData.CurrentFleetController = null;
                    Debug.Log($"Added ship '{ship.ShipData.ShipName}' to system '{targetSystem.name}'");
                }
            }

            // Update max warp for fleets
            if (targetFleet != null)
            {
                targetFleet.UpdateMaxWarp();
            }
            if (TopFleet != null && TopFleet != targetFleet)
            {
                TopFleet.UpdateMaxWarp();
            }

            // Reparent UI elements to their final homes
            ReparentUIFromShipControllerHierarchy(targetFleet);
            ReparentUIFromShipControllerHierarchy(targetSystem);
            ReparentUIFromShipControllerHierarchy(TopFleet);
            ReparentUIFromShipControllerHierarchy(TopStarSyst);

            // ✅ Ensure any ship UIs still in slots are moved
            ReparentShipUIsToOwners();

            // Clean up UIs before hiding panel
            FleetMenuUIController.Instance?.MoveBackAnyaFleetUIGO();
            StarSysMenuUIController.Instance?.MoveBackAnyStarSysUIGO();

            // Hide the deploy menu
            HideShipDeployMenuView();
            gameObject.SetActive(false);

            Debug.Log($"=== COMMIT MERGE END ===");

            // Callback
            onCommitComplete?.Invoke();
        }

        private void ReparentUIFromShipControllerHierarchy(FleetController fleet)
        {
            if (fleet == null) return;

            // Debug.Log($"ReparentUIFromShipControllerHierarchy: Processing fleet '{fleet.name}'");

            // Ensure ShipListUIParent exists
            if (fleet.FleetData?.ShipListUIParent == null)
            {
                var uiFields = fleet.FleetUIGameObject?.GetComponent<FleetUI_Fields>();
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
            var shipControllers = fleet.GetComponentsInChildren<ShipController>(true);
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
                var uiFields = starSys.StarSysUIGameObject?.GetComponent<StarSysUI_Fields>();
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
            var shipControllers = starSys.GetComponentsInChildren<ShipController>(true);
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
            var all = UnityEngine.Object.FindObjectsByType<ShipListUI_Item>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            // Debug.Log($"DumpAllShipListUIs: found {all.Length} ShipListUI_Item instances");
            for (int i = 0; i < all.Length; i++)
            {
                var item = all[i];
                var parentName = item.gameObject.transform.parent != null ? item.gameObject.transform.parent.name : "<null>";
                // Debug.Log($"ShipUI '{item.gameObject.name}': parent='{parentName}', CurrentFleet='{item.CurrentFleet?.name}', CurrentStarSyst='{item.CurrentStarSyst?.name}', ShipController='{item.ShipController?.name}'");
            }
        }
    }
}