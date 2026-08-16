using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using BOTF3D.Core;

namespace BOTF3D.UI
{
    // Attach to Panel-ClientRoster. Mirrors PlayerManager.Instance.Roster (a Mirror SyncList
    // replicated to every client) onto 7 fixed slots (Players/Slot0..Slot6, each carrying a
    // RosterSlotUI) rather than instantiating a row prefab per player - the slot count is fixed
    // at the same 7 as SelectableCivs, so there's always exactly one slot per possible player.
    //
    // Inspector wiring:
    //   slots -> Players/Slot0..Slot6's RosterSlotUI components, in slot order
    public class ClientRosterPanelUIController : MonoBehaviour
    {
        public static ClientRosterPanelUIController Instance { get; private set; }

        [SerializeField] private RosterSlotUI[] slots;

        private static readonly CivEnum[] SelectableCivs =
        {
            CivEnum.FED, CivEnum.ROM, CivEnum.KLING, CivEnum.CARD, CivEnum.DOM, CivEnum.BORG, CivEnum.TERRAN
        };

        private class RowState
        {
            public RosterSlotUI slot;
            public TextMeshProUGUI playerNameText;
            public TextMeshProUGUI civilizationNameText;
            public TMP_Dropdown civilizationDropdown;
            public List<CivEnum> dropdownCivs = new List<CivEnum>();
            public List<bool> dropdownCivAvailable = new List<bool>();
            public int lastValidIndex;
        }

        private readonly List<RowState> rows = new List<RowState>();
        private bool rosterCallbackSubscribed;

        private void Awake()
        {
            Instance = this;
            BuildRows();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            TrySubscribeRosterCallback();
            RefreshPanel();
        }

        private void OnDisable()
        {
            if (rosterCallbackSubscribed && PlayerManager.Instance != null)
                PlayerManager.Instance.Roster.Callback -= OnRosterChanged;
            rosterCallbackSubscribed = false;
        }

        // OnEnable fires the instant the lobby activates this panel (as soon as the transport
        // connects), which on a real remote connection is well before PlayerManager.Instance
        // exists - so a single check-and-bail there misses the subscription forever. Retrying
        // here on every RefreshPanel() call (including the guaranteed later one from
        // OnLocalPlayerReady) catches it as soon as PlayerManager.Instance actually appears.
        private void TrySubscribeRosterCallback()
        {
            if (rosterCallbackSubscribed || PlayerManager.Instance == null)
                return;
            PlayerManager.Instance.Roster.Callback += OnRosterChanged;
            rosterCallbackSubscribed = true;
        }

        private void OnRosterChanged(SyncList<RosterEntry>.Operation op, int index, RosterEntry oldItem, RosterEntry newItem)
        {
            RefreshPanel();
        }

        private void BuildRows()
        {
            if (rows.Count > 0 || slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                RosterSlotUI slot = slots[i];
                if (slot == null)
                {
                    Debug.LogWarning($"ClientRosterPanelUIController: slot {i} is not wired in the Inspector.");
                    continue;
                }

                var row = new RowState
                {
                    slot = slot,
                    playerNameText = slot.PlayerNameText,
                    civilizationNameText = slot.CivilizationNameText,
                    civilizationDropdown = slot.CivilizationDropdown
                };
                if (row.civilizationDropdown != null)
                    row.civilizationDropdown.onValueChanged.AddListener(index => OnDropdownValueChanged(row, index));
                rows.Add(row);
            }
        }

        public void RefreshPanel()
        {
            TrySubscribeRosterCallback();
            BuildRows();

            if (rows.Count == 0)
            {
                Debug.LogWarning("ClientRosterPanelUIController: 'slots' is not wired in the Inspector.");
                return;
            }
            if (PlayerManager.Instance == null)
                return;

            IReadOnlyList<RosterEntry> roster = PlayerManager.Instance.Roster;
            int? localPlayerId = GetLocalPlayerId();
            Debug.Log($"[RosterDiag] RefreshPanel: rosterCount={roster.Count} localPlayerId={(localPlayerId.HasValue ? localPlayerId.Value.ToString() : "null")}");

            for (int i = 0; i < rows.Count; i++)
            {
                if (i < roster.Count)
                    PopulateRow(rows[i], i, roster[i], roster, localPlayerId);
                else
                    PopulateEmptySlot(rows[i], i);
            }
        }

        private static int? GetLocalPlayerId()
        {
            var localController = PlayerManager.Instance.LocalPlayerController;
            if (localController == null)
                return null;
            return localController.netId.GetHashCode();
        }

