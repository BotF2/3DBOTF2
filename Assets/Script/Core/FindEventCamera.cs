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
        Camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        Canvas.worldCamera = Camera;
    }
    private void OnMouseDown()
    {
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
