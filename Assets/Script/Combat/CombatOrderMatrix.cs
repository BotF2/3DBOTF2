using UnityEngine;

namespace BOTF3D.Core
{
    /// <summary>
    /// Defines combat advantages/disadvantages based on order combinations
    /// Matrix values: positive = advantage to side one, negative = advantage to side two
    /// </summary>
    public static class CombatOrderMatrix // rock,  paper, scissors, lizard, Spock style matrix for combat orders
    {
        // Matrix[sideOneOrder, sideTwoOrder] = advantage value
        private static readonly int[,] orderMatrix = new int[5, 5]
        {
            //             Engage, Formation, Rush, Retreat, AttackTransports
            /* Engage */        { 0,      1,     -1,     1,         -1 },
            /* Formation */     {-1,      0,      1,    -1,          1 },
            /* Rush */          { 1,     -1,      0,     1,         -1 },
            /* Retreat */       {-1,      1,     -1,     0,          1 },
            /* AttackTrans */   { 1,     -1,      1,    -1,          0 }
        };

        /// <summary>
        /// Get combat advantage for side one based on order combination
        /// Returns: 1 (advantage), 0 (neutral), -1 (disadvantage)
        /// </summary>
        public static int GetAdvantage(CombatOrders sideOneOrder, CombatOrders sideTwoOrder)
        {
            int row = GetOrderIndex(sideOneOrder);
            int col = GetOrderIndex(sideTwoOrder);

            if (row < 0 || col < 0)
            {
                Debug.LogWarning($"Invalid combat order combination: {sideOneOrder} vs {sideTwoOrder}");
                return 0;
            }

            return orderMatrix[row, col];
        }

        private static int GetOrderIndex(CombatOrders order)
        {
            switch (order)
            {
                case CombatOrders.Engage: return 0;
                case CombatOrders.Formation: return 1;
                case CombatOrders.Rush: return 2;
                case CombatOrders.Retreat: return 3;
                case CombatOrders.TargetTransports: return 4;
                default: return -1;
            }
        }

        /// <summary>
        /// Get movement speed multiplier based on order
        /// </summary>
        public static float GetSpeedMultiplier(CombatOrders order)
        {
            switch (order)
            {
                case CombatOrders.Rush: return 1.5f;        // 50% faster
                case CombatOrders.Retreat: return 1.3f;     // 30% faster (running away)
                case CombatOrders.Formation: return 0.7f;   // 30% slower (holding formation)
                case CombatOrders.Engage: return 1.0f;      // Normal speed
                case CombatOrders.TargetTransports: return 1.0f; // Normal speed
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Get accuracy multiplier based on order
        /// </summary>
        public static float GetAccuracyMultiplier(CombatOrders order)
        {
            switch (order)
            {
                case CombatOrders.Formation: return 1.2f;   // 20% better accuracy (defensive position)
                case CombatOrders.Rush: return 0.8f;        // 20% worse accuracy (moving fast)
                case CombatOrders.Retreat: return 0.6f;     // 40% worse accuracy (fleeing)
                case CombatOrders.Engage: return 1.0f;      // Normal accuracy
                case CombatOrders.TargetTransports: return 1.1f; // 10% better (focused targeting)
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Get defensive bonus based on order
        /// </summary>
        public static float GetDefenseMultiplier(CombatOrders order)
        {
            switch (order)
            {
                case CombatOrders.Formation: return 0.8f;   // Take 20% less damage (defensive)
                case CombatOrders.Rush: return 1.2f;        // Take 20% more damage (aggressive)
                case CombatOrders.Retreat: return 1.1f;     // Take 10% more damage (fleeing)
                case CombatOrders.Engage: return 1.0f;      // Normal damage
                case CombatOrders.TargetTransports: return 1.0f; // Normal damage
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Check if transports exist on the specified side
        /// </summary>
        public static bool HasTransports(CombatData combatData, int side)
        {
            var ships = side == 1 ? combatData.SideOneShipCons : combatData.SideTwoShipCons;

            foreach (var ship in ships)
            {
                if (ship != null && ship.ShipData.ShipType == ShipType.Transport)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get description of order advantage/disadvantage
        /// </summary>
        public static string GetAdvantageDescription(int advantage)
        {
            if (advantage > 0)
                return "✅ ADVANTAGE - Your tactics are superior!";
            else if (advantage < 0)
                return "⚠️ DISADVANTAGE - Enemy tactics counter yours!";
            else
                return "⚖️ NEUTRAL - Evenly matched tactics.";
        }
    }
}
