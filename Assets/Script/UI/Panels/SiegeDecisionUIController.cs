using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.Galaxy;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;



namespace BOTF3D.UI
{
    /// <summary>
    /// Phase B entry-gate panel (System Invasion Phase 1, Docs/Design/
    /// SystemInvasion_Phase1_Design.md §4.1). Presents four choices once Phase A space combat ends
    /// with a system's orbital defenses cleared:
    ///   - Withdraw       → ends the siege, frees the fleet (same as Break Off Siege)
    ///   - Target Troops  → commits to Phase B attrition, fleet fires on ground troops directly
    ///   - Target Power   → fleet fires on power plants first; once destroyed, ground troops fight at
    ///                      reduced strength (StarSysManager.GetDefenderFirepowerMultiplier) before the
    ///                      same troop ground phase Target Troops uses
    ///   - Total Destruction → instantly destroys all facilities, zeroes population/ground forces,
    ///                         claims the system for the attacker (see StarSysManager.ResolveTotalDestruction)
    ///
    /// Called from two paths:
    ///   1. Fleet UI "Assault System"/"View Assault" button (FleetMenuUIController.
    ///      ClickSystemAssaultButton) — same button throughout the siege, relabeled once a mode is
    ///      chosen. Before that it opens this panel as the §4.1 decision gate; after, it reopens the
    ///      same panel as a live progress view (decision buttons hidden, Close button shown instead —
    ///      see OpenPanel's modeChosen gating). The panel fully covers the Fleet UI while open, so
    ///      the Fleet UI button itself isn't reachable to close it again — that's what Close is for;
    ///      Withdraw/Break Off Siege stays a separate, consequential action.
    ///   2. TurnEventQueue.SiegeDecision drain — fires every InterTurn for each besieged system
    ///      where AssaultMode is still None, guaranteeing the prompt even if the player ignores
    ///      the Fleet UI button (replaces ShowSiegeDecisionStub from Invasion.2).
    ///
    /// Wire-up (Unity Editor):
    ///   - Add this component to a persistent GameObject (PersistentScene recommended).
    ///   - Set PanelRoot to the root GameObject of the decision panel UI (starts disabled/inactive).
    ///   - Set SysNameLabel, OwnerLabel, WithdrawButton, TargetTroopsButton, TargetPowerButton,
    ///     TotalDestructionButton, and CloseButton to their respective UI elements inside PanelRoot.
    ///     CloseButton is now shown at both the entry gate and the reopened progress view — at the
    ///     gate it implies Withdraw (see OpenPanel/OnWithdraw), so it needs no separate "gate-only"
    ///     button.
    ///   - Set CountdownText (a TMP_Text) — shown only at the entry gate, counting down
    ///     GateTimeoutSeconds (20s, design doc §4.1); defaults to Withdraw at 0.
    ///   - PowerOutputText shows the number of power plants still standing (not power output) —
    ///     ticks down as Target Power/Total Destruction destroy them.
    ///   - Progress sprites (shield/power/facilities/population/troop/landed-troop/owner-insignia
    ///     Images) are optional — leave unassigned until the step sprites exist; PhaseBProgressUI
    ///     no-ops on a null Image. Facilities and Population both track the same
    ///     PhaseBInfrastructureHP pool and should gray in lockstep.
    /// </summary>
    public class SiegeDecisionUIController : MonoBehaviour
    {
        public static SiegeDecisionUIController Instance;

        [Header("Panel root — disabled until OpenPanel() activates it")]
        public GameObject PanelRoot;

        [Header("Labels")]
        [SerializeField] private TMP_Text sysNameLabel;
        [SerializeField] private TMP_Text ownerLabel;

