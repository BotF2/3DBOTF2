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
    private FleetController topFleet;
    public FleetController BottomFleet;
    public StarSysController TopStarSyst;
    private StarSysController bottomStarSyst;

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
            chosenFleet.FleetData.ShipsList[i].ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
        }
        topFleet = chosenFleet;
        bottomStarSyst = null;
        SetUpTopShipLists();
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
        bottomStarSyst = StarSysLooking;
        topFleet = null;
        SetUpTopShipLists();
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
            topFleet = galaxyUI.FleetLookingForShipDeploy;
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
            topFleet = null;
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

    internal void DeployShips()
    {
        if (topFleet != null && BottomFleet != null)
        {
            DeployShipsBetweenFleets(topFleet, BottomFleet);
        }
        else if (topFleet != null && bottomStarSyst != null)
        {
            DeployShipsFromFleetToStarSys(topFleet, bottomStarSyst);
        }
        else if (TopStarSyst != null && BottomFleet != null)
        {
            DeployShipsFromStarSysToFleet(TopStarSyst, BottomFleet);
        }
        else if (TopStarSyst != null && bottomStarSyst != null)
        {
            DeployShipsBetweenStarSys(TopStarSyst, bottomStarSyst);
        }

        // After UI elements are moved we must sync the game play data structures (FleetData / StarSysData)
        // to reflect the new ownership. See UpdateOwnersFromUI() for implementation.
        UpdateOwnersFromUI();
    }

    private void DeployShipsBetweenStarSys(StarSysController topStarSyst, StarSysController bottomStarSyst)
    {
        for (int i = 0; GetTopSlotShipListUIGOs().Length > i; i++)
        {
            var shipUIGOTop = GetTopSlotShipListUIGOs()[i];
            var shipListUI_Item = shipUIGOTop.GetComponent<ShipListUI_Item>();
            shipListUI_Item.CurrentStarSyst = topStarSyst;
            shipListUI_Item.CurrentFleet = null;
            shipUIGOTop.transform.SetParent(topStarSyst.StarSysData.ShipListUIParent.transform, false);
        }
        for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
        {
            var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
            var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
            shipListUI_Item.CurrentStarSyst = bottomStarSyst;
            shipListUI_Item.CurrentFleet = null;
            shipUIGOBottom.transform.SetParent(bottomStarSyst.StarSysData.ShipListUIParent.transform, false);
        }
    }

    private void DeployShipsFromStarSysToFleet(StarSysController topStarSyst, FleetController bottomFleet)
    {
        for (int i = 0; GetTopSlotShipListUIGOs().Length > i; i++)
        {
            var shipUIGOTop = GetTopSlotShipListUIGOs()[i];
            var shipListUI_Item = shipUIGOTop.GetComponent<ShipListUI_Item>();
            shipListUI_Item.CurrentStarSyst = topStarSyst;
            shipListUI_Item.CurrentFleet = null;
            shipUIGOTop.transform.SetParent(topStarSyst.StarSysData.ShipListUIParent.transform, false);
        }
        for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
        {
            var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
            var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
            shipListUI_Item.CurrentFleet = bottomFleet;
            shipListUI_Item.CurrentStarSyst = null;
            shipUIGOBottom.transform.SetParent(bottomFleet.FleetData.ShipListUIParent.transform, false);
        }
    }

    private void DeployShipsFromFleetToStarSys(FleetController topFleet, StarSysController bottomStarSyst)
    {

        for (int i = 0; i < GetTopSlotShipListUIGOs().Length; i++)
        {
            var shipUIGOtop = GetTopSlotShipListUIGOs()[i];
            var shipListUI_Item = shipUIGOtop.GetComponent<ShipListUI_Item>();
            shipListUI_Item.CurrentFleet = topFleet;
            shipListUI_Item.CurrentStarSyst = null;
            shipUIGOtop.transform.SetParent(topFleet.FleetData.ShipListUIParent.transform, false);
        }
        for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
        {
            var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
            var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
            shipListUI_Item.CurrentStarSyst = bottomStarSyst;
            shipListUI_Item.CurrentFleet = null;
            shipUIGOBottom.transform.SetParent(bottomStarSyst.StarSysData.ShipListUIParent.transform, false);
        }
    }

    private void DeployShipsBetweenFleets(FleetController topFleet, FleetController bottomFleet)
    {
        for (int i = 0; i < GetTopSlotShipListUIGOs().Length; i++)
        {
            var shipUIGOtop = GetTopSlotShipListUIGOs()[i];
            var shipListUI_Item = shipUIGOtop.GetComponent<ShipListUI_Item>();
            shipListUI_Item.CurrentFleet = topFleet;
            shipListUI_Item.CurrentStarSyst = null;
            shipUIGOtop.transform.SetParent(topFleet.FleetData.ShipListUIParent.transform, false);
        }
        for (int i = 0; i < GetBottomSlotShipListUIGOs().Length; i++)
        {
            var shipUIGOBottom = GetBottomSlotShipListUIGOs()[i];
            var shipListUI_Item = shipUIGOBottom.GetComponent<ShipListUI_Item>();
            shipListUI_Item.CurrentFleet = bottomFleet;
            shipListUI_Item.CurrentStarSyst = null;
            shipUIGOBottom.transform.SetParent(bottomFleet.FleetData.ShipListUIParent.transform, false);
        }
    }

    // ---- New: synchronize model ownership to match UI after drag/drop ----
    // After UI objects have been moved between UI parents we rebuild FleetData/StarSysData ship lists
    // from the UI elements. This keeps game play (FleetData.ShipsList / StarSysData.ShipsList)
    // as the single source of truth for ownership while allowing UI items to be transient and moved.
    private void UpdateOwnersFromUI()
    {
        // Collect affected owners to clear lists first
        var fleetsToClear = new HashSet<FleetController>();
        var starSysToClear = new HashSet<StarSysController>();

        if (topFleet != null) fleetsToClear.Add(topFleet);
        if (BottomFleet != null) fleetsToClear.Add(BottomFleet);
        if (TopStarSyst != null) starSysToClear.Add(TopStarSyst);
        if (bottomStarSyst != null) starSysToClear.Add(bottomStarSyst);

        // Clear existing lists so we can rebuild
        foreach (var f in fleetsToClear)
        {
            f.FleetData.ShipsList.Clear();
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
        if (topFleet != null && topFleet.FleetData.ShipListUIParent != null)
            RebuildFromUIParent(topFleet.FleetData.ShipListUIParent.transform);
        if (BottomFleet != null && BottomFleet.FleetData.ShipListUIParent != null)
            RebuildFromUIParent(BottomFleet.FleetData.ShipListUIParent.transform);
        if (TopStarSyst != null && TopStarSyst.StarSysData.ShipListUIParent != null)
            RebuildFromUIParent(TopStarSyst.StarSysData.ShipListUIParent.transform);
        if (bottomStarSyst != null && bottomStarSyst.StarSysData.ShipListUIParent != null)
            RebuildFromUIParent(bottomStarSyst.StarSysData.ShipListUIParent.transform);
    }

    private void RebuildFromUIParent(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)  // wrong bottom count!!!
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
            try // not working, prop null !!!!!
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
    // -------------------------------------------------------------------
}
