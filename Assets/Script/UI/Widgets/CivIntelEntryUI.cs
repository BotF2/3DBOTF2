using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Civilization;

namespace BOTF3D.UI
{
    /// <summary>
    /// Attached to CivEntryPrefab. Populate() is called by IntelligenceUIController
    /// each time the Intel panel refreshes. Wire all fields in the Inspector.
    /// </summary>
    public class CivIntelEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI civNameText;
        [SerializeField] private TextMeshProUGUI techText;
        [SerializeField] private TextMeshProUGUI relationText;
        [SerializeField] private TextMeshProUGUI activeOpsText;
        [SerializeField] private Button gatherIntelButton;
        [SerializeField] private Button stealTechButton;
        [SerializeField] private Button sabotageButton;
        [SerializeField] private Button disinformationButton;

        private CivEnum _initiatorCiv;
        private CivEnum _targetCiv;

        public void Populate(IntelligenceController intelCon, CivController localCiv,
            CivController targetCiv, DiplomacyController diplomaCon)
        {
            _initiatorCiv = localCiv.CivData.CivEnum;
            _targetCiv    = targetCiv.CivData.CivEnum;

            if (civNameText  != null) civNameText.text  = targetCiv.CivData.CivShortName;
            if (techText     != null) techText.text     = targetCiv.CivData.CurrentTechLevel.ToString();
            if (relationText != null) relationText.text = diplomaCon != null
                ? diplomaCon.DiplomacyData.DiplomacyStatusEnumOfCivs.ToString()
                : "Unknown";

            RefreshActiveOpsText(intelCon);
            WireButton(gatherIntelButton,    intelCon, SecretActionsEnum.GatherIntelligence, alwaysEnabled: true);
            WireButton(stealTechButton,      intelCon, SecretActionsEnum.IntellectualTheft,  alwaysEnabled: true);
            // ToDo: gate stealTechButton on targetCiv.CivData.TechRating >= 5f once TechRating is added to CivData
            WireButton(sabotageButton,       intelCon, SecretActionsEnum.Sabotage,           alwaysEnabled: true);
            WireButton(disinformationButton, intelCon, SecretActionsEnum.Disinformation,     alwaysEnabled: true);
        }

        private void RefreshActiveOpsText(IntelligenceController intelCon)
        {
            if (activeOpsText == null) return;

            string summary = "None";
            if (intelCon?.IntelligenceData?.ActiveProjects != null)
            {
                foreach (var p in intelCon.IntelligenceData.ActiveProjects)
                {
                    if (p.IsComplete) continue;
                    summary = $"{FormatAction(p.ActionType)} ({p.TurnsTotal - p.TurnsRemaining}/{p.TurnsTotal})";
                    break; // show first active op; multiple types can run but show the first
                }
            }
            activeOpsText.text = summary;
        }

        private void WireButton(Button btn, IntelligenceController intelCon,
            SecretActionsEnum action, bool alwaysEnabled)
        {
            if (btn == null) return;

            bool duplicateRunning = HasActiveProjectOfType(intelCon, action);
            btn.interactable = alwaysEnabled && !duplicateRunning;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnActionClicked(action));
        }

        private void OnActionClicked(SecretActionsEnum action)
        {
            bool started = IntelligenceManager.Instance.CreateIntelProject(action, _initiatorCiv, _targetCiv);

            string msg = started
                ? $"{FormatAction(action)} launched against {_targetCiv}."
                : $"Cannot start {FormatAction(action)} against {_targetCiv}. Check IntelPoints or active ops.";

            IntelligenceUIController.Instance?.SetFeedback(msg);
            IntelligenceUIController.Instance?.RefreshPanel();
        }

        private static bool HasActiveProjectOfType(IntelligenceController intelCon, SecretActionsEnum action)
        {
            if (intelCon?.IntelligenceData?.ActiveProjects == null) return false;
            foreach (var p in intelCon.IntelligenceData.ActiveProjects)
                if (p.ActionType == action && !p.IsComplete) return true;
            return false;
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
