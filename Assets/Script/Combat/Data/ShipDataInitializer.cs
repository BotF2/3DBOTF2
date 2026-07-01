using BOTF3D.Core;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Initializes ShipData from ShipSO (scriptable object).
    /// Handles all data copying and setup logic.
    /// </summary>
    public class ShipDataInitializer
    {
        /// <summary>
        /// Initialize ShipData from ShipSO
        /// </summary>
        public void InitializeShipData(ShipController shipController, ShipSO shipSO)
        {
            if (shipController == null || shipSO == null)
            {
                Debug.LogError("ShipDataInitializer: shipController or shipSO is null!");
                return;
            }

            // Create new ShipData if needed
            if (shipController.ShipData == null)
            {
                shipController.ShipData = new ShipData();
            }

            ShipData data = shipController.ShipData;

            // Store reference to ShipSO
            data.ShipSO = shipSO;

            // Identity and visual
            data.ShipName = shipSO.ShipName;
            data.CivEnum = shipSO.CivEnum;
            data.TechLevel = shipSO.TechLevel;
            data.ShipType = shipSO.ShipType;
            data.ShipDescription = shipSO.ShipDescription;
            if (shipSO.shipSprite != null)
                data.ShipSprite = shipSO.shipSprite;

            // Calculate combat stats from type, tier, civ doctrine, and flavor
            int quality = CivManager.Instance?.GetCivDataByCivEnum(shipSO.CivEnum)?.QualityScore ?? 5;
            ShipStats stats = ShipStatCalculator.Calculate(shipSO.ShipType, shipSO.TechLevel, shipSO.CivEnum, quality);

            data.ShieldMaxHealth  = stats.ShieldMaxHealth;
            data.HullMaxHealth    = stats.HullMaxHealth;
            data.ShieldHealth     = stats.ShieldMaxHealth;
            data.HullHealth       = stats.HullMaxHealth;
            data.BeamDamage       = stats.BeamDamage;
            data.TorpedoDamage    = stats.TorpedoDamage;
            data.maxWarpFactor    = stats.MaxWarpFactor;
            data.currentWarpFactor = 0f;
            data.BuildDuration    = stats.BuildDuration;
            data.DilithiumCost    = stats.DilithiumCost;

            Debug.Log($"ShipDataInitializer: Initialized '{data.ShipName}' — " +
                      $"Sh:{data.ShieldMaxHealth} Hu:{data.HullMaxHealth} " +
                      $"Be:{data.BeamDamage} To:{data.TorpedoDamage} " +
                      $"Wp:{data.maxWarpFactor:F1} Bd:{data.BuildDuration} Li2:{data.DilithiumCost} (Q{quality})");
        }

        /// <summary>
        /// Reset ship health to maximum
        /// </summary>
        public void ResetShipHealth(ShipController shipController)
        {
            if (shipController?.ShipData != null)
            {
                shipController.ShipData.ShieldHealth = shipController.ShipData.ShieldMaxHealth;
                shipController.ShipData.HullHealth   = shipController.ShipData.HullMaxHealth;
                Debug.Log($"ShipDataInitializer: Reset health for '{shipController.ShipData.ShipName}'");
            }
        }

        /// <summary>
        /// Validate ship data integrity
        /// </summary>
        public bool ValidateShipData(ShipController shipController)
        {
            if (shipController == null)
            {
                Debug.LogError("ShipDataInitializer: shipController is null!");
                return false;
            }

            if (shipController.ShipData == null)
            {
                Debug.LogError($"ShipDataInitializer: ShipData is null for '{shipController.name}'!");
                return false;
            }

            if (shipController.ShipData.ShipSO == null)
            {
                Debug.LogError($"ShipDataInitializer: ShipSO is null for '{shipController.ShipData.ShipName}'!");
                return false;
            }

            if (string.IsNullOrEmpty(shipController.ShipData.ShipName))
            {
                Debug.LogError($"ShipDataInitializer: ShipName is null or empty!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Copy data from one ship to another (useful for combat transitions)
        /// </summary>
        public void CopyShipData(ShipData source, ShipData destination)
        {
            if (source == null || destination == null)
            {
                Debug.LogError("ShipDataInitializer: Cannot copy null ShipData!");
                return;
            }

            destination.ShipSO = source.ShipSO;
            destination.ShipName = source.ShipName;
            destination.CivEnum = source.CivEnum;
            destination.TechLevel = source.TechLevel;
            destination.ShipType = source.ShipType;
            destination.ShipSprite = source.ShipSprite;
            destination.maxWarpFactor = source.maxWarpFactor;
            destination.currentWarpFactor = source.currentWarpFactor;
            destination.ShieldMaxHealth = source.ShieldMaxHealth;
            destination.HullMaxHealth   = source.HullMaxHealth;
            destination.ShieldHealth = source.ShieldHealth;
            destination.HullHealth = source.HullHealth;
            destination.TorpedoDamage = source.TorpedoDamage;
            destination.BeamDamage = source.BeamDamage;
            destination.BuildDuration = source.BuildDuration;
            destination.ShipDescription = source.ShipDescription;

            Debug.Log($"ShipDataInitializer: Copied data from '{source.ShipName}'");
        }
    }
}
