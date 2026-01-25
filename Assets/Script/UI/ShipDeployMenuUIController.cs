using Assets.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        ShipManager.Instance?.ProcessPendingShipUIs();

        ShipDeployPanel.SetActive(true);
        // Bring panel to front
        transform.SetAsLastSibling();

        Debug.Log($"ShowShipDeployMenuView: opened. TopSlot children={TopSlot?.transform.childCount ?? 0}, BottomSlot children={BottomSlot?.transform.childCount ?? 0}");
    }

    public void HideShipDeployMenuView()
    {
        ShipDeployPanel.SetActive(false);
    }

    internal void SetUpBottomShipLists(FleetController chosenFleet)
    {
        if (chosenFleet == null)
        {
            Debug.LogWarning("SetUpBottomShipLists: chosenFleet is null.");
            return;
        }

        // Make sure any ship UI created earlier is reparented to its owners.
        ShipManager.Instance?.ProcessPendingShipUIs();

        var ships = chosenFleet.FleetData?.ShipsList;
        if (ships == null || ships.Count == 0)
        {
            Debug.Log($"SetUpBottomShipLists: chosenFleet {chosenFleet?.name} has no ships.");
        }
        else
        {
            for (int i = 0; i < ships.Count; i++)
            {
                var ship = ships[i];
                if (ship == null) continue;

                // Ensure a ShipList UI exists for this ship
                if (ship.ShipListUIGameObject == null)
                {
                    ShipManager.Instance?.InstantiateShipListUIGameObject(ship, chosenFleet.gameObject);
                }

                // Process pending reparenting so the UI exists and can be moved into slot
                ShipManager.Instance?.ProcessPendingShipUIs();

                if (ship.ShipListUIGameObject != null)
                {
                    ship.ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
                }
                else
                {
                    Debug.LogWarning($"SetUpBottomShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                }
            }
        }

        BottomFleet = chosenFleet;
        BottomStarSyst = null;
    }
    internal void SetUpBottomShipLists(StarSysController StarSysLooking, bool deployNotMerge)
    {
        if (StarSysLooking.SettingUpNewFleet) return; // new fleet has no ships yet
        else
        {
            var galaxyMenu = GalaxyMenuUIController.Instance;
            List<ShipController> shipConList;
            if (deployNotMerge)
                shipConList = galaxyMenu.StarSystSelectedForShipDeploy.StarSysData?.ShipsList;
            else
                shipConList = galaxyMenu.StarSystSelectedForShipMerge.StarSysData?.ShipsList;
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
        for (int i = 0; i < shipList.Count; i++)
        {
            if (shipList[i] != null)
            {
                shipList[i].ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
            }
        }
    }
    internal void SetUpTopShipLists() // load top ship deployment view containers 
    {
        // Ensure any pending ship UI reparenting is attempted first
        ShipManager.Instance?.ProcessPendingShipUIs();

        var galaxyUI = GalaxyMenuUIController.Instance;
        if (galaxyUI == null)
        {
            Debug.LogError("SetUpTopShipLists: GalaxyMenuUIController.Instance is null.");
            return;
        }

        if (galaxyUI.FleetLookingForShipDeploy != null)
        {
            var shipConList = galaxyUI.FleetLookingForShipDeploy.FleetData?.ShipsList;
            if (shipConList == null || shipConList.Count == 0)
            {
                Debug.Log($"SetUpTopShipLists: Fleet {galaxyUI.FleetLookingForShipDeploy.name} has no ships to show.");
            }
            else
            {
                for (int i = 0; i < shipConList.Count; i++)
                {
                    var ship = shipConList[i];
                    if (ship == null) continue;

                    if (ship.ShipListUIGameObject == null)
                    {
                        ShipManager.Instance?.InstantiateShipListUIGameObject(ship, galaxyUI.FleetLookingForShipDeploy.gameObject);
                    }

                    ShipManager.Instance?.ProcessPendingShipUIs();

                    if (ship.ShipListUIGameObject != null)
                    {
                        ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
                    }
                    else
                    {
                        Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                    }
                }
            }

            TopFleet = galaxyUI.FleetLookingForShipDeploy;
            TopStarSyst = null;
        }
        else if (galaxyUI.StarSystLookingForShipDeploy != null)
        {
            var shipConList = galaxyUI.StarSystLookingForShipDeploy.StarSysData?.ShipsList;
            if (shipConList == null || shipConList.Count == 0)
            {
                Debug.Log($"SetUpTopShipLists: StarSys {galaxyUI.StarSystLookingForShipDeploy.name} has no ships to show.");
            }
            else
            {
                for (int i = 0; i < shipConList.Count; i++)
                {
                    var ship = shipConList[i];
                    if (ship == null) continue;

                    if (ship.ShipListUIGameObject == null)
                    {
                        ShipManager.Instance?.InstantiateShipListUIGameObject(ship, galaxyUI.StarSystLookingForShipDeploy.gameObject);
                    }

                    ShipManager.Instance?.ProcessPendingShipUIs();

                    if (ship.ShipListUIGameObject != null)
                    {
                        ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
                    }
                    else
                    {
                        Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                    }
                }
            }

            TopStarSyst = galaxyUI.StarSystLookingForShipDeploy;
            TopFleet = null;
        }
        if (galaxyUI.FleetLookingForShipMerge != null)
        {
            var shipConList = galaxyUI.FleetLookingForShipMerge.FleetData?.ShipsList;
            if (shipConList == null || shipConList.Count == 0)
            {
                Debug.Log($"SetUpTopShipLists: Fleet {galaxyUI.FleetLookingForShipMerge.name} has no ships to show.");
            }
            else
            {
                for (int i = 0; i < shipConList.Count; i++)
                {
                    var ship = shipConList[i];
                    if (ship == null) continue;

                    if (ship.ShipListUIGameObject == null)
                    {
                        ShipManager.Instance?.InstantiateShipListUIGameObject(ship, galaxyUI.FleetLookingForShipMerge.gameObject);
                    }

                    ShipManager.Instance?.ProcessPendingShipUIs();

                    if (ship.ShipListUIGameObject != null)
                    {
                        ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
                    }
                    else
                    {
                        Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
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
                Debug.Log($"SetUpTopShipLists: StarSys {galaxyUI.StarSystLookingForShipMerge.name} has no ships to show.");
            }
            else
            {
                for (int i = 0; i < shipConList.Count; i++)
                {
                    var ship = shipConList[i];
                    if (ship == null) continue;

                    if (ship.ShipListUIGameObject == null)
                    {
                        ShipManager.Instance?.InstantiateShipListUIGameObject(ship, galaxyUI.StarSystLookingForShipMerge.gameObject);
                    }

                    ShipManager.Instance?.ProcessPendingShipUIs();

                    if (ship.ShipListUIGameObject != null)
                    {
                        ship.ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
                    }
                    else
                    {
                        Debug.LogWarning($"SetUpTopShipLists: Ship UI missing for ship {ship?.name} after Instantiate/ProcessPending.");
                    }
                }
            }
            TopStarSyst = galaxyUI.StarSystLookingForShipMerge;
            TopFleet = null;
        }
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
        ReconcileMissingShips(bottomShipControllerList, newBottomShipControllerList, bottomFleet.FleetData?.ShipListUIParent?.transform);

        bottomFleet.FleetData.ShipsList = newBottomShipControllerList;
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
                Debug.Log($"ReconcileMissingShips: restored missing ship '{orig.name}' to owner UI parent.");
            }
        }
    }

    // synchronize model ownership to match UI after drag/drop ----
    // After UI objects have been moved between UI parents we rebuild FleetData/StarSysData ship lists
    // from the UI elements. This keeps game play (FleetData.ShipsList / StarSysData.ShipsList)
    // as the single source of truth for ownership while allowing UI items to be transient and moved.
    private void UpdateOwnersFromUI() // only used if we need a check that slots for ships UIGO do match model data
    {
        Debug.Log($"UpdateOwnersFromUI: TopSlot children={TopSlot?.transform.childCount ?? 0}, BottomSlot children={BottomSlot?.transform.childCount ?? 0}");
        // Collect affected owners to consider
        var fleetsToConsider = new HashSet<FleetController>();
        var starSysToConsider = new HashSet<StarSysController>();

        if (TopFleet != null) fleetsToConsider.Add(TopFleet);
        if (BottomFleet != null) fleetsToConsider.Add(BottomFleet);
        if (TopStarSyst != null) starSysToConsider.Add(TopStarSyst);
        if (BottomStarSyst != null) starSysToConsider.Add(BottomStarSyst);

        // Prepare temporary results (do not mutate authoritative lists until we have new data)
        var newFleetLists = new Dictionary<FleetController, List<ShipController>>();
        var newStarLists = new Dictionary<StarSysController, List<ShipController>>();

        foreach (var f in fleetsToConsider) newFleetLists[f] = new List<ShipController>();
        foreach (var s in starSysToConsider) newStarLists[s] = new List<ShipController>();

        // Helper to collect from a UI parent container
        void CollectFromParent(Transform parent)
        {
            if (parent == null) return;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i).gameObject;
                var item = child.GetComponent<ShipListUI_Item>();
                if (item == null || item.ShipController == null) continue;

                if (item.CurrentFleet != null)
                {
                    if (!newFleetLists.TryGetValue(item.CurrentFleet, out var list))
                    {
                        list = new List<ShipController>();
                        newFleetLists[item.CurrentFleet] = list;
                    }
                    if (!list.Contains(item.ShipController)) list.Add(item.ShipController);
                }
                else if (item.CurrentStarSyst != null)
                {
                    if (!newStarLists.TryGetValue(item.CurrentStarSyst, out var sList))
                    {
                        sList = new List<ShipController>();
                        newStarLists[item.CurrentStarSyst] = sList;
                    }
                    if (!sList.Contains(item.ShipController)) sList.Add(item.ShipController);
                }
            }
        }

        // Collect from the active UI slots first (these represent the player's drag/drop view)
        CollectFromParent(TopSlot?.transform);
        CollectFromParent(BottomSlot?.transform);

        // Also collect from the owner UI parents themselves (they might contain items if Deploy flow placed them there)
        foreach (var f in fleetsToConsider)
            CollectFromParent(f.FleetData?.ShipListUIParent?.transform);
        foreach (var s in starSysToConsider)
            CollectFromParent(s.StarSysData?.ShipListUIParent?.transform);

        // Now we have the new ownership mapping in newFleetLists/newStarLists.
        // Assign them to authoritative model structures.
        foreach (var kv in newFleetLists)
        {
            var fleet = kv.Key;
            var shipList = kv.Value ?? new List<ShipController>();
            fleet.FleetData.ShipsList = shipList;
            try { fleet.UpdateMaxWarp(); } catch { }
        }

        foreach (var kv in newStarLists)
        {
            var star = kv.Key;
            var shipList = kv.Value ?? new List<ShipController>();
            star.StarSysData.ShipsList = shipList;
        }

        // Ensure UI objects are parented to their owner containers
        foreach (var kv in newFleetLists)
        {
            var fleet = kv.Key;
            var parent = fleet.FleetData?.ShipListUIParent;
            if (parent == null) continue;
            foreach (var ship in kv.Value)
            {
                if (ship?.ShipListUIGameObject != null && ship.ShipListUIGameObject.transform.parent != parent.transform)
                {
                    ship.ShipListUIGameObject.transform.SetParent(parent.transform, false);
                }
            }
        }
        foreach (var kv in newStarLists)
        {
            var star = kv.Key;
            var parent = star.StarSysData?.ShipListUIParent;
            if (parent == null) continue;
            foreach (var ship in kv.Value)
            {
                if (ship?.ShipListUIGameObject != null && ship.ShipListUIGameObject.transform.parent != parent.transform)
                {
                    ship.ShipListUIGameObject.transform.SetParent(parent.transform, false);
                }
            }
        }

        Debug.Log($"UpdateOwnersFromUI completed: fleets updated={newFleetLists.Count}, stars updated={newStarLists.Count}");
    }

    private void RebuildFromUIParent(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i).gameObject;
            var item = child.GetComponent<ShipListUI_Item>();
            if (item == null || item.ShipController == null) continue;

            // If this UI item is assigned to a fleet, add it to that fleet's data and call FleetController helper
            if (item.CurrentFleet != null)
            {
                var fleet = item.CurrentFleet;
                if (!fleet.FleetData.ShipsList.Contains(item.ShipController))
                {
                    fleet.FleetData.ShipsList.Add(item.ShipController);
                }

                // Attempt to update FleetController internal state via its public API if available.
                // This keeps FleetController in sync (e.g. visual updates, internal bookkeeping).
                try
                {
                    fleet.AddToShipList(item.ShipController);
                }
                catch
                {
                    // If AddToShipList doesn't exist or throws, we still have fleet.FleetData updated.
                }
            }
            else if (item.CurrentStarSyst != null)
            {
                var sys = item.CurrentStarSyst;
                if (!sys.StarSysData.ShipsList.Contains(item.ShipController))
                {
                    sys.StarSysData.ShipsList.Add(item.ShipController);
                }

                // If StarSysController has helper methods for adding a ship, call them similarly (best-effort).
                try
                {
                    // Example: sys.SomeAddShipMethod(item.ShipController);
                }
                catch
                {
                }
            }

            // Optional: update the ShipController to know its current owner if it exposes such a property.
            // Use reflection or a known property name if necessary. Keep this optional and silent if not present.
            try // not working, prop null !!
            {

            }
            catch { }
        }
    }
    public void CommitShipDeployAndClose()
    {
        gameObject.SetActive(true);
        // Run async to let drag/drop & layout events finish
        StartCoroutine(CommitShipDeployCoroutine());
    }

    private System.Collections.IEnumerator CommitShipDeployCoroutine()
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

        // 3) Wait one frame to allow EndDrag handlers and UI events to finish.
        //    If you still see missing items, change to yield return null; yield return null; (two frames).
        yield return null;

        // 4) Diagnostic dump to see where UI items are right now
        Debug.Log($"CommitShipDeployAndClose (pre): TopSlot children={TopSlot?.transform.childCount ?? 0} BottomSlot children={BottomSlot?.transform.childCount ?? 0}");
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

        // 7) Finish up and report
        GalaxyMenuUIController.Instance?.CompleteShipExchange();
        Debug.Log($"CommitShipDeployAndClose committed: TopSlot={TopSlot?.transform.childCount ?? 0} BottomSlot={BottomSlot?.transform.childCount ?? 0}");

        yield break;
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
        Debug.Log($"DumpAllShipListUIs: found {all.Length} ShipListUI_Item instances");
        for (int i = 0; i < all.Length; i++)
        {
            var item = all[i];
            var parentName = item.gameObject.transform.parent != null ? item.gameObject.transform.parent.name : "<null>";
            Debug.Log($"ShipUI '{item.gameObject.name}': parent='{parentName}', CurrentFleet='{item.CurrentFleet?.name}', CurrentStarSyst='{item.CurrentStarSyst?.name}', ShipController='{item.ShipController?.name}'");
        }
    }
}