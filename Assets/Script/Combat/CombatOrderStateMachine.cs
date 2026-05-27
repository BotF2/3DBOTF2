using BOTF3D.Core;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Manages combat order execution for individual ships.
    /// Implements Engage (group coordination), Rush (max speed center), Retreat (warp out),
    /// Formation (defensive wall), and AttackTransports (flanking) orders.
    /// </summary>
    public class CombatOrderStateMachine : MonoBehaviour
    {
        [Header("Ship References")]
        public ShipController ShipController;
        public CombatOrders CurrentOrder = CombatOrders.Engage;

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
        private const float RETREAT_TURN_TIME = 2.5f; // Vulnerable turn period
        private bool isWarpingOut = false;
        private bool weaponsCutOff = false;
        private bool retreatTurnIsHorizontal; // True = Y-axis turn, False = X or Z-axis turn
        private float retreatTurnAngle; // Always 100°
        private Vector3 warpOutVelocity; // Acceleration during warp-out
        private float warpOutTimer;
        private const float WARP_OUT_DURATION = 1.5f; // Time to stretch and accelerate away
        private const float WARP_OUT_SPEED_MULTIPLIER = 40f; // 40x max warp speed
        private Transform shipModel; // Reference to child model for stretching

        [Header("Formation Settings - Defensive Wall")]
        public Vector3 formationPosition;
        public int formationSlot = -1;
        private const float FORMATION_SPACING = 35f;
        private bool isTransport = false;

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
            currentState = OrderState.Idle;

            // Find ship model (first child with MeshRenderer or SkinnedMeshRenderer)
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MeshRenderer>() != null || child.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    shipModel = child;
                    break;
                }
            }
        }

        private ShipController FindFallbackEnemy()
        {
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return null;

            bool isSideOne = transform.position.x < 0;
            List<ShipController> enemies = isSideOne ? combatController.CombatData.SideTwoShipCons : combatController.CombatData.SideOneShipCons;
            
            // Try center first
            ShipController target = enemies.Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType != ShipType.Transport && Mathf.Abs(s.transform.position.z) < RUSH_CENTER_RANGE)
                          .OrderBy(s => Vector3.Distance(transform.position, s.transform.position))
                          .FirstOrDefault();

            // Fallback to any combat ship
            if (target == null)
            {
                target = enemies.Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType != ShipType.Transport)
                          .OrderBy(s => Vector3.Distance(transform.position, s.transform.position))
                          .FirstOrDefault();
            }

            return target;
        }

        void Update()
        {
            if (ShipController == null || ShipController.ShipData == null || ShipController.ShipData.Distroyed)
                return;

            // In turn-based combat, only execute orders during Resolution phase
            var combatController = BOTF3D.UI.CombatUIManager.Instance?.CurrentCombatController;
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

            // Ensure we have a target if we are a combat ship
            if (!isTransport && (ShipController.ShipData.TargetThisShipController == null || ShipController.ShipData.TargetThisShipController.ShipData.Distroyed))
            {
                ShipController.ShipData.TargetThisShipController = FindFallbackEnemy();
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
            weaponsCutOff = false;
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

            // Target only enemies near center
            ShipController target = FindCenterEnemy();
            if (target != null)
            {
                ShipController.ShipData.TargetThisShipController = target;
            }
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
            bool isSideOne = transform.position.x < 0;
            // Side 1 (left, negative X) faces +X (toward right/enemy) = +90°
            // Side 2 (right, positive X) faces -X (toward left/enemy) = -90°
            transform.rotation = Quaternion.Euler(0, isSideOne ? 90 : -90, 0);
        }

        private Vector3 CalculateFormationWallPosition(int slot)
        {
            // Wall formation in YZ plane
            int col = slot % 5;
            int row = slot / 5;
            float sideSign = transform.position.x < 0 ? -1 : 1;
            float startX = sideSign * 400f; // Hold at warp-in end line
            
            return new Vector3(startX, (row - 2) * FORMATION_SPACING, (col - 2) * FORMATION_SPACING);
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
                    
                    float sideSign = transform.position.x < 0 ? 1 : -1;
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
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return null;

            bool isSideOne = transform.position.x < 0;
            List<ShipController> enemies = isSideOne ? combatController.CombatData.SideTwoShipCons : combatController.CombatData.SideOneShipCons;
            
            return enemies.Where(s => s != null && !s.ShipData.Distroyed && Mathf.Abs(s.transform.position.z) < RUSH_CENTER_RANGE)
                          .OrderBy(s => Vector3.Distance(transform.position, s.transform.position))
                          .FirstOrDefault();
        }

        private ShipController FindEnemyTransport()
        {
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return null;

            bool isSideOne = transform.position.x < 0;
            List<ShipController> enemies = isSideOne ? combatController.CombatData.SideTwoShipCons : combatController.CombatData.SideOneShipCons;
            
            return enemies.Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType == ShipType.Transport)
                          .OrderBy(s => Vector3.Distance(transform.position, s.transform.position))
                          .FirstOrDefault();
        }

        private void ExecuteTransportBehavior()
        {
            if (!isTransport) return;

            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return;

            bool isSideOne = transform.position.x < 0;
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
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return null;

            bool isSideOne = transform.position.x < 0;
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
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return Vector3.zero;

            bool isSideOne = transform.position.x < 0;

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
            // ✅ Initialize retreat turn
            if (currentState != OrderState.TurningToRetreat && currentState != OrderState.WarpingOut)
            {
                currentState = OrderState.TurningToRetreat;
                stateTimer = 0f;
                retreatStartRotation = transform.rotation;

                // ✅ Randomly choose turn direction: 50% horizontal (Y-axis), 50% vertical (X or Z-axis)
                retreatTurnIsHorizontal = Random.value > 0.5f;
                retreatTurnAngle = 100f; // Always 100° turn

                if (retreatTurnIsHorizontal)
                {
                    // Y-axis turn (left or right)
                    float turnDirection = Random.value > 0.5f ? 1f : -1f;
                    retreatTargetRotation = Quaternion.Euler(
                        transform.eulerAngles.x,
                        transform.eulerAngles.y + (retreatTurnAngle * turnDirection),
                        transform.eulerAngles.z
                    );
                    Debug.Log($"🔄 {ShipController.ShipData.ShipName} turning {(turnDirection > 0 ? "right" : "left")} 100° on Y-axis");
                }
                else
                {
                    // Vertical turn on Y-axis (up or down doesn't make sense, so use X-axis pitch)
                    float turnDirection = Random.value > 0.5f ? 1f : -1f;
                    retreatTargetRotation = Quaternion.Euler(
                        transform.eulerAngles.x + (retreatTurnAngle * turnDirection),
                        transform.eulerAngles.y,
                        transform.eulerAngles.z
                    );
                    Debug.Log($"🔄 {ShipController.ShipData.ShipName} turning {(turnDirection > 0 ? "up" : "down")} 100° on X-axis");
                }
            }

            // ✅ Phase 1: Turning (vulnerable, no movement)
            if (currentState == OrderState.TurningToRetreat)
            {
                stateTimer += Time.unscaledDeltaTime;
                float turnProgress = stateTimer / RETREAT_TURN_TIME;

                transform.rotation = Quaternion.Slerp(retreatStartRotation, retreatTargetRotation, turnProgress);

                if (stateTimer >= RETREAT_TURN_TIME)
                {
                    // ✅ Turn complete - start warp-out
                    currentState = OrderState.WarpingOut;
                    isWarpingOut = true;
                    warpOutTimer = 0f;
                    warpOutVelocity = Vector3.zero;

                    // Lock rotation - no more turning during warp-out
                    transform.rotation = retreatTargetRotation;

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
                }

                // ✅ Warp-out complete - destroy ship (it escaped)
                if (warpOutTimer >= WARP_OUT_DURATION)
                {
                    Debug.Log($"✅ {ShipController.ShipData.ShipName} warped out successfully!");

                    // Mark as destroyed (but don't apply damage - it escaped)
                    ShipController.ShipData.Distroyed = true;

                    // Remove from combat
                    var combatController = CombatUIManager.Instance?.CurrentCombatController;
                    if (combatController != null)
                    {
                        combatController.CombatData.SideOneShipCons.Remove(ShipController);
                        combatController.CombatData.SideTwoShipCons.Remove(ShipController);
                    }

                    // Destroy game object
                    Destroy(gameObject);
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
