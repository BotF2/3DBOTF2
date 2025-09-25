using Assets.Core;
using Mirror.BouncyCastle.Asn1.Crmf;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public GameObject CombatUICanvasGO;
    private Canvas Cambat3DCamvas;
    public GameObject HealthbarPrefab;
    [SerializeField]
    private CombatController combatConPrefab;  
    private CombatData _combatData;
    public List<CombatController> CombatControllers = new List<CombatController>();
    public List<IPlayerController> participants;
    //public List<Animator> animators; // Assign in Inspector or dynamically
    [SerializeField] GameObject sideOneAnima1;
    [SerializeField] GameObject sideOneAnima2;
    [SerializeField] GameObject sideOneAnima3;
    [SerializeField] GameObject sideTwoAnima1;
    [SerializeField] GameObject sideTwoAnima2;
    [SerializeField] GameObject sideTwoAnima3;
    [SerializeField] private Animator _sideOneA1Animator;
    public Animator sideOneA1Animator
    {
        get => _sideOneA1Animator;
        set
        {
            if (value == null) Debug.LogWarning("Assigned null animator");
            _sideOneA1Animator = value;
        }
    }
    [SerializeField] private Animator _sideOneA2Animator;
    public Animator sideOneA2Animator
    {
        get => _sideOneA2Animator;
        set
        {
            if (value == null) Debug.LogWarning("Assigned null animator");
            _sideOneA2Animator = value;
        }
    }
    [SerializeField] private Animator _sideOneA3Animator;
    public Animator sideOneA3Animator
    {
        get => _sideOneA3Animator;
        set
        {
            if (value == null) Debug.LogWarning("Assigned null animator");
            _sideOneA3Animator = value;
        }
    }
    [SerializeField] private Animator _sideTwoA1Animator;
    public Animator sideTwoA1Animator
    {
        get => _sideTwoA1Animator;
        set
        {
            if (value == null) Debug.LogWarning("Assigned null animator");
            _sideTwoA1Animator = value;
        }
    }
    [SerializeField] private Animator _sideTwoA2Animator;
    public Animator sideTwoA2Animator
    {
        get => _sideTwoA2Animator;
        set
        {
            if (value == null) Debug.LogWarning("Assigned null animator");
            _sideTwoA2Animator = value;
        }
    }
    [SerializeField] private Animator _sideTwoA3Animator;
    public Animator sideTwoA3Animator
    {
        get => _sideTwoA3Animator;
        set
        {
            if (value == null) Debug.LogWarning("Assigned null animator");
            _sideTwoA3Animator = value;
        }
    }

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
        var intelCon = IntelligenceManager.Instance.ReturnAnIntelligenceController(diplomacyController.DiplomacyData.CivSideOne, diplomacyController.DiplomacyData.CivSideTwo); //var intelData = CivRelationsManager.Instance.GetRelationsData(diplomacyController.DiplomacyData.CivSideOne, diplomacyController.DiplomacyData.CivSideTwo);
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
                    if (sideOneShips.Count > 0 && sideTwoShips.Count > 0)
                        InstantiateCombatController(sideOneShips, sideTwoShips);
                }
                else if (intelCon.IntelligenceData.LastSeenStarSysController != null)
                {
                    sideTwoShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;
                    if (sideOneShips.Count > 0 && sideTwoShips.Count > 0)
                        InstantiateCombatController(sideOneShips, sideTwoShips);
                }
            }
            else if (intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData != null)
            {
                sideTwoShips = intelCon.IntelligenceData.LastSeenFleetOfSideTwo.FleetData.ShipsList;
                sideOneShips = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData.ShipsList;
            }
        }
    }

    public void InstantiateCombatController(List<ShipController> sideOneShipCons, List<ShipController> sideTwoShipCons)
     {
        {
            CombatData combatData = new CombatData();

            combatData.SideOneShipCons = sideOneShipCons;
            combatData.SideTwoShipCons = sideTwoShipCons;
            combatData.CivEnumSideOne = sideOneShipCons[0].ShipData.CivEnum;
            combatData.CivEnumSideTwo = sideTwoShipCons[0].ShipData.CivEnum;
            combatData.Name = "CombatData_" + CombatControllers.Count.ToString();
            CombatController aCombatController = Instantiate(combatConPrefab, new Vector3(0, 0, 0),
                Quaternion.identity);
            aCombatController.isMoving = false;
            aCombatController.isClosing = false;
            aCombatController.WarpingIn = false;
            aCombatController.WarpingAnimationOver = false;
            aCombatController.ShipCombatCanvas = Cambat3DCamvas;
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

            aCombatController.sideOneA1Animator = sideOneA1Animator;
            aCombatController.animators.Add(aCombatController.sideOneA1Animator);
            aCombatController.sideOneA2Animator = sideOneA2Animator;
            aCombatController.animators.Add(aCombatController.sideOneA2Animator);
            aCombatController.sideOneA3Animator = sideOneA3Animator;
            aCombatController.animators.Add(aCombatController.sideOneA3Animator);
            aCombatController.sideTwoA1Animator = sideTwoA1Animator;
            aCombatController.animators.Add(aCombatController.sideTwoA1Animator);
            aCombatController.sideTwoA2Animator = sideTwoA2Animator;
            aCombatController.animators.Add(aCombatController.sideTwoA2Animator);
            aCombatController.sideTwoA3Animator = sideTwoA3Animator;
            aCombatController.animators.Add(aCombatController.sideTwoA3Animator);    
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
        GameObject thisCombatUIGameObject = CombatUICanvasGO;

        if (thisCombatUIGameObject != null)
        {
            thisCombatUIGameObject.SetActive(true);
            thisCombatUIGameObject.layer = 5;
        }

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

    //internal void RemoveShip(ShipController shipController)
    //{
    //    for (int i = 0; i < CombatControllers.Count; i++)
    //    {
    //        if (CombatControllers[i].CombatData.SideOneShipCons.Contains(shipController))
    //        {
    //            CombatControllers[i].CombatData.SideOneShipCons.Remove(shipController);
    //        }
    //        if (CombatControllers[i].CombatData.SideTwoShipCons.Contains(shipController))
    //        {
    //            CombatControllers[i].CombatData.SideTwoShipCons.Remove(shipController);
    //        }
    //    }
    //}

    internal void RemoveThisShipController(ShipController shipController)
    {
        for (int i = 0; i < CombatControllers.Count; i++)
        {
            for (int j = 0; j < CombatControllers[i].CombatData.SideOneShipCons.Count; j++)
            {
                if (CombatControllers[i].CombatData.SideOneShipCons[j] == shipController)
                {
                    CombatControllers[i].CombatData.SideOneShipCons.Remove(shipController);
                    Scene combatScene = SceneManager.GetSceneByName("CombatScene");
                    combatScene.GetRootGameObjects().ToList().ForEach(go => Destroy(go));
                    ShipCombatCameraController.Instance.WarpingInOver = false; // also turns off autoroation of camera
                    break;
                }
            }
            for (int j = 0; j < CombatControllers[i].CombatData.SideTwoShipCons.Count; j++)
            {
                if (CombatControllers[i].CombatData.SideTwoShipCons[j] == shipController)
                {
                    CombatControllers[i].CombatData.SideTwoShipCons.Remove(shipController);
                    Scene combatScene = SceneManager.GetSceneByName("CombatScene");
                    combatScene.GetRootGameObjects().ToList().ForEach(go => Destroy(go));
                    ShipCombatCameraController.Instance.WarpingInOver = false;
                    break;
                }
            }
        }
    }
}

