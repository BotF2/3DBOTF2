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

        void Start()
        {
            // ✅ Check if we're in combat scene
            Scene currentScene = gameObject.scene;
            isInCombatScene = (currentScene.name == "CombatScene");

            if (!isInCombatScene)
            {
                Debug.LogWarning($"BillboardCameraCombat on '{gameObject.name}' is NOT in CombatScene (in '{currentScene.name}'). Disabling.");
                enabled = false; // Disable this component
                return;
            }

            // ✅ Find combat camera
            FindCombatCamera();
        }

        private void FindCombatCamera()
        {
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
                Debug.LogWarning($"BillboardCameraCombat: ShipCamera not found yet (might not be active). Will retry each frame.");
            }
        }

        void LateUpdate()
        {
            // ✅ Simple null guard
            if (cameraShip == null)
                return;

            transform.LookAt(cameraShip.transform, Vector3.up);
            transform.rotation = cameraShip.transform.rotation;
        }

        private void OnEnable()
        {
            // ✅ Try to find camera when re-enabled
            if (cameraShip == null)
            {
                FindCombatCamera();
            }
        }
    }
}