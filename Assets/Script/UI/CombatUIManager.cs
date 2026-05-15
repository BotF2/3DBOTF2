using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMP = TMPro;

namespace BOTF3D.UI
{
    /// <summary>
    /// Persistent manager that controls the CombatUICanvas in CombatScene
    /// Lives in GalaxyScene and updates for each new combat
    /// </summary>
    public class CombatUIManager : MonoBehaviour
    {
        public static CombatUIManager Instance { get; private set; }

        // ✅ References to current combat
        public CombatController CurrentCombatController;
        private GameObject currentCombatUICanvas;
        private GameObject currentCombat3DCanvas;
        private GameObject currentGameOverCanvas;

        // ✅ Cached UI references (found dynamically each combat)
        private GameObject panelCombatMenu;
        private GameObject panelShipCombat;
        private GameObject panelCombatOver;
        private TMP.TextMeshProUGUI timerText;
        private Toggle engage, rush, retreat, formation, AttackTransports;

        // ✅ Combat state
        private float remainingTime = 10f;
        private bool isTimerRunning = false;
        private CombatOrders currentOrder = CombatOrders.Engage;
        public CivEnum CivEnumLocalPlayer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate CombatUIManager - destroying duplicate");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("✅ CombatUIManager initialized (persistent)");
        }

        private void Update()
        {
            if (isTimerRunning)
            {
                // ✅ Use unscaledDeltaTime instead of deltaTime
                remainingTime -= Time.unscaledDeltaTime;

                if (remainingTime > 0f)
                {
                    if (timerText != null)
                    {
                        timerText.text = Mathf.FloorToInt(remainingTime).ToString("00");
                    }
                }
                else
                {
                    isTimerRunning = false;
                    remainingTime = 0f;
                    if (timerText != null) timerText.text = "00";
                    EnterShipCombatPhase();
                }
            }
            // ✅ Camera rotation is now handled by ShipCombatCameraController itself
            // No need to duplicate the spacebar logic here
        }

        /// <summary>
        /// Called by CombatManager when a new combat starts for the local player
        /// CRITICAL: Must work in both Editor AND builds!
        /// </summary>
        public void SetupForCombat(CombatController combatController, GameObject combatUICanvas, GameObject combat3DCanvas, GameObject gameOverCanvas)
        {
            Debug.Log($"🎮 CombatUIManager: Setting up for combat {combatController.CombatID}");

            CurrentCombatController = combatController;
            currentCombatUICanvas = combatUICanvas;
            currentCombat3DCanvas = combat3DCanvas;
            currentGameOverCanvas = gameOverCanvas;
            CivEnumLocalPlayer = GameController.Instance.GameData.LocalPlayerCivEnum;

            // ✅ CRITICAL: Use coroutine to ensure scene is fully loaded (fixes build issues!)
            StartCoroutine(SetupCombatUIAfterSceneLoad());
        }

        /// <summary>
        /// Coroutine to setup combat UI after CombatScene fully loads
        /// Waits 2 frames per copilot instructions for ownership normalization
        /// </summary>
        private IEnumerator SetupCombatUIAfterSceneLoad()
        {
            Debug.Log("SetupCombatUIAfterSceneLoad: Waiting for scene to stabilize...");

            // ✅ Wait two frames (from copilot-instructions.md)
            yield return null;
            yield return null;

            Debug.Log("SetupCombatUIAfterSceneLoad: Scene stabilized - initializing UI...");

            // ✅ CRITICAL: Ensure EventSystem exists in CombatScene
            EnsureEventSystemExists();

            // ✅ CRITICAL: Wait for ShipCombatCameraController to initialize
            yield return WaitForCombatCameraReady();

            // ✅ CRITICAL: Configure all canvases for builds
            ConfigureCombatCanvases();

            // ✅ Find and setup all UI elements
            FindAndSetupUI();

            // ✅ Force Canvas rebuild (critical for builds!)
            ForceCanvasRebuild();

            // ✅ Start timer
            remainingTime = 10f;
            isTimerRunning = true;

            Debug.Log("✅ Combat UI setup complete");
        }

