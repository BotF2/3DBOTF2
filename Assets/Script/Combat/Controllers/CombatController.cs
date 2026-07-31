using BOTF3D.Audio;
using BOTF3D.Core;

using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Main combat controller - coordinates all combat systems.
    /// Refactored to delegate responsibilities to specialized managers.
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        public void Initialize() { }
        public void Cleanup() { }
        // Static wait timers
        private static WaitForSecondsRealtime _waitForSeconds3 = new WaitForSecondsRealtime(3f);
        private static WaitForSecondsRealtime _waitForSeconds2 = new WaitForSecondsRealtime(2f);

        // Combat data
        private CombatData combatData;
        public CombatData CombatData { get { return combatData; } set { combatData = value; } }
        public int CombatID { get; private set; }

        // Combat state flags
        public bool WarpingIn = false;
        public bool WarpingAnimationOver = false;
        public bool isMoving = false;
        public bool isClosing = false;
        private bool combatEnded = false;
        public bool CombatEnded => combatEnded;
        private bool showingEndPanel = false;
        public bool groupsInitialized => shipGroupManager?.groupsInitialized ?? false;
        // Turn-based combat system
        [Header("Turn-Based Combat")]
        public TurnBasedCombatResolver TurnResolver;
        public bool UseTurnBasedCombat = true;

        // Combat UI
        public Canvas ShipCombatCanvas;

        // Audio clips
        public AudioClip SideOneBeamFireClip;
        public AudioClip SideTwoBeamFireClip;
        public AudioClip SideOneTorpedoFireClip;
        public AudioClip SideTwoTorpedoFireClip;
        public SoundData warpInSound;

        // Weapon prefabs
        public GameObject SideOneTorpedoPrefab;
        public GameObject SideTwoTorpedoPrefab;
        public GameObject SideOneBeamPrefab;
        public GameObject SideTwoBeamPrefab;

        // === REFACTORED: Specialized Managers ===
        private ShipSetupManager shipSetupManager;
        private WarpAnimationController warpAnimationController;
        private CombatTargetingSystem targetingSystem;
        private HealthBarManager healthBarManager;
        private ShipGroupManager shipGroupManager;
        private ShipMovementController shipMovementController;
        private ShipFormationManager formationManager;

        // Fleet refs captured before combat starts, used for reliable end-of-combat cleanup
        private List<FleetController> _involvedFleets = new List<FleetController>();

        // A stable, still-valid FleetController from this combat, used as the Command/Rpc call
        // channel by code (TurnBasedCombatResolver, CombatUIManager) that isn't itself a
        // NetworkBehaviour and needs one to reach the network.
        public FleetController GetInvolvedFleetAnchor() => _involvedFleets.Find(f => f != null);

        // Used by RpcCombatEnded to identify which of possibly several concurrent same-civ-pair
        // combats it belongs to - a fleet can only be a combatant in one active combat at a time,
        // so this is unambiguous where civ-pair matching (the old approach) was not. See
        // FleetController.RpcCombatEnded for why civ-pair matching broke under back-to-back
        // same-civ-pair test battles.
        public bool InvolvesFleet(FleetController fleet) => fleet != null && _involvedFleets.Contains(fleet);

        // === DEBUG & TESTING ===
        private BOTF3D.Combat.Testing.CombatRecorder combatRecorder;
        private BOTF3D.Combat.Debugging.CombatDebugUI combatDebugUI;

        private void Awake()
        {
            CombatID = GetInstanceID();
            GameLogger.Log(GameLogger.LogCategory.Combat, $"✅ CombatController {CombatID}: Created", this);

            // Add turn-based resolver component
            if (UseTurnBasedCombat)
            {
                TurnResolver = gameObject.AddComponent<TurnBasedCombatResolver>();
            }

            // Add debug & testing components
            InitializeDebugTools();

            // Show welcome message with debug tools info
            BOTF3D.Combat.Testing.CombatTestingHelper.ShowWelcomeMessage();
        }

        private void Start()
        {
            CleanupOrphanedProjectiles();
        }

        /// <summary>
        /// Initialize debug and testing tools (recorder, debug UI)
        /// </summary>
        private void InitializeDebugTools()
        {
            // Add combat recorder
            combatRecorder = gameObject.AddComponent<BOTF3D.Combat.Testing.CombatRecorder>();
            combatRecorder.RecordingName = $"combat_{CombatID}";
            combatRecorder.IsRecording = true;
            combatRecorder.AutoSaveOnEnd = true;

            // Add debug UI
            combatDebugUI = gameObject.AddComponent<BOTF3D.Combat.Debugging.CombatDebugUI>();
            combatDebugUI.ShowOnStart = false; // Press F1 to show

            GameLogger.Log(GameLogger.LogCategory.Combat, "🐛 Debug tools initialized (F1 for debug UI)", this);
        }

        void Update()
        {
            // Skip real-time combat logic if using turn-based
            if (UseTurnBasedCombat) return;

            // Update group targets for Engage order
            if (WarpingAnimationOver && !combatEnded)
            {
                if (CombatData.SideOneOrder == CombatOrders.Engage || CombatData.SideTwoOrder == CombatOrders.Engage)
                {
                    shipGroupManager?.UpdateGroupTargets();
                }
            }

            // Check for combat end condition
            if (!combatEnded && WarpingAnimationOver && !WarpingIn)
            {
                int sideOneAlive = CombatData.SideOneShipCons.Count(s => s != null && s.ShipData != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);
                int sideTwoAlive = CombatData.SideTwoShipCons.Count(s => s != null && s.ShipData != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy && s.ShipData.ShipType != ShipType.Transport);

                if (sideOneAlive == 0 || sideTwoAlive == 0)
{
                    GameLogger.Log(GameLogger.LogCategory.Combat, $"🏁 Combat ended! Side 1: {sideOneAlive} ships, Side 2: {sideTwoAlive} ships", this);
                    combatEnded = true;
                    targetingSystem.StopAllWeaponFire();

                    // Destroy all torpedoes still in flight so they can't hit ships during the end sequence
                    foreach (var t in FindObjectsByType<Torpedo>())
                        Destroy(t.gameObject);

                    if (!showingEndPanel)
                    {
                        showingEndPanel = true;
                        StartCoroutine(ShowCombatEndSequence(sideOneAlive > 0));
                    }
                }
            }
        }

        void LateUpdate()
        {
            // In turn-based combat, only move during Resolution phase
            if (UseTurnBasedCombat && TurnResolver != null)
            {
                if (TurnResolver.CurrentPhase != CombatPhase.Resolution)
                {
                    return;
                }
            }

            // Order-based movement system
            if (WarpingAnimationOver && !WarpingIn && isMoving && !combatEnded && shipMovementController != null)
            {
                shipGroupManager?.UpdateEngageGroups();

                // Process each ship individually
                foreach (var ship in CombatData.SideOneShipCons)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        shipMovementController.MoveShipBasedOnOrder(ship);
                    }
                }

                foreach (var ship in CombatData.SideTwoShipCons)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        shipMovementController.MoveShipBasedOnOrder(ship);
                    }
                }
            }
        }

        /// <summary>
        /// Main entry point: Setup ships and start warp-in animation
        /// </summary>
        public void PopulateShipData(CombatController theCombatController)
        {
            if (theCombatController != this) return;

            GameLogger.Log(GameLogger.LogCategory.Combat, "=== Starting Ship Setup ===", this);

            // Capture fleet refs before any ship deaths can occur during combat
            CaptureInvolvedFleets();

            // Initialize all managers
            InitializeManagers();

            // Setup ships using ShipSetupManager
            shipSetupManager.SetupAllShips();

            BuildShipIDLookup();

            // Initialize the resolver now (not after warp-in) so networked turn-1 order
            // submission - which happens from the pre-warp combat menu, before StartWarpInAnimation
            // ever runs - has combatController/combatData available. See TurnBasedCombatResolver.
            if (UseTurnBasedCombat && TurnResolver != null)
            {
                TurnResolver.Initialize(this);
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, "=== Ship Setup Complete ===", this);
        }

        // ShipID -> ShipController, used to apply networked per-ship outcome Rpcs (destroyed/
        // captured/scuttled - see ShipController.ForceDestroyFromServer & friends) by stable ID
        // rather than by local instance reference, since each client instantiates its own ships.
        private readonly Dictionary<int, ShipController> shipsByID = new Dictionary<int, ShipController>();

        private void BuildShipIDLookup()
        {
            shipsByID.Clear();
            foreach (var ship in CombatData.SideOneShipCons.Concat(CombatData.SideTwoShipCons))
            {
                if (ship?.ShipData == null) continue;
                shipsByID[ship.ShipData.ShipID] = ship;
            }
        }

        public ShipController GetShipByID(int shipID) => shipsByID.TryGetValue(shipID, out var ship) ? ship : null;

        private void CaptureInvolvedFleets()
        {
            _involvedFleets.Clear();
            if (CombatData?.SideOneShipCons != null)
            {
                foreach (var ship in CombatData.SideOneShipCons)
                {
                    if (ship?.ShipData?.CurrentFleetController != null &&
                        !_involvedFleets.Contains(ship.ShipData.CurrentFleetController))
                        _involvedFleets.Add(ship.ShipData.CurrentFleetController);
                }
            }
            if (CombatData?.SideTwoShipCons != null)
            {
                foreach (var ship in CombatData.SideTwoShipCons)
                {
                    if (ship?.ShipData?.CurrentFleetController != null &&
                        !_involvedFleets.Contains(ship.ShipData.CurrentFleetController))
                        _involvedFleets.Add(ship.ShipData.CurrentFleetController);
                }
            }
            GameLogger.Log(GameLogger.LogCategory.Combat, $"  Captured {_involvedFleets.Count} fleet refs for end-of-combat cleanup", this);
        }

        /// <summary>
        /// Initialize all specialized managers
        /// </summary>
        private void InitializeManagers()
        {
            formationManager = new ShipFormationManager();

            shipSetupManager = new ShipSetupManager(this);
            shipSetupManager.SideOneTorpedoPrefab = SideOneTorpedoPrefab;
            shipSetupManager.SideTwoTorpedoPrefab = SideTwoTorpedoPrefab;
            shipSetupManager.SideOneBeamPrefab = SideOneBeamPrefab;
            shipSetupManager.SideTwoBeamPrefab = SideTwoBeamPrefab;
            shipSetupManager.SideOneBeamFireClip = SideOneBeamFireClip;
            shipSetupManager.SideTwoBeamFireClip = SideTwoBeamFireClip;
            shipSetupManager.SideOneTorpedoFireClip = SideOneTorpedoFireClip;
            shipSetupManager.SideTwoTorpedoFireClip = SideTwoTorpedoFireClip;

            warpAnimationController = new WarpAnimationController(this, warpInSound);
            targetingSystem = new CombatTargetingSystem(CombatData);
            healthBarManager = new HealthBarManager(CombatData);
            shipGroupManager = new ShipGroupManager(CombatData);
            shipMovementController = new ShipMovementController(CombatData, formationManager);

            // Initialize debug tools with combat controller reference
            if (combatRecorder != null)
            {
                combatRecorder.Initialize(this);
            }
            if (combatDebugUI != null)
            {
                combatDebugUI.Initialize(this);
            }
        }

        /// <summary>
        /// Start the warp-in animation
        /// </summary>
        public IEnumerator StartWarpInAnimation()
        {
            yield return warpAnimationController.StartWarpInAnimation();

            SetupCameraTargets();
            healthBarManager.CreateHealthBarsForAllShips();

            SetupFlankingPositions();

            // Choose combat mode
            if (UseTurnBasedCombat)
            {
                if (TurnResolver != null)
                {
                    // Already Initialize()'d in PopulateShipData, before warp-in - see comment there.
                    GameLogger.Log(GameLogger.LogCategory.Combat, "🎮 Starting Turn-Based Combat", this);
                    TurnResolver.BeginOrderSelection();
                }
                else
                {
                    GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ TurnResolver is null! Cannot start turn-based combat.", this);
                }
            }
            else
            {
                // Original real-time combat
                shipGroupManager.InitializeShipGroupsForEngage();
                targetingSystem.AssignTargetsToAllShips();
                yield return StartAllShipWeaponFire();
                isMoving = true;
                GameLogger.Log(GameLogger.LogCategory.Combat, $"✅ Combat Controller {CombatID}: Order-based movement ENABLED", this);
            }
        }

        /// <summary>
        /// Setup flanking positions for AttackTransports order
        /// </summary>
        private void SetupFlankingPositions()
        {
            SetupSideFlankingPositions(CombatData.SideOneShipCons, CombatData.SideOneOrder, true);
            SetupSideFlankingPositions(CombatData.SideTwoShipCons, CombatData.SideTwoOrder, false);
        }

        private void SetupSideFlankingPositions(List<ShipController> ships, CombatOrders order, bool isSideOne)
        {
            if (order != CombatOrders.AttackTransports && order != CombatOrders.Rush) return;

            float baseYRotation = isSideOne ? 90f : -90f;

            foreach (var ship in ships)
            {
                if (ship == null || ship.ShipData.Distroyed) continue;

                bool isFlankingShip = ship.ShipData.ShipType == ShipType.Scout ||
                                      ship.ShipData.ShipType == ShipType.Destroyer;

                if (isFlankingShip)
                {
                    ship.transform.rotation = Quaternion.Euler(0, baseYRotation, 0);
                    GameLogger.Log(GameLogger.LogCategory.Combat, $"  🛸 {ship.ShipData.ShipName} ({ship.ShipData.ShipType}): Forward rotation {baseYRotation}°", this);
                }
            }
        }

        /// <summary>
        /// Setup camera to track all ships
        /// </summary>
        private void SetupCameraTargets()
        {
            if (ShipCombatCameraController.Instance == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "❌ ShipCombatCameraController.Instance is null!", this);
                return;
            }

            List<GameObject> allShips = new List<GameObject>();

            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship != null && ship.gameObject != null)
                {
                    allShips.Add(ship.gameObject);
                }
            }

            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship != null && ship.gameObject != null)
                {
                    allShips.Add(ship.gameObject);
                }
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, $"📷 Setting camera to track {allShips.Count} ships", this);

            ShipCombatCameraController.Instance.SetTargets(allShips.ToArray());
            ShipCombatCameraController.Instance.SetWarpingIn(false);
            ShipCombatCameraController.Instance.SetWarpingInOver(true);

            GameLogger.Log(GameLogger.LogCategory.Combat, "✅ Camera targets configured", this);
        }

        /// <summary>
        /// Begin order-based movement after warp completes
        /// </summary>
        public void BeginOrderBasedMovement()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "📊 Beginning order-based movement...", this);
            isMoving = true;
        }

        /// <summary>
        /// Assign targets to all ships
        /// </summary>
        public void AssignTargetsToAllShips()
        {
            targetingSystem.AssignTargetsToAllShips();
        }

        /// <summary>
        /// Reassign target when current target is destroyed
        /// </summary>
        public void ReassignTarget(ShipController ship)
        {
            targetingSystem.ReassignTarget(ship);
        }

        /// <summary>
        /// Start weapon firing for all ships
        /// </summary>
        public IEnumerator StartAllShipWeaponFire()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "🔫 Starting weapon fire for all ships with balanced timing...", this);

            yield return new WaitForSecondsRealtime(0.5f);

            int maxShips = Mathf.Max(
                CombatData.SideOneShipCons.Count,
                CombatData.SideTwoShipCons.Count
            );

            List<float> side1Delays = new List<float>();
            List<float> side2Delays = new List<float>();

            for (int i = 0; i < maxShips; i++)
            {
                float delay = Random.Range(0.1f, 0.5f);
                side1Delays.Add(delay);
                side2Delays.Add(delay);
            }

            side1Delays = side1Delays.OrderBy(x => Random.value).ToList();
            side2Delays = side2Delays.OrderBy(x => Random.value).ToList();

            // Start firing for Side One
            int index1 = 0;
            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    float delay = index1 < side1Delays.Count ? side1Delays[index1] : 0f;
                    StartCoroutine(ShipFireLoopProxy(ship, delay));
                    GameLogger.Log(GameLogger.LogCategory.Combat, $"  Side 1: {ship.ShipData.ShipName} starting in {delay:F2}s", this);
                    index1++;
                }
            }

            // Start firing for Side Two
            int index2 = 0;
            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    float delay = index2 < side2Delays.Count ? side2Delays[index2] : 0f;
                    StartCoroutine(ShipFireLoopProxy(ship, delay));
                    GameLogger.Log(GameLogger.LogCategory.Combat, $"  Side 2: {ship.ShipData.ShipName} starting in {delay:F2}s", this);
                    index2++;
                }
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, $"✅ Weapon fire started for {index1 + index2} ships", this);
            yield return null;
        }

        /// <summary>
        /// Proxy coroutine for ship firing
        /// </summary>
        private IEnumerator ShipFireLoopProxy(ShipController ship, float initialDelay)
        {
            if (ship == null) yield break;

            if (initialDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(initialDelay);
            }

            yield return StartCoroutine(ship.ShipFireLoop(0f));
        }

        /// <summary>
        /// Initialize ship groups for Engage order
        /// </summary>
        public void InitializeShipGroupsForEngage()
        {
            shipGroupManager.InitializeShipGroupsForEngage();
        }

        /// <summary>
        /// Set ship orders for a side
        /// </summary>
        public void SetShipOrders(CombatOrders order, CivEnum civEnum, int side = 0)
        {
            List<ShipController> sideShips = null;

            // If side is specified (1 or 2), use it. Otherwise fallback to CivEnum (ambiguous in mirror matches)
            if (side == 1 || (side == 0 && civEnum == CombatData.CivEnumSideOne))
            {
                CombatData.SideOneOrder = order;
                sideShips = CombatData.SideOneShipCons;
                GameLogger.Log(GameLogger.LogCategory.Combat, $"Side One order set to: {order}", this);
            }
            else if (side == 2 || (side == 0 && civEnum == CombatData.CivEnumSideTwo))
            {
                CombatData.SideTwoOrder = order;
                sideShips = CombatData.SideTwoShipCons;
                GameLogger.Log(GameLogger.LogCategory.Combat, $"Side Two order set to: {order}", this);
            }

            // Propagate order to individual ships
            if (sideShips != null)
            {
                foreach (var ship in sideShips)
                {
                    if (ship != null)
                    {
                        ship.Order = order;
                        var stateMachine = ship.GetComponent<CombatOrderStateMachine>();
                        if (stateMachine != null) stateMachine.CurrentOrder = order;
                    }
                }
            }

            if (CombatData.SideOneOrder != CombatOrders.None && CombatData.SideTwoOrder != CombatOrders.None)
            {
                string summary = CombatOrderHelper.GetOrderSummary(CombatData.SideOneOrder, CombatData.SideTwoOrder);
                GameLogger.Log(GameLogger.LogCategory.Combat, $"📊 Combat Orders: {summary}", this);
            }
        }

        /// <summary>
        /// Stop all weapon fire
        /// </summary>
        public void StopAllWeaponFire()
        {
            targetingSystem.StopAllWeaponFire();
        }

        /// <summary>
        /// Show combat end sequence
        /// </summary>
        private IEnumerator ShowCombatEndSequence(bool sideOneWon)
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "Combat End Phase 1: Stopping movement and weapons", this);
            isMoving = false;

            yield return new WaitForSecondsRealtime(0.5f);

            GameLogger.Log(GameLogger.LogCategory.Combat, "Combat End Phase 2: Showing victory panel", this);
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.ShowCombatOverPanel();

                CivEnum winner = sideOneWon ? CombatData.CivEnumSideOne : CombatData.CivEnumSideTwo;
                CivEnum loser = sideOneWon ? CombatData.CivEnumSideTwo : CombatData.CivEnumSideOne;

                GameLogger.Log(GameLogger.LogCategory.Combat, $"🏆 Victory for: {winner}", this);
                GameLogger.Log(GameLogger.LogCategory.Combat, $"💀 Defeated: {loser}", this);
            }

            yield return new WaitForSecondsRealtime(2f);

            GameLogger.Log(GameLogger.LogCategory.Combat, "Combat End Phase 3: Returning to galaxy", this);
            EndCombat();
}

        public void OnReturnToGalaxyButtonClicked()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "Player clicked return to galaxy", this);
            EndCombat();
        }

        /// <summary>
        /// Cleanup orphaned projectiles from previous combat
        /// </summary>
        private void CleanupOrphanedProjectiles()
        {
            var torpedoes = FindObjectsByType<Torpedo>(FindObjectsSortMode.None);
            if (torpedoes.Length > 0)
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, $"⚠️ Found {torpedoes.Length} orphaned torpedoes - destroying silently", this);

                foreach (var torpedo in torpedoes)
                {
                    DestroyImmediate(torpedo.gameObject);
                }
            }
        }

        /// <summary>
        /// End combat - cleanup and return to galaxy
        /// </summary>
        public void EndCombat()
        {
            // Idempotency guard: the server calls this directly, then its own RpcCombatEnded
            // broadcast (see FleetController.RpcCombatEnded) loops back and calls it again on the
            // host's own client connection, and a non-host combatant client calls it from that same
            // Rpc. Without this guard all three paths would re-run cleanup.
            if (combatEnded) return;
            combatEnded = true;

            // Signals CombatQueueManager.ProcessCombatQueue's wait loop that this combat is
            // wrapping up, independent of whether/when ActiveCombatController gets nulled out
            // below via CombatManager.OnCombatEnded.
            isClosing = true;

            // The whole cleanup body below is wrapped in try/finally: any single exception here
            // (e.g. a null ref while walking a surviving fleet's diagnostics, or a scene-unload
            // failure) used to silently abort EndCombat() before it ever reached the
            // ServerNotifyCombatEnded broadcast at the bottom - meaning RpcCombatEnded never
            // reached any client and their Combat Over panel stayed up forever with no visible
            // error except whatever the server happened to log for the exception itself. The
            // finally block guarantees that broadcast still fires (so clients can always leave
            // the Combat scene) even if some cleanup step throws, and logs the exception so the
            // real cause is visible instead of just "the panel never closed."
            try
            {
                EndCombatCleanup();
            }
            catch (System.Exception ex)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, $"❌ EndCombat: exception during cleanup - {ex}", this);
            }
            finally
            {
                // Tell bystanders (see FleetController.RpcCombatEnded) they can hide the paused
                // notice, and combatants that haven't run EndCombat() locally yet (any non-host
                // client) tear down their own Combat scene view.
                //
                // Look this up now, AFTER EndCombatCleanup's fleet cleanup loop has already
                // destroyed any fleet that ended combat with 0 ships - _involvedFleets.Find(f =>
                // f != null) uses FleetController's overridden UnityEngine.Object == operator, so
                // a destroyed fleet is correctly skipped here. Previously this was captured
                // *before* that loop ran, so on a decisive victory it could grab the very fleet
                // the loop was about to destroy; calling ServerNotifyCombatEnded() through `?.` on
                // that now-destroyed MonoBehaviour doesn't short-circuit the way it would for a
                // real null (`?.` uses the raw C# reference, not Unity's overridden equality), so
                // it threw a MissingReferenceException that aborted EndCombat() right before this
                // broadcast.
                FleetController combatEndedAnchor = GetInvolvedFleetAnchor();
                if (NetworkServer.active && combatEndedAnchor != null)
                    combatEndedAnchor.ServerNotifyCombatEnded(CombatData.CivEnumSideOne, CombatData.CivEnumSideTwo);
            }
        }

        /// <summary>
        /// EndCombat's actual cleanup work, split out so EndCombat() can wrap it in a single
        /// try/finally that guarantees the end-of-combat network broadcast always fires.
        /// </summary>
        private void EndCombatCleanup()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "=== EndCombat: Starting cleanup ===", this);

            // Clean up ships
            if (CombatData.SideOneShipCons != null)
            {
                CleanupShips(CombatData.SideOneShipCons);
            }

            if (CombatData.SideTwoShipCons != null)
            {
                CleanupShips(CombatData.SideTwoShipCons);
            }

            // Use pre-captured fleet refs (collected at combat start, before any ship deaths)
            GameLogger.Log(GameLogger.LogCategory.Combat, $"  Processing {_involvedFleets.Count} fleets for end-of-combat cleanup", this);

            foreach (var fleet in _involvedFleets)
            {
                if (fleet == null) continue;

                int shipCount = fleet.FleetData?.ShipsList?.Count ?? 0;
                GameLogger.Log(GameLogger.LogCategory.Combat, $"  Fleet '{fleet.name}': {shipCount} ships remaining", this);

                if (shipCount == 0)
                {
                    GameLogger.Log(GameLogger.LogCategory.Combat, $"  Fleet '{fleet.name}' has no ships remaining - destroying", this);
                    FleetManager.Instance?.DestroyFleetController(fleet);
                }
                else
                {
                    GameLogger.Log(GameLogger.LogCategory.Combat, $"  Fleet '{fleet.name}' survived with {shipCount} ships", this);
                    fleet.UpdateMaxWarp();

                    // Diagnostic for the "fleet owner can't see their own surviving fleet on the
                    // galaxy map after combat" bug: dump every piece of state that governs whether
                    // this fleet's map icon should be visible, from THIS peer's own local
                    // perspective, right at the moment combat cleanup decides to keep it alive.
                    var diagFields = fleet.GetComponent<FleetChildFields>();
                    bool diagIsLocalPlayer = GameController.Instance != null && GameController.Instance.AreWeLocalPlayer(fleet.FleetData.CivEnum);
                    GameLogger.Log(GameLogger.LogCategory.Combat,
                        $"  [VisibilityDiag] Fleet '{fleet.name}' civ={fleet.FleetData.CivEnum} " +
                        $"AreWeLocalPlayer={diagIsLocalPlayer} NetworkServer.active={NetworkServer.active} " +
                        $"gameObject.activeInHierarchy={fleet.gameObject.activeInHierarchy} " +
                        $"gameObject.activeSelf={fleet.gameObject.activeSelf} " +
                        $"position={fleet.transform.position} parent={(fleet.transform.parent != null ? fleet.transform.parent.name : "null")} " +
                        $"InsigniaGO.activeSelf={(diagFields?.InsigniaGO != null ? diagFields.InsigniaGO.activeSelf.ToString() : "null")} " +
                        $"InsigniaSpriteRenderer.enabled={(diagFields?.InsigniaGO != null ? diagFields.InsigniaGO.GetComponent<SpriteRenderer>()?.enabled.ToString() : "null")} " +
                        $"InsigniaUnknownGO.activeSelf={(diagFields?.InsigniaUnknownGO != null ? diagFields.InsigniaUnknownGO.activeSelf.ToString() : "null")}",
                        this);
                }
            }

            // Clear temp fog revealer
            if (FleetManager.Instance != null && FleetManager.Instance.TempFogRevealerFleet != null)
            {
                FleetManager.Instance.TempFogRevealerFleet = null;
            }

            // Destroy health bars
            healthBarManager?.DestroyAllHealthBars();

            // Clean up UI references
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.CleanupCombat();
            }

            GameLogger.Log(GameLogger.LogCategory.Combat, "=== EndCombat: Cleanup complete ===", this);

            // Unload combat scene
            SceneController.Instance.UnloadCombatScene();
            SceneController.Instance.ReturnToGalaxyFromCombat();

            // Re-enable galaxy camera
            if (GalaxyCameraDragMoveZoom.Instance != null)
            {
                if (GalaxyCameraDragMoveZoom.Instance.TryGetComponent<Camera>(out var galaxyCam))
                {
                    galaxyCam.enabled = true;
                }
                GalaxyCameraDragMoveZoom.Instance.EnableCameraControl();
            }

            // Hide star system UI
            if (StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.MoveBackAnyStarSysUIGO();
                StarSysMenuUIController.Instance.HideA_SystemMenuView();
            }

            // Resume time
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.ResumeTime();
            }

            // Notify the queue this combat is done so it can dequeue the next one - was previously
            // nested inside the TimeManager null check above, which meant a null TimeManager.Instance
            // (unrelated to combat state) would silently leave ActiveCombatController set forever and
            // stall the entire combat queue.
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnCombatEnded(this);
            }
        }

        /// <summary>
        /// Clean up ships - remove combat elements and return to fleet
        /// </summary>
        private void CleanupShips(List<ShipController> ships)
        {
            for (int i = ships.Count - 1; i >= 0; i--)
            {
                var ship = ships[i];

                if (ship == null || ship.gameObject == null)
                {
                    continue;
                }

                if (ship.ShipData != null && ship.ShipData.CurrentFleetController != null)
                {
                    GameLogger.Log(GameLogger.LogCategory.Combat, $"  ✅ Ship '{ship.name}' survived - returning to fleet", this);

                    // Remove combat-specific children
                    List<Transform> childrenToDestroy = new List<Transform>();

                    foreach (Transform child in ship.transform)
                    {
                        if (child.name.Contains("_Model") ||
                            child.name.Contains("Healthbar") ||
                            child.name.Contains("Health") ||
                            child.name.Contains("Beam") ||
                            child.name.Contains("Torpedo"))
                        {
                            childrenToDestroy.Add(child);
                        }
                    }

                    foreach (var child in childrenToDestroy)
                    {
                        Destroy(child.gameObject);
                    }

                    // Re-parent to fleet GameObject
                    ship.transform.SetParent(ship.ShipData.CurrentFleetController.transform, false);
                    ship.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    ship.transform.localScale = Vector3.one;
                    ship.gameObject.SetActive(true);

                    GameLogger.Log(GameLogger.LogCategory.Combat, $"    ✅ Ship cleaned and returned to fleet", this);
                }
            }
        }

        // Audio helper methods
        public void PlayExplosionSound(Vector3 position)
        {
            AudioManager.Instance?.PlaySFX3D("Explosion", position);
        }

        public void PlayLaserSound()
        {
            AudioManager.Instance?.PlayRandomSFX("LaserShot");
        }

        public void PlayShieldHitSound()
        {
            AudioManager.Instance?.PlaySFX("ShieldHit");
        }

        // Legacy methods for compatibility
        public void ResetFriendAndEnemyLists()
        {
            CombatData.SideOneShipCons.Clear();
            CombatData.SideTwoShipCons.Clear();
        }

        public CivController SideTwoCivCombatants()
        {
            return CombatData.sideTwoCiv;
        }

        public CivController SideOneCivCombatants()
        {
            return CombatData.sideOneCiv;
        }

        public void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diplomacyCon, IPlayerController player)
        {
            // Implement logic for handling UI diplomacy orders if needed
        }

        public void GiveIntelOrder(SecretActionsEnum order, IPlayerController player)
        {
            // Implement logic for handling UI intel orders if needed
        }

        internal void TrySetPlayerOrders(CombatData combatData)
        {
            // TODO: Implement AI logic to set player orders based on combat data
        }
    }
}
