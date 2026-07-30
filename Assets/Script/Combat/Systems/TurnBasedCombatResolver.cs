using BOTF3D.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;
using BOTF3D.Combat.Testing;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Handles turn-based combat resolution.
    /// Each turn: both sides choose orders → resolve simultaneously → show results → next turn
    /// </summary>
    public class TurnBasedCombatResolver : MonoBehaviour
    {
        public void Initialize() { }
        public void Cleanup() { }
        [Header("Turn State")]
        public int CurrentTurn = 0;
        public CombatPhase CurrentPhase = CombatPhase.Warping;

        [Header("Order Selection")]
        public CombatOrders SideOneSelectedOrder = CombatOrders.None;
        public CombatOrders SideTwoSelectedOrder = CombatOrders.None;
        public bool SideOneOrderLocked = false;
        public bool SideTwoOrderLocked = false;

        [Header("Combat Data")]
        private CombatController combatController;
        private CombatData combatData;

        [Header("Resolution Settings")]
        public float ResolutionAnimationDuration = 15f; // Combat time limit (seconds)
public float ResultsDisplayDuration = 2f;       // Quick results display

        [Header("Turn Results")]
        public TurnResult LastTurnResult;

        [Header("UI")]
        private BOTF3D.UI.TurnResultsUI resultsUI;

        public void Initialize(CombatController controller)
        {
            combatController = controller;
            combatData = controller.CombatData;
            CurrentTurn = 0;
            CurrentPhase = CombatPhase.Warping;

            // Setup UI (will use debug display if no UI found)
            resultsUI = GetComponent<BOTF3D.UI.TurnResultsUI>();
            if (resultsUI == null)
            {
                resultsUI = gameObject.AddComponent<BOTF3D.UI.TurnResultsUI>();
            }
            resultsUI.Initialize(this);

            Debug.Log("🎮 Turn-Based Combat Resolver initialized");

            // Turn 1 orders are submitted from the pre-warp combat menu (see CombatUIManager /
            // FleetController.CmdSubmitCombatOrder → ServerSubmitOrder above). If either side has
            // no connected human player, nothing would ever submit for it and combat would hang
            // forever waiting on both locks - auto-submit for AI-controlled sides here instead.
            ServerAutoSubmitAIOrder(1);
            ServerAutoSubmitAIOrder(2);
        }

        /// <summary>
        /// Start the order selection phase after warp-in completes
        /// </summary>
        public void BeginOrderSelection()
        {
            CurrentTurn++;
            CurrentPhase = CombatPhase.OrderSelection;
            BroadcastPhaseChanged();

            Debug.Log($"📋 Turn {CurrentTurn}: Order Selection Phase");

            // For Turn 1, orders were already submitted+resolved over the network before warp-in
            // even started (see CombatUIManager.EnterShipCombatPhase / ApplyResolvedOrdersAndResolve's
            // CombatPhase.Warping branch below) - combatData.SideOneOrder/SideTwoOrder are already
            // both real, network-confirmed choices by the time we get here.
            if (CurrentTurn == 1)
            {
                SideOneSelectedOrder = combatData.SideOneOrder;
                SideTwoSelectedOrder = combatData.SideTwoOrder;
                SideOneOrderLocked = true;
                SideTwoOrderLocked = true;

                Debug.Log($"✅ Turn 1 using pre-selected orders: Side1={SideOneSelectedOrder}, Side2={SideTwoSelectedOrder}");

                StartCoroutine(ResolveTurn());
            }
            else
            {
                // Reset locks so fresh submissions can come in for this turn
                SideOneOrderLocked = false;
                SideTwoOrderLocked = false;

                ShowOrderSelectionUI();

                // Same reasoning as Initialize() above - auto-submit for any side with no human
                // player so a stalled AI side can't freeze subsequent turns either.
                ServerAutoSubmitAIOrder(1);
                ServerAutoSubmitAIOrder(2);
            }
        }

        /// <summary>
        /// Show the combat menu UI for player to select next order
        /// </summary>
        private void ShowOrderSelectionUI()
        {
            // Purely cosmetic for a local human player's screen (opens the order menu, restarts the
            // countdown timer). A pure dedicated server has no player watching it, but CombatUIManager
            // still exists there as a scene object (its Awake sets Instance regardless of who's
            // looking) - without this guard, ShowOrderSelectionForNextTurn's own timer would run to
            // completion server-side every turn from turn 2 onward (SetUpLocalPlayer's guard in
            // CombatManager only covers the turn-1 SetupForCombat path, not this one) and
            // EnterShipCombatPhase would then auto-submit a phantom order via the server's stale
            // GameController.GetOurCiv() fallback civ - which ServerSubmitOrder has no guard against
            // re-locking, so it can re-broadcast and kick off a second overlapping ResolveTurn() on
            // every client mid-turn, corrupting combat state.
            if (NetworkServer.active && !NetworkClient.active) return;

            if (BOTF3D.UI.CombatUIManager.Instance != null)
            {
                // Re-open the combat menu for next turn
                BOTF3D.UI.CombatUIManager.Instance.ShowOrderSelectionForNextTurn();
            }
        }

        /// <summary>
        /// Server-only entry point for a submitted order (see FleetController.CmdSubmitCombatOrder).
        /// Replaces the old locally-callable OnPlayerSelectOrder - only the server may decide when
        /// both sides have locked in and a turn can resolve; clients (host included) then hear the
        /// resolved orders back via FleetController.RpcOrdersResolved / ApplyResolvedOrdersAndResolve
        /// below, so both sides always simulate from the same inputs.
        /// </summary>
        public void ServerSubmitOrder(CivEnum civ, CombatOrders order)
        {
            if (!NetworkServer.active) return;

            // Warping: this is a turn-1 submission, made from the pre-warp menu before
            // BeginOrderSelection has ever run. OrderSelection: turn 2+ resubmission.
            if (CurrentPhase != CombatPhase.Warping && CurrentPhase != CombatPhase.OrderSelection)
            {
                Debug.LogWarning($"ServerSubmitOrder: civ {civ} submitted {order} outside an order-selection window (phase {CurrentPhase}) - ignored.");
                return;
            }

            int side = civ == combatData.CivEnumSideOne ? 1 : (civ == combatData.CivEnumSideTwo ? 2 : 0);
            if (side == 0)
            {
                Debug.LogWarning($"ServerSubmitOrder: civ {civ} is not part of this combat ({combatData.CivEnumSideOne} vs {combatData.CivEnumSideTwo}) - ignored.");
                return;
            }

            // Once a side is locked for this turn, ignore further submissions for it - a duplicate
            // submission (e.g. a stray/phantom source) re-locking an already-locked side would
            // re-broadcast ServerBroadcastOrdersResolved below and could kick off a second, overlapping
            // ResolveTurn() coroutine on every client mid-turn.
            if (side == 1 && SideOneOrderLocked)
            {
                Debug.LogWarning($"ServerSubmitOrder: side 1 ({civ}) already locked in this turn - ignoring duplicate {order} submission.");
                return;
            }
            if (side == 2 && SideTwoOrderLocked)
            {
                Debug.LogWarning($"ServerSubmitOrder: side 2 ({civ}) already locked in this turn - ignoring duplicate {order} submission.");
                return;
            }

            if (side == 1)
            {
                SideOneSelectedOrder = order;
                SideOneOrderLocked = true;
            }
            else
            {
                SideTwoSelectedOrder = order;
                SideTwoOrderLocked = true;
            }
            Debug.Log($"✅ Side {side} locked in: {order}");

            FleetController anchor = combatController.GetInvolvedFleetAnchor();
            anchor?.ServerBroadcastOrderLocked(side);

            if (SideOneOrderLocked && SideTwoOrderLocked)
                anchor?.ServerBroadcastOrdersResolved(SideOneSelectedOrder, SideTwoSelectedOrder);
        }

        /// <summary>
        /// Called on every client (including the host) via FleetController.RpcOrdersResolved once
        /// the server has both sides' real orders for the current selection window.
        /// </summary>
        public void ApplyResolvedOrdersAndResolve(CombatOrders sideOneOrder, CombatOrders sideTwoOrder)
        {
            if (CurrentPhase == CombatPhase.Warping)
            {
                // Turn 1: feed BeginOrderSelection's pre-selected-order branch above, then let the
                // (already-waiting) combat menu kick off warp-in now that both sides are confirmed.
                combatData.SideOneOrder = sideOneOrder;
                combatData.SideTwoOrder = sideTwoOrder;
                BOTF3D.UI.CombatUIManager.Instance?.OnTurnOneOrdersResolved();
                return;
            }

            SideOneSelectedOrder = sideOneOrder;
            SideTwoSelectedOrder = sideTwoOrder;
            SideOneOrderLocked = true;
            SideTwoOrderLocked = true;
            StartCoroutine(ResolveTurn());
        }

        private void BroadcastPhaseChanged()
        {
            if (!NetworkServer.active) return;
            combatController.GetInvolvedFleetAnchor()?.ServerBroadcastCombatPhaseChanged(CurrentPhase);
        }

        /// <summary>
        /// Server-only: if the given side has no connected human player (see IsAIControlled),
        /// pick and submit an order for it through the same ServerSubmitOrder path a human's
        /// FleetController.CmdSubmitCombatOrder would use, so the lock/broadcast/resolve
        /// machinery below behaves identically regardless of who (or what) submitted the order.
        /// No-ops if the side already has a locked order or is human-controlled.
        /// </summary>
        private void ServerAutoSubmitAIOrder(int side)
        {
            if (!NetworkServer.active) return;
            if (side == 1 && SideOneOrderLocked) return;
            if (side == 2 && SideTwoOrderLocked) return;

            CivEnum civ = side == 1 ? combatData.CivEnumSideOne : combatData.CivEnumSideTwo;
            if (!IsAIControlled(civ)) return;

            CombatOrders order;
            if (side == 2 && CombatManager.Instance != null && CombatManager.Instance.gameConfig != null
                && CombatManager.Instance.gameConfig.disableRandomSideTwoOrders)
            {
                // Random orders disabled for Side Two: keep whatever order is already selected
                // (e.g. set up by a test harness) instead of rolling a new one.
                order = SideTwoSelectedOrder != CombatOrders.None ? SideTwoSelectedOrder : CombatOrders.Engage;
            }
            else
            {
                order = PickAIOrder(side);
            }

            Debug.Log($"🤖 AI Side {side} ({civ}) auto-submitting: {order}");
            ServerSubmitOrder(civ, order);
        }

        /// <summary>
        /// Pick an AI order based on situation
        /// </summary>
        private CombatOrders PickAIOrder(int side)
        {
            var availableOrders = new List<CombatOrders>
            {
                CombatOrders.Engage,
                CombatOrders.Formation,
                CombatOrders.Rush
            };

            // Only add Retreat if losing badly
            float friendlyHP = GetTotalHP(side);
            float enemyHP = GetTotalHP(side == 1 ? 2 : 1);

            if (friendlyHP < enemyHP * 0.4f)
            {
                availableOrders.Add(CombatOrders.Retreat);
            }

            // Only add AttackTransports if enemy has transports
            bool enemyHasTransports = CombatOrderHelper.HasTransports(combatData, side == 1 ? 2 : 1);
            if (enemyHasTransports)
            {
                availableOrders.Add(CombatOrders.AttackTransports);
            }

            return availableOrders[Random.Range(0, availableOrders.Count)];
        }

        /// <summary>
        /// Resolve a turn: apply orders, let combat play out visually, record results
        /// </summary>
        private IEnumerator ResolveTurn()
        {
            CurrentPhase = CombatPhase.Resolution;
            BroadcastPhaseChanged();
            Debug.Log($"⚔️ Turn {CurrentTurn}: Resolving {SideOneSelectedOrder} vs {SideTwoSelectedOrder}");

            // Apply orders to combat controller (for positioning/animation)
            combatController.SetShipOrders(SideOneSelectedOrder, combatData.CivEnumSideOne, 1);
            combatController.SetShipOrders(SideTwoSelectedOrder, combatData.CivEnumSideTwo, 2);

            // Position Formation ships in formation before combat starts
            PositionFormationShips(SideOneSelectedOrder, combatData.SideOneShipCons, 1);
            PositionFormationShips(SideTwoSelectedOrder, combatData.SideTwoShipCons, 2);

            // Scuttle phase: ships self-destruct BEFORE weapons fire (success based on hull integrity)
            yield return StartCoroutine(ProcessScuttleOrders());

            // Record starting HP for damage calculation
            int side1StartHP = (int)GetTotalHP(1);
            int side2StartHP = (int)GetTotalHP(2);

            // Reset per-turn shot instrumentation - Beam/Torpedo hits log into this during
            // AnimateShipPositioning() below (see CombatShotLog, BeamWeapon.Fire, Torpedo.OnReachedTarget).
            CombatShotLog.BeginTurn();

            // Animate ships moving, fighting - THIS IS THE VISUAL COMBAT!
            // Ships will move based on orders, fire weapons, deal damage
            yield return StartCoroutine(AnimateShipPositioning());

            // Destroyed ships are removed from SideOneShipCons/SideTwoShipCons entirely by
            // ShipController.DestroyShip (see CombatManager.RemoveThisShipController), so they
            // can't be found by diffing the post-combat roster - read kills back from the shot
            // log itself instead (covers both TakeDamage kills and reconciled-from-server kills).
            var shipsDestroyedThisTurn = CombatShotLog.Shots
                .Where(s => s.TargetDestroyed)
                .Select(s => s.TargetName)
                .Distinct()
                .ToList();

            // Calculate what happened during combat
            LastTurnResult = new TurnResult
            {
                TurnNumber = CurrentTurn,
                SideOneOrder = SideOneSelectedOrder,
                SideTwoOrder = SideTwoSelectedOrder,
                SideOneDamageDealt = side2StartHP - (int)GetTotalHP(2), // Damage = HP lost by enemy
                SideTwoDamageDealt = side1StartHP - (int)GetTotalHP(1),
                SideOneRetreated = SideOneSelectedOrder == CombatOrders.Retreat,
                SideTwoRetreated = SideTwoSelectedOrder == CombatOrders.Retreat,
                ShipsDestroyed = shipsDestroyedThisTurn,
                Shots = new List<ShotRecord>(CombatShotLog.Shots)
            };

            Debug.Log($"💥 Turn {CurrentTurn} Damage: Side1({combatData.CivEnumSideOne}) dealt {LastTurnResult.SideOneDamageDealt}, " +
                      $"Side2({combatData.CivEnumSideTwo}) dealt {LastTurnResult.SideTwoDamageDealt} — " +
                      $"HP remaining: Side1={GetTotalHP(1):F0}, Side2={GetTotalHP(2):F0}");

            BOTF3D.Combat.Testing.CombatTestingHelper.PrintTurnSummary(LastTurnResult);

            // Record this turn for debugging/replay
            RecordTurnResult(LastTurnResult);

            // Check for combat end
            if (IsCombatOver())
            {
                CurrentPhase = CombatPhase.Victory;
                BroadcastPhaseChanged();
                yield return StartCoroutine(ShowVictoryScreen());
                yield break;
            }

            // Show turn results
            CurrentPhase = CombatPhase.Results;
            BroadcastPhaseChanged();
            yield return StartCoroutine(ShowTurnResults());

            // Start next turn
            BeginOrderSelection();
        }

        /// <summary>
        /// Animate ships moving and fighting based on their orders
        /// This is where the visual combat happens!
        /// </summary>
        private IEnumerator AnimateShipPositioning()
        {
            Debug.Log($"🎬 Starting turn resolution - ships will move and fire!");

            // Enable ship movement and weapon fire for this turn
            combatController.isMoving = true;

            // Initialize combat systems if not already done
            if (!combatController.groupsInitialized)
            {
                combatController.InitializeShipGroupsForEngage();
            }

            // Assign targets to all ships
            combatController.AssignTargetsToAllShips();

            // Start weapon firing for all ships
            yield return combatController.StartAllShipWeaponFire();

            // Let the combat play out for the resolution duration
            // Ships will move based on orders, fire weapons, etc.
            float elapsed = 0f;
            float duration = ResolutionAnimationDuration;

            Debug.Log($"⚔️ Combat resolution playing for {duration} seconds...");

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                // Early exit when one side is eliminated (destroyed, captured, OR warped out)
                int s1 = combatData.SideOneShipCons.Count(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy);
                int s2 = combatData.SideTwoShipCons.Count(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy);
                if (s1 == 0 || s2 == 0) break;
                yield return null;
            }

            // Capture vs Retreat: ships that didn't complete their warp-out are caught
            CaptureIncompletedRetreaters();

            // Stop movement and weapon fire
            combatController.isMoving = false;
            combatController.StopAllWeaponFire();

            Debug.Log("✅ Ship combat animation complete");
        }

        // Damage calculation methods removed - damage now happens naturally from weapon fire during AnimateShipPositioning()

        /// <summary>
        /// Show turn results UI
        /// </summary>
        private IEnumerator ShowTurnResults()
        {
            Debug.Log($"📊 Turn {CurrentTurn} Results:");
            Debug.Log($"  Side 1 ({SideOneSelectedOrder}): Dealt {LastTurnResult.SideOneDamageDealt} damage");
            Debug.Log($"  Side 2 ({SideTwoSelectedOrder}): Dealt {LastTurnResult.SideTwoDamageDealt} damage");

            // Show UI panel with results
            if (resultsUI != null)
            {
                resultsUI.ShowTurnResults(LastTurnResult);
            }

            yield return new WaitForSecondsRealtime(ResultsDisplayDuration);

            if (resultsUI != null)
            {
                resultsUI.HideResults();
            }
        }

        /// <summary>
        /// Check if combat is over
        /// </summary>
        private bool IsCombatOver()
        {
            int sideOneAlive = combatData.SideOneShipCons.Count(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);
            int sideTwoAlive = combatData.SideTwoShipCons.Count(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);

            bool retreat = LastTurnResult.SideOneRetreated || LastTurnResult.SideTwoRetreated;

            return sideOneAlive == 0 || sideTwoAlive == 0 || retreat;
        }

        /// <summary>
        /// Server-authoritative safety net: called whenever the server reconciles a ship
        /// destroyed/captured report (see FleetController.ServerBroadcastShipDestroyed/Captured),
        /// including reports from non-host clients whose local damage simulation diverged from the
        /// host's and reached a kill the host's own ResolveTurn/IsCombatOver check missed. If
        /// combat isn't already resolving (Resolution/Victory phase - ResolveTurn's own IsCombatOver
        /// check already covers those) and the now-reconciled ship counts show one side eliminated,
        /// force the win path immediately instead of waiting on the host's own turn loop, which may
        /// never reach this conclusion on its own and would otherwise stall every client forever.
        /// </summary>
        [Server]
        public void ServerCheckCombatOverAfterReconciliation()
        {
            if (combatController == null || combatController.CombatEnded) return;
            if (CurrentPhase == CombatPhase.Resolution || CurrentPhase == CombatPhase.Victory) return;

            int sideOneAlive = combatData.SideOneShipCons.Count(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);
            int sideTwoAlive = combatData.SideTwoShipCons.Count(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);
            if (sideOneAlive > 0 && sideTwoAlive > 0) return;

            Debug.Log($"🏆 ServerCheckCombatOverAfterReconciliation: reconciled ship state shows combat is over (side1Alive={sideOneAlive}, side2Alive={sideTwoAlive}) - forcing victory.");
            CurrentPhase = CombatPhase.Victory;
            BroadcastPhaseChanged();
            StartCoroutine(ShowVictoryScreen());
        }

        /// <summary>
        /// Show victory/defeat screen
        /// </summary>
        private IEnumerator ShowVictoryScreen()
        {
            int sideOneAlive = combatData.SideOneShipCons.Count(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);
            int sideTwoAlive = combatData.SideTwoShipCons.Count(s => s != null && !s.ShipData.Distroyed && !s.ShipData.IsCaptured && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);

            string winner = sideOneAlive > 0 ? "Side One" : "Side Two";
            Debug.Log($"🏆 {winner} WINS!");

            // Reward-granting and system-assimilation mutate persistent CivData - only the server
            // may decide/apply these, matching CombatController.EndCombat's fleet-cleanup gating below.
            if (NetworkServer.active)
            {
                ApplyCaptureRewards();

                // Borg have no diplomacy - they assimilate defended/attacked systems by winning combat instead.
                CivEnum winningCivEnum = sideOneAlive > 0 ? combatData.CivEnumSideOne : combatData.CivEnumSideTwo;
                if (winningCivEnum == CivEnum.BORG && combatData.StarSysCon != null)
                {
                    CivManager.Instance.AssimilateSystem(combatData.StarSysCon, CivEnum.BORG);
                }
            }

            // Show combat end panel via manager - cosmetic, so skip entirely on a pure dedicated
            // server (same guard as ShowOrderSelectionUI above): nobody is watching it there, and
            // a headless server has no resolved Localization SelectedLocale, so a localized text
            // lookup reached from this UI path throws and would otherwise kill this coroutine
            // before EndCombat() below ever runs, leaving every client stuck on the results panel.
            if (!(NetworkServer.active && !NetworkClient.active) && BOTF3D.UI.CombatUIManager.Instance != null)
            {
                BOTF3D.UI.CombatUIManager.Instance.ShowCombatOverPanel();
                BOTF3D.UI.CombatUIManager.Instance.SetCombatOutcomeText(combatData);
            }

            yield return new WaitForSecondsRealtime(4f);

            // End combat - only the server decides this; non-host clients tear down their own
            // Combat scene view when FleetController.RpcCombatEnded arrives (see EndCombat/ServerNotifyCombatEnded).
            if (NetworkServer.active)
                combatController.EndCombat();
        }

        /// <summary>
        /// Get total HP for a side
        /// </summary>
        private float GetTotalHP(int side)
        {
            var ships = side == 1 ? combatData.SideOneShipCons : combatData.SideTwoShipCons;
            return ships.Where(s => s != null && !s.ShipData.Distroyed)
                        .Sum(s => s.ShipData.ShieldHealth + s.ShipData.HullHealth);
        }

        /// <summary>
        /// Check if a civilization is AI-controlled: true unless a connected human player has
        /// claimed it (see PlayerManager.Roster, populated by LocalHumanPlayerController.OnStartServer).
        /// AiPlayerController is never actually spawned, so "no roster entry" is the only signal
        /// we have for "nobody is playing this civ" - treat that as AI-controlled.
        /// </summary>
        private bool IsAIControlled(CivEnum civEnum)
        {
            if (PlayerManager.Instance == null) return true;

            var roster = PlayerManager.Instance.Roster;
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i].PlayerCiv == civEnum) return false;
            }
            return true;
        }

        /// <summary>
        /// Position ships in formation grid immediately if Formation order is selected
        /// </summary>
        private void PositionFormationShips(CombatOrders order, List<ShipController> ships, int side)
        {
            if (order != CombatOrders.Formation || ships == null) return;

            const float FORMATION_SPACING = 35f;
            float formationX = side == 1 ? WarpAnimationController.SIDE1_COMBAT_END_X : WarpAnimationController.SIDE2_COMBAT_END_X;

            int slotIndex = 0;
            foreach (var ship in ships)
            {
                if (ship == null || ship.ShipData.Distroyed) continue;

                bool isTransport = ship.ShipData.ShipType == ShipType.Transport;
                if (isTransport)
                {
                    // Transports stay at their warp-in position
                    formationX = side == 1 ? WarpAnimationController.SIDE1_TRANSPORT_END_X : WarpAnimationController.SIDE2_TRANSPORT_END_X;
                }
                else
                {
                    formationX = side == 1 ? WarpAnimationController.SIDE1_COMBAT_END_X : WarpAnimationController.SIDE2_COMBAT_END_X;
                }

                // Calculate grid position (5 columns)
                int col = slotIndex % 5;
                int row = slotIndex / 5;
                Vector3 formationPos = new Vector3(formationX, (row - 2) * FORMATION_SPACING, (col - 2) * FORMATION_SPACING);

                // Move ship to formation position
                ship.transform.position = formationPos;

                // Assign formation slot to the state machine
                var osm = ship.GetComponent<CombatOrderStateMachine>();
                if (osm != null)
                {
                    osm.formationSlot = slotIndex;
                }

                slotIndex++;
            }

            Debug.Log($"📐 Positioned {slotIndex} ships in Formation (Side {side}) with {FORMATION_SPACING} unit spacing");
        }

        /// <summary>
        /// Record turn result to combat recorder for debugging/replay
        /// </summary>
        private void RecordTurnResult(TurnResult result)
        {
            if (combatController == null) return;

            var recorder = combatController.GetComponent<BOTF3D.Combat.Testing.CombatRecorder>();
            if (recorder != null && recorder.IsRecording)
            {
                recorder.RecordTurn(result);
            }
        }

        /// <summary>
        /// Scuttle phase: before weapon fire, Scuttle-order ships attempt self-destruct.
        /// Success chance = current HP / max HP (undamaged = ~100%; damaged = lower chance).
        /// Failed scuttles leave the ship alive and capturable by an enemy Capture order.
        /// </summary>
        private IEnumerator ProcessScuttleOrders()
        {
            bool hasScuttle = SideOneSelectedOrder == CombatOrders.Scuttle || SideTwoSelectedOrder == CombatOrders.Scuttle;
            if (!hasScuttle) yield break;

            // Ships are visible — pause before detonation so the player sees them
            Debug.Log("💣 Scuttle order detected — 1s before detonation");
            float preTimer = 0f;
            while (preTimer < 1f)
            {
                preTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            ProcessScuttleSide(combatData.SideOneShipCons, SideOneSelectedOrder);
            ProcessScuttleSide(combatData.SideTwoShipCons, SideTwoSelectedOrder);

            // Let explosion particles and audio finish before weapon fire starts
            float postTimer = 0f;
            while (postTimer < 2f)
            {
                postTimer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void ProcessScuttleSide(List<ShipController> ships, CombatOrders order)
        {
            if (order != CombatOrders.Scuttle) return;

            // Unlike weapon-fire damage (left as redundant per-client simulation - see
            // ShipController.TakeDamage), the scuttle success roll is a single discrete decision
            // with no natural "both clients converge anyway" behavior, so only the server rolls it;
            // non-host clients apply the outcome via ShipController.ApplyScuttledFromServer (see
            // FleetController.RpcShipScuttled) instead of rolling their own independent chance.
            if (!NetworkServer.active) return;

            // Iterate a snapshot so destruction during iteration is safe
            foreach (var ship in ships.ToList())
            {
                if (ship == null || ship.ShipData.Distroyed || ship.ShipData.IsCaptured) continue;

                int maxHP = ship.ShipData.ShieldMaxHealth + ship.ShipData.HullMaxHealth;

                int currentHP = ship.ShipData.ShieldHealth + ship.ShipData.HullHealth;
                float successChance = maxHP > 0 ? (float)currentHP / maxHP : 0f;

                if (Random.value <= successChance)
                {
                    Debug.Log($"💥 {ship.ShipData.ShipName} scuttled successfully (HP {currentHP}/{maxHP}, chance {successChance:P0})");
                    ship.ShipData.IsScuttled = true;
                    ship.SelfDestruct();
                }
                else
                {
                    Debug.Log($"⚠️ {ship.ShipData.ShipName} scuttle FAILED (HP {currentHP}/{maxHP}, chance {successChance:P0}) — vulnerable to capture");
                }
            }
        }

        /// <summary>
        /// After combat animation ends, retreating ships that haven't completed warp-out
        /// are captured when the enemy side has the Capture order.
        /// </summary>
        private void CaptureIncompletedRetreaters()
        {
            bool side1Capture = SideTwoSelectedOrder == CombatOrders.Capture && SideOneSelectedOrder == CombatOrders.Retreat;
            bool side2Capture = SideOneSelectedOrder == CombatOrders.Capture && SideTwoSelectedOrder == CombatOrders.Retreat;

            if (side1Capture)
                CaptureStillTurningShips(combatData.SideOneShipCons);
            if (side2Capture)
                CaptureStillTurningShips(combatData.SideTwoShipCons);
        }

        private void CaptureStillTurningShips(List<ShipController> retreatShips)
        {
            foreach (var ship in retreatShips)
            {
                if (ship == null || ship.ShipData.Distroyed || ship.ShipData.IsCaptured) continue;
                if (!ship.gameObject.activeInHierarchy) continue; // warped out — escaped

                var osm = ship.GetComponent<CombatOrderStateMachine>();
                if (osm != null && !osm.IsWarpingOut())
                {
                    // Still turning — ran out of time, caught by capture ships
                    ship.ShipData.IsCaptured = true;
                    combatData.CapturedShips.Add(ship);
                    Debug.Log($"🎯 {ship.ShipData.ShipName} captured while attempting retreat!");
                }
            }
        }

        /// <summary>
        /// Grant post-combat rewards for each captured ship to the capturing civilization:
        ///   - Shipyard build time reduction = half of captured ship's BuildDuration
        ///   - Small tech point bonus (5 pts per ship)
        /// Captured ships are destroyed after combat and never returned to the galaxy map.
        /// </summary>
        private void ApplyCaptureRewards()
        {
            if (combatData.CapturedShips == null || combatData.CapturedShips.Count == 0) return;

            const int TECH_BONUS_PER_CAPTURE = 5;

            foreach (var ship in combatData.CapturedShips)
            {
                if (ship?.ShipData == null) continue;

                // Determine which side captured this ship (it was on the OTHER side)
                bool capturedFromSideTwo = combatData.SideTwoShipCons.Contains(ship);
                CivController capturingCiv = capturedFromSideTwo ? combatData.sideOneCiv : combatData.sideTwoCiv;
                CivController capturedCiv  = capturedFromSideTwo ? combatData.sideTwoCiv : combatData.sideOneCiv;

                if (capturingCiv?.CivData == null) continue;

                // Shipyard bonus: reduce next ship build by half of the captured ship's BuildDuration
                int buildBonus = ship.ShipData.BuildDuration / 2;
                if (buildBonus > 0)
                {
                    capturingCiv.CivData.PendingBuildTimeReduction += buildBonus;
                    Debug.Log($"⚙️ {capturingCiv.CivData.CivShortName}: +{buildBonus} shipyard build-time reduction from captured {ship.ShipData.ShipName}");
                }

                // Tech bonus only when the captured ship's civ is at or above the capturing civ's tech level
                if (capturedCiv?.CivData != null && capturedCiv.CivData.CurrentTechLevel >= capturingCiv.CivData.CurrentTechLevel)
                {
                    capturingCiv.CivData.AddTechPoints(TECH_BONUS_PER_CAPTURE);
                    Debug.Log($"🔬 {capturingCiv.CivData.CivShortName}: +{TECH_BONUS_PER_CAPTURE} tech pts from captured {ship.ShipData.ShipName}");
                }
                else
                {
                    Debug.Log($"🔬 {capturingCiv.CivData.CivShortName}: no tech pts from {ship.ShipData.ShipName} (captured civ tech level too low)");
                }

                // Captured ship is destroyed — remove from its fleet so it never returns to the map
                if (ship.ShipData.CurrentFleetController != null)
                    ship.ShipData.CurrentFleetController.RemoveShipFromFleet(ship);
                if (CombatManager.Instance != null)
                    CombatManager.Instance.RemoveThisShipController(ship);
            }
        }
    }

    /// <summary>
    /// Results from one turn of combat
    /// </summary>
    public class TurnResult
    {
        public int TurnNumber;
        public CombatOrders SideOneOrder;
        public CombatOrders SideTwoOrder;
        public int SideOneDamageDealt;
        public int SideTwoDamageDealt;
        public bool SideOneRetreated;
        public bool SideTwoRetreated;
        public List<string> ShipsDestroyed = new List<string>();
        public List<ShotRecord> Shots = new List<ShotRecord>();
    }
}