        /// <summary>
        /// Wait for ShipCombatCameraController to be ready (up to 5 seconds)
        /// </summary>
        private IEnumerator WaitForCombatCameraReady()
        {
            float timeout = 5f;
            float elapsed = 0f;

            Debug.Log("  Waiting for ShipCombatCameraController...");

            while (ShipCombatCameraController.Instance == null && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (ShipCombatCameraController.Instance != null)
            {
                Debug.Log($"  ✅ ShipCombatCameraController ready after {elapsed:F2}s");
            }
            else
            {
                Debug.LogError($"  ❌ ShipCombatCameraController NOT FOUND after {timeout}s timeout!");
                Debug.LogError("     ACTION REQUIRED: Ensure CombatScene has a GameObject with ShipCombatCameraController component");
            }
        }

        /// <summary>
        /// Called when timer ends or button clicked
        /// </summary>
        private void EnterShipCombatPhase()
        {
            isTimerRunning = false;

            if (CurrentCombatController != null)
            {
                // ✅ Hide combat menu, show 3D combat view
                if (panelCombatMenu != null)
                {
                    panelCombatMenu.SetActive(false);
                    Debug.Log("✅ Combat menu closed");
                }

                if (panelShipCombat != null)
                {
                    panelShipCombat.SetActive(true);
                    Debug.Log("✅ Ship combat panel activated");
                }

                // Apply current order
                CurrentCombatController.SetShipOrders(currentOrder, CivEnumLocalPlayer);

                // ✅ Give AI a random order for the other side
                CivEnum aiCivEnum = (CivEnumLocalPlayer == CurrentCombatController.CombatData.CivEnumSideOne)
                    ? CurrentCombatController.CombatData.CivEnumSideTwo
                    : CurrentCombatController.CombatData.CivEnumSideOne;

                // Generate random combat order for AI
                System.Array combatOrderValues = System.Enum.GetValues(typeof(CombatOrders));
                CombatOrders randomAIOrder = (CombatOrders)combatOrderValues.GetValue(Random.Range(0, combatOrderValues.Length));
                CurrentCombatController.SetShipOrders(randomAIOrder, aiCivEnum);

                Debug.Log($"✅ Combat orders set - Player: {currentOrder}, AI: {randomAIOrder}");

                // ✅ NOW trigger warp-in animation and wait for it to complete before starting combat movement
                StartCoroutine(StartCombatSequence());
            }
            else
            {
                Debug.LogError("❌ Cannot start combat - controller is null!");
            }
        }

        /// <summary>
        /// ✅ NEW: Sequence that triggers warp-in animation, waits for completion, then starts combat movement
        /// </summary>
        private IEnumerator StartCombatSequence()
        {
            Debug.Log("🌀 Starting combat sequence...");

            if (CurrentCombatController == null)
            {
                Debug.LogError("❌ Cannot start combat sequence - CurrentCombatController is null!");
                yield break;
            }

            // ✅ STEP 1: Verify camera is ready
            if (ShipCombatCameraController.Instance == null)
            {
                Debug.LogError("❌ Cannot start combat - ShipCombatCameraController not found!");
                yield break;
            }

            // ✅ STEP 1.5: CRITICAL - Set animator starting positions and ship rotations BEFORE animation starts
            SetupAnimatorsForWarpIn();

            // ✅ STEP 2: Set warping state
            CurrentCombatController.WarpingIn = true;
            CurrentCombatController.WarpingAnimationOver = false;
            ShipCombatCameraController.Instance.SetWarpingIn(true);
            Debug.Log("  ✅ Set WarpingIn = true");

            // ✅ STEP 3: Set camera targets to all ships
            List<GameObject> allShips = new List<GameObject>();
            foreach (var ship in CurrentCombatController.CombatData.SideOneShipCons)
            {
                if (ship != null && ship.gameObject != null)
                {
                    allShips.Add(ship.gameObject);
                }
            }
            foreach (var ship in CurrentCombatController.CombatData.SideTwoShipCons)
            {
                if (ship != null && ship.gameObject != null)
                {
                    allShips.Add(ship.gameObject);
                }
            }

            if (allShips.Count > 0)
            {
                ShipCombatCameraController.Instance.SetTargets(allShips.ToArray());
                Debug.Log($"  ✅ Set camera targets: {allShips.Count} ships");
            }

            // ✅ STEP 4: Start manual warp-in animation
            Debug.Log("  ✅ Starting manual warp-in animation via CombatController");
            yield return StartCoroutine(CurrentCombatController.AnimateWarpIn());

            // ✅ STEP 6: Update camera state
            CurrentCombatController.WarpingIn = false;
            ShipCombatCameraController.Instance.SetWarpingIn(false);
            ShipCombatCameraController.Instance.SetWarpingInOver(true);

            // ✅ STEP 7: CREATE HEALTH BARS NOW (after warp animation, before combat starts)
            Debug.Log("🏥 Creating health bars...");
            CurrentCombatController.CreateHealthBarsForAllShips();

            // ✅ STEP 8: Initialize ship groups for combat (targeting system)
            CurrentCombatController.InitializeShipGroupsForEngage();
            Debug.Log("  ✅ Ship groups initialized");

            // ✅ STEP 9: NOW start combat movement
            CurrentCombatController.BeginPhysicsLikeMovement();
            Debug.Log("  ✅ Ship movement started");

            // ✅ STEP 10: START WEAPON FIRING for all ships SIMULTANEOUSLY
            yield return StartAllShipWeaponFire();

            // ✅ STEP 11: Verify combat state
            Debug.Log($"📊 Combat State Check:");
            Debug.Log($"   isMoving: {CurrentCombatController.isMoving}");
            Debug.Log($"   WarpingAnimationOver: {CurrentCombatController.WarpingAnimationOver}");
            Debug.Log($"   Side 1 ships: {CurrentCombatController.CombatData.SideOneShipCons.Count}");
            Debug.Log($"   Side 2 ships: {CurrentCombatController.CombatData.SideTwoShipCons.Count}");
            Debug.Log($"   Health bars: {CurrentCombatController.HealthbarRenderers.Count}");

            Debug.Log("🎬 Combat sequence complete - ships moving and firing!");
        }

        /// <summary>
        /// ✅ NEW: Start weapon firing with balanced delays for both sides
        /// </summary>
        private IEnumerator StartAllShipWeaponFire()
        {
            Debug.Log("🔫 Starting weapon fire for all ships with balanced timing...");

            // ✅ Wait a brief moment for ships to be fully positioned
            yield return new WaitForSecondsRealtime(0.5f);

            int shipCount = 0;

            // ✅ Generate matched delay pairs so both sides have equal timing distribution
            List<float> side1Delays = new List<float>();
            List<float> side2Delays = new List<float>();

            int maxShips = Mathf.Max(
                CurrentCombatController.CombatData.SideOneShipCons.Count,
                CurrentCombatController.CombatData.SideTwoShipCons.Count
            );

            // Generate random delays, then assign the SAME delays to both sides
            for (int i = 0; i < maxShips; i++)
            {
                float delay = UnityEngine.Random.Range(0.1f, 0.5f); // Shorter range for faster combat start
                side1Delays.Add(delay);
                side2Delays.Add(delay); // ✅ Same delay for both sides
            }

            // Shuffle each list independently so ships don't fire in perfect sync
            // but the DISTRIBUTION of delays is the same
            side1Delays = side1Delays.OrderBy(x => UnityEngine.Random.value).ToList();
            side2Delays = side2Delays.OrderBy(x => UnityEngine.Random.value).ToList();

            // Start firing for Side One ships
            int index1 = 0;
            foreach (var ship in CurrentCombatController.CombatData.SideOneShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    float delay = index1 < side1Delays.Count ? side1Delays[index1] : 0f;
                    ship.StartCoroutine(ship.ShipFireLoop(delay));
                    Debug.Log($"  Side 1: {ship.ShipData.ShipName} starting in {delay:F2}s");
                    shipCount++;
                    index1++;
                }
            }

            // Start firing for Side Two ships
            int index2 = 0;
            foreach (var ship in CurrentCombatController.CombatData.SideTwoShipCons)
            {
                if (ship != null && !ship.ShipData.Distroyed)
                {
                    float delay = index2 < side2Delays.Count ? side2Delays[index2] : 0f;
                    ship.StartCoroutine(ship.ShipFireLoop(delay));
                    Debug.Log($"  Side 2: {ship.ShipData.ShipName} starting in {delay:F2}s");
                    shipCount++;
                    index2++;
                }
            }

            Debug.Log($"✅ Weapon fire started for {shipCount} ships with BALANCED timing");
            yield return null;
        }

