using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.Combat;
using FischlWorks_FogWar;

namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Drives the StarSysData.AIBuildMode toggle (Off/Economy/War/Defence) with real
    /// per-turn behavior. Off is human-only manual control; AI-played civs (and all
    /// minor races) can never sit on Off, so they fall back to Economy, the stated
    /// default for hands-off systems.
    /// </summary>
    public class StarSysAIManager : MonoBehaviour, IManager
    {
        public void Initialize() { }
        public void Cleanup() { }
        public static StarSysAIManager Instance;

        private const int MaxFacilitiesPerType = 4;

        private static readonly StarSysFacilityType[] EconomyPowerPriority =
        {
            StarSysFacilityType.ResearchCenter,
            StarSysFacilityType.Factory,
            StarSysFacilityType.Shipyard,
            StarSysFacilityType.ShieldGenerator,
            StarSysFacilityType.OrbitalBattery,
        };

        private static readonly StarSysFacilityType[] WarPowerPriority =
        {
            StarSysFacilityType.Shipyard,
            StarSysFacilityType.Factory,
            StarSysFacilityType.ShieldGenerator,
            StarSysFacilityType.OrbitalBattery,
            StarSysFacilityType.ResearchCenter,
        };

        private static readonly StarSysFacilityType[] DefencePowerPriority =
        {
            StarSysFacilityType.ShieldGenerator,
            StarSysFacilityType.OrbitalBattery,
            StarSysFacilityType.Shipyard,
            StarSysFacilityType.Factory,
        };

        private void Awake()
        {
            ServiceLocator.Register<StarSysAIManager>(this);
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTurnAdvanced += ProcessAllSystems;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTurnAdvanced -= ProcessAllSystems;
        }

        private void ProcessAllSystems()
        {
            foreach (var civ in CivManager.Instance.CivControllersInGame)
            {
                if (civ?.CivData?.StarSysWeOwn == null) continue;

                bool aiOwned = !GameController.Instance.AreWeLocalPlayer(civ.CivData.CivEnum);

                foreach (var sysCon in civ.CivData.StarSysWeOwn)
                {
                    if (sysCon?.StarSysData == null) continue;

                    UpdateSubspaceScanner(sysCon);

                    // Off is a human-only manual mode. AI-played majors and minor races
                    // are never left un-managed, so they default to Economy.
                    if (aiOwned && sysCon.StarSysData.AIBuildMode == AIBuildMode.Off)
                        sysCon.StarSysData.AIBuildMode = AIBuildMode.Economy;

                    // A detected hostile fleet always takes priority over whatever the AI was
                    // doing before. The human player's own systems are left alone here - it's
                    // their call whether to flip to Defence manually.
                    if (aiOwned && sysCon.StarSysData.DetectedEnemyFleets.Count > 0 &&
                        sysCon.StarSysData.AIBuildMode != AIBuildMode.Defence)
                    {
                        sysCon.StarSysData.AIBuildMode = AIBuildMode.Defence;
                    }

                    switch (sysCon.StarSysData.AIBuildMode)
                    {
                        case AIBuildMode.Economy:
                            ProcessEconomyMode(sysCon, civ.CivData);
                            break;
                        case AIBuildMode.War:
                            ProcessWarMode(sysCon, civ.CivData);
                            break;
                        case AIBuildMode.Defence:
                            ProcessDefenceMode(sysCon, civ.CivData);
                            break;
                        case AIBuildMode.Off:
                        default:
                            break; // human manual control - leave untouched
                    }
                }
            }
        }

        private void ProcessEconomyMode(StarSysController sysCon, CivData civData)
        {
            TryPowerOnOneFacility(sysCon, EconomyPowerPriority);

            if (sysCon.StarSysBuildManager == null) return;
            if (sysCon.sysBuildQueueList != null && sysCon.sysBuildQueueList.Count > 0) return; // let the current build finish first

            var next = PickNextEconomyFacility(civData, sysCon.StarSysData);
            if (next.HasValue)
                sysCon.StarSysBuildManager.QueueFacilityBuild(next.Value);
        }

        /// <summary>
        /// War mode keeps shipyards fed above all else: facility construction only ever
        /// expands power or shipyard capacity. The ship queue builds toward a specific
        /// "war party" composition sized to whatever it's actually going to attack -
        /// a raid on a minor race's system needs far less than an assault on a major
        /// race's defended system (orbital batteries, shields, its own ships). Once that
        /// composition is reached, the ships are formed into a fleet and dispatched at
        /// the target (see DispatchFleetToWar). Numbers are a first pass for playtesting -
        /// expect them to move once system-vs-fleet combat (attacking defences) is built.
        /// </summary>
        private const int WarPartyCombatShipsVsMinor = 5;
        private const int WarPartyTransportsVsMinor = 1;
        private const int WarPartyCombatShipsVsMajor = 9;
        private const int WarPartyTransportsVsMajor = 2;

        private void ProcessWarMode(StarSysController sysCon, CivData civData)
        {
            var data = sysCon.StarSysData;

            TryPowerOnOneFacility(sysCon, WarPowerPriority);

            if (sysCon.StarSysBuildManager == null) return;

            if (sysCon.sysBuildQueueList != null && sysCon.sysBuildQueueList.Count == 0)
            {
                if (data.CanBuildPowerPlant() &&
                    data.TotalSysPowerOutput - data.TotalSysPowerLoad < data.BasePowerPerPlant)
                {
                    sysCon.StarSysBuildManager.QueueFacilityBuild(StarSysFacilityType.PowerPlanet);
                }
                else if ((data.Shipyards?.Count ?? 0) < MaxFacilitiesPerType)
                {
                    sysCon.StarSysBuildManager.QueueFacilityBuild(StarSysFacilityType.Shipyard);
                }
            }

            var (enemyEnum, targetGO) = ResolveWarTarget(sysCon, civData);

            if (sysCon.sysShipBuildQueueList != null && sysCon.sysShipBuildQueueList.Count == 0)
            {
                var shipType = PickWarPartyShipType(civData, data, enemyEnum);
                if (shipType.HasValue && CanAffordShip(sysCon, shipType.Value))
                    sysCon.StarSysBuildManager.QueueShipBuild(shipType.Value);
            }

            if (enemyEnum.HasValue && targetGO != null && HasReachedWarPartyStrength(data, enemyEnum.Value))
                DispatchFleetToWar(sysCon, targetGO);
        }

        /// <summary>
        /// Forms a fleet from every ship currently sitting in the system and sends it at
        /// max warp toward the given target (an enemy fleet or system GameObject).
        /// </summary>
        private void DispatchFleetToWar(StarSysController sysCon, GameObject target)
        {
            var shipsToMove = new List<ShipController>(sysCon.StarSysData.ShipsList);
            FleetController newFleet = FleetManager.Instance.FormFleetFromSystemShips(sysCon, shipsToMove);
            if (newFleet == null) return;

            newFleet.FleetData.Destination = target;
            newFleet.FleetData.CurrentWarpFactor = newFleet.FleetData.MaxWarpFactor;
        }

        /// <summary>
        /// Picks who this system is currently building/dispatching its war party against:
        /// a threat the subspace scanner has already picked up nearby takes priority
        /// (engage it directly), falling back to the enemy's nearest owned system.
        /// Returns (null, null) when there's no war target at all yet.
        /// </summary>
        private static (CivEnum? enemyEnum, GameObject targetGO) ResolveWarTarget(StarSysController sysCon, CivData civData)
        {
            var detected = sysCon.StarSysData.DetectedEnemyFleets;
            if (detected != null && detected.Count > 0)
            {
                Vector3 origin = sysCon.StarSysData.GetPosition();
                FleetController nearestFleet = null;
                float nearestSqr = float.MaxValue;
                foreach (var fleet in detected)
                {
                    if (fleet?.FleetData == null) continue;
                    float distSqr = (fleet.transform.position - origin).sqrMagnitude;
                    if (distSqr < nearestSqr)
                    {
                        nearestSqr = distSqr;
                        nearestFleet = fleet;
                    }
                }
                if (nearestFleet != null) return (nearestFleet.FleetData.CivEnum, nearestFleet.gameObject);
            }

            StarSysController nearestSystem = FindNearestEnemySystem(sysCon, civData);
            if (nearestSystem != null)
                return (nearestSystem.StarSysData.CurrentOwnerCivEnum, nearestSystem.gameObject);

            return (null, null);
        }

        /// <summary>
        /// Minor races (CivEnum > 6) rate a small raiding party; another major race's
        /// defended system calls for a proper assault force.
        /// </summary>
        private static (int combat, int transports) GetWarPartyRequirement(CivEnum enemyEnum)
        {
            bool enemyIsMinor = (int)enemyEnum > 6;
            return enemyIsMinor
                ? (WarPartyCombatShipsVsMinor, WarPartyTransportsVsMinor)
                : (WarPartyCombatShipsVsMajor, WarPartyTransportsVsMajor);
        }

        private static bool IsCombatShipType(ShipType type) =>
            type == ShipType.Destroyer || type == ShipType.Cruiser ||
            type == ShipType.LtCruiser || type == ShipType.HvyCruiser;

        private static void CountWarPartyShips(StarSysData data, out int combatCount, out int transportCount)
        {
            combatCount = 0;
            transportCount = 0;
            foreach (var ship in data.ShipsList)
            {
                if (ship?.ShipData == null) continue;
                if (ship.ShipData.ShipType == ShipType.Transport) transportCount++;
                else if (IsCombatShipType(ship.ShipData.ShipType)) combatCount++;
            }
        }

        /// <summary>
        /// No target yet (war not underway against anyone in range) just stockpiles a
        /// trait-picked combat hull, same as before. With a target, builds whichever of
        /// transports/combat hulls the war party is still short of - transports first,
        /// since a raid with no landing capacity can't do anything to a system's ground
        /// once combat is extended to system defences.
        /// </summary>
        private static ShipType? PickWarPartyShipType(CivData civData, StarSysData data, CivEnum? enemyEnum)
        {
            if (!enemyEnum.HasValue)
                return PickWarShipType(civData);

            var (requiredCombat, requiredTransports) = GetWarPartyRequirement(enemyEnum.Value);
            CountWarPartyShips(data, out int currentCombat, out int currentTransports);

            if (currentTransports < requiredTransports) return ShipType.Transport;
            if (currentCombat < requiredCombat) return PickWarShipType(civData);
            return null; // war party already at full strength - waiting on DispatchFleetToWar
        }

        private static bool HasReachedWarPartyStrength(StarSysData data, CivEnum enemyEnum)
        {
            var (requiredCombat, requiredTransports) = GetWarPartyRequirement(enemyEnum);
            CountWarPartyShips(data, out int currentCombat, out int currentTransports);
            return currentCombat >= requiredCombat && currentTransports >= requiredTransports;
        }

        /// <summary>
        /// Scans this civ's diplomacy pairs for anyone at War status, then picks whichever
        /// of that enemy's systems is closest to the dispatching system.
        /// </summary>
        private static StarSysController FindNearestEnemySystem(StarSysController sysCon, CivData civData)
        {
            StarSysController nearest = null;
            float nearestDistSqr = float.MaxValue;
            Vector3 origin = sysCon.StarSysData.GetPosition();

            foreach (var diploCon in DiplomacyManager.Instance.DiplomacyControllers)
            {
                if (diploCon?.DiplomacyData == null) continue;
                if (diploCon.DiplomacyData.DiplomacyStatusEnumOfCivs != DiplomacyStatusEnum.War) continue;

                CivEnum enemyEnum;
                if (diploCon.DiplomacyData.CivEnumSideOne == civData.CivEnum)
                    enemyEnum = diploCon.DiplomacyData.CivEnumSideTwo;
                else if (diploCon.DiplomacyData.CivEnumSideTwo == civData.CivEnum)
                    enemyEnum = diploCon.DiplomacyData.CivEnumSideOne;
                else
                    continue;

                CivController enemyCiv = CivManager.Instance.GetCivControllerByCivEnum(enemyEnum);
                if (enemyCiv?.CivData?.StarSysWeOwn == null) continue;

                foreach (var enemySysCon in enemyCiv.CivData.StarSysWeOwn)
                {
                    if (enemySysCon?.StarSysData == null) continue;
                    float distSqr = (enemySysCon.StarSysData.GetPosition() - origin).sqrMagnitude;
                    if (distSqr < nearestDistSqr)
                    {
                        nearestDistSqr = distSqr;
                        nearest = enemySysCon;
                    }
                }
            }
            return nearest;
        }

        /// <summary>
        /// Refreshes DetectedEnemyFleets with every hostile fleet currently within this
        /// system's SubspaceScannerRadius. This is a separate, AI-facing detection layer -
        /// distinct from FischlWorks_FogWar, which only tracks what's visible to the local
        /// player for rendering. Runs for every system regardless of owner or AIBuildMode,
        /// so the data is available for both AI reactions and future player-facing UI.
        /// </summary>
        private static void UpdateSubspaceScanner(StarSysController sysCon)
        {
            var data = sysCon.StarSysData;
            data.DetectedEnemyFleets.Clear();

            if (FleetManager.Instance?.FleetControllersInGame == null) return;

            Vector3 origin = data.GetPosition();
            float radiusSqr = data.SubspaceScannerRadius * data.SubspaceScannerRadius;
            var fogWar = csFogWar.Instance;

            foreach (var fleet in FleetManager.Instance.FleetControllersInGame)
            {
                if (fleet?.FleetData == null || fleet.transform == null) continue;
                if ((fleet.FleetData.ShipsList?.Count ?? 0) == 0) continue;
                if (!IsHostileCiv(data.CurrentOwnerCivEnum, fleet.FleetData.CivEnum)) continue;

                float distSqr = (fleet.transform.position - origin).sqrMagnitude;
                if (distSqr > radiusSqr) continue;

                // Align detection with what's actually rendered on the GalaxyScene map:
                // a fleet outside the local player's revealed fog tiles doesn't count as
                // "detected" even if it's within scanner range.
                if (fogWar != null && fogWar.CheckWorldGridRange(fleet.transform.position) &&
                    !fogWar.CheckVisibility(fleet.transform.position, 0))
                {
                    continue;
                }

                data.DetectedEnemyFleets.Add(fleet);
            }
        }

        /// <summary>
        /// The Borg never negotiate, so they're always treated as hostile to (and by)
        /// everyone else. Otherwise "enemy" means Hostile status or worse (Hostile/ColdWar/
        /// War) - UnFriendly and above isn't a scanner-worthy threat. No diplomacy pair yet
        /// on record means no relationship has soured, so it's treated as not hostile.
        /// </summary>
        private static bool IsHostileCiv(CivEnum ownerEnum, CivEnum otherEnum)
        {
            if (ownerEnum == otherEnum) return false;
            if (ownerEnum == CivEnum.BORG || otherEnum == CivEnum.BORG) return true;

            var diploCon = DiplomacyManager.Instance.ReturnADiplomacyController(ownerEnum, otherEnum);
            if (diploCon?.DiplomacyData == null) return false;
            return diploCon.DiplomacyData.DiplomacyStatusEnumOfCivs <= DiplomacyStatusEnum.Hostile;
        }

        /// <summary>
        /// More warlike + ruthless civs commit to heavier hulls; gentler civs field
        /// cheaper Destroyers/LtCruisers instead.
        /// </summary>
        private static ShipType PickWarShipType(CivData civData)
        {
            int aggression = -(int)civData.Warlike - (int)civData.Ruthless; // range -4..+4, higher = more aggressive
            if (aggression >= 3) return ShipType.HvyCruiser;
            if (aggression >= 1) return ShipType.Cruiser;
            if (aggression >= -1) return ShipType.LtCruiser;
            return ShipType.Destroyer;
        }

        private static bool CanAffordShip(StarSysController sysCon, ShipType shipType)
        {
            var civ = sysCon.StarSysData.CurrentCivController;
            if (civ?.CivData == null) return false;

            int cost = ShipStatCalculator.Calculate(
                shipType, civ.CivData.CurrentTechLevel, sysCon.StarSysData.CurrentOwnerCivEnum, civ.CivData.QualityScore
            ).DilithiumCost;

            return sysCon.StarSysData.HasDilithium(cost);
        }

        /// <summary>
        /// Defence mode powers down Research (it "gets in the way" per design) to free
        /// headroom for Shield/OrbitalBattery, prioritizes building whichever of
        /// Shield/OrbitalBattery/Shipyard is thinnest, and keeps a modest trickle of
        /// cheap picket ships going rather than War mode's dedicated fleet production.
        /// </summary>
        private void ProcessDefenceMode(StarSysController sysCon, CivData civData)
        {
            var data = sysCon.StarSysData;

            TryPowerOffOneFacility(sysCon, StarSysFacilityType.ResearchCenter);
            TryPowerOnOneFacility(sysCon, DefencePowerPriority);

            if (sysCon.StarSysBuildManager == null) return;

            if (sysCon.sysBuildQueueList != null && sysCon.sysBuildQueueList.Count == 0)
            {
                var next = PickNextDefenceFacility(data);
                if (next.HasValue)
                    sysCon.StarSysBuildManager.QueueFacilityBuild(next.Value);
            }

            if (sysCon.sysShipBuildQueueList != null && sysCon.sysShipBuildQueueList.Count == 0)
            {
                var shipType = PickDefenceShipType(civData);
                if (CanAffordShip(sysCon, shipType))
                    sysCon.StarSysBuildManager.QueueShipBuild(shipType);
            }
        }

        /// <summary>
        /// Never grows Factory/Research under Defence - only the hard defensive trio,
        /// power plants jumping ahead when headroom runs tight.
        /// </summary>
        private StarSysFacilityType? PickNextDefenceFacility(StarSysData data)
        {
            if (data.CanBuildPowerPlant() &&
                data.TotalSysPowerOutput - data.TotalSysPowerLoad < data.BasePowerPerPlant)
                return StarSysFacilityType.PowerPlanet;

            var candidates = new (StarSysFacilityType type, int count)[]
            {
                (StarSysFacilityType.ShieldGenerator, data.ShieldGenerators?.Count ?? 0),
                (StarSysFacilityType.OrbitalBattery,  data.OrbitalBatteries?.Count ?? 0),
                (StarSysFacilityType.Shipyard,        data.Shipyards?.Count ?? 0),
            };

            StarSysFacilityType? best = null;
            int bestCount = int.MaxValue;
            foreach (var (type, count) in candidates)
            {
                if (count >= MaxFacilitiesPerType) continue;
                if (count < bestCount)
                {
                    bestCount = count;
                    best = type;
                }
            }
            return best;
        }

        /// <summary>
        /// Xenophobic civs favor cheap Destroyers in numbers; more trusting civs field
        /// slightly heavier LtCruisers instead. Either way this is picket duty, not a fleet.
        /// </summary>
        private static ShipType PickDefenceShipType(CivData civData)
        {
            return (int)civData.Xenophobia <= 0 ? ShipType.Destroyer : ShipType.LtCruiser;
        }

        /// <summary>
        /// Turns off at most one powered-on instance of the given facility type per
        /// call, mirroring TryPowerOnOneFacility's gradual, one-step-per-turn approach.
        /// </summary>
        private void TryPowerOffOneFacility(StarSysController sysCon, StarSysFacilityType type)
        {
            var list = ListFor(sysCon.StarSysData, type);
            if (list == null) return;

            bool hasOnInstance = false;
            foreach (var go in list)
                if (go != null && go.GetComponent<TMPro.TextMeshProUGUI>()?.text == "1")
                {
                    hasOnInstance = true;
                    break;
                }
            if (!hasOnInstance) return;

            switch (type)
            {
                case StarSysFacilityType.Factory: sysCon.FactoryButtonOffClicked(sysCon); break;
                case StarSysFacilityType.Shipyard: sysCon.YardButtonOffClicked(sysCon); break;
                case StarSysFacilityType.ShieldGenerator: sysCon.ShieldButtonOffClicked(sysCon); break;
                case StarSysFacilityType.OrbitalBattery: sysCon.OBButtonOffClicked(sysCon); break;
                case StarSysFacilityType.ResearchCenter: sysCon.ResearchButtonOffClicked(sysCon); break;
            }
        }

        /// <summary>
        /// Turns on at most one powered-off facility per call, in priority order, so
        /// power allocation ramps up gradually across turns instead of all at once.
        /// </summary>
        private void TryPowerOnOneFacility(StarSysController sysCon, StarSysFacilityType[] priority)
        {
            var data = sysCon.StarSysData;
            foreach (var type in priority)
            {
                if (!HasPoweredOffInstance(data, type)) continue;
                if (data.TotalSysPowerLoad + LoadFor(data, type) > data.TotalSysPowerOutput) continue;

                switch (type)
                {
                    case StarSysFacilityType.Factory: sysCon.FactoryButtonOnClicked(sysCon); break;
                    case StarSysFacilityType.Shipyard: sysCon.YardButtonOnClicked(sysCon); break;
                    case StarSysFacilityType.ShieldGenerator: sysCon.ShieldButtonOnClicked(sysCon); break;
                    case StarSysFacilityType.OrbitalBattery: sysCon.OBButtonOnClicked(sysCon); break;
                    case StarSysFacilityType.ResearchCenter: sysCon.ResearchButtonOnClicked(sysCon); break;
                }
                return;
            }
        }

        private static bool HasPoweredOffInstance(StarSysData data, StarSysFacilityType type)
        {
            var list = ListFor(data, type);
            if (list == null) return false;
            foreach (var go in list)
                if (go != null && go.GetComponent<TMPro.TextMeshProUGUI>()?.text == "0")
                    return true;
            return false;
        }

        private static List<GameObject> ListFor(StarSysData data, StarSysFacilityType type)
        {
            switch (type)
            {
                case StarSysFacilityType.Factory: return data.Factories;
                case StarSysFacilityType.Shipyard: return data.Shipyards;
                case StarSysFacilityType.ShieldGenerator: return data.ShieldGenerators;
                case StarSysFacilityType.OrbitalBattery: return data.OrbitalBatteries;
                case StarSysFacilityType.ResearchCenter: return data.ResearchCenters;
                default: return null;
            }
        }

        private static int LoadFor(StarSysData data, StarSysFacilityType type)
        {
            switch (type)
            {
                case StarSysFacilityType.Factory: return data.FactoryData?.PowerLoad ?? 0;
                case StarSysFacilityType.Shipyard: return data.ShipyardData?.PowerLoad ?? 0;
                case StarSysFacilityType.ShieldGenerator: return data.ShieldGeneratorData?.PowerLoad ?? 0;
                case StarSysFacilityType.OrbitalBattery: return data.OrbitalBatteryData?.PowerLoad ?? 0;
                case StarSysFacilityType.ResearchCenter: return data.ResearchCenterData?.PowerLoad ?? 0;
                default: return 0;
            }
        }

        /// <summary>
        /// Trait-weighted pick of the next facility to queue: Greedy civs lean Factory
        /// over Research, Warlike civs lean Shipyard, Xenophobic civs lean defensive
        /// (Shield/OrbitalBattery). Power plants jump the queue whenever headroom gets
        /// tight so new consumers always have somewhere to draw from.
        /// </summary>
        private StarSysFacilityType? PickNextEconomyFacility(CivData civData, StarSysData data)
        {
            if (data.CanBuildPowerPlant() &&
                data.TotalSysPowerOutput - data.TotalSysPowerLoad < data.BasePowerPerPlant)
                return StarSysFacilityType.PowerPlanet;

            var candidates = new (StarSysFacilityType type, float weight, int count)[]
            {
                (StarSysFacilityType.Factory,        3f - (int)civData.Greedy,     data.Factories?.Count ?? 0),
                (StarSysFacilityType.ResearchCenter,  3f + (int)civData.Greedy,     data.ResearchCenters?.Count ?? 0),
                (StarSysFacilityType.Shipyard,        2f - (int)civData.Warlike,    data.Shipyards?.Count ?? 0),
                (StarSysFacilityType.ShieldGenerator, 1f - (int)civData.Xenophobia, data.ShieldGenerators?.Count ?? 0),
                (StarSysFacilityType.OrbitalBattery,  1f - (int)civData.Xenophobia, data.OrbitalBatteries?.Count ?? 0),
            };

            StarSysFacilityType? best = null;
            float bestScore = float.MinValue;
            foreach (var (type, weight, count) in candidates)
            {
                if (count >= MaxFacilitiesPerType) continue;
                float score = weight - count * 2f; // favors whichever type is most under-built relative to desire
                if (score > bestScore)
                {
                    bestScore = score;
                    best = type;
                }
            }
            return best;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<StarSysAIManager>();
        }
    }
}
