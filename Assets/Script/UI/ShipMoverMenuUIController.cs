using Assets.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShipMoverMenuUIController : MonoBehaviour 
{
    public static ShipMoverMenuUIController Instance;
    public GameObject ShipMoveMenuView;
    public GameObject ShipMoveContainer;
    public GameObject TopSlot;
    public GameObject BottomSlot;
    private FleetController fleetConLookingAtShips;
    private StarSysController starSysConLookingAtShips;
    private FleetController fleetConSelectedForShips;
    private StarSysController starSysConSelectedForShips;
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
        ShipMoveMenuView.SetActive(true);
    }
   
    public void HideShipMoveMenuView()
    {
        ShipMoveMenuView.SetActive(false);
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


    internal void UpdateShipLists()
    {
        //for (int i = 0; i < fleetCon.FleetData.ShipsList.Count; i++)
        //{
        //    if (fleetCon.FleetData.ShipsList[i].ShipListUIGameObject != null)
        //    {
        //        var transforms = fleetCon.FleetUIGameObject.GetComponentsInChildren<Transform>(true);
        //        for (int k = 0; k < transforms.Length; k++)
        //        {
        //            if (transforms[k].gameObject.name == "FleetShipContent")
        //            {
        //                fleetShipListContainer = transforms[k].gameObject;
        //                break;
        //            }
        //        }
        //        fleetCon.FleetData.ShipsList[i].ShipListUIGameObject.transform.SetParent(fleetShipListContainer.transform, false);
        //    }
        //}
        // existing ship list UIs (if present)
        //List <GameObject> topChildGO = new List<GameObject>();
        //List<GameObject> bottomChildGO = new List<GameObject>();
        //// Iterate through the children of the parentGameObject's Transform
        ////for (int i = 0; i < TopSlot.transform.childCount; i++)
        ////{
        ////    topChildGO.Add(TopSlot.transform.GetChild(i).gameObject);
        ////}
        ////for (int i = 0; i < BottomSlot.transform.childCount; i++)
        ////{
        ////    bottomChildGO.Add(BottomSlot.transform.GetChild(i).gameObject);
        ////}
        //var galaxyUI = GalaxyMenuUIController.Instance;
        //if (galaxyUI.FleetLookingForShipExchange != null)
        //{
        //    galaxyUI.FleetLookingForShipExchange.FleetData.ShipsList.Clear();
        //    for (int i = 0; topChildGO.Count > i; i++)
        //    {

        //        galaxyUI.FleetLookingForShipExchange.FleetData.ShipsList.Add(topChildGO[i].GetComponent<ShipController>());
        //    }
        //}
        //else if (galaxyUI.StarSysLookingForShipExchange != null)
        //{
        //    galaxyUI.StarSysLookingForShipExchange.StarSysData.ShipsList.Clear();
        //    for (int i = 0; topChildGO.Count > i; i++)
        //    {
        //        galaxyUI.StarSysLookingForShipExchange.StarSysData.ShipsList.Add(topChildGO[i].GetComponent<ShipController>());
        //    }
        //}
        
        //if (fleetConSelectedForShips != null)
        //{
        //    fleetConSelectedForShips.FleetData.ShipsList.Clear();
        //    for (int i = 0; bottomChildGO.Count > i; i++)
        //    {
        //        fleetConSelectedForShips.FleetData.ShipsList.Add(bottomChildGO[i].GetComponent<ShipController>());
        //    }
        //}
        //else if (starSysConSelectedForShips != null)
        //{
        //    starSysConSelectedForShips.StarSysData.ShipsList.Clear();
        //    for (int i = 0; bottomChildGO.Count > i; i++)
        //    {
        //        starSysConSelectedForShips.StarSysData.ShipsList.Add(bottomChildGO[i].GetComponent<ShipController>());
        //    }
        //}
    }

    internal void WhoIsSelectedForShipMove(FleetController fleetController)
    {
        fleetConSelectedForShips = fleetController;
        starSysConSelectedForShips = null;
    }
    internal void WhoIsSelectedForShipMove(StarSysController starSysController)
    {
        starSysConSelectedForShips = starSysController;
        fleetConSelectedForShips = null;
    }
}
