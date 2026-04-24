// Ignore Spelling: BOTF

using BOTF3D.Core;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

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
        private bool isTogglingPause = false; // ✅ Add debounce flag

        // Cached references to avoid reflection look-ups every frame
        private object audioManagerInstance;
        private object timeManagerInstance;
        private MethodInfo audioGetMasterVolumeMethod;
        private MethodInfo audioSetMasterVolumeMethod;
        private PropertyInfo timeCurrentStardateProperty; // For reading stardate
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

            CacheManagerReferences();
            InitializeUI();
        }

        private void Start()
        {
            // Retry caching managers in case they weren't ready in Awake
            StartCoroutine(RetryCachingManagers());

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
            // Re-cache managers if they weren't available at startup
            if (audioManagerInstance == null || timeManagerInstance == null)
            {
                CacheManagerReferences();
            }

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
                UpdateButtonState();
            }
            else
            {
                Debug.LogError("❌ TimeManager not found after 5 seconds - button state not initialized");
            }
        }

        public void OnPauseButtonClicked()
        {
            if (!TimeManager.Instance.timeRunning)
            {
                TimeManager.Instance.ResumeTime();
            }
            else
            {
                TimeManager.Instance.PauseTime();
            }

            UpdateButtonState();
        }
        public void TogglePause()
        {
            int currentFrame = Time.frameCount;

            // ✅ Ignore if called on the same frame
            if (currentFrame == lastToggleFrame)
            {
                Debug.Log($"⚠️ TogglePause called AGAIN on frame {currentFrame} - ignoring double-click");
                return;
            }

            lastToggleFrame = currentFrame;
            Debug.Log($"🎯 TogglePause called on frame {currentFrame}");
            if (TimeManager.Instance == null)
            {
                Debug.LogError("❌ GameControlOverlay: TimeManager.Instance is NULL");
                return;
            }

            // ✅ Debounce: Prevent multiple clicks in quick succession
            if (isTogglingPause)
            {
                Debug.LogWarning("⚠️ TogglePause called while already toggling - ignoring");
                return;
            }

            isTogglingPause = true;

            // Toggle based on current state
            if (TimeManager.Instance.IsPaused)
            {
                TimeManager.Instance.ResumeTime();
                Debug.Log("▶️ Game RESUMED");
            }
            else
            {
                TimeManager.Instance.PauseTime();
                Debug.Log("🛑 Game PAUSED");
            }

            Debug.Log($"🔄 After toggle: IsPaused={TimeManager.Instance.IsPaused}");

            // ✅ Wait one frame for localization and layout to update
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
            yield return new WaitForSecondsRealtime(0.2f); // Use realtime (works when paused)
            isTogglingPause = false;
        }

        private void UpdateButtonState()
        {
            if (TimeManager.Instance == null)
            {
                Debug.LogWarning("⚠️ UpdateButtonState: TimeManager.Instance is NULL");
                return;
            }

            bool isPaused = TimeManager.Instance.IsPaused;

            Debug.Log($"🔄 UpdateButtonState: isPaused={isPaused}");

            // Update localization key
            if (pauseResumeTextLocalizer != null)
            {
                string key = isPaused ? "Resume" : "Pause";

                Debug.Log($"🔄 Setting key to '{key}'");

                pauseResumeTextLocalizer.StringReference.SetReference("StringTableCollection", key);
                pauseResumeTextLocalizer.RefreshString();

                if (pauseButtonTextTMP != null)
                {
                    Debug.Log($"📝 Text is: '{pauseButtonTextTMP.text}'");
                }
            }

            // Update icon
            if (pauseButtonImage != null && pauseIcon != null && playIcon != null)
            {
                pauseButtonImage.sprite = isPaused ? playIcon : pauseIcon;
                pauseButtonImage.enabled = true;
                Debug.Log($"🖼️ Icon: {pauseButtonImage.sprite.name}");
            }
        }

        /// <summary>
        /// Retry caching managers if they weren't available in Awake
        /// </summary>
        private System.Collections.IEnumerator RetryCachingManagers()
        {
            int retries = 0;
            int maxRetries = 10; // Try for ~1 second

            while ((audioManagerInstance == null || timeManagerInstance == null) && retries < maxRetries)
            {
                yield return new WaitForSeconds(0.1f); // Wait 100ms
                CacheManagerReferences();
                retries++;
            }

            if (audioManagerInstance == null)
            {
                Debug.LogError("❌ GameControlOverlay: Failed to find AudioManager after retries - volume control will not work");
            }

            if (timeManagerInstance == null)
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

        /// <summary>
        /// Cache references to AudioManager and TimeManager using reflection to avoid assembly issues
        /// </summary>
        private void CacheManagerReferences()
        {
            // Find AudioManager
            var audioManagerType = System.Type.GetType("BOTF3D.Audio.AudioManager, Assembly-CSharp");
            if (audioManagerType != null)
            {
                // Try to get Instance as a property first
                var instanceProperty = audioManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    audioManagerInstance = instanceProperty.GetValue(null);
                }
                else
                {
                    // If not a property, try as a field
                    var instanceField = audioManagerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceField != null)
                    {
                        audioManagerInstance = instanceField.GetValue(null);
                    }
                }

                if (audioManagerInstance != null)
                {
                    audioGetMasterVolumeMethod = audioManagerType.GetMethod("GetMasterVolume");
                    audioSetMasterVolumeMethod = audioManagerType.GetMethod("SetMasterVolume");
                    Debug.Log("✅ GameControlOverlay: AudioManager cached successfully");
                }
                else
                {
                    // ✅ Change to Info level since this is expected during startup
                    Debug.Log("⏳ GameControlOverlay: AudioManager not ready yet - will retry");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ GameControlOverlay: AudioManager type not found");
            }

            // Find TimeManager
            var timeManagerType = System.Type.GetType("BOTF3D.Core.TimeManager, Assembly-CSharp");
            if (timeManagerType != null)
            {
                // Try to get Instance as a property first
                var instanceProperty = timeManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    timeManagerInstance = instanceProperty.GetValue(null);
                }
                else
                {
                    // If not a property, try as a field
                    var instanceField = timeManagerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceField != null)
                    {
                        timeManagerInstance = instanceField.GetValue(null);
                    }
                }
                if (timeManagerInstance != null)
                {
                    timeCurrentStardateProperty = timeManagerType.GetProperty("currentStardate");
                    Debug.Log("✅ GameControlOverlay: TimeManager cached successfully");
                }
                else
                {
                    // ✅ Change to Info level
                    Debug.Log("⏳ GameControlOverlay: TimeManager not ready yet - will retry");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ GameControlOverlay: TimeManager type not found");
            }
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
            // ✅ Initialize pause button
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(TogglePause);
                //UpdatePauseButtonText();
                Debug.Log("GameControlOverlay: Pause button initialized");
            }
            else
            {
                Debug.LogWarning("GameControlOverlay: pauseButton not assigned in Inspector!");
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
        /// Get master volume from AudioManager using reflection
        /// </summary>
        private float GetMasterVolume()
        {
            if (audioManagerInstance != null && audioGetMasterVolumeMethod != null)
            {
                return (float)audioGetMasterVolumeMethod.Invoke(audioManagerInstance, null);
            }
            return PlayerPrefs.GetFloat("MasterVolume", 1f);
        }

        /// <summary>
        /// Set master volume on AudioManager using reflection
        /// </summary>
        private void SetMasterVolume(float volume)
        {
            if (audioManagerInstance != null && audioSetMasterVolumeMethod != null)
            {
                audioSetMasterVolumeMethod.Invoke(audioManagerInstance, new object[] { volume });
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

            // Control stardate text visibility (show when GalaxyScene loaded)
            if (stardateText != null)
            {
                stardateText.gameObject.SetActive(showGameplayControls);
                Debug.Log($"GameControlOverlay: StardateText set to {showGameplayControls}");
            }

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
                //if (timeManagerInstance != null && timeResumeMethod != null)
                //{
                //    timeResumeMethod.Invoke(timeManagerInstance, null);
                //}
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
            if (timeManagerInstance != null && timeCurrentStardateProperty != null)
            {
                int currentStardate = (int)timeCurrentStardateProperty.GetValue(timeManagerInstance);
                stardateText.text = $"Stardate: {currentStardate}";
            }
            else
            {
                stardateText.text = "Stardate: --";
            }
        }

        private void OnEnable()
        {
            // Subscribe to scene loaded event to update visibility
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            // Unsubscribe from scene loaded event
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            UpdateOverlayVisibility();

            // Force unpause when returning to main menu
            if (scene.name.Contains("MainMenu") || scene.name.Contains("Lobby"))
            {
                ForceUnpause();
            }
        }
    }
}
