using UnityEngine;

namespace BOTF3D.Core
{
    public class BillboardCameraCombat : MonoBehaviour
    {
        private Camera cameraShip;

        void Start()
        {
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera.tag == "ShipCamera")
                {
                    cameraShip = camera;
                }
            }
        }

        void LateUpdate()
        {
            transform.LookAt(cameraShip.transform, Vector3.up);
            transform.rotation = cameraShip.transform.rotation;
        }
    }
}