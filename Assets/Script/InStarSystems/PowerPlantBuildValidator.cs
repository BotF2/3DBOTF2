using BOTF3D.GamePlay;


namespace BOTF3D.Core
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

            // ✅ Check Dilithium capacity
            if (sysData.CurrentPowerPlantCount >= sysData.DilithiumCapacity)
            {
                reason = $"Dilithium reserves exhausted. Maximum capacity: {sysData.DilithiumCapacity}";
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
            return $"Power Plants: {sysData.CurrentPowerPlantCount}/{sysData.DilithiumCapacity}";
        }

        /// <summary>
        /// Get power output info
        /// </summary>
        public static string GetPowerOutputInfo(StarSysData sysData, CivData civData)
        {
            float basePower = 10f;
            float techMultiplier = civData.GetPowerTechMultiplier();
            float totalPower = sysData.CalculateTotalPower(techMultiplier);

            return $"Power Output: {totalPower:F1} ({sysData.CurrentPowerPlantCount} × {basePower} × {techMultiplier:F1}x tech)";
        }
    }
}