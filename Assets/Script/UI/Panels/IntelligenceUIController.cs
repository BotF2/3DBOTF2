using BOTF3D.Civilization;
using BOTF3D.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BOTF3D.UI
{
    /// <summary>
    /// Drives the Intel panel (intelMenuView in GalaxyMenuUIController).
    /// Attach to the root of the Intel panel GameObject in the GalaxyScene.
    ///
    /// Inspector wiring required:
    ///   civTableContainer      → scroll Content Transform inside CivIntelTable
    ///   activeProjectContainer → scroll Content Transform inside ActiveProjectPanel (optional)
    ///   civEntryPrefab         → Assets/PreFabs/DiplomacyIntel/CivEntryPrefab
    ///   projectEntryPrefab     → Assets/PreFabs/DiplomacyIntel/ProjectEntryPrefab (optional)
    ///   intelPointsText        → IntelPointsDisplay / IntelPoints TMP
    ///   perTurnRateText        → IntelPointsDisplay / PerTurnGenerationRate TMP
    ///   feedbackText           → FeedbackText TMP
    /// </summary>
    public class IntelligenceUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }

        public static IntelligenceUIController Instance;

        // ── Inspector fields ──────────────────────────────────────────────────
        [Header("Civ Intel Table")]
        [SerializeField] private Transform civTableContainer;
        [SerializeField] private GameObject civEntryPrefab;

        [Header("Active Project Panel (optional consolidated view)")]
        [SerializeField] private Transform activeProjectContainer;
        [SerializeField] private GameObject projectEntryPrefab;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI headerText;

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI intelPointsText;
        [SerializeField] private TextMeshProUGUI perTurnRateText;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Controls")]
        [SerializeField] private UnityEngine.UI.Button closeButton;

        // ── Kept from original stub ───────────────────────────────────────────
        public IntelligenceController IntelligenceController;
        public GameObject IntelUIToggle;
        public GameObject IntelUITable;

        // ── Stable row pools — rows are created once and reused in place ──────
        // This prevents Destroy+Instantiate on every refresh, which collapses
        // the Hierarchy and loses the user's expanded inspector state.
        private readonly List<GameObject> _civRows = new List<GameObject>();
        private readonly List<GameObject> _projectRows = new List<GameObject>();

        private bool _subscribed;
        private bool _hasPinnedCiv;
        private CivEnum _pinnedCivEnum;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("IntelligenceUIController: duplicate detected — overwriting Instance.");
            Instance = this;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => GalaxyMenuUIController.Instance?.CloseCurrentMenu());
            }
        }

        private void OnEnable()
        {
            IntelligenceManager.OnProjectResolved += OnIntelProjectResolved;
            IntelligenceManager.OnNewContact += PinCiv;
            TrySubscribe();
            RefreshPanel();

            if (IsLocalPlayerBorg())
                StartCoroutine(BorgAutoClose());
        }

        private void OnDisable()
        {
            IntelligenceManager.OnProjectResolved -= OnIntelProjectResolved;
            IntelligenceManager.OnNewContact -= PinCiv;
            if (_subscribed)
            {
                if (TimeManager.Instance != null)
                    TimeManager.Instance.OnTurnAdvanced -= RefreshPanel;
                _subscribed = false;
            }
        }

        private void TrySubscribe()
        {
            if (!_subscribed && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTurnAdvanced += RefreshPanel;
                _subscribed = true;
            }
        }

        private void OnDestroy()
        {
            // Pools are children of their containers so Unity cleans them up;
            // clear the lists so stale refs can't be accessed after destroy.
            _civRows.Clear();
            _projectRows.Clear();
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void RefreshPanel()
        {
            if (IntelligenceManager.Instance == null) return;
            TrySubscribe(); // catch the case where TimeManager wasn't ready at OnEnable

            if (IsLocalPlayerBorg())
            {
                SetBorgTexts();
                return;
            }

            RefreshIntelPoints();
            RefreshCivTable();
            RefreshActiveProjectPanel();
        }

        public void PinCiv(CivEnum civEnum)
        {
            _pinnedCivEnum = civEnum;
            _hasPinnedCiv = true;
            RefreshPanel();
        }

        public void SetFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message;
        }

        // ── Borg-specific behaviour ───────────────────────────────────────────

        private bool IsLocalPlayerBorg()
        {
            return CivManager.Instance?.LocalPlayerCivController?.CivData.CivEnum == CivEnum.BORG;
        }

        private void SetBorgTexts()
        {
            if (headerText != null) headerText.text = GetAgencyName(CivEnum.BORG);
            if (feedbackText != null) feedbackText.text = BOTF3D.Core.Loc.Get("Resistance is Futile");
            if (intelPointsText != null) intelPointsText.text = "";
            if (perTurnRateText != null) perTurnRateText.text = "";
        }

        private System.Collections.IEnumerator BorgAutoClose()
        {
            yield return new WaitForSecondsRealtime(2f);
            GalaxyMenuUIController.Instance?.CloseCurrentMenu();
        }

        // ── Private refresh methods ───────────────────────────────────────────

        private void RefreshIntelPoints()
        {
            CivController localCiv = CivManager.Instance?.LocalPlayerCivController;
            if (localCiv == null) return;

            if (headerText != null)
                headerText.text = GetAgencyName(localCiv.CivData.CivEnum);
            if (intelPointsText != null)
                intelPointsText.text = localCiv.CivData.IntelPoints.ToString("F0");
            if (perTurnRateText != null)
                perTurnRateText.text = $"+{CalculateIntelPerTurn(localCiv)}/turn";
        }

        private static string GetAgencyName(CivEnum civ)
        {
            switch (civ)
            {
                case CivEnum.FED: return "Starfleet Intelligence / Section 31";
                case CivEnum.ROM: return "Tal Shiar";
                case CivEnum.KLING: return "Imperial Intelligence";
                case CivEnum.CARD: return "Obsidian Order";
                case CivEnum.DOM: return "Founder Intelligence";
                case CivEnum.TERRAN: return "Section 31";
                case CivEnum.BORG: return "Borg Intelligence Node";
                default: return "Intelligence";
            }
        }

        private void RefreshCivTable()
        {
            if (civTableContainer == null || civEntryPrefab == null) return;

            CivController localCiv = CivManager.Instance?.LocalPlayerCivController;
            if (localCiv == null) return;

            // Build the current contact list
            CivEnum localEnum = localCiv.CivData.CivEnum;
            var contacts = BuildContactList(localEnum, localCiv);

            // Grow the pool only when new contacts appear — never destroy
            while (_civRows.Count < contacts.Count)
                _civRows.Add(Instantiate(civEntryPrefab, civTableContainer));

            // Update visible rows in place (same GameObjects, new data)
            for (int i = 0; i < contacts.Count; i++)
            {
                _civRows[i].SetActive(true);
                _civRows[i].GetComponent<CivIntelEntryUI>()?.Populate(
                    contacts[i].intelCon, localCiv, contacts[i].targetCiv, contacts[i].diplomaCon);
            }

            // Hide any surplus rows (contacts can never shrink mid-game, but be safe)
            for (int i = contacts.Count; i < _civRows.Count; i++)
                _civRows[i].SetActive(false);
        }

        private void RefreshActiveProjectPanel()
        {
            if (activeProjectContainer == null || projectEntryPrefab == null) return;

            CivEnum localEnum = GameController.Instance.GameData.LocalPlayerCivEnum;

            // Collect all active local-player projects across all civ pairs
            var projects = new List<IntelProject>();
            foreach (var intelCon in IntelligenceManager.Instance.IntelligenceControllerList)
            {
                if (intelCon?.IntelligenceData?.ActiveProjects == null) continue;
                foreach (var p in intelCon.IntelligenceData.ActiveProjects)
                    if (p.InitiatorCiv == localEnum && !p.IsComplete)
                        projects.Add(p);
            }

            // Grow pool only when project count increases — never destroy
            while (_projectRows.Count < projects.Count)
                _projectRows.Add(Instantiate(projectEntryPrefab, activeProjectContainer));

            // Update visible rows in place
            for (int i = 0; i < projects.Count; i++)
            {
                _projectRows[i].SetActive(true);
                _projectRows[i].GetComponent<ProjectEntryUI>()?.Populate(projects[i]);
            }

            // Hide finished / surplus rows
            for (int i = projects.Count; i < _projectRows.Count; i++)
                _projectRows[i].SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private struct ContactEntry
        {
            public IntelligenceController intelCon;
            public CivController targetCiv;
            public DiplomacyController diplomaCon;
        }

        private List<ContactEntry> BuildContactList(CivEnum localEnum, CivController localCiv)
        {
            var list = new List<ContactEntry>();
            foreach (var intelCon in IntelligenceManager.Instance.IntelligenceControllerList)
            {
                if (intelCon?.IntelligenceData == null) continue;
                if (intelCon.IntelligenceData.CivSideOne != localEnum &&
                    intelCon.IntelligenceData.CivSideTwo != localEnum) continue;

                CivEnum otherEnum = intelCon.IntelligenceData.CivSideOne == localEnum
                    ? intelCon.IntelligenceData.CivSideTwo
                    : intelCon.IntelligenceData.CivSideOne;

                CivController targetCiv = CivManager.Instance.GetCivControllerByCivEnum(otherEnum);
                if (targetCiv == null) continue;

                DiplomacyController diplomaCon = DiplomacyManager.Instance
                    .ReturnADiplomacyController(localEnum, otherEnum);

                list.Add(new ContactEntry
                {
                    intelCon = intelCon,
                    targetCiv = targetCiv,
                    diplomaCon = diplomaCon
                });
            }

            if (_hasPinnedCiv)
            {
                int pinIdx = list.FindIndex(e => e.targetCiv.CivData.CivEnum == _pinnedCivEnum);
                if (pinIdx > 0)
                {
                    var pinned = list[pinIdx];
                    list.RemoveAt(pinIdx);
                    list.Insert(0, pinned);
                }
            }

            return list;
        }

        private void OnIntelProjectResolved(SecretActionsEnum action, CivEnum initiator, CivEnum target,
            bool succeeded, bool discovered, int techGain)
        {
            if (GameController.Instance == null) return;
            if (initiator != GameController.Instance.GameData.LocalPlayerCivEnum) return;

            string opName = FormatAction(action);
            string msg;
            if (succeeded)
            {
                msg = action == SecretActionsEnum.IntellectualTheft
                    ? $"{opName} against {target} succeeded! Your agents obtained {techGain} tech points."
                    : $"{opName} against {target} succeeded!";
            }
            else if (discovered)
            {
                msg = $"{opName} against {target} was discovered! Diplomatic relations damaged.";
            }
            else
            {
                msg = $"{opName} against {target} failed.";
            }

            SetFeedback(msg);
            RefreshPanel();
        }

        private static string FormatAction(SecretActionsEnum action)
        {
            switch (action)
            {
                case SecretActionsEnum.IntellectualTheft: return "Tech Theft";
                case SecretActionsEnum.GatherIntelligence: return "Gather Intel";
                case SecretActionsEnum.Sabotage: return "Sabotage";
                case SecretActionsEnum.Disinformation: return "Disinformation";
                default: return action.ToString();
            }
        }

        // ToDo: derive from owned star systems once per-system intel output is implemented
        private static int CalculateIntelPerTurn(CivController civ) => 10;
    }
}
