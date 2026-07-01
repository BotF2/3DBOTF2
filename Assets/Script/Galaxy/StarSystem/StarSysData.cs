
using BOTF3D.Combat;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;


/// <summary>
/// This is a type of galactic object that is a 'StarSystem' class (Manager/Controller/Data and can have a habitable 'planet') 
/// with a real star or a nebula or a complex as in the Borg Uni-complex)
/// Other galactic objects not described by StarSys (will have their own classes (ToDo: Managers/Controllers/Data) for stations (one class),
/// and black-holes/wormholes (one class.)
/// Star systems also hold ships just like fleets hold ships
/// </summary>
namespace BOTF3D.Galaxy
{
    public class StarSysData
    {
        private int starSysInt;
        private Vector3 position;
        public GameObject SysGameObject;
        private string sysName;
        public string SysName { get { return sysName; } }

        public GameObject ShipListUIParent { get; internal set; }
        private CivEnum firstOwnerCivEnum;
        public CivEnum CurrentOwnerCivEnum;
        public int PlayerId; // network player ID, not used in single player
        public CivController CurrentCivController;
        public GalaxyObjectType SystemType;
        //public StartingTechLevel is a civ level value, not a system data value.
        public int TechUnits; // ResearchCenters centers provide tech output units that determines progress to a civ level StartingTechLevel enum.
        public Sprite StarSprit;
        public List<BOTF3D.Combat.ShipController> ShipsList = new List<BOTF3D.Combat.ShipController>();
        [Header("Dilithium & Power")]
        public int DilithiumCapacity = 1; // Currently 1 for minor civ, habitable or for terraform and 2 for playable major civs, 0 for black holes or other non-habitable systems
        public int CurrentPowerPlantCount = 1; // 1 dilithium = 1 power plant, so this is also the current dilithium being mined.
                                               // Consider adding other power sources and adjust this variable to be more general for total power output.
        public int DilithiumStockpile; // Unallocated dilithium available for construction
        public AIBuildMode AIBuildMode = AIBuildMode.Off;
        public bool IsAIManaged => AIBuildMode != AIBuildMode.Off;

        public bool HasDilithium(int amount) => DilithiumStockpile >= amount;
        public void DeductDilithium(int amount) => DilithiumStockpile = Mathf.Max(0, DilithiumStockpile - amount);
        public List<GameObject> PowerPlants;
        public List<GameObject> Factories;
        public List<GameObject> FactoryBuildQueue;
        public List<GameObject> ResearchCenters;
        public List<GameObject> Shipyards;
        public List<ShipData> ShipyardQueue;
        public List<GameObject> ShieldGenerators;
        public List<GameObject> OrbitalBatteries;
        public GameObject buildSlotItemImage;
        public List<GameObject> buildQueueImageList;
        public int BasePowerPerPlant = 20; // two power plants for major home systems so 40 total

        public int TotalSysPowerOutput = 0;
        public int TotalSysPowerLoad = 0;
        public PowerPlantData PowerPlantData;
        public FactoryData FactoryData;
        public ShipyardData ShipyardData;
        public ShieldGeneratorData ShieldGeneratorData;
        public OrbitalBatteryData OrbitalBatteryData;
        public ResearchCenterData ResearchCenterData;
        [SerializeField]
        private Image powerPlant;
        [SerializeField]
        private Image factory;
        [SerializeField]
        private Image shipyard;
        [SerializeField]
        private Image shield;
        [SerializeField]
        private Image orbital;
        [SerializeField]
        private Image researchCenter;

        public string Description;
        public bool IsHomeworld;
        public bool IsHabitable;
        public bool? IsTerraformable;
        private string v;

        public StarSysData(StarSysSO starSysSO)
        {
            starSysInt = starSysSO.StarSysInt;
            position = new Vector3(starSysSO.Position.x, starSysSO.Position.y, starSysSO.Position.z);
            sysName = starSysSO.SysName;
            firstOwnerCivEnum = starSysSO.FirstOwner;
        }
        public StarSysData(string v)
        {
            this.v = v;
            this.sysName = v;
        }
        public int GetStarSysInt()
        {
            return this.starSysInt;
        }
        public Vector3 GetPosition(Vector3 vector3)
        {
            return this.position;
        }
        public List<ShipController> GetShipList()
        {
            return ShipsList;
        }
        public void SetShipList(List<ShipController> newShipList)
        {
            ShipsList = newShipList;
        }
        public void AddToShipList(ShipController shipController)
        {
            ShipsList.Add(shipController);
        }
        public void RemoveFromShipList(ShipController shipController)
        {
            ShipsList.Remove(shipController);
        }
        public string GetSysName() { return this.sysName; }
        public CivEnum GetFirstOwner() { return this.firstOwnerCivEnum; }
        /// <summary>
        /// Check if system can build another power plant
        /// </summary>
        public bool CanBuildPowerPlant()
        {
            return PowerPlants.Count < DilithiumCapacity;
        }
        /// <summary>
        /// Get available power plant slots
        /// </summary>
        public int GetAvailablePowerPlantSlots()
        {
            return Mathf.Max(0, DilithiumCapacity - CurrentPowerPlantCount);
        }

        /// <summary>
        /// Calculate total power output for this system
        /// </summary>
        public float CalculateTotalPower(float techMultiplier)
        {
            return CurrentPowerPlantCount * BasePowerPerPlant * techMultiplier;
        }

        public Vector3 GetPosition()
        {
            return this.position;
        }

        public void SetPosition(Vector3 pos)
        {
            this.position = pos;
        }
    }
}
