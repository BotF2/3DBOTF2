using BOTF3D.Audio;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BOTF3D.Combat
{
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

        public SoundData warpInSound;
        public Canvas ShipCombatCanvas;
        public CombatData CombatData { get { return combatData; } set { combatData = value; } }
        public int CombatID { get; private set; } // for specific combat instance
        public List<Vector2Int> spiralPositions = new List<Vector2Int>();
        public List<Animator> animators; // Assign in Inspector or dynamically
        public Animator sideOneA1Animator;
        public Animator sideOneA2Animator;
        public Animator sideOneA3Animator;
        public Animator sideTwoA1Animator;
        public Animator sideTwoA2Animator;
        public Animator sideTwoA3Animator;
        public bool WarpingIn = false;
        public bool WarpingAnimationOver = false;
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
        List<Vector2Int> _spiralPositionsOtherShipsSide1 = new List<Vector2Int>();
        List<Vector2Int> _spiralPositionsOtherShipsSide2 = new List<Vector2Int>();
        List<GameObject> healthbarRenderers = new List<GameObject>();
        [Header("Move animators to move ships")]
        public float initialSpeed = 30f;     // starting velocity (units/sec)
        public float stopDistance;    // distance over which to stop
        private float deceleration;         // computed deceleration
        private float currentSpeed;
        private List<Vector3> moveDirections = new List<Vector3>();
        public bool isMoving = false;
        public bool isClosing = false;

        private void Awake()
        {
            CombatID = GetInstanceID(); // Unity object id for this combat instance
            Debug.Log($"✅ CombatController {CombatID}: Created");
        }
        private void Start()
        {
            minFirstShotDelay = 0.2f;
            maxFirstShotDelay = 0.9f;
            currentSpeed = 30f;
            stopDistance = 390f;
            CleanupOrphanedProjectiles();
            // ✅ TEMPORARY: Stop all AudioSources playing "Explosion" clips on scene load
            //AudioSource[] allSources = FindObjectsOfType<AudioSource>(true);
            //foreach (var source in allSources)
            //{
            //    if (source.clip != null && source.clip.name.Contains("Explosion"))
            //    {
            //        Debug.LogWarning($"⚠️ Stopping auto-play explosion on: {source.gameObject.name}");
            //        source.Stop();
            //        source.playOnAwake = false; // Prevent it from playing again
            //    }
            //}

        }

        void LateUpdate()
        {
            if (WarpingIn && !WarpingAnimationOver)
            {
                for (int i = 0; i < CombatData.SideOneShipCons.Count; i++)
                {
                    CombatData.SideOneShipCons[i].transform.localPosition = new Vector3(0, CombatData.SideOneShipCons[i].transform.position.y, CombatData.SideOneShipCons[i].transform.position.z);
                }
                for (int i = 0; i < CombatData.SideTwoShipCons.Count; i++)
                {
                    CombatData.SideTwoShipCons[i].transform.localPosition = new Vector3(0, CombatData.SideTwoShipCons[i].transform.position.y, CombatData.SideTwoShipCons[i].transform.position.z);
                }
            }
            else if (WarpingAnimationOver && !WarpingIn)
            {
                if (isMoving)
                {
                    float step = currentSpeed * Time.deltaTime;
                    for (int i = 0; i < animators.Count; i++)
                    {
                        var numChildren = animators[i].transform.childCount;
                        for (int j = 0; j < numChildren; j++)
                        {
                            animators[i].transform.GetChild(j).transform.Translate(moveDirections[i] * step, Space.Self);
                        }
                    }
                    currentSpeed -= deceleration * Time.deltaTime;
                    if (currentSpeed <= 0f)
                    {
                        currentSpeed = 0f;
                        isMoving = false;
                    }
                }
            }
            if (!isClosing)
            {
                if (CombatData != null &&
                    CombatData.SideOneShipCons != null &&
                    CombatData.SideTwoShipCons != null)
                {
                    // Clean up null/destroyed ships from lists
                    CombatData.SideOneShipCons.RemoveAll(s => s == null || s.gameObject == null);
                    CombatData.SideTwoShipCons.RemoveAll(s => s == null || s.gameObject == null);

                    if (CombatData.SideOneShipCons.Count == 0 || CombatData.SideTwoShipCons.Count == 0)
                    {
                        isClosing = true;
                        Debug.Log($"Combat ending - Side 1: {CombatData.SideOneShipCons.Count}, Side 2: {CombatData.SideTwoShipCons.Count}");

                        if (CombatUIManager.Instance != null)
                        {
                            CombatUIManager.Instance.ShowCombatOverPanel();
                        }

                        StartCoroutine(DelayedActionSomeSec());
                    }
                }
            }

        }
        /// <summary>
        /// Destroys any torpedoes/beams left in the scene from previous combat
        /// </summary>
        private void CleanupOrphanedProjectiles()
        {
            var torpedoes = FindObjectsOfType<Torpedo>();
            if (torpedoes.Length > 0)
            {
                Debug.Log($"⚠️ Found {torpedoes.Length} orphaned torpedoes - destroying silently");

                foreach (var torpedo in torpedoes)
                {
                    // ✅ Destroy immediately without triggering OnDestroy() sound
                    DestroyImmediate(torpedo.gameObject);
                }
            }
        }
        public void BeginPhysicsLikeMovement()
        {
            moveDirections.Clear();
            for (int i = 0; i < animators.Count; i++)
            {
                Vector3 dir = Vector3.zero;
                if (animators[i].transform.childCount > 0)
                {
                    dir = (i < 3) ? -animators[i].transform.GetChild(0).transform.right.normalized
                                        : animators[i].transform.GetChild(0).transform.right.normalized;
                }
                else
                {
                    dir = Vector3.zero;
                }

                moveDirections.Add(dir.normalized); // cache direction
            }

            deceleration = (initialSpeed * initialSpeed) / (2f * stopDistance); // 2f would be stop at the distance.
            currentSpeed = initialSpeed;
            isMoving = true;
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
        public void SetShipOrders(CombatOrders order, CivEnum civOfOrder)
        {
            // Determine which list of ships to use based on the civOfOrder  
            if (civOfOrder == CombatData.CivEnumSideOne)
            {
                CombatData.OrderSideOne = order;
            }
            else if (civOfOrder == CombatData.CivEnumSideTwo)
            {
                CombatData.OrderSideTwo = order;
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

        internal void TrySetPlayerOrders(CombatData combatData)
        {
            //ToDo: Implement AI logic to set player orders based on the combat data.
            //and is player AiPlayerController (do it now) vs RemoteHumanPlayerController (wait for network messages)

        }

        /// <summary>
        /// Call this when combat ends - destroys fleets with no ships left
        /// </summary>
        public void EndCombat()
        {
            Debug.Log("=== EndCombat: Starting cleanup ===");

            // ✅ CRITICAL: Hide/destroy combat ship visuals FIRST
            if (CombatData != null)
            {
                // Hide all side one ships
                if (CombatData.SideOneShipCons != null)
                {
                    foreach (var ship in CombatData.SideOneShipCons)
                    {
                        if (ship != null && ship.gameObject != null)
                        {
                            var boxCollider = ship.GetComponent<BoxCollider>();
                            if (boxCollider != null) Destroy(boxCollider);

                            ship.gameObject.SetActive(false);
                            ship.transform.SetParent(null);
                        }
                    }
                }

                // Hide all side two ships
                if (CombatData.SideTwoShipCons != null)
                {
                    foreach (var ship in CombatData.SideTwoShipCons)
                    {
                        if (ship != null && ship.gameObject != null)
                        {
                            var boxCollider = ship.GetComponent<BoxCollider>();
                            if (boxCollider != null) Destroy(boxCollider);

                            ship.gameObject.SetActive(false);
                            ship.transform.SetParent(null);
                        }
                    }
                }
            }

            // Get all fleets involved in combat
            var allCombatFleets = new List<FleetController>();

            if (CombatData != null)
            {
                if (CombatData.SideOneShipCons != null)
                {
                    foreach (var ship in CombatData.SideOneShipCons)
                    {
                        if (ship != null && ship.ShipData != null && ship.ShipData.CurrentFleetController != null)
                        {
                            if (!allCombatFleets.Contains(ship.ShipData.CurrentFleetController))
                            {
                                allCombatFleets.Add(ship.ShipData.CurrentFleetController);
                            }
                        }
                    }
                }

                if (CombatData.SideTwoShipCons != null)
                {
                    foreach (var ship in CombatData.SideTwoShipCons)
                    {
                        if (ship != null && ship.ShipData != null && ship.ShipData.CurrentFleetController != null)
                        {
                            if (!allCombatFleets.Contains(ship.ShipData.CurrentFleetController))
                            {
                                allCombatFleets.Add(ship.ShipData.CurrentFleetController);
                            }
                        }
                    }
                }
            }

            Debug.Log($"  Found {allCombatFleets.Count} unique fleets in combat");

            // Destroy empty fleets
            foreach (var fleet in allCombatFleets)
            {
                if (fleet == null) continue;

                int shipCount = fleet.FleetData?.ShipsList?.Count ?? 0;
                Debug.Log($"  Fleet '{fleet.name}': {shipCount} ships remaining");

                if (shipCount == 0)
                {
                    Debug.Log($"  ✅ Destroying empty fleet '{fleet.name}'");
                    if (FleetManager.Instance != null)
                    {
                        FleetManager.Instance.DestroyFleetController(fleet);
                    }
                }
            }

            // ✅ Clear temp fog revealer
            if (FleetManager.Instance != null && FleetManager.Instance.TempFogRevealerFleet != null)
            {
                FleetManager.Instance.TempFogRevealerFleet = null;
            }

            // ✅ Destroy CombatUICanvas
            if (ShipCombatCanvas != null)
            {
                Destroy(ShipCombatCanvas.gameObject);
                Debug.Log("  Destroyed CombatUICanvas");
            }

            // ✅ Destroy all health bars
            foreach (var hb in healthbarRenderers)
            {
                if (hb != null) Destroy(hb);
            }
            healthbarRenderers.Clear();
            // ✅ Clean up UI references
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.CleanupCombat();
            }
            Debug.Log("=== EndCombat: Cleanup complete ===");

            // ✅ Unload combat scene
            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            if (combatScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(combatScene);
                Debug.Log("  Unloaded combat scene");
            }

            SceneController.Instance.UnloadCombatScene();
            SceneController.Instance.ReturnToGalaxyFromCombat();

            // ✅ Re-enable galaxy camera
            if (GalaxyCameraDragMoveZoom.Instance != null)
            {
                var galaxyCam = GalaxyCameraDragMoveZoom.Instance.GetComponent<Camera>();
                if (galaxyCam != null)
                {
                    galaxyCam.enabled = true;
                    Debug.Log($"  Galaxy camera enabled: {galaxyCam.enabled}");
                }
                GalaxyCameraDragMoveZoom.Instance.EnableCameraControl();
            }

            // ✅ Hide star system UI when returning from combat
            if (StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.MoveBackAnyStarSysUIGO(); // ✅ This method exists!
                StarSysMenuUIController.Instance.HideA_SystemMenuView(); // ✅ And this!
            }

            // Resume time
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.ResumeTime();
                Debug.Log("  Resumed time");
                CombatManager.Instance.OnCombatEnded(this);
            }

            Debug.Log("=== EndCombat: Complete ===");
        }
        public void PlayExplosionSound(Vector3 position)
        {
            AudioManager.Instance.PlaySFX3D("Explosion", position);
        }

        public void PlayLaserSound()
        {
            AudioManager.Instance.PlayRandomSFX("LaserShot"); // Plays random variation
        }

        public void PlayShieldHitSound()
        {
            AudioManager.Instance.PlaySFX("ShieldHit");
        }
        public void ResetFriendAndEnemyLists()
        {
            CombatData.SideOneShipCons.Clear();
            CombatData.SideTwoShipCons.Clear();
        }
        public CivController SideOneCivCombatants()
        {
            return CombatData.sideOneCiv;
        }
        public CivController SideTwoCivCombatants()
        {
            return CombatData.sideTwoCiv;
        }
        public void PopulateShipData(CombatController theCombatController)
        {
            CountShips(); // Count the ships by type for both sides
            if (theCombatController == this)
            {
                List<ShipController> sideOneShips = theCombatController.CombatData.SideOneShipCons;
                List<ShipController> sideTwoShips = theCombatController.CombatData.SideTwoShipCons;
                PopulateShipGOAndAnimation(sideOneShips, -1); //sideOne is on the left, ships are -x axis world space attached to an animator...
                PopulateShipGOAndAnimation(sideTwoShips, 1);
            }
        }
        private void PopulateShipGOAndAnimation(List<ShipController> shipConList, int side1negSide2pos)
        {
            if (ShipCombatCanvas == null)
            {
                ShipCombatCanvas = FindAnyObjectByType<Canvas>();
            }
            ShipCombatCanvas.worldCamera = ShipCombatCameraController.Instance.GetComponentInChildren<Camera>();
            if (ShipCombatCanvas != null)
            {
                // ✅ Configure for World Space rendering
                ShipCombatCanvas.renderMode = RenderMode.WorldSpace;
                ShipCombatCanvas.worldCamera = ShipCombatCameraController.Instance.GetComponentInChildren<Camera>();

                // ✅ IMPORTANT: Set canvas scale for world space (1 = 1 Unity unit)
                var canvasRect = ShipCombatCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    canvasRect.localScale = Vector3.one;
                }

                Debug.Log($"✅ Canvas configured: RenderMode={ShipCombatCanvas.renderMode}, Camera={ShipCombatCanvas.worldCamera?.name}");
            }
            else
            {
                Debug.LogError("❌ ShipCombatCanvas is NULL!");
                return;
            }
            int currentTransportIndex1 = -1;
            int currentTransportIndex2 = -1;
            int currentOtherShipIndex1 = -1;
            int currentOtherShipIndex2 = -1;

            if (_transportsSide1 > 0 && _spiralPositionsTran1.Count == 0)
            {
                _spiralPositionsTran1 = GenerateSpiralPositions(_transportsSide1);
            }
            if (_transportsSide2 > 0 && _spiralPositionsTran2.Count == 0)
            {
                _spiralPositionsTran2 = GenerateSpiralPositions(_transportsSide2);
            }
            if (_scoutsSide1 + _destroyersSide1 + _capitalsSide1 > 0 && _spiralPositionsOtherShipsSide1.Count == 0)
            {
                _spiralPositionsOtherShipsSide1 = GenerateSpiralPositions(_scoutsSide1 + _destroyersSide1 + _capitalsSide1);
            }
            if (_scoutsSide2 + _destroyersSide2 + _capitalsSide2 > 0 && _spiralPositionsOtherShipsSide2.Count == 0)
            {
                _spiralPositionsOtherShipsSide2 = GenerateSpiralPositions(_scoutsSide2 + _destroyersSide2 + _capitalsSide2);
            }
            int flipAnimation1 = -1;
            int flipAnimation2 = -1;
            for (int i = 0; i < shipConList.Count; i++)
            {
                shipConList[i].transform.localScale = Vector3.one;
                shipConList[i].name = shipConList[i].ShipData.ShipName;
                shipConList[i].gameObject.SetActive(true);
                //********** Health bar code here for now *************
                GameObject healthbarGO = Instantiate(CombatManager.Instance.HealthbarPrefab);
                healthbarGO.SetActive(true);
                healthbarGO.SetActive(true);
                // ✅ Parent directly to ship (skip canvas entirely for world-space UI)
                healthbarGO.transform.SetParent(shipConList[i].transform, false);
                healthbarGO.transform.localPosition = new Vector3(5 * side1negSide2pos, -1.5f, 0);
                healthbarGO.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                healthbarGO.transform.localRotation = Quaternion.Euler(0, -90 * side1negSide2pos, 0);
                // ✅ Ensure health bar Canvas is on World Space
                Canvas healthbarCanvas = healthbarGO.GetComponent<Canvas>();
                if (healthbarCanvas == null)
                {
                    healthbarCanvas = healthbarGO.AddComponent<Canvas>();
                }

                healthbarCanvas.renderMode = RenderMode.WorldSpace;
                healthbarCanvas.worldCamera = ShipCombatCameraController.Instance.GetComponentInChildren<Camera>();

                // ✅ Add CanvasScaler for proper sizing
                var canvasScaler = healthbarGO.GetComponent<CanvasScaler>();
                if (canvasScaler == null)
                {
                    canvasScaler = healthbarGO.AddComponent<CanvasScaler>();
                }
                canvasScaler.dynamicPixelsPerUnit = 10;

                // ✅ Set health bar layer to Default (NOT UI layer for world-space)
                healthbarGO.layer = LayerMask.NameToLayer("Default");

                // Set child layers recursively
                SetLayerRecursively(healthbarGO, LayerMask.NameToLayer("Default"));

                Image[] healthbarImages = healthbarGO.GetComponentsInChildren<Image>();
                for (int j = 0; j < healthbarImages.Length; j++)
                {
                    if (healthbarImages[j].gameObject.name == "HealthFill")
                    {
                        shipConList[i].HealthFillImage = healthbarImages[j];
                        shipConList[i].HealthFillImage.fillAmount = 1f;
                        shipConList[i].HealthFillImage.color = Color.green;
                    }
                }

                healthbarGO.SetActive(false); // Start hidden until warp-in completes
                healthbarRenderers.Add(healthbarGO);

                // ✅ Add billboard component to face camera
                var billboard = healthbarGO.GetComponent<BillboardCameraCombat>();
                if (billboard == null)
                {
                    billboard = healthbarGO.AddComponent<BillboardCameraCombat>();
                }

                Debug.Log($"  ✅ Created health bar for {shipConList[i].ShipData.ShipName}");
                GameObject shipGameOb = shipConList[i].gameObject;
                shipGameOb.transform.SetPositionAndRotation(new Vector3(0, 0, 0),
                    Quaternion.Euler(0, 0, 0)); // 90 * side1negSide2pos, 0));
                if (shipGameOb.GetComponent<ShipController>() != null)
                {
                    var shipType = shipGameOb.GetComponent<ShipController>().ShipData.ShipType;

                    if (shipType == ShipType.Transport)
                    {
                        if (side1negSide2pos < 0)
                        {
                            currentTransportIndex1++;
                            if (currentTransportIndex1 <= (_spiralPositionsTran1.Count - 1))
                            {
                                sideOneA3Animator.gameObject.SetActive(true);
                                shipGameOb.transform.SetParent(sideOneA3Animator.gameObject.transform, false);
                                SetLocalTransportPosition(shipGameOb, currentTransportIndex1, _spiralPositionsTran1);

                            }
                        }
                        else
                        {
                            currentTransportIndex2++;
                            if (currentTransportIndex2 <= (_spiralPositionsTran2.Count - 1))
                            {
                                sideTwoA3Animator.gameObject.SetActive(true);
                                shipGameOb.transform.SetParent(sideTwoA3Animator.gameObject.transform, false);
                                SetLocalTransportPosition(shipGameOb, currentTransportIndex2, _spiralPositionsTran2);

                            }
                        }
                    }
                    else
                    {
                        if (side1negSide2pos < 0)
                        {
                            currentOtherShipIndex1++;
                            if (currentOtherShipIndex1 <= (_spiralPositionsOtherShipsSide1.Count - 1))
                            {
                                if (flipAnimation1 < 0)
                                {
                                    sideOneA1Animator.gameObject.SetActive(true);
                                    shipGameOb.transform.SetParent(sideOneA1Animator.gameObject.transform, false);
                                    SetLocalOtherShipPosition(shipGameOb, currentOtherShipIndex1, _spiralPositionsOtherShipsSide1);

                                    flipAnimation1 = 1;
                                }
                                else
                                {
                                    sideOneA2Animator.gameObject.SetActive(true);
                                    shipGameOb.transform.SetParent(sideOneA2Animator.gameObject.transform, false);
                                    SetLocalOtherShipPosition(shipGameOb, currentOtherShipIndex1, _spiralPositionsOtherShipsSide1);

                                    flipAnimation1 = -1;
                                }
                            }

                        }
                        else if (side1negSide2pos > 0)
                        {
                            currentOtherShipIndex2++;
                            if (currentOtherShipIndex2 <= (_spiralPositionsOtherShipsSide2.Count - 1))
                            {
                                if (flipAnimation2 < 0)
                                {
                                    sideTwoA1Animator.gameObject.SetActive(true);
                                    shipGameOb.transform.SetParent(sideTwoA1Animator.gameObject.transform, false);
                                    SetLocalOtherShipPosition(shipGameOb, currentOtherShipIndex2, _spiralPositionsOtherShipsSide2);

                                    flipAnimation2 = 1;
                                }
                                else
                                {
                                    sideTwoA2Animator.gameObject.SetActive(true);
                                    shipGameOb.transform.SetParent(sideTwoA2Animator.gameObject.transform, false);
                                    SetLocalOtherShipPosition(shipGameOb, currentOtherShipIndex2, _spiralPositionsOtherShipsSide2);

                                    flipAnimation2 = -1;
                                }
                            }
                        }
                    }
                }
                shipGameOb.transform.localRotation = Quaternion.Euler(0, 90 * side1negSide2pos, 0);
                Rigidbody rigid = shipGameOb.GetComponent<Rigidbody>();
                rigid.useGravity = false;
                rigid.isKinematic = true; // kinematic until warp in is over
                BoxCollider boxCollider = shipGameOb.AddComponent<BoxCollider>();
                boxCollider.isTrigger = false;
                boxCollider.includeLayers = 9;
                //******** ship size here for now **************
                boxCollider.transform.localScale = new Vector3(5, 5, 5); //size model to fit ShipCombatCameraController calculations and the view appearance;
                float length = 1f;
                float height = 1f;
                float width = 1f;

                ShipSO shipSO = GetShipSOForShip(shipConList[i]);  // You need to pass ShipSO to this method
                GameObject mesheGO = shipSO != null ? shipSO.ShipFBX_ModelAsGOPrefab : null;

                if (mesheGO == null)
                {
                    Debug.LogWarning($"❌NEED FBX MODLE IN SO❌ Ship model prefab is NULL for {shipConList[i].ShipData.ShipName}");

                    // ✅ Load fallback from ShipManager
                    ShipSO fallbackSO = ShipManager.Instance.GetFallbackShipSO();
                    mesheGO = fallbackSO?.ShipFBX_ModelAsGOPrefab;
                }

                if (mesheGO == null)
                {
                    Debug.LogError("❌ Fallback ship model also NULL - cannot spawn ship!");
                    continue;  // Skip this ship
                }

                GameObject fbx = Instantiate(mesheGO, shipGameOb.transform, false);

                //GameObject mesheGO = Resources.Load<GameObject>("FBX/" + shipConList[i].ShipData.ShipName.ToUpper().Replace("(CLONE)", ""));
                //if (mesheGO == null)
                //{
                //    mesheGO = Resources.Load<GameObject>("FBX/FED_DESTROYER_I");
                //}
                //GameObject fbx = Instantiate(mesheGO, shipGameOb.transform, false);// fbx is as a prefab so instantiate it  
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
        /// <summary>
        /// Sets the layer of a GameObject and all its children recursively
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        private void SetLocalTransportPosition(GameObject shipGameOb, int indexTrans, List<Vector2Int> spiralPositions)
        {
            shipGameOb.transform.localPosition = new Vector3(0, spiralPositions[indexTrans].x * 100, spiralPositions[indexTrans].y * 100);
        }
        private void SetLocalOtherShipPosition(GameObject shipGameOb, int indexOther, List<Vector2Int> spiralPositions)
        {
            shipGameOb.transform.localPosition = new Vector3(0, spiralPositions[indexOther].x * 100, spiralPositions[indexOther].y * 100);
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
                        // ********** do not know why the ray cast is not working, want to check with ray cast for one of our ships getting in the way
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

        IEnumerator RealtimeTimerCoroutineWeaponDischarge(float delayInSeconds)
        {
            yield return new WaitForSecondsRealtime(delayInSeconds);
        }

        public void RunAnimation()
        {
            WarpingIn = true;
            WarpingAnimationOver = false;

            // ✅ Play warp-in sound
            if (warpInSound != null)
            {
                AudioSource tempSource = gameObject.AddComponent<AudioSource>();
                tempSource.playOnAwake = false;
                tempSource.spatialBlend = 0f;
                warpInSound.Play(tempSource);
                AudioClip clip = warpInSound.GetClip();
                float clipLength = clip != null ? clip.length : 2f;
                Destroy(tempSource, clipLength / warpInSound.GetPitchWithVariation() + 0.5f);
                Debug.Log("🔊 Playing warp-in sound from CombatController");
            }
            else
            {
                Debug.LogWarning("⚠️ warpInSound is not assigned on CombatController!");
            }

            // ✅ NEW: Trigger animations on animator GameObjects
            Debug.Log("🎬 Triggering animator scripts...");

            if (sideOneA1Animator != null)
            {
                var animScript = sideOneA1Animator.GetComponent<S1A1Animator>();
                if (animScript != null)
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S1A1Animator");
                }
            }

            if (sideOneA2Animator != null)
            {
                var animScript = sideOneA2Animator.GetComponent<S1A2Animator>();
                if (animScript != null)
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S1A2Animator");
                }
            }

            if (sideOneA3Animator != null)
            {
                var animScript = sideOneA3Animator.GetComponent<S1A3Animator>();
                if (animScript != null)
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S1A3Animator");
                }
            }

            if (sideTwoA1Animator != null)
            {
                var animScript = sideTwoA1Animator.GetComponent<S2A1Animator>();
                if (animScript != null)
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S2A1Animator");
                }
            }

            if (sideTwoA2Animator != null)
            {
                var animScript = sideTwoA2Animator.GetComponent<S2A2Animator>();
                if (animScript != null)
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S2A2Animator");
                }
            }

            if (sideTwoA3Animator != null)
            {
                var animScript = sideTwoA3Animator.GetComponent<S2A3Animator>();
                if (animScript != null)
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S2A3Animator");
                }
            }

            List<GameObject> shipGameObjects = new List<GameObject>();
            for (int i = 0; i < CombatData.SideOneShipCons.Count; i++)
            {
                CombatData.SideOneShipCons[i].gameObject.SetActive(true);
                shipGameObjects.Add(CombatData.SideOneShipCons[i].gameObject);
                CombatData.SideOneShipCons[i].SetWarpInOver();
            }
            for (int i = 0; i < CombatData.SideTwoShipCons.Count; i++)
            {
                CombatData.SideTwoShipCons[i].gameObject.SetActive(true);
                shipGameObjects.Add(CombatData.SideTwoShipCons[i].gameObject);
                CombatData.SideTwoShipCons[i].SetWarpInOver();
            }

            Scene scene = SceneManager.GetSceneByName("CombatScene");
            while (!scene.isLoaded)
            {
                System.Threading.Thread.Sleep(100);
            }

            GameObject[] cameraTargets = shipGameObjects.ToArray();
            ShipCombatCameraController.Instance.SetTargets(cameraTargets);
            StartCoroutine(WaitForAllAnimations());

        }
        private List<Vector2Int> GenerateSpiralPositions(int count)
        {    // output (0,0), (10,0), (10,10), (0,10), (-10,10), (-10,0), (-10,-10), (0,-10), ...
            spiralPositions.Clear();

            Vector2Int[] directions =
            {
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
        IEnumerator DelayedActionSomeSec()
        {
            yield return new WaitForSeconds(2f);
            // Action to perform after the delay
            EndCombat();
        }
        public IEnumerator WaitForAllAnimations()
        {
            ShipCombatCameraController.Instance.SetWarpingIn(true);
            ShipCombatCameraController.Instance.SetWarpingInOver(false);

            // ✅ Check if any animators have controllers assigned
            bool hasValidAnimators = animators.Any(a => a != null && a.runtimeAnimatorController != null);

            if (hasValidAnimators)
            {
                Debug.Log($"⏳ Waiting for animator-based warp-in... ({animators.Count} animators with controllers)");

                // ✅ NEW: Wait one frame for animator scripts' Start() to run
                yield return null;
                Debug.Log("   Frame 1: Animator scripts initialized, beginning animation check...");

                int frameCount = 0;
                int maxFrames = 600; // Safety timeout (10 seconds at 60fps)

                // Wait for animations to complete
                while (AnyAnimatorIsPlaying())
                {
                    frameCount++;

                    // ✅ Log every 30 frames (twice per second)
                    if (frameCount % 30 == 0)
                    {
                        Debug.Log($"   Frame {frameCount}: Still waiting for animations...");
                    }

                    // ✅ Safety timeout
                    if (frameCount > maxFrames)
                    {
                        Debug.LogWarning($"⚠️ Animation timeout after {maxFrames} frames - force continuing");
                        break;
                    }

                    yield return null;
                }

                Debug.Log($"✅ Animation check complete after {frameCount} frames");
            }
            else
            {
                // ✅ No valid animators - use timed wait instead
                Debug.LogWarning("⚠️ No AnimatorControllers assigned - using timed warp-in (3 seconds)");
                yield return new WaitForSeconds(3f);
            }

            Debug.Log("✅ Warp-in animation complete");

            ShipCombatCameraController.Instance.SetWarpingIn(false);
            ShipCombatCameraController.Instance.SetWarpingInOver(true);

            // ✅ Start ship movement
            BeginPhysicsLikeMovement();

            // ✅ Show health bars
            for (int i = 0; i < healthbarRenderers.Count; i++)
            {
                healthbarRenderers[i].SetActive(true);
            }

            WarpingAnimationOver = true;
            WarpingIn = false;

            // ✅ Wait for ships to move closer (2 seconds) before firing
            Debug.Log("⏳ Ships moving to battle positions...");
            yield return new WaitForSeconds(2f);
            Debug.Log("✅ Ships in position - starting weapon fire");

            // ✅ Now assign targets and fire weapons
            FindClosestPairsForTargets(CombatData.SideOneShipCons, CombatData.SideTwoShipCons);
            FindClosestPairsForTargets(CombatData.SideTwoShipCons, CombatData.SideOneShipCons);
            FireWeaponsOrderOnShipControllers(CombatData.SideOneShipCons);
            FireWeaponsOrderOnShipControllers(CombatData.SideTwoShipCons);
        }

        private bool AnyAnimatorIsPlaying()
        {
            bool anyPlaying = false;

            for (int i = 0; i < animators.Count; i++)
            {
                Animator animator = animators[i];

                if (animator == null)
                {
                    Debug.LogWarning($"   ⚠️ Animator [{i}] is null");
                    continue;
                }

                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"   ⚠️ Animator [{i}] ({animator.name}) has no controller");
                    continue;
                }

                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                bool isInTransition = animator.IsInTransition(0);
                float normalizedTime = stateInfo.normalizedTime;

                // ✅ Detailed logging for first frame
                if (Time.frameCount % 30 == 0) // Log every 30 frames
                {
                    Debug.Log($"   Animator [{i}] '{animator.name}': State='{stateInfo.shortNameHash}', Time={normalizedTime:F3}, Transition={isInTransition}");
                }

                if (normalizedTime < 1f && !isInTransition)
                {
                    anyPlaying = true;
                }
            }

            return anyPlaying;
        }

        internal void GiveCombatOrders(CombatOrders order, CivEnum civEnumLocalPlayer)
        {
            if (civEnumLocalPlayer == CombatData.CivEnumSideOne || civEnumLocalPlayer == CombatData.CivEnumSideTwo)
                NetworkClient.localPlayer.GetComponent<IPlayerController>().GiveCombatOrder(order, this, civEnumLocalPlayer);
            else if (GameController.Instance.GameData.GameMode == GameMode.SINGLEPLAYER)
            {
                //var aiPlayer = PlayerManager.Instance.AllPlayerControllers.Find(p => p is AiPlayerController && (p as AiPlayerController));
                //if (aiPlayer != null)
                //    aiPlayer.GiveCombatOrder(order, this, aiPlayer.PlayerCiv);
            }
        }

        /// <summary>
        /// Initialize combat with two fleets at a location
        /// Called by SceneController after combat scene loads additively
        /// </summary>
        public void InitializeCombat(FleetController playerFleet, FleetController enemyFleet, StarSysController combatLocation)
        {
            Debug.Log($"=== InitializeCombat: Starting ===");
            Debug.Log($"  Player fleet: {(playerFleet != null ? playerFleet.name : "NULL")}");
            Debug.Log($"  Enemy fleet: {(enemyFleet != null ? enemyFleet.name : "NULL")}");
            Debug.Log($"  Location: {(combatLocation != null ? combatLocation.name : "NULL")}");
            // Reset closing flag for new combat
            isClosing = false;
            if (playerFleet == null || enemyFleet == null)
            {
                Debug.LogError("InitializeCombat: One or both fleets are null! Cannot start combat.");
                return;
            }

            // ✅ Initialize CombatData
            if (CombatData == null)
            {
                CombatData = new CombatData();
                Debug.Log("  Created new CombatData");
            }

            // ✅ Assign fleets to sides
            CombatData.CivEnumSideOne = playerFleet.FleetData?.CivEnum ?? CivEnum.None;
            CombatData.CivEnumSideTwo = enemyFleet.FleetData?.CivEnum ?? CivEnum.None;

            Debug.Log($"  Side One Civ: {CombatData.CivEnumSideOne}");
            Debug.Log($"  Side Two Civ: {CombatData.CivEnumSideTwo}");

            CombatData.sideOneCiv = playerFleet.FleetData?.CivController;
            CombatData.sideTwoCiv = enemyFleet.FleetData?.CivController;

            // Clear previous ship lists
            CombatData.SideOneShipCons.Clear();
            CombatData.SideTwoShipCons.Clear();

            // ✅ Add player fleet ships to side one
            if (playerFleet.FleetData?.ShipsList != null)
            {
                Debug.Log($"  Player fleet has {playerFleet.FleetData.ShipsList.Count} ships");

                foreach (var ship in playerFleet.FleetData.ShipsList)
                {
                    if (ship != null)
                    {
                        CombatData.SideOneShipCons.Add(ship);
                        Debug.Log($"    Added player ship: {ship.name}");
                    }
                }
            }
            else
            {
                Debug.LogError("  ❌ Player fleet has NO ShipsList!");
            }

            // ✅ Add enemy fleet ships to side two
            if (enemyFleet.FleetData?.ShipsList != null)
            {
                Debug.Log($"  Enemy fleet has {enemyFleet.FleetData.ShipsList.Count} ships");

                foreach (var ship in enemyFleet.FleetData.ShipsList)
                {
                    if (ship != null)
                    {
                        CombatData.SideTwoShipCons.Add(ship);
                        Debug.Log($"    Added enemy ship: {ship.name}");
                    }
                }
            }
            else
            {
                Debug.LogError("  ❌ Enemy fleet has NO ShipsList!");
            }

            Debug.Log($"  ✅ Side One: {CombatData.SideOneShipCons.Count} ships");
            Debug.Log($"  ✅ Side Two: {CombatData.SideTwoShipCons.Count} ships");

            // Set default orders
            CombatData.OrderSideOne = CombatOrders.Engage;
            CombatData.OrderSideTwo = CombatOrders.Engage;

            // ✅ CRITICAL: Enable combat camera
            if (ShipCombatCameraController.Instance != null)
            {
                var camera = ShipCombatCameraController.Instance.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.enabled = true;
                    Debug.Log($"  ✅ Combat camera enabled: {camera.enabled}");
                }
                else
                {
                    Debug.LogError("  ❌ Combat camera component not found!");
                }
            }
            else
            {
                Debug.LogError("  ❌ ShipCombatCameraController.Instance is NULL!");
            }

            // ✅ CRITICAL: Enable combat canvas
            if (ShipCombatCanvas != null)
            {
                ShipCombatCanvas.gameObject.SetActive(true);
                Debug.Log($"  ✅ Combat canvas activated");
            }
            else
            {
                Debug.LogError("  ❌ ShipCombatCanvas is NULL!");
            }

            if (CombatUIManager.Instance != null)
            {
                Debug.Log($"  ✅ CombatUIManager found and ready");
            }
            else
            {
                Debug.LogWarning("  ⚠️ CombatUIManager.Instance is NULL - UI will not show!");
            }

            // Populate ship data and UI
            Debug.Log("  Calling PopulateShipData...");
            PopulateShipData(this);

            // Start combat animation
            Debug.Log("  Calling RunAnimation...");
            RunAnimation();

            Debug.Log("=== InitializeCombat: Complete ===");
        }

        private ShipSO GetShipSOForShip(ShipController ship)
        {
            // ShipData has a ShipSO property that holds the SO with .fbx game object 'prefab' reference
            return ship?.ShipData?.ShipSO;

        }
    }
}