        private static void OnDropdownValueChanged(RowState row, int index)
        {
            if (index < 0 || index >= row.dropdownCivs.Count)
                return;

            if (!row.dropdownCivAvailable[index])
            {
                // Taken civs stay visible (greyed out) so the player can see what's unavailable,
                // but selecting one snaps the dropdown back instead of submitting it - the server
                // would reject it anyway (see LocalHumanPlayerController.CmdSetPlayerCiv).
                row.civilizationDropdown.SetValueWithoutNotify(row.lastValidIndex);
                return;
            }

            row.lastValidIndex = index;
            var localCon = PlayerManager.Instance?.LocalPlayerController;
            Debug.Log($"[RosterDiag] OnDropdownValueChanged: submitting civ={row.dropdownCivs[index]} via LocalPlayerController netId={(localCon != null ? localCon.netId.ToString() : "null")}");
            localCon?.SubmitPlayerCiv(row.dropdownCivs[index]);

            // Singleplayer's Panel-SelectLocalPlayer toggles play a per-civ selection sting via
            // MainMenuUIController.PlayCivSelectionSound - this dropdown is the multiplayer
            // equivalent (see class header) and never played one at all.
            MainMenuUIController.Instance?.PlayCivSelectionSoundForCiv(row.dropdownCivs[index]);

            // This dropdown is only shown/interactable on the local player's own row (see class
            // header contract), so the chosen civ is always this client's own - unlike
            // SubmitPlayerCiv above (a SyncVar, replicated to every client for gameplay), the theme
            // is local-only cosmetic state and was never being updated here, so ThemedUIElement-driven
            // UI (e.g. the "Button Create Galaxy Map" background/colors) stayed on the default Fed
            // theme for any client that picked their civ via this roster dropdown - same code path
            // for LAN and Edgegap dedicated-server multiplayer, since both use this same panel.
            ThemeManager.Instance?.ApplyTheme((ThemeEnum)(int)row.dropdownCivs[index]);
        }

        private void PopulateRow(RowState r, int slotIndex, RosterEntry entry, IReadOnlyList<RosterEntry> roster, int? localPlayerId)
        {
            string displayName = string.IsNullOrEmpty(entry.PlayerName) ? $"Player {slotIndex + 1}" : entry.PlayerName;
            if (r.playerNameText != null) r.playerNameText.text = displayName;

            bool isLocalRow = localPlayerId.HasValue && entry.PlayerId == localPlayerId.Value;

            if (r.civilizationDropdown == null)
            {
                // No dropdown wired on this slot - fall back to read-only text.
                if (r.civilizationNameText != null) r.civilizationNameText.text = GetDisplayName(entry.PlayerCiv);
                return;
            }

            if (!isLocalRow)
            {
                r.civilizationDropdown.gameObject.SetActive(false);
                if (r.civilizationNameText != null)
                {
                    r.civilizationNameText.gameObject.SetActive(true);
                    r.civilizationNameText.text = GetDisplayName(entry.PlayerCiv);
                }
                return;
            }

            if (r.civilizationNameText != null)
                r.civilizationNameText.gameObject.SetActive(false);
            r.civilizationDropdown.gameObject.SetActive(true);

            r.dropdownCivs.Clear();
            r.dropdownCivAvailable.Clear();
            var options = new List<TMP_Dropdown.OptionData>();
            int selectedIndex = 0;
            for (int i = 0; i < SelectableCivs.Length; i++)
            {
                CivEnum civ = SelectableCivs[i];
                bool takenByOther = civ != entry.PlayerCiv && IsTakenByAnotherPlayer(civ, entry.PlayerId, roster);

                if (civ == entry.PlayerCiv)
                    selectedIndex = r.dropdownCivs.Count;

                r.dropdownCivs.Add(civ);
                r.dropdownCivAvailable.Add(!takenByOther);

                // Keep taken civs visible but greyed out (rather than hiding them) so it's obvious
                // why they can't be picked instead of them silently disappearing from the list.
                string label = takenByOther ? $"<color=#808080>{GetDisplayName(civ)} (Taken)</color>" : GetDisplayName(civ);
                options.Add(new TMP_Dropdown.OptionData(label));
            }

            r.civilizationDropdown.options = options;
            r.civilizationDropdown.SetValueWithoutNotify(selectedIndex);
            r.civilizationDropdown.RefreshShownValue();
            r.lastValidIndex = selectedIndex;
        }

        private static void PopulateEmptySlot(RowState r, int slotIndex)
        {
            if (r.playerNameText != null) r.playerNameText.text = $"Player {slotIndex + 1}";

            r.civilizationDropdown?.gameObject.SetActive(false);
            if (r.civilizationNameText != null)
            {
                r.civilizationNameText.gameObject.SetActive(true);
                r.civilizationNameText.text = "";
            }
        }

        private static string GetDisplayName(CivEnum civ)
        {
            switch (civ)
            {
                case CivEnum.FED:    return "Federation";
                case CivEnum.ROM:    return "Romulan";
                case CivEnum.KLING:  return "Klingon";
                case CivEnum.CARD:   return "Cardassian";
                case CivEnum.DOM:    return "Dominion";
                case CivEnum.BORG:   return "Borg";
                case CivEnum.TERRAN: return "Terran";
                default:             return civ.ToString();
            }
        }

        private static bool IsTakenByAnotherPlayer(CivEnum civ, int excludingPlayerId, IReadOnlyList<RosterEntry> roster)
        {
            for (int i = 0; i < roster.Count; i++)
                if (roster[i].PlayerCiv == civ && roster[i].PlayerId != excludingPlayerId)
                    return true;
            return false;
        }
    }
}
