using BOTF3D.Core;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Helper utilities for combat orders.
    /// Implements "Rock Paper Scissors Lizard Spock" tactical matrix:
    /// - Engage beats Rush, Retreat. Loses to Formation, Attack Transports.
    /// - Rush beats Retreat, Formation. Loses to Engage, Attack Transports.
    /// - Retreat beats Formation, Attack Transports. Loses to Engage, Rush.
    /// - Formation beats Engage, Attack Transports. Loses to Rush, Retreat.
    /// - Attack Transports beats Engage, Rush. Loses to Retreat, Formation.
    /// </summary>
    public static class CombatOrderHelper
    {
        private const float ADVANTAGE_MULTIPLIER = 1.25f;
        private const float DISADVANTAGE_MULTIPLIER = 0.75f;

        /// <summary>
        /// Get the tactical multiplier based on the interaction between two orders.
        /// Applied to damage and accuracy.
        /// </summary>
        public static float GetOrderMultiplier(CombatOrders attackerOrder, CombatOrders targetOrder)
        {
            if (attackerOrder == targetOrder || attackerOrder == CombatOrders.None || targetOrder == CombatOrders.None)
                return 1.0f;

            switch (attackerOrder)
            {
                case CombatOrders.Engage:
                    if (targetOrder == CombatOrders.Rush || targetOrder == CombatOrders.Retreat) return ADVANTAGE_MULTIPLIER;
                    if (targetOrder == CombatOrders.Formation || targetOrder == CombatOrders.AttackTransports) return DISADVANTAGE_MULTIPLIER;
                    break;

                case CombatOrders.Rush:
                    if (targetOrder == CombatOrders.Retreat || targetOrder == CombatOrders.Formation) return ADVANTAGE_MULTIPLIER;
                    if (targetOrder == CombatOrders.Engage || targetOrder == CombatOrders.AttackTransports) return DISADVANTAGE_MULTIPLIER;
                    break;

                case CombatOrders.Retreat:
                    if (targetOrder == CombatOrders.Formation || targetOrder == CombatOrders.AttackTransports) return ADVANTAGE_MULTIPLIER;
                    if (targetOrder == CombatOrders.Engage || targetOrder == CombatOrders.Rush) return DISADVANTAGE_MULTIPLIER;
                    break;

                case CombatOrders.Formation:
                    if (targetOrder == CombatOrders.Engage || targetOrder == CombatOrders.AttackTransports) return ADVANTAGE_MULTIPLIER;
                    if (targetOrder == CombatOrders.Rush || targetOrder == CombatOrders.Retreat) return DISADVANTAGE_MULTIPLIER;
                    break;

                case CombatOrders.AttackTransports:
                    if (targetOrder == CombatOrders.Engage || targetOrder == CombatOrders.Rush) return ADVANTAGE_MULTIPLIER;
                    if (targetOrder == CombatOrders.Retreat || targetOrder == CombatOrders.Formation) return DISADVANTAGE_MULTIPLIER;
                    break;
            }

            return 1.0f;
        }

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
