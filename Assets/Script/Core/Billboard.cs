using UnityEngine;

namespace BOTF3D.Core
{
    public class Billboard : MonoBehaviour
    {
        // [SerializeField]
        private Camera theCam;
        void Start()
        {
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera.tag == "MainCamera")
                {
                    theCam = camera;
                }
            }
        }
        void LateUpdate()
        {
            if (theCam == null)
                theCam = Camera.main;
            else
            {
                transform.forward = theCam.transform.forward;
                transform.LookAt(transform.position + theCam.transform.rotation * Vector3.forward,
                    theCam.transform.rotation * Vector3.up);
            }
        }
    }
}