using BOTF3D.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



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
        }

        /// <summary>
        /// Start the order selection phase after warp-in completes
        /// </summary>
        public void BeginOrderSelection()
        {
            CurrentTurn++;
            CurrentPhase = CombatPhase.OrderSelection;

            Debug.Log($"📋 Turn {CurrentTurn}: Order Selection Phase");

            // For Turn 1, orders were already selected in the UI before warp-in
            if (CurrentTurn == 1)
            {
                // Use the orders that were set in CombatController.CombatData
                SideOneSelectedOrder = combatData.SideOneOrder;
                SideTwoSelectedOrder = combatData.SideTwoOrder;
                SideOneOrderLocked = true;
                SideTwoOrderLocked = true;

                Debug.Log($"✅ Turn 1 using pre-selected orders: Side1={SideOneSelectedOrder}, Side2={SideTwoSelectedOrder}");

                // Start resolution immediately
                StartCoroutine(ResolveTurn());
            }
            else
            {
                // Mid-combat re-order disabled during development: hold original orders.
                SideOneSelectedOrder = combatData.SideOneOrder;
                SideTwoSelectedOrder = combatData.SideTwoOrder;
                SideOneOrderLocked = true;
                SideTwoOrderLocked = true;
                StartCoroutine(ResolveTurn());
            }
        }

        /// <summary>
        /// Show the combat menu UI for player to select next order
        /// </summary>
        private void ShowOrderSelectionUI()
        {
            if (BOTF3D.UI.CombatUIManager.Instance != null)
            {
                // Re-open the combat menu for next turn
                BOTF3D.UI.CombatUIManager.Instance.ShowOrderSelectionForNextTurn();
            }
        }

        /// <summary>
        /// Player selects an order for their side
        /// </summary>
        public void OnPlayerSelectOrder(CombatOrders order, int side)
        {
            if (CurrentPhase != CombatPhase.OrderSelection)
            {
                Debug.LogWarning("Cannot select order outside OrderSelection phase");
                return;
            }

            if (side == 1)
            {
                SideOneSelectedOrder = order;
                SideOneOrderLocked = true;
                Debug.Log($"✅ Side 1 locked in: {order}");
            }
            else
            {
                SideTwoSelectedOrder = order;
                SideTwoOrderLocked = true;
                Debug.Log($"✅ Side 2 locked in: {order}");
            }

            // If both sides ready, resolve turn
            if (SideOneOrderLocked && SideTwoOrderLocked)
            {
                StartCoroutine(ResolveTurn());
            }
        }

        /// <summary>
        /// AI selects a random order
        /// </summary>
        private void SelectAIOrder(int side)
        {
            // Check if random orders are disabled in config for Side Two
            if (side == 2 && CombatManager.Instance != null && CombatManager.Instance.gameConfig != null)
            {
                if (CombatManager.Instance.gameConfig.disableRandomSideTwoOrders)
                {
                    Debug.Log($"🤖 AI Side 2: Random orders DISABLED by config. Using current order: {SideTwoSelectedOrder}");
                    SideTwoOrderLocked = true;
                    return;
                }
            }

            CombatOrders aiOrder = PickAIOrder(side);

            if (side == 1)
            {
                SideOneSelectedOrder = aiOrder;
                SideOneOrderLocked = true;
            }
            else
            {
                SideTwoSelectedOrder = aiOrder;
                SideTwoOrderLocked = true;
            }

            Debug.Log($"🤖 AI Side {side} selected: {aiOrder}");
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
            Debug.Log($"⚔️ Turn {CurrentTurn}: Resolving {SideOneSelectedOrder} vs {SideTwoSelectedOrder}");

            // Apply tactical multipliers to ship stats for this turn
            ApplyOrderMultipliers();

            // Apply orders to combat controller (for positioning/animation)
            combatController.SetShipOrders(SideOneSelectedOrder, combatData.CivEnumSideOne, 1);
            combatController.SetShipOrders(SideTwoSelectedOrder, combatData.CivEnumSideTwo, 2);

            // Position Formation ships in formation before combat starts
            PositionFormationShips(SideOneSelectedOrder, combatData.SideOneShipCons, 1);
            PositionFormationShips(SideTwoSelectedOrder, combatData.SideTwoShipCons, 2);

            // Record starting HP for damage calculation
            int side1StartHP = (int)GetTotalHP(1);
            int side2StartHP = (int)GetTotalHP(2);

            // Animate ships moving, fighting - THIS IS THE VISUAL COMBAT!
            // Ships will move based on orders, fire weapons, deal damage
            yield return StartCoroutine(AnimateShipPositioning());

            // Calculate what happened during combat
            LastTurnResult = new TurnResult
            {
                TurnNumber = CurrentTurn,
                SideOneOrder = SideOneSelectedOrder,
                SideTwoOrder = SideTwoSelectedOrder,
                SideOneDamageDealt = side2StartHP - (int)GetTotalHP(2), // Damage = HP lost by enemy
                SideTwoDamageDealt = side1StartHP - (int)GetTotalHP(1),
                SideOneRetreated = SideOneSelectedOrder == CombatOrders.Retreat,
                SideTwoRetreated = SideTwoSelectedOrder == CombatOrders.Retreat
            };

            Debug.Log($"💥 Turn {CurrentTurn} Damage: Side1 dealt {LastTurnResult.SideOneDamageDealt}, Side2 dealt {LastTurnResult.SideTwoDamageDealt}");

            // Record this turn for debugging/replay
            RecordTurnResult(LastTurnResult);

            // Remove multipliers
            RemoveOrderMultipliers();

            // Check for combat end
            if (IsCombatOver())
            {
                CurrentPhase = CombatPhase.Victory;
                yield return StartCoroutine(ShowVictoryScreen());
                yield break;
            }

            // Show turn results
            CurrentPhase = CombatPhase.Results;
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
                // Early exit when one side is eliminated (destroyed OR warped out)
                int s1 = combatData.SideOneShipCons.Count(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy);
                int s2 = combatData.SideTwoShipCons.Count(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy);
                if (s1 == 0 || s2 == 0) break;
                yield return null;
            }

            // Stop movement and weapon fire
            combatController.isMoving = false;
            combatController.StopAllWeaponFire();

            Debug.Log("✅ Ship combat animation complete");
        }

        /// <summary>
        /// Apply tactical multipliers to ship damage stats for this turn
        /// </summary>
        private void ApplyOrderMultipliers()
        {
            // Get tactical multipliers from order matchup
            float sideOneMultiplier = CombatOrderHelper.GetOrderMultiplier(SideOneSelectedOrder, SideTwoSelectedOrder);
            float sideTwoMultiplier = CombatOrderHelper.GetOrderMultiplier(SideTwoSelectedOrder, SideOneSelectedOrder);

            Debug.Log($"⚔️ Applying multipliers: Side1={sideOneMultiplier:F2}x, Side2={sideTwoMultiplier:F2}x");

            // Temporarily boost damage for side with advantage
            foreach (var ship in combatData.SideOneShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    // Store original damage on ShipController GameObject
                    if (!ship.gameObject.TryGetComponent<TempDamageMultiplier>(out var temp))
                    {
                        temp = ship.gameObject.AddComponent<TempDamageMultiplier>();
                        temp.originalBeamDamage = ship.ShipData.BeamDamage;
                        temp.originalTorpedoDamage = ship.ShipData.TorpedoDamage;
                    }

                    // Apply multiplier
                    ship.ShipData.BeamDamage = Mathf.RoundToInt(temp.originalBeamDamage * sideOneMultiplier);
                    ship.ShipData.TorpedoDamage = Mathf.RoundToInt(temp.originalTorpedoDamage * sideOneMultiplier);
                }
            }

            foreach (var ship in combatData.SideTwoShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    // Store original damage on ShipController GameObject
                    if (!ship.gameObject.TryGetComponent<TempDamageMultiplier>(out var temp))
                    {
                        temp = ship.gameObject.AddComponent<TempDamageMultiplier>();
                        temp.originalBeamDamage = ship.ShipData.BeamDamage;
                        temp.originalTorpedoDamage = ship.ShipData.TorpedoDamage;
                    }

                    ship.ShipData.BeamDamage = Mathf.RoundToInt(temp.originalBeamDamage * sideTwoMultiplier);
                    ship.ShipData.TorpedoDamage = Mathf.RoundToInt(temp.originalTorpedoDamage * sideTwoMultiplier);
                }
            }
        }

        /// <summary>
        /// Remove tactical multipliers and restore original damage
        /// </summary>
        private void RemoveOrderMultipliers()
        {
            foreach (var ship in combatData.SideOneShipCons)
            {
                if (ship != null && ship.gameObject.TryGetComponent<TempDamageMultiplier>(out var temp))
                {
                    ship.ShipData.BeamDamage = temp.originalBeamDamage;
                    ship.ShipData.TorpedoDamage = temp.originalTorpedoDamage;
                    Destroy(temp);
                }
            }

            foreach (var ship in combatData.SideTwoShipCons)
            {
                if (ship != null && ship.gameObject.TryGetComponent<TempDamageMultiplier>(out var temp))
                {
                    ship.ShipData.BeamDamage = temp.originalBeamDamage;
                    ship.ShipData.TorpedoDamage = temp.originalTorpedoDamage;
                    Destroy(temp);
                }
            }
        }

        /// <summary>
        /// Helper component to store original damage values
        /// </summary>
        private class TempDamageMultiplier : MonoBehaviour
        {
            public int originalBeamDamage;
            public int originalTorpedoDamage;
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
            int sideOneAlive = combatData.SideOneShipCons.Count(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);
            int sideTwoAlive = combatData.SideTwoShipCons.Count(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);

            bool retreat = LastTurnResult.SideOneRetreated || LastTurnResult.SideTwoRetreated;

            return sideOneAlive == 0 || sideTwoAlive == 0 || retreat;
        }

        /// <summary>
        /// Show victory/defeat screen
        /// </summary>
        private IEnumerator ShowVictoryScreen()
        {
            int sideOneAlive = combatData.SideOneShipCons.Count(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);
            int sideTwoAlive = combatData.SideTwoShipCons.Count(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);

            string winner = sideOneAlive > 0 ? "Side One" : "Side Two";
            Debug.Log($"🏆 {winner} WINS!");

            // ✅ Show combat end panel via manager
            if (BOTF3D.UI.CombatUIManager.Instance != null)
            {
                BOTF3D.UI.CombatUIManager.Instance.ShowCombatOverPanel();
            }

            yield return new WaitForSecondsRealtime(2f);

            // End combat
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
        /// Check if a civilization is AI-controlled
        /// </summary>
        private bool IsAIControlled(CivEnum civEnum)
        {
            // TODO: Check if this civ is AI or human player
            // For now, assume Side Two is AI in single-player
            return civEnum == combatData.CivEnumSideTwo;
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
    }
}
