using BOTF3D.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Handles target assignment and retargeting for ships in combat.
    /// Manages both initial targeting and dynamic retargeting when targets are destroyed.
    /// </summary>
    public class CombatTargetingSystem
    {
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

            List<ShipController> enemies = isSideOne
                ? combatData.SideTwoShipCons
                : combatData.SideOneShipCons;

            CombatOrders myOrder = isSideOne ? combatData.SideOneOrder : combatData.SideTwoOrder;
            bool canTargetTransports = (myOrder == CombatOrders.AttackTransports);

            List<ShipController> myShips = isSideOne ? combatData.SideOneShipCons : combatData.SideTwoShipCons;

            ShipController newTarget = enemies
                .Where(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy &&
                            (canTargetTransports || s.ShipData.ShipType != ShipType.Transport))
                .OrderBy(e => myShips.Count(s => s != null && s.ShipData != null && s.ShipData.TargetThisShipController == e))
                .ThenBy(t => Vector3.Distance(ship.transform.position, t.transform.position))
                .FirstOrDefault();

            ship.ShipData.TargetThisShipController = newTarget;

            if (newTarget != null)
                Debug.Log($"  🎯 Retargeted {ship.ShipData.ShipName} → {newTarget.ShipData.ShipName}");
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
