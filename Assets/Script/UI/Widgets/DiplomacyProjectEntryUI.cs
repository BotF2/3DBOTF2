using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Civilization;

namespace BOTF3D.UI
{
    /// <summary>
    /// Row widget for a pending DiplomacyProject (see DiplomacyManager.CreateDiplomacyProject) -
    /// the Diplomacy-side counterpart to ProjectEntryUI (Intel's equivalent row). Not yet wired to a
    /// prefab/panel in GalaxyScene - see DiplomacyMenuUIController for where a container + this
    /// prefab still need to be added. Wire all fields in the Inspector once that panel exists.
    /// </summary>
    public class DiplomacyProjectEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI proposalText;
        [SerializeField] private TextMeshProUGUI targetCivText;
        [SerializeField] private TextMeshProUGUI progressText;   // e.g. "Turn 2 of 3"
        [SerializeField] private TextMeshProUGUI acceptanceText; // e.g. "65%"
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button cancelButton;

        private DiplomacyProject _project;

        public void Populate(DiplomacyProject project)
        {
            _project = project;

            int turnsDone = project.TurnsTotal - project.TurnsRemaining;

            if (proposalText   != null) proposalText.text   = FormatProposal(project.ProposalType);
            if (targetCivText  != null) targetCivText.text  = project.TargetCiv.ToString();
            if (progressText   != null) progressText.text   = $"Turn {turnsDone} of {project.TurnsTotal}";
            if (acceptanceText != null) acceptanceText.text = $"{project.AcceptanceChance:P0}";

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
            _project.IsComplete = true; // DiplomacyManager.TickDiplomacyProjects skips/removes it next turn
        }

        private static string FormatProposal(NegotiationPloysEnum proposalType)
        {
            switch (proposalType)
            {
                case NegotiationPloysEnum.OfferAlliance:         return "Alliance Proposal";
                case NegotiationPloysEnum.OfferTrade:            return "Trade Proposal";
                case NegotiationPloysEnum.OfferTech:             return "Tech Bargain";
                case NegotiationPloysEnum.OfferAid:              return "Aid Offer";
                case NegotiationPloysEnum.OfferCulturalExchange: return "Cultural Exchange";
                default:                                         return proposalType.ToString();
            }
        }
    }
}
