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
        ShipDeployPanel.SetActive(true);

        transform.SetAsLastSibling();
    }

    public void HideShipDeployMenuView()
    {
        ShipDeployPanel.SetActive(false);
    }

    internal void SetUpBottomShipLists(FleetController chosenFleet)
    {
        for (int i = 0; chosenFleet.FleetData.ShipsList.Count > i; i++)
        {
            // UI item is a separate prefab instance - move the UI object into the slot
            if (chosenFleet.FleetData.ShipsList[i].ShipListUIGameObject == null)
                ShipManager.Instance.InstantiateShipListUIGameObject(chosenFleet.FleetData.ShipsList[i], BottomSlot);
            chosenFleet.FleetData.ShipsList[i].ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
        }
        BottomFleet = chosenFleet;
        BottomStarSyst = null;
        //SetUpTopShipLists();
    }
    internal void SetUpBottomShipLists(StarSysController StarSysLooking)
    {
        if (StarSysLooking.SettingUpNewFleet) return; // new fleet has no ships yet
        else
        {
            var galaxyMenu = GalaxyMenuUIController.Instance;
            for (int i = 0; galaxyMenu.StarSystConSelectedForShipDeploy.StarSysData.ShipsList.Count > i; i++)
            {
                //galaxyMenu.StarSystConSelectedForShipDeploy.StarSysData.ShipsList[i].transform.SetParent(BottomSlot.transform, false);
                galaxyMenu.StarSystConSelectedForShipDeploy.StarSysData.ShipsList[i].ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
            }
        }
        BottomStarSyst = StarSysLooking;
        BottomFleet = null;
        SetUpTopShipLists();
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
        var galaxyUI = GalaxyMenuUIController.Instance;
        if (galaxyUI.FleetLookingForShipDeploy != null)
        {
            var shipCon = galaxyUI.FleetLookingForShipDeploy.FleetData.ShipsList;
            for (int i = 0; shipCon.Count > i; i++)
            {
                shipCon[i].ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
            }
            TopFleet = galaxyUI.FleetLookingForShipDeploy;
            TopStarSyst = null;
        }
        else if (galaxyUI.StarSystLookingForShipDeploy != null)
        {
            var shipCon = galaxyUI.StarSystLookingForShipDeploy.StarSysData.ShipsList;
            for (int i = 0; shipCon.Count > i; i++)
            {
                shipCon[i].ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
            }
            TopStarSyst = galaxyUI.StarSystLookingForShipDeploy;
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
        // The drag handler (ShipListItemDrag) performs authoritative Remove/Add on the model.
        // Keep this UI mover lightweight. If you need to reconcile programmatically, call UpdateOwnersFromUI().
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
            for (int j = 0; j < bottomShipControllerList.Count; j++)
            {
                if (bottomShipControllerList[j] == shipListUI_Item.ShipController)
                {
                    newBottomShipControllerList.Add(bottomShipControllerList[j]);
                }
            }
        }
        bottomFleet.FleetData.ShipsList = newBottomShipControllerList;
    }

    private void DeployShipUIgoFromFleetToStarSys(FleetController topFleet, StarSysController bottomStarSyst)
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
        bottomFleet.FleetData.ShipsList = newBottomShipControllerList;
    }

    // synchronize model ownership to match UI after drag/drop ----
    // After UI objects have been moved between UI parents we rebuild FleetData/StarSysData ship lists
    // from the UI elements. This keeps game play (FleetData.ShipsList / StarSysData.ShipsList)
    // as the single source of truth for ownership while allowing UI items to be transient and moved.
    private void UpdateOwnersFromUI() // only used if we need a check that slots for ships UIGO do match model data
    {
        // Collect affected owners to clear lists first
        var fleetsToClear = new HashSet<FleetController>();
        var starSysToClear = new HashSet<StarSysController>();

        if (TopFleet != null) fleetsToClear.Add(TopFleet);
        if (BottomFleet != null) fleetsToClear.Add(BottomFleet);
        if (TopStarSyst != null) starSysToClear.Add(TopStarSyst);
        if (BottomStarSyst != null) starSysToClear.Add(BottomStarSyst);

        // Clear existing lists so we can rebuild
        foreach (var f in fleetsToClear)
        {
            f.FleetData.ShipsList.Clear(); // clear ship 
        }
        foreach (var s in starSysToClear)
        {
            s.StarSysData.ShipsList.Clear();
        }

        // Rebuild based on UI parents that now hold the ship UI items.
        RebuildFromUIParent(TopSlot.transform);
        RebuildFromUIParent(BottomSlot.transform);

        // Also rebuild any ShipListUIParent containers we used as final parents:
        // topFleet / bottomFleet parents may also contain children depending on Deploy* paths
        if (TopFleet != null && TopFleet.FleetData.ShipListUIParent != null)
            RebuildFromUIParent(TopFleet.FleetData.ShipListUIParent.transform);
        if (BottomFleet != null && BottomFleet.FleetData.ShipListUIParent != null)
            RebuildFromUIParent(BottomFleet.FleetData.ShipListUIParent.transform);
        if (TopStarSyst != null && TopStarSyst.StarSysData.ShipListUIParent != null)
            RebuildFromUIParent(TopStarSyst.StarSysData.ShipListUIParent.transform);
        if (BottomStarSyst != null && BottomStarSyst.StarSysData.ShipListUIParent != null)
            RebuildFromUIParent(BottomStarSyst.StarSysData.ShipListUIParent.transform);
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
            try // not working, prop null !!!!
            {
                var shipCon = item.ShipController;
                var shipType = shipCon.GetType();
                var prop = shipType.GetProperty("CurrentFleetController");
                if (prop != null && item.CurrentFleet != null)
                {
                    prop.SetValue(shipCon, item.CurrentFleet);
                }
            }
            catch
            {
                // ignore if property doesn't exist
            }
        }
    }
}
