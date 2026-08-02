using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;

namespace BOTF3D.UI
{
    // Attach to Panel-ClientRoster. Mirrors PlayerManager.Instance.Roster (a Mirror SyncList
    // replicated to every client) into one ClientRosterPrefab row per connected player.
    //
    // Inspector wiring:
    //   content         -> Panel-ClientRoster (Transform rows get instantiated under)
    //   rosterRowPrefab -> ClientRosterPrefab asset
    //
    // ClientRosterPrefab child-name contract (find-by-name):
    //   "PlayerName"          TextMeshProUGUI - player's chosen name
    //   "CivilizationName"    TextMeshProUGUI - read-only civ label, shown for every row except
    //                         the local player's own row
    //   "CivilizationDropdown" TMP_Dropdown   - civ picker, shown/interactable only on the local
    //                         player's own row; options are the 7 major civs minus whichever ones
    //                         other connected players already hold (server rejects duplicates too -
    //                         see LocalHumanPlayerController.CmdSetPlayerCiv - this is just the UI side)
    public class ClientRosterPanelUIController : MonoBehaviour
    {
        public static ClientRosterPanelUIController Instance { get; private set; }

        [SerializeField] private Transform content;
        [SerializeField] private GameObject rosterRowPrefab;

        private static readonly CivEnum[] SelectableCivs =
        {
            CivEnum.FED, CivEnum.ROM, CivEnum.KLING, CivEnum.CARD, CivEnum.DOM, CivEnum.BORG, CivEnum.TERRAN
        };

        private class RowState
        {
            public GameObject root;
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

        public void RefreshPanel()
        {
            TrySubscribeRosterCallback();

            if (content == null)
            {
                Debug.LogWarning("ClientRosterPanelUIController: 'content' is not wired in the Inspector.");
                return;
            }
            if (rosterRowPrefab == null)
            {
                Debug.LogWarning("ClientRosterPanelUIController: 'rosterRowPrefab' is not wired in the Inspector.");
                return;
            }
            if (PlayerManager.Instance == null)
                return;

            IReadOnlyList<RosterEntry> roster = PlayerManager.Instance.Roster;
            int? localPlayerId = GetLocalPlayerId();
            Debug.Log($"[RosterDiag] RefreshPanel: rosterCount={roster.Count} localPlayerId={(localPlayerId.HasValue ? localPlayerId.Value.ToString() : "null")}");

            while (rows.Count < roster.Count)
                rows.Add(BuildRow());

            for (int i = 0; i < roster.Count; i++)
                PopulateRow(rows[i], roster[i], roster, localPlayerId);

            for (int i = roster.Count; i < rows.Count; i++)
                rows[i].root.SetActive(false);

            // Newly instantiated/reactivated rows don't reliably retrigger VerticalLayoutGroup's
            // own dirty-tracking (SetActive doesn't fire OnTransformChildrenChanged), so without
            // this a just-joined player's row can be left at a stale/default position instead of
            // being stacked under the previous row.
            LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
        }

        private static int? GetLocalPlayerId()
        {
            var localController = PlayerManager.Instance.LocalPlayerController;
            if (localController == null)
                return null;
            return localController.netId.GetHashCode();
        }

        private RowState BuildRow()
        {
            GameObject go = Instantiate(rosterRowPrefab, content);
            var row = new RowState
            {
                root = go,
                playerNameText = FindTMP(go, "PlayerName"),
                civilizationNameText = FindTMP(go, "CivilizationName"),
                civilizationDropdown = FindDropdown(go, "CivilizationDropdown")
            };
            if (row.civilizationDropdown != null)
                row.civilizationDropdown.onValueChanged.AddListener(index => OnDropdownValueChanged(row, index));
            return row;
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

            // This dropdown is only shown/interactable on the local player's own row (see class
            // header contract), so the chosen civ is always this client's own - unlike
            // SubmitPlayerCiv above (a SyncVar, replicated to every client for gameplay), the theme
            // is local-only cosmetic state and was never being updated here, so ThemedUIElement-driven
            // UI (e.g. the "Button Create Galaxy Map" background/colors) stayed on the default Fed
            // theme for any client that picked their civ via this roster dropdown - same code path
            // for LAN and Edgegap dedicated-server multiplayer, since both use this same panel.
            ThemeManager.Instance?.ApplyTheme((ThemeEnum)(int)row.dropdownCivs[index]);
        }

        private void PopulateRow(RowState r, RosterEntry entry, IReadOnlyList<RosterEntry> roster, int? localPlayerId)
        {
            r.root.SetActive(true);
            if (r.playerNameText != null) r.playerNameText.text = entry.PlayerName;

            bool isLocalRow = localPlayerId.HasValue && entry.PlayerId == localPlayerId.Value;

            if (r.civilizationDropdown == null)
            {
                // No dropdown wired on this prefab (older/placeholder rows) - fall back to read-only text.
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

        private static TextMeshProUGUI FindTMP(GameObject root, string childName)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == childName)
                    return t.GetComponent<TextMeshProUGUI>();
            return null;
        }

        private static TMP_Dropdown FindDropdown(GameObject root, string childName)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == childName)
                    return t.GetComponent<TMP_Dropdown>();
            return null;
        }
    }
}
