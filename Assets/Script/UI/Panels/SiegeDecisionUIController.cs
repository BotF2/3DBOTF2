using BOTF3D.Core;
using BOTF3D.Galaxy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



namespace BOTF3D.UI
{
    /// <summary>
    /// Phase B entry-gate panel (System Invasion Phase 1, Docs/Design/
    /// SystemInvasion_Phase1_Design.md §4.1). Presents three choices once Phase A space combat ends
    /// with a system's orbital defenses cleared:
    ///   - Withdraw       → ends the siege, frees the fleet (same as Break Off Siege)
    ///   - Target Troops  → commits to Phase B attrition (abstract, stub until Invasion.5)
    ///   - Total Destruction → instantly destroys all facilities, zeroes population/ground forces,
    ///                         claims the system for the attacker (see StarSysManager.ResolveTotalDestruction)
    ///
    /// Called from two paths:
    ///   1. Fleet UI "Assault System" button (FleetMenuUIController.ClickSystemAssaultButton) —
    ///      player-initiated at any time while AssaultMode == None.
    ///   2. TurnEventQueue.SiegeDecision drain — fires every InterTurn for each besieged system
    ///      where AssaultMode is still None, guaranteeing the prompt even if the player ignores
    ///      the Fleet UI button (replaces ShowSiegeDecisionStub from Invasion.2).
    ///
    /// Wire-up (Unity Editor):
    ///   - Add this component to a persistent GameObject (PersistentScene recommended).
    ///   - Set PanelRoot to the root GameObject of the decision panel UI (starts disabled/inactive).
    ///   - Set SysNameLabel, OwnerLabel, WithdrawButton, TargetTroopsButton, TotalDestructionButton
    ///     to their respective UI elements inside PanelRoot.
    /// </summary>
    public class SiegeDecisionUIController : MonoBehaviour
    {
        public static SiegeDecisionUIController Instance;

        [Header("Panel root — disabled until OpenPanel() activates it")]
        public GameObject PanelRoot;

        [Header("Labels")]
        [SerializeField] private TMP_Text sysNameLabel;
        [SerializeField] private TMP_Text ownerLabel;

        [Header("Live stats — updated every Phase B tick via RefreshStats()")]
        [SerializeField] private TMP_Text shieldText;         // ShieldText
        [SerializeField] private TMP_Text sysTroopsText;      // SysTroopsText
        [SerializeField] private TMP_Text transportTroopsText; // TransportTroopsText
        [SerializeField] private TMP_Text landedTroopText;    // LandedTroopText
        // PowerOutputText intentionally not wired yet — leave the field here for future use.
        [SerializeField] private TMP_Text powerOutputText;

        [Header("Buttons")]
        [SerializeField] private Button withdrawButton;
        [SerializeField] private Button targetTroopsButton;
        [SerializeField] private Button totalDestructionButton;

        private StarSysController currentSys;
        private FleetController currentFleet;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (PanelRoot != null) PanelRoot.SetActive(false);

            if (withdrawButton != null)
                withdrawButton.onClick.AddListener(OnWithdraw);
            if (targetTroopsButton != null)
                targetTroopsButton.onClick.AddListener(OnTargetTroops);
            if (totalDestructionButton != null)
                totalDestructionButton.onClick.AddListener(OnTotalDestruction);
        }

        /// <summary>
        /// Shows the panel populated for the given system/fleet pair.
        /// Safe to call when PanelRoot is null (logs a warning — Editor wiring needed).
        /// </summary>
        public void OpenPanel(StarSysController sysCon, FleetController fleet)
        {
            if (PanelRoot == null)
            {
                Debug.LogWarning("[SiegeDecision] PanelRoot not assigned — wire it in the Inspector.");
                return;
            }

            currentSys = sysCon;
            currentFleet = fleet;

            var data = sysCon?.StarSysData;
            if (sysNameLabel != null)
                sysNameLabel.text = data?.SysName ?? "Unknown System";
            if (ownerLabel != null)
                ownerLabel.text = $"Defending: {data?.CurrentOwnerCivEnum}";

            // Target Troops needs at least one transport with ground forces loaded to be meaningful.
            // Don't hide the button (player may still want to bombard with no troops), but gray it
            // out and adjust the label so the intent is clear.
            bool hasLoadedTroops = fleet != null && fleet.FleetData != null
                && fleet.FleetData.ShipsList != null
                && fleet.FleetData.ShipsList.Exists(s => s != null && s.ShipData != null
                    && s.ShipData.ShipType == ShipType.Transport
                    && s.ShipData.LoadedGroundForces > 0);

            if (targetTroopsButton != null)
            {
                targetTroopsButton.interactable = true;
                var lbl = targetTroopsButton.GetComponentInChildren<TMP_Text>();
                if (lbl != null)
                    lbl.text = hasLoadedTroops ? "Target Troops" : "Target Troops\n(no troops loaded)";
            }

            // Populate the live-stat labels before opening so the first frame shows real numbers.
            PopulateStats(data, fleet);

            // Ensure the panel renders on top of galaxy and fleet UI canvases (which use sortingOrder 0).
            var canvas = PanelRoot.GetComponentInParent<Canvas>(true);
            if (canvas != null) canvas.sortingOrder = 100;

            PanelRoot.SetActive(true);
        }

