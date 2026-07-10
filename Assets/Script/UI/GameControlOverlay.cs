// Ignore Spelling: BOTF

using BOTF3D.Core;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    /// <summary>
    /// Persistent UI overlay that provides master volume control and game pause functionality.
    /// This GameObject should be marked as DontDestroyOnLoad and exist in all gameplay scenes.
    /// </summary>
    public class GameControlOverlay : MonoBehaviour
    {
        public static GameControlOverlay Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI volumeValueText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI stardateText; // Displays current stardate
        [SerializeField] private Button advanceTurnButton;

        [Header("Advance Turn Colors")]
        [SerializeField] private Color turnReadyColor = new Color(0.2f, 0.8f, 0.2f); // stands out — ready to accept a click
        [SerializeField] private Color turnProcessingColor = new Color(0.5f, 0.5f, 0.5f, 0.45f); // gray + transparent — turn is processing

        [Header("Turn Progress Bar")]
        [SerializeField] private Image turnProgressBar;
        [SerializeField] private GameObject progressBackground; // background panel behind the progress bar
        [SerializeField] private Color progressBarActiveColor = new Color(0.2f, 0.8f, 0.2f);    // green while turn runs
        [SerializeField] private Color progressBarIdleColor   = new Color(0.4f, 0.4f, 0.4f, 0.5f); // dim grey when waiting

        [Header("Turn Phase Display")]
        [SerializeField] private TextMeshProUGUI turnPhaseText;    // e.g. "READY" / "ADVANCING" / "ENCOUNTER"
        [SerializeField] private TooltipTrigger turnPhaseTrigger;  // TooltipTrigger on the turnPhaseText GO — drives hover description

        [Header("Pause Button Display")]
        [SerializeField] private Image pauseButtonImage; // Image icon (optional)
        [SerializeField] private Sprite pauseIcon; // Icon to show when game is running (unpaused)
        [SerializeField] private Sprite playIcon; // Icon to show when game is paused

        [Header("Pause Button Text (Localized)")]
        [SerializeField] private TextMeshProUGUI pauseButtonTextTMP; // Localized text label

        [SerializeField] private Button toggleOverlayButton; // Optional: button to show/hide the overlay
        [Header("Localization")]
        [SerializeField] private LocalizeStringEvent pauseResumeTextLocalizer;

        [Header("Settings")]
        [SerializeField] private bool startVisible = true;
        [SerializeField] private bool showInMainMenu = false; // Hide overlay in main menu
        [SerializeField] private bool showInCombatScene = false; // Hide overlay in combat (time pauses automatically)
        [SerializeField] private bool showIconAndText = true; // Show both icon and text together

        private bool isPaused = false;
        private bool isInMainMenu = true;
        private bool isInCombat = false;
        private bool isTogglingPause = false;
        // Tracks whether the player explicitly clicked Pause (distinct from clock being stopped between turns)
        private bool _playerPaused = false;
        private Coroutine flashCoroutine;

        // Cached references are no longer needed for singletons
        private int lastToggleFrame = -999;
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("✅ GameControlOverlay: Instance created and set to DontDestroyOnLoad");
            }
            else
            {
                Debug.LogWarning("⚠️ Duplicate GameControlOverlay detected - destroying");
                Destroy(gameObject);
                return;
            }

            InitializeUI();
        }

        private void Start()
        {
            // Retry checking managers in case they weren't ready in Awake
            StartCoroutine(CheckManagersReady());

            // Delay visibility update to ensure we're in the correct scene
            StartCoroutine(DelayedVisibilityUpdate());

            // Find LocalizeStringEvent if not assigned
            if (pauseResumeTextLocalizer == null)
            {
                pauseResumeTextLocalizer = GetComponentInChildren<LocalizeStringEvent>();
            }
            if (pauseResumeTextLocalizer == null)
            {
                Debug.LogError("GameControlOverlay: LocalizeStringEvent not found! Assign it in Inspector.");
            }

            // ✅ Wait for TimeManager before updating button state
            StartCoroutine(InitializeButtonStateWhenReady());
        }
        /// <summary>
        /// Keyboard shortcut support and stardate update
        /// </summary>
        private void Update()
        {
            // Update stardate display every frame (lightweight property read)
            UpdateStardateDisplay();

            // Press 'P' to toggle pause
            if (Input.GetKeyDown(KeyCode.P) && !isInMainMenu && !isInCombat)
            {
                Debug.Log("🎮 P key pressed - toggling pause");
                TogglePause();
            }

            // Press 'M' to toggle overlay visibility
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleOverlayVisibility();
            }
        }
        /// <summary>
        /// Wait for TimeManager to be ready before initializing button state
        /// </summary>
        private System.Collections.IEnumerator InitializeButtonStateWhenReady()
        {
            // Wait until TimeManager is available
            int attempts = 0;
            while (TimeManager.Instance == null && attempts < 50)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            if (TimeManager.Instance != null)
            {
                Debug.Log("✅ TimeManager ready - initializing button state");
                TimeManager.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
                TimeManager.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
                TimeManager.Instance.OnStardateChanged -= UpdateTurnProgress;
                TimeManager.Instance.OnStardateChanged += UpdateTurnProgress;
                UpdateButtonState();
                OnTurnPhaseChanged(TimeManager.Instance.TurnPhase);
            }
            else
            {
                Debug.LogError("❌ TimeManager not found after 5 seconds - button state not initialized");
            }
        }

        public void OnPauseButtonClicked()
        {
            if (TimeManager.Instance == null) return;
            _playerPaused = !_playerPaused;
            if (_playerPaused)
            {
                Time.timeScale = 0f; // freeze FixedUpdate — ships and coroutines stop
                TimeManager.Instance.PauseTime();
            }
            else
            {
                Time.timeScale = 1f;
                if (TimeManager.Instance.TurnPhase == TurnPhase.TurnProgression)
                    TimeManager.Instance.ResumeTime();
            }
            UpdateButtonState();
        }

        public void TogglePause()
        {
            int currentFrame = Time.frameCount;
            if (currentFrame == lastToggleFrame)
            {
                Debug.Log($"⚠️ TogglePause called AGAIN on frame {currentFrame} - ignoring double-click");
                return;
            }
            lastToggleFrame = currentFrame;

            if (TimeManager.Instance == null)
            {
                Debug.LogError("❌ GameControlOverlay: TimeManager.Instance is NULL");
                return;
            }
            if (isTogglingPause)
            {
                Debug.LogWarning("⚠️ TogglePause called while already toggling - ignoring");
                return;
            }

            isTogglingPause = true;
            _playerPaused = !_playerPaused;

            if (_playerPaused)
            {
                Time.timeScale = 0f; // freeze FixedUpdate — ships and all coroutines stop
                TimeManager.Instance.PauseTime();
                Debug.Log("🛑 Game PAUSED by player — timeScale=0");
            }
            else
            {
                Time.timeScale = 1f;
                // Only restart the turn clock if a turn was actually progressing
                if (TimeManager.Instance.TurnPhase == TurnPhase.TurnProgression)
                    TimeManager.Instance.ResumeTime();
                Debug.Log("▶️ Player pause lifted — timeScale=1");
            }

            StartCoroutine(UpdateButtonStateDelayed());
        }
        private System.Collections.IEnumerator UpdateButtonStateDelayed()
        {
            // Wait for end of frame so localization and UI layout complete
            yield return new WaitForEndOfFrame();

            UpdateButtonState();

            // Force canvas update
            if (pauseButtonTextTMP != null)
            {
                Canvas.ForceUpdateCanvases();
            }

            // ✅ Re-enable button after short delay (debounce)
            yield return new WaitForSecondsRealtime(0.2f); // Use real time (works when paused)
            isTogglingPause = false;
        }

        private void UpdateButtonState()
        {
            if (TimeManager.Instance == null)
            {
                Debug.LogWarning("⚠️ UpdateButtonState: TimeManager.Instance is NULL");
                return;
            }

            Debug.Log($"🔄 UpdateButtonState: _playerPaused={_playerPaused}");

            // Pause button label: "Resume" when player has explicitly paused, "Pause" otherwise
            if (pauseResumeTextLocalizer != null)
            {
                string key = _playerPaused ? "Resume" : "Pause";
                Debug.Log($"🔄 Setting key to '{key}'");
                pauseResumeTextLocalizer.StringReference.SetReference("StringTableCollection", key);
                pauseResumeTextLocalizer.RefreshString();
                if (pauseButtonTextTMP != null)
                    Debug.Log($"📝 Text is: '{pauseButtonTextTMP.text}'");
            }

            if (pauseButtonImage != null && pauseIcon != null && playIcon != null)
            {
                pauseButtonImage.sprite = _playerPaused ? playIcon : pauseIcon;
                pauseButtonImage.enabled = true;
                Debug.Log($"🖼️ Icon: {pauseButtonImage.sprite.name}");
            }

            // Re-evaluate which controls should be interactive
            SetControlsInteractable();
        }

        /// <summary>
        /// Check if managers are ready
        /// </summary>
        private System.Collections.IEnumerator CheckManagersReady()
        {
            int retries = 0;
            int maxRetries = 10; // Try for ~1 second

            while ((AudioManager.Instance == null || TimeManager.Instance == null) && retries < maxRetries)
            {
                yield return new WaitForSeconds(0.1f); // Wait 100ms
                retries++;
            }

            if (AudioManager.Instance == null)
            {
                Debug.LogError("❌ GameControlOverlay: Failed to find AudioManager after retries - volume control will not work");
            }
            else
            {
                // Refresh volume slider once AudioManager is ready
                if (masterVolumeSlider != null)
                {
                    float currentVolume = GetMasterVolume();
                    masterVolumeSlider.value = currentVolume;
                    UpdateVolumeText(currentVolume);
                }
            }

            if (TimeManager.Instance == null)
            {
                Debug.LogError("❌ GameControlOverlay: Failed to find TimeManager after retries - pause and stardate will not work");
            }
        }

        /// <summary>
        /// Wait one frame before updating visibility to ensure scene is fully loaded
        /// </summary>
        private System.Collections.IEnumerator DelayedVisibilityUpdate()
        {
            yield return null; // Wait one frame
            UpdateOverlayVisibility();
        }

        private void InitializeUI()
        {
            // ✅ Initialize master volume slider
            if (masterVolumeSlider != null)
            {
                // Get current master volume from AudioManager (0-1 range)
                float currentVolume = GetMasterVolume();
                masterVolumeSlider.minValue = 0f;
                masterVolumeSlider.maxValue = 1f;
                masterVolumeSlider.value = currentVolume;
                masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                UpdateVolumeText(currentVolume);
                Debug.Log($"GameControlOverlay: Volume slider initialized to {currentVolume:F2}");
            }
            else
            {
                Debug.LogWarning("GameControlOverlay: masterVolumeSlider not assigned in Inspector!");
            }
            // ✅ Initialize pause button
            if (pauseButton != null)
            {
                // ✅ CRITICAL: Remove ALL listeners before adding (prevents duplicates)
                pauseButton.onClick.RemoveAllListeners();

                // Set TargetGraphic if not set
                if (pauseButton.targetGraphic == null && pauseButtonImage != null)
                {
                    pauseButton.targetGraphic = pauseButtonImage;
                    Debug.Log("✅ Set PauseButton targetGraphic");
                }

                // Add listener ONCE
                pauseButton.onClick.AddListener(TogglePause);

                Debug.Log($"GameControlOverlay: Pause button initialized with {pauseButton.onClick.GetPersistentEventCount()} persistent listeners");
            }
            else
            {
                Debug.LogWarning("GameControlOverlay: pauseButton not assigned in Inspector!");
            }

            // ✅ Initialize advance turn button
            if (advanceTurnButton != null)
            {
                advanceTurnButton.onClick.RemoveAllListeners();
                advanceTurnButton.onClick.AddListener(OnAdvanceTurnClicked);
                SetAdvanceTurnProcessing(); // placeholder until real turn phase is known
            }

            // ✅ Initialize toggle overlay button (optional)
            if (toggleOverlayButton != null)
            {
                toggleOverlayButton.onClick.AddListener(ToggleOverlayVisibility);
            }

            // ✅ Don't set initial visibility here - let UpdateOverlayVisibility() handle it based on scene
            // This prevents conflicts between startVisible setting and scene-based visibility
            Debug.Log($"GameControlOverlay: UI initialized, waiting for scene-based visibility update");
        }

        /// <summary>
        /// Get master volume from AudioManager
        /// </summary>
        private float GetMasterVolume()
        {
            if (AudioManager.Instance != null)
            {
                return AudioManager.Instance.GetMasterVolume();
            }
            return PlayerPrefs.GetFloat("MasterVolume", 1f);
        }

        /// <summary>
        /// Set master volume on AudioManager
        /// </summary>
        private void SetMasterVolume(float volume)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(volume);
            }
        }

        /// <summary>
        /// Called when volume slider value changes
        /// </summary>
        private void OnVolumeChanged(float value)
        {
            SetMasterVolume(value);
            UpdateVolumeText(value);
            Debug.Log($"GameControlOverlay: Master volume set to {value:F2}");
        }

        /// <summary>
        /// Update the volume percentage text display
        /// </summary>
        private void UpdateVolumeText(float volume)
        {
            if (volumeValueText != null)
            {
                volumeValueText.text = $"{Mathf.RoundToInt(volume * 100)}%";
            }
        }

        /// <summary>
        /// Toggle overlay panel visibility
        /// </summary>
        public void ToggleOverlayVisibility()
        {
            if (overlayPanel != null)
            {
                overlayPanel.SetActive(!overlayPanel.activeSelf);
            }
        }

        /// <summary>
        /// Show the overlay panel
        /// </summary>
        public void ShowOverlay()
        {
            if (overlayPanel != null)
            {
                overlayPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the overlay panel
        /// </summary>
        public void HideOverlay()
        {
            if (overlayPanel != null)
            {
                overlayPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Update overlay visibility based on current scene
        /// </summary>
        private void UpdateOverlayVisibility()
        {
            // Check active scene name
            string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // ✅ CRITICAL: Check if GalaxyScene is LOADED (not just active)
            // PersistentScene is often the "active" scene, but GalaxyScene is loaded additively
            UnityEngine.SceneManagement.Scene galaxyScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("GalaxyScene");
            bool isGalaxySceneLoaded = galaxyScene.IsValid() && galaxyScene.isLoaded;

            isInMainMenu = activeSceneName.Contains("MainMenu") || activeSceneName.Contains("Lobby");
            isInCombat = activeSceneName.Contains("Combat");

            // ✅ Treat as galaxy gameplay if GalaxyScene is loaded
            bool isGalaxyScene = isGalaxySceneLoaded;
            bool isPersistentScene = activeSceneName.Contains("Persistent");

            // Default to showing overlay unless we're in a scene that should hide it
            bool shouldShowOverlay = true;

            // Hide in main menu if showInMainMenu is false
            if (isInMainMenu && !showInMainMenu)
            {
                shouldShowOverlay = false;
            }

            // Hide in combat scene if showInCombatScene is false
            if (isInCombat && !showInCombatScene)
            {
                shouldShowOverlay = false;
            }

            // Show overlay panel
            if (overlayPanel != null)
            {
                overlayPanel.SetActive(shouldShowOverlay);
                Debug.Log($"GameControlOverlay: OverlayPanel set to {shouldShowOverlay}");
            }

            // ✅ Show pause button and stardate when GalaxyScene is loaded
            bool showGameplayControls = isGalaxyScene;

            // Control pause button visibility (show when GalaxyScene loaded)
            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(showGameplayControls);
                Debug.Log($"GameControlOverlay: PauseButton set to {showGameplayControls}");
            }

            // Control advance turn button visibility (show when GalaxyScene loaded)
            if (advanceTurnButton != null)
            {
                advanceTurnButton.gameObject.SetActive(showGameplayControls);
            }

            // Control stardate text visibility (show when GalaxyScene loaded)
            if (stardateText != null)
            {
                stardateText.gameObject.SetActive(showGameplayControls);
                Debug.Log($"GameControlOverlay: StardateText set to {showGameplayControls}");
            }

            // Turn phase label — hidden until GalaxyScene is active
            if (turnPhaseText != null)
                turnPhaseText.gameObject.SetActive(showGameplayControls);

            // Progress bar and its background — hidden until GalaxyScene is active
            if (turnProgressBar != null)
                turnProgressBar.gameObject.SetActive(showGameplayControls);
            if (progressBackground != null)
                progressBackground.SetActive(showGameplayControls);

            Debug.Log($"GameControlOverlay visibility: ActiveScene={activeSceneName}, GalaxySceneLoaded={isGalaxySceneLoaded}, MainMenu={isInMainMenu}, Combat={isInCombat}, Persistent={isPersistentScene}, ShowOverlay={shouldShowOverlay}, ShowGameplayControls={showGameplayControls}");
        }

        /// <summary>
        /// Public method to force "unpause" (useful for scene transitions)
        /// </summary>
        public void ForceUnpause()
        {
            if (isPaused)
            {
                isPaused = false;
                Time.timeScale = 1f;
                Debug.Log("GameControlOverlay: Force unpaused");
            }
        }

        /// <summary>
        /// Get current pause state
        /// </summary>
        public bool IsPaused()
        {
            return isPaused;
        }


        public void TestSetToPause()
        {
            if (pauseResumeTextLocalizer != null)
            {
                pauseResumeTextLocalizer.StringReference.SetReference("StringTableCollection", "Pause");
                pauseResumeTextLocalizer.RefreshString();
                Debug.Log($"Test: Set to 'Pause', text is now: '{pauseButtonTextTMP.text}'");
            }
        }

        public void TestSetToResume()
        {
            if (pauseResumeTextLocalizer != null)
            {
                pauseResumeTextLocalizer.StringReference.SetReference("StringTableCollection", "Resume");
                pauseResumeTextLocalizer.RefreshString();
                Debug.Log($"Test: Set to 'Resume', text is now: '{pauseButtonTextTMP.text}'");
            }
        }
        /// <summary>
        /// Update stardate text display from TimeManager
        /// </summary>
        private void UpdateStardateDisplay()
        {
            if (stardateText == null) return;

            if (!stardateText.gameObject.activeInHierarchy) return;

            // Get current stardate from TimeManager
            string label = BOTF3D.Core.Loc.Get("Stardate", "Stardate");
            if (TimeManager.Instance != null)
                stardateText.text = $"{label}: {TimeManager.Instance.currentStardate}";
            else
                stardateText.text = $"{label}: --";
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
                TimeManager.Instance.OnStardateChanged += UpdateTurnProgress;
            }
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
                TimeManager.Instance.OnStardateChanged -= UpdateTurnProgress;
            }
        }

        private void OnAdvanceTurnClicked()
        {
            if (TimeManager.Instance == null) return;
            if (TimeManager.Instance.TurnPhase != TurnPhase.InterTurn) return;
            TimeManager.Instance.AdvanceTurn();
        }

        private void OnTurnPhaseChanged(TurnPhase phase)
        {
            if (advanceTurnButton == null) return;

            switch (phase)
            {
                case TurnPhase.InterTurn:
                    SetAdvanceTurnColor(turnReadyColor); // stands out — ready to accept a click
                    if (turnProgressBar != null)
                    {
                        turnProgressBar.fillAmount = 1f;
                        turnProgressBar.color = progressBarActiveColor;
                    }
                    break;
                case TurnPhase.TurnProgression:
                    SetAdvanceTurnProcessing(); // gray + transparent — turn is processing, button looks inactive
                    if (turnProgressBar != null)
                    {
                        turnProgressBar.color = progressBarActiveColor;
                        UpdateTurnProgress();
                    }
                    break;
                case TurnPhase.EncounterResolution:
                    break;
            }

            UpdateTurnPhaseText(phase);
            SetControlsInteractable();
        }

        private void UpdateTurnPhaseText(TurnPhase phase)
        {
            if (turnPhaseText == null) return;

            StopFlash();

            switch (phase)
            {
                case TurnPhase.InterTurn:
                    turnPhaseText.text = "READY";
                    turnPhaseText.enabled = true;
                    turnPhaseTrigger?.SetContent(
                        "Awaiting orders. Issue commands to fleets and systems, then click Turn.");
                    break;
                case TurnPhase.TurnProgression:
                    turnPhaseText.text = "ADVANCING";
                    turnPhaseTrigger?.SetContent(
                        "Stardate advancing — fleets are moving and production is underway.");
                    flashCoroutine = StartCoroutine(FlashText());
                    break;
                case TurnPhase.EncounterResolution:
                    turnPhaseText.text = "ENCOUNTER";
                    turnPhaseText.enabled = true;
                    turnPhaseTrigger?.SetContent(
                        "An event requires your attention before time can continue.");
                    break;
            }
        }

        private void StopFlash()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            if (turnPhaseText != null)
                turnPhaseText.enabled = true;
        }

        private System.Collections.IEnumerator FlashText()
        {
            while (true)
            {
                turnPhaseText.enabled = true;
                yield return new WaitForSecondsRealtime(0.6f);
                turnPhaseText.enabled = false;
                yield return new WaitForSecondsRealtime(0.4f);
            }
        }

        /// <summary>
        /// Refresh interactability of all controls except the pause button.
        /// The advance turn button is enabled only during InterTurn and only when
        /// the player has not explicitly paused. The volume slider is disabled only
        /// during an explicit player pause so the player can still adjust it
        /// while issuing orders between turns.
        /// </summary>
        private void SetControlsInteractable()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.interactable = !_playerPaused;

            if (advanceTurnButton != null)
                advanceTurnButton.interactable = !_playerPaused
                    && (TimeManager.Instance?.TurnPhase == TurnPhase.InterTurn);
        }

        private void UpdateTurnProgress()
        {
            if (turnProgressBar == null || TimeManager.Instance == null) return;
            int perTurn = TimeManager.Instance.StarDatesPerTurn;
            if (perTurn <= 0) return;
            int elapsed = TimeManager.Instance.currentStardate % perTurn;
            turnProgressBar.fillAmount = (float)elapsed / perTurn;
        }

        private void SetAdvanceTurnColor(Color color)
        {
            if (advanceTurnButton == null) return;
            var cb = advanceTurnButton.colors;
            cb.normalColor = color;
            cb.highlightedColor = color * 1.2f;
            cb.disabledColor = turnProcessingColor;
            advanceTurnButton.colors = cb;
        }

        /// <summary>Grays out and fades the button while a turn is processing — Selectable renders
        /// disabledColor (not normalColor) whenever interactable is false, so this is what actually
        /// makes the button look inactive during TurnProgression.</summary>
        private void SetAdvanceTurnProcessing()
        {
            if (advanceTurnButton == null) return;
            var cb = advanceTurnButton.colors;
            cb.normalColor = turnProcessingColor;
            cb.highlightedColor = turnProcessingColor;
            cb.disabledColor = turnProcessingColor;
            advanceTurnButton.colors = cb;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Clear any explicit player pause on scene transition and restore normal time
            _playerPaused = false;
            Time.timeScale = 1f;
            UpdateOverlayVisibility();

            if (scene.name.Contains("MainMenu") || scene.name.Contains("Lobby"))
                ForceUnpause();
        }
    }
}
