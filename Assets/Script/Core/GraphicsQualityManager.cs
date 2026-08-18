using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    /// <summary>
    /// Manages graphics quality and resolution settings for high definition display
    /// </summary>
    public class GraphicsQualityManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        [Header("Resolution Settings")]
        [SerializeField] private bool forceHighDefinition = true;
        [SerializeField] private int targetWidth = 1920;
        [SerializeField] private int targetHeight = 1080;
        [SerializeField] private bool fullscreen = true;
        [SerializeField] private FullScreenMode fullscreenMode = FullScreenMode.MaximizedWindow;

        [Header("Quality Settings")]
        [SerializeField] private int qualityLevel = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
        [SerializeField] private int vSyncCount = 1; // 0=off, 1=60fps, 2=30fps
        [SerializeField] private int targetFrameRate = -1; // -1=unlimited
        [SerializeField] private int antiAliasing = 4; // 0, 2, 4, 8

        // PlayerPrefs Keys
        private const string QUALITY_LEVEL_KEY = "GraphicsQualityLevel";
        private const string FULLSCREEN_KEY = "GraphicsFullscreen";
        private const string VSYNC_KEY = "GraphicsVSyncCount";
        private const string TARGET_FRAMERATE_KEY = "GraphicsTargetFrameRate";
        private const string ANTIALIASING_KEY = "GraphicsAntiAliasing";
        private const string RESOLUTION_WIDTH_KEY = "GraphicsResolutionWidth";
        private const string RESOLUTION_HEIGHT_KEY = "GraphicsResolutionHeight";

        private void Awake()
        {
            ServiceLocator.Register<GraphicsQualityManager>(this);
            LoadSettings();
            ApplyGraphicsSettings();
        }

        private void LoadSettings()
        {
            qualityLevel = PlayerPrefs.GetInt(QUALITY_LEVEL_KEY, qualityLevel);
            fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, fullscreen ? 1 : 0) == 1;
            vSyncCount = PlayerPrefs.GetInt(VSYNC_KEY, vSyncCount);
            targetFrameRate = PlayerPrefs.GetInt(TARGET_FRAMERATE_KEY, targetFrameRate);
            antiAliasing = PlayerPrefs.GetInt(ANTIALIASING_KEY, antiAliasing);

            if (PlayerPrefs.HasKey(RESOLUTION_WIDTH_KEY) && PlayerPrefs.HasKey(RESOLUTION_HEIGHT_KEY))
            {
                targetWidth = PlayerPrefs.GetInt(RESOLUTION_WIDTH_KEY, targetWidth);
                targetHeight = PlayerPrefs.GetInt(RESOLUTION_HEIGHT_KEY, targetHeight);
            }

            fullscreenMode = fullscreen ? FullScreenMode.MaximizedWindow : FullScreenMode.Windowed;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(QUALITY_LEVEL_KEY, qualityLevel);
            PlayerPrefs.SetInt(FULLSCREEN_KEY, fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(VSYNC_KEY, vSyncCount);
            PlayerPrefs.SetInt(TARGET_FRAMERATE_KEY, targetFrameRate);
            PlayerPrefs.SetInt(ANTIALIASING_KEY, antiAliasing);
            PlayerPrefs.SetInt(RESOLUTION_WIDTH_KEY, targetWidth);
            PlayerPrefs.SetInt(RESOLUTION_HEIGHT_KEY, targetHeight);
            PlayerPrefs.Save();
        }

        private void Start()
        {
            // Double-check on start
            if (forceHighDefinition)
            {
                ApplyGraphicsSettings();
            }
        }

        public void ApplyGraphicsSettings()
        {
            // ✅ FIX: Validate quality level before applying
            int maxQualityLevel = QualitySettings.names.Length - 1;
            int validQualityLevel = Mathf.Clamp(qualityLevel, 0, maxQualityLevel);

            if (validQualityLevel != qualityLevel)
            {
                Debug.Log($"⚠️ Quality level {qualityLevel} is out of range. Available levels: 0-{maxQualityLevel}. Using {validQualityLevel} instead.");
            }

            // Set Quality Level
            if (QualitySettings.GetQualityLevel() != validQualityLevel)
            {
                QualitySettings.SetQualityLevel(validQualityLevel, true);
                Debug.Log($"✅ Quality Level set to: {QualitySettings.names[validQualityLevel]} (index {validQualityLevel})");
            }

            // Set Resolution
            if (forceHighDefinition)
            {
                // Use native resolution if available
                Resolution nativeRes = Screen.currentResolution;
                int width = targetWidth > 0 ? targetWidth : nativeRes.width;
                int height = targetHeight > 0 ? targetHeight : nativeRes.height;

                Screen.SetResolution(width, height, fullscreenMode, nativeRes.refreshRateRatio);
                Debug.Log($"✅ Resolution set to: {width}x{height} @ {nativeRes.refreshRateRatio}Hz ({fullscreenMode})");
            }

            // VSync
            QualitySettings.vSyncCount = vSyncCount;

            // Target Frame Rate
            Application.targetFrameRate = targetFrameRate;

            // Anti-Aliasing
            QualitySettings.antiAliasing = antiAliasing;
            Debug.Log($"✅ Anti-Aliasing: {antiAliasing}x MSAA");

            // Additional HD settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 150f;
            QualitySettings.lodBias = 2f; // Higher = better quality at distance

            Debug.Log("🎮 High Definition Graphics Settings Applied");
        }

        /// <summary>
        /// Call this to change quality at runtime (e.g., from settings menu)
        /// </summary>
        public void SetQualityLevel(int level)
        {
            qualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            ApplyGraphicsSettings();
            SaveSettings();
        }

        /// <summary>
        /// Change resolution at runtime
        /// </summary>
        public void SetResolution(int width, int height, bool fullscreen)
        {
            targetWidth = width;
            targetHeight = height;
            this.fullscreen = fullscreen;
            fullscreenMode = fullscreen ? FullScreenMode.MaximizedWindow : FullScreenMode.Windowed;
            ApplyGraphicsSettings();
            SaveSettings();
        }

        /// <summary>
        /// Toggle fullscreen at runtime, keeping the current resolution
        /// </summary>
        public void SetFullscreen(bool isFullscreen)
        {
            fullscreen = isFullscreen;
            fullscreenMode = isFullscreen ? FullScreenMode.MaximizedWindow : FullScreenMode.Windowed;
            ApplyGraphicsSettings();
            SaveSettings();
        }

        /// <summary>
        /// vSyncCount: 0=off, 1=every vblank, 2=every second vblank
        /// </summary>
        public void SetVSyncCount(int count)
        {
            vSyncCount = Mathf.Clamp(count, 0, 2);
            QualitySettings.vSyncCount = vSyncCount;
            SaveSettings();
            Debug.Log($"✅ VSync set to: {(vSyncCount == 0 ? "Off" : $"On ({vSyncCount}x)")}");
        }

        /// <summary>
        /// -1 for unlimited/uncapped frame rate
        /// </summary>
        public void SetTargetFrameRate(int fps)
        {
            targetFrameRate = fps;
            Application.targetFrameRate = targetFrameRate;
            SaveSettings();
        }

        /// <summary>
        /// MSAA level: 0, 2, 4, or 8
        /// </summary>
        public void SetAntiAliasing(int samples)
        {
            antiAliasing = samples;
            QualitySettings.antiAliasing = antiAliasing;
            SaveSettings();
        }

        // Optional: Detect and use native resolution
        public void UseNativeResolution()
        {
            Resolution native = Screen.currentResolution;
            SetResolution(native.width, native.height, true);
        }

        public int GetQualityLevel() => qualityLevel;
        public bool GetFullscreen() => fullscreen;
        public int GetVSyncCount() => vSyncCount;
        public int GetTargetFrameRate() => targetFrameRate;
        public int GetAntiAliasing() => antiAliasing;
        public int GetResolutionWidth() => targetWidth;
        public int GetResolutionHeight() => targetHeight;


        private void OnDestroy()
        {
            ServiceLocator.Unregister<GraphicsQualityManager>(); }
    }
}