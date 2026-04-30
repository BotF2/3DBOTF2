using BOTF3D.GamePlay;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Core
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
        public XenophobiaEnum Xenophbia; // XenophobiaEnum
        public RuthlessEnum Ruthelss; //XenophobiaEnum
        public GreedyEnum Greedy; //XenophobiaEnum
        public Sprite CivRaceSprite;
        public Sprite InsigniaSprite;
        public int Population = 5;
        // public int Credits = 100;
        public int TechPoints = 10; // 10 for pre warp and playable get 90 more to be tech level early at 100; 
        public TechLevel TechLevel = TechLevel.EARLY; // all cis have tech points and the tech level enum value sets a level threshold
        public bool Playable;
        public bool PlayedByAI = true;
        public CivEnum LocalPlayerCivEnum;
        public bool HasWarp;
        public string Decription = "We are the Borg";
        public List<StarSysController> StarSysWeOwn;
        //public List<CivController> CivControllersWeKnow;
        //public List<CivEnum> CivEnumsWeKnow;
        //public float TaxRate; // universal or variable by civ/system??
        //public float GrowthRate; // universal or variable by civ/system??
        public float IntelPoints;
        private object SystemsOwned;

        /// <summary>
        /// Get power efficiency multiplier based on tech level
        /// Uses TechManager for centralized tech bonuses
        /// </summary>
        public float GetPowerTechMultiplier()
        {
            if (TechManager.Instance != null)
            {
                return TechManager.Instance.GetPowerEfficiencyMultiplier(TechLevel);
            }

            // Fallback if TechManager not available
            switch (TechLevel)
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
                TechManager.Instance.AddResearchPoints(this, points);
            }
            else
            {
                // Fallback if TechManager not available
                TechLevel oldLevel = TechLevel;
                TechPoints += points;

                // Simple threshold check
                if (TechPoints >= 1000 && oldLevel != TechLevel.SUPREME)
                    TechLevel = TechLevel.SUPREME;
                else if (TechPoints >= 600 && oldLevel != TechLevel.ADVANCED)
                    TechLevel = TechLevel.ADVANCED;
                else if (TechPoints >= 300 && oldLevel != TechLevel.DEVELOPED)
                    TechLevel = TechLevel.DEVELOPED;
                else if (TechPoints >= 100 && oldLevel != TechLevel.EARLY)
                    TechLevel = TechLevel.EARLY;
            }
        }

        /// <summary>
        /// Get progress toward next tech level (0-1)
        /// </summary>
        public float GetTechProgressToNextLevel()
        {
            if (TechManager.Instance != null)
            {
                return TechManager.Instance.GetProgressToNextLevel(TechPoints, TechLevel);
            }

            // Fallback calculation
            int currentThreshold = 0;
            int nextThreshold = 300;

            switch (TechLevel)
            {
                case TechLevel.EARLY:
                    currentThreshold = 0;
                    nextThreshold = 300;
                    break;
                case TechLevel.DEVELOPED:
                    currentThreshold = 300;
                    nextThreshold = 600;
                    break;
                case TechLevel.ADVANCED:
                    currentThreshold = 600;
                    nextThreshold = 1000;
                    break;
                case TechLevel.SUPREME:
                    return 1f; // Max level
            }

            return Mathf.Clamp01((float)(TechPoints - currentThreshold) / (nextThreshold - currentThreshold));
        }
    }
}

