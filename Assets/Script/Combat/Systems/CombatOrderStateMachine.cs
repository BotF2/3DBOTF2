using BOTF3D.Core;

using BOTF3D.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Manages combat order execution for individual ships.
    /// Implements Engage (group coordination), Rush (max speed center), Retreat (warp out),
    /// Formation (defensive wall), and AttackTransports (flanking) orders.
    /// </summary>
    public class CombatOrderStateMachine : MonoBehaviour
    {
        public void Initialize() { }
        public void Cleanup() { }
        // Skip all execution if this ship has been captured
        private bool IsCaptured => ShipController?.ShipData?.IsCaptured == true;
        [Header("Ship References")]
        public ShipController ShipController;
        public CombatOrders CurrentOrder = CombatOrders.Engage;
        public int Side; // 1 for SideOne, 2 for SideTwo

        [Header("State Tracking")]
        private OrderState currentState;
        private OrderState previousState;
        private CombatOrders previousOrder;
        private float stateTimer;

        [Header("Engage Settings - Group Coordination")]
        public ShipGroup assignedGroup; // Reference to 2-3 ship group
        public int groupId = -1;
        public ShipController groupTarget; // Common target for this group
        private const float ENGAGE_GROUP_SIZE = 3; // 2-3 ships per group

        [Header("Rush Settings - Center Attack")]
        private Vector3 enemyCenterPosition; // Where enemies entered combat
        private const float RUSH_CENTER_RANGE = 80f; // Only target enemies within this range of center

        [Header("Retreat Settings - Turn and Warp")]
        private bool hasCompletedTurn;
        private Quaternion retreatStartRotation;
        private Quaternion retreatTargetRotation;
        private Vector3 retreatMoveDirection; // Direction ship drifts toward while turning
        private const float RETREAT_TURN_TIME = 8.0f;
        private const float RETREAT_DRIFT_SPEED = 3f;  // maxWarpFactor multiplier for turn-phase drift
        private bool isWarpingOut = false;
        private const float RETREAT_TURN_DEGREES = 180f;
        private Vector3 warpOutVelocity; // Acceleration during warp-out
        private float warpOutTimer;
        private const float WARP_OUT_DURATION = 1.2f; // Time to stretch and accelerate away
        private const float WARP_OUT_SPEED_MULTIPLIER = 40f; // 40x max warp speed
        private Transform shipModel; // Reference to child model for stretching
        private float meshZMin = 0f; // Minimum Z coordinate of the mesh for pivot correction

        [Header("Formation Settings - Defensive Wall")]
        public Vector3 formationPosition;
        public int formationSlot = -1;
        private const float FORMATION_SPACING = 35f;
        private bool isTransport = false;
        private bool isSystemOwned = false; // stationed combat ship, orbital battery, or future shield

        [Header("Attack Transports Settings - Wide Flank")]
        private Vector3 flankTargetPosition;
        private bool isFlankingLeft;
        private bool hasChosenFlankPath = false;
        private const float FLANK_WIDTH = 150f; // How far to swing out

        private enum OrderState
        {
            Idle,
            MovingWithGroup,      // Engage: moving with 2-3 ship group
            RushingCenter,        // Rush: attacking center enemies at max speed
            TurningToRetreat,     // Retreat: turning 180° (vulnerable)
            WarpingOut,           // Retreat: warp animation (invulnerable)
            HoldingFormation,     // Formation: defensive wall
            FlankingWide,         // AttackTransports: wide swing to hit transports
            ProtectingTransports, // Formation: transports staying behind
            AvoidingAttack        // Transport: turning 30° away and running
        }

        private ShipController attackerTargetingMe;

        void Start()
        {
            ShipController = GetComponent<ShipController>();
            if (ShipController == null)
            {
                Debug.LogError($"CombatOrderStateMachine: No ShipController on {gameObject.name}!");
                enabled = false;
                return;
            }

            isTransport = (ShipController.ShipData.ShipType == ShipType.Transport);
            isSystemOwned = (ShipController.ShipData.CurrentStarSysController != null);
            currentState = OrderState.Idle;

            // Find ship model (first child with a renderer in its hierarchy)
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Target")) continue; // Skip target GO
                
                Renderer rMain = child.GetComponentInChildren<Renderer>();
                if (rMain != null)
                {
                    shipModel = child;
                    
                    // Calculate local bounds relative to the ShipController's forward axis
                    Renderer[] renderers = shipModel.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        float minZ = 0f;
                        float maxZ = 0f;
                        
                        foreach (var r in renderers)
                        {
                            // We need the point furthest from the pivot along the ShipController's Z axis
                            Bounds b = r.bounds;
                            Vector3 p1 = transform.InverseTransformPoint(b.min);
                            Vector3 p2 = transform.InverseTransformPoint(b.max);
                            
                            minZ = Mathf.Min(minZ, p1.z, p2.z);
                            maxZ = Mathf.Max(maxZ, p1.z, p2.z);
                        }
                        
                        // We want the coordinate of the "tail" in ShipController local space.
                        // Usually this is negative (behind the nose pivot at 0,0,0).
                        if (Mathf.Abs(minZ) > Mathf.Abs(maxZ))
                            meshZMin = minZ;
                        else
                            meshZMin = maxZ;

                        Debug.Log($"[CombatOrderStateMachine] {gameObject.name} initialized shipModel: {shipModel.name}, tailZ (ShipController LS): {meshZMin}");
                    }
                    break;
                }
            }
        }

        private ShipController FindFallbackEnemy()
        {
            var combatController = CombatManager.Instance?.GetActiveCombatControllerForCiv(ShipController.ShipData.CivEnum);
            if (combatController == null) return null;

            bool isSideOne = Side == 1;
            List<ShipController> enemies = isSideOne ? combatController.CombatData.SideTwoShipCons : combatController.CombatData.SideOneShipCons;
            List<ShipController> myShips = isSideOne ? combatController.CombatData.SideOneShipCons : combatController.CombatData.SideTwoShipCons;

            CombatOrders myOrder    = isSideOne ? combatController.CombatData.SideOneOrder : combatController.CombatData.SideTwoOrder;
            CombatOrders enemyOrder = isSideOne ? combatController.CombatData.SideTwoOrder : combatController.CombatData.SideOneOrder;

            var validEnemies = enemies
                .Where(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport)
                .ToList();

            // Formation focus fire: FOCUS_FIRE_RATIO of ships converge on the lowest-HP enemy;
            // the rest spread normally so the advantage is tuned rather than absolute.
            if (myOrder == CombatOrders.Formation &&
                (enemyOrder == CombatOrders.Engage || enemyOrder == CombatOrders.Rush) &&
                Random.value < CombatTargetingSystem.FOCUS_FIRE_RATIO)
            {
                return validEnemies
                    .OrderBy(e => e.ShipData.ShieldHealth + e.ShipData.HullHealth)
                    .FirstOrDefault();
            }

            return validEnemies
                .OrderBy(e => myShips.Count(s => s != null && s.ShipData != null && s.ShipData.TargetThisShipController == e))
                .ThenBy(s => Vector3.Distance(transform.position, s.transform.position))
                .FirstOrDefault();
        }

        void Update()
        {
            if (ShipController == null || ShipController.ShipData == null || ShipController.ShipData.Distroyed || IsCaptured)
                return;

            // In turn-based combat, only execute orders during Resolution phase
            var combatController = CombatManager.Instance?.GetActiveCombatControllerForCiv(ShipController.ShipData.CivEnum);
            if (combatController != null && combatController.UseTurnBasedCombat)
            {
                // Only move/fight during Resolution phase
                if (combatController.TurnResolver.CurrentPhase != CombatPhase.Resolution)
                {
                    return;
                }
            }

            // ✅ Detect order change and reset state
            if (ShipController.Order != CurrentOrder)
            {
                previousOrder = CurrentOrder;
                CurrentOrder = ShipController.Order;
                ResetOrderState();
            }

            // Guard: non-Retreat ships must never be in retreat states
            if (CurrentOrder != CombatOrders.Retreat &&
                (currentState == OrderState.TurningToRetreat || currentState == OrderState.WarpingOut || isWarpingOut))
            {
                Debug.LogWarning($"⚠️ {ShipController.ShipData.ShipName} in retreat state but order is {CurrentOrder} — resetting");
                ResetOrderState();
            }

            // Ensure we have a target if we are a combat ship
            var currentTarget = ShipController.ShipData.TargetThisShipController;
            if (!isTransport && (currentTarget == null || currentTarget.ShipData.Distroyed || currentTarget.ShipData.IsCaptured || !currentTarget.gameObject.activeInHierarchy))
            {
                ShipController.ShipData.TargetThisShipController = FindFallbackEnemy();
            }

            // System-owned ships (stationed combat ships, orbital batteries, future shields)
            // are stationary system defenses that never warp in and never move — skip every
            // movement/retreat order entirely (targeting above still runs so they keep firing).
            // Without this guard ExecuteRetreat would deactivate/reparent the ship's GameObject
            // and ExecuteFormation/Engage/Rush would drag it off its fixed position.
            if (isSystemOwned)
            {
                currentState = OrderState.Idle;
                return;
            }

            // Execute current order
            switch (CurrentOrder)
            {
                case CombatOrders.Engage:
                    ExecuteEngage();
                    break;

                case CombatOrders.Rush:
                    ExecuteRush();
                    break;

                case CombatOrders.Retreat:
                    ExecuteRetreat();
                    break;

                case CombatOrders.Formation:
                    ExecuteFormation();
                    break;

                case CombatOrders.AttackTransports:
                    ExecuteAttackTransports();
                    break;

                case CombatOrders.Capture:
                    ExecuteCapture();
                    break;

                case CombatOrders.Scuttle:
                    // No movement — ship will be destroyed before weapon fire by TurnBasedCombatResolver
                    break;
            }

            if (currentState != previousState)
            {
                previousState = currentState;
                stateTimer = 0f;
            }
        }

        /// <summary>
        /// Reset state flags when orders change (e.g., stop warping/stretching)
        /// </summary>
        private void ResetOrderState()
        {
            Debug.Log($"🔄 {ShipController.ShipData.ShipName} order changed from {previousOrder} to {CurrentOrder} - resetting state");
            
            isWarpingOut = false;
            currentState = OrderState.Idle;
            stateTimer = 0f;

            // Reset model scale (remove warp stretching)
            if (shipModel != null)
            {
                shipModel.localScale = Vector3.one;
            }
        }

        #region Engage Order - Group Coordination
        private void ExecuteEngage()
        {
            if (isTransport)
            {
                ExecuteTransportBehavior();
                return;
            }

            currentState = OrderState.MovingWithGroup;

            // Coordination happens in CombatController, but we handle individual target update here if group target is lost
            if (groupTarget == null || groupTarget.ShipData.Distroyed)
            {
                groupTarget = assignedGroup?.commonTarget;
                if (groupTarget != null)
                {
                    ShipController.ShipData.TargetThisShipController = groupTarget;
                }
            }
        }
        #endregion

        #region Rush Order - Individual Max Speed, Center Targets Only
        private void ExecuteRush()
        {
            if (isTransport)
            {
                ExecuteTransportBehavior();
                return;
            }

            currentState = OrderState.RushingCenter;

            ShipController target = FindFallbackEnemy();
            if (target != null)
                ShipController.ShipData.TargetThisShipController = target;
        }
        #endregion

        #region Formation Order - Defensive Wall
        private void ExecuteFormation()
        {
            if (isTransport)
            {
                ExecuteTransportBehavior();
                return;
            }

            if (currentState != OrderState.HoldingFormation)
            {
                if (formationSlot == -1) AssignFormationSlot();
                currentState = OrderState.HoldingFormation;
            }

            // Calculate formation position (y,z plane wall)
            Vector3 targetPos = CalculateFormationWallPosition(formationSlot);

            // Block fire if needed
            Vector3 blockingPos = CheckForTransportBlocking();
            if (blockingPos != Vector3.zero) targetPos = blockingPos;

            float formationSpeed = ShipController.ShipData.maxWarpFactor * 0.65f;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, formationSpeed * Time.unscaledDeltaTime);
            
            // In formation, stay facing forward toward enemy
            bool isSideOne = Side == 1;
            // Side 1 (left, negative X) faces +X (toward right/enemy) = +90°
            // Side 2 (right, positive X) faces -X (toward left/enemy) = -90°
            transform.rotation = Quaternion.Euler(0, isSideOne ? 90 : -90, 0);
        }

        private Vector3 CalculateFormationWallPosition(int slot)
        {
            // Wall formation in YZ plane
            int col = slot % 5;
            int row = slot / 5;
            float sideSign = Side == 1 ? -1 : 1;
            float startX = sideSign * 400f; // Hold at warp-in end line
            
            return new Vector3(startX, (row - 2) * FORMATION_SPACING, (col - 2) * FORMATION_SPACING);
        }
        #endregion

        #region Capture Order - Board Enemy Ships
        private void ExecuteCapture()
        {
            if (isTransport)
            {
                ExecuteTransportBehavior();
                return;
            }

            currentState = OrderState.MovingWithGroup;

            // Prefer retreating or scuttling ships as targets (they are capturable)
            var combatController = CombatManager.Instance?.GetActiveCombatControllerForCiv(ShipController.ShipData.CivEnum);
            if (combatController != null)
            {
                bool isSideOne = Side == 1;
                var enemies = isSideOne ? combatController.CombatData.SideTwoShipCons : combatController.CombatData.SideOneShipCons;

                var captureTarget = enemies
                    .Where(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy
                             && s.ShipData.ShipType != ShipType.Transport
                             && (s.Order == CombatOrders.Retreat || s.Order == CombatOrders.Scuttle))
                    .OrderBy(s => Vector3.Distance(transform.position, s.transform.position))
                    .FirstOrDefault();

                if (captureTarget != null)
                {
                    ShipController.ShipData.TargetThisShipController = captureTarget;
                    return;
                }
            }

            // No priority target — fall back to group coordination like Engage
            if (groupTarget == null || groupTarget.ShipData.Distroyed || groupTarget.ShipData.IsCaptured)
            {
                groupTarget = assignedGroup?.commonTarget;
                if (groupTarget != null)
                    ShipController.ShipData.TargetThisShipController = groupTarget;
            }
        }
        #endregion

        #region Attack Transports Order - Wide Flanking
        private void ExecuteAttackTransports()
        {
            if (isTransport)
            {
                ExecuteTransportBehavior();
                return;
            }

            if (currentState != OrderState.FlankingWide)
            {
                currentState = OrderState.FlankingWide;
                if (!hasChosenFlankPath)
                {
                    isFlankingLeft = Random.value > 0.5f;
                    hasChosenFlankPath = true;
                    
                    float sideSign = Side == 1 ? 1 : -1;
                    Vector3 flankDir = isFlankingLeft ? Vector3.forward : Vector3.back;
                    flankTargetPosition = transform.position + flankDir * FLANK_WIDTH + Vector3.right * sideSign * 300f;
                }
            }

            // Movement handled by CombatController

            ShipController transportTarget = FindEnemyTransport();
            if (transportTarget != null)
            {
                ShipController.ShipData.TargetThisShipController = transportTarget;
            }
        }
        #endregion

        #region Helper Methods
        private ShipController FindCenterEnemy()
        {
            var combatController = CombatManager.Instance?.GetActiveCombatControllerForCiv(ShipController.ShipData.CivEnum);
            if (combatController == null) return null;

            bool isSideOne = Side == 1;
            List<ShipController> enemies = isSideOne ? combatController.CombatData.SideTwoShipCons : combatController.CombatData.SideOneShipCons;
            List<ShipController> myShips = isSideOne ? combatController.CombatData.SideOneShipCons : combatController.CombatData.SideTwoShipCons;

            return enemies.Where(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy
                                  && s.ShipData.ShipType != ShipType.Transport
                                  && Mathf.Abs(s.transform.position.z) < RUSH_CENTER_RANGE)
                          .OrderBy(e => myShips.Count(s => s != null && s.ShipData != null && s.ShipData.TargetThisShipController == e))
                          .ThenBy(s => Vector3.Distance(transform.position, s.transform.position))
                          .FirstOrDefault();
        }

        private ShipController FindEnemyTransport()
        {
            var combatController = CombatManager.Instance?.GetActiveCombatControllerForCiv(ShipController.ShipData.CivEnum);
            if (combatController == null) return null;

            bool isSideOne = Side == 1;
            List<ShipController> enemies = isSideOne ? combatController.CombatData.SideTwoShipCons : combatController.CombatData.SideOneShipCons;

            return enemies.Where(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy && s.ShipData.ShipType == ShipType.Transport)
                          .OrderBy(s => Vector3.Distance(transform.position, s.transform.position))
                          .FirstOrDefault();
        }

        private void ExecuteTransportBehavior()
        {
            if (!isTransport) return;

            var combatController = CombatManager.Instance?.GetActiveCombatControllerForCiv(ShipController.ShipData.CivEnum);
            if (combatController == null) return;

            bool isSideOne = Side == 1;
            CombatOrders myOrder = isSideOne ? combatController.CombatData.SideOneOrder : combatController.CombatData.SideTwoOrder;
            CombatOrders enemyOrder = isSideOne ? combatController.CombatData.SideTwoOrder : combatController.CombatData.SideOneOrder;

            // Scenario 3: Formation order - transports hold position
            if (myOrder == CombatOrders.Formation)
            {
                currentState = OrderState.ProtectingTransports;
                attackerTargetingMe = null;
                return;
            }

            // Scenario 2: Avoidance behavior (only if NOT in formation)
            if (enemyOrder == CombatOrders.AttackTransports)
            {
                attackerTargetingMe = FindEnemyTargetingMe();
                if (attackerTargetingMe != null)
                {
                    currentState = OrderState.AvoidingAttack;
                    return;
                }
            }

            // Scenario 1: No forward movement in Rush or Engage
            if (myOrder == CombatOrders.Rush || myOrder == CombatOrders.Engage)
            {
                currentState = OrderState.Idle;
                attackerTargetingMe = null;
            }
        }

        private ShipController FindEnemyTargetingMe()
        {
            var combatController = CombatManager.Instance?.GetActiveCombatControllerForCiv(ShipController.ShipData.CivEnum);
            if (combatController == null) return null;

            bool isSideOne = Side == 1;
            List<ShipController> enemies = isSideOne ? combatController.CombatData.SideTwoShipCons : combatController.CombatData.SideOneShipCons;

            return enemies.FirstOrDefault(e => e != null && !e.ShipData.Distroyed && e.ShipData.TargetThisShipController == ShipController);
        }

        public bool IsAvoidingAttack() => currentState == OrderState.AvoidingAttack;
        public ShipController GetAttackerTargetingMe() => attackerTargetingMe;

        private void ExecuteFormationTransport()
        {
            currentState = OrderState.ProtectingTransports;
            // ✅ Transports hold position in formation (no movement)
        }

        private Vector3 CheckForTransportBlocking()
        {
            var combatController = CombatManager.Instance?.GetActiveCombatControllerForCiv(ShipController.ShipData.CivEnum);
            if (combatController == null) return Vector3.zero;

            bool isSideOne = Side == 1;

            // Get friendly transports and enemy ships
            List<ShipController> friendlyShips = isSideOne ? combatController.CombatData.SideOneShipCons : combatController.CombatData.SideTwoShipCons;
            List<ShipController> enemies = isSideOne ? combatController.CombatData.SideTwoShipCons : combatController.CombatData.SideOneShipCons;

            // Find friendly transports
            var transports = friendlyShips.Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType == ShipType.Transport).ToList();
            if (transports.Count == 0) return Vector3.zero;

            // Check if enemy is using AttackTransports order (they'll be flanking)
            CombatOrders enemyOrder = isSideOne ? combatController.CombatData.SideTwoOrder : combatController.CombatData.SideOneOrder;
            if (enemyOrder != CombatOrders.AttackTransports) return Vector3.zero;

            // Find enemy flanking ships (scouts/destroyers) that are targeting our transports
            var flankingEnemies = enemies.Where(e => e != null && !e.ShipData.Distroyed &&
                                                     (e.ShipData.ShipType == ShipType.Scout || e.ShipData.ShipType == ShipType.Destroyer) &&
                                                     e.ShipData.TargetThisShipController != null &&
                                                     e.ShipData.TargetThisShipController.ShipData.ShipType == ShipType.Transport).ToList();

            if (flankingEnemies.Count == 0) return Vector3.zero;

            // Find closest threatened transport
            ShipController threatenedTransport = transports
                .Where(t => flankingEnemies.Any(e => e.ShipData.TargetThisShipController == t))
                .OrderBy(t => flankingEnemies.Where(e => e.ShipData.TargetThisShipController == t)
                                             .Min(e => Vector3.Distance(e.transform.position, t.transform.position)))
                .FirstOrDefault();

            if (threatenedTransport == null) return Vector3.zero;

            // Find the closest flanking enemy threatening this transport
            ShipController closestFlanker = flankingEnemies
                .Where(e => e.ShipData.TargetThisShipController == threatenedTransport)
                .OrderBy(e => Vector3.Distance(e.transform.position, threatenedTransport.transform.position))
                .First();

            // Calculate intercept position (midpoint between flanker and transport)
            Vector3 interceptPos = (closestFlanker.transform.position + threatenedTransport.transform.position) / 2f;

            Debug.Log($"🛡️ {ShipController.ShipData.ShipName} moving to block {closestFlanker.ShipData.ShipName} from {threatenedTransport.ShipData.ShipName}");

            return interceptPos;
        }

        private void AssignFormationSlot()
        {
            formationSlot = Random.Range(0, 10);
        }

        private void ExecuteRetreat()
        {
            // Initialize retreat turn: 180° on Y axis over RETREAT_TURN_TIME seconds
            if (currentState != OrderState.TurningToRetreat && currentState != OrderState.WarpingOut)
            {
                currentState = OrderState.TurningToRetreat;
                stateTimer = 0f;
                retreatStartRotation = transform.rotation;

                float turnSign = Random.value > 0.5f ? 1f : -1f;
                retreatTargetRotation = Quaternion.Euler(
                    transform.eulerAngles.x,
                    transform.eulerAngles.y + RETREAT_TURN_DEGREES * turnSign,
                    transform.eulerAngles.z
                );

                // Direction the ship will face once the turn completes
                retreatMoveDirection = retreatTargetRotation * Vector3.forward;
            }

            // ✅ Phase 1: Turning (vulnerable, slow drift toward retreat direction)
            if (currentState == OrderState.TurningToRetreat)
            {
                stateTimer += Time.unscaledDeltaTime;
                float turnProgress = stateTimer / RETREAT_TURN_TIME;

                transform.rotation = Quaternion.Slerp(retreatStartRotation, retreatTargetRotation, turnProgress);

                // Drift slowly along the x-axis in the direction the ship will warp out
                transform.position += retreatMoveDirection * ShipController.ShipData.maxWarpFactor
                                      * RETREAT_DRIFT_SPEED * Time.unscaledDeltaTime;

                if (stateTimer >= RETREAT_TURN_TIME)
                {
                    // ✅ Turn complete - start warp-out
                    currentState = OrderState.WarpingOut;
                    isWarpingOut = true;
                    warpOutTimer = 0f;
                    warpOutVelocity = Vector3.zero;

                    // Lock rotation - no more turning during warp-out
                    transform.rotation = retreatTargetRotation;

                    // Stop weapon fire loops before the object is deactivated
                    ShipController.StopAllCoroutines();

                    Debug.Log($"🌀 {ShipController.ShipData.ShipName} starting warp-out animation");
                }
            }

            // ✅ Phase 2: Warp-Out (invulnerable, accelerate + stretch)
            if (currentState == OrderState.WarpingOut)
            {
                warpOutTimer += Time.unscaledDeltaTime;
                float warpProgress = warpOutTimer / WARP_OUT_DURATION;

                // ✅ Accelerate in facing direction (40x max warp speed)
                float maxWarpOutSpeed = ShipController.ShipData.maxWarpFactor * WARP_OUT_SPEED_MULTIPLIER;
                Vector3 warpDirection = transform.forward; // Ship faces direction it's warping

                // Smooth acceleration curve
                warpOutVelocity = Vector3.Lerp(Vector3.zero, warpDirection * maxWarpOutSpeed, warpProgress);

                // Apply movement
                transform.position += warpOutVelocity * Time.unscaledDeltaTime;

                // ✅ Stretch ship model along travel direction (child model's local Z)
                if (shipModel != null)
                {
                    // Stretch increases as ship accelerates (1x → 50x scale on local Z)
                    float stretchFactor = Mathf.Lerp(1f, 50f, warpProgress);
                    shipModel.localScale = new Vector3(1f, 1f, stretchFactor);

                    // ✅ Offset model forward so it stretches in facing direction
                    // Since pivot is at nose (0), and mesh is at Z < 0, 
                    // scaling Z pushes the tail back. We move the model forward by 
                    // the elongation amount to keep the tail fixed at its starting point.
                    float offset = meshZMin * (1f - stretchFactor);
                    shipModel.localPosition = new Vector3(0f, 0f, offset);
                }

                // ✅ Warp-out complete - ship escaped, return to fleet
                if (warpOutTimer >= WARP_OUT_DURATION)
                {
                    Debug.Log($"✅ {ShipController.ShipData.ShipName} warped out successfully!");

                    // Return to fleet — ship survived retreat, keep reference valid in FleetData.ShipsList
                    var fleet = ShipController.ShipData?.CurrentFleetController;
                    if (fleet != null)
                    {
                        ShipController.transform.SetParent(fleet.transform, false);
                        ShipController.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                        ShipController.transform.localScale = Vector3.one;
                        fleet.UpdateMaxWarp();
                    }

                    // Reset model stretching and position before deactivating
                    if (shipModel != null)
                    {
                        shipModel.localScale = Vector3.one;
                        shipModel.localPosition = Vector3.zero;
                    }

                    // Deactivate rather than destroy — reference stays alive for fleet UI
                    // Note: We leave it in SideOneShipCons/SideTwoShipCons so CombatController.EndCombat can find its fleet
                    gameObject.SetActive(false);
                }
            }
        }

        public float GetOrderSpeedFactor()
{
            switch (CurrentOrder)
            {
                case CombatOrders.Engage:
                    if (isTransport)
                    {
                        if (currentState == OrderState.AvoidingAttack) return 1.0f;
                        return 0f;
                    }
                    // Speed determined by group (slowest ship in group)
                    return assignedGroup != null ? assignedGroup.groupSpeed / ShipController.ShipData.maxWarpFactor : 1.0f;

                case CombatOrders.Rush:
                    if (isTransport)
                    {
                        if (currentState == OrderState.AvoidingAttack) return 1.0f;
                        return 0f;
                    }
                    return 1.0f; // Max speed for combat ships

                case CombatOrders.Retreat:
                    if (currentState == OrderState.TurningToRetreat)
                        return 0.0f; // Stopped while turning
                    else
                        return 0.0f; // Warp animation handles movement

                case CombatOrders.Formation:
                    if (isTransport) return 0f; // Transports hold position
                    return 0.65f; // Slow defensive movement

                case CombatOrders.AttackTransports:
                    if (isTransport)
                    {
                        if (currentState == OrderState.AvoidingAttack) return 1.0f;
                        return 0f;
                    }
                    // Fast flanking for Scouts/Destroyers, half speed for others
                    bool isFlankingShip = ShipController.ShipData.ShipType == ShipType.Scout ||
                                         ShipController.ShipData.ShipType == ShipType.Destroyer;
                    return isFlankingShip ? 1.5f : 0.5f;

                case CombatOrders.Capture:
                    if (isTransport) return 0f;
                    return assignedGroup != null ? assignedGroup.groupSpeed / ShipController.ShipData.maxWarpFactor : 1.0f;

                case CombatOrders.Scuttle:
                    return 0f; // No movement — ships self-destruct before combat begins

                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Check if ship is vulnerable (e.g., turning to retreat)
        /// </summary>
        public bool IsVulnerable()
        {
            return currentState == OrderState.TurningToRetreat;
        }

        /// <summary>
        /// Check if ship is warping out (invulnerable, weapons cut off)
        /// </summary>
        public bool IsWarpingOut()
        {
            return isWarpingOut || currentState == OrderState.WarpingOut;
        }

        /// <summary>
        /// Assign formation slot (called by CombatController)
        /// </summary>
        public void AssignFormationSlot(int slot, Vector3 centerPosition)
        {
            formationSlot = slot;
            formationPosition = centerPosition;
        }
        #endregion
    }

    /// <summary>
    /// Represents a group of 2-3 ships for Engage order coordination
    /// </summary>
    [System.Serializable]
    public class ShipGroup
    {
        public List<ShipController> ships = new List<ShipController>();
        public float groupSpeed; // Speed of slowest ship in group
        public ShipController commonTarget; // Shared target for group

        public void RecalculateGroupSpeed()
        {
            if (ships.Count == 0)
            {
                groupSpeed = 0f;
                return;
            }

            // Find slowest ship and apply 70% speed reduction for Engage order
            groupSpeed = ships.Min(s => s.ShipData.maxWarpFactor) * 0.7f;
        }
    }
}
