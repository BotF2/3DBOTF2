using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Civilization;

namespace BOTF3D.UI
{
    /// <summary>
    /// Attached to ProjectEntryPrefab. Used by the optional consolidated ActiveProjectPanel.
    /// Wire all fields in the Inspector.
    /// </summary>
    public class ProjectEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private TextMeshProUGUI targetCivText;
        [SerializeField] private TextMeshProUGUI progressText;   // e.g. "Turn 2 of 3"
        [SerializeField] private TextMeshProUGUI successText;    // e.g. "33%"
        [SerializeField] private TextMeshProUGUI discoveryText;  // e.g. "40%"
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button cancelButton;

        private IntelProject _project;

        public void Populate(IntelProject project)
        {
            _project = project;

            int turnsDone = project.TurnsTotal - project.TurnsRemaining;

            if (actionText    != null) actionText.text    = FormatAction(project.ActionType);
            if (targetCivText != null) targetCivText.text = project.TargetCiv.ToString();
            if (progressText  != null) progressText.text  = $"Turn {turnsDone} of {project.TurnsTotal}";
            if (successText   != null) successText.text   = $"{project.SuccessChance:P0}";
            if (discoveryText != null) discoveryText.text = $"{project.DiscoveryChance:P0}";

            if (progressBar != null)
            {
                progressBar.minValue = 0;
                progressBar.maxValue = project.TurnsTotal;
                progressBar.value    = turnsDone;
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void OnCancelClicked()
        {
            if (_project == null) return;
            string actionName = FormatAction(_project.ActionType);
            _project.IsComplete = true; // TickIntelProjects will skip and remove on next turn
            IntelligenceUIController.Instance?.SetFeedback($"{actionName} cancelled.");
            IntelligenceUIController.Instance?.RefreshPanel();
        }

        private static string FormatAction(SecretActionsEnum action)
        {
            switch (action)
            {
                case SecretActionsEnum.IntellectualTheft:  return "Tech Theft";
                case SecretActionsEnum.GatherIntelligence: return "Gather Intel";
                case SecretActionsEnum.Sabotage:           return "Sabotage";
                case SecretActionsEnum.Disinformation:     return "Disinformation";
                default:                                   return action.ToString();
            }
        }
    }
}
