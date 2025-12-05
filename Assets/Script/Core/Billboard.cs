using UnityEngine;

namespace Assets.Core
{
    public class Billboard : MonoBehaviour
    {
        // [SerializeField]
        private Camera theCam;

        void LateUpdate()
        {
            if (theCam == null)
                theCam = Camera.main;
            else
            {
                transform.forward = Camera.main.transform.forward;
                transform.LookAt(transform.position + theCam.transform.rotation * Vector3.forward,
                    theCam.transform.rotation * Vector3.up);
            }
        }
    }
}