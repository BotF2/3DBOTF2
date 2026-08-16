using BOTF3D.Core;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
{
    public class PlayerDefinedTargetController : MonoBehaviour, IController
    {
        public void Initialize() { }
        public void UpdateState() { }
        public PlayerDefinedTargetData PlayerTargetData;
        public Sprite Insignia;
        public MapLineMovable DropLine;
        public Camera galaxyEventCamera;
        public GameObject galaxyBackgroundImage;
        public Canvas CanvasToolTip;
        private Rigidbody rb;

        public bool IsDragging { get; private set; }

        void Start()
        {
            galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            CanvasToolTip.worldCamera = galaxyEventCamera;
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
        }
        private void FixedUpdate()
        {
            if (transform.hasChanged)
                MoveTheDropline();
        }
        private void OnMouseDown()
        {
            StartDrag();
        }

        // Called for a real click on the marker's collider (OnMouseDown above), and also
        // directly by PlayerDefinedTargetManager.InstantiatePlayerTarget so the marker starts
        // following the mouse the instant it's created, without requiring the user to first
        // land a precise click on its (small, freshly-spawned-off-screen-of-the-cursor) collider.
        public void StartDrag()
        {
            if (PlayerTargetData != null && GameController.Instance.AreWeLocalPlayer(PlayerTargetData.CivOwnerEnum))
            {
                IsDragging = true;
                PlayerDefinedTargetDrag.Instance.SetPlayerTargetDrag(true, this);
                GalaxyCameraDragMoveZoom.Instance.SetPlayerTargetDrag(true);

                // Set click mode to avoid other objects intercepting destination clicks
                if (GalaxyMenuUIController.Instance != null)
                {
                    GalaxyMenuUIController.Instance.SetClickMode(GalaxyClickMode.Normal);
                }
            }
        }
        private void OnMouseUp()
        {
            FinalizeDrag();
        }

        // Because StartDrag() above is armed programmatically rather than via a real mouse-down
        // on this collider, Unity's native mouse-picking never records a down-press on it - so a
        // real release over empty map/background (the normal case, since the user drags from
        // wherever they grabbed the map, not the tiny marker icon) never reaches OnMouseUp here.
        // PlayerDefinedTargetDrag polls for the actual button-up and calls this directly so the
        // destination reliably finalizes regardless of what's under the cursor on release.
        public void FinalizeDrag()
        {
            if (!IsDragging) return;
            IsDragging = false;

            // Guard: if TargetController was cleared by cancel (DestroyPlayerTarget nulls it before
            // deferred Destroy completes), skip re-setting the destination so the cancel sticks.
            if (PlayerTargetData != null && PlayerTargetData.FleetController != null
                && PlayerTargetData.FleetController.TargetController == this)
            {
                var galaxyUI = GalaxyMenuUIController.Instance;
                if (galaxyUI != null)
                    galaxyUI.BeginSetDestination(PlayerTargetData.FleetController); // re-register fleet before destination is set

                PlayerTargetData.FleetController.PlayerTargetAsNewDestination(this.gameObject);

                if (galaxyUI != null)
                    galaxyUI.CompleteSetDestination(); // clears fleet + resets click mode to Normal
            }

            PlayerDefinedTargetDrag.Instance.SetPlayerTargetDrag(false, this);
            GalaxyCameraDragMoveZoom.Instance.SetPlayerTargetDrag(false);
        }
        private void MoveTheDropline()
        {
            Vector3 galaxyPlanePoint = new Vector3(rb.position.x, -60f, rb.position.z);
            Vector3[] points = { rb.position, galaxyPlanePoint };
            DropLine.SetUpLine(points);
        }
    }
}
