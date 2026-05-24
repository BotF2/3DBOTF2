using BOTF3D.Combat;
using BOTF3D.GamePlay;
using UnityEngine;

namespace BOTF3D.Core
{
    public class ShipData
    {
        public ShipSO ShipSO;

        public string ShipName;
        public CivEnum CivEnum;
        public int PlayerId; // network player ID, not used in single player
        public TechLevel TechLevel;
        public ShipType ShipType;
        public Sprite ShipSprite;
        public float maxWarpFactor;
        public float currentWarpFactor;
        public int ShieldHealth;
        public int HullHealth;
        public int TorpedoDamage;
        public int BeamDamage;
        public int BuildDuration;
        public string ShipDescription;
        public ShipController TargetThisShipController;
        public GameObject TargetOnThisShip;
        public FleetController CurrentFleetController;
        public StarSysController CurrentStarSysController;
        public bool Distroyed = false;
        public Vector3 Position; // <-- Will we need to save a combat position?


        public ShipData(string name)
        {
            ShipName = name;
        }
        public ShipData(ShipSO shipSO)
        {
            if (shipSO == null) return;

            this.ShipSO = shipSO;
            this.ShipName = shipSO.ShipName;
            this.CivEnum = shipSO.CivEnum;
            this.TechLevel = shipSO.TechLevel;
            this.ShipType = shipSO.ShipType;
            this.ShipSprite = shipSO.shipSprite;
            this.maxWarpFactor = shipSO.maxWarpFactor;
            this.ShieldHealth = shipSO.ShieldMaxHealth;
            this.HullHealth = shipSO.HullMaxHealth;
            this.TorpedoDamage = shipSO.TorpedoDamage;
            this.BeamDamage = shipSO.BeamDamage;
            this.BuildDuration = shipSO.BuildDuration;
            this.ShipDescription = shipSO.ShipDescription;
        }
public ShipData()
        {

        }
    }
}
