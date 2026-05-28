using BOTF3D.Combat;
using BOTF3D.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    /// <summary>
    /// Displays turn-by-turn combat results on screen
    /// Shows: Turn number, orders chosen, damage dealt, tactical advantage
    /// </summary>
    public class TurnResultsUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject ResultsPanel;
        public TextMeshProUGUI TurnNumberText;
        public TextMeshProUGUI SideOneOrderText;
        public TextMeshProUGUI SideTwoOrderText;
        public TextMeshProUGUI SideOneDamageText;
        public TextMeshProUGUI SideTwoDamageText;
        public TextMeshProUGUI TacticalFeedbackText;
        public Button ContinueButton;

        private TurnBasedCombatResolver resolver;

        public void Initialize(TurnBasedCombatResolver combatResolver)
        {
            resolver = combatResolver;

            if (ContinueButton != null)
            {
                ContinueButton.onClick.AddListener(OnContinueClicked);
            }

            HideResults();
        }

        /// <summary>
        /// Display results from the last turn
        /// </summary>
        public void ShowTurnResults(TurnResult result)
        {
            if (ResultsPanel != null)
            {
                ResultsPanel.SetActive(true);
            }

            // Turn number
            if (TurnNumberText != null)
            {
                TurnNumberText.text = $"TURN {result.TurnNumber}";
            }

            // Orders chosen
            if (SideOneOrderText != null)
            {
                SideOneOrderText.text = $"Side 1: {GetOrderDisplayName(result.SideOneOrder)}";
            }

            if (SideTwoOrderText != null)
            {
                SideTwoOrderText.text = $"Side 2: {GetOrderDisplayName(result.SideTwoOrder)}";
            }

            // Damage dealt
            if (SideOneDamageText != null)
            {
                SideOneDamageText.text = result.SideOneRetreated ? "RETREATED" : $"{result.SideOneDamageDealt} damage";
                SideOneDamageText.color = result.SideOneRetreated ? Color.yellow : Color.white;
            }

            if (SideTwoDamageText != null)
            {
                SideTwoDamageText.text = result.SideTwoRetreated ? "RETREATED" : $"{result.SideTwoDamageDealt} damage";
                SideTwoDamageText.color = result.SideTwoRetreated ? Color.yellow : Color.white;
            }

            // Tactical feedback
            if (TacticalFeedbackText != null)
            {
                string feedback = GetTacticalFeedback(result);
                TacticalFeedbackText.text = feedback;
            }

            Debug.Log($"📊 Displayed turn {result.TurnNumber} results");
        }

        /// <summary>
        /// Hide the results panel
        /// </summary>
        public void HideResults()
        {
            if (ResultsPanel != null)
            {
                ResultsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// User clicked continue to next turn
        /// </summary>
        private void OnContinueClicked()
        {
            HideResults();
            // Results panel stays hidden while next turn's order selection happens
            Debug.Log("🎮 Player ready for next turn");
        }

        /// <summary>
        /// Get display-friendly order name
        /// </summary>
        private string GetOrderDisplayName(CombatOrders order)
        {
            switch (order)
            {
                case CombatOrders.Engage: return "ENGAGE";
                case CombatOrders.Rush: return "RUSH";
                case CombatOrders.Retreat: return "RETREAT";
                case CombatOrders.Formation: return "FORMATION";
                case CombatOrders.AttackTransports: return "ATTACK TRANSPORTS";
                default: return "UNKNOWN";
            }
        }

        /// <summary>
        /// Generate tactical feedback text based on order matchup
        /// </summary>
        private string GetTacticalFeedback(TurnResult result)
        {
            float side1Multiplier = CombatOrderHelper.GetOrderMultiplier(result.SideOneOrder, result.SideTwoOrder);
            float side2Multiplier = CombatOrderHelper.GetOrderMultiplier(result.SideTwoOrder, result.SideOneOrder);

            if (side1Multiplier > 1.0f)
            {
                return $"Side 1's {result.SideOneOrder} countered Side 2's {result.SideTwoOrder}! (+25% damage)";
            }
            else if (side2Multiplier > 1.0f)
            {
                return $"Side 2's {result.SideTwoOrder} countered Side 1's {result.SideOneOrder}! (+25% damage)";
            }
            else if (result.SideOneOrder == result.SideTwoOrder)
            {
                return $"Both sides chose {result.SideOneOrder} - evenly matched!";
            }
            else
            {
                return "No tactical advantage this turn.";
            }
        }

        /// <summary>
        /// Create a simple on-screen debug display if no UI canvas exists
        /// </summary>
        public void ShowDebugResults(TurnResult result)
        {
            string debugText = $@"
═══ TURN {result.TurnNumber} RESULTS ═══
Side 1: {result.SideOneOrder} → {result.SideOneDamageDealt} damage
Side 2: {result.SideTwoOrder} → {result.SideTwoDamageDealt} damage
{GetTacticalFeedback(result)}
";
            Debug.Log(debugText);

            // Could show on-screen with a simple GUI.Label in OnGUI if needed
        }
    }
}
