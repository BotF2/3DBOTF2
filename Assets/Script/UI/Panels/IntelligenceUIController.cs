using System.Collections.Generic;
using TMPro;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;

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

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI intelPointsText;
        [SerializeField] private TextMeshProUGUI perTurnRateText;
        [SerializeField] private TextMeshProUGUI feedbackText;

        // ── Kept from original stub ───────────────────────────────────────────
        public IntelligenceController IntelligenceController;
        public GameObject IntelUIToggle;
        public GameObject IntelUITable;

        // ── Stable row pools — rows are created once and reused in place ──────
        // This prevents Destroy+Instantiate on every refresh, which collapses
        // the Hierarchy and loses the user's expanded inspector state.
        private readonly List<GameObject> _civRows     = new List<GameObject>();
        private readonly List<GameObject> _projectRows = new List<GameObject>();

        // Guard against double-subscription if OnEnable fires without OnDisable
        private bool _subscribed;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("IntelligenceUIController: duplicate detected — overwriting Instance.");
            Instance = this;
        }

        private void OnEnable()
        {
            if (!_subscribed && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTurnAdvanced += RefreshPanel;
                _subscribed = true;
            }
            RefreshPanel();
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                if (TimeManager.Instance != null)
                    TimeManager.Instance.OnTurnAdvanced -= RefreshPanel;
                _subscribed = false;
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

            RefreshIntelPoints();
            RefreshCivTable();
            RefreshActiveProjectPanel();
        }

        public void SetFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message;
        }

        // ── Private refresh methods ───────────────────────────────────────────

        private void RefreshIntelPoints()
        {
            CivController localCiv = CivManager.Instance?.LocalPlayerCivController;
            if (localCiv == null) return;

            if (intelPointsText != null)
                intelPointsText.text = localCiv.CivData.IntelPoints.ToString("F0");
            if (perTurnRateText != null)
                perTurnRateText.text = $"+{CalculateIntelPerTurn(localCiv)}/turn";
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
            public CivController          targetCiv;
            public DiplomacyController    diplomaCon;
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
                    intelCon   = intelCon,
                    targetCiv  = targetCiv,
                    diplomaCon = diplomaCon
                });
            }
            return list;
        }

        // ToDo: derive from owned star systems once per-system intel output is implemented
        private static int CalculateIntelPerTurn(CivController civ) => 10;
    }
}