        [Header("Entry-gate countdown — only runs while AssaultMode is still None")]
        [SerializeField] private TMP_Text countdownText;
        // Mirrors the only other countdown-timer pattern in the project (CombatUIManager's
        // remainingTime/isTimerRunning/Update()). Design doc §4.1's already-settled 20s value.
        private const float GateTimeoutSeconds = 20f;
        private float remainingTime;
        private bool isTimerRunning;
        // countdownText is repurposed as the outcome banner (ShowOutcome) once a mode is chosen and
        // the assault resolves - captured here so that repurposing can be undone the next time this
        // same panel instance opens a fresh entry gate for a different siege.
        private Color _countdownDefaultColor = Color.white;

        [Header("Live stats — updated every Phase B tick via RefreshStats()")]
        [SerializeField] private TMP_Text shieldText;         // ShieldText
        [SerializeField] private TMP_Text sysTroopsText;      // SysTroopsText
        [SerializeField] private TMP_Text transportTroopsText; // TransportTroopsText
        [SerializeField] private TMP_Text landedTroopText;    // LandedTroopText
        // Shows the count of power plants still standing in the system (not power output) —
        // ticks down as Target Power/Total Destruction destroy them.
        [SerializeField] private TMP_Text powerOutputText;
        // Attacker's own fleet condition - added so a Phase B counter-fire death spiral (defending
        // ground troops firing at the besieging fleet's combat ships every tick - see StarSysManager.
        // ResolvePhaseBAtritionTick) is visible in real time instead of only showing up as a
        // "Repelled" report after the fact. Ships alive/total and aggregate shield+hull HP, counting
        // every ship in the fleet including transports (transports don't currently take Phase B
        // damage themselves, but are included here so the count/total stays honest either way).
        [SerializeField] private TMP_Text fleetStatusText;

        [Header("Progress sprites — normal color on open, gray toward Depleted as each pool drains")]
        [SerializeField] private Image shieldProgressImage;
        [SerializeField] private Image powerProgressImage;
        // Split 2026-09-14: Factories/Research Centers (facilitiesProgressImage) and Population
        // (populationProgressImage) now get their own icon each, both fed by the same
        // PhaseBInfrastructureHP/MaxHP pool so they gray together — Total Destruction only.
        [FormerlySerializedAs("infrastructureProgressImage")]
        [SerializeField] private Image facilitiesProgressImage;
        [SerializeField] private Image populationProgressImage;
        [SerializeField] private Image troopProgressImage;
        [SerializeField] private Image landedTroopProgressImage;
        [SerializeField] private Image ownerInsigniaImage;

        [Header("Buttons")]
        [SerializeField] private Button withdrawButton;
        [SerializeField] private Button targetTroopsButton;
        [SerializeField] private Button targetPowerButton;
        [SerializeField] private Button totalDestructionButton;
        // View-mode only (AssaultMode already chosen) — dismisses the panel with no side effect on
        // the siege, distinct from Withdraw/Break Off Siege which actually ends it. Not shown at the
        // initial §4.1 gate, which must be answered by picking one of the other buttons.
        [SerializeField] private Button closeButton;

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
            if (countdownText != null)
                _countdownDefaultColor = countdownText.color;

            if (withdrawButton != null)
                withdrawButton.onClick.AddListener(OnWithdraw);
            if (targetTroopsButton != null)
                targetTroopsButton.onClick.AddListener(OnTargetTroops);
            if (targetPowerButton != null)
                targetPowerButton.onClick.AddListener(OnTargetPower);
            if (totalDestructionButton != null)
                totalDestructionButton.onClick.AddListener(OnTotalDestruction);
            // closeButton's listener is wired dynamically in OpenPanel — it means "dismiss" in the
            // reopened progress view but "imply Withdraw" at the entry gate (see OpenPanel).
        }

