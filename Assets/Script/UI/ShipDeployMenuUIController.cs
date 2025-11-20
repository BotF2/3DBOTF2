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
            chosenFleet.FleetData.ShipsList[i].transform.SetParent(BottomSlot.transform, false);
            chosenFleet.FleetData.ShipsList[i].ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
        }
        SetUpTopShipLists();
    }
    internal void SetUpBottomShipLists(StarSysController StarSysLooking)
    {
        if (StarSysLooking.SettingUpNewFleet) return; // new fleet has no ships yet
        else
        {
            var galaxyMenu = GalaxyMenuUIController.Instance;
            for (int i = 0; galaxyMenu.StarSysConSelectedForShipDeploy.StarSysData.ShipsList.Count > i; i++)
            {
                galaxyMenu.StarSysConSelectedForShipDeploy.StarSysData.ShipsList[i].transform.SetParent(BottomSlot.transform, false);
                galaxyMenu.StarSysConSelectedForShipDeploy.StarSysData.ShipsList[i].ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
            }
        }
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
        }
        else if (galaxyUI.StarSysLookingForShipDeploy != null)
        {
            var shipCon = galaxyUI.StarSysLookingForShipDeploy.StarSysData.ShipsList;
            for (int i = 0; shipCon.Count > i; i++)
            {
                shipCon[i].ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
            }
        }
    }
    public GameObject[] GetTopSlotShipListItems()
    {
        List<GameObject> shipListItems = new List<GameObject>();
        for (int i = 0; TopSlot.transform.childCount > i; i++)
        {
            shipListItems.Add(TopSlot.transform.GetChild(i).gameObject);
        }
        return shipListItems.ToArray();
    }
    public GameObject[] GetBottomSlotShipListItems()
    {
        List<GameObject> shipListItems = new List<GameObject>();
        for (int i = 0; BottomSlot.transform.childCount > i; i++)
        {
            shipListItems.Add(BottomSlot.transform.GetChild(i).gameObject);
        }
        return shipListItems.ToArray();
    }
}
