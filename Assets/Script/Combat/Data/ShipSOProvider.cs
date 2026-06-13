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

            Debug.LogWarning($"GetShipSOAtBestTechLevel: No {shipType} found for {civEnum} at or below {maxTechLevel}");
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

            // Filter: ship type must pass the global TechLevel gate AND civ must own an SO for it
            List<ShipSO> availableShips = allCivShips
                .Where(s => ShipTypeProfiles.IsAvailableAtTechLevel(s.ShipType, currentTechLevel))
                .ToList();

            Debug.Log($"GetAvailableShipsForCiv: {civEnum} at {currentTechLevel} has {availableShips.Count}/{allCivShips.Count} ships available");

            return availableShips;
        }

        /// <summary>
        /// Check if a specific ship type is available for a civilization.
        /// Uses ShipTypeProfiles.IsAvailableAtTechLevel as the primary gate, then verifies
        /// that the civ actually has an SO for that type (so phantom ship types are blocked).
        /// </summary>
        public bool IsShipTypeAvailable(ShipType shipType, CivEnum civEnum, TechLevel currentTechLevel)
        {
            if (!ShipTypeProfiles.IsAvailableAtTechLevel(shipType, currentTechLevel))
            {
                Debug.Log($"IsShipTypeAvailable: {shipType} gated at {currentTechLevel}");
                return false;
            }

            List<ShipSO> civShips = GetShipSOListByCiv(civEnum);
            bool hasSO = civShips?.Any(s => s != null && s.ShipType == shipType) ?? false;
            Debug.Log($"IsShipTypeAvailable: {shipType} for {civEnum} @ {currentTechLevel} — gate=pass, hasSO={hasSO}");
            return hasSO;
        }

        /// <summary>
        /// Get fallback ship SO (default ship when nothing else works)
        /// </summary>
        public ShipSO GetFallbackShipSO()
        {
            return GetShipSO(ShipType.Destroyer, TechLevel.EARLY, CivEnum.FED);
        }

        /// <summary>
        /// Get ships for a civilization's starting fleet
        /// Major races get 3 ships (Destroyer, Scout, Transport)
        /// Minor races get 1 ship (Destroyer or Scout)
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
                // Major races get THREE ships
                List<ShipSO> startingFleet = new List<ShipSO>();

                ShipSO destroyer = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Destroyer);
                ShipSO scout = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Scout);
                ShipSO transport = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Transport);

                if (destroyer != null) startingFleet.Add(destroyer);
                if (scout != null) startingFleet.Add(scout);
                if (transport != null) startingFleet.Add(transport);

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
    }
}
