using BOTF3D.Core;
using UnityEngine;

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

            // Copy movement stats
            data.maxWarpFactor = shipSO.maxWarpFactor;
            data.currentWarpFactor = 0f;

            // Copy health stats (initialize to max)
            data.ShieldHealth = shipSO.ShieldMaxHealth;
            data.HullHealth = shipSO.HullMaxHealth;

            // Copy combat stats
            data.TorpedoDamage = shipSO.TorpedoDamage;
            data.BeamDamage = shipSO.BeamDamage;

            // Copy build stats
            data.BuildDuration = shipSO.BuildDuration;

            Debug.Log($"ShipDataInitializer: Initialized '{data.ShipName}' from ShipSO");
        }

        /// <summary>
        /// Reset ship health to maximum
        /// </summary>
        public void ResetShipHealth(ShipController shipController)
        {
            if (shipController?.ShipData?.ShipSO != null)
            {
                shipController.ShipData.ShieldHealth = shipController.ShipData.ShipSO.ShieldMaxHealth;
                shipController.ShipData.HullHealth = shipController.ShipData.ShipSO.HullMaxHealth;
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
