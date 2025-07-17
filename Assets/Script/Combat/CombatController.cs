using Assets.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatController : MonoBehaviour, IPlayerController
{
    /// <summary>
    /// [CombatController]
    /// |
    /// v
    /// [IPlayerController] <--- [LocalHumanPlayerController] (UI)
    ///                     <--- [RemoteHumanPlayerController] (Network)
    ///                     <--- [AIPlayerController] (AI)
    /// </summary>

    private CombatData combatData;
    public CombatData CombatData { get { return combatData; } set { combatData = value; } }
    private CombatController combatController;
    public GameObject prefabSphere;
    public GameObject cameraEmptyGo;
    public int maxPositions = 200; // the max number of positions to generate in the spiral
    public List<Vector2Int> spiralPositions = new List<Vector2Int>();
    int _scoutsSide1;
    int _scoutsSide2;
    int _destroyersSide1;
    int _destroyersSide2;
    int _capitalsSide1;
    int _capitalsSide2;
    int _transportsSide1;
    int _transportsSide2;
    int _totalScoutShips; // the total # of scouts in the list, ToDo: why do we need this when it is just the infall count of scouts?
    int _totalDestroyerShips;
    int _totalCapitalShips;
    int _totalTransportsShips;
    public List<IPlayerController> PlayerControllers;


    public CivEnum PlayerCiv { get; }
    public bool IsLocal { get; }

    public PlayerData PlayerData { get; private set; }

    public void GiveOrder(Orders orders)
    {
        // implementation
    }
    public void SetCombatOrder(Orders order, CivEnum civ)
    {
        var player = PlayerControllers.FirstOrDefault(p => p.PlayerCiv == civ);
        player?.GiveOrder(order);
    }
    public void SetCombatOrder(Orders theOrder)
    {
        this.CombatData.Order = theOrder;
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
    public CivController SideOneCivCombatants()
    {
        return combatController.CombatData.sideOneCiv;
    }
    public CivController EnemyCivCombatants()
    {
        return combatController.CombatData.sideTwoCiv;
    }
    public void PopulateShipData(CombatController theCombatController)
    {
        combatController = theCombatController;
        if (theCombatController == null)
        {
            Debug.Log("CombatController Instance is null.");
            return;
        }
        CombatUIController.Instance.CombatOrdersUI.SetActive(true);

        var sideOneShips = theCombatController.CombatData.SideOneShipCons;

        var sideTwoShips = theCombatController.CombatData.SideTwoShipCons;

        BuildShipGOAndPosition(sideOneShips, -1); // left side ships are -x axis...
        BuildShipGOAndPosition(sideTwoShips, 1);
    }
    private void BuildShipGOAndPosition(List<ShipController> shipConList, int side1negSide2pog)
    {
        for (int i = 0; i < shipConList.Count; i++)
        {
            shipConList[i].transform.localScale = Vector3.one;
            shipConList[i].name = shipConList[i].ShipData.ShipName;
            GameObject shipGameOb = shipConList[i].gameObject;
            shipGameOb.AddComponent<Rigidbody>();
            shipGameOb.transform.SetPositionAndRotation(new Vector3(combatController.CombatData.xStart * side1negSide2pog, i * 10, i * 10), Quaternion.Euler(0, 90 * side1negSide2pog, 0));
            shipGameOb.transform.SetParent(CombatManager.Instance.CombatShipCanvas.transform, true);
            Rigidbody rigid = shipGameOb.GetComponent<Rigidbody>();
            rigid.transform.localScale = Vector3.one;
            rigid.useGravity = false;
            rigid.isKinematic = true;
            BoxCollider boxCollider = shipGameOb.AddComponent<BoxCollider>();
            boxCollider.transform.localScale = Vector3.one;
            float length = 1f;
            float height = 1f;
            float width = 1f;
            GameObject mesheGO = Resources.Load<GameObject>("FBX/" + shipConList[i].ShipData.ShipName.ToUpper().Replace("(CLONE)", ""));
            if (mesheGO == null)
            { // This is the fallback for missing ship models for now  
                mesheGO = Resources.Load<GameObject>("FBX/FED_DESTROYER_I");
            }
            GameObject fbx = Instantiate(mesheGO, shipConList[i].transform);// fbx is as a prefab so instantiate it  
            fbx.name = shipConList[i].ShipData.ShipName.Replace("(CLONE)", "_Model");
            fbx.transform.SetParent(shipGameOb.transform, false);
            fbx.transform.localScale = Vector3.one;
            Renderer renderer = fbx.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Vector3 localCenter = fbx.transform.InverseTransformPoint(renderer.bounds.center);
                Vector3 localSize = fbx.transform.InverseTransformVector(renderer.bounds.size);
                boxCollider.center = new Vector3(localCenter.x, localCenter.z, localCenter.y);
                width = Math.Abs(localSize.x);
                height = Math.Abs(localSize.z);
                length = Math.Abs(localSize.y);
                boxCollider.size = new Vector3(width, height, length);
            }
        }
    }

    private void CountShips()
    {
        _scoutsSide1 = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Scout);
        _scoutsSide2 = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Scout);

        _destroyersSide1 = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
        _destroyersSide2 = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
        _capitalsSide1 = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Cruiser ||
                                                     s.ShipData.ShipType == ShipType.LtCruiser ||
                                                     s.ShipData.ShipType == ShipType.HvyCruiser);
        _capitalsSide2 = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Cruiser ||
                                                   s.ShipData.ShipType == ShipType.LtCruiser ||
                                                   s.ShipData.ShipType == ShipType.HvyCruiser);
        _transportsSide1 = CombatData.SideOneShipCons.Count(s => s.ShipData.ShipType == ShipType.Transport);
        _transportsSide2 = CombatData.SideTwoShipCons.Count(s => s.ShipData.ShipType == ShipType.Transport);
    }
    public void SetThisCombatOrder(Orders order, CivEnum civOfOrder)
    {
        List<ShipController> shipCons = null; // Initialize the variable to avoid CS0165  
        int sideSignFactor = -1; // Default to -1 for Side One, will be set to 1 for Side Two
        // Determine which list of ships to use based on the civOfOrder  
        if (civOfOrder == CombatData.CivEnumSideOne)
        {
            shipCons = CombatData.SideOneShipCons;
            sideSignFactor = -1; // Side One is always on the left side
        }
        else if (civOfOrder == CombatData.CivEnumSideTwo)
        {
            shipCons = CombatData.SideTwoShipCons;
            sideSignFactor = 1; // Side Two is always on the right side
        }

        // Ensure shipCons is not null before proceeding  
        if (shipCons == null)
        {
            Debug.LogError("Ship list is null. Unable to act on combat order.");
            return;
        }

        CombatData.Order = order; // order to controller combat data

    }
    public void ActOnCombatOrders(List<ShipController> shipCons, int sideSignFactor)
    {

        switch (CombatData.Order)
        {
            case Orders.Engage:
                for (int i = 0; i < shipCons.Count; i++)
                {
                    if (shipCons[i].ShipData.ShipType == ShipType.Transport)
                    {
                        shipCons[i].transform.position = new Vector3((CombatData.xStart * sideSignFactor) + (CombatData.zSeparator * sideSignFactor),
                            spiralPositions[i].x, spiralPositions[i].y);
                    }
                    else
                    {
                        shipCons[i].transform.position = new Vector3(CombatData.xStart * sideSignFactor, spiralPositions[i].x, spiralPositions[i].y);
                    }
                }
                break;

            case Orders.Rush:
                break;
            case Orders.Retreat:
                break;
            case Orders.Formation:
                break;
            case Orders.TargetTransports:
                break;
            default:
                break;
        }
    }

    private List<Vector2Int> GenerateSpiralPositions(int count)
    {    // output (0,0), (10,0), (10,10), (0,10), (-10,10), (-10,0), (-10,-10), (0,-10), ...
        spiralPositions.Clear();

        Vector2Int[] directions = {
            Vector2Int.right,   // Right
            Vector2Int.up,      // Up
            Vector2Int.left,    // Left
            Vector2Int.down     // Down
        };

        Vector2Int pos = Vector2Int.zero;
        spiralPositions.Add(pos);

        int stepSize = 10;
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
    }
}

// The CS1022 error typically occurs when there is an extra closing brace ('}') in the code.  
// After reviewing the provided code, the issue seems to be caused by an extra closing brace at the end of the file.  
// To fix this, remove the unnecessary closing brace at the end of the file.  

// Original code snippet at the end of the file:  
// }  

// Corrected code:  
// Simply remove the extra closing brace at the end of the file.