        private void Update()
        {
            if (!isTimerRunning) return;

            remainingTime -= Time.unscaledDeltaTime;
            if (remainingTime > 0f)
            {
                if (countdownText != null)
                    countdownText.text = Mathf.CeilToInt(remainingTime).ToString();
                return;
            }

            isTimerRunning = false;
            if (countdownText != null)
                countdownText.text = "0";
            // Timed out with no choice made — default to Withdraw (design doc §4.1) rather than
            // leaving the Assault paused with no resolution.
            OnWithdraw();
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

            // Once a mode is chosen the assault is locked in for this siege — reopening this panel
            // (via FleetMenuUIController's "View Assault") is a live progress view, not a second
            // decision. Hide the three mode-choice buttons in that case; Withdraw stays available as
            // the mid-assault escape hatch (relabeled to match FleetMenuUIController's Break Off Siege).
            bool modeChosen = data != null && data.AssaultMode != AssaultMode.None;

            if (targetTroopsButton != null)
            {
                targetTroopsButton.gameObject.SetActive(!modeChosen);
                if (!modeChosen)
                {
                    // Target Troops needs at least one transport with ground forces loaded to be
                    // meaningful. Don't hide the button (player may still want to bombard with no
                    // troops), but gray it out and adjust the label so the intent is clear.
                    bool hasLoadedTroops = fleet != null && fleet.FleetData != null
                        && fleet.FleetData.ShipsList != null
                        && fleet.FleetData.ShipsList.Exists(s => s != null && s.ShipData != null
                            && s.ShipData.ShipType == ShipType.Transport
                            && s.ShipData.LoadedGroundForces > 0);
                    targetTroopsButton.interactable = true;
                    var lbl = targetTroopsButton.GetComponentInChildren<TMP_Text>();
                    if (lbl != null)
                        lbl.text = hasLoadedTroops ? "Target Troops" : "Target Troops\n(no troops loaded)";
                }
            }
            if (targetPowerButton != null)
                targetPowerButton.gameObject.SetActive(!modeChosen);
            if (totalDestructionButton != null)
                totalDestructionButton.gameObject.SetActive(!modeChosen);
            if (withdrawButton != null)
            {
                // Undo ShowOutcome's hide from a previous siege on this same panel instance — this
                // is a fresh OpenPanel for a (possibly different) siege that hasn't resolved yet.
                withdrawButton.gameObject.SetActive(true);
                var lbl = withdrawButton.GetComponentInChildren<TMP_Text>();
                if (lbl != null)
                    lbl.text = modeChosen ? "Break Off Siege" : "Withdraw";
            }
            // closeButton is shown in both states now, but means something different in each:
            // at the entry gate (no mode chosen yet) it's a dismiss with no free pass — clicking it
            // before any other button implies Withdraw, same as letting the countdown expire, so
            // the Assault can never be left paused with no resolution. In the reopened progress
            // view (mode already locked in) it's a plain dismiss with no side effect.
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
                closeButton.onClick.RemoveAllListeners();
                if (modeChosen)
                    closeButton.onClick.AddListener(ClosePanel);
                else
                    closeButton.onClick.AddListener(OnWithdraw);
            }

