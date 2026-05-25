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

            // Sync order from controller
            CurrentOrder = ShipController.Order;

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
        }

        #region Engage Order - Group Coordination
        private void ExecuteEngage()
        {
            if (isTransport)
            {
                currentState = OrderState.Idle;
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
                currentState = OrderState.Idle;
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
                ExecuteFormationTransport();
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

        private void ExecuteFormationTransport()
        {
            currentState = OrderState.ProtectingTransports;
            bool isSideOne = transform.position.x < 0;
            float sideSign = isSideOne ? -1 : 1;
            Vector3 behindPos = new Vector3(sideSign * 500f, 0, transform.position.z);
            
            transform.position = Vector3.MoveTowards(transform.position, behindPos, ShipController.ShipData.maxWarpFactor * 0.4f * Time.unscaledDeltaTime);
        }

        private Vector3 CheckForTransportBlocking()
        {
            // Simple logic: if an enemy is targeting a friendly transport, move to intercept
            return Vector3.zero; // Placeholder for now
        }

        private void AssignFormationSlot()
        {
            formationSlot = Random.Range(0, 10);
        }

        private void ExecuteRetreat()
        {
            if (currentState != OrderState.TurningToRetreat && currentState != OrderState.WarpingOut)
            {
                currentState = OrderState.TurningToRetreat;
                stateTimer = 0f;
                retreatStartRotation = transform.rotation;
                retreatTargetRotation = Quaternion.Euler(0, transform.eulerAngles.y + 180f, 0);
            }

            if (currentState == OrderState.TurningToRetreat)
            {
                stateTimer += Time.unscaledDeltaTime;
                transform.rotation = Quaternion.Slerp(retreatStartRotation, retreatTargetRotation, stateTimer / RETREAT_TURN_TIME);
                if (stateTimer >= RETREAT_TURN_TIME)
                {
                    currentState = OrderState.WarpingOut;
                    isWarpingOut = true;
                    // Trigger warp out logic (e.g. animation)
                }
            }
        }

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
