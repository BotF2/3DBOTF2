using BOTF3D.Core;
using UnityEngine;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Phase-based movement along the x-axis after warp-in.
    /// Each ship follows: Accelerate(200) → Cruise+Rotate(400) → Decelerate(200) → reverse.
    /// Formation and Retreat movement are owned by CombatOrderStateMachine.
    /// </summary>
    public class ShipMovementController : IController
    {
        public void Initialize() { }
        public void UpdateState() { }

        private readonly CombatData combatData;
        private readonly ShipFormationManager formationManager;

        // Phase distances for Engage / Rush (all along the x-axis)
        // Combat ships 400 units apart (±200), so forward leg = 400 units total
        private const float ACCEL_DIST = 100f;
        private const float CRUISE_DIST = 200f;
        private const float DECEL_DIST = 100f;
        private const float FORWARD_LEG = 400f;   // ACCEL + CRUISE + DECEL
        private const float FULL_CYCLE = 800f;   // forward leg + return leg

        // Attack Transport scout phase distances (longer forward run before rotating)
        // Forward: Accel(100) + PreCruise(300) + Rotate(100) + Decel(100) = 600
        private const float AT_PRE_DIST = 300f;   // constant-speed stretch before rotation
        private const float AT_ROTATE_DIST = 100f;   // rotation while still moving forward
        private const float AT_FORWARD_LEG = 600f;   // ACCEL + AT_PRE + AT_ROTATE + DECEL
        private const float AT_FULL_CYCLE = 1000f;  // AT_FORWARD_LEG + FORWARD_LEG (return)

        // Speed multipliers on ship.maxWarpFactor  (tune via testing)
        private const float ENGAGE_SPEED = 12f;
        private const float RUSH_SPEED = 18f;
        private const float AT_HEAVY_SPEED = 8f;
        private const float AT_SCOUT_SPEED = 16f;
        private const float TRANSPORT_AVOID_SPEED = 3f;

        public ShipMovementController(CombatData data, ShipFormationManager formation)
        {
            combatData = data;
            formationManager = formation;
        }

        public void MoveShipBasedOnOrder(ShipController ship)
        {
            if (ship.ShipData.Distroyed) return;

            var osm = ship.GetComponent<CombatOrderStateMachine>();
            if (osm == null || osm.IsWarpingOut() || osm.IsVulnerable()) return;

            bool isSideOne = combatData.SideOneShipCons.Contains(ship);
            CombatOrders order = isSideOne ? combatData.SideOneOrder : combatData.SideTwoOrder;

            if (ship.ShipData.ShipType == ShipType.Transport)
            {
                MoveTransport(ship, osm);
                return;
            }

            CombatOrders enemyOrder = isSideOne ? combatData.SideTwoOrder : combatData.SideOneOrder;
            bool shipsWillPass = enemyOrder != CombatOrders.Retreat &&
                                  enemyOrder != CombatOrders.Formation;
            bool holdAtEnd = order == CombatOrders.Rush && enemyOrder == CombatOrders.Retreat;

            switch (order)
            {
                case CombatOrders.Engage:
                    {
                        float groupSpeed = osm.assignedGroup?.groupSpeed ?? ship.ShipData.maxWarpFactor;
                        MovePhased(ship, isSideOne, groupSpeed * ENGAGE_SPEED, FULL_CYCLE, shipsWillPass);
                        break;
                    }
                case CombatOrders.Rush:
                    MovePhased(ship, isSideOne, ship.ShipData.maxWarpFactor * RUSH_SPEED, FULL_CYCLE, shipsWillPass, holdAtEnd);
                    break;

                case CombatOrders.AttackTransports:
                    {
                        bool isFlankShip = ship.ShipData.ShipType == ShipType.Scout ||
                                           ship.ShipData.ShipType == ShipType.Destroyer;
                        if (isFlankShip)
                            MovePhasedAT(ship, isSideOne, ship.ShipData.maxWarpFactor * AT_SCOUT_SPEED);
                        else
                            MovePhased(ship, isSideOne, ship.ShipData.maxWarpFactor * AT_HEAVY_SPEED, FULL_CYCLE);
                        break;
                    }
                // Formation and Retreat are handled entirely by CombatOrderStateMachine
                default:
                    break;
            }
        }

        // Standard phase movement: Accel → Cruise+Rotate → Decel → reverse
        private void MovePhased(ShipController ship, bool isSideOne, float maxSpeed, float cycleLen,
                                bool doRotation = true, bool holdAtEnd = false)
        {
            var t = GetOrInit(ship, isSideOne, cycleLen);
            float dt = Time.unscaledDeltaTime;

            // Rush vs Retreat: freeze at end of forward leg so ships hold position and fire
            if (holdAtEnd && t.travelDist >= FORWARD_LEG)
            {
                t.currentSpeed = 0f;
                t.travelDist = FORWARD_LEG;
                return;
            }

            float d = t.travelDist;
            bool onReturn = d >= FORWARD_LEG;
            float legDist = onReturn ? d - FORWARD_LEG : d;

            // Snap to leg boundary when nearly stopped in the decel zone
            if (legDist >= ACCEL_DIST + CRUISE_DIST && t.currentSpeed < 0.5f)
            {
                t.currentSpeed = 0f;
                t.travelDist = onReturn ? 0f : FORWARD_LEG;
                t.cruiseRotStarted = false;
                return;
            }

            // Acceleration rate so accel and decel each cover exactly ACCEL_DIST / DECEL_DIST
            float accelRate = maxSpeed * maxSpeed / (2f * ACCEL_DIST);

            float targetSpeed = legDist < ACCEL_DIST + CRUISE_DIST ? maxSpeed : 0f;
            t.currentSpeed = Mathf.MoveTowards(t.currentSpeed, targetSpeed, accelRate * dt);

            int sign = isSideOne ? 1 : -1;
            if (onReturn) sign = -sign;
            if (doRotation)
                HandleCruiseRotation(ship, t, legDist, ACCEL_DIST, CRUISE_DIST, sign);
            ship.transform.position += Vector3.right * sign * t.currentSpeed * dt;
            t.travelDist += t.currentSpeed * dt;

            // Reset rotation flag at each leg transition
            if (!onReturn && t.travelDist >= FORWARD_LEG)
                t.cruiseRotStarted = false;
            if (t.travelDist >= cycleLen)
            {
                t.travelDist -= cycleLen;
                t.cruiseRotStarted = false;
            }
        }

        // Attack Transport scout movement: longer forward run before the 180° rotation
        private void MovePhasedAT(ShipController ship, bool isSideOne, float maxSpeed)
        {
            var t = GetOrInit(ship, isSideOne, AT_FULL_CYCLE);
            float dt = Time.unscaledDeltaTime;

            float d = t.travelDist;
            bool onReturn = d >= AT_FORWARD_LEG;
            float legDist = onReturn ? d - AT_FORWARD_LEG : d;

            if (legDist >= ACCEL_DIST + CRUISE_DIST && t.currentSpeed < 0.5f)
            {
                t.currentSpeed = 0f;
                t.travelDist = onReturn ? 0f : AT_FORWARD_LEG;
                t.cruiseRotStarted = false;
                return;
            }

            float accelRate = maxSpeed * maxSpeed / (2f * ACCEL_DIST);

            float targetSpeed;
            float rotStart;
            float rotDist;

            if (!onReturn)
            {
                // Forward: constant speed through accel+precruise+rotate; decel at the end
                float fullCruise = ACCEL_DIST + AT_PRE_DIST + AT_ROTATE_DIST; // 1000
                targetSpeed = legDist < fullCruise ? maxSpeed : 0f;
                rotStart = ACCEL_DIST + AT_PRE_DIST;  // 800
                rotDist = AT_ROTATE_DIST;             // 200
            }
            else
            {
                // Return: standard 200/400/200 phases (same as Engage/Rush return)
                targetSpeed = legDist < ACCEL_DIST + CRUISE_DIST ? maxSpeed : 0f;
                rotStart = ACCEL_DIST;
                rotDist = CRUISE_DIST;
            }

            t.currentSpeed = Mathf.MoveTowards(t.currentSpeed, targetSpeed, accelRate * dt);
            HandleCruiseRotation(ship, t, legDist, rotStart, rotDist);

            int sign = isSideOne ? 1 : -1;
            if (onReturn) sign = -sign;
            ship.transform.position += Vector3.right * sign * t.currentSpeed * dt;
            t.travelDist += t.currentSpeed * dt;

            if (!onReturn && t.travelDist >= AT_FORWARD_LEG)
                t.cruiseRotStarted = false;
            if (t.travelDist >= AT_FULL_CYCLE)
            {
                t.travelDist -= AT_FULL_CYCLE;
                t.cruiseRotStarted = false;
            }
        }

        // Rotate 180° on Y during the cruise sub-leg; resets automatically before each new cruise.
        // moveDir != 0: progress driven by x-position so ships are at 90° as they cross x=0.
        // moveDir == 0: progress driven by distance traveled (AT scout forward leg).
        private void HandleCruiseRotation(ShipController ship, ShipPhaseTracker t,
                                          float legDist, float rotStart, float rotDist,
                                          int moveDir = 0)
        {
            if (legDist < rotStart)
            {
                t.cruiseRotStarted = false;
                return;
            }
            if (legDist >= rotStart + rotDist) return;

            if (!t.cruiseRotStarted)
            {
                t.cruiseRotStarted = true;
                t.rotFrom = ship.transform.rotation;
                t.rotTo = Quaternion.Euler(
                    ship.transform.eulerAngles.x,
                    ship.transform.eulerAngles.y + 180f,
                    ship.transform.eulerAngles.z);
            }

            float progress;
            if (moveDir != 0)
            {
                // x=0 is the crossing point; rotDist/2 is half the rotation zone width.
                // Guarantees 90° rotation at x=0 regardless of each ship's speed.
                float xPos = ship.transform.position.x;
                progress = Mathf.Clamp01((xPos * moveDir + rotDist * 0.5f) / rotDist);
            }
            else
            {
                progress = (legDist - rotStart) / rotDist;
            }
            ship.transform.rotation = Quaternion.Slerp(t.rotFrom, t.rotTo, progress);
        }

        // Transports drift slowly away from any attacker targeting them.
        private void MoveTransport(ShipController ship, CombatOrderStateMachine osm)
        {
            if (!osm.IsAvoidingAttack()) return;
            ShipController attacker = osm.GetAttackerTargetingMe();
            if (attacker == null) return;

            Vector3 away = (ship.transform.position - attacker.transform.position).normalized;
            ship.transform.position += away * ship.ShipData.maxWarpFactor * TRANSPORT_AVOID_SPEED
                                       * Time.unscaledDeltaTime;
        }

        private ShipPhaseTracker GetOrInit(ShipController ship, bool isSideOne, float cycleLen)
        {
            if (!ship.TryGetComponent<ShipPhaseTracker>(out var t))
                t = ship.gameObject.AddComponent<ShipPhaseTracker>();

            if (!t.initialized)
            {
                t.sideDir = isSideOne ? 1 : -1;
                t.cycleLength = cycleLen;
                t.initialized = true;
            }
            return t;
        }
    }

    /// <summary>
    /// Per-ship state for phase-based x-axis movement.
    /// </summary>
    public class ShipPhaseTracker : MonoBehaviour
    {
        public bool initialized = false;
        public int sideDir = 1;      // +1 = side 1 (moves right), -1 = side 2
        public float currentSpeed = 0f;
        public float travelDist = 0f;     // cumulative travel; wraps at cycleLength
        public float cycleLength = 1600f;
        public bool cruiseRotStarted = false;
        public Quaternion rotFrom;
        public Quaternion rotTo;
    }

    /// <summary>
    /// Legacy velocity component kept for backward compatibility.
    /// </summary>
    public class ShipVelocity : MonoBehaviour
    {
        public Vector3 currentVelocity;
    }
}
