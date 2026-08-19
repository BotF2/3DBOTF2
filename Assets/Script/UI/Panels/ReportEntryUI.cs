using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;

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
    ///   "TurnText"      TextMeshProUGUI  — stardate stamp
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
            // Note: GameEvents.OnCombatEnded is intentionally NOT subscribed here. Combat reports
            // are already pushed by CombatUIManager.PushCombatReportEntry (the richer, per-side
            // breakdown) from the active turn-based combat path; subscribing here too would push
            // a second, duplicate, lower-detail entry for the same combat.
            IntelligenceManager.OnProjectResolved += OnIntelResolved;
            IntelligenceManager.OnNewContact      += OnNewContact;
            GameEvents.OnDiplomacyChanged         += OnDiplomacyChanged;
            GameEvents.OnSystemOwnershipChanged   += OnSystemOwnershipChanged;
            GameEvents.OnCivEliminated            += OnCivEliminated;
            GameEvents.OnDiplomaticRipple         += OnDiplomaticRipple;
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
            GameEvents.OnDiplomacyChanged         -= OnDiplomacyChanged;
            GameEvents.OnSystemOwnershipChanged   -= OnSystemOwnershipChanged;
            GameEvents.OnCivEliminated            -= OnCivEliminated;
            GameEvents.OnDiplomaticRipple         -= OnDiplomaticRipple;
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

            PushReport(new ReportEntry(ReportCategory.Intel, GetStardate(), summary, detail));
        }

        private void OnNewContact(CivEnum civ)
        {
            PushReport(new ReportEntry(ReportCategory.Intel, GetStardate(),
                $"First contact: {civ}",
                $"Your forces have established first contact with the {civ} civilization."));
        }

        private void OnSystemOwnershipChanged(string systemName, CivEnum previousOwner, CivEnum newOwner)
        {
            if (GameController.Instance == null) return;
            CivEnum local = GameController.Instance.GameData.LocalPlayerCivEnum;

            // Only the local player's own gains/losses are report-worthy - AI-vs-AI ownership
            // changes elsewhere in the galaxy would otherwise flood this feed every turn.
            bool weGained = newOwner == local && previousOwner != local;
            bool weLost   = previousOwner == local && newOwner != local;
            if (!weGained && !weLost) return;

            StarSysController sysCon = StarSysManager.Instance != null
                ? StarSysManager.Instance.StarSysControllerList
                    .Find(s => s != null && s.StarSysData != null && s.StarSysData.SysName == systemName)
                : null;
            GalaxyQuadrant? quadrant = sysCon != null
                ? ReportEntry.QuadrantFromPosition(sysCon.StarSysData.GetPosition())
                : (GalaxyQuadrant?)null;

            string summary  = weGained ? $"{systemName} captured" : $"{systemName} lost";
            string detail   = weGained
                ? $"{systemName} has come under your control (previously held by {previousOwner})."
                : $"{systemName} has fallen to {newOwner}.";
            ReportSeverity severity = weGained ? ReportSeverity.Info : ReportSeverity.Critical;

            PushReport(new ReportEntry(ReportCategory.Combat, GetStardate(), summary, detail,
                systemName, quadrant, severity));
        }

        private void OnDiplomacyChanged(CivEnum civA, CivEnum civB, DiplomacyStatusEnum status)
        {
            if (GameController.Instance == null) return;
            CivEnum local = GameController.Instance.GameData.LocalPlayerCivEnum;
            if (civA != local && civB != local) return;

            CivEnum other   = civA == local ? civB : civA;
            string  summary = BuildDiplomacySummary(status, other);
            string  detail  = BuildDiplomacyDetail(status, local, other);
            PushReport(new ReportEntry(ReportCategory.Diplomacy, GetStardate(), summary, detail,
                severity: DiplomacySeverity(status)));
        }

        private void OnCivEliminated(CivEnum eliminatedCiv, CivEnum absorbedByCiv)
        {
            if (GameController.Instance == null) return;
            CivEnum local = GameController.Instance.GameData.LocalPlayerCivEnum;
            if (eliminatedCiv == local) return;

            // Only report civs we've actually had contact with - a DiplomacyController record for
            // this pair only exists after an encounter, so its absence means "never met."
            bool weKnowThem = DiplomacyManager.Instance != null &&
                DiplomacyManager.Instance.ReturnADiplomacyController(local, eliminatedCiv) != null;
            if (!weKnowThem) return;

            bool   weAbsorbedThem = absorbedByCiv == local;
            string summary = weAbsorbedThem
                ? $"{eliminatedCiv} annexed into our empire"
                : $"{eliminatedCiv} has been eliminated";
            string detail = weAbsorbedThem
                ? $"The {eliminatedCiv} civilization has been fully annexed into {absorbedByCiv}."
                : $"The {eliminatedCiv} civilization has been absorbed by {absorbedByCiv} and no longer exists as an independent power.";
            ReportSeverity severity = weAbsorbedThem ? ReportSeverity.Info : ReportSeverity.Warning;

            PushReport(new ReportEntry(ReportCategory.Diplomacy, GetStardate(), summary, detail, severity: severity));
        }

        private void OnDiplomaticRipple(CivEnum actor, CivEnum thirdParty, CivEnum causingTarget, int pointDelta, DiplomaticEventEnum eventType)
        {
            if (GameController.Instance == null) return;
            CivEnum local = GameController.Instance.GameData.LocalPlayerCivEnum;
            if (actor != local && thirdParty != local) return;

            CivEnum otherParty = actor == local ? thirdParty : actor;
            string  direction  = pointDelta > 0 ? "improved" : "worsened";
            string  summary    = $"{otherParty} relations {direction} ({eventType})";
            string  detail     = $"Relations between {actor} and {thirdParty} shifted by {pointDelta:+0;-0} points " +
                                  $"due to {actor}'s dealings with {causingTarget} ({eventType}).";
            ReportSeverity severity = pointDelta > 0 ? ReportSeverity.Info : ReportSeverity.Warning;

            PushReport(new ReportEntry(ReportCategory.Diplomacy, GetStardate(), summary, detail, severity: severity));
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
            PushReport(new ReportEntry(ReportCategory.Combat, 1, "Victory — Combat ships lost 0, surviving 4. Transports lost 0, surviving 2.",
                "Side 1 destroyed 3 ships. Side 2 retreated.", "Vulcan", GalaxyQuadrant.Alpha, ReportSeverity.Info));
            PushReport(new ReportEntry(ReportCategory.Combat, 1, "Defeat — Combat ships lost 5, surviving 0. Transports lost 1, surviving 0.",
                "All combat ships lost.", "Qo'noS", GalaxyQuadrant.Beta, ReportSeverity.Critical));
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
            if (r.turnText     != null) r.turnText.text     = $"SD{entry.Stardate}";
            if (r.summaryText  != null) r.summaryText.text  = SeverityColor(entry.Severity, BuildSummaryLine(entry));
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

        private static int GetStardate() =>
            TimeManager.Instance != null ? TimeManager.Instance.currentStardate : 0;

        private static string BuildSummaryLine(ReportEntry entry)
        {
            if (string.IsNullOrEmpty(entry.Location))
                return entry.Summary;

            string quadrant = entry.Quadrant.HasValue ? $"{entry.Quadrant.Value} Quadrant" : "unknown quadrant";
            return $"{entry.Summary} — near {entry.Location} ({quadrant})";
        }

        private static string SeverityColor(ReportSeverity severity, string text)
        {
            switch (severity)
            {
                case ReportSeverity.Critical: return $"<color=#FF5050>{text}</color>";
                case ReportSeverity.Warning:  return $"<color=#FFD050>{text}</color>";
                default:                      return text;
            }
        }

        private static string CategoryLabel(ReportCategory cat)
        {
            switch (cat)
            {
                case ReportCategory.Combat:    return BOTF3D.Core.Loc.Get("Report.Combat",    "COMBAT");
                case ReportCategory.Diplomacy: return BOTF3D.Core.Loc.Get("Report.Diplomacy", "DIPL");
                case ReportCategory.Intel:     return BOTF3D.Core.Loc.Get("Report.Intel",     "INTEL");
                default:                       return "—";
            }
        }

        private static string FormatAction(SecretActionsEnum action)
        {
            switch (action)
            {
                case SecretActionsEnum.IntellectualTheft:  return BOTF3D.Core.Loc.Get("Intel.TechTheft",      "Tech Theft");
                case SecretActionsEnum.GatherIntelligence: return BOTF3D.Core.Loc.Get("Intel.GatherIntel",    "Gather Intel");
                case SecretActionsEnum.Sabotage:           return BOTF3D.Core.Loc.Get("Intel.Sabotage",       "Sabotage");
                case SecretActionsEnum.Disinformation:     return BOTF3D.Core.Loc.Get("Intel.Disinformation", "Disinformation");
                default:                                   return action.ToString();
            }
        }

        private static string BuildDiplomacySummary(DiplomacyStatusEnum status, CivEnum other)
        {
            switch (status)
            {
                case DiplomacyStatusEnum.War:        return BOTF3D.Core.Loc.Format("Dipl.War",        "War declared with {0}",              other);
                case DiplomacyStatusEnum.ColdWar:     return BOTF3D.Core.Loc.Format("Dipl.ColdWar",    "Cold war tensions with {0}",          other);
                case DiplomacyStatusEnum.Hostile:     return BOTF3D.Core.Loc.Format("Dipl.Hostile",    "Relations with {0} turn hostile",     other);
                case DiplomacyStatusEnum.UnFriendly:  return BOTF3D.Core.Loc.Format("Dipl.UnFriendly", "Relations with {0} sour",             other);
                case DiplomacyStatusEnum.Neutral:     return BOTF3D.Core.Loc.Format("Dipl.Neutral",    "Relations with {0}: neutral",         other);
                case DiplomacyStatusEnum.Friendly:    return BOTF3D.Core.Loc.Format("Dipl.Friendly",   "Relations with {0} improve: friendly",other);
                case DiplomacyStatusEnum.Allied:      return BOTF3D.Core.Loc.Format("Dipl.Allied",     "Alliance forged with {0}",             other);
                case DiplomacyStatusEnum.Membership:  return BOTF3D.Core.Loc.Format("Dipl.Membership", "{0} petitions for membership",        other);
                default:                              return BOTF3D.Core.Loc.Format("Dipl.Changed",    "Diplomacy changed with {0}",          other);
            }
        }

        private static string BuildDiplomacyDetail(DiplomacyStatusEnum status, CivEnum local, CivEnum other)
        {
            switch (status)
            {
                case DiplomacyStatusEnum.War:
                    return BOTF3D.Core.Loc.Format("Dipl.War.Detail",
                        "A state of war now exists between {0} and {1}. All peace treaties are void.", local, other);
                case DiplomacyStatusEnum.ColdWar:
                    return BOTF3D.Core.Loc.Format("Dipl.ColdWar.Detail",
                        "Relations between {0} and {1} have deteriorated into a cold war.", local, other);
                case DiplomacyStatusEnum.Hostile:
                    return BOTF3D.Core.Loc.Format("Dipl.Hostile.Detail",
                        "Relations between {0} and {1} have turned hostile.", local, other);
                case DiplomacyStatusEnum.UnFriendly:
                    return BOTF3D.Core.Loc.Format("Dipl.UnFriendly.Detail",
                        "Relations between {0} and {1} have soured.", local, other);
                case DiplomacyStatusEnum.Neutral:
                    return BOTF3D.Core.Loc.Format("Dipl.Neutral.Detail",
                        "Relations between {0} and {1} have settled at neutral.", local, other);
                case DiplomacyStatusEnum.Friendly:
                    return BOTF3D.Core.Loc.Format("Dipl.Friendly.Detail",
                        "Relations between {0} and {1} have warmed to friendly.", local, other);
                case DiplomacyStatusEnum.Allied:
                    return BOTF3D.Core.Loc.Format("Dipl.Allied.Detail",
                        "An alliance has been forged between {0} and {1}. Mutual defense is now active.", local, other);
                case DiplomacyStatusEnum.Membership:
                    return BOTF3D.Core.Loc.Format("Dipl.Membership.Detail",
                        "{1} has grown close enough to {0} to petition for membership.", local, other);
                default:
                    return "";
            }
        }

        private static ReportSeverity DiplomacySeverity(DiplomacyStatusEnum status)
        {
            switch (status)
            {
                case DiplomacyStatusEnum.War:                              return ReportSeverity.Critical;
                case DiplomacyStatusEnum.ColdWar:
                case DiplomacyStatusEnum.Hostile:
                case DiplomacyStatusEnum.UnFriendly:                       return ReportSeverity.Warning;
                default:                                                   return ReportSeverity.Info;
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
