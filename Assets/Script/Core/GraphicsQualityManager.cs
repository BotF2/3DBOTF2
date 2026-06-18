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
        [SerializeField] private FullScreenMode fullscreenMode = FullScreenMode.ExclusiveFullScreen;

        [Header("Quality Settings")]
        [SerializeField] private int qualityLevel = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
        [SerializeField] private int vSyncCount = 1; // 0=off, 1=60fps, 2=30fps
        [SerializeField] private int targetFrameRate = -1; // -1=unlimited
        [SerializeField] private int antiAliasing = 4; // 0, 2, 4, 8

        private void Awake()
        {
            ServiceLocator.Register<GraphicsQualityManager>(this);
            ApplyGraphicsSettings();
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
        }

        /// <summary>
        /// Change resolution at runtime
        /// </summary>
        public void SetResolution(int width, int height, bool fullscreen)
        {
            targetWidth = width;
            targetHeight = height;
            this.fullscreen = fullscreen;
            ApplyGraphicsSettings();
        }

        // Optional: Detect and use native resolution
        public void UseNativeResolution()
        {
            Resolution native = Screen.currentResolution;
            SetResolution(native.width, native.height, true);
        }
    

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GraphicsQualityManager>(); }
    }
}