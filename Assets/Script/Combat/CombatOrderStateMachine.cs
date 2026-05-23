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
            ProtectingTransports  // Formation: transports staying behind
        }

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
        }

        void Update()
        {
            if (ShipController == null || ShipController.ShipData == null || ShipController.ShipData.Distroyed)
                return;

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
        }

        #region Engage Order - Group Coordination
        /// <summary>
        /// Engage: Ships move in groups of 2-3 with similar speeds.
        /// Group speed = slowest ship's maxWarpFactor.
        /// Ships coordinate to maintain LOS on a common target.
        /// Mainly forward movement, no wide swings.
        /// Target: Closest and slowest enemy combat ship.
        /// </summary>
        private void ExecuteEngage()
        {
            if (isTransport)
            {
                // Transports stay back during Engage
                currentState = OrderState.Idle;
                return;
            }

            if (currentState != OrderState.MovingWithGroup)
            {
                currentState = OrderState.MovingWithGroup;
            }

            // Group coordination happens in CombatController
            // This just marks the ship as following Engage behavior

            // Find and target closest/slowest enemy combat ship
            if (groupTarget == null || groupTarget.ShipData.Distroyed)
            {
                groupTarget = FindClosestSlowestEnemy();
                if (groupTarget != null)
                {
                    ShipController.ShipData.TargetThisShipController = groupTarget;
                }
            }
        }

        /// <summary>
        /// Find the closest and slowest enemy combat ship (not transport)
        /// </summary>
        private ShipController FindClosestSlowestEnemy()
        {
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return null;

            bool isSideOne = transform.position.x < 0;
            List<ShipController> enemyShips = isSideOne
                ? combatController.CombatData.SideTwoShipCons
                : combatController.CombatData.SideOneShipCons;

            // Filter to combat ships only (no transports)
            var combatShips = enemyShips.Where(s =>
                s != null &&
                !s.ShipData.Distroyed &&
                s.ShipData.ShipType != ShipType.Transport
            ).ToList();

            if (combatShips.Count == 0) return null;

            // Find closest
            ShipController closest = combatShips.OrderBy(s =>
                Vector3.Distance(transform.position, s.transform.position)
            ).FirstOrDefault();

            // Among ships at similar distance, pick slowest
            float closestDistance = Vector3.Distance(transform.position, closest.transform.position);
            var nearbyShips = combatShips.Where(s =>
                Mathf.Abs(Vector3.Distance(transform.position, s.transform.position) - closestDistance) < 30f
            );

            return nearbyShips.OrderBy(s => s.ShipData.maxWarpFactor).FirstOrDefault();
        }
        #endregion

        #region Rush Order - Individual Max Speed, Center Targets Only
        /// <summary>
        /// Rush: Ships attack at individual max speed (no coordination).
        /// Target only enemy ships near center (where they entered combat).
        /// No wide swings - straight forward attack.
        /// Ships spread out naturally due to different speeds.
        /// </summary>
        private void ExecuteRush()
        {
            if (isTransport)
            {
                // Transports don't rush - stay back
                currentState = OrderState.Idle;
                return;
            }

            if (currentState != OrderState.RushingCenter)
            {
                currentState = OrderState.RushingCenter;

                // Calculate enemy center position (where they entered)
                var combatController = CombatUIManager.Instance?.CurrentCombatController;
                if (combatController != null)
                {
                    bool isSideOne = transform.position.x < 0;
                    List<ShipController> enemyShips = isSideOne
                        ? combatController.CombatData.SideTwoShipCons
                        : combatController.CombatData.SideOneShipCons;

                    if (enemyShips.Count > 0)
                    {
                        // Average position of all enemy ships
                        Vector3 sum = Vector3.zero;
                        int count = 0;
                        foreach (var enemy in enemyShips)
                        {
                            if (enemy != null)
                            {
                                sum += enemy.transform.position;
                                count++;
                            }
                        }
                        enemyCenterPosition = sum / count;
                    }
                }
            }

            // Target only enemies near center
            ShipController target = FindCenterEnemy();
            if (target != null)
            {
                ShipController.ShipData.TargetThisShipController = target;
            }
        }

        /// <summary>
        /// Find enemy ships near the center position (within RUSH_CENTER_RANGE)
        /// </summary>
        private ShipController FindCenterEnemy()
        {
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return null;

            bool isSideOne = transform.position.x < 0;
            List<ShipController> enemyShips = isSideOne
                ? combatController.CombatData.SideTwoShipCons
                : combatController.CombatData.SideOneShipCons;

            // Find enemies near center
            var centerEnemies = enemyShips.Where(s =>
                s != null &&
                !s.ShipData.Distroyed &&
                Vector3.Distance(s.transform.position, enemyCenterPosition) < RUSH_CENTER_RANGE
            ).ToList();

            if (centerEnemies.Count == 0) return null;

            // Return closest center enemy
            return centerEnemies.OrderBy(s =>
                Vector3.Distance(transform.position, s.transform.position)
            ).FirstOrDefault();
        }
        #endregion

        #region Retreat Order - Turn and Warp Out
        /// <summary>
        /// Retreat: Phase 1 - Turn 180° (2.5s vulnerable, weapons still fire at you).
        ///          Phase 2 - Warp out animation (invulnerable, weapons cut off).
        /// </summary>
        private void ExecuteRetreat()
        {
            if (currentState != OrderState.TurningToRetreat && currentState != OrderState.WarpingOut)
            {
                // ✅ Start retreat sequence
                currentState = OrderState.TurningToRetreat;
                retreatStartRotation = transform.rotation;
                retreatTargetRotation = Quaternion.Euler(0, transform.eulerAngles.y + 180f, 0);
                stateTimer = 0f;
                hasCompletedTurn = false;

                Debug.Log($"🔄 {ShipController.ShipData.ShipName} RETREAT: Starting 180° turn (VULNERABLE for {RETREAT_TURN_TIME}s)");
            }

            if (currentState == OrderState.TurningToRetreat)
            {
                // ⚠️ VULNERABLE PHASE: Turning around
                stateTimer += Time.unscaledDeltaTime;
                float turnProgress = Mathf.Clamp01(stateTimer / RETREAT_TURN_TIME);

                transform.rotation = Quaternion.Slerp(retreatStartRotation, retreatTargetRotation, turnProgress);

                if (turnProgress >= 1f)
                {
                    hasCompletedTurn = true;
                    currentState = OrderState.WarpingOut;
                    Debug.Log($"✅ {ShipController.ShipData.ShipName} turn complete - starting WARP OUT");
                }
            }
            else if (currentState == OrderState.WarpingOut)
            {
                // ✅ INVULNERABLE PHASE: Warping out
                if (!isWarpingOut)
                {
                    StartWarpOutAnimation();
                    isWarpingOut = true;
                }

                // ✅ Cut off weapons fire to/from this ship
                if (!weaponsCutOff)
                {
                    CutOffWeaponsFire();
                    weaponsCutOff = true;
                }
            }
        }

        /// <summary>
        /// Start reverse warp-out animation (mirror of warp-in)
        /// Ship becomes invulnerable during this animation
        /// </summary>
        private void StartWarpOutAnimation()
        {
            // Find parent animator (S1A1, S1A2, S1A3, S2A1, S2A2, or S2A3)
            Animator parentAnimator = GetComponentInParent<Animator>();

            if (parentAnimator != null)
            {
                // Trigger reverse warp animation
                bool isSideOne = transform.position.x < 0;

                if (isSideOne)
                {
                    // Side 1 warp out
                    parentAnimator.SetBool("WarpOutS1", true);
                }
                else
                {
                    // Side 2 warp out
                    parentAnimator.SetBool("WarpOutS2", true);
                }

                Debug.Log($"🌌 {ShipController.ShipData.ShipName} WARP OUT animation started (now INVULNERABLE)");

                // Disable colliders so weapons can't hit
                Collider[] colliders = GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    col.enabled = false;
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ {ShipController.ShipData.ShipName}: No parent Animator for warp-out!");
            }
        }

        /// <summary>
        /// Cut off all weapons fire to/from this ship during warp
        /// </summary>
        private void CutOffWeaponsFire()
        {
            // Clear this ship's target (stops firing)
            if (ShipController.ShipData != null)
            {
                ShipController.ShipData.TargetThisShipController = null;
            }

            // Clear any ships targeting this ship
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController != null)
            {
                foreach (var ship in combatController.CombatData.SideOneShipCons)
                {
                    if (ship != null && ship.ShipData != null &&
                        ship.ShipData.TargetThisShipController == ShipController)
                    {
                        ship.ShipData.TargetThisShipController = null;
                    }
                }

                foreach (var ship in combatController.CombatData.SideTwoShipCons)
                {
                    if (ship != null && ship.ShipData != null &&
                        ship.ShipData.TargetThisShipController == ShipController)
                    {
                        ship.ShipData.TargetThisShipController = null;
                    }
                }
            }

            Debug.Log($"🚫 {ShipController.ShipData.ShipName} weapons fire CUT OFF (warping)");
        }
        #endregion

        #region Formation Order - Defensive Wall
        /// <summary>
        /// Formation: Defensive wall with overlapping fire.
        /// Transports stay behind combat ships.
        /// Ships can move to block LOS to transports.
        /// Some ships may lose LOS on flanking enemies to maintain formation.
        /// </summary>
        private void ExecuteFormation()
        {
            if (isTransport)
            {
                // Transports stay BEHIND combat ships
                ExecuteFormationTransport();
                return;
            }

            if (currentState != OrderState.HoldingFormation)
            {
                if (formationSlot == -1)
                {
                    AssignFormationSlot();
                }
                currentState = OrderState.HoldingFormation;
            }

            // Calculate formation position
            Vector3 targetPos = CalculateFormationPosition(formationSlot);

            // ✅ Check if we need to move to block LOS to transports
            Vector3 blockingPosition = CheckForTransportBlocking();
            if (blockingPosition != Vector3.zero)
            {
                targetPos = blockingPosition; // Override formation to block
            }

            // Move to formation position (slow, defensive)
            float formationSpeed = ShipController.ShipData.maxWarpFactor * 0.65f;
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                formationSpeed * Time.unscaledDeltaTime
            );
        }

        /// <summary>
        /// Transports in Formation stay behind combat ships
        /// </summary>
        private void ExecuteFormationTransport()
        {
            currentState = OrderState.ProtectingTransports;

            // Find the formation line (average position of combat ships)
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return;

            bool isSideOne = transform.position.x < 0;
            List<ShipController> friendlyShips = isSideOne
                ? combatController.CombatData.SideOneShipCons
                : combatController.CombatData.SideTwoShipCons;

            var combatShips = friendlyShips.Where(s =>
                s != null &&
                !s.ShipData.Distroyed &&
                s.ShipData.ShipType != ShipType.Transport
            ).ToList();

            if (combatShips.Count == 0) return;

            // Calculate average combat ship position
            Vector3 combatLine = Vector3.zero;
            foreach (var ship in combatShips)
            {
                combatLine += ship.transform.position;
            }
            combatLine /= combatShips.Count;

            // Stay 60 units behind the combat line
            Vector3 behindPosition = combatLine - (isSideOne ? Vector3.right : Vector3.left) * 60f;

            float transportSpeed = ShipController.ShipData.maxWarpFactor * 0.4f;
            transform.position = Vector3.MoveTowards(
                transform.position,
                behindPosition,
                transportSpeed * Time.unscaledDeltaTime
            );
        }

        /// <summary>
        /// Check if we need to move to block LOS from enemy to friendly transports
        /// Returns blocking position if needed, Vector3.zero otherwise
        /// </summary>
        private Vector3 CheckForTransportBlocking()
        {
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return Vector3.zero;

            bool isSideOne = transform.position.x < 0;
            List<ShipController> friendlyShips = isSideOne
                ? combatController.CombatData.SideOneShipCons
                : combatController.CombatData.SideTwoShipCons;
            List<ShipController> enemyShips = isSideOne
                ? combatController.CombatData.SideTwoShipCons
                : combatController.CombatData.SideOneShipCons;

            // Find friendly transports
            var transports = friendlyShips.Where(s =>
                s != null &&
                !s.ShipData.Distroyed &&
                s.ShipData.ShipType == ShipType.Transport
            ).ToList();

            if (transports.Count == 0) return Vector3.zero;

            // Check if any enemy has LOS to transports
            foreach (var enemy in enemyShips)
            {
                if (enemy == null || enemy.ShipData.Distroyed) continue;

                foreach (var transport in transports)
                {
                    // Check if this enemy can see this transport
                    Vector3 toTransport = transport.transform.position - enemy.transform.position;

                    // If we can intercept this line of sight, calculate blocking position
                    Vector3 midpoint = enemy.transform.position + toTransport * 0.5f;

                    // If we're close to this midpoint, move to block
                    if (Vector3.Distance(transform.position, midpoint) < 50f)
                    {
                        return midpoint; // Block here
                    }
                }
            }

            return Vector3.zero; // No blocking needed
        }

        /// <summary>
        /// Calculate formation grid position
        /// </summary>
        private Vector3 CalculateFormationPosition(int slot)
        {
            int row = slot / 5;
            int col = slot % 5;

            Vector3 offset = new Vector3(col * FORMATION_SPACING, 0, row * FORMATION_SPACING);
            return formationPosition + offset;
        }

        private void AssignFormationSlot()
        {
            formationSlot = Random.Range(0, 25);
        }
        #endregion

        #region Attack Transports Order - Wide Flanking
        /// <summary>
        /// AttackTransports: Swing wide and fast to flank around combat ships.
        /// Surgical focus on enemy transports.
        /// Bypasses center combat entirely.
        /// </summary>
        private void ExecuteAttackTransports()
        {
            if (currentState != OrderState.FlankingWide)
            {
                currentState = OrderState.FlankingWide;

                if (!hasChosenFlankPath)
                {
                    // Choose flank direction (left or right)
                    isFlankingLeft = Random.value > 0.5f;
                    hasChosenFlankPath = true;

                    // Calculate wide flank position
                    Vector3 flankDirection = isFlankingLeft ? Vector3.forward : Vector3.back;
                    flankTargetPosition = transform.position + flankDirection * FLANK_WIDTH;

                    Debug.Log($"🎯 {ShipController.ShipData.ShipName} ATTACK TRANSPORTS: Flanking {(isFlankingLeft ? "LEFT" : "RIGHT")} (wide swing)");
                }
            }

            // Move along flank path at high speed
            float flankSpeed = ShipController.ShipData.maxWarpFactor * 0.95f;
            transform.position = Vector3.MoveTowards(
                transform.position,
                flankTargetPosition,
                flankSpeed * Time.unscaledDeltaTime
            );

            // Target enemy transports only
            ShipController transportTarget = FindEnemyTransport();
            if (transportTarget != null)
            {
                ShipController.ShipData.TargetThisShipController = transportTarget;
            }
        }

        /// <summary>
        /// Find closest enemy transport
        /// </summary>
        private ShipController FindEnemyTransport()
        {
            var combatController = CombatUIManager.Instance?.CurrentCombatController;
            if (combatController == null) return null;

            bool isSideOne = transform.position.x < 0;
            List<ShipController> enemyShips = isSideOne
                ? combatController.CombatData.SideTwoShipCons
                : combatController.CombatData.SideOneShipCons;

            var transports = enemyShips.Where(s =>
                s != null &&
                !s.ShipData.Distroyed &&
                s.ShipData.ShipType == ShipType.Transport
            ).ToList();

            if (transports.Count == 0) return null;

            return transports.OrderBy(s =>
                Vector3.Distance(transform.position, s.transform.position)
            ).FirstOrDefault();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Get speed factor for this order (used by CombatController movement)
        /// </summary>
        public float GetOrderSpeedFactor()
        {
            switch (CurrentOrder)
            {
                case CombatOrders.Engage:
                    // Speed determined by group (slowest ship in group)
                    return assignedGroup != null ? assignedGroup.groupSpeed / ShipController.ShipData.maxWarpFactor : 1.0f;

                case CombatOrders.Rush:
                    return isTransport ? 0.4f : 1.0f; // Max speed for combat ships

                case CombatOrders.Retreat:
                    if (currentState == OrderState.TurningToRetreat)
                        return 0.0f; // Stopped while turning
                    else
                        return 0.0f; // Warp animation handles movement

                case CombatOrders.Formation:
                    return isTransport ? 0.4f : 0.65f; // Slow defensive movement

                case CombatOrders.AttackTransports:
                    return 0.95f; // Fast flanking

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

            // Find slowest ship
            groupSpeed = ships.Min(s => s.ShipData.maxWarpFactor);
        }
    }
}
