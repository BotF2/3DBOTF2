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
        // A headless dedicated server has no Camera in the scene, so this legitimately returns
        // null there - skip wiring the world-space canvas rather than throwing on every spawn.
        GameObject mainCameraGo = GameObject.FindGameObjectWithTag("MainCamera");
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
