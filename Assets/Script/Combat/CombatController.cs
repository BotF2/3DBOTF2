// Ignore Spelling: BOTF Healthbar

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
        public List<GameObject> HealthbarRenderers { get; private set; } = new List<GameObject>();
        [Header("Move animators to move ships")]
        public float initialSpeed = 30f;     // starting velocity (units/sec)
        public float stopDistance;    // distance over which to stop
        private float deceleration;         // computed deceleration
        private float currentSpeed;
        private readonly List<Vector3> moveDirections = new List<Vector3>();
        public bool isMoving = false;
        public bool isClosing = false;
        private bool combatEnded = false;
        private bool showingEndPanel = false;


        private void Awake()
        {
            CombatID = GetInstanceID(); // Unity object id for this combat instance
            Debug.Log($"✅ CombatController {CombatID}: Created");

            // ✅ CRITICAL: Initialize animators list
            // This list is used by BeginPhysicsLikeMovement() and LateUpdate()
            if (animators == null)
            {
                animators = new List<Animator>();
            }
        }
        private void Start()
        {
            minFirstShotDelay = 0.2f;
            maxFirstShotDelay = 0.9f;
            currentSpeed = 30f;
            stopDistance = 390f;
            CleanupOrphanedProjectiles();

            // ✅ CRITICAL: Populate animators list with the 6 animator references
            // Order matters: [0-2] = Side One, [3-5] = Side Two
            animators.Clear();

            //if (sideOneA1Animator.TryGetComponent<S1A1Animator>(out var animScript)) ;
            //animScript.RunAnimation();
            //if (sideOneA2Animator.TryGetComponent<S1A2Animator>(out var animScript2)) ;
            //animScript2.RunAnimation();
            //if (sideOneA3Animator.TryGetComponent<S1A3Animator>(out var animScript3)) ;
            //animScript3.RunAnimation();
            //if (sideTwoA1Animator.TryGetComponent<S2A1Animator>(out var animScript4)) ;
            //animScript4.RunAnimation();
            //if (sideTwoA2Animator.TryGetComponent<S2A2Animator>(out var animScript5)) ;
            //animScript5.RunAnimation();
            //if (sideTwoA3Animator.TryGetComponent<S2A3Animator>(out var animScript6)) ;
            //animScript6.RunAnimation();

            if (sideOneA1Animator != null) animators.Add(sideOneA1Animator);
            if (sideOneA2Animator != null) animators.Add(sideOneA2Animator);
            if (sideOneA3Animator != null) animators.Add(sideOneA3Animator);
            if (sideTwoA1Animator != null) animators.Add(sideTwoA1Animator);
            if (sideTwoA2Animator != null) animators.Add(sideTwoA2Animator);
            if (sideTwoA3Animator != null) animators.Add(sideTwoA3Animator);

            Debug.Log($"✅ Populated animators list with {animators.Count} animators");
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
                // ✅ Stop moving if combat has ended
                if (isMoving && !combatEnded)
                {
                    // ✅ Use unscaledDeltaTime for combat movement
                    float step = currentSpeed * Time.unscaledDeltaTime;

                    for (int i = 0; i < animators.Count; i++)
                    {
                        // ✅ Apply order-based speed multiplier
                        bool isSideOne = (i < 3);
                        float speedMult = isSideOne ? CombatData.SideOneSpeedMultiplier : CombatData.SideTwoSpeedMultiplier;
                        float adjustedStep = step * speedMult;

                        var numChildren = animators[i].transform.childCount;
                        for (int j = 0; j < numChildren; j++)
                        {
                            var child = animators[i].transform.GetChild(j);

                            // ✅ CRITICAL: Don't move destroyed ships
                            if (child != null && child.TryGetComponent<ShipController>(out var shipController))
                            {
                                if (shipController.ShipData != null &&
                                    !shipController.ShipData.Distroyed &&
                                    shipController.ShipData.ShieldHealth + shipController.ShipData.HullHealth > 0)
                                {
                                    child.transform.Translate(moveDirections[i] * adjustedStep, Space.Self);
                                }
                            }
                        }
                    }

                    // ✅ Use unscaledDeltaTime for deceleration
                    currentSpeed -= deceleration * Time.unscaledDeltaTime;
                    if (currentSpeed <= 0f)
                    {
                        currentSpeed = 0f;
                        isMoving = false;
                    }
                }
            }
        }

        void Update()
        {
            // Check for combat end condition after animations complete
            if (!combatEnded && WarpingAnimationOver && !WarpingIn)
            {
                // Count surviving ships on each side (only active, non-destroyed ships)
                int sideOneAlive = CombatData.SideOneShipCons.Count(s => s != null && s.ShipData != null && !s.ShipData.Distroyed && s.ShipData.ShieldHealth + s.ShipData.HullHealth > 0);
                int sideTwoAlive = CombatData.SideTwoShipCons.Count(s => s != null && s.ShipData != null && !s.ShipData.Distroyed && s.ShipData.ShieldHealth + s.ShipData.HullHealth > 0);

                // Check if one side has been eliminated
                if (sideOneAlive == 0 || sideTwoAlive == 0)
                {
                    Debug.Log($"🏁 Combat ended! Side 1: {sideOneAlive} ships, Side 2: {sideTwoAlive} ships");
                    combatEnded = true;

                    // Stop all weapon fire immediately
                    StopAllWeaponFire();

                    // Show the combat end sequence
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

        // Add this public method for button handlers (optional)
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

            for (int i = 0; i < animators.Count; i++)
            {
                Vector3 dir; //= Vector3.zero;
                if (animators[i].transform.childCount > 0)
                {
                    // ✅ Base direction (towards enemy)
                    bool isSideOne = (i < 3);
                    dir = isSideOne ? -animators[i].transform.GetChild(0).transform.right.normalized
                                    : animators[i].transform.GetChild(0).transform.right.normalized;

                    // ✅ Modify direction based on combat order
                    var ships = isSideOne ? CombatData.SideOneShipCons : CombatData.SideTwoShipCons;
                    CombatOrders order = isSideOne ? CombatData.OrderSideOne : CombatData.OrderSideTwo;

                    // Apply order-based movement modifiers
                    switch (order)
                    {
                        case CombatOrders.Retreat:
                            // Reverse direction (run away)
                            dir = -dir;
                            Debug.Log($"  Animator {i}: RETREAT - reversing direction");
                            break;

                        case CombatOrders.Formation:
                            // Slight spread pattern (maintain defensive formation)
                            float spreadAngle = (i % 3 - 1) * 15f; // -15°, 0°, +15°
                            dir = Quaternion.Euler(0, spreadAngle, 0) * dir;
                            Debug.Log($"  Animator {i}: FORMATION - spread {spreadAngle}°");
                            break;

                        case CombatOrders.Rush:
                            // Direct aggressive approach (no modification needed, just faster)
                            Debug.Log($"  Animator {i}: RUSH - full speed ahead");
                            break;

                        case CombatOrders.TargetTransports:
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
            float sideOneSpeedMult = CombatOrderMatrix.GetSpeedMultiplier(CombatData.OrderSideOne);
            float sideTwoSpeedMult = CombatOrderMatrix.GetSpeedMultiplier(CombatData.OrderSideTwo);

            // Calculate advantage-based speed boost
            int advantage = CombatOrderMatrix.GetAdvantage(CombatData.OrderSideOne, CombatData.OrderSideTwo);
            if (advantage > 0)
            {
                sideOneSpeedMult *= 1.1f; // 10% bonus for advantageous tactics
                Debug.Log("  ✅ Side One gets 10% speed bonus from tactical advantage!");
            }
            else if (advantage < 0)
            {
                sideTwoSpeedMult *= 1.1f; // 10% bonus for advantageous tactics
                Debug.Log("  ✅ Side Two gets 10% speed bonus from tactical advantage!");
            }

            // Apply base speed with order modifiers
            // (We'll apply this in LateUpdate based on which animator group)
            CombatData.SideOneSpeedMultiplier = sideOneSpeedMult;
            CombatData.SideTwoSpeedMultiplier = sideTwoSpeedMult;

            deceleration = (initialSpeed * initialSpeed) / (2f * stopDistance);
            currentSpeed = initialSpeed;
            isMoving = true;

            Debug.Log($"📊 Movement started: Side1 speed={sideOneSpeedMult:F2}x, Side2 speed={sideTwoSpeedMult:F2}x");
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

                // ✅ Apply order to all side one ships
                foreach (var ship in CombatData.SideOneShipCons)
                {
                    if (ship != null)
                    {
                        ship.SetShipOrder(order);
                    }
                }
            }
            else if (civOfOrder == CombatData.CivEnumSideTwo)
            {
                CombatData.OrderSideTwo = order;

                // ✅ Apply order to all side two ships
                foreach (var ship in CombatData.SideTwoShipCons)
                {
                    if (ship != null)
                    {
                        ship.SetShipOrder(order);
                    }
                }
            }

            // ✅ Calculate and log combat advantage
            int advantage = CombatOrderMatrix.GetAdvantage(CombatData.OrderSideOne, CombatData.OrderSideTwo);
            Debug.Log($"📊 Combat Orders: Side 1={CombatData.OrderSideOne}, Side 2={CombatData.OrderSideTwo}");
            Debug.Log($"   {CombatOrderMatrix.GetAdvantageDescription(advantage)} (Advantage: {advantage})");
        }
        /// <summary>
        /// Give AI ships a random combat order
        /// </summary>
        public void SetAIRandomOrder(CivEnum aiCivEnum)
        {
            // Check if this civ has transports (affects available orders)
            bool hasTransports = false;
            int side = 0;

            if (aiCivEnum == CombatData.CivEnumSideOne)
            {
                hasTransports = CombatOrderMatrix.HasTransports(CombatData, 1);
                side = 1;
            }
            else if (aiCivEnum == CombatData.CivEnumSideTwo)
            {
                hasTransports = CombatOrderMatrix.HasTransports(CombatData, 2);
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

            // Only add TargetTransports if enemy has transports
            bool enemyHasTransports = CombatOrderMatrix.HasTransports(CombatData, side == 1 ? 2 : 1);
            if (enemyHasTransports)
            {
                availableOrders.Add(CombatOrders.TargetTransports);
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

            // ✅ STEP 8: Re-enable EventSystem in GalaxyScene BEFORE unloading CombatScene
            Scene galaxyScene = SceneManager.GetSceneByName("GalaxyScene");
            if (galaxyScene.isLoaded)
            {
                GameObject[] rootObjects = galaxyScene.GetRootGameObjects();
                foreach (var go in rootObjects)
                {
                    var eventSystem = go.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true);
                    if (eventSystem != null)
                    {
                        eventSystem.enabled = true;
                        Debug.Log($"  ✅ Re-enabled EventSystem: '{eventSystem.gameObject.name}'");
                    }
                }
            }

            Debug.Log("=== EndCombat: Cleanup complete ===");

            // ✅ STEP 9: Unload combat scene (ships are now safe in GalaxyScene)
            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            if (combatScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(combatScene);
                Debug.Log("  Unloaded combat scene");
            }

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

                Debug.Log($"✅ Canvas configured: RenderMode={ShipCombatCanvas.renderMode}, Camera={(ShipCombatCanvas.worldCamera != null ? ShipCombatCanvas.worldCamera.name : null)}");
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
                if (!healthbarGO.TryGetComponent<CanvasScaler>(out var canvasScaler))
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
                        shipConList[i].HealthFillImage.color = Color.green; // Start green
                    }
                    else if (healthbarImages[j].gameObject.name == "HealthBackground")
                    {
                        // NEW: Set up background as red damage indicator
                        shipConList[i].HealthBackgroundImage = healthbarImages[j];
                        shipConList[i].HealthBackgroundImage.fillAmount = 1f;
                        shipConList[i].HealthBackgroundImage.color = Color.red;
                    }
                }

                healthbarGO.SetActive(false); // Start hidden until warp-in completes
                HealthbarRenderers.Add(healthbarGO);

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
                    Debug.Log($"❌NEED FBX MODLE IN SO❌ Ship model prefab is NULL for {shipConList[i].ShipData.ShipName}");

                    // ✅ Load fallback from ShipManager
                    ShipSO fallbackSO = ShipManager.Instance.GetFallbackShipSO();
                    mesheGO = fallbackSO != null ? fallbackSO.ShipFBX_ModelAsGOPrefab : null;
                }

                if (mesheGO == null)
                {
                    Debug.Log("❌ Fallback ship model also NULL - cannot spawn ship!");
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
                // ✅ Disable stencil masking on ship materials
                DisableStencilOnShipRenderers(fbx);
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
            Debug.Log($"🎯 FindClosestPairsForTargets: {shipListFiring.Count} ships firing at {shipListTargets.Count} targets");

            for (int i = 0; i < shipListFiring.Count; i++)
            {
                ShipController closestTarget = null;
                float shortestDist = Mathf.Infinity;

                // ✅ Get firing ship's combat order
                CombatOrders firingOrder = shipListFiring[i].Order;

                Debug.Log($"   Ship {i}: '{shipListFiring[i].ShipData.ShipName}' (Order={firingOrder}) searching for target...");

                for (int j = 0; j < shipListTargets.Count; j++)
                {
                    var potentialTarget = shipListTargets[j];

                    // ✅ ORDER-BASED TARGETING PRIORITY
                    // If order is TargetTransports, prioritize transport ships
                    if (firingOrder == CombatOrders.TargetTransports)
                    {
                        // Skip non-transport ships if transports are available
                        if (potentialTarget.ShipData.ShipType != ShipType.Transport)
                        {
                            bool hasTransportTargets = shipListTargets.Any(s => s.ShipData.ShipType == ShipType.Transport);
                            if (hasTransportTargets)
                                continue; // Skip this ship, look for transports
                        }
                    }

                    Vector3 origin = shipListFiring[i].transform.position;
                    Vector3 targetPos = potentialTarget.transform.position;
                    Vector3 dir = (targetPos - origin).normalized;
                    Vector3 safeOrigin = origin + dir * 10f;
                    float dist = Vector3.Distance(origin, targetPos);

                    float distSqr = (shipListFiring[i].transform.position - potentialTarget.transform.position).sqrMagnitude;

                    // ✅ Apply targeting priority bonus for transports when using TargetTransports order
                    if (firingOrder == CombatOrders.TargetTransports && potentialTarget.ShipData.ShipType == ShipType.Transport)
                    {
                        distSqr *= 0.5f; // Make transports seem "closer" for priority targeting
                    }

                    if (distSqr < shortestDist)
                    {
                        shortestDist = distSqr;
                        if (Physics.Raycast(safeOrigin, dir, out RaycastHit hit, dist, 9) == false)
                        {
                            if (dist < shortestDist)
                            {
                                shortestDist = dist;
                                closestTarget = potentialTarget;
                            }
                        }
                    }
                }

                if (closestTarget != null)
                {
                    shipListFiring[i].ShipData.TargetThisShipController = closestTarget;
                    Debug.Log($"   ✅ Ship '{shipListFiring[i].ShipData.ShipName}' ({firingOrder}) targets '{closestTarget.ShipData.ShipName}' ({closestTarget.ShipData.ShipType})");
                }
                else
                {
                    Debug.LogWarning($"   ⚠️ Ship '{shipListFiring[i].ShipData.ShipName}' found NO valid target!");
                }
            }
        }

        private void FireWeaponsOrderOnShipControllers(List<ShipController> shipCons)
        {
            Debug.Log($"🔫 FireWeaponsOrderOnShipControllers called with {shipCons.Count} ships");

            int weaponsStarted = 0;

            // Implement logic to fire weapons on their enemy ships
            for (int i = 0; i < shipCons.Count; i++)
            {
                string shipName = shipCons[i].ShipData.ShipName;
                string targetName = shipCons[i].ShipData.TargetThisShipController?.ShipData.ShipName ?? "NULL";
                int torpedoDmg = shipCons[i].ShipData.TorpedoDamage;
                int beamDmg = shipCons[i].ShipData.BeamDamage;

                Debug.Log($"   Ship [{i}]: '{shipName}' - Target={targetName}, TorpedoDmg={torpedoDmg}, BeamDmg={beamDmg}");

                if (shipCons[i].ShipData.TargetThisShipController != null && (shipCons[i].ShipData.TorpedoDamage > 0 || shipCons[i].ShipData.BeamDamage > 0))
                {
                    float delay = UnityEngine.Random.Range(minFirstShotDelay, maxFirstShotDelay);
                    Debug.Log($"   ✅ Starting fire loop for '{shipName}' with {delay}s delay");
                    StartCoroutine(shipCons[i].ShipFireLoop(delay));
                    weaponsStarted++;
                }
                else
                {

                }
                if (shipCons[i].ShipData.TargetThisShipController == null)
                    Debug.Log($"   ⚠️ '{shipName}' has NO TARGET assigned!");
            }

            Debug.Log($"🔫 Started {weaponsStarted} weapon fire loops out of {shipCons.Count} ships");
        }

        IEnumerator RealtimeTimerCoroutineWeaponDischarge(float delayInSeconds)
        {
            yield return new WaitForSecondsRealtime(delayInSeconds);
        }

        public void RunAnimation()
        {
            WarpingIn = true;
            WarpingAnimationOver = false;

            Debug.Log($"🎬 RunAnimation() called: WarpingIn={WarpingIn}, WarpingAnimationOver={WarpingAnimationOver}");
            Debug.Log($"   ⏱️ Time.timeScale={Time.timeScale}, Time.deltaTime={Time.deltaTime}");

            // ✅ CRITICAL: Ensure CombatUIManager knows about this controller BEFORE triggering animations
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.CurrentCombatController = this;
                Debug.Log("✅ Set CurrentCombatController in CombatUIManager before animations");
            }
            else
            {
                Debug.LogError("❌ CombatUIManager.Instance is NULL!");
            }

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

            // ✅ Log animator status
            Debug.Log($"🎬 Animator count: {animators.Count}");
            Debug.Log($"   sideOneA1Animator: {(sideOneA1Animator != null ? sideOneA1Animator.name : "NULL")}");
            Debug.Log($"   sideOneA2Animator: {(sideOneA2Animator != null ? sideOneA2Animator.name : "NULL")}");
            Debug.Log($"   sideOneA3Animator: {(sideOneA3Animator != null ? sideOneA3Animator.name : "NULL")}");
            Debug.Log($"   sideTwoA1Animator: {(sideTwoA1Animator != null ? sideTwoA1Animator.name : "NULL")}");
            Debug.Log($"   sideTwoA2Animator: {(sideTwoA2Animator != null ? sideTwoA2Animator.name : "NULL")}");
            Debug.Log($"   sideTwoA3Animator: {(sideTwoA3Animator != null ? sideTwoA3Animator.name : "NULL")}");

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
                else
                {
                    Debug.LogError("   ❌ sideOneA1Animator has no S1A1Animator component!");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ sideOneA1Animator is NULL - cannot trigger animation");
            }

            if (sideOneA2Animator != null)
            {
                if (sideOneA2Animator.TryGetComponent<S1A2Animator>(out var animScript))
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S1A2Animator");
                }
                else
                {
                    Debug.LogError("   ❌ sideOneA2Animator has no S1A2Animator component!");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ sideOneA2Animator is NULL - skipping");
            }

            if (sideOneA3Animator != null)
            {
                var animScript = sideOneA3Animator.GetComponent<S1A3Animator>();
                if (animScript != null)
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S1A3Animator");
                }
                else
                {
                    Debug.LogError("   ❌ sideOneA3Animator has no S1A3Animator component!");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ sideOneA3Animator is NULL - skipping");
            }

            if (sideTwoA1Animator != null)
            {
                if (sideTwoA1Animator.TryGetComponent<S2A1Animator>(out var animScript))
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S2A1Animator");
                }
                else
                {
                    Debug.LogError("   ❌ sideTwoA1Animator has no S2A1Animator component!");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ sideTwoA1Animator is NULL - skipping");
            }

            if (sideTwoA2Animator != null)
            {
                var animScript = sideTwoA2Animator.GetComponent<S2A2Animator>();
                if (animScript != null)
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S2A2Animator");
                }
                else
                {
                    Debug.LogError("   ❌ sideTwoA2Animator has no S2A2Animator component!");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ sideTwoA2Animator is NULL - skipping");
            }

            if (sideTwoA3Animator != null)
            {
                if (sideTwoA3Animator.TryGetComponent<S2A3Animator>(out var animScript))
                {
                    animScript.RunAnimation();
                    Debug.Log("   ✅ Triggered S2A3Animator");
                }
                else
                {
                    Debug.LogError("   ❌ sideTwoA3Animator has no S2A3Animator component!");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ sideTwoA3Animator is NULL - skipping");
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
            yield return _waitForSeconds2;
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
                yield return _waitForSeconds2;
            }

            Debug.Log("✅ Warp-in animation complete");

            ShipCombatCameraController.Instance.SetWarpingIn(false);
            ShipCombatCameraController.Instance.SetWarpingInOver(true);

            // ✅ Start ship movement
            BeginPhysicsLikeMovement();

            // ✅ Show health bars
            for (int i = 0; i < HealthbarRenderers.Count; i++)
            {
                HealthbarRenderers[i].SetActive(true);
            }

            WarpingAnimationOver = true;
            WarpingIn = false;

            // ✅ Wait for ships to move closer (2 seconds) before firing
            Debug.Log("⏳ Ships moving to battle positions...");
            yield return _waitForSeconds2;
            Debug.Log("✅ Ships in position - starting weapon fire");

            // ✅ Now assign targets and fire weapons
            Debug.Log($"📊 Side One ships: {CombatData.SideOneShipCons.Count}, Side Two ships: {CombatData.SideTwoShipCons.Count}");

            Debug.Log("🎯 Assigning targets for Side One ships...");
            FindClosestPairsForTargets(CombatData.SideOneShipCons, CombatData.SideTwoShipCons);

            Debug.Log("🎯 Assigning targets for Side Two ships...");
            FindClosestPairsForTargets(CombatData.SideTwoShipCons, CombatData.SideOneShipCons);

            Debug.Log("🔫 Starting weapon fire for Side One ships...");
            FireWeaponsOrderOnShipControllers(CombatData.SideOneShipCons);

            Debug.Log("🔫 Starting weapon fire for Side Two ships...");
            FireWeaponsOrderOnShipControllers(CombatData.SideTwoShipCons);

            Debug.Log("✅ All weapon systems initialized");
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
                    Debug.LogError($"   ❌ Animator [{i}] ({animator.name}) has NO CONTROLLER ASSIGNED!");
                    continue;
                }

                // ✅ CHECK CULLING MODE
                if (animator.cullingMode == AnimatorCullingMode.CullCompletely)
                {
                    Debug.LogError($"   ❌ Animator [{i}] ({animator.name}) CullingMode is 'CullCompletely' - animations won't play!");
                }

                // ✅ CHECK IF ENABLED
                if (!animator.enabled)
                {
                    Debug.LogError($"   ❌ Animator [{i}] ({animator.name}) is DISABLED!");
                }

                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                bool isInTransition = animator.IsInTransition(0);
                float normalizedTime = stateInfo.normalizedTime;

                // ✅ Detailed logging for first frame
                if (Time.frameCount % 30 == 0) // Log every 30 frames
                {
                    Debug.Log($"   Animator [{i}] '{animator.name}': State='{stateInfo.shortNameHash}', Time={normalizedTime:F3}, Transition={isInTransition}, Enabled={animator.enabled}, CullingMode={animator.cullingMode}");
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

            // ✅ DON'T call RunAnimation() here - it will be called by CombatUIManager.EnterShipCombatPhase()
            // when the player clicks the button or the timer expires
            Debug.Log("  ⏳ Waiting for player to start combat (button click or timer)...");

            Debug.Log("=== InitializeCombat: Complete ===");
        }

        private ShipSO GetShipSOForShip(ShipController ship)
        {
            // ShipData has a ShipSO property that holds the SO with .fbx game object 'prefab' reference
            return ship?.ShipData?.ShipSO;

        }
        /// <summary>
        /// Prevents 3D ship materials from conflicting with UI masking
        /// Call after instantiating ship FBX models
        /// </summary>
        private void DisableStencilOnShipRenderers(GameObject shipGameObject)
        {
            Renderer[] renderers = shipGameObject.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;

                    // Set render queue above UI to avoid masking
                    material.renderQueue = 3001;

                    Debug.Log($"    Set render queue for material '{material.name}' to 3001");
                }
            }

            Debug.Log($"  ✅ Fixed {renderers.Length} renderers for '{shipGameObject.name}'");
        }
    }
}

