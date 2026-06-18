using BOTF3D.Core;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



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

                case CombatOrders.Capture:
                    return "Board enemy ships. Retreating and failed-scuttle ships are captured instead of destroyed. Grants shipyard bonus and tech points per capture.";

                case CombatOrders.Scuttle:
                    return "Self-destruct at combat start to deny captures. Undamaged ships succeed reliably; damaged ships may fail and remain capturable.";

                default:
                    return "No order set.";
            }
        }
    }
}
