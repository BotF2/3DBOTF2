using Assets.Core;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;
    public static Dictionary<string, int[]> ShipDataDictionary = new Dictionary<string, int[]>();
    public static string[] FriendNameArray; // For current SpaceCombatScene ****
    public static string[] EnemyNameArray;
    private Orders orders; // for now we will use the Orders class to hold the combat orders

    public int friends;
    public int enemies;
    public static List<GameObject> FriendShips = new List<GameObject>();  // updated to current combat
    public static List<GameObject> EnemyShips = new List<GameObject>();

    private int friendShipLayer;
    private int enemyShipLayer;
    public List<GameObject> _friendCombatans; // for now be get the combatant gameObjects as they are instantiated in InstantiatCombatShips
    public List<GameObject> _enemyCombatans;

    public List<CivController> _friendCivs = new List<CivController>(); //{ Civilization.FED };
    public List<CivController> _enemyCivs = new List<CivController>();
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
    public void InstatniateCombat(List<ShipController> firendlyShipss, List<ShipController> enemyShips)
    { // call from diplomacy total war or from Diplomacy UI on fleet meets fleet 

    }
    public void ResetFriendAndEnemyDictionaries()
    {
        FriendShips.Clear();
        EnemyShips.Clear();
    }
    public List<GameObject> UpdateFriendCombatants()
    {
        return _friendCombatans;
    }
    public List<GameObject> UpdateEnemyCombatants()
    {
        return _enemyCombatans;
    }
    public List<CivController> FriendCivCombatants()
    {
        return _friendCivs;
    }
    public List<CivController> EnemyCivCombatants()
    {
        return _enemyCivs;
    }
}
