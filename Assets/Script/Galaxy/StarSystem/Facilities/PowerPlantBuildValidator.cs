
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

            // ✅ Check Dilithium capacity (slot limit)
            if (sysData.CurrentPowerPlantCount >= sysData.DilithiumCapacity)
            {
                reason = $"Dilithium reserves exhausted. Maximum capacity: {sysData.DilithiumCapacity}";
                return false;
            }

            // ✅ Check dilithium stockpile (each power plant costs 1)
            if (!sysData.HasDilithium(1))
            {
                reason = "Insufficient dilithium in stockpile.";
                return false;
            }

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