            // Entry-gate countdown (design doc §4.1) — only runs while a decision is still pending.
            // Stopped/hidden in the reopened progress view; OnWithdraw/OnTargetTroops/etc. also
            // stop it early via StopGateTimer.
            if (!modeChosen)
            {
                remainingTime = GateTimeoutSeconds;
                isTimerRunning = true;
                if (countdownText != null)
                {
                    countdownText.gameObject.SetActive(true);
                    countdownText.color = _countdownDefaultColor;
                    countdownText.text = Mathf.CeilToInt(remainingTime).ToString();
                }
            }
            else
            {
                // Progress view (a mode is already locked in) - hides countdownText, same as before.
                // OnTargetTroops/OnTargetPower/OnTotalDestruction also call OpenPanel to switch
                // straight from the entry gate into this view, so this also clears any outcome
                // banner ShowOutcome left on a stale prior reopen of this same panel instance.
                StopGateTimer();
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

        /// <summary>
        /// Called once by StarSysManager.PhaseBRealtimeResolutionCoroutine when a tick returns a
        /// terminal PhaseBOutcome (repel/capture/stalemate/safety-cap). No-ops unless this exact
        /// siege is the one currently showing in the panel — a system the player isn't watching is
        /// left alone; the ReportEntryUI report ResolvePhaseBAtritionTick/ResolveTotalDestruction
        /// already pushed the same tick is what reaches a player who looked away. Repurposes
        /// countdownText as a short outcome banner and leaves the panel open (the player dismisses
        /// it via the existing Close button) so the final gray/depleted sprite state from this same
        /// tick's RefreshStats is actually visible instead of vanishing with the panel.
        /// </summary>
        public void ShowOutcome(StarSysController sysCon, FleetController fleet, PhaseBOutcome outcome)
        {
            if (PanelRoot == null || !PanelRoot.activeSelf || currentSys != sysCon || outcome == PhaseBOutcome.None)
                return;

            PopulateStats(sysCon.StarSysData, fleet);

            // The siege is already over by the time any outcome fires here (EndSiege has already
            // run - see ResolvePhaseBAtritionTick's repel branches and ResolveTotalDestruction) —
            // there's nothing left to break off, so hide Withdraw/Break Off Siege. Close remains the
            // only way to dismiss the panel from here.
            if (withdrawButton != null)
                withdrawButton.gameObject.SetActive(false);

            if (countdownText == null) return;
            var (label, color) = DescribeOutcome(outcome);
            countdownText.gameObject.SetActive(true);
            countdownText.text = label;
            countdownText.color = color;
        }

        private static (string label, Color color) DescribeOutcome(PhaseBOutcome outcome) => outcome switch
        {
            PhaseBOutcome.Repelled => ("REPELLED", new Color(0.85f, 0.25f, 0.25f)),
            PhaseBOutcome.Captured => ("CAPTURED", new Color(0.25f, 0.85f, 0.35f)),
            PhaseBOutcome.MutualElimination => ("STALEMATE", new Color(0.9f, 0.8f, 0.2f)),
            PhaseBOutcome.DefendersEliminatedNoTroops => ("NO CLAIM", new Color(0.9f, 0.8f, 0.2f)),
            PhaseBOutcome.SafetyCapped => ("STALLED", Color.gray),
            _ => ("", Color.white)
        };

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

            // Power plants remaining — plain count, not output; updates as Target Power/Total
            // Destruction destroy them (data.PowerPlants shrinks live in StarSysManager).
            if (powerOutputText != null)
                powerOutputText.text = $"Power Plants: {data.PowerPlants?.Count ?? 0}";

            // Attacker's own fleet condition + per-ship Fleet-menu hull bars - see fleetStatusText's
            // field comment for why. Ships alive vs. total, plus aggregate shield+hull HP across every
            // living ship (transports included).
            var ships = fleet?.FleetData?.ShipsList;
            if (fleetStatusText != null)
            {
                if (ships == null || ships.Count == 0)
                {
                    fleetStatusText.text = "Fleet: —";
                }
                else
                {
                    int total = ships.Count;
                    int alive = 0;
                    float curHP = 0f, maxHP = 0f;
                    foreach (var s in ships)
                    {
                        if (s?.ShipData == null || s.ShipData.Distroyed) continue;
                        alive++;
                        curHP += s.ShipData.ShieldHealth + s.ShipData.HullHealth;
                        maxHP += s.ShipData.ShieldMaxHealth + s.ShipData.HullMaxHealth;
                    }
                    fleetStatusText.text = $"Fleet: {alive}/{total} ships ({curHP:F0} / {maxHP:F0} HP)";
                }
            }
            if (ships != null)
                foreach (var s in ships)
                    RefreshShipHullBar(s);

            UpdateProgressSprites(data);
        }

