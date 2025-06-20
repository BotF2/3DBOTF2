using Assets.Core;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CombatData
{
    public Orders Order;
    public CivEnum CivEnumSideOne;
    public CivEnum CivEnumSideTwo;
    public CivController FriendCiv;
    public CivController EnemyCiv;
    public List<ShipController> SideOneShipControllers;
    public List<ShipController> SideTwoShipControllers;
    public string Name;
    public static string[] FriendNameArray; // For current SpaceCombatScene ****
    public static string[] EnemyNameArray;
    public int friends;
    public int enemies;
    public List<ShipController> FriendShips = new List<ShipController>();  // updated to current combat
    public List<ShipController> EnemyShips = new List<ShipController>();
    private int friendShipLayer;
    private int enemyShipLayer;
    public List<GameObject> _friendCombatans; // for now, get the combatant gameObjects as they are instantiated in InstantiatCombatShips
    public List<GameObject> _enemyCombatans;
    public CivController _friendCivCon; //{ Civilization.FED };
    public CivController _enemyCivCon;
    public GameObject cameraEmpty;
    [SerializeField]
    private GameObject animFriend1;
    [SerializeField]
    private GameObject animFriend2;
    [SerializeField]
    private GameObject animFriend3;
    [SerializeField]
    private GameObject animEnemy1;
    [SerializeField]
    private GameObject animEnemy2;
    [SerializeField]
    private GameObject animEnemy3;
    bool _isFriend; // true if friend, false if enemy
    int _scoutsFriend;
    int _destroyersFriend;
    int _capitalsFriend;
    int _transportsFriend;
    int _scoutsEnemy;
    int _destroyersEnemy;
    int _capitalsEnemy;
    int _transportsEnemy;
    int _totalScoutShips; // the total # of scouts in the list
    int _totalDestroyerShips;
    int _totalCapitalShips;
    int _totalTransportsShips;
    int xStartFriend = -5500; // in the wings out of the field of view
    int xEndFriend = 0; // end of warpin on x left-right axis
    int rotationFriendOnY = 90; // face right
    int xStartEnemy = 5500;
    int xEndEnemy = 300; // end of warpin on x left-right axis
    int rotationEnemyOnY = -90; // face left
    int ySeparator = 40; // gap in grid between ships on y axis
    int zSeparator = 70;
    float shipScale = 100f;
    int yScout = 180; // shipGameOb types gap roes up
    int yCapital = 90;
    int yDestroyer = 0;

    public List<GameObject> CameraTargetList; // do not send directly to CameraMultiTarget, send to GameManager first
    private string[] arrayCountShipTypes; // change to array ship type
    private string[] arrayNames; //??? do we need this?

    public List<GameObject> combatShips; // for CameraMultiTarget to use for camera targets
    public Orders order;
    //public GameObject Friend_0; // prefab empty gameobject to clone instantiat into the grids
    //public GameObject Enemy_0;


    // ****** Use a running count of ships by type for shipGameOb starting locaitons, reset to zero on enterying first enemy
    int _scoutShips = 0;
    int _destroyerShips = 0;
    int _capitalShips = 0;
    int _utilityShips = 0;

    int zLocation = 0;
    int _zScoutDepth = 0;
    int _zDestroyerDepth = 0;
    int _zCapitalDepth = 0;
    int _zUtilityDepth = 0;

    public CombatData()
    {
        Order = Orders.None;
        CivEnumSideOne = CivEnum.None;
        CivEnumSideTwo = CivEnum.None;
        FriendCiv = null;
        EnemyCiv = null;
        SideOneShipControllers = new List<ShipController>();
        SideTwoShipControllers = new List<ShipController>();
        Name = "CombatData";
    }
}
