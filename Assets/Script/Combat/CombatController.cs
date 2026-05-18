// Ignore Spelling: BOTF Healthbar

using BOTF3D.Audio;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using BOTF3D.UI;
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
        private static WaitForSecondsRealtime _waitForSeconds3 = new WaitForSecondsRealtime(3f);
        private static WaitForSecondsRealtime _waitForSeconds2 = new WaitForSecondsRealtime(2f);

        /// <summary>
        /// [CombatController]
        /// |
        /// v
        /// [IPlayerController] <--- [LocalHumanPlayerController] (UI)
        ///                     <--- [RemoteHumanPlayerController] (Network)
        ///                     <--- [AIPlayerController] (AI)
        /// </summary>

        private CombatData combatData;
        [Header("Combat Order System")]
        public List<ShipGroup> sideOneGroups = new List<ShipGroup>();
        public List<ShipGroup> sideTwoGroups = new List<ShipGroup>();

        private bool groupsInitialized = false;
        public SoundData warpInSound;
        public Canvas ShipCombatCanvas;
        public CombatData CombatData { get { return combatData; } set { combatData = value; } }
        public int CombatID { get; private set; } // for specific combat instance
        public List<Vector2Int> spiralPositions = new List<Vector2Int>();
        public List<GameObject> shipParents; // Parent GameObjects replacing animators
        public GameObject sideOneA1Parent;
        public GameObject sideOneA2Parent;
        public GameObject sideOneA3Parent;
        public GameObject sideTwoA1Parent;
        public GameObject sideTwoA2Parent;
        public GameObject sideTwoA3Parent;

        public bool WarpingIn = false;
        public bool WarpingAnimationOver = false;
        public GameObject SideOneTorpedoPrefab;
        public GameObject SideTwoTorpedoPrefab;
        public GameObject SideOneBeamPrefab;
        public GameObject SideTwoBeamPrefab;
        public AudioClip SideOneBeamFireClip;
        public AudioClip SideTwoBeamFireClip;
        public AudioClip SideOneTorpedoFireClip;
        public AudioClip SideTwoTorpedoFireClip;
        [Header("First Firing Delay Ranges")]
        [SerializeField] private float minFirstShotDelay = 0.2f;
        [SerializeField] private float maxFirstShotDelay = 0.9f;
        private List<ShipController> _hvyCruisersSide1 = new List<ShipController>();
        private List<ShipController> _hvyCruisersSide2 = new List<ShipController>();
        private List<ShipController> _ltCruisersSide1 = new List<ShipController>();
        private List<ShipController> _ltCruisersSide2 = new List<ShipController>();
        private List<ShipController> _cruisersSide1 = new List<ShipController>();
        private List<ShipController> _cruisersSide2 = new List<ShipController>();
        private List<ShipController> _destroyersSide1List = new List<ShipController>();
        private List<ShipController> _destroyersSide2List = new List<ShipController>();
        private List<ShipController> _scoutsSide1List = new List<ShipController>();
        private List<ShipController> _scoutsSide2List = new List<ShipController>();
        private List<ShipController> _transportsSide1List = new List<ShipController>();
        private List<ShipController> _transportsSide2List = new List<ShipController>();
        List<Vector2Int> _spiralPositionsTran1 = new List<Vector2Int>();
        List<Vector2Int> _spiralPositionsTran2 = new List<Vector2Int>();
        List<Vector2Int> _spiralPositionsOtherShipsSide1 = new List<Vector2Int>();
        List<Vector2Int> _spiralPositionsOtherShipsSide2 = new List<Vector2Int>();
        public List<GameObject> HealthbarRenderers { get; private set; } = new List<GameObject>();
        [Header("Move parent GameObjects to move ships")]
        public float initialSpeed = 30f;     // starting velocity (units/sec)
        public float stopDistance;    // distance over which to stop
        private float deceleration;         // computed deceleration
        private float currentSpeed;
        private readonly List<Vector3> moveDirections = new List<Vector3>();
        public bool isMoving = false;
        public bool isClosing = false;
        private bool combatEnded = false;
        private bool showingEndPanel = false;

        [Header("Warp-In Animation")]
        public float warpInDuration = 1.5f;  // Duration of position movement (until stretch starts)
        public float warpStretchDuration = 0.8f;  // Duration of scale-back animation
        private Vector3[] parentStartPositions;  // Starting positions for warp-in
        private Vector3[] parentFinalPositions;  // Final positions after warp-in
        private int _transportsSide1;
        private int _transportsSide2;
        private object _scoutsSide1;
        private object _scoutsSide2;
        private object _destroyersSide1;
        private object _destroyersSide2;
        private int _capitalsSide1;
        private int _capitalsSide2;

        private void Awake()
        {
            CombatID = GetInstanceID(); // Unity object id for this combat instance
            Debug.Log($"✅ CombatController {CombatID}: Created");

            // ✅ Initialize ship parents list
            // This list is used by BeginPhysicsLikeMovement() and LateUpdate()
            if (shipParents == null)
            {
                shipParents = new List<GameObject>();
            }
        }
        private void Start()
        {
            minFirstShotDelay = 0.2f;
            maxFirstShotDelay = 0.9f;
            currentSpeed = 30f;
            stopDistance = 390f;
            CleanupOrphanedProjectiles();

            // ✅ Populate ship parents list with the 6 parent references
            // Order matters: [0-2] = Side One, [3-5] = Side Two
            shipParents.Clear();

            if (sideOneA1Parent != null) shipParents.Add(sideOneA1Parent);
            if (sideOneA2Parent != null) shipParents.Add(sideOneA2Parent);
            if (sideOneA3Parent != null) shipParents.Add(sideOneA3Parent);
            if (sideTwoA1Parent != null) shipParents.Add(sideTwoA1Parent);
            if (sideTwoA2Parent != null) shipParents.Add(sideTwoA2Parent);
            if (sideTwoA3Parent != null) shipParents.Add(sideTwoA3Parent);

            Debug.Log($"✅ Populated shipParents list with {shipParents.Count} parent GameObjects");

            // ✅ Store starting and final positions for warp-in animation
            parentStartPositions = new Vector3[shipParents.Count];
            parentFinalPositions = new Vector3[shipParents.Count];

            for (int i = 0; i < shipParents.Count; i++)
            {
                if (shipParents[i] != null)
                {
                    // Store the current scene position as final position
                    parentFinalPositions[i] = shipParents[i].transform.position;

                    // Calculate start position (farther out for warp-in effect)
                    // Use parent's local right direction (local +X) to move along the correct axis
                    // even if parent is rotated in Unity scene
                    bool isSideOne = (i < 3);
                    float startOffset = isSideOne ? -600f : 600f;  // Extra distance for warp-in

                    // ✅ Move along parent's local X axis (which may be rotated in world space)
                    Vector3 moveDirection = shipParents[i].transform.right; // Parent's local +X direction
                    parentStartPositions[i] = parentFinalPositions[i] + (moveDirection * startOffset);

                    Debug.Log($"✅ Parent '{shipParents[i].gameObject.name}': Start={parentStartPositions[i]}, Final={parentFinalPositions[i]}, MoveDir={moveDirection}");
                }
            }

        }
        void LateUpdate()
        {

            if (WarpingAnimationOver && !WarpingIn)
            {
                // ✅ Initialize ship groups once after warp-in completes
                if (!groupsInitialized)
                {
                    InitializeShipGroupsForEngage();
                }

                // ✅ NEW: Order-based movement system
                if (isMoving && !combatEnded)
                {
                    // Process each parent group (6 total: 3 per side)
                    for (int i = 0; i < shipParents.Count; i++)
                    {
                        var numChildren = shipParents[i].transform.childCount;

                        for (int j = 0; j < numChildren; j++)
                        {
                            var child = shipParents[i].transform.GetChild(j);

                            if (child != null && child.TryGetComponent<ShipController>(out var shipController))
                            {
                                if (shipController.ShipData != null &&
                                    !shipController.ShipData.Distroyed &&
                                    shipController.ShipData.ShieldHealth + shipController.ShipData.HullHealth > 0)
                                {
                                    // ✅ Get order state machine
                                    var orderStateMachine = child.GetComponent<CombatOrderStateMachine>();

                                    if (orderStateMachine != null && !orderStateMachine.IsWarpingOut())
                                    {
                                        // ✅ Use ship's max warp factor and order speed factor
                                        float shipMaxSpeed = shipController.ShipData.maxWarpFactor;
                                        float orderSpeedFactor = orderStateMachine.GetOrderSpeedFactor();
                                        float effectiveSpeed = shipMaxSpeed * orderSpeedFactor;

                                        // ✅ Calculate movement
                                        float step = effectiveSpeed * Time.unscaledDeltaTime;

                                        // ✅ Get movement direction (forward toward enemy)
                                        Vector3 moveDirection = moveDirections[i];

                                        // ✅ Apply movement
                                        child.transform.Translate(moveDirection * step, Space.Self);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void Update()
        {
            // ✅ Update group targets for Engage order
            if (WarpingAnimationOver && !combatEnded)
            {
                if (CombatData.SideOneOrder == CombatOrders.Engage)
                {
                    UpdateGroupTargets();
                }
            }
            if (!combatEnded && WarpingAnimationOver && !WarpingIn)
            {
                int sideOneAlive = CombatData.SideOneShipCons.Count(s => s != null && s.ShipData != null && !s.ShipData.Distroyed && s.ShipData.ShieldHealth + s.ShipData.HullHealth > 0);
                int sideTwoAlive = CombatData.SideTwoShipCons.Count(s => s != null && s.ShipData != null && !s.ShipData.Distroyed && s.ShipData.ShieldHealth + s.ShipData.HullHealth > 0);

                if (sideOneAlive == 0 || sideTwoAlive == 0)
                {
                    Debug.Log($"🏁 Combat ended! Side 1: {sideOneAlive} ships, Side 2: {sideTwoAlive} ships");
                    combatEnded = true;
                    StopAllWeaponFire();

                    if (!showingEndPanel)
                    {
                        showingEndPanel = true;
                        StartCoroutine(ShowCombatEndSequence(sideOneAlive > 0));
                    }
                }
            }
        }

        // Add this helper method to stop all firing
        private void StopAllWeaponFire()
        {
            // Nullify all targets to stop firing loops immediately
            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship != null && ship.ShipData != null)
                {
                    ship.ShipData.TargetThisShipController = null;
                }
            }
            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship != null && ship.ShipData != null)
                {
                    ship.ShipData.TargetThisShipController = null;
                }
            }
            Debug.Log("🛑 All weapon fire stopped");
        }

        // Add this coroutine for the end sequence with proper delays
        /// <summary>
        /// Manually animates the warp-in effect with position movement and warp stretch
        /// Phase 1: Move parents from start to final positions while ships are stretched 5x on X axis
        /// Phase 2: Scale ships back to normal over 0.8 seconds
        /// </summary>
        public IEnumerator AnimateWarpIn()
        {
            Debug.Log("=== Starting Manual Warp-In Animation ===");
            AudioManager.Instance?.PlaySoundData(warpInSound);

            // ✅ PHASE 1 SETUP: Set parents to start positions and stretch all child ships
            List<List<Transform>> allChildShips = new List<List<Transform>>();

            for (int i = 0; i < shipParents.Count; i++)
            {
                if (shipParents[i] != null)
                {
                    // Set parent to start position
                    shipParents[i].transform.position = parentStartPositions[i];
                    Debug.Log($"  Set {shipParents[i].gameObject.name} to start position: {parentStartPositions[i]}");

                    // Collect and stretch all child ships
                    List<Transform> childShips = new List<Transform>();
                    for (int j = 0; j < shipParents[i].transform.childCount; j++)
                    {
                        Transform child = shipParents[i].transform.GetChild(j);
                        if (child.TryGetComponent<ShipController>(out _))
                        {
                            // ✅ Stretch ship 5x on world X axis
                            // Side One (i < 3): X scale = 5
                            // Side Two (i >= 3): X scale = 5
                            child.localScale = new Vector3(100f, 1f, 1f);
                            childShips.Add(child);
                        }
                    }
                    allChildShips.Add(childShips);
                    Debug.Log($"    Stretched {childShips.Count} ships to 5x on X axis");
                }
                else
                {
                    allChildShips.Add(new List<Transform>());
                }
            }

            // ✅ PHASE 1: Animate parent positions from start to final (ships stay stretched)
            float elapsed = 0f;
            while (elapsed < warpInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / warpInDuration);

                // Use ease-out curve for smooth deceleration
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);

                for (int i = 0; i < shipParents.Count; i++)
                {
                    if (shipParents[i] != null)
                    {
                        shipParents[i].transform.position = Vector3.Lerp(
                            parentStartPositions[i],
                            parentFinalPositions[i],
                            smoothT
                        );
                    }
                }

                yield return null;
            }

            // Ensure final positions are exact
            for (int i = 0; i < shipParents.Count; i++)
            {
                if (shipParents[i] != null)
                {
                    shipParents[i].transform.position = parentFinalPositions[i];
                }
            }

            Debug.Log("✅ Phase 1 complete: Parents at final position, ships still stretched");

            // ✅ PHASE 2: Scale ships back from 5x to 1x on X axis over warpStretchDuration
            elapsed = 0f;
            while (elapsed < warpStretchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / warpStretchDuration);

                // Ease-in curve for smooth scale-back
                float smoothT = Mathf.Pow(t, 2f);

                // Lerp scale from 5 to 1
                float currentXScale = Mathf.Lerp(5f, 1f, smoothT);

                for (int i = 0; i < allChildShips.Count; i++)
                {
                    foreach (Transform ship in allChildShips[i])
                    {
                        if (ship != null)
                        {
                            ship.localScale = new Vector3(currentXScale, 1f, 1f);
                        }
                    }
                }

                yield return null;
            }

            // Ensure final scale is exact
            for (int i = 0; i < allChildShips.Count; i++)
            {
                foreach (Transform ship in allChildShips[i])
                {
                    if (ship != null)
                    {
                        ship.localScale = Vector3.one;
                    }
                }
            }

            Debug.Log("✅ Phase 2 complete: Ships scaled back to normal");
            Debug.Log("✅ Warp-in animation complete");

            // Signal that warp-in is complete
            WarpingIn = false;
            WarpingAnimationOver = true;
        }

        private IEnumerator ShowCombatEndSequence(bool sideOneWon)
        {
            // ✅ PHASE 1: Stop all movement and weapon fire
            Debug.Log("Combat End Phase 1: Stopping movement and weapons");
            isMoving = false; // ✅ Stop ship movement immediately

            // ✅ PHASE 2: Wait for last projectiles to hit
            // (Ships are already destroyed via TakeDamage when hull <= 0)
            yield return new WaitForSecondsRealtime(1f);

            // ✅ PHASE 3: Show the combat over panel (destroyed ships already gone!)
            Debug.Log("Combat End Phase 2: Showing victory panel");
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.ShowCombatOverPanel();

                // Determine winner
                CivEnum winner = sideOneWon ? CombatData.CivEnumSideOne : CombatData.CivEnumSideTwo;
                CivEnum loser = sideOneWon ? CombatData.CivEnumSideTwo : CombatData.CivEnumSideOne;

                Debug.Log($"🏆 Victory for: {winner}");
                Debug.Log($"💀 Defeated: {loser}");

                // TODO: Update panel text with winner/loser names
            }

            // ✅ PHASE 4: Wait for player to view results
            yield return new WaitForSecondsRealtime(5f);

            // ✅ PHASE 5: Clean up and return to galaxy
            Debug.Log("Combat End Phase 3: Returning to galaxy");
            EndCombat();
        }
        /// <summary>
        /// Initialize ship groups for Engage order.
        /// Groups ships by similar speed into 2-3 ship groups.
        /// Called after ships warp in and before combat starts.
        /// </summary>
        public void InitializeShipGroupsForEngage()
        {
            if (groupsInitialized) return;

            Debug.Log("=== Initializing Ship Groups for Engage Order ===");

            // Side One Groups
            if (CombatData.SideOneOrder == CombatOrders.Engage)
            {
                sideOneGroups = CreateShipGroups(CombatData.SideOneShipCons);
                AssignGroupsToShips(sideOneGroups);
                Debug.Log($"  Side 1: Created {sideOneGroups.Count} groups");
            }

            // Side Two Groups
            if (CombatData.SideTwoOrder == CombatOrders.Engage)
            {
                sideTwoGroups = CreateShipGroups(CombatData.SideTwoShipCons);
                AssignGroupsToShips(sideTwoGroups);
                Debug.Log($"  Side 2: Created {sideTwoGroups.Count} groups");
            }

            groupsInitialized = true;
        }
        /// <summary>
        /// Create groups of 2-3 ships with similar speeds
        /// </summary>
        private List<ShipGroup> CreateShipGroups(List<ShipController> ships)
        {
            List<ShipGroup> groups = new List<ShipGroup>();

            // Filter out transports and destroyed ships
            var combatShips = ships.Where(s =>
                s != null &&
                !s.ShipData.Distroyed &&
                s.ShipData.ShipType != ShipType.Transport
            ).ToList();

            if (combatShips.Count == 0) return groups;

            // Sort by speed (maxWarpFactor)
            combatShips = combatShips.OrderBy(s => s.ShipData.maxWarpFactor).ToList();

            // Create groups of 2-3 ships
            for (int i = 0; i < combatShips.Count; i += 3)
            {
                ShipGroup group = new ShipGroup();

                // Add 2-3 ships to group
                int groupSize = Mathf.Min(3, combatShips.Count - i);
                for (int j = 0; j < groupSize; j++)
                {
                    if (i + j < combatShips.Count)
                    {
                        group.ships.Add(combatShips[i + j]);
                    }
                }

                // Calculate group speed (slowest ship)
                group.RecalculateGroupSpeed();

                groups.Add(group);

                Debug.Log($"    Group {groups.Count}: {group.ships.Count} ships, speed={group.groupSpeed:F1}");
            }

            return groups;
        }
        /// <summary>
        /// Assign groups to ships (set their CombatOrderStateMachine.assignedGroup)
        /// </summary>
        private void AssignGroupsToShips(List<ShipGroup> groups)
        {
            int groupId = 0;
            foreach (var group in groups)
            {
                foreach (var ship in group.ships)
                {
                    var orderStateMachine = ship.GetComponent<CombatOrderStateMachine>();
                    if (orderStateMachine != null)
                    {
                        orderStateMachine.assignedGroup = group;
                        orderStateMachine.groupId = groupId;
                    }
                }
                groupId++;
            }
        }
        /// <summary>
        /// Update group targets for Engage order
        /// Each group focuses fire on a common target
        /// </summary>
        private void UpdateGroupTargets()
        {
            // Update Side One groups
            foreach (var group in sideOneGroups)
            {
                if (group.ships.Count == 0) continue;

                // Find common target (closest enemy to group center)
                Vector3 groupCenter = Vector3.zero;
                int validShips = 0;

                foreach (var ship in group.ships)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        groupCenter += ship.transform.position;
                        validShips++;
                    }
                }

                if (validShips > 0)
                {
                    groupCenter /= validShips;

                    // Find closest enemy to group center
                    ShipController closestEnemy = FindClosestEnemyToPosition(groupCenter, CombatData.SideTwoShipCons);
                    group.commonTarget = closestEnemy;

                    // Assign to all ships in group
                    foreach (var ship in group.ships)
                    {
                        if (ship != null && !ship.ShipData.Distroyed)
                        {
                            ship.ShipData.TargetThisShipController = closestEnemy;
                        }
                    }
                }
            }

            // Update Side Two groups
            foreach (var group in sideTwoGroups)
            {
                if (group.ships.Count == 0) continue;

                Vector3 groupCenter = Vector3.zero;
                int validShips = 0;

                foreach (var ship in group.ships)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        groupCenter += ship.transform.position;
                        validShips++;
                    }
                }

                if (validShips > 0)
                {
                    groupCenter /= validShips;

                    ShipController closestEnemy = FindClosestEnemyToPosition(groupCenter, CombatData.SideOneShipCons);
                    group.commonTarget = closestEnemy;

                    foreach (var ship in group.ships)
                    {
                        if (ship != null && !ship.ShipData.Distroyed)
                        {
                            ship.ShipData.TargetThisShipController = closestEnemy;
                        }
                    }
                }
            }
        }
        private ShipController FindClosestEnemyToPosition(Vector3 position, List<ShipController> enemies)
        {
            ShipController closest = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.ShipData.Distroyed || enemy.ShipData.ShipType == ShipType.Transport)
                    continue;

                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = enemy;
                }
            }

            return closest;
        }
        public void OnReturnToGalaxyButtonClicked()
        {
            Debug.Log("Player clicked return to galaxy");
            EndCombat();
        }
        /// <summary>
        /// Destroys any torpedoes/beams left in the scene from previous combat
        /// </summary>
        private void CleanupOrphanedProjectiles()
        {
            var torpedoes = FindObjectsByType<Torpedo>(FindObjectsSortMode.None);
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

            for (int i = 0; i < shipParents.Count; i++)
            {
                Vector3 dir; //= Vector3.zero;
                if (shipParents[i].transform.childCount > 0)
                {
                    // ✅ Base direction (towards enemy)
                    // Side One (at negative X) moves toward positive X (+right)
                    // Side Two (at positive X) moves toward negative X (-right)
                    bool isSideOne = (i < 3);
                    dir = isSideOne ? shipParents[i].transform.GetChild(0).transform.right.normalized
                                    : -shipParents[i].transform.GetChild(0).transform.right.normalized;

                    // ✅ Modify direction based on combat order
                    var ships = isSideOne ? CombatData.SideOneShipCons : CombatData.SideTwoShipCons;
                    CombatOrders order = isSideOne ? CombatData.OrderSideOne : CombatData.OrderSideTwo;

                    // Apply order-based movement modifiers
                    switch (order)
                    {
                        case CombatOrders.Retreat:
                            // Reverse direction (run away)
                            dir = -dir;
                            Debug.Log($"  Parent {i}: RETREAT - reversing direction");
                            break;

                        case CombatOrders.Formation:
                            // Slight spread pattern (maintain defensive formation)
                            float spreadAngle = (i % 3 - 1) * 15f; // -15°, 0°, +15°
                            dir = Quaternion.Euler(0, spreadAngle, 0) * dir;
                            Debug.Log($"  Parent {i}: FORMATION - spread {spreadAngle}°");
                            break;

                        case CombatOrders.Rush:
                            // Direct aggressive approach (no modification needed, just faster)
                            Debug.Log($"  Parent {i}: RUSH - full speed ahead");
                            break;

                        case CombatOrders.AttackTransports:
                        case CombatOrders.Engage:
                        default:
                            // Standard approach
                            break;
                    }
                }
                else
                {
                    dir = Vector3.zero;
                }

                moveDirections.Add(dir.normalized);
            }

            // ✅ Apply speed multipliers based on orders
            float sideOneSpeedMult = 1f;
            float sideTwoSpeedMult = 1f;

            // Apply base speed with order modifiers
            // (We'll apply this in LateUpdate based on which parent group)
            CombatData.SideOneSpeedMultiplier = sideOneSpeedMult;
            CombatData.SideTwoSpeedMultiplier = sideTwoSpeedMult;

            deceleration = (initialSpeed * initialSpeed) / (2f * stopDistance);
            currentSpeed = initialSpeed;
            isMoving = true;

            Debug.Log($"📊 Movement started: Side1 speed={sideOneSpeedMult:F2}x, Side2 speed={sideTwoSpeedMult:F2}x");
        }

        // Replace the SetShipOrders method (around line 604)
        public void SetShipOrders(CombatOrders order, CivEnum civEnum)
        {
            if (civEnum == CombatData.CivEnumSideOne)
            {
                CombatData.SideOneOrder = order;
                Debug.Log($"Side One order set to: {order}");
            }
            else if (civEnum == CombatData.CivEnumSideTwo)
            {
                CombatData.SideTwoOrder = order;
                Debug.Log($"Side Two order set to: {order}");
            }

            // ✅ Log order summary (no artificial advantage calculation)
            if (CombatData.SideOneOrder != CombatOrders.None && CombatData.SideTwoOrder != CombatOrders.None)
            {
                string summary = CombatOrderHelper.GetOrderSummary(CombatData.SideOneOrder, CombatData.SideTwoOrder);
                Debug.Log($"📊 Combat Orders: {summary}");
                Debug.Log($"   Side 1: {CombatOrderHelper.GetOrderDescription(CombatData.SideOneOrder)}");
                Debug.Log($"   Side 2: {CombatOrderHelper.GetOrderDescription(CombatData.SideTwoOrder)}");
            }
        }

        // Replace the SetAIRandomOrder method (around line 623)
        public void SetAIRandomOrder(CivEnum aiCivEnum)
        {
            int side = 0;

            if (aiCivEnum == CombatData.CivEnumSideOne)
            {
                side = 1;
            }
            else if (aiCivEnum == CombatData.CivEnumSideTwo)
            {
                side = 2;
            }

            // Build list of available orders
            var availableOrders = new System.Collections.Generic.List<CombatOrders>
    {
        CombatOrders.Engage,
        CombatOrders.Formation,
        CombatOrders.Rush,
        CombatOrders.Retreat
    };

            // ✅ Only add AttackTransports if enemy has transports
            bool enemyHasTransports = CombatOrderHelper.HasTransports(CombatData, side == 1 ? 2 : 1);
            if (enemyHasTransports)
            {
                availableOrders.Add(CombatOrders.AttackTransports);
                Debug.Log($"🎯 Enemy has transports - AttackTransports order available for AI");
            }

            // Pick random order
            CombatOrders randomOrder = availableOrders[UnityEngine.Random.Range(0, availableOrders.Count)];

            Debug.Log($"🤖 AI ({aiCivEnum}) selected order: {randomOrder}");

            SetShipOrders(randomOrder, aiCivEnum);
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

            if (CombatData.SideOneShipCons != null)
            {
                for (int i = CombatData.SideOneShipCons.Count - 1; i >= 0; i--)
                {
                    var ship = CombatData.SideOneShipCons[i];

                    // ✅ Skip if ship is null (already destroyed during combat)
                    if (ship == null || ship.gameObject == null)
                    {
                        Debug.Log($"  ⚠️ Ship at index {i} is null (destroyed during combat)");
                        continue;
                    }

                    if (ship.ShipData != null)
                    {
                        // Remove combat-only components
                        for (int j = ship.transform.childCount - 1; j >= 0; j--)
                        {
                            var child = ship.transform.GetChild(j);
                            if (child.name != "TargetPrefab(Clone)")
                            {
                                Destroy(child.gameObject);
                            }
                        }

                        // ✅ Ship survived - prepare for return to galaxy
                        if (ship.ShipData.CurrentFleetController != null)
                        {
                            Debug.Log($"  ✅ Ship '{ship.name}' survived - returning to fleet");

                            // ✅ CRITICAL: Remove combat-specific children before reparenting
                            List<Transform> childrenToDestroy = new List<Transform>();

                            foreach (Transform child in ship.transform)
                            {
                                // Destroy FBX model and combat additions
                                if (child.name.Contains("_Model") ||
                                    child.name.Contains("Healthbar") ||
                                    child.name.Contains("Health") ||
                                    child.name.Contains("Beam") ||
                                    child.name.Contains("Torpedo"))
                                {
                                    childrenToDestroy.Add(child);
                                }
                            }

                            foreach (var child in childrenToDestroy)
                            {
                                Debug.Log($"    🗑️ Destroying combat child: {child.name}");
                                Destroy(child.gameObject);
                            }

                            // ✅ Re-parent to fleet GameObject (must be in GalaxyScene)
                            ship.transform.SetParent(ship.ShipData.CurrentFleetController.transform, false);

                            // ✅ Reset position/rotation relative to fleet
                            ship.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                            // ✅ Reset scale
                            ship.transform.localScale = Vector3.one;

                            // ✅ Deactivate ship in galaxy (fleets keep ships inactive)
                            ship.gameObject.SetActive(false);

                            Debug.Log($"    ✅ Ship cleaned and returned to fleet '{ship.ShipData.CurrentFleetController.name}'");
                        }
                        else
                        {
                            Debug.LogError($"  ❌ Ship '{ship.name}' has no fleet reference - will be destroyed with scene!");
                        }
                    }
                }
            }

            // ✅ STEP 2: Get all fleets involved in combat
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

            // ✅ STEP 3: Destroy empty fleets and clean up fleet references
            foreach (var fleet in allCombatFleets)
            {
                if (fleet == null)
                {
                    Debug.LogWarning("  ⚠️ Null fleet reference in combat cleanup");
                    continue;
                }

                int shipCount = fleet.FleetData?.ShipsList?.Count ?? 0;
                Debug.Log($"  🚢 Fleet '{fleet.name}' ({fleet.FleetData?.CivEnum}): {shipCount} ships remaining");

                if (shipCount == 0)
                {
                    Debug.LogWarning($"  💀💀💀 Fleet '{fleet.name}' has NO SHIPS - DESTROYING FLEET 💀💀💀");

                    // ✅ CRITICAL: Use DestroyImmediate to ensure fleet is gone from GalaxyScene
                    if (FleetManager.Instance != null)
                    {
                        // Call the manager's destroy method first (cleans up references)
                        FleetManager.Instance.DestroyFleetController(fleet);
                        Debug.Log($"    ✅ Called FleetManager.DestroyFleetController()");
                    }

                    // ✅ FORCE immediate destruction of fleet GameObject
                    if (fleet != null && fleet.gameObject != null)
                    {
                        Debug.LogWarning($"    🗑️🗑️ FORCE DESTROYING fleet GameObject: {fleet.gameObject.name}");

                        // Destroy UI elements first
                        if (fleet.FleetUIGameObject != null)
                        {
                            DestroyImmediate(fleet.FleetUIGameObject);
                        }

                        // Destroy drop line
                        if (fleet.DropLine != null && fleet.DropLine.gameObject != null)
                        {
                            DestroyImmediate(fleet.DropLine.gameObject);
                        }

                        // Destroy the fleet GameObject itself
                        DestroyImmediate(fleet.gameObject);

                        Debug.LogWarning($"    ✅✅ Fleet '{fleet.name}' GameObject DESTROYED from GalaxyScene");
                    }
                }
                else
                {
                    Debug.Log($"  ✅ Fleet '{fleet.name}' survived with {shipCount} ships");

                    // ✅ Update fleet's max warp factor based on remaining ships
                    if (fleet != null)
                    {
                        fleet.UpdateMaxWarp();
                    }
                }
            }

            // ✅ STEP 4: Clear temp fog revealer
            if (FleetManager.Instance != null && FleetManager.Instance.TempFogRevealerFleet != null)
            {
                FleetManager.Instance.TempFogRevealerFleet = null;
            }

            // ✅ STEP 5: Destroy CombatUICanvas
            if (ShipCombatCanvas != null)
            {
                Destroy(ShipCombatCanvas.gameObject);
                Debug.Log("  Destroyed CombatUICanvas");
            }

            // ✅ STEP 6: Destroy all health bars
            foreach (var hb in HealthbarRenderers)
            {
                if (hb != null) Destroy(hb);
            }
            HealthbarRenderers.Clear();

            // ✅ STEP 7: Clean up UI references
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.CleanupCombat();
            }

            // ✅ STEP 8: EventSystem cleanup no longer needed - persistent EventSystem stays active

            Debug.Log("=== EndCombat: Cleanup complete ===");

            // ✅ STEP 9: Unload combat scene
            Scene combatScene = SceneManager.GetSceneByName("CombatScene");

            SceneController.Instance.UnloadCombatScene();
            SceneController.Instance.ReturnToGalaxyFromCombat();

            // ✅ STEP 10: Re-enable galaxy camera
            if (GalaxyCameraDragMoveZoom.Instance != null)
            {
                if (GalaxyCameraDragMoveZoom.Instance.TryGetComponent<Camera>(out var galaxyCam))
                {
                    galaxyCam.enabled = true;
                    Debug.Log($"  Galaxy camera enabled: {galaxyCam.enabled}");
                }
                GalaxyCameraDragMoveZoom.Instance.EnableCameraControl();
            }

            // ✅ STEP 11: Hide star system UI when returning from combat
            if (StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.MoveBackAnyStarSysUIGO();
                StarSysMenuUIController.Instance.HideA_SystemMenuView();
            }

            // ✅ STEP 12: Resume time
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.ResumeTime();
                Debug.Log("  Resumed time");
                CombatManager.Instance.OnCombatEnded(this);
            }
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

                // ✅ Debug: Log which civilizations are on which side
                Debug.Log($"📊 SIDE ASSIGNMENT:");
                Debug.Log($"   Side 1 ({CombatData.CivEnumSideOne}): {sideOneShips.Count} ships → S1A1/S1A2/S1A3 animators (LEFT, X=-1000)");
                Debug.Log($"   Side 2 ({CombatData.CivEnumSideTwo}): {sideTwoShips.Count} ships → S2A1/S2A2/S2A3 animators (RIGHT, X=+1000)");

                PopulateShipGOAndAnimation(sideOneShips, -1); //sideOne is on the left, ships are -x axis world space attached to an animator...
                PopulateShipGOAndAnimation(sideTwoShips, 1);
            }
        }

        private void SetLocalOtherShipPosition(GameObject shipGameOb, int indexOther, List<Vector2Int> spiralPositions)
        {
            // Map spiral to Y (vertical) and Z (depth) to create a wall formation
            shipGameOb.transform.localPosition = new Vector3(
                0,  // X stays at 0 - side offset handled by parent animator
                spiralPositions[indexOther].y * 100,  // Y = vertical position
                spiralPositions[indexOther].x * 100   // Z = depth position
            );
        }

        private void SetLocalCombatShipPosition(GameObject shipGameOb, int index, List<Vector2Int> spiralPositions)
        {
            // ✅ ONLY set spiral formation position - NO rotation changes
            // X = 0 (ship follows parent animator's world X position)
            // Y and Z = spiral pattern for wall formation
            shipGameOb.transform.localPosition = new Vector3(
                0,  // X stays at 0 - side offset handled by parent animator
                spiralPositions[index].y * 100,  // Y = vertical position (spiral's Y component)
                spiralPositions[index].x * 100   // Z = depth position (spiral's X component)
            );

            // ✅ NO rotation manipulation - ship inherits parent's rotation naturally
            Debug.Log($"    Combat ship '{shipGameOb.name}' local pos: (0, {spiralPositions[index].y * 100}, {spiralPositions[index].x * 100})");
        }

        private void SetLocalTransportPosition(GameObject shipGameOb, int indexTrans, List<Vector2Int> spiralPositions)
        {
            // ✅ ONLY set spiral formation position - NO rotation changes
            // Transports positioned BEHIND the combat wall
            shipGameOb.transform.localPosition = new Vector3(
                0,                                    // ✅ Local X = 0 (animator handles world position)
                spiralPositions[indexTrans].y * 100,  // Y = vertical position
                spiralPositions[indexTrans].x * 100   // Z = depth position
            );

            // ✅ NO rotation manipulation - ship inherits parent's rotation naturally
            Debug.Log($"    Transport '{shipGameOb.name}' local pos: (0, {spiralPositions[indexTrans].y * 100}, {spiralPositions[indexTrans].x * 100})");
        }
        /// <summary>
        /// Count and categorize ships by type for both sides
        /// </summary>
        private void CountShips()
        {
            // Clear existing lists
            _hvyCruisersSide1.Clear();
            _hvyCruisersSide2.Clear();
            _ltCruisersSide1.Clear();
            _ltCruisersSide2.Clear();
            _cruisersSide1.Clear();
            _cruisersSide2.Clear();
            _destroyersSide1List.Clear();
            _destroyersSide2List.Clear();
            _scoutsSide1List.Clear();
            _scoutsSide2List.Clear();
            _transportsSide1List.Clear();
            _transportsSide2List.Clear();

            // Reset counters
            _transportsSide1 = 0;
            _transportsSide2 = 0;
            _capitalsSide1 = 0;
            _capitalsSide2 = 0;

            // Count Side One ships
            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship == null || ship.ShipData == null) continue;

                switch (ship.ShipData.ShipType)
                {
                    case ShipType.HvyCruiser:
                        _hvyCruisersSide1.Add(ship);
                        _capitalsSide1++;
                        break;
                    case ShipType.LtCruiser:
                        _ltCruisersSide1.Add(ship);
                        _capitalsSide1++;
                        break;
                    case ShipType.Cruiser:
                        _cruisersSide1.Add(ship);
                        _capitalsSide1++;
                        break;
                    case ShipType.Destroyer:
                        _destroyersSide1List.Add(ship);
                        break;
                    case ShipType.Scout:
                        _scoutsSide1List.Add(ship);
                        break;
                    case ShipType.Transport:
                        _transportsSide1List.Add(ship);
                        _transportsSide1++;
                        break;
                }
            }

            // Count Side Two ships
            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship == null || ship.ShipData == null) continue;

                switch (ship.ShipData.ShipType)
                {
                    case ShipType.HvyCruiser:
                        _hvyCruisersSide2.Add(ship);
                        _capitalsSide2++;
                        break;
                    case ShipType.LtCruiser:
                        _ltCruisersSide2.Add(ship);
                        _capitalsSide2++;
                        break;
                    case ShipType.Cruiser:
                        _cruisersSide2.Add(ship);
                        _capitalsSide2++;
                        break;
                    case ShipType.Destroyer:
                        _destroyersSide2List.Add(ship);
                        break;
                    case ShipType.Scout:
                        _scoutsSide2List.Add(ship);
                        break;
                    case ShipType.Transport:
                        _transportsSide2List.Add(ship);
                        _transportsSide2++;
                        break;
                }
            }

            Debug.Log($"Ship count - Side 1: Capitals={_capitalsSide1}, Destroyers={_destroyersSide1List.Count}, Scouts={_scoutsSide1List.Count}, Transports={_transportsSide1}");
            Debug.Log($"Ship count - Side 2: Capitals={_capitalsSide2}, Destroyers={_destroyersSide2List.Count}, Scouts={_scoutsSide2List.Count}, Transports={_transportsSide2}");
        }

        public CivController SideOneCivCombatants()
        {
            return CombatData.sideOneCiv;
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
        private ShipSO GetShipSOForShip(ShipController shipCon)
        {
            List<ShipSO> daList = ShipManager.Instance.FedShipSOList;
            {
                CivEnum daCiv = shipCon.ShipData.CivEnum;
                switch (daCiv)
                {
                    case CivEnum.FED:
                        daList = ShipManager.Instance.FedShipSOList;
                        break;
                    case CivEnum.KLING:
                        daList = ShipManager.Instance.KlingShipSOList;
                        break;
                    case CivEnum.ROM:
                        daList = ShipManager.Instance.RomShipSOList;
                        break;
                    case CivEnum.CARD:
                        daList = ShipManager.Instance.CardShipSOList;
                        break;
                    case CivEnum.DOM:
                        daList = ShipManager.Instance.DomShipSOList;
                        break;
                    case CivEnum.BORG:
                        daList = ShipManager.Instance.BorgShipSOList;
                        break;
                    case CivEnum.TERRAN:
                        daList = ShipManager.Instance.TerranShipSOList;
                        break;
                    default:
                        daList = ShipManager.Instance.FedShipSOList; break;
                }
                for (int j = 0; j < daList.Count; j++)
                {
                    if (daList[j].ShipName == shipCon.ShipData.ShipName)
                    {
                        return daList[j];
                    }
                }
            }
            return ShipManager.Instance.FedShipSOList.FirstOrDefault();
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

                Debug.Log($"✅ Canvas configured: RenderMode={ShipCombatCanvas.renderMode}, Camera={(ShipCombatCanvas.worldCamera != null ? ShipCombatCanvas.worldCamera.name : "NULL")}");
            }
            else
            {
                Debug.LogError("❌ ShipCombatCanvas is NULL!");
                return;
            }

            // ✅ NEW: Generate formation positions for combat ships (by type priority)
            List<ShipController> combatShipsSide1 = new List<ShipController>();
            List<ShipController> combatShipsSide2 = new List<ShipController>();

            // ✅ Build ordered combat ship lists (HvyCruiser → LtCruiser → Cruiser → Destroyer → Scout)
            if (side1negSide2pos < 0) // Side 1
            {
                combatShipsSide1.AddRange(_hvyCruisersSide1);
                combatShipsSide1.AddRange(_ltCruisersSide1);
                combatShipsSide1.AddRange(_cruisersSide1);
                combatShipsSide1.AddRange(_destroyersSide1List);
                combatShipsSide1.AddRange(_scoutsSide1List);
            }
            else // Side 2
            {
                combatShipsSide2.AddRange(_hvyCruisersSide2);
                combatShipsSide2.AddRange(_ltCruisersSide2);
                combatShipsSide2.AddRange(_cruisersSide2);
                combatShipsSide2.AddRange(_destroyersSide2List);
                combatShipsSide2.AddRange(_scoutsSide2List);
            }

            // Generate spiral positions for combat ships (the "wall")
            List<Vector2Int> combatPositionsSide1 = combatShipsSide1.Count > 0 ? GenerateSpiralPositions(combatShipsSide1.Count) : new List<Vector2Int>();
            List<Vector2Int> combatPositionsSide2 = combatShipsSide2.Count > 0 ? GenerateSpiralPositions(combatShipsSide2.Count) : new List<Vector2Int>();

            // Generate spiral positions for transports (behind the wall)
            _spiralPositionsTran1 = _transportsSide1 > 0 ? GenerateTransportSpiralPositions(_transportsSide1) : new List<Vector2Int>();
            _spiralPositionsTran2 = _transportsSide2 > 0 ? GenerateTransportSpiralPositions(_transportsSide2) : new List<Vector2Int>();

            int combatShipIndex1 = 0;
            int combatShipIndex2 = 0;
            int transportIndex1 = 0;
            int transportIndex2 = 0;
            int flipAnimation1 = -1;
            int flipAnimation2 = -1;

            for (int i = 0; i < shipConList.Count; i++)
            {
                shipConList[i].transform.localScale = Vector3.one;
                shipConList[i].name = shipConList[i].ShipData.ShipName;
                shipConList[i].gameObject.SetActive(true);

                GameObject shipGameOb = shipConList[i].gameObject;
                ShipType shipType = shipConList[i].ShipData.ShipType;

                // ✅ Parent ship to parent GameObject - SetParent with worldPositionStays=false ensures clean local transform
                if (shipType == ShipType.Transport)
                {
                    // Transports go behind the combat ship wall
                    if (side1negSide2pos < 0) // Side 1
                    {
                        if (transportIndex1 < _spiralPositionsTran1.Count)
                        {
                            sideOneA3Parent.gameObject.SetActive(true);
                            // ✅ SetParent with false = reset to local identity, then apply spiral position
                            shipGameOb.transform.SetParent(sideOneA3Parent.gameObject.transform, false);
                            //shipGameOb.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                            SetLocalTransportPosition(shipGameOb, transportIndex1, _spiralPositionsTran1);
                            transportIndex1++;
                        }
                    }
                    else // Side 2
                    {
                        if (transportIndex2 < _spiralPositionsTran2.Count)
                        {
                            sideTwoA3Parent.gameObject.SetActive(true);
                            shipGameOb.transform.SetParent(sideTwoA3Parent.gameObject.transform, false);
                            // shipGameOb.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                            SetLocalTransportPosition(shipGameOb, transportIndex2, _spiralPositionsTran2);
                            transportIndex2++;
                        }
                    }
                }
                else
                {
                    // Combat ships form the protective wall
                    if (side1negSide2pos < 0) // Side 1
                    {
                        if (combatShipIndex1 < combatPositionsSide1.Count)
                        {
                            // Alternate between parent 1 and 2 for variety
                            if (flipAnimation1 < 0)
                            {
                                sideOneA1Parent.gameObject.SetActive(true);
                                shipGameOb.transform.SetParent(sideOneA1Parent.gameObject.transform, false);
                                //shipGameOb.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                                SetLocalCombatShipPosition(shipGameOb, combatShipIndex1, combatPositionsSide1);
                                flipAnimation1 = 1;
                            }
                            else
                            {
                                sideOneA2Parent.gameObject.SetActive(true);
                                shipGameOb.transform.SetParent(sideOneA2Parent.gameObject.transform, false);
                                // shipGameOb.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                                SetLocalCombatShipPosition(shipGameOb, combatShipIndex1, combatPositionsSide1);
                                flipAnimation1 = -1;
                            }
                            combatShipIndex1++;
                        }
                    }
                    else // Side 2
                    {
                        if (combatShipIndex2 < combatPositionsSide2.Count)
                        {
                            if (flipAnimation2 < 0)
                            {
                                sideTwoA1Parent.gameObject.SetActive(true);
                                shipGameOb.transform.SetParent(sideTwoA1Parent.gameObject.transform, false);
                                // shipGameOb.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                                SetLocalCombatShipPosition(shipGameOb, combatShipIndex2, combatPositionsSide2);
                                flipAnimation2 = 1;
                            }
                            else
                            {
                                sideTwoA2Parent.gameObject.SetActive(true);
                                shipGameOb.transform.SetParent(sideTwoA2Parent.gameObject.transform, false);
                                // shipGameOb.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                                SetLocalCombatShipPosition(shipGameOb, combatShipIndex2, combatPositionsSide2);
                                flipAnimation2 = -1;
                            }
                            combatShipIndex2++;
                        }
                    }
                }

                // ✅ Continue with ship model setup
                GameObject fbx = GetShipSOForShip(shipConList[i]).ShipFBX_ModelAsGOPrefab;
                if (fbx == null)
                {
                    Debug.LogError($"❌ Ship prefab is null for {shipConList[i].ShipData.ShipName}!");
                    continue;
                }

                // ✅ Debug: Log rotation BEFORE instantiating model
                Debug.Log($"    Ship '{shipConList[i].ShipData.ShipName}' localRotation before model: {shipGameOb.transform.localRotation.eulerAngles}");

                // ✅ Instantiate ship model at ship's current position/rotation
                GameObject shipModel = Instantiate(fbx, shipGameOb.transform.position, shipGameOb.transform.rotation);
                shipModel.transform.SetParent(shipGameOb.transform, false);
                shipModel.transform.localPosition = Vector3.zero;
                shipModel.transform.localRotation = Quaternion.identity;

                // ✅ Debug: Log rotation AFTER instantiating model
                Debug.Log($"    Ship '{shipConList[i].ShipData.ShipName}' localRotation after model: {shipGameOb.transform.localRotation.eulerAngles}");

                // Disable stencil operations on ship renderers
                DisableStencilOnShipRenderers(shipModel);

                // Set ship layer
                shipGameOb.layer = LayerMask.NameToLayer("Default");
                SetLayerRecursively(shipGameOb, LayerMask.NameToLayer("Default"));

                // Add collider for weapon targeting
                BoxCollider boxCollider = shipGameOb.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = shipGameOb.AddComponent<BoxCollider>();
                }
                boxCollider.isTrigger = true;

                Renderer renderer = shipModel.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    float width, height, length;
                    Vector3 localCenter = fbx.transform.InverseTransformPoint(renderer.bounds.center);
                    Vector3 localSize = fbx.transform.InverseTransformVector(renderer.bounds.size);
                    boxCollider.center = new Vector3(localCenter.x, localCenter.z, localCenter.y);
                    width = Math.Abs(localSize.x);
                    height = Math.Abs(localSize.z);
                    length = Math.Abs(localSize.y);
                    boxCollider.size = new Vector3(width, height, length);
                }

                shipConList[i].SetWeaponPrefabs();

                // ✅ Set civilization-specific weapon fire audio clips based on side
                if (side1negSide2pos < 0) // Side One
                {
                    shipConList[i].SetWeaponAudioClips(SideOneBeamFireClip, SideOneTorpedoFireClip);
                }
                else // Side Two
                {
                    shipConList[i].SetWeaponAudioClips(SideTwoBeamFireClip, SideTwoTorpedoFireClip);
                }
            }

            Debug.Log($"✅ Formation complete - Side {(side1negSide2pos < 0 ? "1" : "2")}: {combatShipIndex1 + combatShipIndex2} combat ships, {transportIndex1 + transportIndex2} transports");
            Debug.Log($"   Parent GameObjects will move during warp-in. Ships have spiral formation only.");

            // ✅ DO NOT rotate parents - keep them at (0,0,0) so warp animation moves along world X axis
            // Ship rotation is handled in CombatUIManager.SetupAnimatorsForWarpIn()
        }

        /// <summary>
        /// Generate spiral positions for combat ships forming the protective wall
        /// </summary>
        private List<Vector2Int> GenerateSpiralPositions(int count)
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            if (count <= 0) return positions;

            // Start at center
            positions.Add(new Vector2Int(0, 0));

            if (count == 1) return positions;

            // Generate square spiral pattern
            int x = 0, y = 0;
            int dx = 0, dy = -1;
            int maxSteps = count;

            for (int i = 1; i < count; i++)
            {
                // Move to next position in spiral
                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    // Change direction
                    int temp = dx;
                    dx = -dy;
                    dy = temp;
                }

                x += dx;
                y += dy;
                positions.Add(new Vector2Int(x, y));
            }

            return positions;
        }

        /// <summary>
        /// Generate spiral positions for transports positioned behind combat ships
        /// </summary>
        private List<Vector2Int> GenerateTransportSpiralPositions(int count)
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            if (count <= 0) return positions;

            // Transports form a tighter formation since they're behind the protective wall
            // Start at center
            positions.Add(new Vector2Int(0, 0));

            if (count == 1) return positions;

            // Generate compact spiral pattern
            int x = 0, y = 0;
            int dx = 0, dy = -1;

            for (int i = 1; i < count; i++)
            {
                // Move to next position in spiral
                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    // Change direction
                    int temp = dx;
                    dx = -dy;
                    dy = temp;
                }

                x += dx;
                y += dy;
                positions.Add(new Vector2Int(x, y));
            }

            return positions;
        }
        /// <summary>
        /// Disables stencil buffer operations on ship renderers to prevent rendering conflicts
        /// </summary>
        private void DisableStencilOnShipRenderers(GameObject shipModel)
        {
            if (shipModel == null) return;

            Renderer[] renderers = shipModel.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    // Disable stencil operations on materials
                    renderer.material.SetInt("_StencilComp", 0);
                    renderer.material.SetInt("_Stencil", 0);
                    renderer.material.SetInt("_StencilOp", 0);
                    renderer.material.SetInt("_StencilWriteMask", 0);
                    renderer.material.SetInt("_StencilReadMask", 0);
                }
            }
        }
        /// <summary>
        /// ✅ NEW: Create health bars for all ships AFTER warp animation completes
        /// Called by CombatUIManager after warp-in finishes
        /// This prevents health bars from interfering with warp animation visuals
        /// </summary>
        public void CreateHealthBarsForAllShips()
        {
            Debug.Log("🏥 Creating health bars for all ships...");

            int healthbarCount = 0;

            // Create health bars for Side One ships
            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    CreateHealthBarForShip(ship, -1); // -1 for side one (left side)
                    healthbarCount++;
                }
            }

            // Create health bars for Side Two ships
            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    CreateHealthBarForShip(ship, 1); // 1 for side two (right side)
                    healthbarCount++;
                }
            }

            Debug.Log($"✅ Created {healthbarCount} health bars (visible immediately)");
        }

        /// <summary>
        /// ✅ NEW: Create a health bar for a single ship
        /// </summary>
        private void CreateHealthBarForShip(ShipController ship, int side1negSide2pos)
        {
            if (ship == null || CombatManager.Instance == null || CombatManager.Instance.HealthbarPrefab == null)
            {
                Debug.LogWarning($"Cannot create health bar - missing ship or prefab");
                return;
            }

            GameObject healthbarGO = Instantiate(CombatManager.Instance.HealthbarPrefab);
            healthbarGO.SetActive(true); // ✅ Active immediately (no warp animation to worry about)

            // ✅ Parent directly to ship (world-space UI)
            healthbarGO.transform.SetParent(ship.transform, false);
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
            healthbarCanvas.worldCamera = ShipCombatCameraController.Instance?.GetComponentInChildren<Camera>();

            // ✅ Add CanvasScaler for proper sizing
            if (!healthbarGO.TryGetComponent<CanvasScaler>(out var canvasScaler))
            {
                canvasScaler = healthbarGO.AddComponent<CanvasScaler>();
            }
            canvasScaler.dynamicPixelsPerUnit = 10;

            // ✅ Set health bar layer to Default (NOT UI layer for world-space)
            healthbarGO.layer = LayerMask.NameToLayer("Default");
            SetLayerRecursively(healthbarGO, LayerMask.NameToLayer("Default"));

            // ✅ Set up health bar images
            Image[] healthbarImages = healthbarGO.GetComponentsInChildren<Image>();
            foreach (var img in healthbarImages)
            {
                if (img.gameObject.name == "HealthFill")
                {
                    ship.HealthFillImage = img;
                    ship.HealthFillImage.fillAmount = 1f;
                    ship.HealthFillImage.color = Color.green;
                }
                else if (img.gameObject.name == "HealthBackground")
                {
                    ship.HealthBackgroundImage = img;
                    ship.HealthBackgroundImage.fillAmount = 1f;
                    ship.HealthBackgroundImage.color = Color.red;
                }
            }

            // ✅ Add to tracking list
            HealthbarRenderers.Add(healthbarGO);

            // ✅ Add billboard component to face camera
            var billboard = healthbarGO.GetComponent<BillboardCameraCombat>();
            if (billboard == null)
            {
                billboard = healthbarGO.AddComponent<BillboardCameraCombat>();
            }

            Debug.Log($"  ✅ Created health bar for {ship.ShipData.ShipName}");
        }
    }
}

