using BOTF3D.Core;
using BOTF3D.GamePlay;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Helper utilities for combat orders.
    /// Does NOT apply artificial advantage/disadvantage modifiers.
    /// Advantages emerge from actual combat mechanics:
    /// - Rush: Ships move at max speed (per copilot-instructions.md)
    /// - Retreat: Ships turn around then warp out
    /// - Formation: Ships maintain formation with overlapping fire, protect transports
    /// - AttackTransports: Ships flank to bypass LOS blocking
    /// - Engage: Baseline neutral order
    /// </summary>
    public static class CombatOrderHelper
    {
        /// <summary>
        /// Check if a side has transport ships
        /// </summary>
        public static bool HasTransports(CombatData combatData, int side)
        {
            if (combatData == null)
            {
                Debug.LogWarning("CombatOrderHelper.HasTransports: combatData is null!");
                return false;
            }

            List<ShipController> ships = side == 1 ? combatData.SideOneShipCons : combatData.SideTwoShipCons;

            if (ships == null || ships.Count == 0)
                return false;

            foreach (var ship in ships)
            {
                if (ship?.ShipData?.ShipSO != null && ship.ShipData.ShipSO.ShipType == ShipType.Transport)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the speed multiplier for a given combat order.
        /// Per copilot-instructions.md: "Combat orders should NOT use speed multipliers.
        /// Ships have a max speed and operate at or below it."
        /// Rush: 1.0 (max speed), Formation: ~0.8 (maintain formation), Retreat: 1.0 (after turning)
        /// </summary>
        public static float GetOrderSpeedFactor(CombatOrders order)
        {
            switch (order)
            {
                case CombatOrders.Rush:
                    return 1.0f; // Max speed rush

                case CombatOrders.Formation:
                    return 0.8f; // Slower to maintain formation

                case CombatOrders.Retreat:
                    return 1.0f; // Max speed after turning around

                case CombatOrders.AttackTransports:
                    return 0.9f; // Slightly slower while flanking

                case CombatOrders.Engage:
                default:
                    return 0.9f; // Standard engagement speed
            }
        }

        /// <summary>
        /// Check if an order protects transports via formation/positioning
        /// </summary>
        public static bool OrderProtectsTransports(CombatOrders order)
        {
            return order == CombatOrders.Formation;
        }

        /// <summary>
        /// Check if an order attempts to bypass LOS blocking
        /// </summary>
        public static bool OrderBypassesLOS(CombatOrders order)
        {
            return order == CombatOrders.AttackTransports;
        }

        /// <summary>
        /// Check if ships are retreating (turning around to warp out)
        /// </summary>
        public static bool IsRetreating(CombatOrders order)
        {
            return order == CombatOrders.Retreat;
        }

        /// <summary>
        /// Get a descriptive summary of both sides' orders (for debugging/UI)
        /// </summary>
        public static string GetOrderSummary(CombatOrders side1Order, CombatOrders side2Order)
        {
            return $"Side 1: {side1Order} | Side 2: {side2Order}";
        }

        /// <summary>
        /// Get tactical description of an order (what it does mechanically)
        /// </summary>
        public static string GetOrderDescription(CombatOrders order)
        {
            switch (order)
            {
                case CombatOrders.Rush:
                    return "Ships rush at max speed. Vulnerable if enemy is in Formation.";

                case CombatOrders.Formation:
                    return "Ships maintain formation with overlapping fire. Protects transports via positioning.";

                case CombatOrders.Retreat:
                    return "Ships turn around then warp out. Vulnerable during turn delay.";

                case CombatOrders.AttackTransports:
                    return "Ships flank around blocking ships to target transports at close range.";

                case CombatOrders.Engage:
                    return "Standard combat engagement.";

                default:
                    return "No order set.";
            }
        }
    }
}
