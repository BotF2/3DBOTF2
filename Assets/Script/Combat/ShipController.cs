using Assets.Core;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;


public class ShipController : MonoBehaviour
{
    private ShipData shipData;
    public ShipData ShipData { get { return shipData; } set { shipData = value; } }
    public string Name;
    private ShipManager _manager;
    public GameObject torpedoPrefab;
    public GameObject beamWeaponPrefab;
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
    public void SetWeaponPrefabs() 
    {
        GameObject[] torpedoPrefabs = ShipManager.Instance.torpedoPrefabs;
        GameObject[] beamPrefabs = ShipManager.Instance.beamWeaponPrefabs;
        for (int i = 0; i < torpedoPrefabs.Length; i++)
        {
            if ((int)this.ShipData.CivEnum > 7)
            {
                torpedoPrefab = torpedoPrefabs.LastOrDefault();
            }
            else if (torpedoPrefabs[i].name.Contains(ShipData.CivEnum.ToString().ToUpper()))
            {
                torpedoPrefab = torpedoPrefabs[i];

            }
        }

        for (int i = 0; i < beamPrefabs.Length; i++)
        {
            if ((int)ShipData.CivEnum > 7)
            {
                beamWeaponPrefab = beamPrefabs.LastOrDefault();
            }
            else if (beamPrefabs[i].name.Contains(ShipData.CivEnum.ToString().ToUpper()))
            {
               beamWeaponPrefab = beamPrefabs[i];

            }
        }
        //var beamGo = Instantiate(shipCon.beamWeaponPrefab, shipCon.transform.position, shipCon.transform.rotation);
        //beamGo.transform.SetParent(shipCon.transform, false);
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

    internal void FireWeapons(bool baem)
    {
        if (baem && ShipData.BeamDamage > 0)
        {
            var beamWeaponGo = Instantiate(beamWeaponPrefab, this.transform.position, Quaternion.identity);
            var lineRenderer = beamWeaponGo.GetComponent<LineRenderer>();
            var beamWeaponScript = beamWeaponGo.GetComponent<BeamWeapon>();
            beamWeaponScript.LineRenderer = lineRenderer;
            beamWeaponScript.SetWeaponAndTarget(this.transform, ShipData.FireAtThis.transform);
            Destroy(beamWeaponGo, 0.5f); // Destroy the beam after so much time

        }
        else if (ShipData.TorpedoDamage > 0)
        {
            var torpedoGo = Instantiate(torpedoPrefab, this.transform.position, Quaternion.identity);
            var torpedoScript = torpedoGo.GetComponent<Torpedo>();
            torpedoScript.SetCurrentTarget(ShipData.FireAtThis.transform);
            Destroy(torpedoGo, 5f); // Destroy the torpedo after 5
        }

    }
}
