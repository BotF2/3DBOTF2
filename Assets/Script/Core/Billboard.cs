using UnityEngine;
using BOTF3D.Galaxy;

namespace BOTF3D.Core
{
    public class Billboard : MonoBehaviour
    {
        private Camera theCam;

        void LateUpdate()
        {
            if (theCam == null)
            {
                // GalaxyCameraDragMoveZoom.Instance is the single authoritative galaxy camera for
                // THIS process (destroys duplicates in its own Awake, never DontDestroyOnLoad - see
                // that file). Scanning Camera.allCameras/tag "MainCamera" instead used to pick
                // whichever enabled camera enumerated last: on host in real multiplayer, a second
                // camera can be transiently active during scene load (e.g. a not-yet-unloaded menu
                // camera while waiting on a joining player), so the scan could latch onto the wrong
                // one and cache it forever, rotating this billboard to match a camera the host isn't
                // even looking through. Single player and joining clients only ever have one camera,
                // so they never hit this - which is why only the host saw the misalignment.
                theCam = GalaxyCameraDragMoveZoom.Instance != null
                    ? GalaxyCameraDragMoveZoom.Instance.GetComponent<Camera>()
                    : Camera.main;
                if (theCam == null) return;
            }

            transform.forward = theCam.transform.forward;
            transform.LookAt(transform.position + theCam.transform.rotation * Vector3.forward,
                theCam.transform.rotation * Vector3.up);
        }
    }
}