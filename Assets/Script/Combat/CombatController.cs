using BOTF3D.Audio;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BOTF3D.Combat
{
    public class CombatController : MonoBehaviour
    {
        // Static wait timers
        private static WaitForSecondsRealtime _waitForSeconds3 = new WaitForSecondsRealtime(3f);
        private static WaitForSecondsRealtime _waitForSeconds2 = new WaitForSecondsRealtime(2f);
        // formation spacing
        int spacing = 50;
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

        // Ship groups for combat orders
        [Header("Combat Order System")]
        public List<ShipGroup> sideOneGroups = new List<ShipGroup>();
        public List<ShipGroup> sideTwoGroups = new List<ShipGroup>();
        private bool groupsInitialized = false;

        // Combat UI and resources
        public Canvas ShipCombatCanvas;
        public List<GameObject> HealthbarRenderers { get; private set; } = new List<GameObject>();

        // Weapon prefabs
        public GameObject SideOneTorpedoPrefab;
        public GameObject SideTwoTorpedoPrefab;
        public GameObject SideOneBeamPrefab;
        public GameObject SideTwoBeamPrefab;

        // Audio clips
        public AudioClip SideOneBeamFireClip;
        public AudioClip SideTwoBeamFireClip;
        public AudioClip SideOneTorpedoFireClip;
        public AudioClip SideTwoTorpedoFireClip;
        public SoundData warpInSound;

        // Movement parameters
        [Header("Combat Movement")]
        public float initialSpeed = 30f;
        public float stopDistance = 390f;
        private float deceleration;
        private float currentSpeed;

        // Ship categorization lists (used for ordering)
        private List<ShipController> _hvyCruisersSide1 = new List<ShipController>();
        private List<ShipController> _hvyCruisersSide2 = new List<ShipController>();
        private List<ShipController> _ltCruisersSide1 = new List<ShipController>();
        private List<ShipController> _ltCruisersSide2 = new List<ShipController>();
        private List<ShipController> _cruisersSide1 = new List<ShipController>();
        private List<ShipController> _cruisersSide2 = new List<ShipController>();
        private List<ShipController> _destroyersSide1List = new List<ShipController>();
        private List<ShipController> _destroyersSide2List = new List<ShipController>();
        private List<ShipController> _scoutsSide1List = new List<ShipController>();
        private List<ShipController> _scoutsSide2List = new List<ShipController>();
        private List<ShipController> _transportsSide1List = new List<ShipController>();
        private List<ShipController> _transportsSide2List = new List<ShipController>();

        // Warp animation constants
        private const float SIDE1_COMBAT_START_X = -3000f;
        private const float SIDE1_COMBAT_END_X = -200f;
        private const float SIDE1_TRANSPORT_START_X = -3200f;
        private const float SIDE1_TRANSPORT_END_X = -400f;
        private const float SIDE2_COMBAT_START_X = 3000f;
        private const float SIDE2_COMBAT_END_X = 200f;
        private const float SIDE2_TRANSPORT_START_X = 3200f;
        private const float SIDE2_TRANSPORT_END_X = 400f;
        private const float WARP_DURATION = 2.5f; // seconds - buffer for staggered arrivals
        private const float CONTRACTION_DURATION = 0.4f; // seconds - quick contraction so early ships finish before late arrivals

        private void Awake()
        {
            CombatID = GetEntityId();
            Debug.Log($"✅ CombatController {CombatID}: Created");
        }

        private void Start()
        {
            currentSpeed = 30f;
            stopDistance = 390f;
            CleanupOrphanedProjectiles();
        }

        void Update()
        {
            // Update group targets for Engage order
            if (WarpingAnimationOver && !combatEnded)
            {
                if (CombatData.SideOneOrder == CombatOrders.Engage || CombatData.SideTwoOrder == CombatOrders.Engage)
                {
                    UpdateGroupTargets();
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
                    StopAllWeaponFire();

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
            // Order-based movement system (after warp completes)
            if (WarpingAnimationOver && !WarpingIn && isMoving && !combatEnded)
            {
                UpdateEngageGroups();

                // Process each ship individually
                foreach (var ship in CombatData.SideOneShipCons)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        MoveShipBasedOnOrder(ship);
                    }
                }

                foreach (var ship in CombatData.SideTwoShipCons)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        MoveShipBasedOnOrder(ship);
                    }
                }
            }
            else if (isMoving && !combatEnded)
            {
                // Diagnostic logging to see why it stopped
                // Debug.Log($"Waiting for Warp: AnimationOver={WarpingAnimationOver}, WarpingIn={WarpingIn}");
            }
        }

        private void UpdateEngageGroups()
        {
            if (sideOneGroups != null) foreach (var g in sideOneGroups) UpdateGroupTarget(g, true);
            if (sideTwoGroups != null) foreach (var g in sideTwoGroups) UpdateGroupTarget(g, false);
        }

        private void UpdateGroupTarget(ShipGroup group, bool isSideOne)
        {
            group.ships.RemoveAll(s => s == null || s.ShipData.Distroyed);
            if (group.ships.Count == 0) return;

            if (group.commonTarget == null || group.commonTarget.ShipData.Distroyed)
            {
                Vector3 center = Vector3.zero;
                foreach (var s in group.ships) center += s.transform.position;
                center /= group.ships.Count;

                List<ShipController> enemies = isSideOne ? CombatData.SideTwoShipCons : CombatData.SideOneShipCons;
                group.commonTarget = enemies.Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType != ShipType.Transport)
                                            .OrderBy(s => Vector3.Distance(center, s.transform.position))
                                            .FirstOrDefault();

                foreach (var s in group.ships) s.ShipData.TargetThisShipController = group.commonTarget;
            }
        }

        /// <summary>
        /// Main entry point: Setup ships and start warp-in animation
        /// </summary>
        public void PopulateShipData(CombatController theCombatController)
        {
            if (theCombatController != this) return;

            Debug.Log("=== Starting Ship Setup ===");

            // Categorize ships by type
            CountShips();

            // Setup Side One ships
            SetupShips(CombatData.SideOneShipCons, 1);

            // Setup Side Two ships
            SetupShips(CombatData.SideTwoShipCons, 2);

            Debug.Log("=== Ship Setup Complete ===");
        }
        private void SetupShips(List<ShipController> shipList, int side)
        {
            // ✅ DIAGNOSTIC: Log what we received
            Debug.Log($"=== SetupShips Side {side}: {shipList.Count} total ships ===");
            foreach (var s in shipList)
            {
                if (s == null) Debug.LogWarning($"  ⚠️ NULL ship in Side {side} list!");
                else Debug.Log($"  Ship: {s.ShipData?.ShipName ?? "NO DATA"} Type: {s.ShipData?.ShipType}");
            }

            // Separate combat ships from transports
            List<ShipController> combatShips = shipList
                .Where(s => s != null && s.ShipData != null && s.ShipData.ShipType != ShipType.Transport)
                .ToList();
            List<ShipController> transportShips = shipList
                .Where(s => s != null && s.ShipData != null && s.ShipData.ShipType == ShipType.Transport)
                .ToList();

            Debug.Log($"  Side {side}: {combatShips.Count} combat, {transportShips.Count} transports");

            // Generate spiral positions for wall formation
            List<Vector2Int> combatSpiralPositions = GenerateSpiralPositions(combatShips.Count);

            // ✅ Offset transport spiral so they don't overlap combat ships at (0,0)
            // Transports are placed behind the combat line (higher X-index offset in spiral)
            int transportSpiralOffset = Mathf.CeilToInt(Mathf.Sqrt(combatShips.Count)) + 1;
            List<Vector2Int> transportSpiralPositions = GenerateSpiralPositions(transportShips.Count + transportSpiralOffset)
                .Skip(transportSpiralOffset)
                .ToList();

            // Setup combat ships
            for (int i = 0; i < combatShips.Count; i++)
            {
                Debug.Log($"  Combat ship [{i}] {combatShips[i].ShipData.ShipName} → spiral {combatSpiralPositions[i]}");
                SetupSingleShip(combatShips[i], side, false, combatSpiralPositions[i]);
            }

            // Setup transport ships
            for (int i = 0; i < transportShips.Count; i++)
            {
                Debug.Log($"  Transport [{i}] {transportShips[i].ShipData.ShipName} → spiral {transportSpiralPositions[i]}");
                SetupSingleShip(transportShips[i], side, true, transportSpiralPositions[i]);
            }

            Debug.Log($"Side {side}: Setup {combatShips.Count} combat ships + {transportShips.Count} transports");
        }

        /// <summary>
        /// Setup a single ship with model, position, and rotation
        /// </summary>
        private void SetupSingleShip(ShipController ship, int side, bool isTransport, Vector2Int spiralPos)
        {
            // Calculate start and end X positions
            float startX, endX;
            if (side == 1)
            {
                startX = isTransport ? SIDE1_TRANSPORT_START_X : SIDE1_COMBAT_START_X;
                endX = isTransport ? SIDE1_TRANSPORT_END_X : SIDE1_COMBAT_END_X;
            }
            else // side == 2
            {
                startX = isTransport ? SIDE2_TRANSPORT_START_X : SIDE2_COMBAT_START_X;
                endX = isTransport ? SIDE2_TRANSPORT_END_X : SIDE2_COMBAT_END_X;
            }

            // Side 1 enters from left (-X) moving right (+X direction)
            // Side 2 enters from right (+X) moving left (-X direction)
            // Use the spiral position to spread ships in Y (vertical) and Z (depth)
            Vector3 startPosition = new Vector3(startX, spiralPos.y * spacing, spiralPos.x * spacing);
            Vector3 endPosition = new Vector3(endX, spiralPos.y * spacing, spiralPos.x * spacing);

            // ✅ FIX: Remove parent FIRST (before moving to scene)
            ship.transform.SetParent(null, true); // worldPositionStays = true

            // ✅ NOW move to CombatScene (only works on root GameObjects)
            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            if (combatScene.isLoaded)
            {
                if (ship.gameObject.scene != combatScene)
                {
                    Debug.Log($"  Moving ship '{ship.ShipData.ShipName}' from {ship.gameObject.scene.name} to CombatScene");
                    SceneManager.MoveGameObjectToScene(ship.gameObject, combatScene);
                }
            }
            else
            {
                Debug.LogError("❌ CombatScene is not loaded! Cannot move ships.");
                return;
            }

            // Set ship transform
            ship.transform.position = startPosition;

            // ✅ Reverted to prior rotation settings for Blender mesh import alignment
            if (side == 1)
            {
                ship.transform.rotation = Quaternion.Euler(0, -90, 0); // Side 1
            }
            else
            {
                ship.transform.rotation = Quaternion.Euler(0, 90, 0); // Side 2
            }

            ship.transform.localScale = Vector3.one;
            ship.name = ship.ShipData.ShipName;
            ship.gameObject.SetActive(true);

            // Verify
            if (!ship.gameObject.activeInHierarchy)
            {
                Debug.LogError($"❌ Ship '{ship.ShipData.ShipName}' is not active after setup!");
            }

            GameObject shipModel = null;

            // Instantiate ship model
            GameObject fbx = GetShipSOForShip(ship).ShipFBX_ModelAsGOPrefab;
            if (fbx != null)
            {
                shipModel = Instantiate(fbx);
                shipModel.transform.SetParent(ship.transform, false);
                shipModel.transform.localPosition = Vector3.zero;

                // ✅ Apply constant rotation offset to correct FBX orientation
                shipModel.transform.localRotation = Quaternion.Euler(0, 0, 0); // Adjust as needed

                shipModel.transform.localScale = Vector3.one;

                DisableStencilOnShipRenderers(shipModel);
                SetLayerRecursively(ship.gameObject, LayerMask.NameToLayer("Default"));

                // Add collider for targeting
                BoxCollider boxCollider = ship.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = ship.gameObject.AddComponent<BoxCollider>();
                }
                boxCollider.isTrigger = true;

                // ✅ Ensure CombatOrderStateMachine is present
                if (ship.GetComponent<CombatOrderStateMachine>() == null)
                {
                    ship.gameObject.AddComponent<CombatOrderStateMachine>();
                    Debug.Log($"  ➕ Added CombatOrderStateMachine to {ship.ShipData.ShipName}");
                }

                // Set collider bounds from renderer
                Renderer renderer = shipModel.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    Vector3 localCenter = fbx.transform.InverseTransformPoint(renderer.bounds.center);
                    Vector3 localSize = fbx.transform.InverseTransformVector(renderer.bounds.size);
                    boxCollider.center = new Vector3(localCenter.x, localCenter.z, localCenter.y);
                    float width = Mathf.Abs(localSize.x);
                    float height = Mathf.Abs(localSize.z);
                    float length = Mathf.Abs(localSize.y);
                    boxCollider.size = new Vector3(width, height, length);
                }
            }
            else
            {
                Debug.LogError($"❌ Ship FBX prefab is null for {ship.ShipData.ShipName}!");
            }

            // Store warp data for animation - NOW includes ship model reference
            WarpData warpData = ship.gameObject.AddComponent<WarpData>();
            warpData.Initialize(startPosition, endPosition, shipModel, side);

            // Setup weapons
            ship.SetWeaponPrefabs();
            ship.SetWeaponAudioClips(
                side == 1 ? SideOneBeamFireClip : SideTwoBeamFireClip,
                side == 1 ? SideOneTorpedoFireClip : SideTwoTorpedoFireClip
            );

            Debug.Log($"  ✅ Setup {ship.ShipData.ShipName} in CombatScene at {startPosition}");
        }

        /// <summary>
        /// Simple component to store warp start/end positions and ship model reference
        /// </summary>
        private class WarpData : MonoBehaviour
        {
            public Vector3 startPosition;
            public Vector3 endPosition;
            public GameObject shipModel; // Reference to the child model for stretching
            public float startDelay; // Random delay before ship starts warping in
            public float travelDuration; // How long this ship takes to reach end position
            public bool hasArrived; // Track if ship has reached end position
            public bool isContracting; // Track if ship is currently contracting
            public float contractionStartTime; // When contraction began
            public float contractionProgress; // 0-1 progress through contraction

            public void Initialize(Vector3 start, Vector3 end, GameObject model, int shipSide)
            {
                startPosition = start;
                endPosition = end;
                shipModel = model;

                // ✅ Random start delay: 0-1.0 seconds (spread out ship starts)
                startDelay = UnityEngine.Random.Range(0f, 1.0f);

                // ✅ Constant travel duration: all ships move at same speed
                travelDuration = 0.6f;

                hasArrived = false;
                isContracting = false;
                contractionStartTime = 0f;
                contractionProgress = 0f;
            }
        }

        /// <summary>
        /// Start the warp-in animation coroutine with Star Trek warp stretch effect
        /// </summary>
        public IEnumerator StartWarpInAnimation()
        {
            Debug.Log("🌀 Starting warp-in animation...");

            WarpingIn = true;
            WarpingAnimationOver = false;

            // ✅ Tell camera we're warping in
            if (ShipCombatCameraController.Instance != null)
            {
                ShipCombatCameraController.Instance.SetWarpingIn(true);
            }

            // Play warp sound
            if (warpInSound != null)
            {
                AudioManager.Instance?.PlaySFX3D(warpInSound.name, Vector3.zero);
            }

            // Collect all ships with warp data
            List<WarpData> allWarpData = new List<WarpData>();
            foreach (var ship in CombatData.SideOneShipCons)
            {
                var wd = ship?.GetComponent<WarpData>();
                if (wd != null) allWarpData.Add(wd);
            }
            foreach (var ship in CombatData.SideTwoShipCons)
            {
                var wd = ship?.GetComponent<WarpData>();
                if (wd != null) allWarpData.Add(wd);
            }

            Debug.Log($"  Animating {allWarpData.Count} ships - staggered start with individual contraction");


            // ✅ Initial stretch: all ships start stretched (not yet visible)
            float warpStretchScale = 50f;

            foreach (var wd in allWarpData)
            {
                if (wd != null && wd.gameObject != null && wd.shipModel != null)
                {
                    // ✅ Stretch the CHILD MODEL along its local Z-axis (ship's forward direction)
                    wd.shipModel.transform.localScale = new Vector3(1f, 1f, warpStretchScale);
                }
            }

            yield return null;

            // ✅ Combined Phase: Ships warp in at random times, contract individually when arriving
            // Total max duration: longest startDelay + longest travelDuration + CONTRACTION_DURATION
            // = 0.5s + 1.2s + 1.0s = 2.7 seconds maximum
            float maxPhaseDuration = WARP_DURATION + 1.5f; // Extra buffer to ensure all complete
            float elapsed = 0f;

            while (elapsed < maxPhaseDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                bool allShipsComplete = true;

                foreach (var wd in allWarpData)
                {
                    if (wd == null || wd.gameObject == null) continue;

                    ShipController shipController = wd.GetComponent<ShipController>();
                    if (shipController == null) continue;

                    // ✅ Phase 1: Ship hasn't started yet (waiting for startDelay)
                    if (elapsed < wd.startDelay)
                    {
                        // Ship stays at start position, still stretched
                        allShipsComplete = false;
                        continue;
                    }

                    // ✅ Phase 2: Ship is traveling to end position
                    if (!wd.hasArrived)
                    {
                        float travelElapsed = elapsed - wd.startDelay;
                        float travelT = Mathf.Clamp01(travelElapsed / wd.travelDuration);

                        if (travelT < 1f)
                        {
                            // Still traveling
                            float smoothT = 1f - Mathf.Pow(1f - travelT, 3f);
                            shipController.transform.position = Vector3.Lerp(wd.startPosition, wd.endPosition, smoothT);
                            allShipsComplete = false;
                        }
                        else
                        {
                            // Just arrived - snap to end position and start contraction
                            shipController.transform.position = wd.endPosition;
                            wd.hasArrived = true;
                            wd.isContracting = true;
                            wd.contractionStartTime = elapsed;
                            wd.contractionProgress = 0f;
                        }
                    }

                    // ✅ Phase 3: Ship is contracting from 5x to 1x
                    if (wd.isContracting && wd.contractionProgress < 1f)
                    {
                        float contractionElapsed = elapsed - wd.contractionStartTime;
                        wd.contractionProgress = Mathf.Clamp01(contractionElapsed / CONTRACTION_DURATION);

                        float smoothContractionT = Mathf.Pow(wd.contractionProgress, 2f);
                        float currentScale = Mathf.Lerp(warpStretchScale, 1f, smoothContractionT);

                        if (wd.shipModel != null)
                        {
                            wd.shipModel.transform.localScale = new Vector3(1f, 1f, currentScale);
                        }

                        if (wd.contractionProgress < 1f)
                        {
                            allShipsComplete = false;
                        }
                    }
                }

                // Exit early if all ships finished
                if (allShipsComplete)
                {
                    Debug.Log($"✅ All ships complete at {elapsed:F2}s");
                    break;
                }

                yield return null;
            }

            Debug.Log("✅ Warp-in and contraction complete for all ships");

            // Final cleanup - reset parent AND child scales
            foreach (var wd in allWarpData)
            {
                if (wd != null && wd.gameObject != null)
                {
                    ShipController shipController = wd.GetComponent<ShipController>();
                    if (shipController != null)
                    {
                        shipController.transform.position = wd.endPosition;
                        shipController.transform.localScale = Vector3.one; // Parent stays at 1,1,1
                    }

                    // ✅ Ensure child model is also reset to 1,1,1
                    if (wd.shipModel != null)
                    {
                        wd.shipModel.transform.localScale = Vector3.one;
                    }

                    shipController.SetWarpInOver();
                    Destroy(wd);
                }
            }

            Debug.Log("✅ Warp-in animation complete");

            WarpingIn = false;
            WarpingAnimationOver = true;

            SetupCameraTargets();
            CreateHealthBarsForAllShips();
            // Initialize ship groups
            InitializeShipGroupsForEngage();

            // ✅ Assign targets BEFORE weapon fire starts
            AssignTargetsToAllShips();

            // Start weapon firing
            yield return StartAllShipWeaponFire();

            isMoving = true;
            Debug.Log($"✅ Combat Controller {CombatID}: Order-based movement ENABLED. Side 1 order: {CombatData.SideOneOrder}, Side 2 order: {CombatData.SideTwoOrder}");
        }

        /// <summary>
        /// Setup camera to track all ships and enable dynamic framing
        /// </summary>
        private void SetupCameraTargets()
        {
            if (ShipCombatCameraController.Instance == null)
            {
                Debug.LogError("❌ ShipCombatCameraController.Instance is null!");
                return;
            }

            // Combine all ships from both sides
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

            // Tell camera which ships to track
            ShipCombatCameraController.Instance.SetTargets(allShips.ToArray());

            // Mark warp complete - enables dynamic camera positioning
            ShipCombatCameraController.Instance.SetWarpingIn(false);
            ShipCombatCameraController.Instance.SetWarpingInOver(true);

            Debug.Log("✅ Camera targets configured - dynamic framing enabled");
        }

        /// <summary>
        /// Generate spiral positions for ship wall formation
        /// </summary>
        private List<Vector2Int> GenerateSpiralPositions(int count)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            if (count <= 0) return positions;

            // Start at center
            positions.Add(new Vector2Int(0, 0));
            if (count == 1) return positions;

            // Generate square spiral pattern
            int x = 0, y = 0;
            int dx = 0, dy = -1;

            for (int i = 1; i < count; i++)
            {
                // Change direction at spiral corners
                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    int temp = dx;
                    dx = -dy;
                    dy = temp;
                }

                x += dx;
                y += dy;
                positions.Add(new Vector2Int(x, y));
            }

            return positions;
        }

        /// <summary>
        /// Get ShipSO for a ship controller
        /// </summary>
        private ShipSO GetShipSOForShip(ShipController shipCon)
        {
            List<ShipSO> daList = ShipManager.Instance.FedShipSOList;
            CivEnum daCiv = shipCon.ShipData.CivEnum;

            switch (daCiv)
            {
                case CivEnum.FED: daList = ShipManager.Instance.FedShipSOList; break;
                case CivEnum.KLING: daList = ShipManager.Instance.KlingShipSOList; break;
                case CivEnum.ROM: daList = ShipManager.Instance.RomShipSOList; break;
                case CivEnum.CARD: daList = ShipManager.Instance.CardShipSOList; break;
                case CivEnum.DOM: daList = ShipManager.Instance.DomShipSOList; break;
                case CivEnum.BORG: daList = ShipManager.Instance.BorgShipSOList; break;
                case CivEnum.TERRAN: daList = ShipManager.Instance.TerranShipSOList; break;
                default: daList = ShipManager.Instance.FedShipSOList; break;
            }

            for (int j = 0; j < daList.Count; j++)
            {
                if (daList[j].ShipName == shipCon.ShipData.ShipName)
                {
                    return daList[j];
                }
            }

            return ShipManager.Instance.FedShipSOList.FirstOrDefault();
        }

        /// <summary>
        /// Categorize ships by type for both sides
        /// </summary>
        private void CountShips()
        {
            // Clear all lists
            _hvyCruisersSide1.Clear();
            _hvyCruisersSide2.Clear();
            _ltCruisersSide1.Clear();
            _ltCruisersSide2.Clear();
            _cruisersSide1.Clear();
            _cruisersSide2.Clear();
            _destroyersSide1List.Clear();
            _destroyersSide2List.Clear();
            _scoutsSide1List.Clear();
            _scoutsSide2List.Clear();
            _transportsSide1List.Clear();
            _transportsSide2List.Clear();

            // Categorize Side One ships
            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship == null || ship.ShipData == null) continue;

                switch (ship.ShipData.ShipType)
                {
                    case ShipType.HvyCruiser: _hvyCruisersSide1.Add(ship); break;
                    case ShipType.LtCruiser: _ltCruisersSide1.Add(ship); break;
                    case ShipType.Cruiser: _cruisersSide1.Add(ship); break;
                    case ShipType.Destroyer: _destroyersSide1List.Add(ship); break;
                    case ShipType.Scout: _scoutsSide1List.Add(ship); break;
                    case ShipType.Transport: _transportsSide1List.Add(ship); break;
                }
            }

            // Categorize Side Two ships
            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship == null || ship.ShipData == null) continue;

                switch (ship.ShipData.ShipType)
                {
                    case ShipType.HvyCruiser: _hvyCruisersSide2.Add(ship); break;
                    case ShipType.LtCruiser: _ltCruisersSide2.Add(ship); break;
                    case ShipType.Cruiser: _cruisersSide2.Add(ship); break;
                    case ShipType.Destroyer: _destroyersSide2List.Add(ship); break;
                    case ShipType.Scout: _scoutsSide2List.Add(ship); break;
                    case ShipType.Transport: _transportsSide2List.Add(ship); break;
                }
            }

            Debug.Log($"Ship count - Side 1: HvyCruisers={_hvyCruisersSide1.Count}, LtCruisers={_ltCruisersSide1.Count}, Cruisers={_cruisersSide1.Count}, Destroyers={_destroyersSide1List.Count}, Scouts={_scoutsSide1List.Count}, Transports={_transportsSide1List.Count}");
            Debug.Log($"Ship count - Side 2: HvyCruisers={_hvyCruisersSide2.Count}, LtCruisers={_ltCruisersSide2.Count}, Cruisers={_cruisersSide2.Count}, Destroyers={_destroyersSide2List.Count}, Scouts={_scoutsSide2List.Count}, Transports={_transportsSide2List.Count}");
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
        /// Move ship based on combat order - rotation is optional for visuals
        /// </summary>
        private void MoveShipBasedOnOrder(ShipController ship)
        {
            if (ship.ShipData.Distroyed) return;

            var orderStateMachine = ship.GetComponent<CombatOrderStateMachine>();
            if (orderStateMachine != null && !orderStateMachine.IsWarpingOut())
            {
                float speed = ship.ShipData.maxWarpFactor * orderStateMachine.GetOrderSpeedFactor();
                float step = speed * Time.unscaledDeltaTime;

                Vector3 targetPosition = GetTargetPositionForShip(ship);

                if (targetPosition != Vector3.zero)
                {
                    Vector3 toTarget = targetPosition - ship.transform.position;

                    // If order is Engage or Rush, move mainly forward (X axis)
                    CombatOrders order = CombatData.SideOneShipCons.Contains(ship) ? CombatData.SideOneOrder : CombatData.SideTwoOrder;
                    if (order == CombatOrders.Engage || order == CombatOrders.Rush)
                    {
                        // Project direction onto world X axis, but keep some Y/Z for intercept
                        toTarget.y *= 0.1f;
                        toTarget.z *= 0.1f;
                    }

                    Vector3 directionToTarget = toTarget.normalized;

                    // ✅ Move toward target
                    ship.transform.position += directionToTarget * step;
                }
                else
                {
                    // Default straight-line movement
                    bool isSideOne = CombatData.SideOneShipCons.Contains(ship);
                    Vector3 direction = isSideOne ? Vector3.right : Vector3.left;
                    ship.transform.position += direction * step;
                }
            }
        }

        /// <summary>
        /// Get target position for ship based on combat order
        /// </summary>
        private Vector3 GetTargetPositionForShip(ShipController ship)
        {
            bool isSideOne = CombatData.SideOneShipCons.Contains(ship);
            CombatOrders order = isSideOne ? CombatData.SideOneOrder : CombatData.SideTwoOrder;

            switch (order)
            {
                case CombatOrders.AttackTransports:
                    return GetFlankingPositionToTransports(ship, isSideOne);

                case CombatOrders.Formation:
                    return GetFormationPosition(ship, isSideOne);

                case CombatOrders.Retreat:
                    return GetRetreatPosition(ship, isSideOne);

                case CombatOrders.Rush:
                case CombatOrders.Engage:
                default:
                    ShipController target = ship.ShipData.TargetThisShipController;
                    if (target != null && !target.ShipData.Distroyed)
                    {
                        return target.transform.position;
                    }
                    return Vector3.zero;
            }
        }
        /// <summary>
        /// Assign each ship a target on the opposing side.
        /// Called once after warp-in completes, before weapon fire starts.
        /// </summary>
        private void AssignTargetsToAllShips()
        {
            Debug.Log("🎯 Assigning targets to all ships...");

            int assigned = 0;

            // Side One ships target Side Two ships
            List<ShipController> side2Alive = CombatData.SideTwoShipCons
                .Where(s => s != null && !s.ShipData.Distroyed)
                .ToList();

            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship == null || ship.ShipData.Distroyed) continue;

                if (side2Alive.Count == 0)
                {
                    Debug.LogWarning($"⚠️ No living Side 2 targets for {ship.ShipData.ShipName}");
                    continue;
                }

                // Assign closest living enemy as target
                ShipController target = side2Alive
                    .OrderBy(t => Vector3.Distance(ship.transform.position, t.transform.position))
                    .First();

                ship.ShipData.TargetThisShipController = target;
                Debug.Log($"  ✅ Side1 {ship.ShipData.ShipName} → targets {target.ShipData.ShipName}");
                assigned++;
            }

            // Side Two ships target Side One ships
            List<ShipController> side1Alive = CombatData.SideOneShipCons
                .Where(s => s != null && !s.ShipData.Distroyed)
                .ToList();

            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship == null || ship.ShipData.Distroyed) continue;

                if (side1Alive.Count == 0)
                {
                    Debug.LogWarning($"⚠️ No living Side 1 targets for {ship.ShipData.ShipName}");
                    continue;
                }

                // Assign closest living enemy as target
                ShipController target = side1Alive
                    .OrderBy(t => Vector3.Distance(ship.transform.position, t.transform.position))
                    .First();

                ship.ShipData.TargetThisShipController = target;
                Debug.Log($"  ✅ Side2 {ship.ShipData.ShipName} → targets {target.ShipData.ShipName}");
                assigned++;
            }

            Debug.Log($"🎯 Target assignment complete: {assigned} ships assigned targets");
        }

        /// <summary>
        /// Reassign a new living target to a ship when its current target is destroyed.
        /// </summary>
        public void ReassignTarget(ShipController ship)
        {
            bool isSideOne = CombatData.SideOneShipCons.Contains(ship);

            List<ShipController> enemies = isSideOne
                ? CombatData.SideTwoShipCons
                : CombatData.SideOneShipCons;

            ShipController newTarget = enemies
                .Where(s => s != null && !s.ShipData.Distroyed)
                .OrderBy(t => Vector3.Distance(ship.transform.position, t.transform.position))
                .FirstOrDefault();

            ship.ShipData.TargetThisShipController = newTarget;

            if (newTarget != null)
                Debug.Log($"  🎯 Retargeted {ship.ShipData.ShipName} → {newTarget.ShipData.ShipName}");
        }

        /// <summary>
        /// Calculate flanking position to get line of sight on transports.
        /// Swings wide outside the main combat area.
        /// </summary>
        private Vector3 GetFlankingPositionToTransports(ShipController ship, bool isSideOne)
        {
            var enemyShips = isSideOne ? CombatData.SideTwoShipCons : CombatData.SideOneShipCons;
            var enemyTransports = enemyShips.Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType == ShipType.Transport).ToList();

            if (enemyTransports.Count == 0) return Vector3.zero;

            ShipController closestTransport = enemyTransports.OrderBy(t => Vector3.Distance(ship.transform.position, t.transform.position)).FirstOrDefault();
            if (closestTransport == null) return Vector3.zero;

            // Use StateMachine to track flank direction and path
            var stateMachine = ship.GetComponent<CombatOrderStateMachine>();
            float flankZ = 0;
            if (stateMachine != null)
            {
                // If not assigned a flank side, pick one based on current Z
                if (ship.transform.position.z > 0) flankZ = 300f; // Wide Left
                else flankZ = -300f; // Wide Right
            }

            // Pathing: 1. Move to wide waypoint. 2. Dive for transport.
            float currentX = ship.transform.position.x;
            float targetX = closestTransport.transform.position.x;

            // If we haven't reached the "wide" Z yet, prioritize moving out
            if (Mathf.Abs(ship.transform.position.z) < 250f)
            {
                return new Vector3(currentX, 0, flankZ);
            }

            return closestTransport.transform.position;
        }

        /// <summary>
        /// Get formation position: Ships form a wall in YZ plane.
        /// Combat ships in front, Transports behind.
        /// </summary>
        private Vector3 GetFormationPosition(ShipController ship, bool isSideOne)
        {
            var stateMachine = ship.GetComponent<CombatOrderStateMachine>();
            if (stateMachine == null) return ship.transform.position;

            bool isTransport = ship.ShipData.ShipType == ShipType.Transport;
            float sideSign = isSideOne ? 1 : -1;
            float formationX = isSideOne ? SIDE1_COMBAT_END_X : SIDE2_COMBAT_END_X;

            if (isTransport)
            {
                // Transports stay 100 units behind the wall
                formationX -= sideSign * 100f;
            }

            // Simple grid based on slot
            int slot = stateMachine.formationSlot;
            if (slot == -1)
            {
                stateMachine.formationSlot = Random.Range(0, 25);
                slot = stateMachine.formationSlot;
            }

            int row = slot / 5;
            int col = slot % 5;
            float spacing = 40f;
            Vector3 basePos = new Vector3(formationX, (row - 2) * spacing, (col - 2) * spacing);

            // ✅ Dynamic Blocking: If a combat ship, check if it can block a shot to a transport
            if (!isTransport)
            {
                Vector3 blockingPos = FindInterceptPosition(ship, isSideOne);
                if (blockingPos != Vector3.zero) return blockingPos;
            }

            return basePos;
        }

        private Vector3 FindInterceptPosition(ShipController ship, bool isSideOne)
        {
            var friendlyShips = isSideOne ? CombatData.SideOneShipCons : CombatData.SideTwoShipCons;
            var enemyShips = isSideOne ? CombatData.SideTwoShipCons : CombatData.SideOneShipCons;

            var transports = friendlyShips.Where(s => s != null && !s.ShipData.Distroyed && s.ShipData.ShipType == ShipType.Transport).ToList();
            if (transports.Count == 0) return Vector3.zero;

            foreach (var enemy in enemyShips)
            {
                if (enemy == null || enemy.ShipData.Distroyed) continue;
                if (enemy.ShipData.TargetThisShipController == null) continue;

                // If enemy is targeting one of our transports
                if (transports.Contains(enemy.ShipData.TargetThisShipController))
                {
                    ShipController targetTransport = enemy.ShipData.TargetThisShipController;
                    Vector3 lineStart = enemy.transform.position;
                    Vector3 lineEnd = targetTransport.transform.position;

                    // Closest point on the threat line to this ship
                    Vector3 lineDir = (lineEnd - lineStart).normalized;
                    float projection = Vector3.Dot(ship.transform.position - lineStart, lineDir);
                    projection = Mathf.Clamp(projection, 0, Vector3.Distance(lineStart, lineEnd));
                    Vector3 interceptPoint = lineStart + lineDir * projection;

                    // If we are close enough to intercept, move there
                    if (Vector3.Distance(ship.transform.position, interceptPoint) < 100f)
                    {
                        return interceptPoint;
                    }
                }
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Get retreat position (away from enemy)
        /// </summary>
        private Vector3 GetRetreatPosition(ShipController ship, bool isSideOne)
        {
            // Retreat toward own side's starting position
            float retreatX = isSideOne ? SIDE1_COMBAT_START_X : SIDE2_COMBAT_START_X;
            return new Vector3(retreatX, ship.transform.position.y, ship.transform.position.z);
        }
        /// <summary>
        /// Create health bars for all ships AFTER warp animation completes
        /// </summary>
        public void CreateHealthBarsForAllShips()
        {
            Debug.Log("🏥 Creating health bars for all ships...");

            int healthbarCount = 0;

            // Create health bars for Side One ships
            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    CreateHealthBarForShip(ship, -1); // -1 for side one (left side)
                    healthbarCount++;
                }
            }

            // Create health bars for Side Two ships
            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    CreateHealthBarForShip(ship, 1); // 1 for side two (right side)
                    healthbarCount++;
                }
            }

            Debug.Log($"✅ Created {healthbarCount} health bars");
        }

        /// <summary>
        /// Create a health bar for a single ship
        /// </summary>
        private void CreateHealthBarForShip(ShipController ship, int side1negSide2pos)
        {
            if (ship == null || CombatManager.Instance == null || CombatManager.Instance.HealthbarPrefab == null)
            {
                Debug.LogWarning($"Cannot create health bar - missing ship or prefab");
                return;
            }

            GameObject healthbarGO = Instantiate(CombatManager.Instance.HealthbarPrefab);
            healthbarGO.SetActive(true);

            // Parent directly to ship (world-space UI)
            healthbarGO.transform.SetParent(ship.transform, false);
            healthbarGO.transform.localPosition = new Vector3(5 * side1negSide2pos, -3f, 0);
            healthbarGO.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            healthbarGO.transform.localRotation = Quaternion.Euler(0, -90 * side1negSide2pos, 0);

            // Ensure health bar Canvas is on World Space
            Canvas healthbarCanvas = healthbarGO.GetComponent<Canvas>();
            if (healthbarCanvas == null)
            {
                healthbarCanvas = healthbarGO.AddComponent<Canvas>();
            }

            healthbarCanvas.renderMode = RenderMode.WorldSpace;
            healthbarCanvas.worldCamera = ShipCombatCameraController.Instance?.GetComponentInChildren<Camera>();

            // Add CanvasScaler for proper sizing
            if (!healthbarGO.TryGetComponent<CanvasScaler>(out var canvasScaler))
            {
                canvasScaler = healthbarGO.AddComponent<CanvasScaler>();
            }
            canvasScaler.dynamicPixelsPerUnit = 10;

            // Set health bar layer to Default (NOT UI layer for world-space)
            healthbarGO.layer = LayerMask.NameToLayer("Default");
            SetLayerRecursively(healthbarGO, LayerMask.NameToLayer("Default"));

            // Set up health bar images
            Image[] healthbarImages = healthbarGO.GetComponentsInChildren<Image>();
            foreach (var img in healthbarImages)
            {
                if (img.gameObject.name == "HealthFill")
                {
                    ship.HealthFillImage = img;
                    ship.HealthFillImage.fillAmount = 1f;
                    ship.HealthFillImage.color = Color.green;
                }
                else if (img.gameObject.name == "HealthBackground")
                {
                    ship.HealthBackgroundImage = img;
                    ship.HealthBackgroundImage.fillAmount = 1f;
                    ship.HealthBackgroundImage.color = Color.red;
                }
            }

            // Add to tracking list
            HealthbarRenderers.Add(healthbarGO);

            // Add billboard component to face camera
            var billboard = healthbarGO.GetComponent<BillboardCameraCombat>();
            if (billboard == null)
            {
                billboard = healthbarGO.AddComponent<BillboardCameraCombat>();
            }
        }
        /// <summary>
        /// Start weapon firing for all ships with balanced delays
        /// Ships don't need to be active - coroutine runs on CombatController
        /// </summary>
        private IEnumerator StartAllShipWeaponFire()
        {
            Debug.Log("🔫 Starting weapon fire for all ships with balanced timing...");

            yield return new WaitForSecondsRealtime(0.5f);

            int shipCount = 0;

            List<float> side1Delays = new List<float>();
            List<float> side2Delays = new List<float>();

            int maxShips = Mathf.Max(
                CombatData.SideOneShipCons.Count,
                CombatData.SideTwoShipCons.Count
            );

            for (int i = 0; i < maxShips; i++)
            {
                float delay = UnityEngine.Random.Range(0.1f, 0.5f);
                side1Delays.Add(delay);
                side2Delays.Add(delay);
            }

            side1Delays = side1Delays.OrderBy(x => UnityEngine.Random.value).ToList();
            side2Delays = side2Delays.OrderBy(x => UnityEngine.Random.value).ToList();

            // Start firing for Side One ships - run on CombatController, not ship
            int index1 = 0;
            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    float delay = index1 < side1Delays.Count ? side1Delays[index1] : 0f;

                    // ✅ Run on CombatController - doesn't matter if ship is active
                    StartCoroutine(ShipFireLoopProxy(ship, delay));

                    Debug.Log($"  Side 1: {ship.ShipData.ShipName} starting in {delay:F2}s");
                    shipCount++;
                    index1++;
                }
            }

            // Start firing for Side Two ships - run on CombatController, not ship
            int index2 = 0;
            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    float delay = index2 < side2Delays.Count ? side2Delays[index2] : 0f;

                    // ✅ Run on CombatController - doesn't matter if ship is active
                    StartCoroutine(ShipFireLoopProxy(ship, delay));

                    Debug.Log($"  Side 2: {ship.ShipData.ShipName} starting in {delay:F2}s");
                    shipCount++;
                    index2++;
                }
            }

            Debug.Log($"✅ Weapon fire started for {shipCount} ships");
            yield return null;
        }

        /// <summary>
        /// Proxy coroutine that runs on CombatController and manages ship firing
        /// This works even if the ship GameObject is inactive
        /// </summary>
        private IEnumerator ShipFireLoopProxy(ShipController ship, float initialDelay)
        {
            if (ship == null) yield break;

            // Wait for initial delay
            if (initialDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(initialDelay);
            }

            // Call the ship's fire loop - this runs on CombatController, not the ship
            yield return StartCoroutine(ship.ShipFireLoop(0f));
        }

        /// <summary>
        /// Initialize ship groups for Engage order
        /// Groups ships by similar speed into 2-3 ship groups
        /// </summary>
        public void InitializeShipGroupsForEngage()
        {
            if (groupsInitialized) return;

            Debug.Log("=== Initializing Ship Groups for Engage Order ===");

            // Side One Groups
            if (CombatData.SideOneOrder == CombatOrders.Engage)
            {
                sideOneGroups = CreateShipGroups(CombatData.SideOneShipCons);
                AssignGroupsToShips(sideOneGroups);
                Debug.Log($"  Side 1: Created {sideOneGroups.Count} groups");
            }

            // Side Two Groups
            if (CombatData.SideTwoOrder == CombatOrders.Engage)
            {
                sideTwoGroups = CreateShipGroups(CombatData.SideTwoShipCons);
                AssignGroupsToShips(sideTwoGroups);
                Debug.Log($"  Side 2: Created {sideTwoGroups.Count} groups");
            }

            groupsInitialized = true;
        }

        /// <summary>
        /// Create groups of 2-3 ships with similar speeds
        /// </summary>
        private List<ShipGroup> CreateShipGroups(List<ShipController> ships)
        {
            List<ShipGroup> groups = new List<ShipGroup>();

            // Filter out transports and destroyed ships
            var combatShips = ships.Where(s =>
                s != null &&
                !s.ShipData.Distroyed &&
                s.ShipData.ShipType != ShipType.Transport
            ).ToList();

            if (combatShips.Count == 0) return groups;

            // Sort by speed (maxWarpFactor)
            combatShips = combatShips.OrderBy(s => s.ShipData.maxWarpFactor).ToList();

            // Create groups of 2-3 ships
            for (int i = 0; i < combatShips.Count; i += 3)
            {
                ShipGroup group = new ShipGroup();

                // Add 2-3 ships to group
                int groupSize = Mathf.Min(3, combatShips.Count - i);
                for (int j = 0; j < groupSize; j++)
                {
                    if (i + j < combatShips.Count)
                    {
                        group.ships.Add(combatShips[i + j]);
                    }
                }

                // Calculate group speed (slowest ship)
                group.RecalculateGroupSpeed();

                groups.Add(group);

                Debug.Log($"    Group {groups.Count}: {group.ships.Count} ships, speed={group.groupSpeed:F1}");
            }

            return groups;
        }

        /// <summary>
        /// Assign groups to ships (set their CombatOrderStateMachine.assignedGroup)
        /// </summary>
        private void AssignGroupsToShips(List<ShipGroup> groups)
        {
            int groupId = 0;
            foreach (var group in groups)
            {
                foreach (var ship in group.ships)
                {
                    var orderStateMachine = ship.GetComponent<CombatOrderStateMachine>();
                    if (orderStateMachine != null)
                    {
                        orderStateMachine.assignedGroup = group;
                        orderStateMachine.groupId = groupId;
                    }
                }
                groupId++;
            }
        }

        /// <summary>
        /// Update group targets for Engage order
        /// Each group focuses fire on a common target
        /// </summary>
        private void UpdateGroupTargets()
        {
            // Update Side One groups
            foreach (var group in sideOneGroups)
            {
                if (group.ships.Count == 0) continue;

                // Find common target (closest enemy to group center)
                Vector3 groupCenter = Vector3.zero;
                int validShips = 0;

                foreach (var ship in group.ships)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        groupCenter += ship.transform.position;
                        validShips++;
                    }
                }

                if (validShips > 0)
                {
                    groupCenter /= validShips;

                    // Find closest enemy to group center
                    ShipController closestEnemy = FindClosestEnemyToPosition(groupCenter, CombatData.SideTwoShipCons);
                    group.commonTarget = closestEnemy;

                    // Assign to all ships in group
                    foreach (var ship in group.ships)
                    {
                        if (ship != null && !ship.ShipData.Distroyed)
                        {
                            ship.ShipData.TargetThisShipController = closestEnemy;
                        }
                    }
                }
            }

            // Update Side Two groups
            foreach (var group in sideTwoGroups)
            {
                if (group.ships.Count == 0) continue;

                Vector3 groupCenter = Vector3.zero;
                int validShips = 0;

                foreach (var ship in group.ships)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        groupCenter += ship.transform.position;
                        validShips++;
                    }
                }

                if (validShips > 0)
                {
                    groupCenter /= validShips;

                    ShipController closestEnemy = FindClosestEnemyToPosition(groupCenter, CombatData.SideOneShipCons);
                    group.commonTarget = closestEnemy;

                    foreach (var ship in group.ships)
                    {
                        if (ship != null && !ship.ShipData.Distroyed)
                        {
                            ship.ShipData.TargetThisShipController = closestEnemy;
                        }
                    }
                }
            }
        }

        private ShipController FindClosestEnemyToPosition(Vector3 position, List<ShipController> enemies)
        {
            ShipController closest = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.ShipData.Distroyed || enemy.ShipData.ShipType == ShipType.Transport)
                    continue;

                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = enemy;
                }
            }

            return closest;
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
                        // Ensure state machine is aware
                        var stateMachine = ship.GetComponent<CombatOrderStateMachine>();
                        if (stateMachine != null) stateMachine.CurrentOrder = order;
                    }
                }
            }

            // Log order summary
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

            // Build list of available orders
            var availableOrders = new List<CombatOrders>
            {
                CombatOrders.Engage,
                CombatOrders.Formation,
                CombatOrders.Rush,
                CombatOrders.Retreat
            };

            // Only add AttackTransports if enemy has transports
            bool enemyHasTransports = CombatOrderHelper.HasTransports(CombatData, side == 1 ? 2 : 1);
            if (enemyHasTransports)
            {
                availableOrders.Add(CombatOrders.AttackTransports);
                Debug.Log($"🎯 Enemy has transports - AttackTransports order available for AI");
            }

            // Pick random order
            CombatOrders randomOrder = availableOrders[UnityEngine.Random.Range(0, availableOrders.Count)];

            Debug.Log($"🤖 AI ({aiCivEnum}) selected order: {randomOrder}");

            SetShipOrders(randomOrder, aiCivEnum);
        }

        /// <summary>
        /// Stop all weapon fire
        /// </summary>
        private void StopAllWeaponFire()
        {
            // Nullify all targets to stop firing loops immediately
            foreach (var ship in CombatData.SideOneShipCons)
            {
                if (ship != null && ship.ShipData != null)
                {
                    ship.ShipData.TargetThisShipController = null;
                }
            }
            foreach (var ship in CombatData.SideTwoShipCons)
            {
                if (ship != null && ship.ShipData != null)
                {
                    ship.ShipData.TargetThisShipController = null;
                }
            }
            Debug.Log("🛑 All weapon fire stopped");
        }

        /// <summary>
        /// Show combat end sequence with delays
        /// </summary>
        private IEnumerator ShowCombatEndSequence(bool sideOneWon)
        {
            // PHASE 1: Stop all movement and weapon fire
            Debug.Log("Combat End Phase 1: Stopping movement and weapons");
            isMoving = false;

            // PHASE 2: Wait for last projectiles to hit
            yield return new WaitForSecondsRealtime(1f);

            // PHASE 3: Show the combat over panel
            Debug.Log("Combat End Phase 2: Showing victory panel");
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.ShowCombatOverPanel();

                // Determine winner
                CivEnum winner = sideOneWon ? CombatData.CivEnumSideOne : CombatData.CivEnumSideTwo;
                CivEnum loser = sideOneWon ? CombatData.CivEnumSideTwo : CombatData.CivEnumSideOne;

                Debug.Log($"🏆 Victory for: {winner}");
                Debug.Log($"💀 Defeated: {loser}");
            }

            // PHASE 4: Wait for player to view results
            yield return new WaitForSecondsRealtime(5f);

            // PHASE 5: Clean up and return to galaxy
            Debug.Log("Combat End Phase 3: Returning to galaxy");
            EndCombat();
        }

        public void OnReturnToGalaxyButtonClicked()
        {
            Debug.Log("Player clicked return to galaxy");
            EndCombat();
        }

        /// <summary>
        /// Cleanup orphaned torpedoes/beams from previous combat
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
        /// Disable stencil buffer operations on ship renderers
        /// </summary>
        private void DisableStencilOnShipRenderers(GameObject shipModel)
        {
            if (shipModel == null) return;

            Renderer[] renderers = shipModel.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.SetInt("_StencilComp", 0);
                    renderer.material.SetInt("_Stencil", 0);
                    renderer.material.SetInt("_StencilOp", 0);
                    renderer.material.SetInt("_StencilWriteMask", 0);
                    renderer.material.SetInt("_StencilReadMask", 0);
                }
            }
        }

        /// <summary>
        /// Set layer of GameObject and all children recursively
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
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
            foreach (var hb in HealthbarRenderers)
            {
                if (hb != null) Destroy(hb);
            }
            HealthbarRenderers.Clear();

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

                    // Reset position/rotation relative to fleet
                    ship.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                    // Reset scale
                    ship.transform.localScale = Vector3.one;

                    // Deactivate ship in galaxy
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
