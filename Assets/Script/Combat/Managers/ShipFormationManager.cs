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
    /// Calculates ship formation positions and spiral patterns.
    /// Handles wall formations and defensive positioning.
    /// </summary>
    public class ShipFormationManager : IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        /// <summary>
        /// Generate spiral positions for ship wall formation
        /// </summary>
        public List<Vector2Int> GenerateSpiralPositions(int count)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            if (count <= 0) return positions;

            // Start at center
            positions.Add(new Vector2Int(0, 0));
            if (count == 1) return positions;

            // Generate square spiral pattern
            int x = 0, y = 0;
            int dx = 0, dy = -1;

            for (int i = 1; i < count; i++)
            {
                // Change direction at spiral corners
                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    int temp = dx;
                    dx = -dy;
                    dy = temp;
                }

                x += dx;
                y += dy;
                positions.Add(new Vector2Int(x, y));
            }

            return positions;
        }

        /// <summary>
        /// Get formation position for a ship in defensive wall
        /// </summary>
        public Vector3 GetFormationPosition(ShipController ship, bool isSideOne)
        {
            var stateMachine = ship.GetComponent<CombatOrderStateMachine>();
            if (stateMachine == null) return ship.transform.position;

            bool isTransport = ship.ShipData.ShipType == ShipType.Transport;
            float sideSign = isSideOne ? 1 : -1;
            float formationX = isSideOne ? WarpAnimationController.SIDE1_COMBAT_END_X : WarpAnimationController.SIDE2_COMBAT_END_X;

            if (isTransport)
            {
                // Transports stay 100 units behind the wall
                formationX -= sideSign * 100f;
            }

            // Simple grid based on slot
            int slot = stateMachine.formationSlot;
            if (slot == -1)
            {
                stateMachine.formationSlot = Random.Range(0, 25);
                slot = stateMachine.formationSlot;
            }

            int row = slot / 5;
            int col = slot % 5;
            float spacing = 40f;
            return new Vector3(formationX, (row - 2) * spacing, (col - 2) * spacing);
        }

        /// <summary>
        /// Get retreat position (away from enemy)
        /// </summary>
        public Vector3 GetRetreatPosition(ShipController ship, bool isSideOne)
        {
            float retreatX = isSideOne ? WarpAnimationController.SIDE1_COMBAT_START_X : WarpAnimationController.SIDE2_COMBAT_START_X;
            return new Vector3(retreatX, ship.transform.position.y, ship.transform.position.z);
        }

        /// <summary>
        /// Get rush position (toward and past enemy)
        /// </summary>
        public Vector3 GetRushPosition(ShipController ship, bool isSideOne)
        {
            // Rush order should aim for a point far beyond the enemy side
            // This prevents stopping and ensures ships coast past
            float rushX = isSideOne ? 1500f : -1500f;
            
            ShipController target = ship.ShipData.TargetThisShipController;
            if (target != null && !target.ShipData.Distroyed)
            {
                // Aim for the target's Y/Z but a far X to ensure we fly THROUGH their formation
                return new Vector3(rushX, target.transform.position.y, target.transform.position.z);
            }

            // If no target, rush toward enemy side
            return new Vector3(rushX, ship.transform.position.y, ship.transform.position.z);
        }

        /// <summary>
        /// Get AttackTransports position for a ship
        /// </summary>
        public Vector3 GetAttackTransportsPosition(ShipController ship, bool isSideOne, CombatData combatData)
        {
            bool isFlankingShip = ship.ShipData.ShipType == ShipType.Scout ||
                                  ship.ShipData.ShipType == ShipType.Destroyer;

            if (isFlankingShip)
            {
                var enemies = isSideOne ? combatData.SideTwoShipCons : combatData.SideOneShipCons;
                var transports = enemies.Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType == ShipType.Transport).ToList();

                if (transports.Count > 0)
                {
                    var closestTransport = transports.OrderBy(t => Vector3.Distance(ship.transform.position, t.transform.position)).First();
                    return closestTransport.transform.position;
                }
            }

            // Non-flanking ships: move forward at half speed toward enemy wall
            return new Vector3(isSideOne ? 100f : -100f, ship.transform.position.y, ship.transform.position.z);
        }
    }
}
