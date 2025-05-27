using UnityEngine;

public class ShipController : MonoBehaviour
{
    private ShipData shipData;
    public ShipData ShipData { get { return shipData; } set { shipData = value; } }
    public string Name;
    private ShipManager _manager;
    //private bool deltaShipList = false; //??? do I need this or the shipdropdown listener

    public void Init(ShipManager shipManager)
    {
        _manager = shipManager;
    }
    private void Start()
    {
        //rb = GetComponent<Rigidbody>();
        //combatEventCamera = GameObject.FindGameObjectWithTag("SpaceCombatScene Camera").GetComponent<Camera>();
        //var CanvasGO = GameObject.Find("CombatShipUI");
        //CombatUICanvas = CanvasGO.GetComponent<canvas>();
        //CombatUICanvas.worldCamera = combatEventCamera;
        //CanvasToolTip.worldCamera = combatEventCamera;
        //TextComponent = shipData.CivEnum.ToString() + " Fleet " + shipData.ShipName;

    }
    private void FixedUpdate()
    {
        // might use this
    }
    private void OnMouseDown()
    {
        //string goName;
        //Ray ray = combatEventCamera.ScreenPointToRay(Input.mousePosition);
        //RaycastHit hit;
        //if (Physics.Raycast(ray, out hit))
        //{
        //    GameObject hitObject = hit.collider.gameObject;
        //    goName = hitObject.name;
        //}
        //CombatUIManager.current.LoadShipUI(gameObject);
    }
    void OnTriggerEnter(Collider collider)
    {
        // this if for SpaceCombatScene, not galaxy map 
        ShipController shipController = collider.gameObject.GetComponent<ShipController>();
        if (shipController != null) // it is a shipController 
        {
            OnShipEncounteredShip(shipController);
            Debug.Log("Controller collided with " + shipController.gameObject.name);
        }
        //OtherController otherController = collider.gameObject.GetComponent<OtherController>();
        //if (otherSysController != null)
        //{
        //    OnShipEncounteredOther(otherController);
        //}
    }
    public void OnShipEncounteredShip(ShipController shipController)
    {
        //1) player get the ShipController of the ship GO we hit
        //2) player ask your factionOwner (CivManager) 
    }
    //public void OnShipEncounteredOther(OtherController StarSysController)
    //{
    //    //1) player get the OtheerController of the GO

    //}
}
