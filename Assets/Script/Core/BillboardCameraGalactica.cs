using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    public class BillboardCameraGalactica : MonoBehaviour
    {
        private Camera cameraGal;

        void Start()
        {
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera.tag == "MainCamera")
                {
                    cameraGal = camera;
                }
            }
        }

        void LateUpdate()
        {
            transform.LookAt(cameraGal.transform, Vector3.up);
            transform.rotation = cameraGal.transform.rotation;
        }
    }
}