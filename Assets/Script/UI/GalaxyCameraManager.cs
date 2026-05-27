using UnityEngine;
using UnityEngine.EventSystems;

namespace BOTF3D.UI
{
    /// <summary>
    /// Manages galaxy camera setup and event system configuration.
    /// Handles camera assignment to canvas and event system.
    /// </summary>
    public class GalaxyCameraManager
    {
        private readonly Canvas parentCanvas;
        private Camera galaxyEventCamera;

        public Camera GalaxyEventCamera => galaxyEventCamera;

        public GalaxyCameraManager(Canvas parentCanvas, Camera initialCamera)
        {
            this.parentCanvas = parentCanvas;
            this.galaxyEventCamera = initialCamera;
        }

        /// <summary>
        /// Initialize galaxy camera and assign to canvas
        /// </summary>
        public void InitializeGalaxyCamera()
        {
            Debug.Log("GalaxyCameraManager: Initializing galaxy camera");

            // Find camera if not assigned
            if (galaxyEventCamera == null)
            {
                galaxyEventCamera = FindGalaxyCamera();
            }

            if (galaxyEventCamera == null)
            {
                Debug.LogError("❌ GalaxyCameraManager: No galaxy camera found!");
                return;
            }

            // Assign camera to canvas
            if (parentCanvas != null)
            {
                parentCanvas.worldCamera = galaxyEventCamera;
                Debug.Log($"✅ GalaxyCameraManager: Assigned camera '{galaxyEventCamera.name}' to canvas");
            }
            else
            {
                Debug.LogWarning("⚠️ GalaxyCameraManager: parentCanvas is null");
            }

            // Configure event system
            ConfigureEventSystem();
        }

        /// <summary>
        /// Find the galaxy camera in the scene
        /// </summary>
        private Camera FindGalaxyCamera()
        {
            Debug.Log("GalaxyCameraManager: Searching for galaxy camera");

            // Try to find by name
            GameObject cameraGO = GameObject.Find("Galaxy3DCamera");
            if (cameraGO != null)
            {
                Camera cam = cameraGO.GetComponent<Camera>();
                if (cam != null)
                {
                    Debug.Log($"✅ Found galaxy camera by name: {cameraGO.name}");
                    return cam;
                }
            }

            // Try to find by tag
            cameraGO = GameObject.FindGameObjectWithTag("GalaxyCamera");
            if (cameraGO != null)
            {
                Camera cam = cameraGO.GetComponent<Camera>();
                if (cam != null)
                {
                    Debug.Log($"✅ Found galaxy camera by tag: {cameraGO.name}");
                    return cam;
                }
            }

            // Fallback to main camera
            Debug.LogWarning("⚠️ GalaxyCameraManager: Using Camera.main as fallback");
            return Camera.main;
        }

        /// <summary>
        /// Configure event system for UI interactions
        /// </summary>
        private void ConfigureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                Debug.LogWarning("⚠️ GalaxyCameraManager: No EventSystem found in scene");
                return;
            }

            // Get or add StandaloneInputModule
            var inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("✅ GalaxyCameraManager: Added StandaloneInputModule to EventSystem");
            }

            Debug.Log("✅ GalaxyCameraManager: Event system configured");
        }

        /// <summary>
        /// Diagnose camera setup
        /// </summary>
        public void DiagnoseCameraSetup()
        {
            Debug.Log("=== GalaxyCameraManager: Camera Diagnostic ===");

            if (galaxyEventCamera != null)
            {
                Debug.Log($"  Galaxy Camera: {galaxyEventCamera.name}");
                Debug.Log($"  Camera Active: {galaxyEventCamera.gameObject.activeInHierarchy}");
                Debug.Log($"  Camera Enabled: {galaxyEventCamera.enabled}");
            }
            else
            {
                Debug.LogWarning("  Galaxy Camera: NULL");
            }

            if (parentCanvas != null)
            {
                Debug.Log($"  Canvas: {parentCanvas.name}");
                Debug.Log($"  Canvas Render Mode: {parentCanvas.renderMode}");
                Debug.Log($"  Canvas Camera: {parentCanvas.worldCamera?.name ?? "NULL"}");
            }
            else
            {
                Debug.LogWarning("  Canvas: NULL");
            }

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                Debug.Log($"  EventSystem: {eventSystem.name}");
                Debug.Log($"  EventSystem Current: {EventSystem.current?.name ?? "NULL"}");
            }
            else
            {
                Debug.LogWarning("  EventSystem: NULL");
            }

            Debug.Log("=== End Camera Diagnostic ===");
        }

        /// <summary>
        /// Set galaxy event camera
        /// </summary>
        public void SetGalaxyEventCamera(Camera camera)
        {
            galaxyEventCamera = camera;

            if (parentCanvas != null && camera != null)
            {
                parentCanvas.worldCamera = camera;
                Debug.Log($"✅ GalaxyCameraManager: Camera set to {camera.name}");
            }
        }
    }
}
