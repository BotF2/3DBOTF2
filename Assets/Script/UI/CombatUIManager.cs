using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using System.Collections;
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
        private Toggle engage, rush, retreat, formation, targetTransports;

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
                remainingTime -= Time.deltaTime;
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
        /// Ensures EventSystem exists in CombatScene (critical for builds!)
        /// </summary>
        private void EnsureEventSystemExists()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;

            if (eventSystem == null)
            {
                Debug.LogWarning("⚠️ No EventSystem found in CombatScene - creating one!");

                GameObject esGO = new GameObject("EventSystem_Combat");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                Debug.Log("✅ Created EventSystem for CombatScene");
            }
            else
            {
                Debug.Log($"✅ EventSystem exists: {eventSystem.gameObject.name}");

                // Ensure it's enabled
                if (!eventSystem.enabled)
                {
                    eventSystem.enabled = true;
                    Debug.Log("  Re-enabled EventSystem");
                }
            }
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

                // Find combat camera
                Camera combatCamera = null;
                if (ShipCombatCameraController.Instance != null)
                {
                    combatCamera = ShipCombatCameraController.Instance.GetComponent<Camera>();
                }

                if (combatCamera != null)
                {
                    canvas3D.worldCamera = combatCamera;
                    Debug.Log($"  Set Combat3DCanvas camera to: {combatCamera.name}");
                }
                else
                {
                    Debug.LogWarning("  ⚠️ Combat camera not found - Combat3DCanvas may not render correctly");
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
                Debug.LogError("  ❌ Could not find PanelCombat_Menu!");
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
            timerText = FindComponentByName<TextMeshProUGUI>(currentCombatUICanvas, "Timer Text", "Timer", "TimerText");
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
                        rush.onValueChanged.AddListener(OnToggleRUSH);
                        Debug.Log("    ✅ Wired Toggle_RUSH");
                        break;
                    case "Toggle_RETREAT":
                        retreat = toggle;
                        retreat.onValueChanged.AddListener(OnToggleRETREAT);
                        Debug.Log("    ✅ Wired Toggle_RETREAT");
                        break;
                    case "Toggle_FORMATION":
                        formation = toggle;
                        formation.onValueChanged.AddListener(OnToggleFORMATION);
                        Debug.Log("    ✅ Wired Toggle_FORMATION");
                        break;
                    case "Toggle_TARGET_TRANSPORTS":
                        targetTransports = toggle;
                        targetTransports.onValueChanged.AddListener(OnToggleTARGET_TRANSPORTS);
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
                currentOrder = CombatOrders.TargetTransports;
                Debug.Log("Order: Target Transports");
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

                // Start combat animation
                CurrentCombatController.RunAnimation();

                Debug.Log($"✅ Combat phase started with order: {currentOrder}, AI order: {randomAIOrder}");
            }
            else
            {
                Debug.LogError("❌ Cannot start combat - controller is null!");
            }
        }

        /// <summary>
        /// Show combat over panel
        /// </summary>
        public void ShowCombatOverPanel()
        {
            if (panelCombatOver != null)
            {
                panelCombatOver.SetActive(true);
                Debug.Log("✅ Combat over panel shown");
            }
            else
            {
                Debug.LogError("❌ Cannot show combat over panel - it's null!");
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
            targetTransports = null;
            isTimerRunning = false;
        }
    }
}