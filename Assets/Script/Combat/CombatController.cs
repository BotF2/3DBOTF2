using Assets.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        combatController = theCombatController;
        if (theCombatController == null)
        {
            Debug.Log("CombatController instance is null.");
            return;
        }
        CombatUIController.Instance.CombatOrdersUI.SetActive(true);
        List<ShipController> aLists = new List<ShipController>();
        var sideOneShips = theCombatController.CombatData.SideOneShipCons;
        aLists.AddRange(sideOneShips);
        var sideTwoShips = theCombatController.CombatData.SideTwoShipCons;
        aLists.AddRange(sideTwoShips);
        BuildShipGOAndPosition(sideOneShips, 1);
        BuildShipGOAndPosition(sideTwoShips, -1);

       CountShips();
    }
    private void BuildShipGOAndPosition(List<ShipController> shipList, int posNeg)
    {
        for (int i = 0; i < shipList.Count; i++)
        {  
            if (posNeg > 0)
                combatController.CombatData.SideOneShipGO.Add(shipList[i].gameObject);
            else
                combatController.CombatData.SideTwoShipGO.Add(shipList[i].gameObject);
            shipList[i].name = shipList[i].ShipData.ShipName;
            GameObject shipGameOb = shipList[i].gameObject;
            
            shipGameOb.AddComponent<Rigidbody>();
            shipGameOb.transform.position = new Vector3(combatController.CombatData.xStartSide1 * posNeg, i, i);
            shipGameOb.transform.rotation = Quaternion.Euler(0, 90 * posNeg, 0);
            shipGameOb.transform.SetParent(CombatManager.Instance.CombatShipCanvas.transform, true);
            Rigidbody rigid = shipGameOb.GetComponent<Rigidbody>();
            rigid.useGravity = false; 
            rigid.isKinematic = true; 
            BoxCollider boxCollider = shipGameOb.AddComponent<BoxCollider>();
            float length = 1f;
            float height = 1f;
            float width = 1f;
            Vector3 center = Vector3.zero;
            GameObject mesheGO = Resources.Load<GameObject>("FBX/" + shipList[i].ShipData.ShipName.ToUpper().Replace("(CLONE)", ""));
            if (mesheGO == null)
                mesheGO = Resources.Load<GameObject>("FBX/FED_DESTROYER_I");

            GameObject fbx = Instantiate(mesheGO, shipList[i].transform);// meshGO is as a prefab so instantiate it
            fbx.name = shipList[i].ShipData.ShipName.Replace("(CLONE)", "_Model");
            fbx.transform.SetParent(shipGameOb.transform, false);
            Renderer renderer = fbx.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Vector3 localCenter = fbx.transform.InverseTransformPoint(renderer.bounds.center);
                Vector3 localSize = fbx.transform.InverseTransformVector(renderer.bounds.size);
                boxCollider.center = localCenter*100;
                width= Math.Abs(localSize.x*100);
                height = Math.Abs(localSize.y*100);
                length = Math.Abs(localSize.z * 100);
                boxCollider.size = new Vector3(width, height, length);
            }
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
        // move ships on x out of the field of view
        if (civOfOrder == CombatData.CivEnumSideOne)
        {


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



