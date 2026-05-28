using BOTF3D.Core;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Handles ship movement logic based on combat orders.
    /// Implements delta-v physics for realistic ship acceleration and coasting.
    /// </summary>
    public class ShipMovementController : IController
    {
        public void Initialize() { }
        public void UpdateState() { }
        private readonly CombatData combatData;
        private readonly ShipFormationManager formationManager;

        public ShipMovementController(CombatData data, ShipFormationManager formation)
        {
            combatData = data;
            formationManager = formation;
        }

        /// <summary>
        /// Move ship based on combat order with simplified delta-v physics
        /// </summary>
        public void MoveShipBasedOnOrder(ShipController ship)
        {
            if (ship.ShipData.Distroyed) return;

            var orderStateMachine = ship.GetComponent<CombatOrderStateMachine>();
            if (orderStateMachine == null || orderStateMachine.IsWarpingOut()) return;

            // Base speed: 10x multiplier for fast visual combat
            float baseSpeed = ship.ShipData.maxWarpFactor * 10f;
            float speedFactor = orderStateMachine.GetOrderSpeedFactor();
            float maxSpeed = baseSpeed * speedFactor;

            // Get or initialize velocity component
            if (!ship.TryGetComponent<ShipVelocity>(out var velocity))
            {
                velocity = ship.gameObject.AddComponent<ShipVelocity>();
                velocity.currentVelocity = Vector3.zero;
            }

            // Get target positions based on order
            Vector3 movementTarget = GetTargetPositionForShip(ship);
            Vector3 rotationTarget = movementTarget; // Default: face movement target
            bool isSideOne = combatData.SideOneShipCons.Contains(ship);
            CombatOrders order = isSideOne ? combatData.SideOneOrder : combatData.SideTwoOrder;

            // For Rush: Rotation target should be the actual enemy ship, not the far-off movement target
            if (order == CombatOrders.Rush)
            {
                ShipController shipTarget = ship.ShipData.TargetThisShipController;
                if (shipTarget != null && !shipTarget.ShipData.Distroyed)
                {
                    rotationTarget = shipTarget.transform.position;
                }
            }

            // Formation small adjustments: instant positioning
            if (order == CombatOrders.Formation && Vector3.Distance(ship.transform.position, movementTarget) < 50f)
            {
                ship.transform.position = Vector3.MoveTowards(ship.transform.position, movementTarget, maxSpeed * Time.unscaledDeltaTime);
                velocity.currentVelocity = Vector3.zero;
                return;
            }

            // Determine directions
            Vector3 movementDirection;
            if (movementTarget != Vector3.zero)
            {
                movementDirection = (movementTarget - ship.transform.position).normalized;
            }
            else
            {
                // Default: move toward enemy side
                movementDirection = isSideOne ? Vector3.right : Vector3.left;
            }

            Vector3 rotationDirection = movementDirection;

            // ✅ Transport Avoidance Logic
            if (ship.ShipData.ShipType == ShipType.Transport && orderStateMachine.IsAvoidingAttack())
            {
                ShipController attacker = orderStateMachine.GetAttackerTargetingMe();
                if (attacker != null)
                {
                    Vector3 awayDirection = (ship.transform.position - attacker.transform.position).normalized;
                    // Turn 30 degrees away from the direct "away" vector
                    rotationDirection = Quaternion.Euler(0, 30f, 0) * awayDirection;
                    movementDirection = rotationDirection;
                }
            }

            // ✅ Rush and Engage: Turn around (180 deg) if passed target or enemy line
            if (order == CombatOrders.Rush || order == CombatOrders.Engage)
            {
                ShipController shipTarget = ship.ShipData.TargetThisShipController;
                bool hasPassedTarget = false;

                if (shipTarget != null && !shipTarget.ShipData.Distroyed)
                {
                    // Check if we passed the actual target on X
                    hasPassedTarget = isSideOne ? (ship.transform.position.x > shipTarget.transform.position.x) : (ship.transform.position.x < shipTarget.transform.position.x);

                    if (hasPassedTarget)
                    {
                        // Face 180° opposite of start (Side 1 faces Left, Side 2 faces Right)
                        rotationDirection = isSideOne ? Vector3.left : Vector3.right;
                    }
                    else if (order == CombatOrders.Rush)
                    {
                        // Rush keeps aiming at specific ship until passed
                        rotationDirection = (shipTarget.transform.position - ship.transform.position).normalized;
                    }
                }
                else
                {
                    // Target destroyed: check if we passed the center to stay turned around
                    hasPassedTarget = isSideOne ? (ship.transform.position.x > 0) : (ship.transform.position.x < 0);
                    if (hasPassedTarget)
                    {
                        rotationDirection = isSideOne ? Vector3.left : Vector3.right;
                    }
                }
            }
            else if (rotationTarget != Vector3.zero && rotationTarget != movementTarget)
            {
                rotationDirection = (rotationTarget - ship.transform.position).normalized;
            }

            // Calculate acceleration (faster ships accelerate faster)
            float acceleration = maxSpeed * 2f; // Reach max speed in 0.5 seconds

            // Check if ship is facing the desired rotation direction
            float facingAngle = Vector3.Angle(ship.transform.forward, rotationDirection);
            bool isFacingTarget = facingAngle < 30f;

            // Rush order: maintain speed and coast past targets
            if (order == CombatOrders.Rush)
            {
                // In Rush, we ALWAYS accelerate or maintain max speed
                if (velocity.currentVelocity.magnitude < maxSpeed * 0.9f)
                {
                    // Speed up to max speed
                    velocity.currentVelocity = Vector3.MoveTowards(
                        velocity.currentVelocity,
                        movementDirection * maxSpeed,
                        acceleration * Time.unscaledDeltaTime
                    );
                }
                else
                {
                    // Coasting: maintain speed magnitude, but rotate velocity toward movement direction
                    // Using RotateTowards to prevent passing through zero magnitude (no stopping)
                    float currentSpeed = velocity.currentVelocity.magnitude;
                    velocity.currentVelocity = Vector3.RotateTowards(
                        velocity.currentVelocity,
                        movementDirection * currentSpeed,
                        2f * Time.unscaledDeltaTime,
                        0f
                    ).normalized * currentSpeed;
                }
            }
            else
            {
                // Other orders: normal acceleration/deceleration toward movement direction
                float movementAngle = Vector3.Angle(ship.transform.forward, movementDirection);
                bool isFacingMovement = movementAngle < 30f;

                if (isFacingMovement)
                {
                    // Accelerate toward movement target
                    velocity.currentVelocity = Vector3.MoveTowards(
                        velocity.currentVelocity,
                        movementDirection * maxSpeed,
                        acceleration * Time.unscaledDeltaTime
                    );
                }
                else
                {
                    // Turning: coast and gradually reduce speed
                    velocity.currentVelocity *= 0.98f; // Space friction
                }
            }

            // Apply velocity to position
            ship.transform.position += velocity.currentVelocity * Time.unscaledDeltaTime;

            // Smoothly rotate to face target direction (for Rush, keep turning toward enemy while coasting)
            if (order == CombatOrders.Rush)
            {
                // Always rotate toward the rotationDirection (the actual enemy ship)
                if (rotationDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(rotationDirection);
                    ship.transform.rotation = Quaternion.Slerp(
                        ship.transform.rotation,
                        targetRotation,
                        3f * Time.unscaledDeltaTime
                    );
                }
            }
            else
            {
                // Other orders: face movement direction
                if (velocity.currentVelocity.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(velocity.currentVelocity);
                    ship.transform.rotation = Quaternion.Slerp(
                        ship.transform.rotation,
                        targetRotation,
                        5f * Time.unscaledDeltaTime
                    );
                }
            }
        }

        /// <summary>
        /// Get target position for ship based on combat order
        /// </summary>
        private Vector3 GetTargetPositionForShip(ShipController ship)
        {
            bool isSideOne = combatData.SideOneShipCons.Contains(ship);
            CombatOrders order = isSideOne ? combatData.SideOneOrder : combatData.SideTwoOrder;

            switch (order)
            {
                case CombatOrders.AttackTransports:
                    return formationManager.GetAttackTransportsPosition(ship, isSideOne, combatData);

                case CombatOrders.Formation:
                    return formationManager.GetFormationPosition(ship, isSideOne);

                case CombatOrders.Retreat:
                    return formationManager.GetRetreatPosition(ship, isSideOne);

                case CombatOrders.Rush:
                    return formationManager.GetRushPosition(ship, isSideOne);

                case CombatOrders.Engage:
                default:
                    ShipController target = ship.ShipData.TargetThisShipController;
                    if (target != null && !target.ShipData.Distroyed)
                    {
                        return target.transform.position;
                    }
                    return Vector3.zero;
            }
        }
    }

    /// <summary>
    /// Simple velocity component for delta-v movement
    /// </summary>
    public class ShipVelocity : MonoBehaviour
    {
        public Vector3 currentVelocity;
    }
}
