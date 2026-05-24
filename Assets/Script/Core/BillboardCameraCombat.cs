using UnityEngine;
using UnityEngine.SceneManagement;

namespace BOTF3D.Core
{
    /// <summary>
    /// Makes UI elements (like healthbars) face the combat camera
    /// ONLY runs in CombatScene where ShipCamera exists
    /// </summary>
    public class BillboardCameraCombat : MonoBehaviour
    {
        private Camera cameraShip;
        private bool isInCombatScene;
        private bool sceneChecked = false; // ✅ ADD: Track if we've checked the scene

        void Start()
        {
            CheckSceneAndInitialize();
        }

        // ✅ NEW: Centralized scene check
        private void CheckSceneAndInitialize()
        {
            if (sceneChecked) return; // Already checked

            // ✅ Check if we're in combat scene
            Scene currentScene = gameObject.scene;
            isInCombatScene = (currentScene.name == "CombatScene" || currentScene.name == "SpaceCombatScene");

            if (!isInCombatScene)
            {
                Debug.Log($"BillboardCameraCombat on '{gameObject.name}' is in '{currentScene.name}' (not combat). Disabling.");
                enabled = false; // Disable this component
                sceneChecked = true;
                return;
            }

            sceneChecked = true;

            // ✅ Find combat camera
            FindCombatCamera();
        }

        private void FindCombatCamera()
        {
            // ✅ Don't search if not in combat scene
            if (!isInCombatScene)
            {
                return;
            }

            // Search for camera with "ShipCamera" tag
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera.CompareTag("ShipCamera"))
                {
                    cameraShip = camera;
                    Debug.Log($"BillboardCameraCombat: Found ShipCamera '{camera.name}'");
                    return;
                }
            }

            // Fallback: Search for ShipCombatCameraController
            if (cameraShip == null && BOTF3D.GamePlay.ShipCombatCameraController.Instance != null)
            {
                cameraShip = BOTF3D.GamePlay.ShipCombatCameraController.Instance.GetComponent<Camera>();

                if (cameraShip != null)
                {
                    Debug.Log($"BillboardCameraCombat: Found camera via ShipCombatCameraController");
                }
            }

            if (cameraShip == null)
            {
                Debug.LogWarning($"BillboardCameraCombat: ShipCamera not found in CombatScene yet. Will retry.");
            }
        }

        void LateUpdate()
        {
            // ✅ Don't run if not in combat scene
            if (!isInCombatScene) return;

            // ✅ Try to find camera if missing (in combat scene only)
            if (cameraShip == null)
            {
                FindCombatCamera();
                return; // Don't billboard until camera found
            }

            transform.LookAt(cameraShip.transform, Vector3.up);
            transform.rotation = cameraShip.transform.rotation;
        }

        private void OnEnable()
        {
            // ✅ Check scene first before trying to find camera
            CheckSceneAndInitialize();
        }
    }
}