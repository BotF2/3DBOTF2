using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
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
        }

        private readonly List<RowState> rows = new List<RowState>();

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
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.Roster.Callback += OnRosterChanged;
            RefreshPanel();
        }

        private void OnDisable()
        {
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.Roster.Callback -= OnRosterChanged;
        }

        private void OnRosterChanged(SyncList<RosterEntry>.Operation op, int index, RosterEntry oldItem, RosterEntry newItem)
        {
            RefreshPanel();
        }

        public void RefreshPanel()
        {
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

            while (rows.Count < roster.Count)
                rows.Add(BuildRow());

            for (int i = 0; i < roster.Count; i++)
                PopulateRow(rows[i], roster[i], roster, localPlayerId);

            for (int i = roster.Count; i < rows.Count; i++)
                rows[i].root.SetActive(false);
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
            PlayerManager.Instance?.LocalPlayerController?.SubmitPlayerCiv(row.dropdownCivs[index]);
        }

        private void PopulateRow(RowState r, RosterEntry entry, IReadOnlyList<RosterEntry> roster, int? localPlayerId)
        {
            r.root.SetActive(true);
            if (r.playerNameText != null) r.playerNameText.text = entry.PlayerName;

            bool isLocalRow = localPlayerId.HasValue && entry.PlayerId == localPlayerId.Value;

            if (r.civilizationDropdown == null)
            {
                // No dropdown wired on this prefab (older/placeholder rows) - fall back to read-only text.
                if (r.civilizationNameText != null) r.civilizationNameText.text = entry.PlayerCiv.ToString();
                return;
            }

            if (!isLocalRow)
            {
                r.civilizationDropdown.gameObject.SetActive(false);
                if (r.civilizationNameText != null)
                {
                    r.civilizationNameText.gameObject.SetActive(true);
                    r.civilizationNameText.text = entry.PlayerCiv.ToString();
                }
                return;
            }

            if (r.civilizationNameText != null)
                r.civilizationNameText.gameObject.SetActive(false);
            r.civilizationDropdown.gameObject.SetActive(true);

            r.dropdownCivs.Clear();
            var options = new List<TMP_Dropdown.OptionData>();
            int selectedIndex = 0;
            for (int i = 0; i < SelectableCivs.Length; i++)
            {
                CivEnum civ = SelectableCivs[i];
                if (civ != entry.PlayerCiv && IsTakenByAnotherPlayer(civ, entry.PlayerId, roster))
                    continue;

                if (civ == entry.PlayerCiv)
                    selectedIndex = r.dropdownCivs.Count;
                r.dropdownCivs.Add(civ);
                options.Add(new TMP_Dropdown.OptionData(civ.ToString()));
            }

            r.civilizationDropdown.options = options;
            r.civilizationDropdown.SetValueWithoutNotify(selectedIndex);
            r.civilizationDropdown.RefreshShownValue();
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
