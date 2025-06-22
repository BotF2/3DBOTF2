using System.Collections.Generic;
using UnityEngine;
using Assets.Core;
using UnityEngine.UI;
using System.Linq;

public class CombatController : MonoBehaviour
{
    private CombatData combatData;
    public CombatData CombatData { get { return combatData; } set { combatData = value; } }
    private CombatController combatController;
    public static List<ShipController> FriendShips = new List<ShipController>();  // updated for current combat from Diplomacy / Scene controller
    public static List<ShipController> EnemyShips = new List<ShipController>();
    public List<GameObject> _friendCombatans; // for now, get the combatant gameObjects as they are instantiated in InstantiatCombatShips
    public List<GameObject> _enemyCombatans;
    int _scoutsFriend;
    int _destroyersFriend;
    int _capitalsFriend;
    int _transportsFriend;
    int _scoutsEnemy;
    int _destroyersEnemy;
    int _capitalsEnemy;
    int _transportsEnemy;
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
        combatController.CombatData.FriendShips.Clear();
        combatController.CombatData.EnemyShips.Clear();
    }
    public List<GameObject> UpdateFriendCombatants()
    {
        return combatController.CombatData._friendCombatans;
    }
    public List<GameObject> UpdateEnemyCombatants()
    {
        return combatController.CombatData._enemyCombatans;
    }
    public CivController FriendCivCombatants()
    {
        return combatController.CombatData._friendCivCon;
    }
    public CivController EnemyCivCombatants()
    {
        return combatController.CombatData._enemyCivCon;
    }
    public void PopulateShipData(List<ShipController> shipConList, bool friends)
    {
        if (friends)
        {
            FriendShips.AddRange(shipConList);
            _friendCombatans = shipConList.Select(s => s.gameObject).ToList();
        }
        else
        {
            EnemyShips.AddRange(shipConList);
            _enemyCombatans = shipConList.Select(s => s.gameObject).ToList();
        }
        CountShips();
    }
    private void CountShips()
    {   
        _totalScoutShips = FriendShips.Count(s => s.ShipData.ShipType == ShipType.Scout) +
                           EnemyShips.Count(s => s.ShipData.ShipType == ShipType.Scout);
        _totalDestroyerShips = FriendShips.Count(s => s.ShipData.ShipType == ShipType.Destroyer) +
                               EnemyShips.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
        _totalCapitalShips = FriendShips.Count(s => s.ShipData.ShipType == ShipType.Cruiser || 
                                                     s.ShipData.ShipType == ShipType.LtCruiser || 
                                                     s.ShipData.ShipType == ShipType.HvyCruiser) +
                             EnemyShips.Count(s => s.ShipData.ShipType == ShipType.Cruiser || 
                                                   s.ShipData.ShipType == ShipType.LtCruiser || 
                                                   s.ShipData.ShipType == ShipType.HvyCruiser);
        _totalTransportsShips = FriendShips.Count(s => s.ShipData.ShipType == ShipType.Transport) +
                                EnemyShips.Count(s => s.ShipData.ShipType == ShipType.Transport);
    }
    public void PreCombatSetup(List<ShipType> preCombatShips, bool isFriend)
    // The preCombatShips is one side of the list of combatents that will come from galaxy screen incoming combat data
    {
        int scouts = 0;
        int destroyers = 0;
        int capitals = 0;
        int transports = 0;
        for (int i = 0; i < preCombatShips.Count; i++)
        {
            switch (preCombatShips[i])
            {
                case ShipType.Scout:
                    scouts++;
                    break;
                case ShipType.Destroyer:
                    destroyers++;
                    break;
                case ShipType.Cruiser:
                case ShipType.LtCruiser:
                case ShipType.HvyCruiser:
                    capitals++;
                    break;
                case ShipType.Transport:
                    transports++;
                    break;
                default:
                    break;
            }
        }
        if (isFriend)
        {
            _scoutsFriend = scouts;
            _destroyersFriend = destroyers;
            _capitalsFriend = capitals;
            _transportsFriend = transports;
            _totalScoutShips += scouts;
            _totalDestroyerShips += destroyers;
            _totalCapitalShips += capitals;
            _totalTransportsShips += transports;
        }
        else
        {
            _scoutsEnemy = scouts;
            _destroyersEnemy = destroyers;
            _capitalsEnemy = capitals;
            _transportsEnemy = transports;
            _totalScoutShips += scouts;
            _totalDestroyerShips += destroyers;
            _totalCapitalShips += capitals;
            _totalTransportsShips += transports;
        }
    }
}
