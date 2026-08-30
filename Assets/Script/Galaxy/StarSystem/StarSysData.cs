
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
        // ── Dilithium & Power ────────────────────────────────────────────────────────
        // Dilithium is the game's limiting strategic resource. It is not consumed by
        // running power plants — instead it is HELD in each reactor crystal matrix as
        // the permanent medium that channels the matter/antimatter reaction. Building a
        // power plant locks dilithium into the reactor; destroying a plant fully recovers
        // it. Ships likewise hold dilithium in their drive systems; destroyed ships lose
        // that dilithium permanently (combat explosions scatter it).
        //
        // Sources of dilithium:
        //   • Per-turn mining  — each inhabited system produces DilithiumMiningRate per turn.
        //     Playable homeworlds produce the most; minor-civ systems with warp produce less;
        //     pre-warp minor homeworlds produce nothing until conquered/joined; colonised
        //     systems produce a small base rate once facilities are built.
        //   • Scrapping ships  — a player-ordered decommission returns the ship's full
        //     DilithiumCost to the system's stockpile (vs combat destruction, which loses it).
        //
        // Sinks of dilithium:
        //   • Ship construction  — each new ship locks dilithium into its drive core.
        //   • Power plant construction  — each new plant locks dilithium into its reactor.
        //   • Colonisation transports  — the transport's loaded cargo seeds the new colony's
        //     starting stockpile when the colonisation timer completes.
        //
        // MaxPowerPlants caps how many plants this system can support, set by system type
        // and civ. It is NOT a dilithium quantity — it is a slot limit on infrastructure.
        [Header("Dilithium & Power")]
        public int MaxPowerPlants = 1;
        public int CurrentPowerPlantCount = 1;
        public int DilithiumStockpile;
        public int DilithiumMiningRate; // dilithium added to stockpile each turn via mining
        public AIBuildMode AIBuildMode = AIBuildMode.Economy;
        public bool IsAIManaged => AIBuildMode != AIBuildMode.Off;

        // ── Antimatter fuel loop ─────────────────────────────────────────────────────
        // Antimatter is the Power Plants' ongoing operating fuel (Dilithium remains the one-time
        // capital cost locked into the reactor at construction). Active Factories bank it into
        // this stockpile every turn; active Power Plants draw from it. See
        // Docs/Design/Economy_Phase1_FuelLoop_FacilityCaps.md §1 and
        // StarSysManager.ProcessAntimatterFuelLoop for the per-turn processing and the
        // destruction-triggered blackout (not a shortage-triggered one - see that method).
        [Header("Antimatter Fuel Loop")]
        public int AntimatterStockpile;
        public int AntimatterProductionRate; // banked per turn from active Factories
        public int AntimatterConsumptionRate; // drawn per turn by active Power Plants
        public bool HasAntimatter(int amount) => AntimatterStockpile >= amount;
        public void DeductAntimatter(int amount) => AntimatterStockpile = Mathf.Max(0, AntimatterStockpile - amount);

        // Subspace scanner: this system's own "fog of war" for detecting nearby enemy fleets,
        // independent of the local player's rendering fog grid (FischlWorks_FogWar). Refreshed
        // every turn by StarSysAIManager for every system regardless of owner, so it's available
        // both for AI auto-Defence triggers and for future "set enemy fleet as destination" UI.
        // Derived from FleetManager.LocalPlayerFogSightRange (the real world-unit sight range
        // given to the player's own fleet's fog revealer) so it stays traceable to fog-of-war
        // rather than an unrelated hand-picked number. An earlier /4 quartering here was a
        // mistaken compensation for what turned out to be a coordinate-space bug in
        // StarSysAIManager.UpdateSubspaceScanner (it compared this position against fleets'
        // true WORLD position while StarSysData.position stores the raw, unscaled local
        // position - 10x smaller than reality because GalaxyCenter's transform has
        // localScale=10). With that bug fixed, the raw galaxy is actually ~2176 world units
        // across, not ~218, so the full sight range no longer spans the map.
        public float SubspaceScannerRadius = BOTF3D.Galaxy.FleetManager.LocalPlayerFogSightRange;
        public List<FleetController> DetectedEnemyFleets = new List<FleetController>();

        // Which civs have ever scanned a hostile fleet within range of this system. Only ever
        // populated for GalaxyObjectType.UniComplex (the Borg home system) by
        // StarSysAIManager.UpdateSubspaceScanner - drives permanent fog-of-war reveal of the
        // Borg system's sprite/drop-line/text (StarSysManager.RefreshBorgConcealment) and gates
        // AI war-targeting (StarSysAIManager.FindNearestEnemySystem) until a civ has found it.
        public HashSet<CivEnum> DiscoveredByCivs = new HashSet<CivEnum>();

        // Dock slots for fleets sitting at this system (see FleetDockLayout). A null entry is a
        // free slot a departed fleet left behind; the list only grows when every existing slot is
        // occupied, so a system that regularly cycles fleets through doesn't leak slots.
        public List<FleetController> FleetDockSlots = new List<FleetController>();

        public int ClaimFleetDockSlot(FleetController fleet)
        {
            for (int i = 0; i < FleetDockSlots.Count; i++)
            {
                if (FleetDockSlots[i] == null)
                {
                    FleetDockSlots[i] = fleet;
                    return i;
                }
            }
            FleetDockSlots.Add(fleet);
            return FleetDockSlots.Count - 1;
        }

        public void ReleaseFleetDockSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < FleetDockSlots.Count)
                FleetDockSlots[slotIndex] = null;
        }

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
        public List<GameObject> GroundForces = new List<GameObject>();

        // ── Facility build ceilings ──────────────────────────────────────────────────
        // How many of each facility type this system can ever have BUILT (active or not) -
        // distinct from power, which only governs how many built facilities can be ACTIVE at
        // once. FacilityCapBase is fixed forever at system creation (see
        // StarSysManager.InitializeFacilityCaps) from the system's role (Major homeworld /
        // minor homeworld / colony) and its FIRST owner's QualityScore - never the current
        // owner, and never recomputed after creation, so conquest can never change it.
        // FacilityCapTechBonus only ever ratchets upward (see StarSysManager.GetFacilityCap),
        // so the combined total can never decrease either. Together this guarantees a system
        // can never end up "over cap" - the numbers only ever hold steady or grow. See
        // Docs/Design/Economy_Phase1_FuelLoop_FacilityCaps.md §2.
        [Header("Facility Caps")]
        public Dictionary<StarSysFacilityType, int> FacilityCapBase = new Dictionary<StarSysFacilityType, int>();
        public int FacilityCapTechBonus;
        [Header("Population & Ground Forces")]
        public int Population; // current population units; converts into GroundForces up to MaxGroundForceUnits
        public float PopulationGrowthAccumulator; // fractional growth carried between stardates (see PopulationManager.GrowSystem)
        public int MaxPopulation; // cap this system's Population can grow to (set by StarSysManager at creation)
        public int MaxGroundForceUnits; // cap on GroundForces.Count; major homeworlds can reach GroundForceData.PopulationPerUnit-scaled 11
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
        public GroundForceData GroundForceData;
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

        [Header("Terraforming & Colonization")]
        // Set true while StarSysController.TerraformSystem's timer coroutine is running; the
        // transport is consumed instantly, but IsHabitable doesn't flip true until
        // TerraformCompleteStardate is reached (see StarSysController.TerraformTimerCoroutine).
        public bool IsTerraforming;
        public int TerraformStartStardate;
        public int TerraformCompleteStardate;

        // Set true while StarSysController.ColonizeWithTransport's timer coroutine is running; the
        // transport is consumed instantly, but the starting Power Plant/Factory aren't granted
        // until ColonizeCompleteStardate is reached (see StarSysController.ColonizeTimerCoroutine).
        public bool IsColonizing;
        public int ColonizeStartStardate;
        public int ColonizeCompleteStardate;

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

        // Same purpose/reasoning as FleetData.GetNextShipCreationSeq - scoped to this system's own
        // stable starSysInt (shared/deterministic across clients via the galaxy's generation seed)
        // instead of a single global counter.
        private int nextShipCreationSeq = 1; // 0 is reserved to mean "unassigned"
        public int GetNextShipCreationSeq() => nextShipCreationSeq++;
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
            return PowerPlants.Count < MaxPowerPlants;
        }
        /// <summary>
        /// Get available power plant slots
        /// </summary>
        public int GetAvailablePowerPlantSlots()
        {
            return Mathf.Max(0, MaxPowerPlants - CurrentPowerPlantCount);
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
