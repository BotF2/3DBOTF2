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

            // Copy basic properties
            data.ShipName = shipSO.ShipName;
            data.CivEnum = shipSO.CivEnum;
            data.TechLevel = shipSO.TechLevel;
            data.ShipType = shipSO.ShipType;
            data.ShipDescription = shipSO.ShipDescription;

            // Copy sprite
            if (shipSO.shipSprite != null)
            {
                data.ShipSprite = shipSO.shipSprite;
            }

            // Combat stats (warp, weapons, shields, hull, build duration, dilithium) are computed
            // later by ShipManager.ComputeShipStats(). Zero-initialize here so they're safe to read
            // before computation and clearly mark that the SO values are no longer authoritative.
            data.maxWarpFactor = 0f;
            data.currentWarpFactor = 0f;
            data.ShieldHealth  = 0;
            data.HullHealth    = 0;
            data.TorpedoDamage = 0;
            data.BeamDamage    = 0;
            data.BuildDuration = 0;
            data.DilithiumCost = ShipTypeProfiles.Get(shipSO.ShipType).DilithiumCost;

            Debug.Log($"ShipDataInitializer: Initialized '{data.ShipName}' from ShipSO");
        }

        /// <summary>
        /// Reset ship health to maximum
        /// </summary>
        /// <summary>
        /// Re-runs ComputeShipStats to restore full shield and hull from formula.
        /// CivData must be provided so the formula can reference base stats.
        /// </summary>
        public void ResetShipHealth(ShipController shipController, BOTF3D.Civilization.CivData civData = null)
        {
            if (shipController?.ShipData == null) return;

            if (civData != null && ShipManager.Instance != null)
            {
                ShipManager.Instance.ComputeShipStats(shipController.ShipData, civData);
                Debug.Log($"ShipDataInitializer: Reset health for '{shipController.ShipData.ShipName}'");
            }
            else
            {
                Debug.LogWarning($"ShipDataInitializer.ResetShipHealth: no civData provided for '{shipController.ShipData.ShipName}' — health unchanged.");
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
