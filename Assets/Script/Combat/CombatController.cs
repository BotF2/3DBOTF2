using Assets.Core;
using Mirror.BouncyCastle.Crypto.Macs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    //public int maxPositions = 200; // the max number of positions to generate in the spiral
    public List<Vector2Int> spiralPositions = new List<Vector2Int>();
    public List<Animator> animators; // Assign in Inspector or dynamically
    public Animator sideOneA1Animator;
    public Animator sideOneA2Animator;
    public Animator sideOneA3Animator;
    public Animator sideTwoA1Animator;
    public Animator sideTwoA2Animator;
    public Animator sideTwoA3Animator;
    public bool warpingIn = false;
    public bool warpingAnimationOver = false;
    int _scoutsSide1;
    int _scoutsSide2;
    int _destroyersSide1;
    int _destroyersSide2;
    int _capitalsSide1;
    int _capitalsSide2;
    int _transportsSide1;
    int _transportsSide2;
    int _totalScoutShips;
    int _totalDestroyerShips;
    int _totalCapitalShips;
    int _totalTransportsShips;

    private void Start()
    {

    }
    public void GiveCombatOrder(CombatOrders orders)
    {
        // implementation
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order)
    {
        // Implement logic for handling UI diplomacy orders.
    }

    public void GiveIntelOrder(SecretActionsEnum order)
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

        // Ensure shipCons is not null before proceeding  
        //if (shipCons == null)
        //{
        //    Debug.LogError("Ship list is null. Unable to act on combat order.");
        //    return;
        //}
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
    public CivController EnemyCivCombatants()
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

        BuildShipGOAndPosition(sideOneShips, -1); //sideOne is on the left, ships are -x axis...
        BuildShipGOAndPosition(sideTwoShips, 1);
    }
    private void BuildShipGOAndPosition(List<ShipController> shipConList, int side1negSide2pog)
    {
        int flip = -1;
        for (int i = 0; i < shipConList.Count; i++)
        {
            shipConList[i].transform.localScale = Vector3.one;
            shipConList[i].name = shipConList[i].ShipData.ShipName;
            GameObject shipGameOb = shipConList[i].gameObject;
            shipGameOb.AddComponent<Rigidbody>();
            shipGameOb.transform.SetPositionAndRotation(new Vector3(combatController.CombatData.xStart * side1negSide2pog, i * 100, i * 100),
                Quaternion.Euler(0, 90 * side1negSide2pog, 0));
            if (shipGameOb.GetComponent<ShipController>() != null)
            {
                var shipType = shipGameOb.GetComponent<ShipController>().ShipData.ShipType;

                if (shipType == ShipType.Transport)
                {
                    if (side1negSide2pog < 0)
                    {
                        sideOneA3Animator.gameObject.SetActive(true);
                        shipGameOb.transform.SetParent(sideOneA3Animator.gameObject.transform, true);
                        shipGameOb.transform.localPosition = new Vector3(0, shipGameOb.transform.position.y, shipGameOb.transform.position.z);
                    }
                    else
                    {
                        sideTwoA3Animator.gameObject.SetActive(true);
                        shipGameOb.transform.SetParent(sideTwoA3Animator.gameObject.transform, true);
                        shipGameOb.transform.localPosition = new Vector3(0, shipGameOb.transform.position.y, shipGameOb.transform.position.z);
                    }
                }
                else
                {
                    if (side1negSide2pog < 0)
                    {
                        if (flip < 0)
                        {
                            sideOneA1Animator.gameObject.SetActive(true);
                            shipGameOb.transform.SetParent(sideOneA1Animator.gameObject.transform, true);
                            shipGameOb.transform.localPosition = new Vector3(0, shipGameOb.transform.position.y, shipGameOb.transform.position.z);
                            flip = 1;
                        }
                        else
                        {
                            sideOneA2Animator.gameObject.SetActive(true);
                            shipGameOb.transform.SetParent(sideOneA2Animator.gameObject.transform, true);
                            shipGameOb.transform.localPosition = new Vector3(0, shipGameOb.transform.position.y, shipGameOb.transform.position.z);
                            flip = -1;
                        }
                    }
                    else
                    {
                        if (flip < 0)
                        {
                            sideTwoA1Animator.gameObject.SetActive(true);
                            shipGameOb.transform.SetParent(sideTwoA1Animator.gameObject.transform, true);
                            shipGameOb.transform.localPosition = new Vector3(0, shipGameOb.transform.position.y, shipGameOb.transform.position.z);
                            flip = 1;
                        }
                        else
                        {
                            sideTwoA2Animator.gameObject.SetActive(true);
                            shipGameOb.transform.SetParent(sideTwoA2Animator.gameObject.transform, true);
                            shipGameOb.transform.localPosition = new Vector3(0, shipGameOb.transform.position.y, shipGameOb.transform.position.z);
                            flip = -1;
                        }
                    }
                }
            }
            Rigidbody rigid = shipGameOb.GetComponent<Rigidbody>();
            rigid.transform.localScale = Vector3.one;
            rigid.useGravity = false;
            rigid.isKinematic = true;
            BoxCollider boxCollider = shipGameOb.AddComponent<BoxCollider>();
            boxCollider.transform.localScale = new Vector3(6, 6, 6); //size model to fit CameraMultitarget calculations/view;
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

        ActOnCurrentCombatOrders(shipConList, side1negSide2pog); // Act on the current combat orders for ships of this side
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

    public void ActOnCurrentCombatOrders(List<ShipController> shipCons, int sideSignFactor)
    {
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
    }
    public void RunAnimation()
    {
        //CameraMultiTarget.Instance.SetWarpingInOver(false);
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
        //if (scene.isLoaded)
        //{
        //    foreach (GameObject rootObj in scene.GetRootGameObjects())
        //    {
        //        //GameObject found = rootObj.transform.Find("ShipCameraHolder")?.gameObject;
        //        //if (found != null)
        //        if (rootObj.name == "ShipCameraHolder")
        //        {
        //            CameraMultiTarget.Instance.CameraHolder = rootObj;
        //            Transform shipCamera = rootObj.transform.Find("CameraShips");
        //            if (shipCamera != null)
        //            {
        //                shipCamera.gameObject.SetActive(true);
        //                CameraMultiTarget.Instance.ShipCamera = shipCamera.GetComponent<Camera>();
        //                break;
        //            }
        //        }
        //    }
        //}

        GameObject[] cameraTargets = shipGameObjects.ToArray();
        ShipCombatCameraController.Instance.SetTargets(cameraTargets);
        StartCoroutine(WaitForAllAnimations());
        // This method is called to run the animation for warping in ships
        //if (CombatUIController.Instance.CombatController != null && !CombatUIController.Instance.CombatController.warpingIn)
        //{
        //    AllAnimations(new Animator[] {
        //        sideOneA1Animator, sideOneA2Animator, sideOneA3Animator,
        //        sideTwoA1Animator, sideTwoA2Animator, sideTwoA3Animator
        //    });
        sideOneA1Animator.SetBool("WarpInS1A1", true);
        sideOneA2Animator.SetBool("WarpInS1A2", true);
        sideOneA3Animator.SetBool("WarpInS1A3", true);
        sideTwoA1Animator.SetBool("WarpInS2A1", true);
        sideTwoA2Animator.SetBool("WarpInS2A2", true);
        sideTwoA3Animator.SetBool("WarpInS2A3", true);
        //    warpingIn = true;
        //}
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

    }
    private bool AnyAnimatorIsPlaying()
    {
        foreach (Animator animator in animators)
        {
            if (animator != null && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f &&
                !animator.IsInTransition(0))
            {
                return true;
                //sideOneA1Animator.SetBool("WarpInS1A1", false);
                //sideOneA2Animator.SetBool("WarpInS1A2", false);
                //sideOneA3Animator.SetBool("WarpInS1A3", false);
                //sideTwoA1Animator.SetBool("WarpInS2A1", false);
                //sideTwoA2Animator.SetBool("WarpInS2A2", false);
                //sideTwoA3Animator.SetBool("WarpInS2A3", false);
            }
        }
        return false;
    }
    //public IEnumerator AllAnimations(Animator[] animators)
    //{
    //    bool allDone = false;
    //    warpingAnimationOver = false;

    //    while (!allDone)
    //    {
    //        allDone = true;

    //        foreach (var animator in animators)
    //        {
    //            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

    //            if (info.normalizedTime < 1f || animator.IsInTransition(0))
    //            {
    //                allDone = false;
    //                warpingAnimationOver = false; // Animation is still playing
    //                CameraMultiTarget.Instance.SetWarpingIn(true); // Set the warping in state to over
    //                                                               // 
    //                break;
    //            }
    //            else
    //            {
    //                warpingAnimationOver = true; // Animation is done
    //                CameraMultiTarget.Instance.SetWarpingIn(false); // Set the warping in state to over
    //                CameraMultiTarget.Instance.SetWarpingInOver(true); // Set the warping in state to over
    //            }
    //        }

    //        yield return null;
    //    }

        //Debug.Log("All animations done.");
        //}

    }

