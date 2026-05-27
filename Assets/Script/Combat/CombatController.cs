using BOTF3D.Audio;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Main combat controller - coordinates all combat systems.
    /// Refactored to delegate responsibilities to specialized managers.
    /// </summary>
    public class CombatController : MonoBehaviour
    {
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

        private void Awake()
        {
            CombatID = GetEntityId();
            Debug.Log($"✅ CombatController {CombatID}: Created");

            // Add turn-based resolver component
            if (UseTurnBasedCombat)
            {
                TurnResolver = gameObject.AddComponent<TurnBasedCombatResolver>();
            }
        }

        private void Start()
        {
            CleanupOrphanedProjectiles();
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
                int sideOneAlive = CombatData.SideOneShipCons.Count(s => s != null && s.ShipData != null && !s.ShipData.Distroyed && s.ShipData.ShieldHealth + s.ShipData.HullHealth > 0);
                int sideTwoAlive = CombatData.SideTwoShipCons.Count(s => s != null && s.ShipData != null && !s.ShipData.Distroyed && s.ShipData.ShieldHealth + s.ShipData.HullHealth > 0);

                if (sideOneAlive == 0 || sideTwoAlive == 0)
                {
                    Debug.Log($"🏁 Combat ended! Side 1: {sideOneAlive} ships, Side 2: {sideTwoAlive} ships");
                    combatEnded = true;
                    targetingSystem.StopAllWeaponFire();

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

            Debug.Log("=== Starting Ship Setup ===");

            // Initialize all managers
            InitializeManagers();

            // Setup ships using ShipSetupManager
            shipSetupManager.SetupAllShips();

            Debug.Log("=== Ship Setup Complete ===");
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
                    TurnResolver.Initialize(this);
                    Debug.Log("🎮 Starting Turn-Based Combat");
                    TurnResolver.BeginOrderSelection();
                }
                else
                {
                    Debug.LogError("❌ TurnResolver is null! Cannot start turn-based combat.");
                }
            }
            else
            {
                // Original real-time combat
                shipGroupManager.InitializeShipGroupsForEngage();
                targetingSystem.AssignTargetsToAllShips();
                yield return StartAllShipWeaponFire();
                isMoving = true;
                Debug.Log($"✅ Combat Controller {CombatID}: Order-based movement ENABLED");
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
            if (order != CombatOrders.AttackTransports) return;

            float centerY = 0f;
            float centerZ = 0f;
            int count = 0;

            foreach (var ship in ships)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    centerY += ship.transform.position.y;
                    centerZ += ship.transform.position.z;
                    count++;
                }
            }

            if (count > 0)
            {
                centerY /= count;
                centerZ /= count;
            }

            Debug.Log($"🎯 Setting up flanking for Side {(isSideOne ? "One" : "Two")}, center: Y={centerY:F1}, Z={centerZ:F1}");

            foreach (var ship in ships)
            {
                if (ship == null || ship.ShipData.Distroyed) continue;

                bool isFlankingShip = ship.ShipData.ShipType == ShipType.Scout ||
                                      ship.ShipData.ShipType == ShipType.Destroyer;

                if (isFlankingShip)
                {
                    bool isAboveCenter = ship.transform.position.y > centerY;
                    float baseYRotation = isSideOne ? 90f : -90f;
                    float flankRotation = isAboveCenter ? 40f : -40f;

                    ship.transform.rotation = Quaternion.Euler(0, baseYRotation + flankRotation, 0);

                    Debug.Log($"  🛸 {ship.ShipData.ShipName} ({ship.ShipData.ShipType}): Flanking rotation {baseYRotation + flankRotation}°");
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
                Debug.LogError("❌ ShipCombatCameraController.Instance is null!");
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

            Debug.Log($"📷 Setting camera to track {allShips.Count} ships");

            ShipCombatCameraController.Instance.SetTargets(allShips.ToArray());
            ShipCombatCameraController.Instance.SetWarpingIn(false);
            ShipCombatCameraController.Instance.SetWarpingInOver(true);

            Debug.Log("✅ Camera targets configured");
        }

        /// <summary>
        /// Begin order-based movement after warp completes
        /// </summary>
        public void BeginOrderBasedMovement()
        {
            Debug.Log("📊 Beginning order-based movement...");
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
            Debug.Log("🔫 Starting weapon fire for all ships with balanced timing...");

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
                    Debug.Log($"  Side 1: {ship.ShipData.ShipName} starting in {delay:F2}s");
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
                    Debug.Log($"  Side 2: {ship.ShipData.ShipName} starting in {delay:F2}s");
                    index2++;
                }
            }

            Debug.Log($"✅ Weapon fire started for {index1 + index2} ships");
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
        public void SetShipOrders(CombatOrders order, CivEnum civEnum)
        {
            List<ShipController> sideShips = null;
            if (civEnum == CombatData.CivEnumSideOne)
            {
                CombatData.SideOneOrder = order;
                sideShips = CombatData.SideOneShipCons;
                Debug.Log($"Side One order set to: {order}");
            }
            else if (civEnum == CombatData.CivEnumSideTwo)
            {
                CombatData.SideTwoOrder = order;
                sideShips = CombatData.SideTwoShipCons;
                Debug.Log($"Side Two order set to: {order}");
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
                Debug.Log($"📊 Combat Orders: {summary}");
            }
        }

        /// <summary>
        /// Set random AI order for a side
        /// </summary>
        public void SetAIRandomOrder(CivEnum aiCivEnum)
        {
            int side = 0;

            if (aiCivEnum == CombatData.CivEnumSideOne)
            {
                side = 1;
            }
            else if (aiCivEnum == CombatData.CivEnumSideTwo)
            {
                side = 2;
            }

            var availableOrders = new List<CombatOrders>
            {
                CombatOrders.Engage,
                CombatOrders.Formation,
                CombatOrders.Rush,
                CombatOrders.Retreat
            };

            bool enemyHasTransports = CombatOrderHelper.HasTransports(CombatData, side == 1 ? 2 : 1);
            if (enemyHasTransports)
            {
                availableOrders.Add(CombatOrders.AttackTransports);
                Debug.Log($"🎯 Enemy has transports - AttackTransports order available for AI");
            }

            CombatOrders randomOrder = availableOrders[Random.Range(0, availableOrders.Count)];

            Debug.Log($"🤖 AI ({aiCivEnum}) selected order: {randomOrder}");

            SetShipOrders(randomOrder, aiCivEnum);
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
            Debug.Log("Combat End Phase 1: Stopping movement and weapons");
            isMoving = false;

            yield return new WaitForSecondsRealtime(1f);

            Debug.Log("Combat End Phase 2: Showing victory panel");
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.ShowCombatOverPanel();

                CivEnum winner = sideOneWon ? CombatData.CivEnumSideOne : CombatData.CivEnumSideTwo;
                CivEnum loser = sideOneWon ? CombatData.CivEnumSideTwo : CombatData.CivEnumSideOne;

                Debug.Log($"🏆 Victory for: {winner}");
                Debug.Log($"💀 Defeated: {loser}");
            }

            yield return new WaitForSecondsRealtime(5f);

            Debug.Log("Combat End Phase 3: Returning to galaxy");
            EndCombat();
        }

        public void OnReturnToGalaxyButtonClicked()
        {
            Debug.Log("Player clicked return to galaxy");
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
                Debug.Log($"⚠️ Found {torpedoes.Length} orphaned torpedoes - destroying silently");

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
            Debug.Log("=== EndCombat: Starting cleanup ===");

            // Clean up ships
            if (CombatData.SideOneShipCons != null)
            {
                CleanupShips(CombatData.SideOneShipCons);
            }

            if (CombatData.SideTwoShipCons != null)
            {
                CleanupShips(CombatData.SideTwoShipCons);
            }

            // Get all fleets involved
            var allCombatFleets = new List<FleetController>();

            if (CombatData != null)
            {
                if (CombatData.SideOneShipCons != null)
                {
                    foreach (var ship in CombatData.SideOneShipCons)
                    {
                        if (ship != null && ship.ShipData != null && ship.ShipData.CurrentFleetController != null)
                        {
                            if (!allCombatFleets.Contains(ship.ShipData.CurrentFleetController))
                            {
                                allCombatFleets.Add(ship.ShipData.CurrentFleetController);
                            }
                        }
                    }
                }

                if (CombatData.SideTwoShipCons != null)
                {
                    foreach (var ship in CombatData.SideTwoShipCons)
                    {
                        if (ship != null && ship.ShipData != null && ship.ShipData.CurrentFleetController != null)
                        {
                            if (!allCombatFleets.Contains(ship.ShipData.CurrentFleetController))
                            {
                                allCombatFleets.Add(ship.ShipData.CurrentFleetController);
                            }
                        }
                    }
                }
            }

            Debug.Log($"  Found {allCombatFleets.Count} unique fleets in combat");

            // Destroy empty fleets
            foreach (var fleet in allCombatFleets)
            {
                if (fleet == null) continue;

                int shipCount = fleet.FleetData?.ShipsList?.Count ?? 0;
                Debug.Log($"  🚢 Fleet '{fleet.name}': {shipCount} ships remaining");

                if (shipCount == 0)
                {
                    Debug.LogWarning($"  💀 Fleet '{fleet.name}' has NO SHIPS - DESTROYING FLEET");

                    if (FleetManager.Instance != null)
                    {
                        FleetManager.Instance.DestroyFleetController(fleet);
                    }

                    if (fleet != null && fleet.gameObject != null)
                    {
                        if (fleet.FleetUIGameObject != null)
                        {
                            DestroyImmediate(fleet.FleetUIGameObject);
                        }

                        if (fleet.DropLine != null && fleet.DropLine.gameObject != null)
                        {
                            DestroyImmediate(fleet.DropLine.gameObject);
                        }

                        DestroyImmediate(fleet.gameObject);
                        Debug.LogWarning($"    ✅ Fleet '{fleet.name}' destroyed");
                    }
                }
                else
                {
                    Debug.Log($"  ✅ Fleet '{fleet.name}' survived with {shipCount} ships");
                    fleet.UpdateMaxWarp();
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

            Debug.Log("=== EndCombat: Cleanup complete ===");

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
                    Debug.Log($"  ✅ Ship '{ship.name}' survived - returning to fleet");

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
                    ship.gameObject.SetActive(false);

                    Debug.Log($"    ✅ Ship cleaned and returned to fleet");
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