        /// <summary>
        /// Ensures the persistent EventSystem is active (from DontDestroyOnLoad)
        /// Destroys any EventSystem found in CombatScene to prevent conflicts
        /// </summary>
        private void EnsureEventSystemExists()
        {
            // ✅ Find the persistent EventSystem (should be in DontDestroyOnLoad)
            UnityEngine.EventSystems.EventSystem[] allEventSystems =
                FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);

            UnityEngine.EventSystems.EventSystem persistentEventSystem = null;
            List<UnityEngine.EventSystems.EventSystem> sceneEventSystems = new List<UnityEngine.EventSystems.EventSystem>();

            foreach (var es in allEventSystems)
            {
                // Check if this EventSystem is in DontDestroyOnLoad
                if (es.gameObject.scene.name == "DontDestroyOnLoad" || es.gameObject.scene.buildIndex == -1)
                {
                    persistentEventSystem = es;
                    Debug.Log($"✅ Found persistent EventSystem: '{es.gameObject.name}' in DontDestroyOnLoad");
                }
                else
                {
                    sceneEventSystems.Add(es);
                    Debug.Log($"⚠️ Found scene EventSystem: '{es.gameObject.name}' in scene '{es.gameObject.scene.name}'");
                }
            }

            // ✅ If no persistent EventSystem exists, create one
            if (persistentEventSystem == null)
            {
                Debug.LogWarning("⚠️ No persistent EventSystem found - creating one!");

                GameObject esGO = new GameObject("EventSystem_Persistent");
                persistentEventSystem = esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                DontDestroyOnLoad(esGO);

                Debug.Log("✅ Created persistent EventSystem in DontDestroyOnLoad");
            }

            // ✅ Ensure persistent EventSystem is enabled
            if (!persistentEventSystem.enabled)
            {
                persistentEventSystem.enabled = true;
                Debug.Log("✅ Enabled persistent EventSystem");
            }

            // ✅ CRITICAL: Destroy any EventSystems found in loaded scenes (prevents conflicts)
            foreach (var sceneES in sceneEventSystems)
            {
                Debug.LogWarning($"🗑️ Destroying duplicate EventSystem '{sceneES.gameObject.name}' from scene to prevent conflicts");
                Destroy(sceneES.gameObject);
            }

