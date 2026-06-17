using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Civilization;

namespace BOTF3D.UI
{
    /// <summary>
    /// Attach to the Report View root GameObject.
    /// Pools up to 10 report rows (newest first) sourced from Combat, Diplomacy, and Intel events.
    ///
    /// Inspector wiring:
    ///   content         → ReportView/Viewport/Content  (Transform)
    ///   reportRowPrefab → ReportEntryPrefab asset
    ///
    /// ReportEntryPrefab child-name contract (find-by-name):
    ///   "CategoryText"  TextMeshProUGUI  — category label (COMBAT / DIPL / INTEL)
    ///   "TurnText"      TextMeshProUGUI  — turn stamp
    ///   "SummaryText"   TextMeshProUGUI  — 1-2 line summary, always visible
    ///   "ExpandButton"  Button           — toggles DetailPanel; hidden when no detail
    ///   "DetailPanel"   GameObject       — starts inactive; contains DetailText
    ///   "DetailText"    TextMeshProUGUI  — full multi-line text inside DetailPanel
    /// </summary>
    public class ReportEntryUI : MonoBehaviour
    {
        [SerializeField] private Transform  content;
        [SerializeField] private GameObject reportRowPrefab;

        private const int MaxReports = 10;

        private static readonly List<ReportEntry> _reports = new List<ReportEntry>();
        private readonly List<RowState> _rows = new List<RowState>();

        public static ReportEntryUI Instance { get; private set; }

        // ── Per-row runtime state ─────────────────────────────────────────────

        private class RowState
        {
            public GameObject      root;
            public TextMeshProUGUI categoryText;
            public TextMeshProUGUI turnText;
            public TextMeshProUGUI summaryText;
            public GameObject      detailPanel;
            public TextMeshProUGUI detailText;
            public Button          expandButton;
            public TextMeshProUGUI expandLabel;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("ReportEntryUI: duplicate detected — overwriting Instance.");
            Instance = this;

            // Subscribe here, not in OnEnable, so reports are captured even while the
            // panel is hidden (SetActive false disables OnEnable but not Awake subscribers).
            IntelligenceManager.OnProjectResolved += OnIntelResolved;
            IntelligenceManager.OnNewContact      += OnNewContact;
            GameEvents.OnCombatEnded              += OnCombatEnded;
            GameEvents.OnDiplomacyChanged         += OnDiplomacyChanged;
        }

        private void OnEnable()
        {
            // Panel just became visible — show whatever has accumulated.
            RefreshPanel();
        }

