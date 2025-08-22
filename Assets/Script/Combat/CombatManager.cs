using Assets.Core;
using Mirror.BouncyCastle.Asn1.Crmf;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public GameObject CombatUICanvas;  
    [SerializeField]
    private CombatController combatConPrefab;   
    public List<CombatController> CombatControllers = new List<CombatController>();
    public List<IPlayerController> participants;
    public List<Animator> animators; // Assign in Inspector or dynamically
    [SerializeField] GameObject sideOneAnima1;
    [SerializeField] GameObject sideOneAnima2;
    [SerializeField] GameObject sideOneAnima3;
    [SerializeField] GameObject sideTwoAnima1;
    [SerializeField] GameObject sideTwoAnima2;
    [SerializeField] GameObject sideTwoAnima3;
    public List<GameObject> TorpedoPrefabs;
    public List<GameObject> BeamPrefabs;

    
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
    {
        var sideOneShips = new List<ShipController>();
        var sideTwoShips = new List<ShipController>();
        if (diplomacyController.DiplomacyData.CurrentFleetOfSideOne.FleetData != null)
        {
            sideOneShips = diplomacyController.DiplomacyData.CurrentFleetOfSideOne.FleetData.ShipsList;
            if (diplomacyController.DiplomacyData.CurrentFleetOfSideTwo.FleetData != null)
            {
                sideTwoShips = diplomacyController.DiplomacyData.CurrentFleetOfSideTwo.FleetData.ShipsList;
                InitCombatData(sideOneShips, sideTwoShips); // instantiate ship game objects
            }
            else
            {
                sideTwoShips = diplomacyController.DiplomacyData.CurrentStarSysController.StarSysData.ShipsList;
                InitCombatData(sideOneShips, sideTwoShips);
            }
        }
        else if (diplomacyController.DiplomacyData.CurrentFleetOfSideTwo.FleetData != null)
        {
            sideTwoShips = diplomacyController.DiplomacyData.CurrentFleetOfSideTwo.FleetData.ShipsList;
            sideOneShips = diplomacyController.DiplomacyData.CurrentStarSysController.StarSysData.ShipsList;
        }
        
    }
    public void InitCombatData(List<ShipController> sideOneShipCons, List<ShipController> sideTwoShipCons)
    {
        CombatData combatData = new CombatData
        {
            SideOneShipCons = sideOneShipCons,
            SideTwoShipCons = sideTwoShipCons,
            CivEnumSideOne = sideOneShipCons[0].ShipData.CivEnum, 
            CivEnumSideTwo = sideTwoShipCons[0].ShipData.CivEnum,
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
        aCombatController.CombatData.OrderSideOne = CombatOrders.Engage; // default order
        aCombatController.CombatData.OrderSideTwo = CombatOrders.Engage; // default order
        aCombatController.transform.SetParent(transform, false); 
        CombatUIController.Instance.CombatController = aCombatController;
        CombatUIController.Instance.sideOneEnum = combatData.CivEnumSideOne;
        CombatUIController.Instance.sideTwoEnum = combatData.CivEnumSideTwo;
        CombatUIController.Instance.SideOneShipControllers = combatData.SideOneShipCons;
        CombatUIController.Instance.SideTwoShipControllers = combatData.SideTwoShipCons;
        aCombatController.name = "CombatController_" + CombatControllers.Count.ToString();
        aCombatController.animators = animators;
        aCombatController.sideOneA1Animator = aCombatController.animators[0];
        aCombatController.sideOneA2Animator = aCombatController.animators[1];
        aCombatController.sideOneA3Animator = aCombatController.animators[2];
        aCombatController.sideTwoA1Animator = aCombatController.animators[3];
        aCombatController.sideTwoA2Animator = aCombatController.animators[4];
        aCombatController.sideTwoA3Animator = aCombatController.animators[5];
        aCombatController.SideOneTorpedoPrefab = GetTorpedoPrefabs(aCombatController, combatData.CivEnumSideOne);
        aCombatController.SideTwoTorpedoPrefab = GetTorpedoPrefabs(aCombatController, combatData.CivEnumSideTwo);
        aCombatController.SideOneBeamPrefab = GetBeamPrefabs(aCombatController, combatData.CivEnumSideOne);
        aCombatController.SideTwoBeamPrefab = GetBeamPrefabs(aCombatController, combatData.CivEnumSideTwo);
        CombatControllers.Add(aCombatController);
        aCombatController.PopulateShipData(aCombatController);
        aCombatController.TrySetPlayerOrders(combatData);
        SetUpLocalPlayer();
        TimeManager.Instance.PauseTime(); // Pause the game when combat UI is opened
    }

    private GameObject GetTorpedoPrefabs(CombatController aCombatController, CivEnum civEnum)
    {
        GameObject torbedoPrefab = TorpedoPrefabs[TorpedoPrefabs.Count -1]; // default to minor civ prefab

        for (int i = 0; i < TorpedoPrefabs.Count; i++)
        {
            if (i == (int)civEnum)
            {
                torbedoPrefab = TorpedoPrefabs[i];
                return torbedoPrefab; // Return the prefab for the specific civ
            }
        }
        return torbedoPrefab; // Return the default prefab if no match found
    }

    
    private GameObject GetBeamPrefabs(CombatController aCombatController, CivEnum civEnum)
    {
        GameObject beamPrefab = BeamPrefabs[BeamPrefabs.Count -1];
        for (int i = 0; i < BeamPrefabs.Count; i++)
        {
            if (i == (int)civEnum)
            {
                beamPrefab = BeamPrefabs[i];
                return beamPrefab;
            }
        }
        return beamPrefab;
    }
    public void EndCombat()
    {
        TimeManager.Instance.ResumeTime(); // Resume the game when combat UI is closed
    }
    public void SetUpLocalPlayer()
    {
        GameObject thisCombatUIGameObject = CombatUICanvas;

        if (thisCombatUIGameObject != null)
        {
            thisCombatUIGameObject.SetActive(true);
            thisCombatUIGameObject.layer = 5;
        }

        //thisCombatUIGameObject.SetActive(true);
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