            // ✅ Set as current EventSystem
            UnityEngine.EventSystems.EventSystem.current = persistentEventSystem;

            Debug.Log($"✅ EventSystem configured: Active='{persistentEventSystem.gameObject.name}', Scene=DontDestroyOnLoad");
        }

        /// <summary>
        /// Configures all combat canvases for reliable rendering in builds
        /// </summary>
        private void ConfigureCombatCanvases()
        {
            Debug.Log("ConfigureCombatCanvases: Starting...");

            // ✅ Configure CombatUICanvas (2D UI overlay)
            if (currentCombatUICanvas != null)
            {
                Canvas canvas = currentCombatUICanvas.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = currentCombatUICanvas.AddComponent<Canvas>();
                    Debug.Log("  Added Canvas component to CombatUICanvas");
                }

                // ✅ Use Overlay mode for most reliable rendering in builds
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Ensure it's on top

                // ✅ Ensure GraphicRaycaster exists
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("  Added GraphicRaycaster to CombatUICanvas");
                }

                // ✅ Ensure CanvasScaler exists for consistent UI sizing
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                }
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvas.enabled = true;
                currentCombatUICanvas.SetActive(true);

                Debug.Log($"  ✅ CombatUICanvas configured: Mode={canvas.renderMode}, Order={canvas.sortingOrder}, Enabled={canvas.enabled}");
            }
            else
            {
                Debug.LogError("  ❌ currentCombatUICanvas is NULL!");
            }

            // ✅ Configure Combat3DCanvas (world-space 3D UI)
            if (currentCombat3DCanvas != null)
            {
                Canvas canvas3D = currentCombat3DCanvas.GetComponent<Canvas>();
                if (canvas3D == null)
                {
                    canvas3D = currentCombat3DCanvas.AddComponent<Canvas>();
                    Debug.Log("  Added Canvas component to Combat3DCanvas");
                }

                // ✅ Use World Space for 3D combat elements
                canvas3D.renderMode = RenderMode.WorldSpace;

                // ✅ Find combat camera - try multiple methods
                Camera combatCamera = null;

                if (ShipCombatCameraController.Instance != null)
                {
                    // Try getting from same GameObject first
                    combatCamera = ShipCombatCameraController.Instance.GetComponent<Camera>();

                    // If not found, try children
                    if (combatCamera == null)
                    {
                        combatCamera = ShipCombatCameraController.Instance.GetComponentInChildren<Camera>();
                        if (combatCamera != null)
                        {
                            Debug.Log($"  Found camera in children: {combatCamera.name}");
                        }
                    }
                    else
                    {
                        Debug.Log($"  Found camera on same GameObject: {combatCamera.name}");
                    }
                }

                // ✅ Fallback: search for any Camera tagged "MainCamera" in CombatScene
                if (combatCamera == null)
                {
                    Debug.LogWarning("  Camera not found via ShipCombatCameraController, searching for MainCamera tag...");
                    combatCamera = Camera.main;

                    if (combatCamera != null)
                    {
                        Debug.Log($"  Found camera via MainCamera tag: {combatCamera.name}");
                    }
                }

                // ✅ Last resort: find any active camera
                if (combatCamera == null)
                {
                    Debug.LogWarning("  Still no camera found, searching all cameras...");
                    Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

                    foreach (var cam in allCameras)
                    {
                        Debug.Log($"    Found camera: {cam.name}, Active: {cam.gameObject.activeInHierarchy}");
                        if (cam.gameObject.activeInHierarchy)
                        {
                            combatCamera = cam;
                            Debug.Log($"  Using active camera: {combatCamera.name}");
                            break;
                        }
                    }
                }

                if (combatCamera != null)
                {
                    canvas3D.worldCamera = combatCamera;
                    Debug.Log($"  ✅ Set Combat3DCanvas camera to: {combatCamera.name}");
                }
                else
                {
                    Debug.LogError("  ❌ Combat camera not found - Combat3DCanvas may not render correctly");
                    Debug.LogError("     ACTION REQUIRED: Ensure CombatScene has an active Camera component");
                }

                var raycaster3D = canvas3D.GetComponent<GraphicRaycaster>();
                if (raycaster3D == null)
                {
                    raycaster3D = canvas3D.gameObject.AddComponent<GraphicRaycaster>();
                }

                canvas3D.enabled = true;
                currentCombat3DCanvas.SetActive(true);

                Debug.Log($"  ✅ Combat3DCanvas configured: Mode={canvas3D.renderMode}, Enabled={canvas3D.enabled}");
            }

            // ✅ Configure GameOverCanvas
            if (currentGameOverCanvas != null)
            {
                Canvas gameOverCanvas = currentGameOverCanvas.GetComponent<Canvas>();
                if (gameOverCanvas == null)
                {
                    gameOverCanvas = currentGameOverCanvas.AddComponent<Canvas>();
                }

                gameOverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                gameOverCanvas.sortingOrder = 200; // Above everything else

                var raycasterGameOver = gameOverCanvas.GetComponent<GraphicRaycaster>();
                if (raycasterGameOver == null)
                {
                    raycasterGameOver = gameOverCanvas.gameObject.AddComponent<GraphicRaycaster>();
                }

                gameOverCanvas.enabled = true;

                Debug.Log($"  ✅ GameOverCanvas configured: Mode={gameOverCanvas.renderMode}, Order={gameOverCanvas.sortingOrder}");
            }

            Debug.Log("ConfigureCombatCanvases: Complete");
        }

        /// <summary>
        /// Forces Unity to rebuild all canvases (critical for builds!)
        /// </summary>
        private void ForceCanvasRebuild()
        {
            Debug.Log("ForceCanvasRebuild: Forcing Canvas update...");

            // ✅ Force all canvases to rebuild
            Canvas.ForceUpdateCanvases();

            // ✅ Also ensure all Image components are enabled
            if (currentCombatUICanvas != null)
            {
                Image[] images = currentCombatUICanvas.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    img.enabled = true;
                }
                Debug.Log($"  Enabled {images.Length} Image components in CombatUICanvas");

                // ✅ Ensure all CanvasRenderers have alpha = 1
                CanvasRenderer[] renderers = currentCombatUICanvas.GetComponentsInChildren<CanvasRenderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.SetAlpha(1f);
                }
                Debug.Log($"  Set alpha on {renderers.Length} CanvasRenderers");
            }

            Debug.Log("ForceCanvasRebuild: Complete");
        }

        /// <summary>
        /// Find all UI elements in the CombatUICanvas
        /// Uses multiple search strategies for build reliability
        /// </summary>
        private void FindAndSetupUI()
        {
            if (currentCombatUICanvas == null)
            {
                Debug.LogError("❌ Cannot setup UI - CombatUICanvas is null!");
                return;
            }

            Debug.Log("FindAndSetupUI: Starting comprehensive UI search...");

            currentCombatUICanvas.SetActive(true);

            // ✅ Find panels with multiple strategies
            panelCombatMenu = FindUIElement(currentCombatUICanvas, "PanelCombat_Menu", "Combat_Menu", "CombatMenu");
            if (panelCombatMenu != null)
            {
                panelCombatMenu.SetActive(true);
                Debug.Log($"  ✅ Found and activated: {panelCombatMenu.name}");
            }
            else
            {
                Debug.LogError("  ❌ Could not find PanelCombat_Menu! Was the name changed?");
            }

            if (currentCombat3DCanvas != null)
            {
                panelShipCombat = FindUIElement(currentCombat3DCanvas, "PanelShipCombat", "ShipCombat", "Combat3D");
                if (panelShipCombat != null)
                {
                    panelShipCombat.SetActive(false); // Start hidden
                    Debug.Log($"  ✅ Found: {panelShipCombat.name}");
                }
            }

            if (currentGameOverCanvas != null)
            {
                panelCombatOver = FindUIElement(currentGameOverCanvas, "PanelCombatEnd", "CombatEnd", "GameOver", "CombatOver");
                if (panelCombatOver != null)
                {
                    panelCombatOver.SetActive(false); // Start hidden
                    Debug.Log($"  ✅ Found: {panelCombatOver.name}");
                }
            }

            // ✅ Find timer text with robust search
            timerText = FindComponentByName<TMP.TextMeshProUGUI>(currentCombatUICanvas, "Timer Text", "Timer", "TimerText");
            if (timerText != null)
            {
                Debug.Log($"  ✅ Found timer text: {timerText.name}");
            }
            else
            {
                Debug.LogWarning("  ⚠️ Timer text not found!");
            }

            // ✅ Find and setup toggles
            SetupToggles();

            // ✅ Find and setup buttons
            SetupButtons();

            Debug.Log("FindAndSetupUI: Complete");
        }

        /// <summary>
        /// Setup all combat order toggles
        /// </summary>
        private void SetupToggles()
        {
            if (currentCombatUICanvas == null) return;

            Toggle[] toggles = currentCombatUICanvas.GetComponentsInChildren<Toggle>(true);
            Debug.Log($"  Found {toggles.Length} toggles");

            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.RemoveAllListeners();

                // ✅ Enable the toggle GameObject
                toggle.gameObject.SetActive(true);
                toggle.interactable = true;

                switch (toggle.name)
                {
                    case "Toggle_ENGAGE":
                        engage = toggle;
                        engage.onValueChanged.AddListener(OnToggleENGAGE);
                        engage.isOn = true; // Default order
                        Debug.Log("    ✅ Wired Toggle_ENGAGE");
                        break;
                    case "Toggle_RUSH":
                        rush = toggle;
                        rush.isOn = false;
                        rush.onValueChanged.AddListener(OnToggleRUSH);
                        Debug.Log("    ✅ Wired Toggle_RUSH");
                        break;
                    case "Toggle_RETREAT":
                        retreat = toggle;
                        retreat.isOn = false;
                        retreat.onValueChanged.AddListener(OnToggleRETREAT);
                        Debug.Log("    ✅ Wired Toggle_RETREAT");
                        break;
                    case "Toggle_FORMATION":
                        formation = toggle;
                        formation.isOn = false;
                        formation.onValueChanged.AddListener(OnToggleFORMATION);
                        Debug.Log("    ✅ Wired Toggle_FORMATION");
                        break;
                    case "Toggle_TARGET_TRANSPORTS":
                        AttackTransports = toggle;
                        AttackTransports.isOn = false;
                        AttackTransports.onValueChanged.AddListener(OnToggleTARGET_TRANSPORTS);
                        Debug.Log("    ✅ Wired Toggle_TARGET_TRANSPORTS");
                        break;
                }
            }
        }

        /// <summary>
        /// Setup all combat buttons
        /// </summary>
        private void SetupButtons()
        {
            if (currentCombatUICanvas == null) return;

            Button[] buttons = currentCombatUICanvas.GetComponentsInChildren<Button>(true);
            Debug.Log($"  Found {buttons.Length} buttons");

            foreach (var button in buttons)
            {
                // ✅ Enable the button
                button.gameObject.SetActive(true);
                button.interactable = true;

                if (button.name == "ButtonEnterCombat" || button.name.Contains("Enter") || button.name.Contains("Start"))
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(EnterShipCombatPhase);
                    Debug.Log($"    ✅ Wired {button.name} to EnterShipCombatPhase");
                }
            }
        }

        /// <summary>
        /// Find a UI element with multiple name variants (for build robustness)
        /// </summary>
        private GameObject FindUIElement(GameObject root, params string[] possibleNames)
        {
            if (root == null) return null;

            foreach (string name in possibleNames)
            {
                // Strategy 1: Direct child search
                Transform found = root.transform.Find(name);
                if (found != null)
                {
                    Debug.Log($"    Found '{name}' via direct search");
                    return found.gameObject;
                }

                // Strategy 2: Deep recursive search
                found = FindInHierarchyRecursive(root.transform, name);
                if (found != null)
                {
                    Debug.Log($"    Found '{name}' via deep search");
                    return found.gameObject;
                }
            }

            Debug.LogWarning($"    Could not find UI element with names: {string.Join(", ", possibleNames)}");
            return null;
        }

        /// <summary>
        /// Find a component by GameObject name (with variants)
        /// </summary>
        private T FindComponentByName<T>(GameObject root, params string[] possibleNames) where T : Component
        {
            if (root == null) return null;

            T[] components = root.GetComponentsInChildren<T>(true);

            foreach (var component in components)
            {
                foreach (string name in possibleNames)
                {
                    if (component.name == name || component.name.Contains(name))
                    {
                        Debug.Log($"    Found component '{component.name}' (type: {typeof(T).Name})");
                        return component;
                    }
                }
            }

            Debug.LogWarning($"    Could not find {typeof(T).Name} with names: {string.Join(", ", possibleNames)}");
            return null;
        }

        /// <summary>
        /// Recursive search for GameObject by name
        /// </summary>
        private Transform FindInHierarchyRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindInHierarchyRecursive(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        // ✅ Toggle callbacks
        private void OnToggleENGAGE(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.Engage;
                Debug.Log("Order: Engage");
            }
        }

        private void OnToggleRUSH(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.Rush;
                Debug.Log("Order: Rush");
            }
        }

        private void OnToggleRETREAT(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.Retreat;
                Debug.Log("Order: Retreat");
            }
        }

        private void OnToggleFORMATION(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.Formation;
                Debug.Log("Order: Formation");
            }
        }

        private void OnToggleTARGET_TRANSPORTS(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.AttackTransports;
                Debug.Log("Order: Target Transports");
            }
        }

        /// <summary>
        /// Show combat over panel
        /// </summary>
        public void ShowCombatOverPanel()
        {
            // ✅ If panel wasn't found during setup, try to find it again
            if (panelCombatOver == null)
            {
                Debug.LogWarning("⚠️ Combat over panel is null - attempting to find it now...");

                // Try in GameOverCanvas first
                if (currentGameOverCanvas != null)
                {
                    panelCombatOver = FindUIElement(currentGameOverCanvas, "PanelCombatEnd", "CombatEnd", "GameOver", "CombatOver", "PanelCombatOver");
                }

                // Try in CombatUICanvas as backup
                if (panelCombatOver == null && currentCombatUICanvas != null)
                {
                    panelCombatOver = FindUIElement(currentCombatUICanvas, "PanelCombatEnd", "CombatEnd", "GameOver", "CombatOver", "PanelCombatOver");
                }

                // Last resort: global search
                if (panelCombatOver == null)
                {
                    Debug.LogWarning("  Searching entire scene for combat over panel...");

                    string[] possibleNames = { "PanelCombatEnd", "CombatEnd", "GameOver", "CombatOver", "PanelCombatOver" };

                    foreach (string name in possibleNames)
                    {
                        GameObject found = GameObject.Find(name);
                        if (found != null)
                        {
                            panelCombatOver = found;
                            Debug.Log($"  ✅ Found combat over panel by global search: '{found.name}'");
                            break;
                        }
                    }
                }
            }

            // ✅ Show panel if found
            if (panelCombatOver != null)
            {
                panelCombatOver.SetActive(true);
                Debug.Log("✅ Combat over panel shown");
            }
            else
            {
                Debug.LogError("❌ Cannot show combat over panel - not found in scene!");
                Debug.LogError("   ACTION REQUIRED: Add a GameObject named 'PanelCombatEnd' to your CombatScene");
                Debug.LogError("   It should be a child of GameOverCanvas or CombatUICanvas");

                // ✅ TEMPORARY WORKAROUND: Just end combat without showing panel
                Debug.LogWarning("   ⚠️ TEMPORARY: Ending combat without panel");
                if (CurrentCombatController != null)
                {
                    // Wait 3 seconds then return to galaxy
                    StartCoroutine(DelayedReturnToGalaxy(3f));
                }
            }
        }

        /// <summary>
        /// Temporary workaround to return to galaxy after delay when panel is missing
        /// </summary>
        private System.Collections.IEnumerator DelayedReturnToGalaxy(float delay)
        {
            Debug.Log($"  Waiting {delay} seconds before returning to galaxy...");
            yield return new WaitForSecondsRealtime(delay);

            if (CurrentCombatController != null)
            {
                CurrentCombatController.OnReturnToGalaxyButtonClicked();
            }
        }

        /// <summary>
        /// Clean up when combat ends
        /// </summary>
        public void CleanupCombat()
        {
            Debug.Log("🧹 CombatUIManager: Cleaning up combat UI references");

            CurrentCombatController = null;
            currentCombatUICanvas = null;
            currentCombat3DCanvas = null;
            currentGameOverCanvas = null;
            panelCombatMenu = null;
            panelShipCombat = null;
            panelCombatOver = null;
            timerText = null;
            engage = null;
            rush = null;
            retreat = null;
            formation = null;
            AttackTransports = null;
            isTimerRunning = false;
        }

        /// <summary>
        /// ✅ REVERTED: Trust animation clips and Unity scene for animator positions
        /// Only set ship rotations to face direction of travel
        /// </summary>
        public void SetupAnimatorsForWarpIn()
        {
            if (CurrentCombatController == null)
            {
                Debug.LogError("❌ Cannot setup animators - CurrentCombatController is null!");
                return;
            }

            Debug.Log("🔧 Setting ship rotations for warp-in (parent positions from Unity scene)...");

            // ✅ DEBUG: Log parent states from Unity scene
            Debug.Log("📊 PARENT POSITIONS FROM UNITY SCENE:");
            LogAnimatorState(CurrentCombatController.sideOneA1Parent, "S1A1");
            LogAnimatorState(CurrentCombatController.sideOneA2Parent, "S1A2");
            LogAnimatorState(CurrentCombatController.sideOneA3Parent, "S1A3");
            LogAnimatorState(CurrentCombatController.sideTwoA1Parent, "S2A1");
            LogAnimatorState(CurrentCombatController.sideTwoA2Parent, "S2A2");
            LogAnimatorState(CurrentCombatController.sideTwoA3Parent, "S2A3");

            int correctionCount = 0;

            // ✅ CRITICAL: Parents should be at (0,0,0) rotation so their local space = world space
            // Then ships need to be rotated to face the direction they'll move
            // Side 1 moves in +X direction, so rotate ships to face +X (Y=90°)
            // Side 2 moves in -X direction, so rotate ships to face -X (Y=-90°)
            Quaternion side1ShipRotation = Quaternion.Euler(0, 90, 0);  // Face +X
            Quaternion side2ShipRotation = Quaternion.Euler(0, -90, 0); // Face -X

            CorrectAnimatorShipPositions(CurrentCombatController.sideOneA1Parent, "S1A1", side1ShipRotation, ref correctionCount);
            CorrectAnimatorShipPositions(CurrentCombatController.sideOneA2Parent, "S1A2", side1ShipRotation, ref correctionCount);
            CorrectAnimatorShipPositions(CurrentCombatController.sideOneA3Parent, "S1A3", side1ShipRotation, ref correctionCount);

            CorrectAnimatorShipPositions(CurrentCombatController.sideTwoA1Parent, "S2A1", side2ShipRotation, ref correctionCount);
            CorrectAnimatorShipPositions(CurrentCombatController.sideTwoA2Parent, "S2A2", side2ShipRotation, ref correctionCount);
            CorrectAnimatorShipPositions(CurrentCombatController.sideTwoA3Parent, "S2A3", side2ShipRotation, ref correctionCount);

            Debug.Log($"✅ Setup complete: {correctionCount} ships rotated, parents use Unity scene positions/rotations");
        }

        /// <summary>
        /// Log parent GameObject position and rotation for debugging
        /// </summary>
        private void LogAnimatorState(GameObject parent, string name)
        {
            if (parent == null)
            {
                Debug.LogWarning($"  ⚠️ {name}: NULL");
                return;
            }

            Vector3 pos = parent.transform.position;
            Vector3 rot = parent.transform.eulerAngles;
            int childCount = parent.transform.childCount;

            Debug.Log($"  {name}: Pos=({pos.x:F0}, {pos.y:F0}, {pos.z:F0}), Rot=({rot.x:F0}, {rot.y:F0}, {rot.z:F0}), Ships={childCount}");
        }

        /// <summary>
        /// Corrects ship rotations under a single parent GameObject
        /// </summary>
        private void CorrectAnimatorShipPositions(GameObject parent, string parentName, Quaternion shipRotation, ref int correctionCount)
        {
            if (parent == null)
            {
                Debug.LogWarning($"  ⚠️ {parentName} parent is null - skipping");
                return;
            }

            int childCount = parent.transform.childCount;

            if (childCount == 0)
            {
                Debug.Log($"  {parentName}: No ships");
                return;
            }

            for (int i = 0; i < childCount; i++)
            {
                Transform shipTransform = parent.transform.GetChild(i);
                if (shipTransform == null) continue;

                ShipController shipController = shipTransform.GetComponent<ShipController>();
                if (shipController == null)
                {
                    Debug.LogWarning($"    ⚠️ Child '{shipTransform.name}' has no ShipController");
                    continue;
                }

                Vector3 currentLocalPos = shipTransform.localPosition;

                // ✅ CRITICAL: Keep local X=0, preserve Y and Z for formation
                shipTransform.localPosition = new Vector3(0f, currentLocalPos.y, currentLocalPos.z);

                // ✅ Set ship rotation to inherit from parent
                shipTransform.localRotation = parent.transform.rotation;

                // ✅ DEBUG: Log ship's world rotation and forward direction
                Vector3 shipWorldRot = shipTransform.eulerAngles;
                Vector3 shipForward = shipTransform.forward;

                Debug.Log($"    {parentName}/{shipController.ShipData.ShipName}: " +
                  $"Local pos=(0, {currentLocalPos.y:F2}, {currentLocalPos.z:F2}), " +
                  $"Local rot={shipRotation.eulerAngles}, World rot={shipWorldRot}, Forward={shipForward}");

                correctionCount++;
            }

            Debug.Log($"  {parentName}: Corrected {childCount} ships");
        }

        /// <summary>
        /// ✅ DIAGNOSTIC: Log parent GameObject and ship positions WITHOUT changing anything
        /// This helps verify Unity scene setup is correct
        /// </summary>
        public void CorrectAnimatorPositions()
        {
            if (CurrentCombatController == null)
            {
                Debug.LogError("❌ Cannot check parent positions - CurrentCombatController is null!");
                return;
            }

            Debug.Log("🔍 Checking parent and ship positions (NO CHANGES MADE)...");

            // ✅ Check Side One parents
            CheckAnimatorShipPositions(CurrentCombatController.sideOneA1Parent, "S1A1");
            CheckAnimatorShipPositions(CurrentCombatController.sideOneA2Parent, "S1A2");
            CheckAnimatorShipPositions(CurrentCombatController.sideOneA3Parent, "S1A3");

            // ✅ Check Side Two parents
            CheckAnimatorShipPositions(CurrentCombatController.sideTwoA1Parent, "S2A1");
            CheckAnimatorShipPositions(CurrentCombatController.sideTwoA2Parent, "S2A2");
            CheckAnimatorShipPositions(CurrentCombatController.sideTwoA3Parent, "S2A3");

            Debug.Log($"✅ Position check complete - see logs above");
        }

        /// <summary>
        /// Checks and logs ship positions under a single parent GameObject WITHOUT modifying them
        /// </summary>
        private void CheckAnimatorShipPositions(GameObject parent, string parentName)
        {
            if (parent == null)
            {
                Debug.LogWarning($"  ⚠️ {parentName} parent is null - skipping");
                return;
            }

            Debug.Log($"  {parentName} Parent:");
            Debug.Log($"    World Pos: {parent.transform.position}");
            Debug.Log($"    World Rot: {parent.transform.rotation.eulerAngles}");

            int childCount = parent.transform.childCount;

            if (childCount == 0)
            {
                Debug.Log($"    No ships (empty parent)");
                return;
            }

            for (int i = 0; i < childCount; i++)
            {
                Transform shipTransform = parent.transform.GetChild(i);

                if (shipTransform == null) continue;

                ShipController shipController = shipTransform.GetComponent<ShipController>();

                if (shipController == null)
                {
                    Debug.LogWarning($"      ⚠️ Child '{shipTransform.name}' has no ShipController");
                    continue;
                }

                Debug.Log($"      Ship: {shipController.ShipData.ShipName}");
                Debug.Log($"        Local Pos: {shipTransform.localPosition}");
                Debug.Log($"        Local Rot: {shipTransform.localRotation.eulerAngles}");
                Debug.Log($"        World Pos: {shipTransform.position}");
                Debug.Log($"        World Rot: {shipTransform.rotation.eulerAngles}");
            }
        }
    }
}
