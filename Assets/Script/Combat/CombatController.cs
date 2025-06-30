using Assets.Core;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class CombatController : MonoBehaviour
{
    private CombatData combatData;
    public CombatData CombatData { get { return combatData; } set { combatData = value; } }
    private CombatController combatController;
    public int maxPositions = 200; // the max number of positions to generate in the spiral
    public List<Vector2Int> spiralPositions = new List<Vector2Int>();
    int _scoutsFriend;
    int _scoutsEnemy;
    int _destroyersFriend;
    int _destroyersEnemy;
    int _capitalsFriend;
    int _capitalsEnemy;
    private List<Vector2Int> _capitalShipSpiralPositionsEnemy = new List<Vector2Int>();
    private List<Vector2Int> _capitalShipSpiralPositionsFriend = new List<Vector2Int>();
    int _transportsFriend;
    int _transportsEnemy;
    private List<Vector2Int> _transportSpiralPositionsFriend = new List<Vector2Int>();
    private List<Vector2Int> _transportSpiralPositionsEnemy = new List<Vector2Int>();

    int _totalScoutShips; // the total # of scouts in the list, ToDo: why do we need this when it is just the infall count of scouts?
    int _totalDestroyerShips;
    int _totalCapitalShips;
    int _totalTransportsShips;

    public void SetCombatOrder(Orders theOrder)
    {
        for (int i = 0; i < CombatManager.Instance.CombatControllers.Count; i++)
        {
            combatController = CombatManager.Instance.CombatControllers[i];
            if (CombatUIController.Instance.CombatController == combatController)
            {
                combatController.CombatData.Order = theOrder;
            }
        }
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
        // Example logic: Reset friend and enemy lists and populate ship data  
        //theCombatController.ResetFriendAndEnemyLists();
        List<ShipController> bothLists = new List<ShipController>();
        bothLists.AddRange(theCombatController.CombatData.SideOneShipCons);
        bothLists.AddRange(theCombatController.CombatData.SideTwoShipCons);
        foreach (var shipCon in bothLists)
        {
            GameObject shipGO = ShipManager.Instance.InstantiateTheCombatShips(shipCon);
            
            theCombatController.CombatData.SideOneShipGO.Add(shipGO);
            GameObject shipModel = Instantiate(shipCon.ShipData.ShipPrefab, shipGO.transform, false);
            shipModel.transform.SetParent(shipGO.transform);
            Renderer renderer = shipModel.GetComponentInChildren<Renderer>();
            BoxCollider boxCollider = shipGO.AddComponent<BoxCollider>();
            if (renderer == null)
            {
                boxCollider.size = renderer.bounds.size;
                boxCollider.center = shipGO.transform.InverseTransformPoint(renderer.bounds.center) - shipGO.transform.localPosition;
            }
            ShipController shipController = shipGO.AddComponent<ShipController>();
            shipController.ShipData = shipCon.ShipData; // Initialize the ShipController with the ShipData  
            shipGO.transform.SetParent(CombatManager.Instance.CombatParent.transform);
           
            Rigidbody rb = shipGO.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Set to true if you want to control movement manuall
            rb.useGravity = false; // Set to true if you want gravity to affect the ship
            rb.mass = 1; // Set the mass of the ship, adjust as needed
   
        } 
        CountShips();
    }

    private void CountShips()
    {
        _scoutsFriend = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Scout);
        _scoutsEnemy = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Scout);

        _destroyersFriend = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
        _destroyersEnemy = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
        _capitalsFriend = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Cruiser ||
                                                     s.ShipData.ShipType == ShipType.LtCruiser ||
                                                     s.ShipData.ShipType == ShipType.HvyCruiser);
        _capitalsEnemy = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Cruiser ||
                                                   s.ShipData.ShipType == ShipType.LtCruiser ||
                                                   s.ShipData.ShipType == ShipType.HvyCruiser);
        _transportsFriend = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Transport);
        _transportsEnemy = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Transport);
    }
    public void IssueCombatOrder(Orders order, bool areFriend)
    {
        CombatData.Order = order;
        //var whatever = GameController.Instance.Loc
        switch (order)// move order to controller combat data
        {
            case Orders.Engage: // counters retreat & formation but weak vs Rush, Attack Transports
                #region Engage Region
                {
                    if (areFriend)
                    {
                        _capitalShipSpiralPositionsFriend = GenerateSpiralPositions(_capitalsFriend + _destroyersFriend + _scoutsFriend);
                        _transportSpiralPositionsFriend = GenerateSpiralPositions(_transportsFriend);
                    }
                    else
                    {
                        _capitalShipSpiralPositionsEnemy = GenerateSpiralPositions(_capitalsEnemy + _destroyersEnemy + _scoutsEnemy);
                        _transportSpiralPositionsEnemy = GenerateSpiralPositions(_transportsEnemy);
                    }
                    // ToDo: ships warp into positions and advance at best speed for all non transports ships
                    break;
                }
            #endregion Engage Region

            case Orders.Rush: // counters Retreat & Engage but weak vs Formation and Attach Transports
                #region Rush Region
                {
                    if (areFriend) { }
                    else { }
                    break;
                }
            #endregion Rush Region

            case Orders.Retreat: // counters Formation & Attach Transports but weak vs Engage and Rush
                #region Retreat Region
                {
                    if (areFriend) { }
                    else { }
                    break;
                }
            #endregion Retreat Region

            case Orders.Formation: // coutners Rush and Attach Transports but weak vs Engage and Retreat
                #region Formation Region
                {
                    // capital ships up front in a spiral shield with transports behind and surrounded by destroyers and scouts
                    if (areFriend)
                    {
                        _capitalShipSpiralPositionsFriend = GenerateSpiralPositions(_capitalsFriend);
                        _capitalShipSpiralPositionsEnemy = GenerateSpiralPositions(_capitalsEnemy);
                    }
                    else
                    {
                        _transportSpiralPositionsFriend = GenerateSpiralPositions(_transportsFriend + _destroyersFriend + _scoutsFriend);
                        _transportSpiralPositionsEnemy = GenerateSpiralPositions(_transportsEnemy + _destroyersEnemy + _scoutsEnemy);
                    }
                    // ToDo: Set ships at their positions, (get to the positions from warp in animation) and do not move
                    break;
                }
            #endregion Formation Region

            case Orders.TargetTransports:
                #region Traget Transports Region
                {
                    if (areFriend) { }
                    else { }
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



