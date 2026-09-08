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

        /// <summary>
        /// Phase II tech tree (§8 II.3, §5a): rolls/applies every attacker-side TechEffects bonus
        /// that modifies a single shot's damage. Called from BeamWeapon.Fire/Torpedo.OnReachedTarget
        /// right before ShipController.TakeDamage - the order-based damage-modifier step CLAUDE.md's
        /// combat overview refers to (previously only Rush/Flanking positioning, no tech input).
        /// Returns the possibly-increased damage; bypassShields is true if this hit should ignore the
        /// target's current shield value entirely (Klingon Subsystem Cripple / Ordnance Transphasic -
        /// see Torpedo.cs for the latter, applied separately since it's ordnance-class-specific).
        /// </summary>
        public static int ApplyAttackerTechBonuses(ShipController owner, int damage, out bool bypassShields)
        {
            bypassShields = false;
            TechEffects fx = owner?.ShipData != null
                ? CivManager.Instance?.GetCivDataByCivEnum(owner.ShipData.CivEnum)?.Effects
                : null;
            if (fx == null) return damage;

            // Ordnance Tier 3 Fire Control Solutions - accuracy/crit: AccuracyBonus doubles as this
            // shot's crit chance, since this combat model has no separate to-hit roll to buff.
            if (fx.AccuracyBonus > 0f && UnityEngine.Random.value < fx.AccuracyBonus)
                damage = Mathf.RoundToInt(damage * 1.5f);

            // Terran Fear-Driven Command Protocols I/II / Elite Strike Teams / Flagship Domination,
            // Dominion Ketracel-White Optimization - flat combat-morale damage bonus.
            if (fx.CombatMoraleBonus > 0f)
                damage = Mathf.RoundToInt(damage * (1f + fx.CombatMoraleBonus));

            // Klingon Disruptor Overload Arrays / Subsystem Cripple - chance to land a crippling hit
            // that bypasses shields entirely. No distinct per-subsystem model exists in this combat
            // system (only aggregate Shield/Hull), so "crippled" is modeled as a direct-to-hull hit
            // rather than disabling a named subsystem.
            if (fx.SubsystemCrippleChance > 0f && UnityEngine.Random.value < fx.SubsystemCrippleChance)
                bypassShields = true;

            // Romulan Warbird Ambush Doctrine - first-strike bonus when decloaking to attack.
            // Simplified: this combat model has no per-turn "just decloaked" flag, so the bonus
            // applies on every shot a currently-cloaked Romulan fleet lands rather than only its
            // opening one - still a real, tunable combat edge for the civ that researched it.
            if (fx.WarbirdAmbushBonus > 0f && fx.GalaxyMapCloak)
                damage = Mathf.RoundToInt(damage * (1f + fx.WarbirdAmbushBonus));

            return damage;
        }
    }
}