        /// <summary>
        /// Pushes ship.ShipData's current HullHealth onto that ship's Fleet-menu list-item hull bar,
        /// on demand rather than waiting for ShipListUI_Item/ShipListingUI's own OnEnable (which only
        /// re-reads ShipData when the item becomes visible, so it goes stale while Phase B damages
        /// ships in the background with the Fleet menu already open). Works for any ShipType,
        /// transports included - ShipListingUI is shared by both the combat-ship and transport list
        /// prefabs (see that class's own doc comment).
        /// </summary>
        private static void RefreshShipHullBar(ShipController shipCon)
        {
            GameObject go = shipCon?.ShipListUIGameObject;
            if (go == null) return;

            var listingUI = go.GetComponent<ShipListingUI>();
            if (listingUI != null)
            {
                listingUI.RefreshHealth(shipCon.ShipData);
                return;
            }

            // Fallback for a list item with no ShipListingUI component - same child-name lookup
            // ShipListUI_Item.OnEnable already uses for that same case.
            var hullBarImg = (go.transform.Find("HullBar/HullBarFill") ?? go.transform.Find("HullBarFill"))
                ?.GetComponent<Image>();
            ShipListingUI.ApplyHealthBar(hullBarImg, shipCon.ShipData);
        }

        /// <summary>
        /// Grays each step's sprite toward Depleted as its pool empties (PhaseBProgressUI). Power
        /// grays under Target Power (gradual sub-stage) and Total Destruction (instant, once shields
        /// fall); Infrastructure only ever grays under Total Destruction (also instant) — neither mode
        /// touches either pool under Target Troops, so both stay normal there. The owner insignia
        /// swaps to the current owner's civ the moment ownership actually flips (CurrentOwnerCivEnum).
        /// See SystemInvasion_Phase1_Design.md §14/§15 for the full per-mode step breakdown.
        /// </summary>
        private void UpdateProgressSprites(StarSysData data)
        {
            PhaseBProgressUI.SetProgress(shieldProgressImage, data.PhaseBShieldHP, data.PhaseBShieldMaxHP);
            PhaseBProgressUI.SetProgress(troopProgressImage, data.PhaseBTroopHP, data.PhaseBTroopMaxHP);

            bool powerTargeted = data.AssaultMode == AssaultMode.TargetPower || data.AssaultMode == AssaultMode.TotalDestruction;
            if (powerTargeted)
                PhaseBProgressUI.SetProgress(powerProgressImage, data.PhaseBPowerPlantHP, data.PhaseBPowerPlantMaxHP);
            else
                PhaseBProgressUI.SetNormal(powerProgressImage);

            if (data.AssaultMode == AssaultMode.TotalDestruction)
            {
                PhaseBProgressUI.SetProgress(facilitiesProgressImage, data.PhaseBInfrastructureHP, data.PhaseBInfrastructureMaxHP);
                PhaseBProgressUI.SetProgress(populationProgressImage, data.PhaseBInfrastructureHP, data.PhaseBInfrastructureMaxHP);
            }
            else
            {
                PhaseBProgressUI.SetNormal(facilitiesProgressImage);
                PhaseBProgressUI.SetNormal(populationProgressImage);
            }

            if (data.PhaseBTroopsLanded)
                PhaseBProgressUI.SetProgress(landedTroopProgressImage, data.PhaseBAttackerTroopHP, data.PhaseBAttackerTroopMaxHP);
            else
                PhaseBProgressUI.SetNormal(landedTroopProgressImage);

            if (ownerInsigniaImage != null)
            {
                var sprite = data.CurrentCivController?.CivData?.InsigniaSprite;
                if (sprite != null) ownerInsigniaImage.sprite = sprite;
            }
        }

        /// <summary>Stops the entry-gate countdown — called whenever the panel closes for any reason.</summary>
        private void StopGateTimer()
        {
            isTimerRunning = false;
            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
        }

