
using BOTF3D.Core;
using BOTF3D.Galaxy;
using System.Collections.Generic;
using UnityEngine;



namespace BOTF3D.Civilization
{
    public class CivData // has list of civsInGame, it's starSytems Data
    {
        public int CivInt;
        public CivEnum CivEnum;
        public int PlayerId; // network player ID, not used in single player
        public string CivShortName;
        public string CivLongName;
        public string CivHomeSystemName;
        public Vector3 HomeStarSystemPosition;
        public WarLikeEnum Warlike;// a enum scale from most work like 0 to neutral 3 and most peaceful 5
        public XenophobiaEnum Xenophobia; // XenophobiaEnum
        public RuthlessEnum Ruthless; //XenophobiaEnum
        public GreedyEnum Greedy; //XenophobiaEnum
        public Sprite CivRaceSprite;
        public Sprite InsigniaSprite;
        public static readonly Dictionary<TechLevel, int> TechThresholds = new()
        {
            { TechLevel.EARLY, 0 },
            { TechLevel.DEVELOPED, 100 },
            { TechLevel.ADVANCED, 300 },
            { TechLevel.SUPREME, 600 }
        };
        public int TechPoints { get; set; } = 0;
        public TechLevel CurrentTechLevel { get; set; } = TechLevel.EARLY;
        public bool Playable;
        public bool PlayedByAI = true;
        public CivEnum LocalPlayerCivEnum;
        public bool HasWarp;
        public string Decription = "We are the Borg";
        public List<StarSysController> StarSysWeOwn;
        //public List<CivController> CivControllersWeKnow;
        //public List<CivEnum> CivEnumsWeKnow;
        public float IntelPoints;
        private object SystemsOwned;
        public int PendingBuildTimeReduction = 0; // Consumed by next ship build at any owned shipyard; set from captured-ship BuildDuration / 2

        /// <summary>
        /// Get power efficiency multiplier based on tech level
        /// Uses TechManager for centralized tech bonuses
        /// </summary>
        public float GetPowerTechMultiplier()
        {
            if (TechManager.Instance != null)
            {
                return TechManager.Instance.GetPowerEfficiencyMultiplier(CurrentTechLevel);
            }

            // Fallback if TechManager not available
            switch (CurrentTechLevel)
            {
                case TechLevel.EARLY: return 1.0f;
                case TechLevel.DEVELOPED: return 1.2f;
                case TechLevel.ADVANCED: return 1.5f;
                case TechLevel.SUPREME: return 2f;
                default: return 1.0f;
            }
        }
        /// <summary>
        /// Calculate total power output across all owned systems
        /// </summary>
        public float CalculateTotalEmpirePower()
        {
            float totalPower = 0f;
            float techMultiplier = GetPowerTechMultiplier();

            // Loop through all owned systems
            var civController = CivManager.Instance?.GetCivControllerByCivEnum(CivEnum);
            if (civController?.CivData?.StarSysWeOwn != null)
            {
                foreach (var system in civController.CivData.StarSysWeOwn)
                {
                    if (system?.StarSysData != null)
                    {
                        totalPower += system.StarSysData.CalculateTotalPower(techMultiplier);
                    }
                }
            }

            return totalPower;
        }

        /// <summary>
        /// Add tech points and check for level advancement
        /// Convenience method that delegates to TechManager
        /// </summary>
        public void AddTechPoints(int points)
        {
            if (TechManager.Instance != null)
            {
                TechManager.Instance.AddResearchPoints(CivManager.Instance.GetCivControllerByCivEnum(CivEnum), points);
            }
            else
            {
                // Fallback if TechManager not available
                TechLevel oldLevel = CurrentTechLevel;
                TechPoints += points;

                // Simple threshold check
                if (TechPoints >= 600 && oldLevel != TechLevel.SUPREME)
                    CurrentTechLevel = TechLevel.SUPREME;
                else if (TechPoints >= 300 && oldLevel != TechLevel.ADVANCED)
                    CurrentTechLevel = TechLevel.ADVANCED;
                else if (TechPoints >= 100 && oldLevel != TechLevel.DEVELOPED)
                    CurrentTechLevel = TechLevel.DEVELOPED;
                else
                    CurrentTechLevel = TechLevel.EARLY;
            }
        }

        /// <summary>
        /// Get progress toward next tech level (0-1)
        /// </summary>
        public float GetTechProgress()
        {
            if (TechManager.Instance == null) return 0f;
            return TechManager.Instance.GetProgressToNextLevel(this);
        }
    }
}

