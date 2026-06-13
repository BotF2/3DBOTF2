
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;




namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Validates power plant construction based on Dilithium capacity
    /// </summary>
    public static class PowerPlantBuildValidator
    {
        /// <summary>
        /// Check if a power plant can be built in this system
        /// </summary>
        public static bool CanBuildPowerPlant(StarSysController systemController, out string reason)
        {
            if (systemController == null || systemController.StarSysData == null)
            {
                reason = "Invalid system";
                return false;
            }

            var sysData = systemController.StarSysData;
            var civData = systemController.StarSysData.CurrentCivController?.CivData;
            TechLevel tech = civData?.CurrentTechLevel ?? TechLevel.EARLY;

            if (!sysData.CanBuildPowerPlant(tech))
            {
                int cost = TechManager.Instance != null ? TechManager.Instance.GetDilithiumCostPerPlant(tech) : 45;
                int avail = sysData.GetDilithiumAvailable(tech);
                reason = $"Insufficient dilithium. Need {cost} units, have {avail} available (of {sysData.DilithiumUnits} total).";
                return false;
            }

            // ✅ You can add other constraints here:
            // - Resource costs
            // - Build time
            // - Tech requirements

            reason = "Build allowed";
            return true;
        }

        /// <summary>
        /// Get formatted info about power plant capacity
        /// </summary>
        public static string GetCapacityInfo(StarSysData sysData)
        {
            TechLevel tech = sysData.CurrentCivController?.CivData?.CurrentTechLevel ?? TechLevel.EARLY;
            int avail = sysData.GetDilithiumAvailable(tech);
            return $"Power Plants: {sysData.PowerPlants?.Count ?? 0} | Dilithium: {avail}/{sysData.DilithiumUnits} free";
        }

        public static string GetPowerOutputInfo(StarSysData sysData, CivData civData)
        {
            TechLevel tech = civData?.CurrentTechLevel ?? TechLevel.EARLY;
            int outPerPlant = TechManager.Instance != null ? TechManager.Instance.GetPowerOutputPerPlant(tech) : 20;
            float total = sysData.CalculateTotalPower(tech);
            return $"Power Output: {total:F0} ({sysData.PowerPlants?.Count ?? 0} × {outPerPlant})";
        }
    }
}