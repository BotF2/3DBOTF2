
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
        /// <summary>
        /// Total dilithium units in this system (fixed — 100 for major home systems, 50 for minor/habitable, 0 for non-habitable).
        /// PowerPlants and docked ships draw from this pool. Ships carry their dilithium when in a fleet.
        /// </summary>
        public int DilithiumUnits = 50;
        public int CurrentPowerPlantCount = 1;
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
        // --- Facility slot limits (derived from system size) ---

        /// <summary>Maximum factories/research centers/orbital batteries = DilithiumUnits / 50 (1 for minor, 2 for major).</summary>
        public int MaxFacilitiesFromDilithium => Mathf.Max(1, DilithiumUnits / 50);
        public int MaxFactories         => MaxFacilitiesFromDilithium;
        public int MaxResearchCenters   => MaxFacilitiesFromDilithium;
        public int MaxOrbitalBatteries  => MaxFacilitiesFromDilithium;
        public int MaxShipyards         => 1;
        public int MaxShieldGenerators  => 1;

        // --- Dilithium model ---

        /// <summary>Dilithium committed to running PowerPlants at this tech level.</summary>
        public int GetDilithiumAllocatedToPlants(TechLevel techLevel)
        {
            int costPerPlant = BOTF3D.Core.TechManager.Instance != null
                ? BOTF3D.Core.TechManager.Instance.GetDilithiumCostPerPlant(techLevel)
                : 45;
            return (PowerPlants?.Count ?? 0) * costPerPlant;
        }

        /// <summary>Dilithium held by ships currently docked (stationed) in this system — not fleet ships.</summary>
        public int GetDilithiumHeldByDockedShips()
        {
            int total = 0;
            if (ShipsList == null) return 0;
            foreach (var sc in ShipsList)
            {
                if (sc?.ShipData != null && !sc.ShipData.IsMothballed)
                    total += sc.ShipData.DilithiumCost;
            }
            return total;
        }

        /// <summary>Dilithium free for new PowerPlants, ships, or moth-ball reactivation.</summary>
        public int GetDilithiumAvailable(TechLevel techLevel)
        {
            int allocated = GetDilithiumAllocatedToPlants(techLevel) + GetDilithiumHeldByDockedShips();
            return Mathf.Max(0, DilithiumUnits - allocated);
        }

        /// <summary>True if there is enough free dilithium to power one more PowerPlant at this tech level.</summary>
        public bool CanBuildPowerPlant(TechLevel techLevel)
        {
            int costPerPlant = BOTF3D.Core.TechManager.Instance != null
                ? BOTF3D.Core.TechManager.Instance.GetDilithiumCostPerPlant(techLevel)
                : 45;
            return GetDilithiumAvailable(techLevel) >= costPerPlant;
        }

        /// <summary>Legacy overload — uses EARLY tech cost as fallback.</summary>
        public bool CanBuildPowerPlant() => CanBuildPowerPlant(TechLevel.EARLY);

        /// <summary>How many additional PowerPlants the remaining dilithium can support at this tech level.</summary>
        public int GetAvailablePowerPlantSlots(TechLevel techLevel)
        {
            int costPerPlant = BOTF3D.Core.TechManager.Instance != null
                ? BOTF3D.Core.TechManager.Instance.GetDilithiumCostPerPlant(techLevel)
                : 45;
            if (costPerPlant <= 0) return 0;
            return GetDilithiumAvailable(techLevel) / costPerPlant;
        }

        // --- Energy model ---

        /// <summary>Total energy produced by active PowerPlants, scaled by TechLevel efficiency.</summary>
        public float CalculateTotalPower(TechLevel techLevel)
        {
            int output = BOTF3D.Core.TechManager.Instance != null
                ? BOTF3D.Core.TechManager.Instance.GetPowerOutputPerPlant(techLevel)
                : BasePowerPerPlant;
            return (PowerPlants?.Count ?? 0) * output;
        }

        /// <summary>Legacy overload for callers that pass a float multiplier.</summary>
        public float CalculateTotalPower(float techMultiplier)
        {
            return CurrentPowerPlantCount * BasePowerPerPlant * techMultiplier;
        }

        // --- Production model ---

        /// <summary>
        /// Factory production factor for build time calculation.
        /// = 1.0 + (activeFactories × perFactoryBonus). Minimum 1.0 (no factories = no bonus, existing behavior).
        /// </summary>
        public float GetProductionFactor(TechLevel techLevel)
        {
            int count = Factories?.Count ?? 0;
            if (count == 0) return 1.0f;
            float bonus = BOTF3D.Core.TechManager.Instance != null
                ? BOTF3D.Core.TechManager.Instance.GetFactoryProductionBonus(techLevel)
                : 0.3f;
            return 1.0f + count * bonus;
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