        private void OnDestroy()
        {
            IntelligenceManager.OnProjectResolved -= OnIntelResolved;
            IntelligenceManager.OnNewContact      -= OnNewContact;
            GameEvents.OnCombatEnded              -= OnCombatEnded;
            GameEvents.OnDiplomacyChanged         -= OnDiplomacyChanged;
            if (Instance == this) Instance = null;
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnIntelResolved(SecretActionsEnum action, CivEnum initiator, CivEnum target,
            bool succeeded, bool discovered, int techGain)
        {
            if (GameController.Instance == null) return;
            if (initiator != GameController.Instance.GameData.LocalPlayerCivEnum) return;

            string name = FormatAction(action);
            string summary, detail;

            if (succeeded)
            {
                summary = action == SecretActionsEnum.IntellectualTheft
                    ? $"Tech theft vs {target}: +{techGain} pts"
                    : $"{name} vs {target}: success";
                detail = action == SecretActionsEnum.IntellectualTheft
                    ? $"Your agents extracted {techGain} technology points from {target}."
                    : $"The {name.ToLower()} operation against {target} succeeded.";
            }
            else if (discovered)
            {
                summary = $"{name} vs {target}: discovered!";
                detail  = $"Your covert operation against {target} was uncovered. Diplomatic relations have been damaged.";
            }
            else
            {
                summary = $"{name} vs {target}: failed";
                detail  = $"The operation against {target} failed without detection.";
            }

            PushReport(new ReportEntry(ReportCategory.Intel, GetTurn(), summary, detail));
        }

        private void OnNewContact(CivEnum civ)
        {
            PushReport(new ReportEntry(ReportCategory.Intel, GetTurn(),
                $"First contact: {civ}",
                $"Your forces have established first contact with the {civ} civilization."));
        }

        private void OnCombatEnded(CivEnum victor)
        {
            string detail = CombatUIManager.LastCombatReport;
            PushReport(new ReportEntry(ReportCategory.Combat, GetTurn(),
                $"Combat ended — Victor: {victor}",
                string.IsNullOrEmpty(detail) ? "No further detail available." : detail));
        }

        private void OnDiplomacyChanged(CivEnum civA, CivEnum civB, DiplomaticState state)
        {
            if (GameController.Instance == null) return;
            CivEnum local = GameController.Instance.GameData.LocalPlayerCivEnum;
            if (civA != local && civB != local) return;

            CivEnum other   = civA == local ? civB : civA;
            string  summary = BuildDiplomacySummary(state, other);
            string  detail  = BuildDiplomacyDetail(state, local, other);
            PushReport(new ReportEntry(ReportCategory.Diplomacy, GetTurn(), summary, detail));
        }

        // ── Static API — call from anywhere to inject a report ────────────────

        public static void PushReport(ReportEntry entry)
        {
            _reports.Insert(0, entry);
            if (_reports.Count > MaxReports)
                _reports.RemoveAt(_reports.Count - 1);

            Instance?.RefreshPanel();
        }

        // ── Panel refresh ─────────────────────────────────────────────────────

        public void RefreshPanel()
        {
            if (content == null)
            {
                Debug.LogWarning("ReportEntryUI: 'content' is not wired in the Inspector.");
                return;
            }
            if (reportRowPrefab == null)
            {
                Debug.LogWarning("ReportEntryUI: 'reportRowPrefab' is not wired in the Inspector.");
                return;
            }

            // When no events have fired yet show a single placeholder row.
            if (_reports.Count == 0)
            {
                if (_rows.Count == 0)
                    _rows.Add(BuildRow());

                var placeholder = new ReportEntry(ReportCategory.Intel, 0,
                    "No reports yet.", "Reports from combat, diplomacy, and intelligence operations will appear here.");
                PopulateRow(_rows[0], placeholder);

                for (int i = 1; i < _rows.Count; i++)
                    _rows[i].root.SetActive(false);
                return;
            }

            while (_rows.Count < _reports.Count)
                _rows.Add(BuildRow());

            for (int i = 0; i < _reports.Count; i++)
                PopulateRow(_rows[i], _reports[i]);

            for (int i = _reports.Count; i < _rows.Count; i++)
                _rows[i].root.SetActive(false);
        }

        [ContextMenu("Test: Push Sample Reports")]
        private void TestPushSampleReports()
        {
            PushReport(new ReportEntry(ReportCategory.Combat,    1, "Combat ended — Victor: FED",    "Side 1 destroyed 3 ships. Side 2 retreated."));
            PushReport(new ReportEntry(ReportCategory.Diplomacy, 2, "War declared with KLING",       "A state of war now exists between FED and KLING."));
            PushReport(new ReportEntry(ReportCategory.Intel,     3, "Tech theft vs ROM: +40 pts",    "Your agents extracted 40 technology points from the Romulans."));
        }

        // ── Row pool ──────────────────────────────────────────────────────────

        private RowState BuildRow()
        {
            GameObject go = Instantiate(reportRowPrefab, content);

            var r = new RowState
            {
                root         = go,
                categoryText = FindTMP(go, "CategoryText"),
                turnText     = FindTMP(go, "TurnText"),
                summaryText  = FindTMP(go, "SummaryText"),
                detailPanel  = FindChild(go, "DetailPanel"),
                expandButton = FindButton(go, "ExpandButton")
            };

            if (r.detailPanel != null)
            {
                r.detailText = FindTMP(r.detailPanel, "DetailText");
                r.detailPanel.SetActive(false);
            }

            if (r.expandButton != null)
            {
                r.expandLabel = r.expandButton.GetComponentInChildren<TextMeshProUGUI>();
                r.expandButton.onClick.AddListener(() => ToggleDetail(r));
            }

            return r;
        }

        private static void PopulateRow(RowState r, ReportEntry entry)
        {
            r.root.SetActive(true);

            if (r.categoryText != null) r.categoryText.text = CategoryLabel(entry.Category);
            if (r.turnText     != null) r.turnText.text     = $"T{entry.Turn}";
            if (r.summaryText  != null) r.summaryText.text  = entry.Summary;
            if (r.detailText   != null) r.detailText.text   = entry.Detail;

            bool hasDetail = !string.IsNullOrEmpty(entry.Detail);
            if (r.expandButton != null)
                r.expandButton.gameObject.SetActive(hasDetail);

            if (r.detailPanel != null)
                r.detailPanel.SetActive(false);

            if (r.expandLabel != null)
                r.expandLabel.text = "▼";
        }

        private static void ToggleDetail(RowState r)
        {
            if (r.detailPanel == null) return;
            bool nowOpen = !r.detailPanel.activeSelf;
            r.detailPanel.SetActive(nowOpen);
            if (r.expandLabel != null)
                r.expandLabel.text = nowOpen ? "▲" : "▼";
        }

        // ── Text helpers ──────────────────────────────────────────────────────

        private static int GetTurn() =>
            TimeManager.Instance != null ? TimeManager.Instance.CurrentTurn : 0;

        private static string CategoryLabel(ReportCategory cat)
        {
            switch (cat)
            {
                case ReportCategory.Combat:    return "COMBAT";
                case ReportCategory.Diplomacy: return "DIPL";
                case ReportCategory.Intel:     return "INTEL";
                default:                       return "—";
            }
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

        private static string BuildDiplomacySummary(DiplomaticState state, CivEnum other)
        {
            switch (state)
            {
                case DiplomaticState.War:     return $"War declared with {other}";
                case DiplomaticState.Neutral: return $"Relations with {other}: neutral";
                case DiplomaticState.Peace:   return $"Peace accord with {other}";
                case DiplomaticState.Allied:  return $"Alliance forged with {other}";
                default:                      return $"Diplomacy changed with {other}";
            }
        }

        private static string BuildDiplomacyDetail(DiplomaticState state, CivEnum local, CivEnum other)
        {
            switch (state)
            {
                case DiplomaticState.War:
                    return $"A state of war now exists between {local} and {other}. All peace treaties are void.";
                case DiplomaticState.Neutral:
                    return $"Relations between {local} and {other} have settled at neutral.";
                case DiplomaticState.Peace:
                    return $"A peace accord has been reached between {local} and {other}.";
                case DiplomaticState.Allied:
                    return $"An alliance has been forged between {local} and {other}. Mutual defense is now active.";
                default:
                    return "";
            }
        }

        // ── Child find helpers ────────────────────────────────────────────────

        private static TextMeshProUGUI FindTMP(GameObject root, string childName)
        {
            GameObject go = FindChild(root, childName);
            return go != null ? go.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Button FindButton(GameObject root, string childName)
        {
            GameObject go = FindChild(root, childName);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static GameObject FindChild(GameObject root, string childName)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t.gameObject;
            return null;
        }
    }
}