        /// <summary>
        /// Called by StarSysManager after each Phase B attrition tick so the panel reflects the
        /// current combat state while AssaultMode is still None (entry gate) or TargetTroops.
        /// Safe to call when the panel is closed — no-ops silently.
        /// </summary>
        public void RefreshStats(StarSysController sysCon, FleetController fleet)
        {
            if (PanelRoot == null || !PanelRoot.activeSelf) return;
            PopulateStats(sysCon?.StarSysData, fleet);
        }

        private void PopulateStats(StarSysData data, FleetController fleet)
        {
            if (data == null) return;

            // Shield HP — shown as current / max (or "Down" once cleared)
            if (shieldText != null)
            {
                if (data.PhaseBShieldsDown)
                    shieldText.text = "Shields: Down";
                else if (data.PhaseBShieldMaxHP > 0)
                    shieldText.text = $"Shields: {data.PhaseBShieldHP:F0} / {data.PhaseBShieldMaxHP:F0}";
                else
                    shieldText.text = $"Shields: {data.ShieldGenerators?.Count ?? 0} generator(s)";
            }

            // Defending troop count
            if (sysTroopsText != null)
                sysTroopsText.text = $"Def. Troops: {data.GroundForces?.Count ?? 0}";

            // Troops currently loaded across all transports
            if (transportTroopsText != null)
            {
                int loaded = 0;
                if (fleet?.FleetData?.ShipsList != null)
                    foreach (var s in fleet.FleetData.ShipsList)
                        if (s != null && s.ShipData != null) loaded += s.ShipData.LoadedGroundForces;
                transportTroopsText.text = $"Transport Troops: {loaded}";
            }

            // Landed attacker troops (only meaningful once Phase B ground phase starts)
            if (landedTroopText != null)
            {
                if (data.PhaseBTroopsLanded)
                    landedTroopText.text = $"Landed Troops: {data.PhaseBAttackerTroopHP:F0} HP";
                else
                    landedTroopText.text = "Landed Troops: —";
            }
        }

        private void ClosePanel()
        {
            if (PanelRoot != null) PanelRoot.SetActive(false);
            currentSys = null;
            currentFleet = null;
            TurnEventQueue.Instance?.NotifyDismissed();
            FleetMenuUIController.Instance?.SetupFleetUIData();
        }

        private void OnWithdraw()
        {
            if (currentFleet != null)
                currentFleet.RequestBreakOffSiege();
            ClosePanel();
        }

        private void OnTargetTroops()
        {
            if (currentSys?.StarSysData == null) { ClosePanel(); return; }

            currentSys.StarSysData.AssaultMode = AssaultMode.TargetTroops;
            StarSysManager.Instance?.InitializePhaseB(currentSys, currentFleet);

            int stardate = TimeManager.Instance != null ? TimeManager.Instance.currentStardate : 0;
            var data = currentSys.StarSysData;
            GalaxyQuadrant quadrant = ReportEntry.QuadrantFromPosition(data.GetPosition());
            ReportEntryUI.PushReport(new ReportEntry(
                ReportCategory.Combat, stardate,
                $"Assault underway: {data.SysName}",
                $"{data.BesiegingCivEnum} has begun a ground assault on {data.SysName}. " +
                $"Phase B attrition resolution is not yet implemented — fleet remains at the system.",
                data.SysName, quadrant, ReportSeverity.Warning));

            Debug.Log($"[Siege] '{data.SysName}': AssaultMode set to TargetTroops (Phase B attrition stub).");
            ClosePanel();
        }

        private void OnTotalDestruction()
        {
            if (currentSys == null || currentFleet == null) { ClosePanel(); return; }

            // Shields must be bombarded down before Total Destruction resolves — InitializePhaseB
            // starts the shield attrition; ResolveTotalDestruction fires automatically once they fall.
            currentSys.StarSysData.AssaultMode = AssaultMode.TotalDestruction;
            StarSysManager.Instance?.InitializePhaseB(currentSys, currentFleet);
            ClosePanel();
        }
    }
}
