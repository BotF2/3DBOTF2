using Assets.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShipDeployMenuUIController : MonoBehaviour 
{
    public static ShipDeployMenuUIController Instance;
    public GameObject ShipDeployPanel;
    public GameObject TopSlot;
    public GameObject BottomSlot;
    public FleetController FleetConLookingAtShips;
    public StarSysController StarSysConLookingAtShips;
    public FleetController FleetConSelectedForShips;
    public StarSysController StarSysConSelectedForShips;
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
    public void ShowShipMoveMenuView()
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
            chosenFleet.FleetData.ShipsList[i].ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
        }
        SetUpTopShipLists();
    }
    internal void SetUpBottomShipLists(StarSysController chosenStarSys)
    {
        for (int i = 0; chosenStarSys.StarSysData.ShipsList.Count > i; i++)
        {
            chosenStarSys.StarSysData.ShipsList[i].ShipListUIGameObject.transform.SetParent(BottomSlot.transform, false);
        }
        SetUpTopShipLists();
    }
    private void SetUpTopShipLists()
    {
        var galaxyUI = GalaxyMenuUIController.Instance;
        if (galaxyUI.FleetLookingForShipExchange != null)
        {
            var listItem =  galaxyUI.FleetLookingForShipExchange.FleetData.ShipsList;
            for (int i = 0; listItem.Count > i; i++)
            {
                listItem[i].ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
            }
        }
        else if (galaxyUI.StarSysLookingForShipExchange != null)
        {
            var listItem = galaxyUI.StarSysLookingForShipExchange.StarSysData.ShipsList;
            for (int i = 0; listItem.Count > i; i++)
            {
                listItem[i].ShipListUIGameObject.transform.SetParent(TopSlot.transform, false);
            }
        }
    }

    internal void WhoIsSelectedForShipMove(FleetController fleetController)
    {
        FleetConSelectedForShips = fleetController;
        StarSysConSelectedForShips = null;
    }
    internal void WhoIsSelectedForShipMove(StarSysController starSysController)
    {
        StarSysConSelectedForShips = starSysController;
        FleetConSelectedForShips = null;
    }
}
