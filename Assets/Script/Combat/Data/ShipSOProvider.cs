using BOTF3D.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Provides ShipSO (scriptable objects) based on civ, tech level, and ship type.
    /// Central repository for ship template queries.
    /// </summary>
    public class ShipSOProvider
    {
        // Ship lists by civilization
        private readonly List<ShipSO> fedShipSOList;
        private readonly List<ShipSO> romShipSOList;
        private readonly List<ShipSO> klingShipSOList;
        private readonly List<ShipSO> cardShipSOList;
        private readonly List<ShipSO> domShipSOList;
        private readonly List<ShipSO> borgShipSOList;
        private readonly List<ShipSO> terranShipSOList;
        private readonly List<ShipSO> minorShipSOList;

        public ShipSOProvider(
            List<ShipSO> fed,
            List<ShipSO> rom,
            List<ShipSO> kling,
            List<ShipSO> card,
            List<ShipSO> dom,
            List<ShipSO> borg,
            List<ShipSO> terran,
            List<ShipSO> minor)
        {
            fedShipSOList = fed;
            romShipSOList = rom;
            klingShipSOList = kling;
            cardShipSOList = card;
            domShipSOList = dom;
            borgShipSOList = borg;
            terranShipSOList = terran;
            minorShipSOList = minor;
        }

        /// <summary>
        /// Get ship SO list for a specific civilization
        /// </summary>
        public List<ShipSO> GetShipSOListByCiv(CivEnum civEnum)
        {
            switch (civEnum)
            {
                case CivEnum.FED: return fedShipSOList;
                case CivEnum.ROM: return romShipSOList;
                case CivEnum.KLING: return klingShipSOList;
                case CivEnum.CARD: return cardShipSOList;
                case CivEnum.DOM: return domShipSOList;
                case CivEnum.BORG: return borgShipSOList;
                case CivEnum.TERRAN: return terranShipSOList;
                default:
                    // Search minor ship list for the given minor civ
                    if (minorShipSOList == null || minorShipSOList.Count == 0)
                    {
                        Debug.LogWarning($"GetShipSOListByCiv: MinorShipSOList is empty!");
                        return new List<ShipSO>();
                    }

                    var minorCivShips = minorShipSOList
                        .Where(s => s != null && s.CivEnum == civEnum)
                        .ToList();

                    if (minorCivShips.Count == 0)
                    {
                        Debug.LogWarning($"GetShipSOListByCiv: No ships found for minor civ {civEnum}");
                    }

                    return minorCivShips;
            }
        }

        /// <summary>
        /// Get a specific ship SO by type and tech level
        /// </summary>
        public ShipSO GetShipSO(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
        {
            List<ShipSO> civShips = GetShipSOListByCiv(civEnum);

            if (civShips == null || civShips.Count == 0)
            {
                Debug.LogWarning($"GetShipSO: No ships found for {civEnum}");
                return null;
            }

            // Find ship matching type AND tech level
            ShipSO foundShip = civShips.FirstOrDefault(s =>
                s != null && s.ShipType == shipType && s.TechLevel == techLevel);

            if (foundShip == null)
            {
                Debug.LogWarning($"GetShipSO: No {shipType} found for {civEnum} at {techLevel} - searching fallback...");

                // Fallback: Try Scout at EARLY tech
                foundShip = civShips.FirstOrDefault(s =>
                    s != null && s.ShipType == ShipType.Scout && s.TechLevel == TechLevel.EARLY);

                if (foundShip != null)
                {
                    Debug.Log($"  ✅ Using fallback: {foundShip.ShipName}");
                }
            }

            return foundShip;
        }

        /// <summary>
        /// Get ship SO at the BEST available tech level for the civ
        /// Searches from current tech level DOWN
        /// </summary>
        public ShipSO GetShipSOAtBestTechLevel(ShipType shipType, TechLevel maxTechLevel, CivEnum civEnum)
        {
            List<ShipSO> civShips = GetShipSOListByCiv(civEnum);

            if (civShips == null || civShips.Count == 0)
            {
                Debug.LogWarning($"GetShipSOAtBestTechLevel: No ships found for {civEnum}");
                return null;
            }

            // Find ALL ships of this type at or below max tech level
            var candidateShips = civShips
                .Where(s => s != null && s.ShipType == shipType && s.TechLevel <= maxTechLevel)
                .OrderByDescending(s => s.TechLevel) // Highest tech first
                .ToList();

            if (candidateShips.Count > 0)
            {
                ShipSO bestShip = candidateShips[0];
                Debug.Log($"GetShipSOAtBestTechLevel: Found {shipType} for {civEnum} at {bestShip.TechLevel} (max allowed: {maxTechLevel})");
                return bestShip;
            }

            // Not every ship type is unlocked at every tech level (e.g. Cruiser-class hulls
            // don't exist at EARLY for most civs) - this is expected, not a problem.
            return null;
        }

        /// <summary>
        /// Get all ships available to a civilization at their current tech level (and below)
        /// </summary>
        public List<ShipSO> GetAvailableShipsForCiv(CivEnum civEnum, TechLevel currentTechLevel)
        {
            List<ShipSO> allCivShips = GetShipSOListByCiv(civEnum);

            if (allCivShips == null || allCivShips.Count == 0)
            {
                Debug.LogWarning($"GetAvailableShipsForCiv: No ships found for {civEnum}");
                return new List<ShipSO>();
            }

            // Remove null entries
            allCivShips = allCivShips.Where(s => s != null).ToList();

            // Filter: Only include ships at or below current tech level
            List<ShipSO> availableShips = allCivShips
                .Where(s => s.TechLevel <= currentTechLevel)
                .ToList();

            Debug.Log($"GetAvailableShipsForCiv: {civEnum} at {currentTechLevel} has {availableShips.Count}/{allCivShips.Count} ships available");

            return availableShips;
        }

        /// <summary>
        /// Check if a specific ship type is available for a civilization
        /// </summary>
        public bool IsShipTypeAvailable(ShipType shipType, CivEnum civEnum, TechLevel currentTechLevel)
        {
            ShipSO ship = GetShipSO(shipType, currentTechLevel, civEnum);

            if (ship == null)
            {
                // Try to find ANY version of this ship type for this civ
                List<ShipSO> civShips = GetShipSOListByCiv(civEnum);
                ship = civShips?.FirstOrDefault(s => s != null && s.ShipType == shipType);

                if (ship == null)
                {
                    Debug.Log($"IsShipTypeAvailable: {civEnum} has no {shipType} at any tech level");
                    return false;
                }
            }

            bool available = ship.TechLevel <= currentTechLevel;
            Debug.Log($"IsShipTypeAvailable: {shipType} for {civEnum} - Required: {ship.TechLevel}, Current: {currentTechLevel}, Available: {available}");
            return available;
        }

        /// <summary>
        /// Get fallback ship SO (default ship when nothing else works)
        /// </summary>
        public ShipSO GetFallbackShipSO()
        {
            return GetShipSO(ShipType.Destroyer, TechLevel.EARLY, CivEnum.FED);
        }

        // Starting-fleet composition for playable major civs (FED..TERRAN), keyed by tech level:
        // ship type -> how many of that type Fleet 1 starts with. Every major civ is built from
        // this same table, sourced from its own per-civ ShipSO list, so parity across civs is
        // automatic rather than hand-matched per civ. Add an entry here (and the matching
        // TechLevel_ShipSO assets) when a higher tech level unlocks Cruiser-class hulls.
        private static readonly Dictionary<TechLevel, Dictionary<ShipType, int>> MajorStartingFleetComposition =
            new Dictionary<TechLevel, Dictionary<ShipType, int>>
            {
                [TechLevel.EARLY] = new Dictionary<ShipType, int>
                {
                    { ShipType.Scout, 6 },
                    { ShipType.Destroyer, 6 },
                    { ShipType.Transport, 2 },
                }
            };

        /// <summary>
        /// Get ships for a civilization's starting fleet.
        /// Major races (FED..TERRAN) get the composition defined in MajorStartingFleetComposition
        /// for the given tech level. Minor races get 1 ship (Destroyer, or Scout as fallback).
        /// </summary>
        public List<ShipSO> GetStartingFleetShips(TechLevel techLevel, CivEnum civEnum)
        {
            List<ShipSO> allCivShips = GetShipSOListByCiv(civEnum);

            if (allCivShips == null || allCivShips.Count == 0)
            {
                Debug.LogWarning($"GetStartingFleetShips: No ships found for {civEnum}");
                return new List<ShipSO>();
            }

            // Filter by tech level
            var techLevelShips = allCivShips
                .Where(s => s != null && s.TechLevel == techLevel)
                .ToList();

            if (techLevelShips.Count == 0)
            {
                Debug.LogWarning($"GetStartingFleetShips: No ships found for {civEnum} at {techLevel}");
                return new List<ShipSO>();
            }

            // Check if major race (FED through TERRAN)
            bool isMajorRace = civEnum >= CivEnum.FED && civEnum <= CivEnum.TERRAN;

            if (isMajorRace)
            {
                if (!MajorStartingFleetComposition.TryGetValue(techLevel, out var composition))
                {
                    Debug.LogWarning($"GetStartingFleetShips: No starting-fleet composition defined for {civEnum} at {techLevel}");
                    return new List<ShipSO>();
                }

                List<ShipSO> startingFleet = new List<ShipSO>();
                foreach (var entry in composition)
                {
                    ShipSO template = techLevelShips.FirstOrDefault(s => s.ShipType == entry.Key);
                    if (template == null)
                    {
                        Debug.LogWarning($"GetStartingFleetShips: {civEnum} has no {entry.Key} at {techLevel}; skipping");
                        continue;
                    }

                    for (int i = 0; i < entry.Value; i++)
                        startingFleet.Add(template);
                }

                Debug.Log($"✅ GetStartingFleetShips: {civEnum} starting fleet has {startingFleet.Count} ships");
                return startingFleet;
            }
            else
            {
                // Minor races get ONE ship (Destroyer, or Scout as fallback)
                ShipSO destroyer = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Destroyer);
                if (destroyer != null)
                {
                    return new List<ShipSO> { destroyer };
                }

                ShipSO scout = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Scout);
                if (scout != null)
                {
                    return new List<ShipSO> { scout };
                }

                Debug.LogError($"❌ GetStartingFleetShips: Minor race {civEnum} has no destroyer or scout!");
                return new List<ShipSO>();
            }
        }

        /// <summary>
        /// Find a usable FBX model prefab to stand in for a ShipSO that has none assigned.
        /// Search order: same civ + same ship type (any tech level) -> the global fallback
        /// ship SO (FED Destroyer EARLY) -> any civ + same ship type.
        /// Returns null if no FBX can be found anywhere.
        /// </summary>
        public GameObject GetFallbackFbx(ShipType shipType, CivEnum civEnum)
        {
            List<ShipSO> civShips = GetShipSOListByCiv(civEnum);
            ShipSO sameTypeSameCiv = civShips?.FirstOrDefault(s =>
                s != null && s.ShipType == shipType && s.ShipFBX_ModelAsGOPrefab != null);
            if (sameTypeSameCiv != null)
            {
                return sameTypeSameCiv.ShipFBX_ModelAsGOPrefab;
            }

            ShipSO globalFallback = GetFallbackShipSO();
            if (globalFallback != null && globalFallback.ShipFBX_ModelAsGOPrefab != null)
            {
                return globalFallback.ShipFBX_ModelAsGOPrefab;
            }

            var allLists = new[]
            {
                fedShipSOList, romShipSOList, klingShipSOList, cardShipSOList,
                domShipSOList, borgShipSOList, terranShipSOList, minorShipSOList
            };

            foreach (var list in allLists)
            {
                ShipSO anyCivSameType = list?.FirstOrDefault(s =>
                    s != null && s.ShipType == shipType && s.ShipFBX_ModelAsGOPrefab != null);
                if (anyCivSameType != null)
                {
                    return anyCivSameType.ShipFBX_ModelAsGOPrefab;
                }
            }

            return null;
        }
    }
}
