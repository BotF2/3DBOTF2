using Assets.Core;
using Assets.GamePlay;
using Assets.UI;
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
    public Camera mainCamera;  // ✅ Add this

    private void Awake()
    {
        Debug.Log("GalaxySceneInitializer: Setting galaxy references...");

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
        }

        // Pass to StarSysManager
        if (StarSysManager.Instance != null)
        {
            StarSysManager.Instance.SetGalaxyReferences(galaxyCenter, systemListContainer);
        }

        // Pass to StarSysMenuUIController
        if (StarSysMenuUIController.Instance != null)
        {
            StarSysMenuUIController.Instance.SetUIReferences(systemListContainer, canvasGalaxy.gameObject);
        }

        // Pass to SceneController
        if (SceneController.Instance != null)
        {
            SceneController.Instance.SetGalaxyReferences(galaxyCameraDragMoveZoom);
        }

        // ✅ Pass to PlayerDefinedTargetManager
        if (PlayerDefinedTargetManager.Instance != null)
        {
            PlayerDefinedTargetManager.Instance.SetGalaxyReferences(galaxyCenter, galaxyImage, mainCamera);
        }

        Debug.Log("GalaxySceneInitializer: All references set!");
    }
}