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

        void LateUpdate()
        {
            if (cameraGal == null)
            {
                // See Billboard.cs for the full rationale: GalaxyCameraDragMoveZoom.Instance is the
                // single authoritative galaxy camera for this process, unlike scanning
                // Camera.allCameras for a "MainCamera" tag, which could latch onto the wrong camera
                // on host in multiplayer.
                cameraGal = GalaxyCameraDragMoveZoom.Instance != null
                    ? GalaxyCameraDragMoveZoom.Instance.GetComponent<Camera>()
                    : Camera.main;
                if (cameraGal == null) return;
            }

            transform.LookAt(cameraGal.transform, Vector3.up);
            transform.rotation = cameraGal.transform.rotation;
        }
    }
}