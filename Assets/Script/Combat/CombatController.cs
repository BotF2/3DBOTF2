using Assets.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class CombatController : MonoBehaviour
{
    private CombatData combatData;
    public CombatData CombatData { get { return combatData; } set { combatData = value; } }
    private CombatController combatController;
    public GameObject prefabSphere;
    public int maxPositions = 200; // the max number of positions to generate in the spiral
    public List<Vector2Int> spiralPositions = new List<Vector2Int>();
    int _scoutsSide1;
    int _scoustSide2;
    int _destroyersSide1;
    int _destroyersSide2;
    int _capitalsSide1;
    int _capitalsSide2;
    private List<Vector2Int> _capitalShipSpiralPositionsSide1 = new List<Vector2Int>();
    private List<Vector2Int> _capitalShipSpiralPositionsSide2 = new List<Vector2Int>();
    int _transportsFriend;
    int _transportsEnemy;
    private List<Vector2Int> _transportSpiralPositionsSide1 = new List<Vector2Int>();
    private List<Vector2Int> _transportSpiralPositionsSide2 = new List<Vector2Int>();

    int _totalScoutShips; // the total # of scouts in the list, ToDo: why do we need this when it is just the infall count of scouts?
    int _totalDestroyerShips;
    int _totalCapitalShips;
    int _totalTransportsShips;

    public void SetCombatOrder(Orders theOrder)
    {
        //for (int i = 0; i < CombatManager.Instance.CombatControllers.Count; i++)
        //{
        //    combatController = CombatManager.Instance.CombatControllers[i];
        //    if (CombatUIController.Instance.CombatController == combatController)
        //    {
        this.CombatData.Order = theOrder;

                //combatController.ActOnCombatOrders(theOrder);
        //    }
        //}
    }
    public void EndCombat()
    {
        ResetFriendAndEnemyLists(); // Resetting friend and enemy lists
    }
    public void ResetFriendAndEnemyLists()
    {
        combatController.CombatData.SideOneShipCons.Clear();
        combatController.CombatData.SideTwoShipCons.Clear();
    }
    public List<GameObject> UpdateFriendCombatants()
    {
        return combatController.CombatData.SideOneShipGO;
    }
    public List<GameObject> UpdateEnemyCombatants()
    {
        return combatController.CombatData.SideTwoShipGO;
    }
    public CivController FriendCivCombatants()
    {
        return combatController.CombatData._friendCivCon;
    }
    public CivController EnemyCivCombatants()
    {
        return combatController.CombatData._enemyCivCon;
    }
    public void PopulateShipData(CombatController theCombatController)
    {

        if (theCombatController == null)
        {
            Debug.Log("CombatController instance is null.");
            return;
        }
        CombatUIController.Instance.CombatOrdersUI.SetActive(true);
        List<ShipController> bothLists = new List<ShipController>();
        var sideOneShips = theCombatController.CombatData.SideOneShipCons;
        bothLists.AddRange(sideOneShips);
        var sideTwoShips = theCombatController.CombatData.SideTwoShipCons;
        bothLists.AddRange(sideTwoShips);
        foreach (var shipCon in bothLists)
        {
            GameObject shipGO = ShipManager.Instance.InstantiateTheCombatShips(shipCon); // ship instantiation done in ShipManager
            theCombatController.CombatData.SideOneShipGO.Add(shipGO);
            float length = 1f;
            float height = 1f;
            float width = 1f;
            Vector3 center = Vector3.zero;
            GameObject mesheGO = Resources.Load<GameObject>("FBX/" + shipCon.ShipData.ShipName.ToUpper().Replace("(CLONE)",""));
            if (mesheGO != null)
            {
                GameObject fbx = Instantiate(mesheGO, shipGO.transform);// meshGO is as a prefab so instantiate it
                fbx.name = shipCon.ShipData.ShipName.Replace("(CLONE)", "_Model");
                fbx.transform.SetParent(shipGO.transform, false);
                length = fbx.transform.localScale.z;
                height = fbx.transform.localScale.y;
                width = fbx.transform.localScale.x;
                center = fbx.transform.localPosition;
            }
            else
            {
                GameObject modelSphereGO = Instantiate(ShipManager.Instance.PrefabSphere, new Vector3(0, 0, 0), Quaternion.identity);
                modelSphereGO.transform.SetParent(shipGO.transform, false);
                length = modelSphereGO.transform.localScale.z;
                height = modelSphereGO.transform.localScale.y;
                width = modelSphereGO.transform.localScale.x;
                center = modelSphereGO.transform.localPosition;
            }
            

            BoxCollider boxCollider = shipGO.AddComponent<BoxCollider>();
                //if (renderer == null)
                //{
                //    boxCollider.size = renderer.bounds.size;
                //    boxCollider.center = shipGO.transform.InverseTransformPoint(renderer.bounds.center) - shipGO.transform.localPosition;
                //}
            //}
            ShipController shipController = shipGO.GetComponentInChildren<ShipController>();
            shipController = shipCon;  
            shipGO.transform.SetParent(CombatManager.Instance.CombatShipCanvas.transform, true);
            shipGO.name = shipCon.ShipData.ShipName;
            shipController.ShipData = shipCon.ShipData;
          
        } 
       CountShips();
       PositionLeftAndRightOfCombatView(sideOneShips, sideTwoShips);
    }

    private void PositionLeftAndRightOfCombatView(List<ShipController> sideOneShipCons,List<ShipController> sideTwoShipCons)
    {// same x as animator for parenting                              shipGameOb.transform.SetParent(animFriend1.transform, true);
        for (int i = 0; i < sideOneShipCons.Count; i++)
        { 
            ShipController shipCon = sideOneShipCons[i];
            GameObject shipGameOb = shipCon.gameObject;
            shipGameOb.transform.localPosition = new Vector3(combatData.xStartSide1, 0, 0);
        }
        for (int i = 0; i < sideTwoShipCons.Count; i++)
        {
            ShipController shipCon = sideTwoShipCons[i];
            GameObject shipGameOb = shipCon.gameObject;
            shipGameOb.transform.localPosition = new Vector3(combatData.xStartSide2, 0, 0);
        }
    }

    private void CountShips()
    {
        _scoutsSide1 = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Scout);
        _scoustSide2 = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Scout);

        _destroyersSide1 = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
        _destroyersSide2 = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
        _capitalsSide1 = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Cruiser ||
                                                     s.ShipData.ShipType == ShipType.LtCruiser ||
                                                     s.ShipData.ShipType == ShipType.HvyCruiser);
        _capitalsSide2 = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Cruiser ||
                                                   s.ShipData.ShipType == ShipType.LtCruiser ||
                                                   s.ShipData.ShipType == ShipType.HvyCruiser);
        _transportsFriend = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Transport);
        _transportsEnemy = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Transport);
    }
    public void ActOnThisCombatOrder(Orders order, CivEnum civOfOrder)
    {
        if (civOfOrder == CombatData.CivEnumSideOne)
        {
            // place ship game objects on left side right here
            //PositionLeftAndRightOfCombatView(CombatData.SideOneShipCons, CombatData.SideTwoShipCons);
        }

        CombatData.Order = order;
        switch (order)// move order to controller combat data
        {


            case Orders.Engage: // counters retreat & formation but weak vs Rush, Attack Transports
                {

                    _capitalShipSpiralPositionsSide2 = GenerateSpiralPositions(_capitalsSide1 + _destroyersSide1 + _scoutsSide1);
                    _transportSpiralPositionsSide1 = GenerateSpiralPositions(_transportsFriend);

                    _capitalShipSpiralPositionsSide1 = GenerateSpiralPositions(_capitalsSide2 + _destroyersSide2 + _scoustSide2);
                    _transportSpiralPositionsSide2 = GenerateSpiralPositions(_transportsEnemy);

                    // ToDo: ships warp into positions and advance at best speed for all non transports ships
                    break;
                }


            case Orders.Rush: // counters Retreat & Engage but weak vs Formation and Attach Transports
                #region Rush Region
                {

                    break;
                }
            #endregion Rush Region

            case Orders.Retreat: // counters Formation & Attach Transports but weak vs Engage and Rush
                #region Retreat Region
                {

                    break;
                }
            #endregion Retreat Region

            case Orders.Formation: // coutners Rush and Attach Transports but weak vs Engage and Retreat
                #region Formation Region
                {
                    // capital ships up front in a spiral shield with transports behind and surrounded by destroyers and scouts

                    _capitalShipSpiralPositionsSide2 = GenerateSpiralPositions(_capitalsSide1);
                    _capitalShipSpiralPositionsSide1 = GenerateSpiralPositions(_capitalsSide2);

                    _transportSpiralPositionsSide1 = GenerateSpiralPositions(_transportsFriend + _destroyersSide1 + _scoutsSide1);
                    _transportSpiralPositionsSide2 = GenerateSpiralPositions(_transportsEnemy + _destroyersSide2 + _scoustSide2);

                    break;
                }
            #endregion Formation Region

            case Orders.TargetTransports:
                #region Traget Transports Region
                {

                    break;
                }
            #endregion Traget Transports Region

            default:
                break;
        }
    }
    private List<Vector2Int> GenerateSpiralPositions(int count)
    {    // output (0,0), (1,0), (1,1), (0,1), (-1,1), (-1,0), (-1,-1), (0,-1), ...
        spiralPositions.Clear();

        Vector2Int[] directions = {
            Vector2Int.right,   // Right
            Vector2Int.up,      // Up
            Vector2Int.left,    // Left
            Vector2Int.down     // Down
        };

        Vector2Int pos = Vector2Int.zero;
        spiralPositions.Add(pos);

        int stepSize = 1;
        int dirIndex = 0;

        while (spiralPositions.Count < count)
        {
            // Go in two directions with the same step size
            for (int i = 0; i < 2; i++)
            {
                Vector2Int dir = directions[dirIndex % 4];
                for (int step = 0; step < stepSize && spiralPositions.Count < count; step++)
                {
                    pos += dir;
                    spiralPositions.Add(pos);
                }
                dirIndex++;
            }
            stepSize++;
        }
        return spiralPositions.ToList();
        // Optional: Debug log first 10
        //for (int i = 0; i < Mathf.Min(10, spiralPositions.Count); i++)
        //{
        //    Debug.Log($"[{i + 1}] = {spiralPositions[i]}");
        //}
    }
    //private GameObject GetShipModel(string shipName, TechLevel techLevel)
    //{
    //    switch (techLevel)
    //    {
    //        case TechLevel.EARLY:
    //            return Resources.Load<GameObject>($"FBX/{shipName}");
    //            break;  
    //        case TechLevel.DEVELOPED:    
    //            return Resources.Load<GameObject>($"FBX/{shipName}");
    //            break;
    //        case TechLevel.ADVANCED:    
    //            return Resources.Load<GameObject>($"FBX/{shipName}");
    //            break;
    //        case TechLevel.SUPREME:
    //            return Resources.Load<GameObject>($"FBX/{shipName}");
    //            break;  
    //        default:
    //            return new GameObject(shipName);
    //    }
    //}
}



