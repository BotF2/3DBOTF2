using BOTF3D.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Handles target assignment and re-targeting for ships in combat.
    /// Manages both initial targeting and dynamic re-targeting when targets are destroyed.
    /// </summary>
    public class CombatTargetingSystem
    {
        // Fraction of Formation ships that lock onto the focus target; the rest spread normally.
        // Adjust this value to tune Formation focus-fire strength (1.0 = all ships focus).
        public const float FOCUS_FIRE_RATIO = 1f;

        private readonly CombatData combatData;

        public CombatTargetingSystem(CombatData data)
        {
            combatData = data;
        }

        /// <summary>
        /// Assign each ship a target on the opposing side.
        /// Called once after warp-in completes, before weapon fire starts.
        /// </summary>
        public void AssignTargetsToAllShips()
        {
            Debug.Log("🎯 Assigning targets to all ships...");

            int assigned = 0;

            // Side One ships target Side Two ships
            List<ShipController> side2Alive = combatData.SideTwoShipCons
                .Where(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy)
                .ToList();

            List<ShipController> side1Attackers = combatData.SideOneShipCons
                .Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType != ShipType.Transport)
                .ToList();

            if (side1Attackers.Count > 0 && side2Alive.Count > 0)
            {
                bool canTargetTransports = (combatData.SideOneOrder == CombatOrders.AttackTransports);
                List<ShipController> s1ValidTargets = side2Alive
                    .Where(t => canTargetTransports || t.ShipData.ShipType != ShipType.Transport)
                    .ToList();

                if (s1ValidTargets.Count > 0)
                {
                    bool side1FocusFire = combatData.SideOneOrder == CombatOrders.Formation &&
                                         (combatData.SideTwoOrder == CombatOrders.Engage || combatData.SideTwoOrder == CombatOrders.Rush);

                    if (side1FocusFire)
                    {
                        ShipController focusTarget = s1ValidTargets
                            .OrderBy(t => t.ShipData.ShieldHealth + t.ShipData.HullHealth)
                            .First();

                        int focusCount = Mathf.CeilToInt(side1Attackers.Count * FOCUS_FIRE_RATIO);

                        Vector3 s1Center = side1Attackers.Aggregate(Vector3.zero, (sum, s) => sum + s.transform.position) / side1Attackers.Count;
                        List<ShipController> s1Spread = s1ValidTargets.OrderBy(t => Vector3.Distance(s1Center, t.transform.position)).ToList();

                        for (int i = 0; i < side1Attackers.Count; i++)
                        {
                            side1Attackers[i].ShipData.TargetThisShipController =
                                i < focusCount ? focusTarget : s1Spread[i % s1Spread.Count];
                            assigned++;
                        }
                        Debug.Log($"🎯 Formation focus fire (Side 1): {focusCount}/{side1Attackers.Count} ships → {focusTarget.ShipData.ShipName}");
                    }
                    else
                    {
                        Vector3 s1Center = side1Attackers.Aggregate(Vector3.zero, (sum, s) => sum + s.transform.position) / side1Attackers.Count;
                        s1ValidTargets = s1ValidTargets.OrderBy(t => Vector3.Distance(s1Center, t.transform.position)).ToList();

                        for (int i = 0; i < side1Attackers.Count; i++)
                        {
                            ShipController target = s1ValidTargets[i % s1ValidTargets.Count];
                            side1Attackers[i].ShipData.TargetThisShipController = target;
                            Debug.Log($"  ✅ Side1 {side1Attackers[i].ShipData.ShipName} → targets {target.ShipData.ShipName}");
                            assigned++;
                        }
                    }
                }
            }
            else if (side2Alive.Count == 0)
                Debug.LogWarning("⚠️ No living Side 2 targets");

            // Side Two ships target Side One ships
            List<ShipController> side1Alive = combatData.SideOneShipCons
                .Where(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy)
                .ToList();

            List<ShipController> side2Attackers = combatData.SideTwoShipCons
                .Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType != ShipType.Transport)
                .ToList();

            if (side2Attackers.Count > 0 && side1Alive.Count > 0)
            {
                bool canTargetTransports = (combatData.SideTwoOrder == CombatOrders.AttackTransports);
                List<ShipController> s2ValidTargets = side1Alive
                    .Where(t => canTargetTransports || t.ShipData.ShipType != ShipType.Transport)
                    .ToList();

                if (s2ValidTargets.Count > 0)
                {
                    bool side2FocusFire = combatData.SideTwoOrder == CombatOrders.Formation &&
                                         (combatData.SideOneOrder == CombatOrders.Engage || combatData.SideOneOrder == CombatOrders.Rush);

                    if (side2FocusFire)
                    {
                        ShipController focusTarget = s2ValidTargets
                            .OrderBy(t => t.ShipData.ShieldHealth + t.ShipData.HullHealth)
                            .First();

                        int focusCount = Mathf.CeilToInt(side2Attackers.Count * FOCUS_FIRE_RATIO);

                        Vector3 s2Center = side2Attackers.Aggregate(Vector3.zero, (sum, s) => sum + s.transform.position) / side2Attackers.Count;
                        List<ShipController> s2Spread = s2ValidTargets.OrderBy(t => Vector3.Distance(s2Center, t.transform.position)).ToList();

                        for (int i = 0; i < side2Attackers.Count; i++)
                        {
                            side2Attackers[i].ShipData.TargetThisShipController =
                                i < focusCount ? focusTarget : s2Spread[i % s2Spread.Count];
                            assigned++;
                        }
                        Debug.Log($"🎯 Formation focus fire (Side 2): {focusCount}/{side2Attackers.Count} ships → {focusTarget.ShipData.ShipName}");
                    }
                    else
                    {
                        Vector3 s2Center = side2Attackers.Aggregate(Vector3.zero, (sum, s) => sum + s.transform.position) / side2Attackers.Count;
                        s2ValidTargets = s2ValidTargets.OrderBy(t => Vector3.Distance(s2Center, t.transform.position)).ToList();

                        for (int i = 0; i < side2Attackers.Count; i++)
                        {
                            ShipController target = s2ValidTargets[i % s2ValidTargets.Count];
                            side2Attackers[i].ShipData.TargetThisShipController = target;
                            Debug.Log($"  ✅ Side2 {side2Attackers[i].ShipData.ShipName} → targets {target.ShipData.ShipName}");
                            assigned++;
                        }
                    }
                }
            }
            else if (side1Alive.Count == 0)
                Debug.LogWarning("⚠️ No living Side 1 targets");

            Debug.Log($"🎯 Target assignment complete: {assigned} ships assigned targets");
        }

        /// <summary>
        /// Reassign a new living target to a ship when its current target is destroyed.
        /// </summary>
        public void ReassignTarget(ShipController ship)
        {
            bool isSideOne = combatData.SideOneShipCons.Contains(ship);

            CombatOrders myOrder = isSideOne ? combatData.SideOneOrder : combatData.SideTwoOrder;
            CombatOrders enemyOrder = isSideOne ? combatData.SideTwoOrder : combatData.SideOneOrder;

            List<ShipController> enemies = isSideOne ? combatData.SideTwoShipCons : combatData.SideOneShipCons;
            List<ShipController> myShips = isSideOne ? combatData.SideOneShipCons : combatData.SideTwoShipCons;
            bool canTargetTransports = (myOrder == CombatOrders.AttackTransports);

            var validEnemies = enemies
                .Where(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy &&
                            (canTargetTransports || s.ShipData.ShipType != ShipType.Transport))
                .ToList();

            // Formation focus fire: redirect every ally to the lowest-HP enemy so the
            // kill chain continues unbroken after a target is destroyed.
            if (myOrder == CombatOrders.Formation &&
                (enemyOrder == CombatOrders.Engage || enemyOrder == CombatOrders.Rush))
            {
                ShipController focusTarget = validEnemies
                    .OrderBy(t => t.ShipData.ShieldHealth + t.ShipData.HullHealth)
                    .FirstOrDefault();

                var activeAllies = myShips
                    .Where(a => a != null && !a.ShipData.Distroyed && a.gameObject.activeInHierarchy)
                    .ToList();

                int focusCount = Mathf.CeilToInt(activeAllies.Count * FOCUS_FIRE_RATIO);

                for (int i = 0; i < activeAllies.Count; i++)
                {
                    if (i < focusCount)
                        activeAllies[i].ShipData.TargetThisShipController = focusTarget;
                    // Remaining 20% keep their current target; FindFallbackEnemy handles
                    // them individually if that target is also gone.
                }

                if (focusTarget != null)
                    Debug.Log($"  🎯 Formation focus re-target: {focusCount}/{activeAllies.Count} Side {(isSideOne ? 1 : 2)} ships → {focusTarget.ShipData.ShipName}");
                return;
            }

            // Normal spread re-targeting
            ShipController newTarget = validEnemies
                .OrderBy(e => myShips.Count(s => s != null && s.ShipData != null && s.ShipData.TargetThisShipController == e))
                .ThenBy(t => Vector3.Distance(ship.transform.position, t.transform.position))
                .FirstOrDefault();

            ship.ShipData.TargetThisShipController = newTarget;

            if (newTarget != null)
                Debug.Log($"  🎯 Re-targeted {ship.ShipData.ShipName} → {newTarget.ShipData.ShipName}");
        }

        /// <summary>
        /// Stop all weapon fire by nullifying targets
        /// </summary>
        public void StopAllWeaponFire()
        {
            foreach (var ship in combatData.SideOneShipCons)
            {
                if (ship != null && ship.ShipData != null)
                {
                    ship.ShipData.TargetThisShipController = null;
                }
            }
            foreach (var ship in combatData.SideTwoShipCons)
            {
                if (ship != null && ship.ShipData != null)
                {
                    ship.ShipData.TargetThisShipController = null;
                }
            }
            Debug.Log("🛑 All weapon fire stopped");
        }
    }
}
