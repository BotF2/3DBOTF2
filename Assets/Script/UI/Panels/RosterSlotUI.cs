
using TMPro;
using UnityEngine;

namespace BOTF3D.UI
{
    // Attach to each Slot0..Slot6 under Panel_ClientRoster/Players. Just a wiring container -
    // ClientRosterPanelUIController owns all the population/behavior logic.
    public class RosterSlotUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI civilizationNameText;
        [SerializeField] private TMP_Dropdown civilizationDropdown;

        public TextMeshProUGUI PlayerNameText => playerNameText;
        public TextMeshProUGUI CivilizationNameText => civilizationNameText;
        public TMP_Dropdown CivilizationDropdown => civilizationDropdown;
    }
}
