using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;

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
        [SerializeField] private TextMeshProUGUI actionText;             // wire to "ActionText" TMP — shows active op type
        [SerializeField] private TextMeshProUGUI lastSeenFleetCountText; // wire to "LastSeenFleetCount" TMP
        [SerializeField] private TextMeshProUGUI stealInfoText;          // wire in Inspector: shows preview before op is launched
        [SerializeField] private TextMeshProUGUI localTechPointsText;    // wire to "ProgressText" TMP — shows local civ's accumulated tech points
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

            if (civNameText        != null) civNameText.text        = targetCiv.CivData.CivShortName;
            if (techText          != null) techText.text          = $"{targetCiv.CivData.TechRating:F1} ({targetCiv.CivData.CurrentTechLevel})";
            if (relationText      != null) relationText.text      = diplomaCon != null
                ? diplomaCon.DiplomacyData.DiplomacyStatusEnumOfCivs.ToString()
                : "Unknown";
            if (localTechPointsText != null) localTechPointsText.text = $"Tech pts: {localCiv.CivData.TechPoints}";

            RefreshActiveOpsText(intelCon);
            RefreshFleetCount(intelCon, _targetCiv);
            RefreshStealInfo(localCiv, targetCiv);
            WireButton(gatherIntelButton,    intelCon, SecretActionsEnum.GatherIntelligence, alwaysEnabled: true);
            WireButton(sabotageButton,       intelCon, SecretActionsEnum.Sabotage,           alwaysEnabled: true);
            WireButton(disinformationButton, intelCon, SecretActionsEnum.Disinformation,     alwaysEnabled: true);

            IntelligenceManager.Instance.GetTheftPreview(localCiv, targetCiv,
                out bool theftPossible, out _, out _, out _);
            WireButton(stealTechButton, intelCon, SecretActionsEnum.IntellectualTheft, alwaysEnabled: theftPossible);
        }

        private void RefreshActiveOpsText(IntelligenceController intelCon)
        {
            string opName = "None";
            string summary = "None";
            if (intelCon?.IntelligenceData?.ActiveProjects != null)
            {
                foreach (var p in intelCon.IntelligenceData.ActiveProjects)
                {
                    if (p.IsComplete) continue;
                    opName  = FormatAction(p.ActionType);
                    summary = $"{opName} ({p.TurnsTotal - p.TurnsRemaining}/{p.TurnsTotal})";
                    break; // show first active op; multiple types can run but show the first
                }
            }
            if (activeOpsText != null) activeOpsText.text = summary;
            if (actionText    != null) actionText.text    = opName;
        }

        private void RefreshFleetCount(IntelligenceController intelCon, CivEnum targetCivEnum)
        {
            if (lastSeenFleetCountText == null) return;
            if (intelCon?.IntelligenceData == null) { lastSeenFleetCountText.text = "--"; return; }

            FleetController fleet = intelCon.IntelligenceData.CivSideOne == targetCivEnum
                ? intelCon.IntelligenceData.LastSeenFleetOfSideOne
                : intelCon.IntelligenceData.LastSeenFleetOfSideTwo;

            if (fleet != null)
            {
                lastSeenFleetCountText.text = BOTF3D.Core.Loc.Format("Ships", "{0} ships", fleet.FleetData?.ShipsList?.Count ?? 0);
            }
            else if (intelCon.IntelligenceData.LastSeenStarSysController != null)
            {
                int count = intelCon.IntelligenceData.LastSeenStarSysController.StarSysData?.ShipsList?.Count ?? 0;
                lastSeenFleetCountText.text = BOTF3D.Core.Loc.Format("Ships", "{0} ships", count);
            }
            else
            {
                lastSeenFleetCountText.text = BOTF3D.Core.Loc.Format("Ships", "{0} ships", 0);
            }
        }

        private void RefreshStealInfo(CivController localCiv, CivController targetCiv)
        {
            if (stealInfoText == null) return;

            IntelligenceManager.Instance.GetTheftPreview(localCiv, targetCiv,
                out bool possible, out float success, out float discover, out int gain);

            stealInfoText.text = possible
                ? $"Success {success:P0}  |  Detect {discover:P0}  |  +{gain} pts"
                : "Impossible — technology gap too large";
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
            bool started = IntelligenceManager.Instance.CreateIntelProject(action, _initiatorCiv, _targetCiv,
                out string failReason);

            string msg = started
                ? $"{FormatAction(action)} launched against {_targetCiv}."
                : $"Cannot launch {FormatAction(action)} against {_targetCiv}: {failReason}.";

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
