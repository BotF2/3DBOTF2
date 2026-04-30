using BOTF3D.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Core
{
    /// <summary>
    /// Manages technology progression, research points, and tech-related bonuses for all civilizations
    /// </summary>
    public class TechManager : MonoBehaviour
    {
        public static TechManager Instance { get; private set; }

        [Header("Tech Level Thresholds")]
        [Tooltip("Tech points needed to reach each level")]
        public int EarlyThreshold = 100;
        public int DevelopedThreshold = 300;
        public int AdvancedThreshold = 600;
        public int SupremeThreshold = 1000;

        [Header("Granular Ship Unlock Thresholds")]
        [Tooltip("Tech points for unlocking ships within tech levels")]
        public int EarlyScoutUnlock = 0;          // Scout_I unlocks immediately
        public int EarlyDestroyerUnlock = 25;     // Destroyer_I unlocks at 25 points
        public int EarlyTransportUnlock = 50;     // Transport_I unlocks at 50 points

        public int DevelopedBaseUnlock = 100;     // Basic ships unlock when reaching DEVELOPED
        public int DevelopedCruiserUnlock = 150;  // Cruiser_II unlocks at 150 points
        public int DevelopedAdvancedUnlock = 200; // Improved stats unlock at 200 points

        public int AdvancedBaseUnlock = 300;      // Basic ships unlock when reaching ADVANCED
        public int AdvancedCruiserUnlock = 400;   // Cruiser_III unlocks at 400 points
        public int AdvancedEliteUnlock = 500;     // Elite variants unlock at 500 points

        public int SupremeBaseUnlock = 600;       // Basic ships unlock when reaching SUPREME
        public int SupremeLtCruiserUnlock = 700;  // LtCruiser_IV unlocks at 700 points
        public int SupremeHvyCruiserUnlock = 850; // HvyCruiser_IV unlocks at 850 points


        [Header("Research Multipliers")]
        [Tooltip("Base research points per Research Center per turn")]
        public int BaseResearchPerCenter = 5;

        [Header("Tech Bonuses")]
        [Tooltip("Power efficiency at each tech level")]
        public float[] PowerEfficiencyMultipliers = { 1.0f, 1.2f, 1.5f, 2.0f };

        [Tooltip("Factory production speed at each tech level")]
        public float[] FactorySpeedMultipliers = { 1.0f, 1.15f, 1.35f, 1.6f };

        [Tooltip("Shipyard build speed at each tech level")]
        public float[] ShipyardSpeedMultipliers = { 1.0f, 1.2f, 1.4f, 1.8f };

        [Tooltip("Shield strength at each tech level")]
        public float[] ShieldStrengthMultipliers = { 1.0f, 1.25f, 1.6f, 2.0f };

        [Tooltip("Research output at each tech level (recursive bonus)")]
        public float[] ResearchOutputMultipliers = { 1.0f, 1.1f, 1.25f, 1.5f };

        /// <summary>
        /// Event fired when a civilization advances to a new tech level
        /// Parameters: CivEnum, old TechLevel, new TechLevel
        /// </summary>
        public event Action<CivEnum, TechLevel, TechLevel> OnTechLevelAdvanced;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("TechManager: Duplicate instance detected, destroying");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ TechManager initialized");
        }

        #region Tech Level Calculation

        /// <summary>
        /// Calculate tech level based on current tech points
        /// </summary>
        public Core.TechLevel GetTechLevelFromPoints(int techPoints)
        {
            if (techPoints >= SupremeThreshold) return Core.TechLevel.SUPREME;
            if (techPoints >= AdvancedThreshold) return Core.TechLevel.ADVANCED;
            if (techPoints >= DevelopedThreshold) return Core.TechLevel.DEVELOPED;
            return Core.TechLevel.EARLY;
            //public int EarlyThreshold = 100;
            //public int DevelopedThreshold = 300;
            //public int AdvancedThreshold = 600;
            //public int SupremeThreshold = 1000;
        }

        /// <summary>
        /// Get tech points needed for next level
        /// </summary>
        public int GetPointsNeededForNextLevel(Core.TechLevel currentLevel)
        {
            switch (currentLevel)
            {
                case Core.TechLevel.EARLY: return DevelopedThreshold;
                case Core.TechLevel.DEVELOPED: return AdvancedThreshold;
                case Core.TechLevel.ADVANCED: return SupremeThreshold;
                case Core.TechLevel.SUPREME: return SupremeThreshold; // Max level
                default: return DevelopedThreshold;
            }
        }

        /// <summary>
        /// Get progress percentage toward next tech level
        /// </summary>
        public float GetProgressToNextLevel(int currentPoints, Core.TechLevel currentLevel)
        {
            int currentThreshold = GetCurrentLevelThreshold(currentLevel);
            int nextThreshold = GetPointsNeededForNextLevel(currentLevel);

            if (currentLevel == Core.TechLevel.SUPREME)
                return 1f; // Already at max

            float progress = (float)(currentPoints - currentThreshold) / (nextThreshold - currentThreshold);
            return Mathf.Clamp01(progress);
        }

        /// <summary>
        /// Get the minimum points needed for current tech level
        /// </summary>
        public int GetCurrentLevelThreshold(Core.TechLevel level)
        {
            switch (level)
            {
                case Core.TechLevel.EARLY: return 0;
                case Core.TechLevel.DEVELOPED: return EarlyThreshold;
                case Core.TechLevel.ADVANCED: return DevelopedThreshold;
                case Core.TechLevel.SUPREME: return AdvancedThreshold;
                default: return 0;
            }
        }

        #endregion

        #region Research Generation

        /// <summary>
        /// Calculate total research points generated by a civilization this turn
        /// </summary>
        public int CalculateResearchPointsPerTurn(Core.CivData civData)
        {
            if (civData == null) return 0;

            int totalResearch = 0;
            float techMultiplier = GetResearchOutputMultiplier(civData.TechLevel);

            // Get research from all owned systems
            var civController = Core.CivManager.Instance?.GetCivControllerByCivEnum(civData.CivEnum);
            if (civController?.CivData?.StarSysWeOwn != null)
            {
                foreach (var system in civController.CivData.StarSysWeOwn)
                {
                    if (system?.StarSysData?.ResearchCenters != null)
                    {
                        int researchCenters = system.StarSysData.ResearchCenters.Count;
                        totalResearch += Mathf.RoundToInt(researchCenters * BaseResearchPerCenter * techMultiplier);
                    }
                }
            }

            return totalResearch;
        }

        /// <summary>
        /// Add research points to a civilization and check for tech level advancement
        /// </summary>
        public void AddResearchPoints(Core.CivData civData, int points)
        {
            if (civData == null) return;

            Core.TechLevel oldLevel = civData.TechLevel;
            civData.TechPoints += points;

            Core.TechLevel newLevel = GetTechLevelFromPoints(civData.TechPoints);

            if (newLevel != oldLevel)
            {
                civData.TechLevel = newLevel;
                HandleTechLevelAdvanced(civData, oldLevel, newLevel);
            }
        }

        /// <summary>
        /// Called when a civilization advances to a new tech level
        /// </summary>
        private void HandleTechLevelAdvanced(Core.CivData civData, Core.TechLevel oldLevel, Core.TechLevel newLevel)
        {
            Debug.Log($"🔬 {civData.CivShortName} advanced from {oldLevel} to {newLevel}!");

            // ✅ Fire event for UI listeners
            OnTechLevelAdvanced?.Invoke(civData.CivEnum, oldLevel, newLevel);

            // Unlock new ship types
            UnlockShipsForTechLevel(civData.CivEnum, newLevel);

            // ✅ NEW: Refresh build UIs for all systems owned by this civ
            RefreshBuildUIsForCiv(civData.CivEnum);

            // TODO: Trigger UI notification
            // TODO: Play sound effect
            // TODO: Update available buildings/facilities
        }

        /// <summary>
        /// Refresh build UIs for all systems owned by a civilization
        /// Call this when tech level changes to update available ships
        /// </summary>
        private void RefreshBuildUIsForCiv(CivEnum civEnum)
        {
            if (StarSysManager.Instance == null) return;

            var civController = CivManager.Instance?.GetCivControllerByCivEnum(civEnum);
            if (civController?.CivData?.StarSysWeOwn == null) return;

            Debug.Log($"  Refreshing build UIs for {civController.CivData.StarSysWeOwn.Count} systems owned by {civEnum}");

            foreach (var system in civController.CivData.StarSysWeOwn)
            {
                if (system != null)
                {
                    // If the build UI is currently open for this system, refresh it
                    RefreshSystemBuildUI(system);
                }
            }
        }

        /// <summary>
        /// Refresh the build UI for a specific system
        /// </summary>
        private void RefreshSystemBuildUI(GamePlay.StarSysController sysCon)
        {
            // Check if this system's build UI is currently open
            if (StarSysMenuUIController.Instance != null &&
                StarSysMenuUIController.Instance.ActiveStarSysController == sysCon)
            {
                // Find the active build UI instance
                GameObject buildUI = GameObject.Find("SysBuildUIList(Clone)");
                if (buildUI != null)
                {
                    Debug.Log($"    ✅ Refreshing build UI for system '{sysCon.name}'");

                    // Re-run the tech level filter
                    if (StarSysManager.Instance != null)
                    {
                        StarSysManager.Instance.UpdateAvailableShipsByTechLevel(sysCon, buildUI);
                    }
                }
            }
        }

        #endregion

        #region Multiplier Getters

        /// <summary>
        /// Get power efficiency multiplier for a tech level
        /// </summary>
        public float GetPowerEfficiencyMultiplier(Core.TechLevel level)
        {
            int index = (int)level;
            if (index >= 0 && index < PowerEfficiencyMultipliers.Length)
                return PowerEfficiencyMultipliers[index];
            return 1.0f;
        }

        /// <summary>
        /// Get factory speed multiplier for a tech level
        /// </summary>
        public float GetFactorySpeedMultiplier(Core.TechLevel level)
        {
            int index = (int)level;
            if (index >= 0 && index < FactorySpeedMultipliers.Length)
                return FactorySpeedMultipliers[index];
            return 1.0f;
        }

        /// <summary>
        /// Get shipyard speed multiplier for a tech level
        /// </summary>
        public float GetShipyardSpeedMultiplier(Core.TechLevel level)
        {
            int index = (int)level;
            if (index >= 0 && index < ShipyardSpeedMultipliers.Length)
                return ShipyardSpeedMultipliers[index];
            return 1.0f;
        }

        /// <summary>
        /// Get shield strength multiplier for a tech level
        /// </summary>
        public float GetShieldStrengthMultiplier(Core.TechLevel level)
        {
            int index = (int)level;
            if (index >= 0 && index < ShieldStrengthMultipliers.Length)
                return ShieldStrengthMultipliers[index];
            return 1.0f;
        }

        /// <summary>
        /// Get research output multiplier for a tech level (recursive bonus)
        /// </summary>
        public float GetResearchOutputMultiplier(Core.TechLevel level)
        {
            int index = (int)level;
            if (index >= 0 && index < ResearchOutputMultipliers.Length)
                return ResearchOutputMultipliers[index];
            return 1.0f;
        }

        #endregion

        #region Ship Unlocking

        /// <summary>
        /// Check if a specific ship is unlocked based on tech points (granular unlocking)
        /// </summary>
        public bool IsShipUnlockedByPoints(ShipSO shipSO, int currentTechPoints)
        {
            if (shipSO == null) return false;

            // Ship is unlocked if current tech points >= ship's minimum required points
            return currentTechPoints >= shipSO.MinTechPointsRequired;
        }

        /// <summary>
        /// Get all ships unlocked for a civilization at their current tech points
        /// </summary>
        public List<ShipSO> GetUnlockedShips(Core.CivEnum civEnum, int currentTechPoints)
        {
            var unlockedShips = new List<ShipSO>();

            if (ShipManager.Instance == null) return unlockedShips;

            // Get all ships for this civ
            var allCivShips = ShipManager.Instance.GetShipSOListByCiv(civEnum);

            foreach (var ship in allCivShips)
            {
                if (ship != null && IsShipUnlockedByPoints(ship, currentTechPoints))
                {
                    unlockedShips.Add(ship);
                }
            }

            return unlockedShips;
        }

        /// <summary>
        /// Get recommended tech point thresholds for ship types
        /// Use this when creating ShipSOs to set MinTechPointsRequired
        /// </summary>
        public int GetRecommendedUnlockPoints(Core.ShipType shipType, Core.TechLevel techLevel)
        {
            // Base unlocks for each tech level
            switch (techLevel)
            {
                case Core.TechLevel.EARLY:
                    switch (shipType)
                    {
                        case Core.ShipType.Scout: return EarlyScoutUnlock;
                        case Core.ShipType.Destroyer: return EarlyDestroyerUnlock;
                        case Core.ShipType.Transport: return EarlyTransportUnlock;
                        default: return 0;
                    }

                case Core.TechLevel.DEVELOPED:
                    switch (shipType)
                    {
                        case Core.ShipType.Scout:
                        case Core.ShipType.Destroyer:
                        case Core.ShipType.Transport:
                            return DevelopedBaseUnlock; // _II variants
                        case Core.ShipType.Cruiser:
                            return DevelopedCruiserUnlock; // Cruiser_II
                        default: return DevelopedBaseUnlock;
                    }

                case Core.TechLevel.ADVANCED:
                    switch (shipType)
                    {
                        case Core.ShipType.Scout:
                        case Core.ShipType.Destroyer:
                        case Core.ShipType.Transport:
                            return AdvancedBaseUnlock; // _III variants
                        case Core.ShipType.Cruiser:
                            return AdvancedCruiserUnlock; // Cruiser_III
                        default: return AdvancedBaseUnlock;
                    }

                case Core.TechLevel.SUPREME:
                    switch (shipType)
                    {
                        case Core.ShipType.Scout:
                        case Core.ShipType.Destroyer:
                        case Core.ShipType.Transport:
                            return SupremeBaseUnlock; // _IV variants
                        case Core.ShipType.LtCruiser:
                            return SupremeLtCruiserUnlock; // LtCruiser_IV
                        case Core.ShipType.HvyCruiser:
                            return SupremeHvyCruiserUnlock; // HvyCruiser_IV
                        default: return SupremeBaseUnlock;
                    }

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Unlock ship types available at a given tech level
        /// </summary>
        private void UnlockShipsForTechLevel(Core.CivEnum civEnum, Core.TechLevel techLevel)
        {
            // Get all ship SOs for this civilization
            if (ShipManager.Instance != null)
            {
                var civData = CivManager.Instance?.GetCivDataByCivEnum(civEnum);
                if (civData != null)
                {
                    var unlockedShips = GetUnlockedShips(civEnum, civData.TechPoints);
                    Debug.Log($"  {civEnum} has {unlockedShips.Count} ships unlocked at {civData.TechPoints} tech points");
                }
            }
        }

        /// <summary>
        /// Check if a ship type is unlocked for a civilization (legacy method)
        /// </summary>
        public bool IsShipUnlocked(Core.CivEnum civEnum, Core.ShipType shipType, Core.TechLevel civTechLevel)
        {
            // Define unlock requirements
            Core.TechLevel requiredLevel = GetRequiredTechLevelForShip(shipType);
            return civTechLevel >= requiredLevel;
        }

        /// <summary>
        /// Get the minimum tech level required for a ship type (base requirement)
        /// </summary>
        public Core.TechLevel GetRequiredTechLevelForShip(Core.ShipType shipType)
        {
            switch (shipType)
            {
                case Core.ShipType.Scout:
                case Core.ShipType.Transport:
                case Core.ShipType.Destroyer:
                    return Core.TechLevel.EARLY; // Available from start

                case Core.ShipType.Cruiser:
                    return Core.TechLevel.DEVELOPED; // Cruiser_II at DEVELOPED

                case Core.ShipType.LtCruiser:
                case Core.ShipType.HvyCruiser:
                    return Core.TechLevel.SUPREME; // Only at SUPREME

                default:
                    return Core.TechLevel.EARLY;
            }
        }

        #endregion

        #region Turn Processing

        /// <summary>
        /// Process research for all civilizations (call this each turn)
        /// </summary>
        public void ProcessResearchForAllCivs()
        {
            if (Core.CivManager.Instance == null) return;

            var allCivs = Core.CivManager.Instance.GetAllCivControllers();
            foreach (var civController in allCivs)
            {
                if (civController?.CivData != null && civController.CivData.Playable)
                {
                    int researchPoints = CalculateResearchPointsPerTurn(civController.CivData);
                    if (researchPoints > 0)
                    {
                        AddResearchPoints(civController.CivData, researchPoints);
                        Debug.Log($"🔬 {civController.CivData.CivShortName} gained {researchPoints} research points");
                    }
                }
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// Debug: Add tech points to local player (for testing)
        /// </summary>
        [ContextMenu("Debug: Add 50 Tech Points")]
        private void DebugAdd50TechPoints()
        {
            var localPlayer = GameController.Instance?.GameData?.LocalPlayerCivEnum;
            if (localPlayer != null)
            {
                var civData = CivManager.Instance?.GetCivDataByCivEnum(localPlayer.Value);
                if (civData != null)
                {
                    AddResearchPoints(civData, 50);
                    Debug.Log($"✅ Added 50 tech points to {civData.CivShortName}. Total: {civData.TechPoints}");
                }
            }
        }

        /// <summary>
        /// Debug: Advance to next tech level immediately
        /// </summary>
        [ContextMenu("Debug: Advance to Next Tech Level")]
        private void DebugAdvanceToNextLevel()
        {
            var localPlayer = GameController.Instance?.GameData?.LocalPlayerCivEnum;
            if (localPlayer != null)
            {
                var civData = CivManager.Instance?.GetCivDataByCivEnum(localPlayer.Value);
                if (civData != null)
                {
                    int pointsNeeded = GetPointsNeededForNextLevel(civData.TechLevel);
                    int pointsToAdd = pointsNeeded - civData.TechPoints + 1;

                    AddResearchPoints(civData, pointsToAdd);
                    Debug.Log($"✅ Advanced {civData.CivShortName} to {civData.TechLevel}");
                }
            }
        }
#endif

        #endregion
    }
}
