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

        // A DropLine (see MapLineFixed) is drawn with a LineRenderer in world space, anchored to
        // this object's own (non-rotating) transform.position. Any direct child with a non-zero
        // RectTransform.anchoredPosition - i.e. offset from this billboard's own pivot rather than
        // centered on it - rotates along with LookAt below every frame, so it visually swings away
        // from that fixed DropLine anchor as the viewing camera's angle changes. Every multiplayer
        // client aims its own galaxy camera independently, so the drift is angle-dependent and
        // shows up differently (or not at all) per client - exactly how BlackHoleSagA's
        // CanvasBlackHoleSagA child was found with a stray {-1.2, 0.5} offset instead of {0, 0}.
        // Logged once here instead of silently auto-correcting, so a bad offset is caught loudly in
        // testing rather than only ever seen as an unexplained client-only visual glitch.
        private void Start()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform childRect = transform.GetChild(i) as RectTransform;
                if (childRect != null && childRect.anchoredPosition != Vector2.zero)
                {
                    Debug.LogWarning($"BillboardCameraGalactica: '{childRect.name}' under billboard "
                        + $"'{name}' had a non-zero anchoredPosition ({childRect.anchoredPosition}) - "
                        + "auto-corrected to (0, 0) so it stays centered on the billboard's own pivot.");
                    childRect.anchoredPosition = Vector2.zero;
                }
            }
        }

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
