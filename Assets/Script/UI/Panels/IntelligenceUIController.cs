using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;

/*
 * UNITY PREFAB REQUIREMENTS — wire these in the Inspector:
 *
 *  IntelPanel (root, this MonoBehaviour)
 *  ├── Header
 *  │   └── IntelPointsText          (TMP)   "Intel Points: 47"
 *  ├── CivListScrollView
 *  │   └── Viewport/CivListContent  (Transform) civListContainer
 *  │       └── [CivEntryPrefab instances added at runtime]
 *  ├── ActiveProjectsPanel
 *  │   └── ActiveProjectsContent    (Transform) activeProjectsContainer
 *  │       └── [ProjectEntryPrefab instances added at runtime]
 *  ├── FeedbackText                 (TMP)   result / error messages
 *  ├── LaunchButton                 (Button) launch selected project
 *  └── CloseButton                  (Button)
 *
 *  CivEntryPrefab needs:
 *    - CivNameText  (TMP)
 *    - TechLevelText (TMP)
 *    - SelectButton  (Button)
 *    - CivEntryUI component (handles its own button wiring)
 *
 *  ProjectEntryPrefab needs:
 *    - TargetText      (TMP)  e.g. "vs VULCANS"
 *    - TurnsLeftText   (TMP)  e.g. "6 turns remaining"
 */

namespace BOTF3D.UI
{
    public class IntelligenceUIController : MonoBehaviour
    {
        public static IntelligenceUIController Instance;

        [Header("Wire in Inspector")]
        [SerializeField] private TextMeshProUGUI intelPointsText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Button launchButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform civListContainer;
        [SerializeField] private Transform activeProjectsContainer;
        [SerializeField] private GameObject civEntryPrefab;       // prefab: CivNameText + SelectButton
        [SerializeField] private GameObject projectEntryPrefab;   // prefab: TargetText + TurnsLeftText

        private CivEnum _selectedTarget = CivEnum.None;
        private BOTF3D.Civilization.CivController _playerCiv;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (launchButton != null)
                launchButton.onClick.AddListener(OnLaunchClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            gameObject.SetActive(false);
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the panel for the local player's civ. Call from GalaxyMenuUIController.
        /// </summary>
        public void Show(BOTF3D.Civilization.CivController playerCiv)
        {
            _playerCiv = playerCiv;
            _selectedTarget = CivEnum.None;
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>
        /// Called by IntelligenceManager when a project resolves so the UI stays in sync.
        /// </summary>
        public void OnProjectResolved(CivEnum agentCivEnum, EspionageProject project)
        {
            if (_playerCiv == null || _playerCiv.CivData.CivEnum != agentCivEnum) return;

            if (project.State == EspionageProjectState.Succeeded)
                ShowFeedback($"SUCCESS: Stole {project.TechPointsStolen} tech points from {project.TargetCivEnum}!");
            else
                ShowFeedback($"FAILED: Espionage operation against {project.TargetCivEnum} was uncovered.");

            Refresh();
        }

        // ─── Display refresh ─────────────────────────────────────────────────────

        public void Refresh()
        {
            if (_playerCiv?.CivData == null) return;
            var data = _playerCiv.CivData;

            // Intel points header
            if (intelPointsText != null)
                intelPointsText.text = $"Intel Points: {(int)data.IntelPoints}  (+{data.IntelPointsPerTurn:F1}/turn)";

            // Launch button enabled only when a valid target is selected and we have enough intel
            if (launchButton != null)
                launchButton.interactable = _selectedTarget != CivEnum.None
                                         && data.IntelPoints >= EspionageProject.IntelCost;

            PopulateCivList();
            PopulateActiveProjects();
        }

        private void PopulateCivList()
        {
            if (civListContainer == null) return;

            // Clear old entries
            foreach (Transform child in civListContainer)
                Destroy(child.gameObject);

            if (IntelligenceManager.Instance == null || _playerCiv == null) return;

            var contacts = IntelligenceManager.Instance.GetContactedCivEnums(_playerCiv.CivData.CivEnum);
            foreach (var civEnum in contacts)
            {
                var targetCon = CivManager.Instance?.GetCivControllerByCivEnum(civEnum);
                if (targetCon == null) continue;
                var targetData = targetCon.CivData;

                // Non-warp civs shown greyed — no tech to steal
                bool canTarget = targetData.HasWarp;

                if (civEntryPrefab != null)
                {
                    var entry = Instantiate(civEntryPrefab, civListContainer);
                    var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
                    if (texts.Length > 0)
                        texts[0].text = targetData.CivShortName + (canTarget ? "" : " (no warp)");
                    if (texts.Length > 1)
                        texts[1].text = targetData.Playable ? targetData.CurrentTechLevel.ToString() : "—";

                    var btn = entry.GetComponentInChildren<Button>();
                    if (btn != null)
                    {
                        CivEnum captured = civEnum;
                        btn.interactable = canTarget;
                        btn.onClick.AddListener(() => SelectTarget(captured));
                    }
                }
                else
                {
                    // Fallback: just log (prefab not wired yet)
                    Debug.Log($"Intel target available: {targetData.CivShortName} (warp={canTarget})");
                }
            }
        }

        private void PopulateActiveProjects()
        {
            if (activeProjectsContainer == null) return;

            foreach (Transform child in activeProjectsContainer)
                Destroy(child.gameObject);

            if (_playerCiv?.CivData?.ActiveEspionageProjects == null) return;

            foreach (var project in _playerCiv.CivData.ActiveEspionageProjects)
            {
                if (project.IsComplete) continue;

                if (projectEntryPrefab != null)
                {
                    var entry = Instantiate(projectEntryPrefab, activeProjectsContainer);
                    var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
                    if (texts.Length > 0) texts[0].text = $"vs {project.TargetCivEnum}";
                    if (texts.Length > 1) texts[1].text = $"{project.TurnsRemaining} turns remaining";
                }
                else
                {
                    Debug.Log($"Active project: vs {project.TargetCivEnum}, {project.TurnsRemaining} turns left");
                }
            }
        }

        // ─── User interaction ────────────────────────────────────────────────────

        private void SelectTarget(CivEnum targetEnum)
        {
            _selectedTarget = targetEnum;
            ShowFeedback($"Selected: {targetEnum}. Cost: {EspionageProject.IntelCost} Intel Points.");
            if (launchButton != null)
                launchButton.interactable = _playerCiv != null
                                         && _playerCiv.CivData.IntelPoints >= EspionageProject.IntelCost;
        }

        private void OnLaunchClicked()
        {
            if (_playerCiv == null || _selectedTarget == CivEnum.None) return;
            if (IntelligenceManager.Instance == null) return;

            bool ok = IntelligenceManager.Instance.TryLaunchEspionageProject(
                _playerCiv, _selectedTarget, out string reason);

            ShowFeedback(reason);
            if (ok) Refresh();
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message;
            Debug.Log($"[IntelUI] {message}");
        }

        public void Initialize() { }
        public void UpdateState() { }
    }
}
