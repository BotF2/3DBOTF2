using Assets.GamePlay;
using UnityEngine;

namespace Assets.Core
{
    public class ShipData
    {
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

        }
        public ShipData()
        {

        }
    }
}
