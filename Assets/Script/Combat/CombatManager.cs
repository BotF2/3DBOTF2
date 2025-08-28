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
        // inquiry the CivRelationsManager's Dictionary for current fleet/system ships data
        // to populate the combat data
        var sideOneShips = new List<ShipController>();
        var sideTwoShips = new List<ShipController>();
        var intelCon = IntelligenceManager.Instance.ReturnAnIntelligenceController(diplomacyController.DiplomacyData.CivSideOne, diplomacyController.DiplomacyData.CivSideTwo);       //var intelData = CivRelationsManager.Instance.GetRelationsData(diplomacyController.DiplomacyData.CivSideOne, diplomacyController.DiplomacyData.CivSideTwo);
        if (intelCon != null)
        {
            if (intelCon == null)
            {
                Debug.LogError("IntelData is null in CivRelationsData for civs: " + diplomacyController.DiplomacyData.CivSideOne + " and " + diplomacyController.DiplomacyData.CivSideTwo);
                return;
            }
            if (intelCon.IntelligenceData.LastSeenFleetOfSideOne != null)
            {

                sideOneShips = intelCon.IntelligenceData.LastSeenFleetOfSideOne.FleetData.ShipsList;
                if (intelCon.IntelligenceData.LastSeenFleetOfSideTwo != null)
                {
                    sideTwoShips = intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData.ShipsList;
                    InitCombatData(sideOneShips, sideTwoShips); // instantiate ship game objects
                }
                else
                {
                    sideTwoShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;
                    InitCombatData(sideOneShips, sideTwoShips);
                }
            }
            else if (intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData != null)
            {
                sideTwoShips = intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData.ShipsList;
                sideOneShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;
            }
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
    public void EndCombatTimePause()
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
}

