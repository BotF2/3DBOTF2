using BOTF3D.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Manages ship groups for Engage order.
    /// Groups ships by similar speed into 2-3 ship groups for coordinated attacks.
    /// </summary>
    public class ShipGroupManager
    {
        private readonly CombatData combatData;

        public List<ShipGroup> sideOneGroups = new List<ShipGroup>();
        public List<ShipGroup> sideTwoGroups = new List<ShipGroup>();
        public bool groupsInitialized = false;
        public ShipGroupManager(CombatData data)
        {
            combatData = data;
        }


        /// <summary>
        /// Initialize ship groups for Engage order
        /// </summary>
        public void InitializeShipGroupsForEngage()
        {
            if (groupsInitialized) return;

            Debug.Log("=== Initializing Ship Groups for Engage Order ===");

            // Side One Groups
            if (combatData.SideOneOrder == CombatOrders.Engage)
            {
                sideOneGroups = CreateShipGroups(combatData.SideOneShipCons);
                AssignGroupsToShips(sideOneGroups);
                Debug.Log($"  Side 1: Created {sideOneGroups.Count} groups");
            }

            // Side Two Groups
            if (combatData.SideTwoOrder == CombatOrders.Engage)
            {
                sideTwoGroups = CreateShipGroups(combatData.SideTwoShipCons);
                AssignGroupsToShips(sideTwoGroups);
                Debug.Log($"  Side 2: Created {sideTwoGroups.Count} groups");
            }

            groupsInitialized = true;
        }

        /// <summary>
        /// Create groups of 2-3 ships with similar speeds
        /// </summary>
        private List<ShipGroup> CreateShipGroups(List<ShipController> ships)
        {
            List<ShipGroup> groups = new List<ShipGroup>();

            // Filter out transports and destroyed ships
            var combatShips = ships.Where(s =>
                s != null &&
                !s.ShipData.Distroyed &&
                s.ShipData.ShipType != ShipType.Transport
            ).ToList();

            if (combatShips.Count == 0) return groups;

            // Sort by speed (maxWarpFactor)
            combatShips = combatShips.OrderBy(s => s.ShipData.maxWarpFactor).ToList();

            // Create groups of 2-3 ships
            for (int i = 0; i < combatShips.Count; i += 3)
            {
                ShipGroup group = new ShipGroup();

                // Add 2-3 ships to group
                int groupSize = Mathf.Min(3, combatShips.Count - i);
                for (int j = 0; j < groupSize; j++)
                {
                    if (i + j < combatShips.Count)
                    {
                        group.ships.Add(combatShips[i + j]);
                    }
                }

                // Calculate group speed (slowest ship)
                group.RecalculateGroupSpeed();

                groups.Add(group);

                Debug.Log($"    Group {groups.Count}: {group.ships.Count} ships, speed={group.groupSpeed:F1}");
            }

            return groups;
        }

        /// <summary>
        /// Assign groups to ships (set their CombatOrderStateMachine.assignedGroup)
        /// </summary>
        private void AssignGroupsToShips(List<ShipGroup> groups)
        {
            int groupId = 0;
            foreach (var group in groups)
            {
                foreach (var ship in group.ships)
                {
                    var orderStateMachine = ship.GetComponent<CombatOrderStateMachine>();
                    if (orderStateMachine != null)
                    {
                        orderStateMachine.assignedGroup = group;
                        orderStateMachine.groupId = groupId;
                    }
                }
                groupId++;
            }
        }

        /// <summary>
        /// Update group targets for Engage order
        /// Each group focuses fire on a common target
        /// </summary>
        public void UpdateGroupTargets()
        {
            // Update Side One groups
            foreach (var group in sideOneGroups)
            {
                if (group.ships.Count == 0) continue;

                Vector3 groupCenter = CalculateGroupCenter(group.ships);
                ShipController closestEnemy = FindClosestEnemyToPosition(groupCenter, combatData.SideTwoShipCons);
                group.commonTarget = closestEnemy;

                // Assign to all ships in group
                foreach (var ship in group.ships)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        ship.ShipData.TargetThisShipController = closestEnemy;
                    }
                }
            }

            // Update Side Two groups
            foreach (var group in sideTwoGroups)
            {
                if (group.ships.Count == 0) continue;

                Vector3 groupCenter = CalculateGroupCenter(group.ships);
                ShipController closestEnemy = FindClosestEnemyToPosition(groupCenter, combatData.SideOneShipCons);
                group.commonTarget = closestEnemy;

                foreach (var ship in group.ships)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        ship.ShipData.TargetThisShipController = closestEnemy;
                    }
                }
            }
        }

        /// <summary>
        /// Update individual groups (called from LateUpdate)
        /// </summary>
        public void UpdateEngageGroups()
        {
            if (sideOneGroups != null) foreach (var g in sideOneGroups) UpdateGroupTarget(g, true);
            if (sideTwoGroups != null) foreach (var g in sideTwoGroups) UpdateGroupTarget(g, false);
        }

        /// <summary>
        /// Update a single group's target
        /// </summary>
        private void UpdateGroupTarget(ShipGroup group, bool isSideOne)
        {
            group.ships.RemoveAll(s => s == null || s.ShipData.Distroyed);
            if (group.ships.Count == 0) return;

            if (group.commonTarget == null || group.commonTarget.ShipData.Distroyed)
            {
                Vector3 center = CalculateGroupCenter(group.ships);

                List<ShipController> enemies = isSideOne ? combatData.SideTwoShipCons : combatData.SideOneShipCons;
                group.commonTarget = enemies.Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType != ShipType.Transport)
                                            .OrderBy(s => Vector3.Distance(center, s.transform.position))
                                            .FirstOrDefault();

                foreach (var s in group.ships) s.ShipData.TargetThisShipController = group.commonTarget;
            }
        }

        /// <summary>
        /// Calculate center position of a group of ships
        /// </summary>
        private Vector3 CalculateGroupCenter(List<ShipController> ships)
        {
            Vector3 center = Vector3.zero;
            int validShips = 0;

            foreach (var ship in ships)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    center += ship.transform.position;
                    validShips++;
                }
            }

            if (validShips > 0)
            {
                center /= validShips;
            }

            return center;
        }

        /// <summary>
        /// Find closest enemy to a position
        /// </summary>
        private ShipController FindClosestEnemyToPosition(Vector3 position, List<ShipController> enemies)
        {
            ShipController closest = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.ShipData.Distroyed || enemy.ShipData.ShipType == ShipType.Transport)
                    continue;

                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = enemy;
                }
            }

            return closest;
        }
    }
}
