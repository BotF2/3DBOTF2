using BOTF3D.Combat;

using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    public class ShipData
    {
        public int ShipID; // Unique ID for event systems and saving
        public ShipSO ShipSO;

        public string ShipName;
        public CivEnum CivEnum;
        public int PlayerId; // network player ID, not used in single player
        public TechLevel TechLevel;
        public ShipType ShipType;
        public Sprite ShipSprite;
        public float maxWarpFactor;
        public float currentWarpFactor;
        public int ShieldMaxHealth; // cached max set during initialization; used for health-bar and reset
        public int HullMaxHealth;   // cached max set during initialization; used for health-bar and reset
        public int ShieldHealth;
        public int HullHealth;
        public int TorpedoDamage;
        public int BeamDamage;
        public int BuildDuration;
        public int DilithiumCost;
        public int CargoCapacity; // Transport-only; how many population/ground-force units it can carry, scales with TechLevel
        public int LoadedPopulation; // Population units currently loaded into this transport's cargo hold
        public int LoadedGroundForces; // Ground force units currently loaded into this transport's cargo hold; shares CargoCapacity with LoadedPopulation
        public string BaseShipName; // ShipName before any cargo-based rename (Colonyship/Dropship); restored once cargo is fully unloaded
        public string ShipDescription;
        public ShipController TargetThisShipController;
        public GameObject TargetOnThisShip;
        public FleetController CurrentFleetController;
        public StarSysController CurrentStarSysController;
        public bool Distroyed = false;
        public bool IsCaptured = false; // Set when captured; ship takes no damage, fires no weapons, grants rewards at combat end
        public bool IsScuttled = false; // Set before SelfDestruct() so the report distinguishes scuttled from combat-destroyed
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
            this.ShipDescription = shipSO.ShipDescription;
            // Combat stats are not stored on ShipSO; call ShipDataInitializer.InitializeShipData
            // (which calls ShipStatCalculator) to populate maxWarpFactor, ShieldMaxHealth, etc.
        }
public ShipData()
        {

        }
    }
}
