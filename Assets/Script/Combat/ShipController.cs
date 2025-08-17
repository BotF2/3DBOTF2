using Assets.Core;
using DG.Tweening.Core.Easing;
using Mirror.BouncyCastle.Utilities.IO.Pem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ShipController : MonoBehaviour
{
    private ShipData shipData;
    public ShipData ShipData { get { return shipData; } set { shipData = value; } }
    public string Name;
    public GameObject torpedoPrefab;
    public GameObject beamWeaponPrefab;
    public GameObject ShipListUIGameObject; //The instantiated ship UI for this fleet and system ship lists, a prefab clone, not a class but a game object
    public AudioClip clipTorpedoFire;
    public AudioClip clipBeamFire;
    private AudioSource theSource;
    //public List<Torpedo> theLocalTargetList = new List<Torpedo>();

    public void Init(ShipManager shipManager)
    {
        ShipManager.Instance = shipManager;
    }
    private void Update()
    {
       // move ship
        if (ShipData != null && ShipData.TargetThisShipController != null)
        {

        }
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
    }
    public void OnShipEncounteredShip(ShipController shipController)
    {
        //1) player get the ShipController of the ship GO we hit
        //2) player ask your factionOwner (CivManager) 
    }
    public void OnShipEncounteredOther(StarSysController StarSysController)
    {
        //1) player get the OtherController of the GO
    }

    internal void FireWeapons(bool baem)
    {
        if (ShipData.TargetThisShipController != null)
        { 
            if (baem && ShipData.BeamDamage > 0)
            {
                var beamWeaponGo = Instantiate(beamWeaponPrefab, this.transform.position, Quaternion.identity);
                var lineRenderer = beamWeaponGo.GetComponent<LineRenderer>();
                var beamWeaponScript = beamWeaponGo.GetComponent<BeamWeapon>();
                beamWeaponScript.TargetTransform = ShipData.TargetThisShipController.transform; // Set the target transform
                beamWeaponScript.WeaponTransform = this.transform; // Set the weapon transform
                beamWeaponScript.LineRenderer = lineRenderer;
                beamWeaponScript.SetWeaponAndTarget(this.transform, ShipData.TargetThisShipController.transform); // Set the weapon and target transforms
                TakeDamage(ShipData.BeamDamage); 
                Destroy(beamWeaponGo, 0.5f); // Destroy the beam after so much time
            }
            else if (ShipData.TorpedoDamage > 0)
            {
                var torpedoGo = Instantiate(torpedoPrefab, this.transform.position, Quaternion.identity);
                var torpedoScript = torpedoGo.GetComponent<Torpedo>();
                torpedoScript.TorpedoDamage = ShipData.TorpedoDamage;
                if (ShipData.TargetThisShipController != null)
                {
                    torpedoScript.Target = ShipData.TargetThisShipController.transform; // ShipData.TargetForThisShip is GameObject and Torpedo.Target is Transform
                    torpedoScript.TargetCivEnum = ShipData.TargetThisShipController.ShipData.CivEnum; // Get the target ship's CivEnum
                }
            }
        }
    }
    public void TakeDamage(int weaponDamageInt)
    {
        if (ShipData.ShieldHealth > 0)
        {
            //If the ship has shields, damage the shields first
            ShipData.ShieldHealth -= (weaponDamageInt / 2);
            return;
        }
        else if (ShipData.HullHealth > 0)
        {
            ShipData.HullHealth -= (weaponDamageInt  / 3);
            return;
        }
        else
        {
            // If both shields and hull are destroyed, destroy the ship
            ShipCombatCameraController.Instance.OnShipDestroyed(this);
            ShipData.TargetThisShipController = null; // Clear the target ship controller
            Destroy(gameObject);
        }

    }
}
