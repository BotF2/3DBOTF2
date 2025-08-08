using Assets.Core;
using System;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    private ShipData shipData;
    public ShipData ShipData { get { return shipData; } set { shipData = value; } }
    public string Name;
    private ShipManager _manager;
    public GameObject ShipListUIGameObject; //The instantiated ship UI for this fleet and system ship lists.
                                            //a prefab clone, not a class but a game object
    public void Init(ShipManager shipManager)
    {
        _manager = shipManager;
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
        // this is for SpaceCombatScene, not galaxy map 
        ShipController shipController = collider.gameObject.GetComponent<ShipController>();
        if (shipController != null) // it is a shipController 
        {
            OnShipEncounteredShip(shipController);
            Debug.Log("Controller collided with " + shipController.gameObject.name);
        }
    }
    public void OnShipEncounteredShip(ShipController shipController)
    {
        //1) player get the ShipController of the ship GO we hit
        //2) player ask your factionOwner (CivManager) 
    }
    public void OnShipEncounteredOther(StarSysController StarSysController)
    {
        //1) player get the OtheerController of the GO

    }

    internal void FireWeapons()
    {
        // ToDo fire weapons;
    }
}
