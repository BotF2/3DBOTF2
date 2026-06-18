using BOTF3D.Civilization;
using BOTF3D.Combat;
using BOTF3D.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public void Initialize() { }
        public void Cleanup() { }
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
        private TextMeshProUGUI combatOutcomeText;
        private Toggle engage, rush, retreat, formation, AttackTransports, capture, scuttle;
        private Toggle engage2, rush2, retreat2, formation2, AttackTransports2, capture2, scuttle2;

        // ✅ Civilization & Ship Count Fields
        private TextMeshProUGUI sideOneCivName, sideTwoCivName;
        private TextMeshProUGUI sideOneTechLevel, sideTwoTechLevel;
        private TextMeshProUGUI s1Scouts, s1Destroyers, s1Cruisers, s1LtCruisers, s1HvyCruisers, s1Transports, s1Total;
        private TextMeshProUGUI s2Scouts, s2Destroyers, s2Cruisers, s2LtCruisers, s2HvyCruisers, s2Transports, s2Total;

        // ✅ Combat state
        private float remainingTime = 15f; // Order selection time
        private bool isTimerRunning = false;
        private CombatOrders currentOrder = CombatOrders.Engage;
        private CombatOrders currentOrderSideTwo = CombatOrders.Engage;
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

            // ✅ Wait for ShipCombatCameraController ready
            yield return WaitForCombatCameraReady();

            // ✅ Configure canvases
            ConfigureCombatCanvases();

            // ✅ Find and setup UI
            FindAndSetupUI();

            // ✅ Update menu data with current combat info
            UpdateCombatMenuData();

            // ✅ Force Canvas rebuild
            ForceCanvasRebuild();

            // ✅ Re-apply toggle selections after rebuild (ForceCanvasRebuild must not clobber them)
            ApplyToggleForOrder(currentOrder, false);
            ApplyToggleForOrder(currentOrderSideTwo, true);

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
                    Debug.Log($"🎮 Turn-based combat: Side 1: {currentOrder}, Side 2: {currentOrderSideTwo}");

                    // Set orders on controller so they're available during warp positioning
                    CurrentCombatController.SetShipOrders(currentOrder, CurrentCombatController.CombatData.CivEnumSideOne, 1);
                    CurrentCombatController.SetShipOrders(currentOrderSideTwo, CurrentCombatController.CombatData.CivEnumSideTwo, 2);

                    // Notify TurnResolver of the selections (for Turn 2+)
                    if (CurrentCombatController.TurnResolver != null)
                    {
                        CurrentCombatController.TurnResolver.OnPlayerSelectOrder(currentOrder, 1);
                        CurrentCombatController.TurnResolver.OnPlayerSelectOrder(currentOrderSideTwo, 2);
                    }

                    // Random AI order is deactivated per request
                    // CivEnum aiCivEnum = (CivEnumLocalPlayer == CurrentCombatController.CombatData.CivEnumSideOne)
                    //     ? CurrentCombatController.CombatData.CivEnumSideTwo
                    //     : CurrentCombatController.CombatData.CivEnumSideOne;
                    // CurrentCombatController.SetAIRandomOrder(aiCivEnum);

                    // ✅ Start combat sequence (warp-in, then turn-based begins)
                    if (CurrentCombatController.WarpingIn) // Only start sequence if not already in turn-based loop
                    {
                        StartCoroutine(StartCombatSequence());
                    }
                }
                else
                {
                    // Original real-time combat
                    CurrentCombatController.SetShipOrders(currentOrder, CurrentCombatController.CombatData.CivEnumSideOne, 1);
                    CurrentCombatController.SetShipOrders(currentOrderSideTwo, CurrentCombatController.CombatData.CivEnumSideTwo, 2);

                    // ✅ Random AI order is deactivated per request
                    // CivEnum aiCivEnum = (CivEnumLocalPlayer == CurrentCombatController.CombatData.CivEnumSideOne)
                    //     ? CurrentCombatController.CombatData.CivEnumSideTwo
                    //     : CurrentCombatController.CombatData.CivEnumSideOne;
                    // CurrentCombatController.SetAIRandomOrder(aiCivEnum);

                    Debug.Log($"✅ Combat orders set - Side 1: {currentOrder}, Side 2: {currentOrderSideTwo}");

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
        /// Forces Unity to rebuild all canvases.
        /// Skips toggle checkmark images so their on/off state is never overridden.
        /// </summary>
        private void ForceCanvasRebuild()
        {
            Canvas.ForceUpdateCanvases();

            if (currentCombatUICanvas != null)
            {
                // Collect all toggle graphic (checkmark) images so we don't clobber their state
                var checkmarks = new HashSet<Image>();
                foreach (var t in currentCombatUICanvas.GetComponentsInChildren<Toggle>(true))
                {
                    if (t.graphic is Image ci) checkmarks.Add(ci);
                }

                Image[] images = currentCombatUICanvas.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (!checkmarks.Contains(img))
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
            FindAndSetupCivDataUI();

            Debug.Log("FindAndSetupUI: Complete");
        }

        /// <summary>
        /// Finds and assigns UI components for Civ names, tech levels, and ship counts
        /// </summary>
        private void FindAndSetupCivDataUI()
        {
            if (panelCombatMenu == null) return;

            GameObject sideOnePanel = FindUIElement(panelCombatMenu, "SideOne");
            GameObject sideTwoPanel = FindUIElement(panelCombatMenu, "SideTwo");

            // Setup Side One
            sideOneCivName = FindComponentByName<TextMeshProUGUI>(panelCombatMenu, "SideOneCivName");
            sideOneTechLevel = FindComponentByName<TextMeshProUGUI>(panelCombatMenu, "SideOneTechLevel");

            if (sideOnePanel != null)
            {
                s1Scouts = FindComponentByName<TextMeshProUGUI>(sideOnePanel, "SideOneNumScouts");
                s1Destroyers = FindComponentByName<TextMeshProUGUI>(sideOnePanel, "SideOneNumDestroyers");
                s1Cruisers = FindComponentByName<TextMeshProUGUI>(sideOnePanel, "SideOneNumCruisers");
                s1LtCruisers = FindComponentByName<TextMeshProUGUI>(sideOnePanel, "SideOneNumLtCruisers");
                s1HvyCruisers = FindComponentByName<TextMeshProUGUI>(sideOnePanel, "SideOneNumHVYCruisers");
                s1Transports = FindComponentByName<TextMeshProUGUI>(sideOnePanel, "SideOneNumTransports");
                s1Total = FindComponentByName<TextMeshProUGUI>(sideOnePanel, "SideOneNumTotal");
                Debug.Log("  ✅ Side One Civ UI found");
            }

            // Setup Side Two
            sideTwoCivName = FindComponentByName<TextMeshProUGUI>(panelCombatMenu, "SideTwoCivName");
            sideTwoTechLevel = FindComponentByName<TextMeshProUGUI>(panelCombatMenu, "SideTwoTechLevel");

            if (sideTwoPanel != null)
            {
                // Using clean names for Side Two as renamed by user
                s2Scouts = FindComponentByName<TextMeshProUGUI>(sideTwoPanel, "SideTwoNumScouts");
                s2Destroyers = FindComponentByName<TextMeshProUGUI>(sideTwoPanel, "SideTwoNumDestroyers");
                s2Cruisers = FindComponentByName<TextMeshProUGUI>(sideTwoPanel, "SideTwoNumCruisers");
                s2LtCruisers = FindComponentByName<TextMeshProUGUI>(sideTwoPanel, "SideTwoNumLtCruisers");
                s2HvyCruisers = FindComponentByName<TextMeshProUGUI>(sideTwoPanel, "SideTwoNumHVYCruisers");
                s2Transports = FindComponentByName<TextMeshProUGUI>(sideTwoPanel, "SideTwoNumTransports");
                s2Total = FindComponentByName<TextMeshProUGUI>(sideTwoPanel, "SideTwoNumTotal");
                Debug.Log("  ✅ Side Two Civ UI found");
            }
        }

        /// <summary>
        /// Populates the combat menu with data from the current combat
        /// </summary>
        private void UpdateCombatMenuData()
        {
            if (CurrentCombatController == null || CurrentCombatController.CombatData == null)
            {
                Debug.LogWarning("UpdateCombatMenuData: No active combat data to display");
                return;
            }

            var data = CurrentCombatController.CombatData;

            // Update Side One Data
            if (sideOneCivName != null) sideOneCivName.text = data.sideOneCiv?.CivShortName ?? data.CivEnumSideOne.ToString();
            if (sideOneTechLevel != null) sideOneTechLevel.text = GetTechLevelRoman(data.sideOneCiv?.CivData?.CurrentTechLevel ?? TechLevel.EARLY);
            UpdateShipCounts(data.SideOneShipCons, s1Scouts, s1Destroyers, s1Cruisers, s1LtCruisers, s1HvyCruisers, s1Transports, s1Total);

            // Update Side Two Data
            if (sideTwoCivName != null) sideTwoCivName.text = data.sideTwoCiv?.CivShortName ?? data.CivEnumSideTwo.ToString();
            if (sideTwoTechLevel != null) sideTwoTechLevel.text = GetTechLevelRoman(data.sideTwoCiv?.CivData?.CurrentTechLevel ?? TechLevel.EARLY);
            UpdateShipCounts(data.SideTwoShipCons, s2Scouts, s2Destroyers, s2Cruisers, s2LtCruisers, s2HvyCruisers, s2Transports, s2Total);

            Debug.Log("📊 Combat Menu data updated");
        }

        private string GetTechLevelRoman(TechLevel level)
        {
            switch (level)
            {
                case TechLevel.EARLY: return "I";
                case TechLevel.DEVELOPED: return "II";
                case TechLevel.ADVANCED: return "III";
                case TechLevel.SUPREME: return "IV";
                default: return "I";
            }
        }

        private void UpdateShipCounts(List<ShipController> ships, TextMeshProUGUI scouts, TextMeshProUGUI destroyers, TextMeshProUGUI cruisers, TextMeshProUGUI ltCruisers, TextMeshProUGUI hvyCruisers, TextMeshProUGUI transports, TextMeshProUGUI total)
        {
            if (ships == null) return;

            // Filter for valid ships
            var activeShips = ships.Where(s => s != null && s.ShipData != null).ToList();

            int nScouts = activeShips.Count(s => s.ShipData.ShipType == ShipType.Scout);
            int nDestroyers = activeShips.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
            int nCruisers = activeShips.Count(s => s.ShipData.ShipType == ShipType.Cruiser);
            int nLtCruisers = activeShips.Count(s => s.ShipData.ShipType == ShipType.LtCruiser);
            int nHvyCruisers = activeShips.Count(s => s.ShipData.ShipType == ShipType.HvyCruiser);
            int nTransports = activeShips.Count(s => s.ShipData.ShipType == ShipType.Transport);

            if (scouts != null) scouts.text = nScouts.ToString();
            if (destroyers != null) destroyers.text = nDestroyers.ToString();
            if (cruisers != null) cruisers.text = nCruisers.ToString();
            if (ltCruisers != null) ltCruisers.text = nLtCruisers.ToString();
            if (hvyCruisers != null) hvyCruisers.text = nHvyCruisers.ToString();
            if (transports != null) transports.text = nTransports.ToString();
            if (total != null) total.text = activeShips.Count.ToString();
        }

        private void SetupToggles()
        {
            if (currentCombatUICanvas == null) return;

            Toggle[] toggles = currentCombatUICanvas.GetComponentsInChildren<Toggle>(true);
            if (toggles.Length == 0) return;

            // Find all ToggleGroups
            ToggleGroup[] groups = currentCombatUICanvas.GetComponentsInChildren<ToggleGroup>(true);
            if (groups == null || groups.Length < 2)
            {
                Debug.LogWarning("CombatUIManager: Found less than 2 ToggleGroups in UI. Some orders may not function correctly.");
            }

            // Phase 1: Reset everything and ensure raycast targets
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.interactable = true;
                toggle.isOn = false;

                // Configure checkmark graphic
                if (toggle.graphic != null)
                {
                    toggle.graphic.gameObject.SetActive(true);
                    toggle.graphic.enabled = false;
                    if (toggle.graphic is UnityEngine.UI.Graphic g) g.raycastTarget = false;
                }

                var images = toggle.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in images)
                {
                    if (toggle.graphic != img && !img.gameObject.name.Contains("Checkmark"))
                        img.raycastTarget = true;
                    else
                        img.raycastTarget = false;
                }
            }

            // Phase 2: Link and setup toggles based on names
            foreach (var toggle in toggles)
            {
                switch (toggle.name)
                {
                    // Side One / Player
                    case "Toggle_ENGAGE":
                        engage = toggle;
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
                    case "Toggle_CAPTURE":
                        capture = toggle;
                        capture.onValueChanged.AddListener(OnToggleCAPTURE);
                        break;
                    case "Toggle_SCUTTLE":
                        scuttle = toggle;
                        scuttle.onValueChanged.AddListener(OnToggleSCUTTLE);
                        break;

                    // Side Two
                    case "Toggle_ENGAGE2":
                        engage2 = toggle;
                        engage2.onValueChanged.AddListener(OnToggleENGAGE2);
                        break;
                    case "Toggle_RUSH2":
                        rush2 = toggle;
                        rush2.onValueChanged.AddListener(OnToggleRUSH2);
                        break;
                    case "Toggle_RETREAT2":
                        retreat2 = toggle;
                        retreat2.onValueChanged.AddListener(OnToggleRETREAT2);
                        break;
                    case "Toggle_FORMATION2":
                        formation2 = toggle;
                        formation2.onValueChanged.AddListener(OnToggleFORMATION2);
                        break;
                    case "Toggle_TARGET_TRANSPORTS2":
                        AttackTransports2 = toggle;
                        AttackTransports2.onValueChanged.AddListener(OnToggleTARGET_TRANSPORTS2);
                        break;
                    case "Toggle_CAPTURE2":
                        capture2 = toggle;
                        capture2.onValueChanged.AddListener(OnToggleCAPTURE2);
                        break;
                    case "Toggle_SCUTTLE2":
                        scuttle2 = toggle;
                        scuttle2.onValueChanged.AddListener(OnToggleSCUTTLE2);
                        break;
                }
            }

            // Phase 3: Ensure Capture and Scuttle are in the same ToggleGroup as the rest of their side.
            // New toggles added to the Canvas without being assigned to the Inspector ToggleGroup
            // will allow multiple selections unless we wire them here.
            if (engage?.group != null)
            {
                if (capture != null) capture.group = engage.group;
                if (scuttle != null) scuttle.group = engage.group;
            }
            if (engage2?.group != null)
            {
                if (capture2 != null) capture2.group = engage2.group;
                if (scuttle2 != null) scuttle2.group = engage2.group;
            }

            // Phase 4: Apply the initial order from CombatData if already set by scenario runner,
            // otherwise fall back to Engage as default.
            CombatOrders initialSideOne = CombatOrders.Engage;
            CombatOrders initialSideTwo = CombatOrders.Engage;

            if (CurrentCombatController != null && CurrentCombatController.CombatData != null)
            {
                if (CurrentCombatController.CombatData.SideOneOrder != CombatOrders.None)
                    initialSideOne = CurrentCombatController.CombatData.SideOneOrder;
                if (CurrentCombatController.CombatData.SideTwoOrder != CombatOrders.None)
                    initialSideTwo = CurrentCombatController.CombatData.SideTwoOrder;
            }

            ApplyToggleForOrder(initialSideOne, false);
            ApplyToggleForOrder(initialSideTwo, true);

            Debug.Log($"✅ Setup {toggles.Length} toggles. Side 1 order: {currentOrder}, Side 2 order: {currentOrderSideTwo}");
        }

        /// <summary>
        /// Activates the toggle matching the given order for one side, clears all other side toggles,
        /// and updates the tracked order field. Guarantees exactly one checkmark per side.
        /// Pass isSideTwo=false for Side One, isSideTwo=true for Side Two.
        /// </summary>
        private void ApplyToggleForOrder(CombatOrders order, bool isSideTwo)
        {
            Toggle target = null;

            if (!isSideTwo)
            {
                currentOrder = order;
                ClearSideToggleGraphics(false);
                switch (order)
                {
                    case CombatOrders.Engage: target = engage; break;
                    case CombatOrders.Rush: target = rush; break;
                    case CombatOrders.Retreat: target = retreat; break;
                    case CombatOrders.Formation: target = formation; break;
                    case CombatOrders.AttackTransports: target = AttackTransports; break;
                    case CombatOrders.Capture: target = capture; break;
                    case CombatOrders.Scuttle: target = scuttle; break;
                    default: target = engage; currentOrder = CombatOrders.Engage; break;
                }
            }
            else
            {
                currentOrderSideTwo = order;
                ClearSideToggleGraphics(true);
                switch (order)
                {
                    case CombatOrders.Engage: target = engage2; break;
                    case CombatOrders.Rush: target = rush2; break;
                    case CombatOrders.Retreat: target = retreat2; break;
                    case CombatOrders.Formation: target = formation2; break;
                    case CombatOrders.AttackTransports: target = AttackTransports2; break;
                    case CombatOrders.Capture: target = capture2; break;
                    case CombatOrders.Scuttle: target = scuttle2; break;
                    default: target = engage2; currentOrderSideTwo = CombatOrders.Engage; break;
                }
            }

            if (target != null)
            {
                target.SetIsOnWithoutNotify(true);
                if (target.graphic != null) target.graphic.enabled = true;
            }
        }

        /// <summary>
        /// Clears isOn and graphic for every toggle on the given side without firing callbacks.
        /// Called before activating a new selection so stale checkmarks can never accumulate.
        /// </summary>
        private void ClearSideToggleGraphics(bool isSideTwo)
        {
            Toggle[] side = isSideTwo
                ? new[] { engage2, rush2, retreat2, formation2, AttackTransports2, capture2, scuttle2 }
                : new[] { engage, rush, retreat, formation, AttackTransports, capture, scuttle };

            foreach (var t in side)
            {
                if (t == null) continue;
                t.SetIsOnWithoutNotify(false);
                if (t.graphic != null) t.graphic.enabled = false;
            }
        }

        /// <summary>
        /// Pre-loads the scenario-selected orders into the UI state and activates the matching toggles.
        /// Call this after SetShipOrders so the UI reflects the editor selection before Enter Combat.
        /// </summary>
        public void PreloadOrdersFromScenario(CombatOrders sideOneOrder, CombatOrders sideTwoOrder)
        {
            if (sideOneOrder != CombatOrders.None)
                ApplyToggleForOrder(sideOneOrder, false);

            if (sideTwoOrder != CombatOrders.None)
                ApplyToggleForOrder(sideTwoOrder, true);

            Debug.Log($"✅ CombatUIManager: Preloaded scenario orders - Side 1: {currentOrder}, Side 2: {currentOrderSideTwo}");
        }

        /// <summary>
        /// Syncs all Combat Scenario Editor fields — orders, civilization names, and tech levels —
        /// into the Combat UI so the panel matches the editor before the player clicks Enter Combat.
        /// </summary>
        public void PreloadScenarioData(
            CombatOrders s1Order, CombatOrders s2Order,
            CivEnum s1Civ, TechLevel s1Tech,
            CivEnum s2Civ, TechLevel s2Tech)
        {
            // Orders → toggles
            if (s1Order != CombatOrders.None) ApplyToggleForOrder(s1Order, false);
            if (s2Order != CombatOrders.None) ApplyToggleForOrder(s2Order, true);

            // Civilization names
            if (sideOneCivName != null) sideOneCivName.text = s1Civ.ToString();
            if (sideTwoCivName != null) sideTwoCivName.text = s2Civ.ToString();

            // Tech levels  (field names differ from parameter names — no shadowing)
            if (sideOneTechLevel != null) sideOneTechLevel.text = GetTechLevelRoman(s1Tech);
            if (sideTwoTechLevel != null) sideTwoTechLevel.text = GetTechLevelRoman(s2Tech);

            Debug.Log($"✅ CombatUIManager: Scenario synced — " +
                      $"S1: {s1Civ} {GetTechLevelRoman(s1Tech)} {s1Order} | " +
                      $"S2: {s2Civ} {GetTechLevelRoman(s2Tech)} {s2Order}");
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

        // Toggle callbacks — each syncs graphic.enabled so the checkmark clears on deselect
        // (ToggleGroup fires isOn=false on the previously selected toggle when a new one is chosen)
        private void OnToggleENGAGE(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Engage;
            if (engage?.graphic != null) engage.graphic.enabled = isOn;
        }

        private void OnToggleRUSH(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Rush;
            if (rush?.graphic != null) rush.graphic.enabled = isOn;
        }

        private void OnToggleRETREAT(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Retreat;
            if (retreat?.graphic != null) retreat.graphic.enabled = isOn;
        }

        private void OnToggleFORMATION(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Formation;
            if (formation?.graphic != null) formation.graphic.enabled = isOn;
        }

        private void OnToggleTARGET_TRANSPORTS(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.AttackTransports;
            if (AttackTransports?.graphic != null) AttackTransports.graphic.enabled = isOn;
        }

        private void OnToggleCAPTURE(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Capture;
            if (capture?.graphic != null) capture.graphic.enabled = isOn;
        }

        private void OnToggleSCUTTLE(bool isOn)
        {
            if (isOn) currentOrder = CombatOrders.Scuttle;
            if (scuttle?.graphic != null) scuttle.graphic.enabled = isOn;
        }

        private void OnToggleENGAGE2(bool isOn)
        {
            if (isOn) currentOrderSideTwo = CombatOrders.Engage;
            if (engage2?.graphic != null) engage2.graphic.enabled = isOn;
        }

        private void OnToggleRUSH2(bool isOn)
        {
            if (isOn) currentOrderSideTwo = CombatOrders.Rush;
            if (rush2?.graphic != null) rush2.graphic.enabled = isOn;
        }

        private void OnToggleRETREAT2(bool isOn)
        {
            if (isOn) currentOrderSideTwo = CombatOrders.Retreat;
            if (retreat2?.graphic != null) retreat2.graphic.enabled = isOn;
        }

        private void OnToggleFORMATION2(bool isOn)
        {
            if (isOn) currentOrderSideTwo = CombatOrders.Formation;
            if (formation2?.graphic != null) formation2.graphic.enabled = isOn;
        }

        private void OnToggleTARGET_TRANSPORTS2(bool isOn)
        {
            if (isOn) currentOrderSideTwo = CombatOrders.AttackTransports;
            if (AttackTransports2?.graphic != null) AttackTransports2.graphic.enabled = isOn;
        }

        private void OnToggleCAPTURE2(bool isOn)
        {
            if (isOn) currentOrderSideTwo = CombatOrders.Capture;
            if (capture2?.graphic != null) capture2.graphic.enabled = isOn;
        }

        private void OnToggleSCUTTLE2(bool isOn)
        {
            if (isOn) currentOrderSideTwo = CombatOrders.Scuttle;
            if (scuttle2?.graphic != null) scuttle2.graphic.enabled = isOn;
        }

        /// <summary>
        /// Show order selection UI for next turn (turn-based combat)
        /// </summary>
        public void ShowOrderSelectionForNextTurn()
        {
            Debug.Log("🎮 Showing order selection for next turn");

            if (panelCombatMenu != null)
                panelCombatMenu.SetActive(true);

            if (panelShipCombat != null)
                panelShipCombat.SetActive(false);

            // Refresh ship counts — some may have been destroyed last turn
            UpdateCombatMenuData();

            // Pre-select the orders from the previous turn so players see their last choice
            ApplyToggleForOrder(currentOrder, false);
            ApplyToggleForOrder(currentOrderSideTwo, true);

            remainingTime = 15f;
            isTimerRunning = true;
        }

        /// <summary>
        /// Show combat over panel
        /// </summary>
        public void ShowCombatOverPanel()
        {
            Debug.Log("🏁 CombatUIManager: ShowCombatOverPanel called");

            if (panelCombatOver == null)
            {
                // 1. Try GameOverCanvas
                if (currentGameOverCanvas != null)
                {
                    panelCombatOver = FindUIElement(currentGameOverCanvas, "PanelCombatEnd");
                }

                // 2. Try CombatUICanvas
                if (panelCombatOver == null && currentCombatUICanvas != null)
                {
                    panelCombatOver = FindUIElement(currentCombatUICanvas, "PanelCombatEnd");
                }

                // 3. Last resort: Global search (expensive but safer if references failed)
                if (panelCombatOver == null)
                {
                    GameObject found = GameObject.Find("PanelCombatEnd");
                    if (found != null) panelCombatOver = found;
                }
            }

            if (panelCombatOver != null)
            {
                panelCombatOver.SetActive(true);

                // Ensure parent canvases are enabled
                Canvas parentCanvas = panelCombatOver.GetComponentInParent<Canvas>();
                if (parentCanvas != null) parentCanvas.enabled = true;

                Debug.Log($"✅ Combat over panel '{panelCombatOver.name}' shown in {parentCanvas?.name ?? "unknown canvas"}");

                // If it's the GameOverCanvas, ensure it's active
                if (currentGameOverCanvas != null) currentGameOverCanvas.SetActive(true);
            }
            else
            {
                Debug.LogError("❌ Cannot show combat over panel - NOT FOUND in any canvas or scene!");

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

        public static string LastCombatReport { get; private set; } = string.Empty;

        public void SetCombatOutcomeText(CombatData combatData)
        {
            if (combatData == null) return;

            if (combatOutcomeText == null && panelCombatOver != null)
                combatOutcomeText = FindComponentByName<TextMeshProUGUI>(panelCombatOver, "CombatOutcome");

            string report = BuildOutcomeReport(combatData);
            LastCombatReport = report;

            if (combatOutcomeText != null)
                combatOutcomeText.text = report;
        }

        private string BuildOutcomeReport(CombatData combatData)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Combat Report</b>");
            sb.AppendLine();
            AppendSideReport(sb,
                combatData.sideOneCiv, combatData.CivEnumSideOne,
                combatData.SideOneShipCons, combatData.CapturedShips, combatData.DestroyedShips);
            sb.AppendLine();
            AppendSideReport(sb,
                combatData.sideTwoCiv, combatData.CivEnumSideTwo,
                combatData.SideTwoShipCons, combatData.CapturedShips, combatData.DestroyedShips);
            return sb.ToString().TrimEnd();
        }

        private void AppendSideReport(StringBuilder sb, CivController civ, CivEnum civEnum,
            List<ShipController> ships, List<ShipController> allCaptured, List<ShipData> allDestroyed)
        {
            string civName = civ?.CivData?.CivLongName ?? civEnum.ToString();
            sb.AppendLine($"<b>{civName}</b>");

            var surviving = ships.Where(s => s != null && s.gameObject.activeInHierarchy
                                              && !s.ShipData.Distroyed && !s.ShipData.IsCaptured).ToList();
            var destroyed = allDestroyed != null
                ? allDestroyed.Where(sd => sd != null && sd.CivEnum == civEnum && !sd.IsScuttled).ToList()
                : new List<ShipData>();
            var scuttled = allDestroyed != null
                ? allDestroyed.Where(sd => sd != null && sd.CivEnum == civEnum && sd.IsScuttled).ToList()
                : new List<ShipData>();
            var retreated = ships.Where(s => s != null && !s.gameObject.activeInHierarchy
                                              && !s.ShipData.Distroyed && !s.ShipData.IsCaptured).ToList();
            var captured = allCaptured != null
                ? allCaptured.Where(s => s != null && ships.Contains(s)).ToList()
                : new List<ShipController>();

            sb.AppendLine($"  Surviving: {ShipGroupSummaryOrDash(surviving)}");
            sb.AppendLine($"  Combat Losses: {ShipDataGroupSummaryOrDash(destroyed)}");
            sb.AppendLine($"  Scuttled:  {ShipDataGroupSummaryOrDash(scuttled)}");
            sb.AppendLine($"  Retreated: {ShipGroupSummaryOrDash(retreated)}");
            sb.Append($"  Captured:  {ShipGroupSummaryOrDash(captured)}");
            sb.AppendLine();
        }

        private static string ShipGroupSummaryOrDash(List<ShipController> ships)
        {
            if (!ships.Any()) return "—";
            var groups = ships
                .GroupBy(s => new { s.ShipData.ShipType, s.ShipData.TechLevel })
                .OrderBy(g => g.Key.ShipType.ToString())
                .Select(g => $"{g.Key.ShipType} ({g.Key.TechLevel}) x{g.Count()}");
            return string.Join(", ", groups);
        }

        private static string ShipDataGroupSummaryOrDash(List<ShipData> ships)
        {
            if (!ships.Any()) return "—";
            var groups = ships
                .GroupBy(s => new { s.ShipType, s.TechLevel })
                .OrderBy(g => g.Key.ShipType.ToString())
                .Select(g => $"{g.Key.ShipType} ({g.Key.TechLevel}) x{g.Count()}");
            return string.Join(", ", groups);
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
            combatOutcomeText = null;
            timerText = null;
            engage = null;
            rush = null;
            retreat = null;
            formation = null;
            AttackTransports = null;
            capture = null;
            scuttle = null;
            engage2 = null;
            rush2 = null;
            retreat2 = null;
            formation2 = null;
            AttackTransports2 = null;
            capture2 = null;
            scuttle2 = null;

            sideOneCivName = null;
            sideTwoCivName = null;
            sideOneTechLevel = null;
            sideTwoTechLevel = null;
            s1Scouts = s1Destroyers = s1Cruisers = s1LtCruisers = s1HvyCruisers = s1Transports = s1Total = null;
            s2Scouts = s2Destroyers = s2Cruisers = s2LtCruisers = s2HvyCruisers = s2Transports = s2Total = null;

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
