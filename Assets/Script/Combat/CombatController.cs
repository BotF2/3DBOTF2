using Assets.Core;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatController : MonoBehaviour
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
    public List<Vector2Int> spiralPositions = new List<Vector2Int>();
    public List<Animator> animators; // Assign in Inspector or dynamically
    public Animator sideOneA1Animator;
    public Animator sideOneA2Animator;
    public Animator sideOneA3Animator;
    public Animator sideTwoA1Animator;
    public Animator sideTwoA2Animator;
    public Animator sideTwoA3Animator;
    private List<ShipController> shipConsSideOne = new List<ShipController>();
    private List<ShipController> shipConsSideTwo = new List<ShipController>();
    public bool warpingIn = false;
    public bool warpingAnimationOver = false;
    public GameObject SideOneTorpedoPrefab;
    public GameObject SideTwoTorpedoPrefab;
    public GameObject SideOneBeamPrefab;
    public GameObject SideTwoBeamPrefab;
    [Header("First Firing Delay Ranges")]
    [SerializeField] private float minFirstShotDelay = 0.2f;
    [SerializeField] private float maxFirstShotDelay = 0.9f;

    int _scoutsSide1;
    int _scoutsSide2;
    int _destroyersSide1;
    int _destroyersSide2;
    int _capitalsSide1;
    int _capitalsSide2;
    int _transportsSide1;
    int _transportsSide2;
    List<Vector2Int> _spiralPositionsTran1 = new List<Vector2Int>();
    List<Vector2Int> _spiralPositionsTran2 = new List<Vector2Int>();
    List<Vector2Int> _spiralPositionsOther1 = new List<Vector2Int>();
    List<Vector2Int> _spiralPositionsOther2 = new List<Vector2Int>();

    private void Start()
    {
        minFirstShotDelay = 0.2f;
        maxFirstShotDelay = 0.9f;
    }
    public void SetCombatOrder(CombatOrders order, CivEnum civEnum)
    {
        //**** ToDo: Create Event to update DiplomacyController state between the two civs involved in combat
        if (CombatData.CivEnumSideOne == civEnum)
        {
            CombatData.OrderSideOne = order; // Set the combat order for Side One
            for (int i = 0; i < CombatData.SideOneShipCons.Count; i++)
            {
                CombatData.SideOneShipCons[i].SetShipOrder(order); // Set the combat order for each ship in Side One
            } 
        }
        else if (CombatData.CivEnumSideTwo == civEnum)
        {
            CombatData.OrderSideTwo = order; // Set the combat order for Side One
            for (int i = 0; i < CombatData.SideTwoShipCons.Count; i++)
            {
                CombatData.SideTwoShipCons[i].SetShipOrder(order); // Set the combat order for each ship in Side One
            }
        }
        else
        {
            Debug.LogWarning("Player does not belong to either combat side.");
        }
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diplomacyCon, IPlayerController player)
    {
        // Implement logic for handling UI diplomacy orders.
    }

    public void GiveIntelOrder(SecretActionsEnum order, IPlayerController player) //ToDo; set up a IntelController
    {
        // Implement logic for handling UI intel orders.
    }
    public void SetShipOrders(CombatOrders order, CivEnum civOfOrder)
    {
        List<ShipController> shipCons = null; // Initialize the variable  
        //int sideSignFactor = -1; // Default to -1 for Side One, will be set to 1 for Side Two
        // Determine which list of ships to use based on the civOfOrder  
        if (civOfOrder == CombatData.CivEnumSideOne)
        {
            //shipCons = CombatData.SideOneShipCons;
            //sideSignFactor = -1; // Side One is always on the left side, ie negative x-axis
            CombatData.OrderSideOne = order;
        }
        else if (civOfOrder == CombatData.CivEnumSideTwo)
        {
            //shipCons = CombatData.SideTwoShipCons;
            // sideSignFactor = 1; // Side Two is always on the right side, ie positive x-axis
            CombatData.OrderSideTwo = order;
        }
    }
    internal void TrySetPlayerOrders(CombatData combatData)
    {
        //ToDo: Implement logic to set player orders based on the combat data.
        //and is player AiPlayerController (do it now) vs RemoteHumanPlayerController (wait for network messages)

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
    public CivController SideTwoCivCombatants()
    {
        return combatController.CombatData.sideTwoCiv;
    }
    public void PopulateShipData(CombatController theCombatController)
    {
        CountShips(); // Count the ships by type for both sides
        combatController = theCombatController;
        if (theCombatController == null)
        {
            Debug.Log("CombatController Instance is null.");
            return;
        }
        CombatUIController.Instance.PanelCombat_Menu.SetActive(true);
        var sideOneShips = theCombatController.CombatData.SideOneShipCons;
        var sideTwoShips = theCombatController.CombatData.SideTwoShipCons;

        PopulateShipGOAndAnimation(sideOneShips, -1); //sideOne is on the left, ships are -x axis...
        PopulateShipGOAndAnimation(sideTwoShips, 1);
    }
    private void PopulateShipGOAndAnimation(List<ShipController> shipConList, int side1negSide2pos)
    {
        int currentTransportIndex1 = -1;
        int currentTransportIndex2 = -1;
        int currentOtherShipIndex1 = -1;
        int currentOtherShipIndex2 = -1;

        if (_transportsSide1 > 0)
        {
            _spiralPositionsTran1 = GenerateSpiralPositions(_transportsSide1);
        }
        if (_transportsSide2 > 0)
        {
            _spiralPositionsTran2 = GenerateSpiralPositions(_transportsSide2);
        }
        if (_scoutsSide1 + _destroyersSide1 + _capitalsSide1 > 0)
        {
            _spiralPositionsOther1 = GenerateSpiralPositions(_scoutsSide1 + _destroyersSide1 + _capitalsSide1);
        }
        if (_scoutsSide2 + _destroyersSide2 + _capitalsSide2 > 0)
        {
            _spiralPositionsOther2 = GenerateSpiralPositions(_scoutsSide2 + _destroyersSide2 + _capitalsSide2);
        }
        int flipAnimation1 = -1;
        int flipAnimation2 = -1;
        for (int i = 0; i < shipConList.Count; i++)
        {
            if (side1negSide2pos < 0)
            {
                shipConsSideOne.Add(shipConList[i]);
            }
            else
            {
                shipConsSideTwo.Add(shipConList[i]);
            }
            shipConList[i].transform.localScale = Vector3.one;
            shipConList[i].name = shipConList[i].ShipData.ShipName;
            shipConList[i].gameObject.SetActive(true);
            //********** Healthbar code here for now *************
            GameObject healthbarGO = Instantiate(CombatManager.Instance.HealthbarPrefab, shipConList[i].transform.position, Quaternion.identity, CombatManager.Instance.CombatUICanvasGO.transform);
            healthbarGO.SetActive(true);
            healthbarGO.transform.SetParent(shipConList[i].transform, false);
            healthbarGO.transform.localPosition = new Vector3(0, -1.5f, 0); // below ship model
            healthbarGO.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); // scale down to fit ship model
            healthbarGO.transform.localRotation = Quaternion.Euler(0, -90 * side1negSide2pos, 0); // face off the side of the ship model
            Image[] healthbarImages = healthbarGO.GetComponentsInChildren<Image>();
            for (int j = 0; j < healthbarImages.Length; j++)
            {
                if (healthbarImages[j].gameObject.name == "HealthFill")
                {
                    shipConList[i].HealthFillImage = healthbarImages[j];
                    shipConList[i].HealthFillImage.fillAmount = 1f; // set to full health
                    shipConList[i].HealthFillImage.color = Color.green; // set to green color
                    //shipConList[i].HealthFillImage.transform.SetParent(shipConList[i].transform, false);
                }
            }
            //shipConList[i].HealthFillImage = shipConList[i].GetComponentInChildren<Image>();
            GameObject shipGameOb = shipConList[i].gameObject;
            shipGameOb.transform.SetPositionAndRotation(new Vector3(0, 0, 0),
                Quaternion.Euler(0, 90 * side1negSide2pos, 29 * side1negSide2pos));
            if (shipGameOb.GetComponent<ShipController>() != null)
            {
                var shipType = shipGameOb.GetComponent<ShipController>().ShipData.ShipType;

                if (shipType == ShipType.Transport)
                {

                    if (side1negSide2pos < 0)
                    {
                        currentTransportIndex1++;
                        sideOneA3Animator.gameObject.SetActive(true);
                        shipGameOb.transform.SetParent(sideOneA3Animator.gameObject.transform, false);
                        SetLocalTransportPosition(shipGameOb, currentTransportIndex1, _spiralPositionsTran1);
                    }
                    else
                    {
                        currentTransportIndex2++;
                        sideTwoA3Animator.gameObject.SetActive(true);
                        shipGameOb.transform.SetParent(sideTwoA3Animator.gameObject.transform, false);
                        SetLocalTransportPosition(shipGameOb, currentTransportIndex2, _spiralPositionsTran2);
                    }
                }
                else
                {
                    if (side1negSide2pos < 0)
                    {
                        currentOtherShipIndex1++;      
                        if (flipAnimation1 < 0)
                        {
                            sideOneA1Animator.gameObject.SetActive(true);
                            shipGameOb.transform.SetParent(sideOneA1Animator.gameObject.transform, false);
                            SetLocalOtherShipPosition(shipGameOb, currentOtherShipIndex1, _spiralPositionsOther1);
                            flipAnimation1 = 1;
                        }
                        else
                        {
                            sideOneA2Animator.gameObject.SetActive(true);
                            shipGameOb.transform.SetParent(sideOneA2Animator.gameObject.transform, false);
                            SetLocalOtherShipPosition(shipGameOb, currentOtherShipIndex1, _spiralPositionsOther1);
                            flipAnimation1 = -1;
                        }
                    }
                    else
                    {
                        currentOtherShipIndex2++;
                        if (flipAnimation2 < 0)
                        {
                            sideTwoA1Animator.gameObject.SetActive(true);
                            shipGameOb.transform.SetParent(sideTwoA1Animator.gameObject.transform, false);
                            SetLocalOtherShipPosition(shipGameOb, currentOtherShipIndex2, _spiralPositionsOther2);
                            flipAnimation2 = 1;
                        }
                        else
                        {
                            sideTwoA2Animator.gameObject.SetActive(true);
                            shipGameOb.transform.SetParent(sideTwoA2Animator.gameObject.transform, false);
                            SetLocalOtherShipPosition(shipGameOb, currentOtherShipIndex2, _spiralPositionsOther2);
                            flipAnimation2 = -1;
                        }
                    }
                }
            }
            Rigidbody rigid = shipGameOb.GetComponent<Rigidbody>();
            rigid.transform.localScale = Vector3.one;
            rigid.useGravity = false;
            rigid.isKinematic = true; // kinematic until warp in is over
            BoxCollider boxCollider = shipGameOb.AddComponent<BoxCollider>();
            boxCollider.isTrigger = false;
            boxCollider.includeLayers = 9;
            //******** ship size here for now **************
            boxCollider.transform.localScale = new Vector3(5, 5, 5); //size model to fit CameraMultitarget calculations and the view appearance;
            float length = 1f;
            float height = 1f;
            float width = 1f;
            GameObject mesheGO = Resources.Load<GameObject>("FBX/" + shipConList[i].ShipData.ShipName.ToUpper().Replace("(CLONE)", ""));
            if (mesheGO == null)
            {  
                mesheGO = Resources.Load<GameObject>("FBX/FED_DESTROYER_I");
            }
            GameObject fbx = Instantiate(mesheGO, shipConList[i].transform);// fbx is as a prefab so instantiate it  
            fbx.name = shipConList[i].ShipData.ShipName.Replace("(CLONE)", "_Model");
            fbx.transform.SetParent(shipGameOb.transform, false);
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
            shipConList[i].SetWeaponPrefabs(); // Set the weapon prefabs for the ship controller
        }

    }

    private void SetLocalTransportPosition(GameObject shipGameOb, int indexTrans, List<Vector2Int> spiralPositions)
    {
        shipGameOb.transform.localPosition = new Vector3(0, spiralPositions[indexTrans].x *100, spiralPositions[indexTrans].y *100);
    }
    private void SetLocalOtherShipPosition(GameObject shipGameOb, int indexOther, List<Vector2Int> spiralPositions)
    {
        shipGameOb.transform.localPosition = new Vector3(0, spiralPositions[indexOther].y * 100, spiralPositions[indexOther].x * 100);
    }

    public void SetPositionByOrders(List<ShipController> shipCons, int sideSignFactor)
    {
        //*** if you need to know which player is ai or local
        //foreach (var player in PlayerManager.Instance.AllPlayerControllers)
        //{
        //    if (player is AiPlayerController ai)
        //        //ai.AssignFleet();
        //    else if (player is LocalHumanPlayerController local)
        //        //local.PrepareForCombat();
        //}
        CombatOrders order;
        if (sideSignFactor < 0)
        {
            order = CombatData.OrderSideOne;
        }
        else
        {
            order = CombatData.OrderSideTwo;
        }
        switch (order)
        {
            case CombatOrders.Engage:
                if (_transportsSide1 > 0)
                {
                    List<Vector2Int> spiralPositions = GenerateSpiralPositions(_transportsSide1);
                    // If there are transports, they should be positioned behind the center of the formation
                    int foundOne = -1;
                    for (int i = 0; i < shipCons.Count; i++)
                    {
                        if (shipCons[i].ShipData.ShipType == ShipType.Transport)
                        {
                            foundOne++;
                            shipCons[i].transform.position = new Vector3((sideTwoA3Animator.transform.position.x * sideSignFactor), spiralPositions[foundOne].x, spiralPositions[foundOne].y);
                        }
                    }
                }
                else
                {
                    List<Vector2Int> spiralPositions = GenerateSpiralPositions(_scoutsSide1 + _destroyersSide1 + _capitalsSide1);
                    int foundOne = -1;
                    for (int i = 0; i < shipCons.Count; i++)
                    {
                        if (shipCons[i].ShipData.ShipType != ShipType.Transport)
                        {
                            foundOne++;
                            shipCons[i].transform.position = new Vector3((sideTwoA1Animator.transform.position.x * sideSignFactor), spiralPositions[foundOne].x, spiralPositions[foundOne].y);
                        }
                    }
                }
                break;
            case CombatOrders.Rush:
                break;
            case CombatOrders.Retreat:
                break;
            case CombatOrders.Formation:
                break;
            case CombatOrders.TargetTransports:
                break;
            default:
                break;
        }
        SetRigidbody();
        //FindClosestPairsForTargets(shipConsSideOne, shipConsSideTwo);
        //FindClosestPairsForTargets(shipConsSideTwo, shipConsSideOne);
        // await warp over in IEnumerator WaitForAllAnimations()
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



    private void SetRigidbody()
    {
        for (int i = 0; i < shipConsSideOne.Count; i++)
        {
            //shipConsSideOne[i].SetWarpInOver();
            var rb = shipConsSideOne[i].GetComponent<Rigidbody>();
            rb.isKinematic = false;
        }
        for (int i = 0; i < shipConsSideTwo.Count; i++)
        {
            //shipConsSideTwo[i].SetWarpInOver();
            var rb = shipConsSideTwo[i].GetComponent<Rigidbody>();
            rb.isKinematic = false;
        }
    }

    //IEnumerator AutoFireWeapons()
    //{
    //    yield return new WaitUntil(() => warpingAnimationOver && shipConsSideOne.Count >= 1 && shipConsSideTwo.Count >= 1);
    //    for (int i = 0; i < shipConsSideOne.Count; i++)
    //    {
    //        shipConsSideOne[i].SetWarpInOver();
    //        var rb = shipConsSideOne[i].GetComponent<Rigidbody>();
    //        rb.isKinematic = false;
    //    }
    //    for (int i = 0; i < shipConsSideTwo.Count; i++)
    //    {
    //        shipConsSideTwo[i].SetWarpInOver();
    //        var rb = shipConsSideTwo[i].GetComponent<Rigidbody>();
    //        rb.isKinematic = false;
    //    }
    //    FindClosestPairsForTargets(shipConsSideOne, shipConsSideTwo);
    //    FindClosestPairsForTargets(shipConsSideTwo, shipConsSideOne);// get target ship for each ship on both sides
    //    FireWeaponsOrder(shipConsSideOne); 
    //    FireWeaponsOrder(shipConsSideTwo);
    //}
    void FindClosestPairsForTargets(List<ShipController> shipListFiring, List<ShipController> shipListTargets)
    {
        for (int i = 0; i < shipListFiring.Count; i++)
        {
            ShipController closestB = null;
            float shortestDist = Mathf.Infinity;
            for (int j = 0; j < shipListTargets.Count; j++)
            {
                Vector3 origin = shipListFiring[i].transform.position;
                Vector3 targetPos = shipListTargets[j].transform.position;
                Vector3 dir = (targetPos - origin).normalized;
                Vector3 safeOrigin = origin + dir * 10f;
                float dist = Vector3.Distance(origin, targetPos);
      
                float distSqr = (shipListFiring[i].transform.position - shipListTargets[j].transform.position).sqrMagnitude;
                if (distSqr < shortestDist)
                {

                    shortestDist = distSqr;
                    if (Physics.Raycast(safeOrigin, dir, out RaycastHit hit, dist, 9) == false)
                    {
                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            closestB = shipListTargets[j];
                        }
                    }
                    // ********** do not know why the raycast is not working, want to check with raycast for one of our ships getting in the way
                    //else if (Physics.Raycast(safeOrigin, dir, out RaycastHit realHit, dist, 9, QueryTriggerInteraction.Collide))
                    //{

                    //    ShipController hitShip = realHit.collider.GetComponent<ShipController>();

                    //    if (hitShip != null)
                    //    {
                    //        // If the first ship we hit is the candidate → line of sight is clear
                    //        if (hitShip == shipListTargets[j])
                    //        {
                    //            if (dist < shortestDist)
                    //            {
                    //                shortestDist = dist;
                    //                closestB = shipListTargets[j];
                    //            }
                    //        }
                    //        else
                    //        {
                    //            // Hit some other ship first (could be friendly) → blocked, skip this target
                    //            continue;
                    //        }
                    //    }
                    //}
                }
            }
            if (closestB != null)
            {
                shipListFiring[i].ShipData.TargetThisShipController = closestB;
            }
        }
    }
    private void FireWeaponsOrderOnShipControllers(List<ShipController> shipCons)
    {
        // Implement logic to fire weapons on their enemy ships
        for (int i = 0; i < shipCons.Count; i++)
        {
            if (shipCons[i].ShipData.TargetThisShipController != null & (shipCons[i].ShipData.TorpedoDamage > 0 || shipCons[i].ShipData.BeamDamage > 0))
            {
                float delay = UnityEngine.Random.Range(minFirstShotDelay, maxFirstShotDelay);
                StartCoroutine(shipCons[i].ShipFireLoop(delay));
            }
        }
    }
    //private IEnumerator ShipFireLoop(ShipController shipCon, float initialDelay)
    //{
    //    yield return new WaitForSeconds(initialDelay);
    //    bool beam = true;
    //    while (true) // ToDo: not true when ship weapons are offline?
    //    {
    //        // Fire the ship's beam weapons
    //        shipCon.FireWeapons(beam);
    //        if (beam)
    //            beam = false;
    //        else
    //            beam = true;
    //        // Wait for a random refire delay before next shot
    //        float refireDelay = UnityEngine.Random.Range(minRefireDelay, maxRefireDelay);
    //        yield return new WaitForSeconds(refireDelay);
    //    }
    //}
    IEnumerator RealtimeTimerCoroutineWeaponDischarge(float delayInSeconds)
    {
        yield return new WaitForSecondsRealtime(delayInSeconds);
    }

    public void RunAnimation()
    {
        List<GameObject> shipGameObjects = new List<GameObject>();
        for (int i = 0; i < CombatData.SideOneShipCons.Count; i++)
        {
            CombatData.SideOneShipCons[i].gameObject.SetActive(true);
            shipGameObjects.Add(CombatData.SideOneShipCons[i].gameObject);
        }
        for (int i = 0; i < CombatData.SideTwoShipCons.Count; i++)
        {
            CombatData.SideTwoShipCons[i].gameObject.SetActive(true);
            shipGameObjects.Add(CombatData.SideTwoShipCons[i].gameObject);
        }
        Scene scene = SceneManager.GetSceneByName("CombatScene");
        while (!scene.isLoaded)
        {
            System.Threading.Thread.Sleep(100); // Wait for the scene to load
            //scene = SceneManager.GetSceneByName("CombatScene");
        }

        GameObject[] cameraTargets = shipGameObjects.ToArray();
        ShipCombatCameraController.Instance.SetTargets(cameraTargets);
        StartCoroutine(WaitForAllAnimations());

        sideOneA1Animator.SetBool("WarpInS1A1", true);
        sideOneA2Animator.SetBool("WarpInS1A2", true);
        sideOneA3Animator.SetBool("WarpInS1A3", true);
        sideTwoA1Animator.SetBool("WarpInS2A1", true);
        sideTwoA2Animator.SetBool("WarpInS2A2", true);
        sideTwoA3Animator.SetBool("WarpInS2A3", true);
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

        int stepSize = 100;
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
    public IEnumerator WaitForAllAnimations()
    {
        ShipCombatCameraController.Instance.SetWarpingIn(true);
        ShipCombatCameraController.Instance.SetWarpingInOver(false);

        // Wait until all animators have stopped playing
        while (AnyAnimatorIsPlaying())
        {
            yield return null; // wait a frame
        }

        ShipCombatCameraController.Instance.SetWarpingIn(false);
        ShipCombatCameraController.Instance.SetWarpingInOver(true);
        warpingAnimationOver = true;
        FindClosestPairsForTargets(shipConsSideOne, shipConsSideTwo);
        FindClosestPairsForTargets(shipConsSideTwo, shipConsSideOne);
        FireWeaponsOrderOnShipControllers(shipConsSideOne);
        FireWeaponsOrderOnShipControllers(shipConsSideTwo);

    }
    private bool AnyAnimatorIsPlaying()
    {
        foreach (Animator animator in animators)
        {
            if (animator != null && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f &&
                !animator.IsInTransition(0))
            {
                return true;
            }
        }
        return false;
    }

    internal void GiveCombatOrders(CombatOrders order, CivEnum civEnumLocalPlayer)
    {
        if (civEnumLocalPlayer == CombatData.CivEnumSideOne || civEnumLocalPlayer == CombatData.CivEnumSideTwo)
            NetworkClient.localPlayer.GetComponent<IPlayerController>().GiveCombatOrder(order, this, civEnumLocalPlayer);
        else if (GameController.Instance.GameData.GameMode == GameMode.SINGLEPLAYER)
        {
            var aiPlayer = PlayerManager.Instance.AllPlayerControllers.Find(p => p is AiPlayerController && (p as AiPlayerController));
            if (aiPlayer != null)
                aiPlayer.GiveCombatOrder(order, this, aiPlayer.PlayerCiv);
        }
    }
}

