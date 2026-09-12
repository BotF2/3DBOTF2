using System.Collections;
using System.Collections.Generic;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Galaxy
{
    public enum TurnEventType
    {
        UninhabitedHabitable,
        UninhabitedTerraformable,
        UninhabitedNonHabitable,
        DiplomacyEncounter,
        // System Invasion Phase 1 (Docs/Design/SystemInvasion_Phase1_Design.md §4) - unlike every
        // type above (one-shot, fires once per contact), a live siege re-enqueues its own
        // SiegeDecision event every InterTurn until it resolves - see EnqueueActiveSiegeEvents.
        SiegeDecision,
        // Shown to the human player who OWNS the system under siege — informational status panel.
        SiegeDefenseNotify
    }

    public struct TurnEvent
    {
        public TurnEventType Type;
        public FleetController Fleet;
        public StarSysController System;
        public System.Action ShowAction; // non-null for DiplomacyEncounter and SiegeDecision
    }

    /// <summary>
    /// Collects contact events during TurnProgression and presents them to the local player one at
    /// a time at the start of InterTurn. The Advance Turn button stays grayed until the queue is
    /// empty. Handles uninhabited-system contacts (three subtypes) and diplomacy encounters
    /// (fleet-vs-fleet / fleet-vs-system). Combat bypasses this queue — it has its own
    /// time-pause authority via CombatQueueManager.
    /// </summary>
    public class TurnEventQueue : MonoBehaviour
    {
        public static TurnEventQueue Instance;

        private readonly Queue<TurnEvent> _queue = new Queue<TurnEvent>();
        private bool _isDraining;
        private bool _waitingForDismiss;

        /// <summary>True when there are no queued events and the drain coroutine is not running.</summary>
        public bool IsDrained => _queue.Count == 0 && !_isDraining;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
        }

        /// <summary>Records a contact event for InterTurn presentation.</summary>
        public void Enqueue(TurnEvent evt)
        {
            _queue.Enqueue(evt);
            Debug.Log($"[TurnEventQueue] Enqueued {evt.Type} for '{evt.System?.StarSysData?.SysName}' — queue size now {_queue.Count}");
        }

        /// <summary>Called by popup close methods to unblock the drain coroutine.</summary>
        public void NotifyDismissed()
        {
            _waitingForDismiss = false;
        }

        private void OnTurnPhaseChanged(TurnPhase phase)
        {
            if (phase == TurnPhase.InterTurn)
            {
                EnqueueActiveSiegeEvents();
                StartDraining();
            }
        }

        /// <summary>
        /// Re-queues a SiegeDecision event for every system under siege that the local player is the
        /// besieger of - same local-player gating convention every other Enqueue call site already
        /// uses (see FleetController.OnTriggerEnter's weAreLocalPlayer checks), applied here instead
        /// of inside StarSysManager since SystemsUnderSiege has no notion of "local player" itself.
        /// Called every InterTurn, before StartDraining, so this turn's siege events are included in
        /// the same drain pass as any other contact/encounter event queued this turn. Docs/Design/
        /// SystemInvasion_Phase1_Design.md §4.
        /// </summary>
        private void EnqueueActiveSiegeEvents()
        {
            if (StarSysManager.Instance == null) return;

            // Snapshot copy: nothing today mutates SystemsUnderSiege while this loop runs, but a
            // future EndSiege call from inside ShowSiegeDecisionStub (once Invasion.3/4 wires real
            // resolution here) would otherwise mutate the list mid-iteration.
            foreach (var sysCon in new List<StarSysController>(StarSysManager.Instance.SystemsUnderSiege))
            {
                var data = sysCon != null ? sysCon.StarSysData : null;
                if (data == null || !data.DefensesCleared || data.BesiegingFleet == null) continue;

                bool weAreAttacker = GameController.Instance != null
                    && GameController.Instance.AreWeLocalPlayer(data.BesiegingCivEnum);
                bool weAreDefender = GameController.Instance != null
                    && GameController.Instance.AreWeLocalPlayer(data.CurrentOwnerCivEnum);

                // Attacker: decision panel when no Phase B mode chosen yet.
                // Active Phase B assaults tick automatically via ProcessPhaseBAtritionForAllSystems.
                if (weAreAttacker && data.AssaultMode == AssaultMode.None)
                {
                    StarSysController capturedSys = sysCon;
                    FleetController capturedFleet = data.BesiegingFleet;
                    Enqueue(new TurnEvent
                    {
                        Type = TurnEventType.SiegeDecision,
                        Fleet = capturedFleet,
                        System = capturedSys,
                        ShowAction = () => ShowSiegeDecision(capturedSys, capturedFleet)
                    });
                }

                // Defender: informational status panel every InterTurn while siege is active.
                if (weAreDefender)
                {
                    StarSysController capturedSys = sysCon;
                    Enqueue(new TurnEvent
                    {
                        Type = TurnEventType.SiegeDefenseNotify,
                        System = capturedSys,
                        ShowAction = () => ShowSiegeDefense(capturedSys)
                    });
                }
            }
        }

        /// <summary>
        /// System Invasion Phase 1 §4.1 gate — shows the real SiegeDecisionUIController panel if
        /// no Phase B mode has been chosen yet; reports "assault in progress" and auto-dismisses if
        /// the player already committed to Target Troops this turn (Total Destruction ends the siege
        /// immediately so it never reaches this path again). Replaces ShowSiegeDecisionStub from
        /// Invasion.2. Docs/Design/SystemInvasion_Phase1_Design.md §4/§8.
        /// </summary>
        private void ShowSiegeDecision(StarSysController sysCon, FleetController besiegingFleet)
        {
            if (sysCon?.StarSysData == null) { NotifyDismissed(); return; }
            string sysName = sysCon.StarSysData.SysName;
            int stardate = TimeManager.Instance != null ? TimeManager.Instance.currentStardate : 0;
            GalaxyQuadrant quadrant = ReportEntry.QuadrantFromPosition(sysCon.StarSysData.GetPosition());

            if (sysCon.StarSysData.AssaultMode == AssaultMode.None)
            {
                // No decision yet — open the panel and let it call NotifyDismissed when closed.
                if (BOTF3D.UI.SiegeDecisionUIController.Instance != null)
                {
                    BOTF3D.UI.SiegeDecisionUIController.Instance.OpenPanel(sysCon, besiegingFleet);
                    // NotifyDismissed is called by SiegeDecisionUIController.ClosePanel().
                }
                else
                {
                    Debug.LogWarning($"[Siege] SiegeDecisionUIController not found — add it to PersistentScene.");
                    ReportEntryUI.PushReport(new ReportEntry(ReportCategory.Combat, stardate,
                        $"{sysName} awaiting assault decision",
                        "Assign a SiegeDecisionUIController to PersistentScene to enable the Assault System panel.",
                        sysName, quadrant, ReportSeverity.Warning));
                    NotifyDismissed();
                }
            }
            else
            {
                // Target Troops: assault already underway — report progress stub and unblock turn.
                ReportEntryUI.PushReport(new ReportEntry(ReportCategory.Combat, stardate,
                    $"Assault in progress: {sysName}",
                    $"{sysCon.StarSysData.BesiegingCivEnum} assault on {sysName} continues " +
                    $"(Target Troops — Phase B attrition not yet implemented).",
                    sysName, quadrant, ReportSeverity.Info));
                NotifyDismissed();
            }
        }

        private void ShowSiegeDefense(StarSysController sysCon)
        {
            if (sysCon?.StarSysData == null) { NotifyDismissed(); return; }
            if (BOTF3D.UI.SiegeDefenseUIController.Instance != null)
            {
                BOTF3D.UI.SiegeDefenseUIController.Instance.OpenPanel(sysCon);
                // NotifyDismissed is called by SiegeDefenseUIController.ClosePanel (dismiss button).
            }
            else
            {
                // Fallback: push a report so the player still sees the state.
                string sysName = sysCon.StarSysData.SysName;
                int stardate = TimeManager.Instance != null ? TimeManager.Instance.currentStardate : 0;
                GalaxyQuadrant quadrant = ReportEntry.QuadrantFromPosition(sysCon.StarSysData.GetPosition());
                int troops = sysCon.StarSysData.GroundForces?.Count ?? 0;
                string shieldInfo = sysCon.StarSysData.PhaseBShieldsDown ? "shields down"
                    : $"shields at {sysCon.StarSysData.PhaseBShieldHP:F0} HP";
                ReportEntryUI.PushReport(new ReportEntry(ReportCategory.Combat, stardate,
                    $"Under assault: {sysName}",
                    $"{sysCon.StarSysData.BesiegingCivEnum} forces attacking {sysName} — {shieldInfo}, {troops} defending troop{(troops != 1 ? "s" : "")}.",
                    sysName, quadrant, ReportSeverity.Warning));
                NotifyDismissed();
            }
        }

        private void StartDraining()
        {
            if (_queue.Count == 0)
            {
                GameEvents.TurnEventQueueDrained();
                return;
            }
            if (!_isDraining)
                StartCoroutine(DrainCoroutine());
        }

        private IEnumerator DrainCoroutine()
        {
            _isDraining = true;
            while (_queue.Count > 0)
            {
                var evt = _queue.Dequeue();
                if (!IsValid(evt))
                {
                    Debug.Log($"[TurnEventQueue] Skipping stale {evt.Type} event — fleet left or system claimed.");
                    continue;
                }
                _waitingForDismiss = true;
                ShowEvent(evt);
                yield return new WaitUntil(() => !_waitingForDismiss);
            }
            _isDraining = false;
            Debug.Log("[TurnEventQueue] Queue drained — enabling Advance Turn.");
            GameEvents.TurnEventQueueDrained();
        }

        private bool IsValid(TurnEvent evt)
        {
            int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;
            switch (evt.Type)
            {
                case TurnEventType.UninhabitedHabitable:
                    return evt.Fleet != null
                        && evt.Fleet.FleetData?.ColonizableSystem == evt.System
                        && (int)evt.System.StarSysData.CurrentOwnerCivEnum >= firstUninhabited;
                case TurnEventType.UninhabitedTerraformable:
                    return evt.Fleet != null
                        && evt.Fleet.FleetData?.TerraformableSystem == evt.System
                        && (int)evt.System.StarSysData.CurrentOwnerCivEnum >= firstUninhabited;
                case TurnEventType.UninhabitedNonHabitable:
                    return evt.System != null
                        && (int)evt.System.StarSysData.CurrentOwnerCivEnum >= firstUninhabited;
                case TurnEventType.DiplomacyEncounter:
                    return evt.ShowAction != null;
                case TurnEventType.SiegeDecision:
                    // Self-healing: a siege can end between being queued (start of this InterTurn)
                    // and being drained (fleet destroyed by something else this same turn, or - once
                    // Invasion.3/4 exist - resolved by another means). EndSiege here both cleans up
                    // StarSysManager.SystemsUnderSiege and releases the fleet's freeze if it's still
                    // alive, so a stale event never leaves either dangling.
                    bool stillUnderSiege = evt.Fleet != null && evt.System != null && evt.System.StarSysData != null
                        && evt.System.StarSysData.DefensesCleared && evt.System.StarSysData.BesiegingFleet == evt.Fleet;
                    if (!stillUnderSiege)
                        StarSysManager.Instance?.EndSiege(evt.System);
                    return stillUnderSiege;
                case TurnEventType.SiegeDefenseNotify:
                    return evt.System != null && evt.System.StarSysData != null
                        && evt.System.StarSysData.DefensesCleared;
                default:
                    return false;
            }
        }

        private void ShowEvent(TurnEvent evt)
        {
            switch (evt.Type)
            {
                case TurnEventType.UninhabitedHabitable:
                case TurnEventType.UninhabitedNonHabitable:
                    HabitableSysUIController.Instance?.LoadHabitableSysUI(evt.System, evt.Fleet.FleetData.CivController);
                    GalaxyMenuUIController.Instance.OpenMenu(Menu.AFleetMenu, evt.Fleet.gameObject);
                    break;
                case TurnEventType.UninhabitedTerraformable:
                    TerraformableSysUIController.Instance?.LoadTerraformableSysUI(evt.System, evt.Fleet.FleetData.CivController);
                    GalaxyMenuUIController.Instance.OpenMenu(Menu.AFleetMenu, evt.Fleet.gameObject);
                    break;
                case TurnEventType.DiplomacyEncounter:
                case TurnEventType.SiegeDecision:
                case TurnEventType.SiegeDefenseNotify:
                    evt.ShowAction?.Invoke();
                    break;
            }
        }
    }
}
