using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



public class FindEventCamera : MonoBehaviour
{
        public void Initialize() { }
        public void UpdateState() { }
    private Canvas Canvas;
    private Camera Camera;
    void Start()
    {
        Canvas = GetComponent<Canvas>();
        // GalaxyCameraDragMoveZoom.Instance is the single authoritative galaxy camera for this
        // process (see Billboard.cs for the full rationale) - GameObject.FindGameObjectWithTag
        // returns whichever "MainCamera"-tagged object happens to exist, which could be the wrong
        // one on host in multiplayer. Fall back to the tag lookup for safety; a headless dedicated
        // server legitimately has no camera at all, so this can still return null.
        GameObject mainCameraGo = GalaxyCameraDragMoveZoom.Instance != null
            ? GalaxyCameraDragMoveZoom.Instance.gameObject
            : GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCameraGo == null) return;
        Camera = mainCameraGo.GetComponent<Camera>();
        Canvas.worldCamera = Camera;
    }
    private void OnMouseDown()
    {
        if (Camera == null) return;
        Ray ray = Camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            //goName = hitObject.name;
            if (hitObject == gameObject)
            {
                //??????????? FleetUIController.current.LoadAFleetUI(gameObject);
            }
        }

    }

}
