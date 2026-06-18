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
            // We know we hit this object because OnMouseDown fired
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
            IsDragging = false;
            if (PlayerTargetData != null && PlayerTargetData.FleetController != null)
            {
                PlayerTargetData.FleetController.PlayerTargetAsNewDestination(this.gameObject);
                
                // Restore destination mode so we can still see the cursor if needed, 
                // but usually setting a destination completes the process.
                if (GalaxyMenuUIController.Instance != null)
                {
                    GalaxyMenuUIController.Instance.SetClickMode(GalaxyClickMode.SetDestination);
                }
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
