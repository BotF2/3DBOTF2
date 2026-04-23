using BOTF3D.Core;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using System.Collections;
using UnityEngine;

public class GalaxySceneInitializer : MonoBehaviour
{
    [Header("Assign All Galaxy Scene References")]
    public GameObject galaxyCenter;
    public GameObject galaxyImage;
    public Canvas canvasGalaxy;
    public GameObject fleetListContainer;
    public GameObject systemListContainer;
    public GameObject galaxyCameraDragMoveZoom;
    public Camera mainCamera;

    private bool isInitialized = false;

    private void Awake()
    {
        Debug.Log("GalaxySceneInitializer: Awake - starting initialization sequence...");

        // ✅ Start coroutine to set references after 2 frames
        // Per copilot-instructions: wait for UI ownership normalization
        StartCoroutine(InitializeGalaxyReferencesAfterDelay());
    }

    /// <summary>
    /// Wait 2 frames before setting galaxy references to ensure scene is fully loaded
    /// Per copilot-instructions.md: "wait two frames (yield return null twice) 
    /// before normalizing UI ownership and rebuilding lists"
    /// </summary>
    private IEnumerator InitializeGalaxyReferencesAfterDelay()
    {
        if (isInitialized)
        {
            Debug.Log("GalaxySceneInitializer: Already initialized - skipping");
            yield break;
        }

        Debug.Log("GalaxySceneInitializer: Waiting 2 frames for scene stabilization...");

        // ✅ Wait two frames (from copilot-instructions.md)
        yield return null;
        yield return null;

        Debug.Log("GalaxySceneInitializer: Scene stabilized - setting galaxy references...");

        // Get MainCamera if not assigned
        if (mainCamera == null)
        {
            var mainCameraGO = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCameraGO != null)
                mainCamera = mainCameraGO.GetComponent<Camera>();
        }

        // Pass to FleetManager
        if (FleetManager.Instance != null)
        {
            FleetManager.Instance.SetGalaxyReferences(galaxyCenter, galaxyImage, canvasGalaxy, fleetListContainer);
            Debug.Log("  ✅ FleetManager references set");
        }
        else
        {
            Debug.LogError("  ❌ FleetManager.Instance is NULL!");
        }

        // Pass to StarSysManager
        if (StarSysManager.Instance != null)
        {
            StarSysManager.Instance.SetGalaxyReferences(galaxyCenter, systemListContainer);
            Debug.Log("  ✅ StarSysManager references set");
        }
        else
        {
            Debug.LogError("  ❌ StarSysManager.Instance is NULL!");
        }

        // Pass to StarSysMenuUIController
        if (StarSysMenuUIController.Instance != null)
        {
            StarSysMenuUIController.Instance.SetUIReferences(systemListContainer, canvasGalaxy.gameObject);
            Debug.Log("  ✅ StarSysMenuUIController references set");
        }
        else
        {
            Debug.LogError("  ❌ StarSysMenuUIController.Instance is NULL!");
        }

        // Pass to SceneController
        if (SceneController.Instance != null)
        {
            SceneController.Instance.SetGalaxyReferences(galaxyCameraDragMoveZoom);
            Debug.Log("  ✅ SceneController references set");
        }
        else
        {
            Debug.LogError("  ❌ SceneController.Instance is NULL!");
        }

        // ✅ Pass to PlayerDefinedTargetManager
        if (PlayerDefinedTargetManager.Instance != null)
        {
            PlayerDefinedTargetManager.Instance.SetGalaxyReferences(galaxyCenter, galaxyImage, mainCamera);
            Debug.Log("  ✅ PlayerDefinedTargetManager references set");
        }
        else
        {
            Debug.LogWarning("  ⚠️ PlayerDefinedTargetManager.Instance is NULL (may not exist yet)");
        }

        isInitialized = true;
        Debug.Log("✅ GalaxySceneInitializer: All references set and initialization complete!");
    }
}