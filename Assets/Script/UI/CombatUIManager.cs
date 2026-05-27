using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private TextMeshProUGUI timerText;
        private Toggle engage, rush, retreat, formation, AttackTransports;

        // ✅ Combat state
        private float remainingTime = 15f; // Order selection time
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
            // FindUIElement(currentCombatUICanvas, "PanelCombat_Menu");
            Debug.Log("✅ CombatUIManager initialized (persistent)");
        }
        IEnumerator Start()
        {
            yield return null;
            FindUIElement(currentCombatUICanvas, "PanelCombat_Menu"); // find it now lest it be null on loading combat
        }
        private void Update()
        {
            if (isTimerRunning)
            {
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
        }

        /// <summary>
        /// Called by CombatManager when a new combat starts for the local player
        /// </summary>
        public void SetupForCombat(CombatController combatController, GameObject combatUICanvas, GameObject combat3DCanvas, GameObject gameOverCanvas)
        {
            Debug.Log($"🎮 CombatUIManager: Setting up for combat {combatController.CombatID}");

            CurrentCombatController = combatController;
            currentCombatUICanvas = combatUICanvas;
            currentCombat3DCanvas = combat3DCanvas;
            currentGameOverCanvas = gameOverCanvas;
            CivEnumLocalPlayer = GameController.Instance.GameData.LocalPlayerCivEnum;

            // ✅ Setup UI after scene loads
            StartCoroutine(SetupCombatUIAfterSceneLoad());
        }

        /// <summary>
        /// Coroutine to setup combat UI after CombatScene fully loads
        /// </summary>
        private IEnumerator SetupCombatUIAfterSceneLoad()
        {
            Debug.Log("SetupCombatUIAfterSceneLoad: Waiting for scene to stabilize...");

            // ✅ Wait two frames (from copilot-instructions.md)
            yield return null;
            yield return null;

            Debug.Log("SetupCombatUIAfterSceneLoad: Scene stabilized - initializing UI...");

            // ✅ Ensure EventSystem exists
            EnsureEventSystemExists();

            // ✅ Wait for ShipCombatCameraController
            yield return WaitForCombatCameraReady();

            // ✅ Configure canvases
            ConfigureCombatCanvases();

            // ✅ Find and setup UI
            FindAndSetupUI();

            // ✅ Force Canvas rebuild
            ForceCanvasRebuild();

            // ✅ Start timer
            remainingTime = 10f;
            isTimerRunning = true;

            Debug.Log("✅ Combat UI setup complete");
        }

        /// <summary>
        /// Wait for ShipCombatCameraController to be ready
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

                // Handle turn-based vs real-time combat
                if (CurrentCombatController.UseTurnBasedCombat)
                {
                    // Store the selected order for Turn 1 (will be submitted after warp-in)
                    Debug.Log($"🎮 Turn-based combat: Player selected {currentOrder} for Turn 1");

                    // Set orders on controller so they're available during warp positioning
                    CurrentCombatController.SetShipOrders(currentOrder, CivEnumLocalPlayer);

                    // AI picks its order too
                    CivEnum aiCivEnum = (CivEnumLocalPlayer == CurrentCombatController.CombatData.CivEnumSideOne)
                        ? CurrentCombatController.CombatData.CivEnumSideTwo
                        : CurrentCombatController.CombatData.CivEnumSideOne;
                    CurrentCombatController.SetAIRandomOrder(aiCivEnum);

                    // ✅ Start combat sequence (warp-in, then turn-based begins)
                    StartCoroutine(StartCombatSequence());
                }
                else
                {
                    // Original real-time combat
                    CurrentCombatController.SetShipOrders(currentOrder, CivEnumLocalPlayer);

                    // ✅ Give AI a random order for the other side
                    CivEnum aiCivEnum = (CivEnumLocalPlayer == CurrentCombatController.CombatData.CivEnumSideOne)
                        ? CurrentCombatController.CombatData.CivEnumSideTwo
                        : CurrentCombatController.CombatData.CivEnumSideOne;

                    CurrentCombatController.SetAIRandomOrder(aiCivEnum);

                    Debug.Log($"✅ Combat orders set - Player: {currentOrder}");

                    // ✅ Start the new simplified warp-in animation
                    StartCoroutine(StartCombatSequence());
                }
            }
            else
            {
                Debug.LogError("❌ Cannot start combat - controller is null!");
            }
        }

        /// <summary>
        /// ✅ NEW SIMPLIFIED: Start warp-in animation and combat
        /// </summary>
        private IEnumerator StartCombatSequence()
        {
            Debug.Log("🌀 Starting combat sequence...");

            if (CurrentCombatController == null)
            {
                Debug.LogError("❌ Cannot start combat sequence - CurrentCombatController is null!");
                yield break;
            }

            // ✅ Verify camera is ready
            if (ShipCombatCameraController.Instance == null)
            {
                Debug.LogError("❌ Cannot start combat - ShipCombatCameraController not found!");
                yield break;
            }

            // ✅ Set camera targets to all ships
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

            // ✅ Start warp-in animation (new simplified coroutine)
            yield return CurrentCombatController.StartWarpInAnimation();

            Debug.Log("🎬 Combat sequence complete - ships should be in formation and ready!");
        }

        /// <summary>
        /// Ensures the persistent EventSystem is active
        /// </summary>
        private void EnsureEventSystemExists()
        {
            UnityEngine.EventSystems.EventSystem[] allEventSystems =
                FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);

            UnityEngine.EventSystems.EventSystem persistentEventSystem = null;
            List<UnityEngine.EventSystems.EventSystem> sceneEventSystems = new List<UnityEngine.EventSystems.EventSystem>();

            foreach (var es in allEventSystems)
            {
                if (es.gameObject.scene.name == "DontDestroyOnLoad" || es.gameObject.scene.buildIndex == -1)
                {
                    persistentEventSystem = es;
                    Debug.Log($"✅ Found persistent EventSystem: '{es.gameObject.name}'");
                }
                else
                {
                    sceneEventSystems.Add(es);
                }
            }

            if (persistentEventSystem == null)
            {
                Debug.LogWarning("⚠️ No persistent EventSystem found - creating one!");

                GameObject esGO = new GameObject("EventSystem_Persistent");
                persistentEventSystem = esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                DontDestroyOnLoad(esGO);

                Debug.Log("✅ Created persistent EventSystem");
            }

            if (!persistentEventSystem.enabled)
            {
                persistentEventSystem.enabled = true;
            }

            foreach (var sceneES in sceneEventSystems)
            {
                Debug.LogWarning($"🗑️ Destroying duplicate EventSystem '{sceneES.gameObject.name}'");
                Destroy(sceneES.gameObject);
            }

            UnityEngine.EventSystems.EventSystem.current = persistentEventSystem;
        }

        /// <summary>
        /// Configures all combat canvases
        /// </summary>
        private void ConfigureCombatCanvases()
        {
            Debug.Log("ConfigureCombatCanvases: Starting...");

            // ✅ Configure CombatUICanvas
            if (currentCombatUICanvas != null)
            {
                Canvas canvas = currentCombatUICanvas.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = currentCombatUICanvas.AddComponent<Canvas>();
                }

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                }

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

                Debug.Log($"  ✅ CombatUICanvas configured");
            }

            // ✅ Configure Combat3DCanvas
            if (currentCombat3DCanvas != null)
            {
                Canvas canvas3D = currentCombat3DCanvas.GetComponent<Canvas>();
                if (canvas3D == null)
                {
                    canvas3D = currentCombat3DCanvas.AddComponent<Canvas>();
                }

                canvas3D.renderMode = RenderMode.WorldSpace;

                Camera combatCamera = ShipCombatCameraController.Instance?.GetComponentInChildren<Camera>();
                if (combatCamera == null)
                {
                    combatCamera = Camera.main;
                }

                if (combatCamera != null)
                {
                    canvas3D.worldCamera = combatCamera;
                    Debug.Log($"  ✅ Set Combat3DCanvas camera to: {combatCamera.name}");
                }

                canvas3D.enabled = true;
                currentCombat3DCanvas.SetActive(true);
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
                gameOverCanvas.sortingOrder = 200;

                var raycasterGameOver = gameOverCanvas.GetComponent<GraphicRaycaster>();
                if (raycasterGameOver == null)
                {
                    raycasterGameOver = gameOverCanvas.gameObject.AddComponent<GraphicRaycaster>();
                }

                gameOverCanvas.enabled = true;
            }

            Debug.Log("ConfigureCombatCanvases: Complete");
        }

        /// <summary>
        /// Forces Unity to rebuild all canvases
        /// </summary>
        private void ForceCanvasRebuild()
        {
            Canvas.ForceUpdateCanvases();

            if (currentCombatUICanvas != null)
            {
                Image[] images = currentCombatUICanvas.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    img.enabled = true;
                }

                CanvasRenderer[] renderers = currentCombatUICanvas.GetComponentsInChildren<CanvasRenderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.SetAlpha(1f);
                }
            }
        }

        /// <summary>
        /// Find all UI elements
        /// </summary>
        private void FindAndSetupUI()
        {
            if (currentCombatUICanvas == null)
            {
                Debug.LogError("❌ Cannot setup UI - CombatUICanvas is null!");
                return;
            }

            Debug.Log("FindAndSetupUI: Starting...");

            currentCombatUICanvas.SetActive(true);

            panelCombatMenu = FindUIElement(currentCombatUICanvas, "PanelCombat_Menu");
            if (panelCombatMenu != null)
            {
                panelCombatMenu.SetActive(true);
                Debug.Log($"  ✅ Found and activated: {panelCombatMenu.name}");
            }

            if (currentCombat3DCanvas != null)
            {
                panelShipCombat = FindUIElement(currentCombat3DCanvas, "PanelShipCombat");
                if (panelShipCombat != null)
                {
                    panelShipCombat.SetActive(false);
                }
            }

            if (currentGameOverCanvas != null)
            {
                panelCombatOver = FindUIElement(currentGameOverCanvas, "PanelCombatEnd");
                if (panelCombatOver != null)
                {
                    panelCombatOver.SetActive(false);
                }
            }

            timerText = FindComponentByName<TextMeshProUGUI>(currentCombatUICanvas, "Timer Text");

            SetupToggles();
            SetupButtons();

            Debug.Log("FindAndSetupUI: Complete");
        }

        private void SetupToggles()
        {
            if (currentCombatUICanvas == null) return;

            Toggle[] toggles = currentCombatUICanvas.GetComponentsInChildren<Toggle>(true);
            if (toggles.Length == 0) return;

            // Find or create ToggleGroup
            ToggleGroup group = currentCombatUICanvas.GetComponentInChildren<ToggleGroup>(true);
            if (group == null)
            {
                Transform parent = toggles[0].transform.parent;
                group = (parent != null) ? parent.gameObject.GetComponent<ToggleGroup>() : null;
                if (group == null) group = (parent != null) ? parent.gameObject.AddComponent<ToggleGroup>() : currentCombatUICanvas.gameObject.AddComponent<ToggleGroup>();
            }
            group.allowSwitchOff = false;

            // Phase 1: Reset everything and ensure raycast targets
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.group = null; // Unlink group to allow reset
                toggle.interactable = true;
                toggle.isOn = false;

                // Configure checkmark graphic
                if (toggle.graphic != null)
                {
                    // Ensure the GameObject is active; Unity Toggle will control the component
                    toggle.graphic.gameObject.SetActive(true);
                    toggle.graphic.enabled = false; // Hide initially
                    if (toggle.graphic is UnityEngine.UI.Graphic g) g.raycastTarget = false;
                }

                var images = toggle.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in images)
                {
                    // Background images should be raycast targets
                    if (toggle.graphic != img && !img.gameObject.name.Contains("Checkmark"))
                    {
                        img.raycastTarget = true;
                    }
                    else
                    {
                        img.raycastTarget = false;
                    }
                }
            }

            // Phase 2: Link group and set default
            foreach (var toggle in toggles)
            {
                toggle.group = group;

                switch (toggle.name)
                {
                    case "Toggle_ENGAGE":
                        engage = toggle;
                        // Set isOn to false first to ensure the value change triggers when set to true
                        engage.isOn = false;
                        engage.isOn = true;
                        currentOrder = CombatOrders.Engage;
                        // Manual activation for the default to be safe
                        if (engage.graphic != null) engage.graphic.enabled = true;
                        engage.onValueChanged.AddListener(OnToggleENGAGE);
                        break;
                    case "Toggle_RUSH":
                        rush = toggle;
                        rush.onValueChanged.AddListener(OnToggleRUSH);
                        break;
                    case "Toggle_RETREAT":
                        retreat = toggle;
                        retreat.onValueChanged.AddListener(OnToggleRETREAT);
                        break;
                    case "Toggle_FORMATION":
                        formation = toggle;
                        formation.onValueChanged.AddListener(OnToggleFORMATION);
                        break;
                    case "Toggle_TARGET_TRANSPORTS":
                        AttackTransports = toggle;
                        AttackTransports.onValueChanged.AddListener(OnToggleTARGET_TRANSPORTS);
                        break;
                }
            }

            Debug.Log($"✅ Setup {toggles.Length} toggles. Default order: {currentOrder}");
        }

        private void SetupButtons()
        {
            if (currentCombatUICanvas == null) return;

            Button[] buttons = currentCombatUICanvas.GetComponentsInChildren<Button>(true);

            foreach (var button in buttons)
            {
                button.gameObject.SetActive(true);
                button.interactable = true;

                if (button.name == "ButtonEnterCombat")
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(EnterShipCombatPhase);
                }
            }
        }

        private GameObject FindUIElement(GameObject root, string name)
        {
            if (root == null) return null;

            Transform found = root.transform.Find(name);
            if (found != null) return found.gameObject;

            found = FindInHierarchyRecursive(root.transform, name);
            if (found != null) return found.gameObject;


            return null;
        }

        private T FindComponentByName<T>(GameObject root, string name) where T : Component
        {
            if (root == null) return null;

            T[] components = root.GetComponentsInChildren<T>(true);

            foreach (var component in components)
            {
                if (component.name == name || component.name.Contains(name))
                {
                    return component;
                }
            }

            return null;
        }

        private Transform FindInHierarchyRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindInHierarchyRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        // Toggle callbacks
        private void OnToggleENGAGE(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Engage;
        }

        private void OnToggleRUSH(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Rush;
        }

        private void OnToggleRETREAT(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Retreat;
        }

        private void OnToggleFORMATION(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Formation;
        }

        private void OnToggleTARGET_TRANSPORTS(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.AttackTransports;
        }

        /// <summary>
        /// Show order selection UI for next turn (turn-based combat)
        /// </summary>
        public void ShowOrderSelectionForNextTurn()
        {
            Debug.Log("🎮 Showing order selection for next turn");

            // Show combat menu again
            if (panelCombatMenu != null)
            {
                panelCombatMenu.SetActive(true);
            }

            // Hide 3D combat view temporarily
            if (panelShipCombat != null)
            {
                panelShipCombat.SetActive(false);
            }

            // Reset timer
            remainingTime = 15f;
            isTimerRunning = true;
        }

        /// <summary>
        /// Show combat over panel
        /// </summary>
        public void ShowCombatOverPanel()
        {
            if (panelCombatOver == null)
            {
                if (currentGameOverCanvas != null)
                {
                    panelCombatOver = FindUIElement(currentGameOverCanvas, "PanelCombatEnd");
                }

                if (panelCombatOver == null && currentCombatUICanvas != null)
                {
                    panelCombatOver = FindUIElement(currentCombatUICanvas, "PanelCombatEnd");
                }
            }

            if (panelCombatOver != null)
            {
                panelCombatOver.SetActive(true);
                Debug.Log("✅ Combat over panel shown");
            }
            else
            {
                Debug.LogError("❌ Cannot show combat over panel - not found!");

                if (CurrentCombatController != null)
                {
                    StartCoroutine(DelayedReturnToGalaxy(3f));
                }
            }
        }

        private IEnumerator DelayedReturnToGalaxy(float delay)
        {
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

                // ✅ CRITICAL: Set ship LOCAL rotation to the specified rotation
                // Parent is at (0,0,0) rotation, so ship's local rotation = world rotation
                Debug.Log($"    🔄 BEFORE rotation: {parentName}/{shipController.ShipData.ShipName} rotation={shipTransform.localRotation.eulerAngles}, forward={shipTransform.forward}");

                shipTransform.localRotation = shipRotation;

                Debug.Log($"    🔄 AFTER rotation: {parentName}/{shipController.ShipData.ShipName} rotation={shipTransform.localRotation.eulerAngles}, forward={shipTransform.forward}");

                // ✅ DEBUG: Log ship's world rotation and forward direction
                Vector3 shipWorldRot = shipTransform.eulerAngles;
                Vector3 shipForward = shipTransform.forward;

                Debug.Log($"    ✅ {parentName}/{shipController.ShipData.ShipName}: " +
                  $"Local pos=(0, {currentLocalPos.y:F2}, {currentLocalPos.z:F2}), " +
                  $"Local rot={shipTransform.localRotation.eulerAngles}, World rot={shipWorldRot}, Forward={shipForward}");

                correctionCount++;
            }

            Debug.Log($"  {parentName}: Corrected {childCount} ships");
        }
    }
}