        private void ClosePanel()
        {
            StopGateTimer();
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
            StarSysManager.Instance?.BeginPhaseBRealtimeResolution(currentSys, currentFleet);

            // Relays this same decision to every other connected peer - see
            // TimeManager.ServerAssaultDecision's comment.
            PlayerManager.Instance?.LocalPlayerController?.SubmitAssaultDecision(
                currentFleet, currentSys.StarSysData.GetStarSysInt(), AssaultMode.TargetTroops);

            int stardate = TimeManager.Instance != null ? TimeManager.Instance.currentStardate : 0;
            var data = currentSys.StarSysData;
            GalaxyQuadrant quadrant = ReportEntry.QuadrantFromPosition(data.GetPosition());
            ReportEntryUI.PushReport(new ReportEntry(
                ReportCategory.Combat, stardate,
                $"Assault underway: {data.SysName}",
                $"{data.BesiegingCivEnum} has begun a ground assault on {data.SysName}.",
                data.SysName, quadrant, ReportSeverity.Warning));

            Debug.Log($"[Siege] '{data.SysName}': AssaultMode set to TargetTroops.");
            // Switch this same open panel straight into the live progress view instead of closing -
            // OpenPanel re-reads AssaultMode (now non-None), so it hides the decision buttons, stops
            // the gate timer, and repopulates stats/sprites in place. ShowOutcome takes over from
            // here once the real-time coroutine above reaches a terminal result.
            OpenPanel(currentSys, currentFleet);
        }

        private void OnTargetPower()
        {
            if (currentSys?.StarSysData == null) { ClosePanel(); return; }

            currentSys.StarSysData.AssaultMode = AssaultMode.TargetPower;
            StarSysManager.Instance?.InitializePhaseB(currentSys, currentFleet);
            StarSysManager.Instance?.BeginPhaseBRealtimeResolution(currentSys, currentFleet);

            // Relays this same decision to every other connected peer - see
            // TimeManager.ServerAssaultDecision's comment.
            PlayerManager.Instance?.LocalPlayerController?.SubmitAssaultDecision(
                currentFleet, currentSys.StarSysData.GetStarSysInt(), AssaultMode.TargetPower);

            int stardate = TimeManager.Instance != null ? TimeManager.Instance.currentStardate : 0;
            var data = currentSys.StarSysData;
            GalaxyQuadrant quadrant = ReportEntry.QuadrantFromPosition(data.GetPosition());
            ReportEntryUI.PushReport(new ReportEntry(
                ReportCategory.Combat, stardate,
                $"Assault underway: {data.SysName}",
                $"{data.BesiegingCivEnum} has opened a power-plant bombardment of {data.SysName}. " +
                $"Ground troops will fight at reduced strength once the power grid falls.",
                data.SysName, quadrant, ReportSeverity.Warning));

            Debug.Log($"[Siege] '{data.SysName}': AssaultMode set to TargetPower.");
            // See OnTargetTroops' comment above - same in-place switch to the progress view.
            OpenPanel(currentSys, currentFleet);
        }

        private void OnTotalDestruction()
        {
            if (currentSys == null || currentFleet == null) { ClosePanel(); return; }

            // Shields must be bombarded down before Total Destruction resolves — InitializePhaseB
            // starts the shield attrition; ResolveTotalDestruction fires automatically once they fall.
            currentSys.StarSysData.AssaultMode = AssaultMode.TotalDestruction;
            StarSysManager.Instance?.InitializePhaseB(currentSys, currentFleet);
            StarSysManager.Instance?.BeginPhaseBRealtimeResolution(currentSys, currentFleet);

            // Relays this same decision to every other connected peer - see
            // TimeManager.ServerAssaultDecision's comment.
            PlayerManager.Instance?.LocalPlayerController?.SubmitAssaultDecision(
                currentFleet, currentSys.StarSysData.GetStarSysInt(), AssaultMode.TotalDestruction);

            // See OnTargetTroops' comment above - same in-place switch to the progress view. Total
            // Destruction usually resolves within one or two ticks once shields fall, so this view is
            // brief, but ShowOutcome still needs a moment to show the Captured banner before the
            // player dismisses it.
            OpenPanel(currentSys, currentFleet);
        }
    }
}
