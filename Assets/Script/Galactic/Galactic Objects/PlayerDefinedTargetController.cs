
using Assets.Core;
using TMPro;
using UnityEngine;


public class PlayerDefinedTargetController : MonoBehaviour
{
    public PlayerDefinedTargetData PlayerTargetData;
    public Sprite Insignia;
    public MapLineMovable DropLine;
    public Camera galaxyEventCamera;
    public GameObject galaxyBackgroundImage;
    public Canvas CanvasToolTip;
    private Rigidbody rb;
    private PlayerDefinedTargetManager _manager;

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
    public void Init(PlayerDefinedTargetManager manager)
    {
        _manager = manager;
    }
    private void OnMouseDown()
    {
        Ray ray = galaxyEventCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject galaxyGo = hit.collider.gameObject;
            if (galaxyGo.tag != "GalaxyImage")
            {
                // What a player defined target does with a hit
                PlayerDefinedTargetController clickedPlayerTargetCon = galaxyGo.GetComponentInChildren<PlayerDefinedTargetController>();

                if (GameController.Instance.AreWeLocalPlayer(clickedPlayerTargetCon.PlayerTargetData.CivOwnerEnum))
                {
                    PlayerDefinedTargetDrag.Instance.SetPlayerTargetDrag(true, this);
                    GalaxyCameraDragMoveZoom.Instance.SetPlayerTargetDrag(true);
                }             
            }
        }
    }
    private void OnMouseUp()
    {
        PlayerTargetData.FleetController.PlayerTargetAsNewDestination(this.gameObject); 
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
