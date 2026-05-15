using BOTF3D.GamePlay;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Checks if ships block line of sight between attacker and target.
    /// Used by Formation order to protect transports.
    /// Implements geometric blocking based on ship positions.
    /// </summary>
    public static class LineOfSightBlocker
    {
        // ✅ Ship collision radius for blocking calculations
        private const float SHIP_BLOCKING_RADIUS = 15f;

        /// <summary>
        /// Check if a shot from attacker to target is blocked by any friendly ships
        /// </summary>
        /// <param name="attackerPos">Position of attacking ship</param>
        /// <param name="targetPos">Position of target ship</param>
        /// <param name="potentialBlockers">List of ships that could block (friendly to target)</param>
        /// <param name="blockingRadius">Distance from firing line to consider blocking</param>
        /// <returns>True if shot is blocked, false if clear</returns>
        public static bool IsLineOfSightBlocked(
            Vector3 attackerPos,
            Vector3 targetPos,
            List<ShipController> potentialBlockers,
            float blockingRadius = SHIP_BLOCKING_RADIUS)
        {
            return GetBlockingShip(attackerPos, targetPos, potentialBlockers, blockingRadius) != null;
        }

        /// <summary>
        /// Find which ship (if any) blocks the shot from attacker to target
        /// </summary>
        /// <returns>The blocking ship, or null if no blocker</returns>
        public static ShipController GetBlockingShip(
            Vector3 attackerPos,
            Vector3 targetPos,
            List<ShipController> potentialBlockers,
            float blockingRadius = SHIP_BLOCKING_RADIUS)
        {
            if (potentialBlockers == null || potentialBlockers.Count == 0)
                return null;

            Vector3 fireDirection = (targetPos - attackerPos).normalized;
            float fireDistance = Vector3.Distance(attackerPos, targetPos);

            foreach (var blocker in potentialBlockers)
            {
                if (blocker == null || blocker.ShipData == null || blocker.ShipData.Distroyed)
                    continue;

                // ✅ Don't block shots to self
                Vector3 blockerPos = blocker.transform.position;
                if (Vector3.Distance(blockerPos, targetPos) < 1f)
                    continue;

                // ✅ Check if blocker is between attacker and target
                Vector3 toBlocker = blockerPos - attackerPos;
                float blockerDistance = toBlocker.magnitude;

                // Skip if blocker is behind attacker or past target
                if (blockerDistance < 5f || blockerDistance > fireDistance - 5f)
                    continue;

                // ✅ Calculate closest point on firing line to blocker
                Vector3 closestPointOnLine = attackerPos + fireDirection * blockerDistance;
                float distanceToLine = Vector3.Distance(blockerPos, closestPointOnLine);

                if (distanceToLine < blockingRadius)
                {
                    Debug.Log($"🛡️ {blocker.ShipData.ShipName} BLOCKS shot (distance to line: {distanceToLine:F1}u)");
                    return blocker; // This ship blocks!
                }
            }

            return null; // Clear shot
        }

        /// <summary>
        /// Check if Formation ships successfully block AttackTransports flankers
        /// </summary>
        public static bool DoesFormationBlockFlankingShot(
            ShipController attacker,
            ShipController target,
            List<ShipController> formationShips)
        {
            if (attacker == null || target == null || formationShips == null)
                return false;

            // ✅ Formation blocks if any ship is between flanker and transport
            return GetBlockingShip(
                attacker.transform.position,
                target.transform.position,
                formationShips,
                SHIP_BLOCKING_RADIUS
            ) != null;
        }
    }
}
