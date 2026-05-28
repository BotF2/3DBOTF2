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
                .Where(s => s != null && !s.ShipData.Distroyed)
                .ToList();

            foreach (var ship in combatData.SideOneShipCons)
            {
                if (ship == null || ship.ShipData.Distroyed) continue;

                // Skip transports - they don't fire
                if (ship.ShipData.ShipType == ShipType.Transport) continue;

                if (side2Alive.Count == 0)
                {
                    Debug.LogWarning($"⚠️ No living Side 2 targets for {ship.ShipData.ShipName}");
                    continue;
                }

                ShipController target = side2Alive
                    .OrderBy(t => Vector3.Distance(ship.transform.position, t.transform.position))
                    .First();

                ship.ShipData.TargetThisShipController = target;
                Debug.Log($"  ✅ Side1 {ship.ShipData.ShipName} → targets {target.ShipData.ShipName}");
                assigned++;
            }

            // Side Two ships target Side One ships
            List<ShipController> side1Alive = combatData.SideOneShipCons
                .Where(s => s != null && !s.ShipData.Distroyed)
                .ToList();

            foreach (var ship in combatData.SideTwoShipCons)
            {
                if (ship == null || ship.ShipData.Distroyed) continue;

                // Skip transports - they don't fire
                if (ship.ShipData.ShipType == ShipType.Transport) continue;

                if (side1Alive.Count == 0)
                {
                    Debug.LogWarning($"⚠️ No living Side 1 targets for {ship.ShipData.ShipName}");
                    continue;
                }

                ShipController target = side1Alive
                    .OrderBy(t => Vector3.Distance(ship.transform.position, t.transform.position))
                    .First();

                ship.ShipData.TargetThisShipController = target;
                Debug.Log($"  ✅ Side2 {ship.ShipData.ShipName} → targets {target.ShipData.ShipName}");
                assigned++;
            }

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

            ShipController newTarget = enemies
                .Where(s => s != null && !s.ShipData.Distroyed)
                .OrderBy(t => Vector3.Distance(ship.transform.position, t.transform.position))
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
