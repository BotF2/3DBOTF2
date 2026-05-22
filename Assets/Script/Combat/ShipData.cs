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
            this.ShipSO = shipSO;  // ✅ Store reference
            ShipName = shipSO.ShipName;
            // ... populate from SO ..
        }
        public ShipData()
        {

        }
    }
}
