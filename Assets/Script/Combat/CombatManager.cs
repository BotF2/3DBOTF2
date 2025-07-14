using Assets.Core;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public GameObject CombatUICanvas;  
    public GameObject PanelCombat_Menu;
    public GameObject CombatShipCanvas;
    [SerializeField]
    private CombatController combatConPrefab;
    public CombatController CurrentCombatController
    {
        get
        {
            if (CombatControllers.Count > 0)
            {
                return CombatControllers[0]; // return the first combat controller, or implement logic to select the current one
            }
            return null;
        }
    }
    public List<CombatController> CombatControllers = new List<CombatController>();

    #region old fields to moved to CombatController
    //public static string[] FriendNameArray; // For current SpaceCombatScene ****
    //public static string[] EnemyNameArray;
    //public int friends;
    //public int enemies;


    //private int friendShipLayer;
    //private int enemyShipLayer;
    //public List<GameObject> _friendCombatans; // for now, get the combatant gameObjects as they are instantiated in InstantiatCombatShips
    //public List<GameObject> _enemyCombatans;
    //[SerializeField]
    //private CivController _friendCivCon; //{ Civilization.FED };
    //[SerializeField]
    //private CivController _enemyCivCon;
    //public GameObject cameraEmpty;
    //public GameObject animFriend1;
    //public GameObject animFriend2;
    //public GameObject animFriend3;
    //public GameObject animEnemy1;
    //public GameObject animEnemy2;
    //public GameObject animEnemy3;
    //bool _isFriend; // true if friend, false if enemy
    //int _scoutsFriend;
    //int _destroyersFriend;
    //int _capitalsFriend;
    //int _transportsFriend;
    //int _scoutsEnemy;
    //int _destroyersEnemy;
    //int _capitalsEnemy;
    //int _transportsEnemy;
    //int _totalScoutShips; // the total # of scouts in the list, ToDo: why do we need this when it is just the infall count of scouts?
    //int _totalDestroyerShips;
    //int _totalCapitalShips;
    //int _totalTransportsShips;
    //int xStartFriend = -5500; // in the wings out of the field of view
    //int xEndFriend = 0; // end of warpin on x left-right axis
    //int rotationFriendOnY = 90; // face right
    //int xStartEnemy = 5500;
    //int xEndEnemy = 300; // end of warpin on x left-right axis
    //int rotationEnemyOnY = -90; // face left
    //int ySeparator = 40; // gap in grid between ships on y axis
    //int zSeparator = 70;
    //float shipScale = 100f;
    //int yScout = 180; // shipGameOb types gap roes up
    //int yCapital = 90;
    //int yDestroyer = 0;

    //public List<GameObject> CameraTargetList; // do not send directly to CameraMultiTarget, send to GameManager first
    //private string[] arrayCountShipTypes; // change to array ship type
    //private string[] arrayNames; //??? do we need this?

    //public List<GameObject> combatShips; // for CameraMultiTarget to use for camera targets
    //public Orders order;
    //public GameObject Friend_0; // prefab empty gameobject to clone instantiat into the grids
    //public GameObject Enemy_0;


    //// ****** Use a running count of ships by type for shipGameOb starting locaitons, reset to zero on enterying first enemy
    //int _scoutShips = 0;
    //int _destroyerShips = 0;
    //int _capitalShips = 0;
    //int _utilityShips = 0;

    //int zLocation = 0;
    //int _zScoutDepth = 0;
    //int _zDestroyerDepth = 0;
    //int _zCapitalDepth = 0;
    //int _zUtilityDepth = 0;
    #endregion old code stuff moved to CombatController

    private void Awake()
    {
        // Prevent duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    internal void SetDiplomacyController(DiplomacyController diplomacyController)
    { // fix naming here "FleetMajor" "enemyShips"
        var enemyShips = new List<ShipController>();
        if (diplomacyController.DiplomacyData.FleetMajor != null)
        {
            var friendlyShips = diplomacyController.DiplomacyData.FleetMajor.FleetData.ShipsList;
            if (diplomacyController.DiplomacyData.FleetOther != null)
            {
                enemyShips = diplomacyController.DiplomacyData.FleetOther.FleetData.ShipsList;
                InitCombatData(friendlyShips, enemyShips);
            }
            else
            {
                enemyShips = diplomacyController.DiplomacyData.StarSysController.StarSysData.ShipsList;
                InitCombatData(friendlyShips, enemyShips);
            }
        }
        else // major fleet is null so only minor fleet is present and local player system
        {
            enemyShips = diplomacyController.DiplomacyData.FleetOther.FleetData.ShipsList;
            var friendlyShips = diplomacyController.DiplomacyData.StarSysController.StarSysData.ShipsList;
            InitCombatData(friendlyShips, enemyShips);
        }
    }
    public void InitCombatData(List<ShipController> friendShipCons, List<ShipController> enemyShipCons)
    {// fix naming here
        CombatData combatData = new CombatData
        {
            SideOneShipCons = friendShipCons,
            SideTwoShipCons = enemyShipCons,
            CivEnumSideOne = friendShipCons[0].ShipData.CivEnum, // all friend and enemy ships are from just one civ each for now
            CivEnumSideTwo = enemyShipCons[0].ShipData.CivEnum,
            Name = "CombatData_" + CombatControllers.Count.ToString(),

        };
        if (GameController.Instance.AreWeLocalPlayer(combatData.CivEnumSideOne) || GameController.Instance.AreWeLocalPlayer(combatData.CivEnumSideTwo))
        {
            InstantiateCombatController(combatData);
        }
    }

    public void InstantiateCombatController(CombatData combatData)
    {
        CombatController aCombatController = Instantiate(combatConPrefab, new Vector3(0, 0, 0),
            Quaternion.identity);
        aCombatController.CombatData = combatData; // set the combat data
        aCombatController.transform.SetParent(transform, false); 
        CombatUIController.Instance.CombatController = aCombatController;
        aCombatController.name = "CombatController_" + CombatControllers.Count.ToString();
        CombatControllers.Add(aCombatController);
        aCombatController.PopulateShipData(aCombatController);
        //aCombatController.PopulateShipData(aCombatController.CombatData.SideTwoShipCons, false);
        //aCombatController.SetupCombat();
    }
    public void SetUpCombatUIGameObject(GameObject parent)
    {
        CombatUICanvas.SetActive(true);
        PanelCombat_Menu.SetActive(true);
        CombatShipCanvas.SetActive(false);
        GameObject thisCombatUIGameObject = CombatUICanvas;

        if (parent != null && thisCombatUIGameObject != null)
        {
           // thisCombatUIGameObject.transform.SetParent(parent.transform, false); // false keeps local transform values
            thisCombatUIGameObject.SetActive(true);
            thisCombatUIGameObject.layer = 5;
        }

        thisCombatUIGameObject.SetActive(true);
        var combatUiController = thisCombatUIGameObject.GetComponent<CombatUIController>();
        if (combatUiController != null)
        {
            combatUiController.CivEnumLocalPlayer = GameController.Instance.GameData.LocalPlayerCivEnum;
            combatUiController.OpenCombatUI(thisCombatUIGameObject);
        }
        else
        {
            Debug.LogError("CombatUIController component is missing on the combat UI GameObject.");
        }
    }  
    #region // More old code moved to CombatController
    //private void PopulateShipData(List<ShipController> shipConList, bool friends)
    //{

    //    //for (int i = 0; i < shipConList.Count; i++)//GameManager.ShipDataDictionary.TryGetValue(_ship.name.ToUpper(), out int[] _result))
    //    //{
    //    //    //shipConList[i].ShipData.CivEnum = CivEnum.MOKRA;
    //    //    //shipConList[i].ShipData.TechLevel = TechLevel.SUPREME;
    //    //    //shipConList[i].ShipData.ShipType = ShipType.Scout;
    //    //    //shipConList[i].ShipData.maxWarpFactor = 12f;
    //    //    //shipConList[i].ShipData.currentWarpFactor = 12f;
    //    //    //shipConList[i].ShipData.ShieldMaxHealth = 12;
    //    //    //shipConList[i].ShipData.HullMaxHealth = 12;
    //    //    //shipConList[i].ShipData.TorpedoDamage = 12;
    //    //    //shipConList[i].ShipData.ShieldMaxHealth = 12;
    //    //    //shipConList[i].ShipData.BeamDamage = 12;
    //    //    //shipConList[i].ShipData.BuildDuration = 12;
    //    //    //shipConList[i].ShipData.ShipName = "freddy";
    //    //}
    //    //if (friends)
    //    //    FriendShips.AddRange(shipConList);
    //    //    _friendCombatans = shipConList.Select(s => s.gameObject).ToList();
    //    //else
    //    //    EnemyShips.AddRange(shipConList);
    //    //    _enemyCombatans = shipConList.Select(s => s.gameObject).ToList();
    //    //FriendShips = shipConList;
    //}
    //public void ResetFriendAndEnemyLists()
    //{
    //    //FriendShips.Clear();
    //    //EnemyShips.Clear();
    //}
    //public List<GameObject> UpdateFriendCombatants()
    //{
    //    return _friendCombatans;
    //}
    //public List<GameObject> UpdateEnemyCombatants()
    //{
    //    return _enemyCombatans;
    //}
    //public CivController FriendCivCombatants()
    //{
    //    return _friendCivCon;
    //}
    //public CivController EnemyCivCombatants()
    //{
    //    return _enemyCivCon;
    //}
    //public void PreCombatSetup(List<ShipType> preCombatShips, bool isFriend)
    //// The preCombatShips is one side of the list of combatents that will come from galaxy screen incoming combat data
    //{
    //    //int scouts = 0;
    //    //int destroyers = 0;
    //    //int capitals = 0;
    //    //int transports = 0;
    //    //for (int i = 0; i < preCombatShips.Count; i++)
    //    //{
    //    //    switch (preCombatShips[i])
    //    //    {
    //    //        case ShipType.Scout:
    //    //            scouts++;
    //    //            break;
    //    //        case ShipType.Destroyer:
    //    //            destroyers++;
    //    //            break;
    //    //        case ShipType.Cruiser:
    //    //        case ShipType.LtCruiser:
    //    //        case ShipType.HvyCruiser:
    //    //            capitals++;
    //    //            break;
    //    //        case ShipType.Transport:
    //    //            transports++;
    //    //            break;
    //    //        default:
    //    //            break;
    //    //    }
    //    //}
    //    //if (isFriend)
    //    //{
    //    //    _scoutsFriend = scouts;
    //    //    _destroyersFriend = destroyers;
    //    //    _capitalsFriend = capitals;
    //    //    _transportsFriend = transports;
    //    //    _totalScoutShips += scouts;
    //    //    _totalDestroyerShips += destroyers;
    //    //    _totalCapitalShips += capitals;
    //    //    _totalTransportsShips += transports;
    //    //}
    //    //else
    //    //{
    //    //    _scoutsEnemy = scouts;
    //    //    _destroyersEnemy = destroyers;
    //    //    _capitalsEnemy = capitals;
    //    //    _transportsEnemy = transports;
    //    //    _totalScoutShips += scouts;
    //    //    _totalDestroyerShips += destroyers;
    //    //    _totalCapitalShips += capitals;
    //    //    _totalTransportsShips += transports;
    //    //}
    //}

    //internal void SetCombatOrder(Orders order)
    //{
    //    order = daOrder;
    //}

    //public void PreCombatSetup(string[] preCombatShips, bool _areFriends)
    // The preCombatShips is one side of the list of combatents that will come from galaxy screen incoming combat data
    //{
    // GameManager.Instance.ResetFriendAndEnemyDictionaries();
    //_isFriend = _areFriends;
    //// we do all friend shipGameOb first, when we do the first enemy reset shipGameOb counts to zero at start           
    //_scoutShips = 0; // running count as we process the list of ships
    //_destroyerShips = 0;
    //_capitalShips = 0;
    //_utilityShips = 0;
    //_zScoutDepth = 0;
    //_zDestroyerDepth = 0;
    //_zCapitalDepth = 0;
    //_zUtilityDepth = 0;
    //_totalScoutShips = 0; // the total # of scouts in the list
    //_totalDestroyerShips = 0;
    //_totalCapitalShips = 0;
    //_totalTransportsShips = 0;
    // timesNotFriend++;
    //List<string> preCombatShipNames = preCombatShips.ToList();
    //var cameraTargets = new List<GameObject>();

    // count total ships by type that came in preCombatShips, first is Friends method call and then Enemies
    // will we use this someplace and is it duplicate of private void SetShipCounts(string shipType) running count????
    //for (int i = 0; i < preCombatShipNames.Count; i++)
    //{
    //    arrayCountShipTypes = preCombatShipNames[i].Split('_');
    //    switch (arrayCountShipTypes[1].ToUpper())
    //    {
    //        case "SCOUT":
    //            _totalScoutShips++;
    //            break;
    //        case "DESTROYER":
    //            _totalDestroyerShips++;
    //            break;
    //        case "CRUISER":
    //        case "LTCRUISER":
    //        case "HVYCRUISER":
    //            _totalCapitalShips++;
    //            break;
    //        case "TRANSPORT":
    //        case "COLONYSHIP":
    //        case "CONSTRUCTION":
    //            _totalTransportsShips++;
    //            break;
    //        case "ONEMORE":
    //            break;
    //        default:
    //            break;
    //    }
    //}

    //#region sort by order to get data for instantiating a ship steping ship by ship constructing the Ship class

    //    for (int i = 0; i < preCombatShipNames.Count; i++) // Step ship by ship through preCombatShipNames and us SetShipCounts to update what ship you are on by type
    //    {
    //        arrayNames = preCombatShipNames[i].Split('_');
    //        #region Do not need to set ship type and civ here?
    //        //ShipType _shipType = new ShipType();
    //        //Civilization _civ = new Civilization();
    //        //switch (arrayNames[0].ToUpper())
    //        //{
    //        //    case "FED":
    //        //        _civ = Civilization.FED;
    //        //        break;
    //        //    case "TERRAN":
    //        //        _civ = Civilization.TERRAN;
    //        //        break;
    //        //    case "ROM":
    //        //        _civ = Civilization.ROM;
    //        //        break;
    //        //    case "KLING":
    //        //        _civ = Civilization.KLING;
    //        //        break;
    //        //    case "CARD":
    //        //        _civ = Civilization.CARD;
    //        //        break;
    //        //    case "DOM":
    //        //        _civ = Civilization.DOM;
    //        //        break;
    //        //    case "BORG":
    //        //        _civ = Civilization.BORG;
    //        //        break;
    //        //    default:
    //        //        break;
    //        //}
    //        //switch (arrayNames[1].ToUpper())
    //        //{
    //        //    case "SCOUT":
    //        //        _shipType = ShipType.Scout;
    //        //        break;
    //        //    case "DESTROYER":
    //        //        _shipType = ShipType.Destroyer;
    //        //        break;
    //        //    case "CRUISER":
    //        //        _shipType = ShipType.Cruiser;
    //        //        break;
    //        //    case "LTCRUISER":
    //        //        _shipType = ShipType.LtCruiser;
    //        //        break;
    //        //    case "HVYCRUISER":
    //        //        _shipType = ShipType.HvyCruiser;
    //        //        break;
    //        //    case "TRANSPORT":
    //        //        _shipType = ShipType.Transport;
    //        //        break;
    //        //    case "COLONYSHIP":
    //        //        _shipType = ShipType.Colonyship;;
    //        //        break;
    //        //    case "CONSTRUCTION":
    //        //        _shipType = ShipType.Construction;
    //        //        break;
    //        //    case "ONEMORE":
    //        //        break;
    //        //    default:
    //        //        break;
    //        //}
    //        #endregion

    //        //int xLocation = -5500;
    //        //int xLocationEnd = 0; // end of warpin on x left right axis
    //        //int yLocation = 0;
    //        //int rotationOnY = 90;

    //        //if (!_isFriend)
    //        //{
    //        //    xLocation = 5500;
    //        //    xLocationEnd = 300;
    //        //    rotationOnY = -90;
    //        //}

    //        //switch (CombatUIController.order)// move order to controller combat data
    //        //{
    //        //    case Orders.Engage:
    //        //        #region Engage Region
    //        //        {
    //        //            switch (arrayNames[1].ToUpper())
    //        //            {
    //        //                case "SCOUT":
    //        //                    yLocation = yScout; // set scouts in top section, y up, z deep, x left right from camera view
    //        //                    if (_scoutShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zScoutDepth;
    //        //                        _zScoutDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "DESTROYER":
    //        //                    yLocation = yDestroyer;
    //        //                    if (_destroyerShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zDestroyerDepth;
    //        //                        _zDestroyerDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "CRUISER":
    //        //                case "LTCRUISER":
    //        //                case "HVYCRUISER":
    //        //                    yLocation = yCapital;
    //        //                    if (_capitalShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zCapitalDepth;
    //        //                        _zCapitalDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "TRANSPORT":
    //        //                case "COLONYSHIP":
    //        //                case "CONSTRUCTION":
    //        //                    if (_isFriend)
    //        //                        xLocation -= zSeparator;
    //        //                    else
    //        //                        xLocation += zSeparator;
    //        //                    yLocation = yCapital;
    //        //                    if (_utilityShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zUtilityDepth;
    //        //                        _zUtilityDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "ONEMORE":
    //        //                    break;
    //        //                default:
    //        //                    break;
    //        //            }
    //        //            //************ Instantiate the ship controllers, not GO at the location and rotation
    //        //            //GameObject shipGameOb = Instantiate(GameManager.PrefabShipDitionary[preCombatShipNames[i]], new Vector3(xLocation, yLocation, zLocation), Quaternion.identity);
    //        //            //shipGameOb.name = preCombatShipNames[i];
    //        //            //PopulateShipData(shipGameOb); // Ship class script is attached in prefab so fill in the data
    //        //            //ShipScaleAndRotation(shipGameOb, rotationOnY);
    //        //            //var aCameraTarget = shipGameOb;
    //        //            ////GameObject aCameraTarget = Instantiate(cameraEmpty, new Vector3(xLocationEnd, yLocation, zLocation), Quaternion.identity); // camera target where ships are
    //        //            ////aCameraTarget.transform.Rotate(0, rotationOnY, 0); // match ship rotation
    //        //            //ParentToAnimation(shipGameOb, _isFriend, CombatOrderSelection.order); //aCameraTarget, _isFriend, CombatOrderSelection.order);
    //        //            //combatShips.Add(shipGameOb); // list of comabat ships informing GameManager of combat ships
    //        //            //cameraTargets.Add(aCameraTarget);
    //        //            //combat.AddCombatant(shipGameOb);
    //        //            break;
    //        //        }
    //        //    #endregion Engage Region

    //        //    case Orders.Rush:
    //        //        #region Rush Region
    //        //        {
    //        //            switch (arrayNames[1].ToUpper())
    //        //            {
    //        //                case "SCOUT":
    //        //                    if (_isFriend)
    //        //                        xLocation = xLocation + 100;
    //        //                    else xLocation = xLocation - 100;
    //        //                    yLocation = yScout;
    //        //                    if (_scoutShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zScoutDepth;
    //        //                        _zScoutDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "DESTROYER":
    //        //                    if (_isFriend)
    //        //                        xLocation = xLocation + 50;
    //        //                    else xLocation = xLocation - 50;
    //        //                    yLocation = yDestroyer;
    //        //                    if (_destroyerShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zDestroyerDepth;
    //        //                        _zDestroyerDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "CRUISER":
    //        //                case "LTCRUISER":
    //        //                case "HVYCRUISER":
    //        //                    yLocation = yCapital;
    //        //                    if (_capitalShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zCapitalDepth;
    //        //                        _zCapitalDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "TRANSPORT":
    //        //                case "COLONYSHIP":
    //        //                case "CONSTRUCTION":
    //        //                    if (_isFriend)
    //        //                        xLocation -= zSeparator;
    //        //                    else
    //        //                        xLocation += zSeparator;
    //        //                    yLocation = yCapital;
    //        //                    if (_utilityShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zUtilityDepth;
    //        //                        _zUtilityDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "ONEMORE":
    //        //                    break;
    //        //                default:
    //        //                    break;
    //        //            }

    //        //            //**************Instantiate Ships ShipManager!!
    //        //            //GameObject shipGameOb = Instantiate(GameManager.PrefabShipDitionary[preCombatShipNames[i]], new Vector3(xLocation, yLocation, zLocation), Quaternion.identity);
    //        //            //shipGameOb.name = preCombatShipNames[i];
    //        //            //var aCameraTarget = shipGameOb;
    //        //            ////GameObject aCameraTarget = Instantiate(cameraEmpty, new Vector3(xLocation, yLocation, zLocation), Quaternion.identity); // camera target where ships are
    //        //            ////aCameraTarget.transform.Rotate(0, rotationOnY, 0); // match ship rotation
    //        //            //ShipScaleAndRotation(shipGameOb, rotationOnY);
    //        //            //ParentToAnimation(shipGameOb, _isFriend, CombatOrderSelection.order);//aCameraTarget, _isFriend, CombatOrderSelection.order);
    //        //            //PopulateShipData(shipGameOb);

    //        //            //combatShips.Add(shipGameOb); // ends up informing GameManager of combat ships
    //        //            //cameraTargets.Add(aCameraTarget);
    //        //            break;
    //        //        }
    //        //    #endregion Rush Region

    //        //    case Orders.Retreat:
    //        //        #region Retreat Region
    //        //        {
    //        //            if (_isFriend)
    //        //            {
    //        //                xLocation = 0;
    //        //                rotationOnY = -90;
    //        //            }
    //        //            else
    //        //            {
    //        //                xLocation = 300;
    //        //                rotationOnY = 90;
    //        //            }

    //        //            switch (arrayNames[1].ToUpper())
    //        //            {
    //        //                case "SCOUT":
    //        //                    if (_isFriend)
    //        //                        xLocation = xLocation - 100;
    //        //                    else xLocation = xLocation + 100;
    //        //                    yLocation = yScout;
    //        //                    if (_scoutShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zScoutDepth;
    //        //                        _zScoutDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "DESTROYER":
    //        //                    if (_isFriend)
    //        //                        xLocation = xLocation - 50;
    //        //                    else xLocation = xLocation + 50;
    //        //                    yLocation = yDestroyer;
    //        //                    if (_destroyerShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zDestroyerDepth;
    //        //                        _zDestroyerDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "CRUISER":
    //        //                case "LTCRUISER":
    //        //                case "HVYCRUISER":
    //        //                    yLocation = yCapital;
    //        //                    if (_capitalShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zCapitalDepth;
    //        //                        _zCapitalDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "TRANSPORT":
    //        //                case "COLONYSHIP":
    //        //                case "CONSTRUCTION":

    //        //                    if (_isFriend)
    //        //                        xLocation += zSeparator;
    //        //                    else
    //        //                        xLocation -= zSeparator;
    //        //                    yLocation = yCapital;
    //        //                    if (_utilityShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zUtilityDepth;
    //        //                        _zUtilityDepth++;
    //        //                    }
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    break;
    //        //                case "ONEMORE":
    //        //                    break;
    //        //                default:
    //        //                    break;
    //        //            }
    //        //            //**************Instantiate Ships ShipManager!!
    //        //            //GameObject shipGameOb = Instantiate(GameManager.PrefabShipDitionary[preCombatShipNames[i]], new Vector3(xLocation, yLocation, zLocation), Quaternion.identity);
    //        //            //shipGameOb.name = preCombatShipNames[i];
    //        //            //var aCameraTarget = shipGameOb;
    //        //            ////GameObject aCameraTarget = Instantiate(cameraEmpty, new Vector3(xLocation, yLocation, zLocation), Quaternion.identity); // camera target where ships are
    //        //            ////aCameraTarget.transform.Rotate(0, rotationOnY, 0); // match ship rotation
    //        //            //ShipScaleAndRotation(shipGameOb, rotationOnY);
    //        //            //// ParentToAnimation(shipGameOb, _isFriend, CombatOrderSelection.order); // aCameraTarget, _isFriend, CombatOrderSelection.order);
    //        //            //PopulateShipData(shipGameOb);
    //        //            //combatShips.Add(shipGameOb); // ends up informing GameManager of combat ships
    //        //            //cameraTargets.Add(aCameraTarget);
    //        //            break;
    //        //        }
    //        //    #endregion Retreat Region

    //        //    case Orders.Formation:
    //        //        #region Formation Region
    //        //        {
    //        //            switch (arrayNames[1].ToUpper())
    //        //            {
    //        //                case "SCOUT":
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    yLocation = yScout;
    //        //                    if (_scoutShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zScoutDepth;
    //        //                        _zScoutDepth++;
    //        //                    }
    //        //                    break;
    //        //                case "DESTROYER":
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    yLocation = yDestroyer;
    //        //                    if (_destroyerShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zDestroyerDepth;
    //        //                        _zDestroyerDepth++;
    //        //                    }
    //        //                    break;
    //        //                case "CRUISER":
    //        //                case "LTCRUISER":
    //        //                case "HVYCRUISER":
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    yLocation = yCapital;
    //        //                    if (_capitalShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zCapitalDepth;
    //        //                        _zCapitalDepth++;
    //        //                    }
    //        //                    break;
    //        //                case "TRANSPORT":
    //        //                case "COLONYSHIP":
    //        //                case "CONSTRUCTION":
    //        //                    SetShipCounts(arrayNames[1].ToUpper());
    //        //                    if (_isFriend)
    //        //                        xLocation -= zSeparator;
    //        //                    else
    //        //                        xLocation += zSeparator;
    //        //                    yLocation = yCapital;
    //        //                    if (_utilityShips % 2 == 0)
    //        //                    {
    //        //                        yLocation += ySeparator;
    //        //                        zLocation = zSeparator * _zUtilityDepth;
    //        //                        _zUtilityDepth++;
    //        //                    }
    //        //                    break;
    //        //                case "ONEMORE":
    //        //                    break;
    //        //                default:
    //        //                    break;
    //        //            }
    //        //            //**************Instantiate Ships ShipManager!!
    //        //            //GameObject shipGameOb = Instantiate(GameManager.PrefabShipDitionary[preCombatShipNames[i]], new Vector3(xLocation, yLocation, zLocation), Quaternion.identity);
    //        //            //shipGameOb.name = preCombatShipNames[i];
    //        //            //var aCameraTarget = shipGameOb;
    //        //            //GameObject aCameraTarget = Instantiate(cameraEmpty, new Vector3(xLocation, yLocation, zLocation), Quaternion.identity); // camera target where ships are
    //        //            //aCameraTarget.transform.Rotate(0, rotationOnY, 0); // match ship rotation
    //        //            //ShipScaleAndRotation(shipGameOb, rotationOnY);
    //        //            //ParentToAnimation(shipGameOb, _isFriend, CombatOrderSelection.order); //aCameraTarget, _isFriend, CombatOrderSelection.order);
    //        //            //PopulateShipData(shipGameOb);
    //        //            //combatShips.Add(shipGameOb); // ends up informing GameManager of combat ships
    //        //            //cameraTargets.Add(aCameraTarget);
    //        //            break;
    //        //        }
    //        //    #endregion Formation Region

    //        //    case Orders.ProtectTransports:
    //        //        #region Protect Transports Region
    //        //        {
    //        //            // Do Something
    //        //        }
    //        //        break;
    //        //    #endregion Protect Transports Region

    //        //    case Orders.TargetTransports:
    //        //        #region Traget Transports Region
    //        //        {
    //        //            // do Something
    //        //        }
    //        //        break;
    //        //    #endregion Traget Transports Region

    //        //    default:
    //        //        break;
    //        //}

    //    }
    //    CameraTargetList.AddRange(cameraTargets);
    //    Dictionary<int, GameObject> localShipObjectDictionary = new Dictionary<int, GameObject>();

    //    for (int j = 0; j < combatShips.Count; j++)
    //    {
    //        localShipObjectDictionary.Add(j, combatShips[j]);

    //        //if (_isFriend)
    //        //{
    //        //    //GameManager.Instance.ProvideFriendCombatShips(j, combatShips[j]);
    //        //}
    //        //else //GameManager.Instance.ProvideEnemyCombatShips(j, combatShips[j]);
    //    }
    //    combatShips.Clear();
    //    #endregion
    //} // end of pre combat setup methode call for friend or enemy


    //private void ShipScaleAndRotation(GameObject the_ship, int rotation)
    //{
    //    the_ship.transform.localScale = new Vector3(transform.localScale.x * shipScale,
    //        transform.localScale.y * shipScale, transform.localScale.z * shipScale);
    //    the_ship.transform.Rotate(0, rotation, 0);
    //}

    //private void SetShipCounts(string shipType) // how many ships of what type SO FAR used in shipGameOb starting locations
    //{
    //    switch (shipType)
    //    {
    //        case "SCOUT":
    //            _scoutShips++;
    //            break;
    //        case "DESTROYER":
    //            _destroyerShips++;
    //            break;
    //        case "CRUISER":
    //        case "LTCRUISER":
    //        case "HVYCRUISER":
    //            _capitalShips++;
    //            break;
    //        case "TRANSPORT":
    //        case "CONSTRUCTION":
    //        case "COLONYSHIP":
    //            _utilityShips++;
    //            break;
    //        case "ONEMORE":
    //            break;
    //        default:
    //            break;
    //    }
    //}
    //public List<GameObject> GetCameraTargets()
    //{
    //    return CameraTargetList;
    //}
    //public void ParentToAnimation(GameObject shipGameOb, bool _aFriend, Orders order) //GameObject cameraEmpty, bool _aFriend, Orders order)
    //{
    //    cameraEmpty.layer = shipGameOb.layer;
    //    // shipGameOb is parent to cameraEmpty and animFriend or animEnemy set as parent of shipGameOb below
    //    switch (order)
    //    {
    //        case Orders.Engage:
    //            #region Engage animation
    //            //Ship(ship.)
    //            if (_utilityShips != 0 && _capitalShips != 0) // if so then capital ships come in before utility / colonyships ships
    //            {

    //                if (shipGameOb.name.ToUpper().Contains("CRUISER") || shipGameOb.name.ToUpper().Contains("LTCRUISER")
    //                        || shipGameOb.name.ToUpper().Contains("HVYCRUISER") || shipGameOb.name.ToUpper().Contains("COLONYSHIP")
    //                        || shipGameOb.name.ToUpper().Contains("TRANSPORT") || shipGameOb.name.ToUpper().Contains("CONSTRUCTION"))
    //                {
    //                    if (_aFriend)
    //                    {
    //                        //animatorFriend1 = animFriend1.GetComponent<Animator>();
    //                        animFriend1.layer = shipGameOb.layer;
    //                        shipGameOb.transform.SetParent(animFriend1.transform, true);
    //                        // cameraEmpty.transform.SetParent(animFriend1.transform, true);
    //                    }
    //                    else
    //                    {
    //                        //animatorEnemy1 = animEnemy1.GetComponent<Animator>();
    //                        animEnemy1.layer = shipGameOb.layer;
    //                        shipGameOb.transform.SetParent(animEnemy1.transform, true);
    //                        // cameraEmpty.transform.SetParent(animEnemy1.transform, true);
    //                    }
    //                    return;
    //                }
    //            }
    //            // if not capital or utility ship do random

    //            if (_aFriend)
    //            {
    //                int choseWarp1 = Random.Range(0, 2);
    //                switch (choseWarp1)
    //                {
    //                    case 0:
    //                        animFriend2.layer = shipGameOb.layer;
    //                        shipGameOb.transform.SetParent(animFriend2.transform, true);
    //                        //cameraEmpty.transform.SetParent(animFriend2.transform, true);
    //                        break;
    //                    case 1:
    //                        animFriend3.layer = shipGameOb.layer;
    //                        shipGameOb.transform.SetParent(animFriend3.transform, true);
    //                        //cameraEmpty.transform.SetParent(animFriend3.transform, true);
    //                        break;
    //                    default:
    //                        animFriend3.layer = shipGameOb.layer;
    //                        shipGameOb.transform.SetParent(animFriend3.transform, true);
    //                        //cameraEmpty.transform.SetParent(animFriend3.transform, true);
    //                        break;
    //                }
    //            }
    //            else
    //            {
    //                int choseWarp2 = Random.Range(0, 2);
    //                switch (choseWarp2)
    //                {
    //                    case 0:
    //                        animEnemy2.layer = shipGameOb.layer;
    //                        shipGameOb.transform.SetParent(animEnemy2.transform, true);
    //                        // cameraEmpty.transform.SetParent(animEnemy2.transform, true);
    //                        break;
    //                    case 1:
    //                        animEnemy3.layer = shipGameOb.layer;
    //                        shipGameOb.transform.SetParent(animEnemy3.transform, true);
    //                        // cameraEmpty.transform.SetParent(animEnemy3.transform, true);
    //                        break;
    //                    default:
    //                        animEnemy3.layer = shipGameOb.layer;
    //                        shipGameOb.transform.SetParent(animEnemy3.transform, true);
    //                        // cameraEmpty.transform.SetParent(animEnemy3.transform, true);
    //                        break;
    //                }

    //            }
    //            break;
    //        #endregion
    //        case Orders.Rush:
    //            #region Rush animation
    //            {
    //                if (_utilityShips != 0 && _capitalShips != 0) // if we have some capital and utility ships capital and utiliy come on same animation
    //                {
    //                    if (shipGameOb.name.ToUpper().Contains("Cruiser") || shipGameOb.name.ToUpper().Contains("LtCruiser")
    //                            || shipGameOb.name.ToUpper().Contains("HvyCruiser") || shipGameOb.name.ToUpper().Contains("Colonyship")
    //                            || shipGameOb.name.ToUpper().Contains("Transport") || shipGameOb.name.ToUpper().Contains("Construction"))
    //                    {
    //                        if (_aFriend)
    //                        {
    //                            animFriend1.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animFriend1.transform, true);
    //                            cameraEmpty.transform.SetParent(animFriend1.transform, true);
    //                        }
    //                        else
    //                        {
    //                            animEnemy1.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animEnemy1.transform, true);
    //                            cameraEmpty.transform.SetParent(animEnemy1.transform, true);
    //                        }
    //                        return;
    //                    }
    //                }
    //                // if not capital or colonyship do random

    //                if (_aFriend)
    //                {
    //                    int choseWarp1 = Random.Range(0, 2);
    //                    switch (choseWarp1)
    //                    {
    //                        case 0:
    //                            animFriend2.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animFriend2.transform, true);
    //                            cameraEmpty.transform.SetParent(animFriend2.transform, true);
    //                            break;
    //                        case 1:
    //                            animFriend3.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animFriend3.transform, true);
    //                            cameraEmpty.transform.SetParent(animFriend3.transform, true);
    //                            break;
    //                        default:
    //                            animFriend3.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animFriend3.transform, true);
    //                            cameraEmpty.transform.SetParent(animFriend3.transform, true);
    //                            break;
    //                    }
    //                }
    //                else
    //                {
    //                    int choseWarp2 = Random.Range(0, 2);
    //                    switch (choseWarp2)
    //                    {
    //                        case 0:
    //                            animEnemy2.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animEnemy2.transform, true);
    //                            cameraEmpty.transform.SetParent(animEnemy2.transform, true);
    //                            break;
    //                        case 1:
    //                            animEnemy3.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animEnemy3.transform, true);
    //                            cameraEmpty.transform.SetParent(animEnemy3.transform, true);
    //                            break;
    //                        default:
    //                            animEnemy3.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animEnemy3.transform, true);
    //                            cameraEmpty.transform.SetParent(animEnemy3.transform, true);
    //                            break;
    //                    }

    //                }

    //                //if (_aFriend)
    //                //{
    //                //    switch (arrayNames[1].ToUpper())
    //                //    {
    //                //    case "SCOUT":
    //                //        animFriendRushScout.layer = shipGameOb.layer;
    //                //        shipGameOb.transform.SetParent(animFriendRushScout.transform, true);
    //                //        cameraEmpty.transform.SetParent(animFriendRushScout.transform, true);
    //                //        break;
    //                //    case "DESTROYER":
    //                //        animFriendRushDistroy.layer = shipGameOb.layer;
    //                //        shipGameOb.transform.SetParent(animFriendRushDistroy.transform, true);
    //                //        cameraEmpty.transform.SetParent(animFriendRushDistroy.transform, true);
    //                //        break;
    //                //    case "CRUISER":
    //                //    case "LT-CRUISER":
    //                //    case "HVY-CRISER":
    //                //        animFriendRushCapital.layer = shipGameOb.layer;
    //                //        shipGameOb.transform.SetParent(animFriendRushCapital.transform, true);
    //                //        cameraEmpty.transform.SetParent(animFriendRushCapital.transform, true);
    //                //        break;
    //                //    case "TRANSPORT":
    //                //    case "COLONYSHIP":
    //                //    case "CONSTRUCTION":
    //                //        animFriendRushUtility.layer = shipGameOb.layer;
    //                //        shipGameOb.transform.SetParent(animFriendRushUtility.transform, true);
    //                //        cameraEmpty.transform.SetParent(animFriendRushUtility.transform, true);
    //                //        break;
    //                //    default:
    //                //        animFriendRushCapital.layer = shipGameOb.layer;
    //                //        shipGameOb.transform.SetParent(animFriendRushCapital.transform, true);
    //                //        cameraEmpty.transform.SetParent(animFriendRushCapital.transform, true);
    //                //        break;
    //                //    }
    //                //}
    //                //else
    //                //{
    //                //    switch (arrayNames[1].ToUpper())
    //                //    {
    //                //        case "SCOUT":
    //                //            animEnemyRushScout.layer = shipGameOb.layer;
    //                //            shipGameOb.transform.SetParent(animEnemyRushScout.transform, true);
    //                //            cameraEmpty.transform.SetParent(animEnemyRushScout.transform, true);
    //                //            break;
    //                //        case "DESTROYER":
    //                //            animEnemyRushDistroy.layer = shipGameOb.layer;
    //                //            shipGameOb.transform.SetParent(animEnemyRushDistroy.transform, true);
    //                //            cameraEmpty.transform.SetParent(animEnemyRushDistroy.transform, true);
    //                //            break;
    //                //        case "CRUISER":
    //                //        case "LT-CRUISER":
    //                //        case "HVY-CRISER":
    //                //            animEnemyRushCapital.layer = shipGameOb.layer;
    //                //            shipGameOb.transform.SetParent(animEnemyRushCapital.transform, true);
    //                //            cameraEmpty.transform.SetParent(animEnemyRushCapital.transform, true);
    //                //            break;
    //                //        case "TRANSPORT":
    //                //        case "COLONYSHIP":
    //                //        case "CONSTRUCTION":
    //                //            animEnemyRushUtility.layer = shipGameOb.layer;
    //                //            shipGameOb.transform.SetParent(animEnemyRushUtility.transform, true);
    //                //            cameraEmpty.transform.SetParent(animEnemyRushUtility.transform, true);
    //                //            break;
    //                //       default:
    //                //            animEnemyRushCapital.layer = shipGameOb.layer;
    //                //            shipGameOb.transform.SetParent(animEnemyRushCapital.transform, true);
    //                //            cameraEmpty.transform.SetParent(animEnemyRushCapital.transform, true);
    //                //            break;
    //                //    }
    //                //}
    //            }
    //            break;
    //        #endregion
    //        case Orders.Retreat:
    //            {

    //            }
    //            //if (_utilityShips != 0 && _capitalShips != 0) // if so then capital ships come in before utility / colonyships ships
    //            //{

    //            //    if (shipGameOb.name.ToUpper().Contains("CRUISER") || shipGameOb.name.ToUpper().Contains("LTCRUISER")
    //            //            || shipGameOb.name.ToUpper().Contains("HVYCRUISER") || shipGameOb.name.ToUpper().Contains("COLONYSHIP")
    //            //            || shipGameOb.name.ToUpper().Contains("TRANSPORT") || shipGameOb.name.ToUpper().Contains("CONSTRUCTION"))
    //            //    {
    //            //        if (_aFriend)
    //            //        {
    //            //            //animatorFriend1 = animFriend1.GetComponent<Animator>();
    //            //            animFriend1.layer = shipGameOb.layer;
    //            //            shipGameOb.transform.SetParent(animFriend1.transform, true);
    //            //            // cameraEmpty.transform.SetParent(animFriend1.transform, true);
    //            //        }
    //            //        else
    //            //        {
    //            //            //animatorEnemy1 = animEnemy1.GetComponent<Animator>();
    //            //            animEnemy1.layer = shipGameOb.layer;
    //            //            shipGameOb.transform.SetParent(animEnemy1.transform, true);
    //            //            // cameraEmpty.transform.SetParent(animEnemy1.transform, true);
    //            //        }
    //            //        return;
    //            //    }
    //            //}
    //            //// if not capital or utility ship do random

    //            //if (_aFriend)
    //            //{
    //            //    int choseWarp1 = Random.Range(0, 2);
    //            //    switch (choseWarp1)
    //            //    {
    //            //        case 0:
    //            //            animFriend2.layer = shipGameOb.layer;
    //            //            shipGameOb.transform.SetParent(animFriend2.transform, true);
    //            //            //cameraEmpty.transform.SetParent(animFriend2.transform, true);
    //            //            break;
    //            //        case 1:
    //            //            animFriend3.layer = shipGameOb.layer;
    //            //            shipGameOb.transform.SetParent(animFriend3.transform, true);
    //            //            //cameraEmpty.transform.SetParent(animFriend3.transform, true);
    //            //            break;
    //            //        default:
    //            //            animFriend3.layer = shipGameOb.layer;
    //            //            shipGameOb.transform.SetParent(animFriend3.transform, true);
    //            //            //cameraEmpty.transform.SetParent(animFriend3.transform, true);
    //            //            break;
    //            //    }
    //            //}
    //            //else
    //            //{
    //            //    int choseWarp2 = Random.Range(0, 2);
    //            //    switch (choseWarp2)
    //            //    {
    //            //        case 0:
    //            //            animEnemy2.layer = shipGameOb.layer;
    //            //            shipGameOb.transform.SetParent(animEnemy2.transform, true);
    //            //            // cameraEmpty.transform.SetParent(animEnemy2.transform, true);
    //            //            break;
    //            //        case 1:
    //            //            animEnemy3.layer = shipGameOb.layer;
    //            //            shipGameOb.transform.SetParent(animEnemy3.transform, true);
    //            //            // cameraEmpty.transform.SetParent(animEnemy3.transform, true);
    //            //            break;
    //            //        default:
    //            //            animEnemy3.layer = shipGameOb.layer;
    //            //            shipGameOb.transform.SetParent(animEnemy3.transform, true);
    //            //            // cameraEmpty.transform.SetParent(animEnemy3.transform, true);
    //            //            break;
    //            //    }

    //            //}

    //            break;
    //        case Orders.Formation:
    //            {
    //                #region Formation animation
    //                if (_aFriend)
    //                {
    //                    int choseWarp1 = Random.Range(0, 3);
    //                    switch (choseWarp1)
    //                    {
    //                        case 0:
    //                            animFriend1.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animFriend1.transform, true);
    //                            cameraEmpty.transform.SetParent(animFriend1.transform, true);
    //                            break;
    //                        case 1:
    //                            animFriend2.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animFriend2.transform, true);
    //                            cameraEmpty.transform.SetParent(animFriend2.transform, true);
    //                            break;
    //                        case 2:
    //                            animFriend3.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animFriend3.transform, true);
    //                            cameraEmpty.transform.SetParent(animFriend3.transform, true);
    //                            break;
    //                        default:
    //                            animFriend1.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animFriend1.transform, true);
    //                            cameraEmpty.transform.SetParent(animFriend1.transform, true);
    //                            break;
    //                    }
    //                }
    //                else
    //                {
    //                    int choseWarp2 = Random.Range(0, 3);
    //                    switch (choseWarp2)
    //                    {
    //                        case 0:
    //                            animEnemy1.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animEnemy1.transform, true);
    //                            cameraEmpty.transform.SetParent(animEnemy1.transform, true);
    //                            break;
    //                        case 1:
    //                            animEnemy2.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animEnemy2.transform, true);
    //                            cameraEmpty.transform.SetParent(animEnemy2.transform, true);
    //                            break;
    //                        case 2:
    //                            animEnemy3.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animEnemy3.transform, true);
    //                            cameraEmpty.transform.SetParent(animEnemy3.transform, true);
    //                            break;
    //                        default:
    //                            animEnemy1.layer = shipGameOb.layer;
    //                            shipGameOb.transform.SetParent(animEnemy1.transform, true);
    //                            cameraEmpty.transform.SetParent(animEnemy1.transform, true);
    //                            break;
    //                    }
    //                }
    //                #endregion
    //            }
    //            break;

    //        case Orders.ProtectTransports:
    //            break;
    //        case Orders.TargetTransports:
    //            break;
    //        default:
    //            break;
    //    }
    //}
    #endregion // more old code moved to CombatManager
}

