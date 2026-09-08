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
        /// Initialize ShipData from ShipSO. civOverride lets a spawner assign the actual owning
        /// civ at creation time instead of the SO's authored default (used for orbital batteries,
        /// whose ShipSO is civ-agnostic — the real owner is whichever civ holds the star system;
        /// see ShipManager.CreateOrbitalBatteryForSystem) — stats (quality/civ-flavor multipliers)
        /// are computed against the override, not shipSO.CivEnum, so a Klingon-built battery gets
        /// Klingon-flavored stats rather than the SO's placeholder civ's.
        /// </summary>
        public void InitializeShipData(ShipController shipController, ShipSO shipSO, CivEnum? civOverride = null)
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
            CivEnum effectiveCiv = civOverride ?? shipSO.CivEnum;

            // Store reference to ShipSO
            data.ShipSO = shipSO;

            // Identity and visual
            data.ShipName = shipSO.ShipName;
            data.CivEnum = effectiveCiv;
            data.TechLevel = shipSO.TechLevel;
            data.BuiltAtTechLevel = shipSO.TechLevel;
            data.ShipType = shipSO.ShipType;
            data.ShipDescription = shipSO.ShipDescription;
            if (shipSO.shipSprite != null)
                data.ShipSprite = shipSO.shipSprite;

            // Calculate combat stats from type, tier, civ doctrine, and flavor
            CivData effectiveCivData = CivManager.Instance?.GetCivDataByCivEnum(effectiveCiv);
            int quality = effectiveCivData?.QualityScore ?? 5;
            ShipStats stats = ShipStatCalculator.Calculate(shipSO.ShipType, shipSO.TechLevel, effectiveCiv, quality);

            // Phase II tech tree (TechTree_Phase2_Design.md §8 II.3): Tactical/Ordnance "+stat%"
            // techs and their civ-specific Branch F equivalents (Ionized/Reinforced Duranium Hulls,
            // Imperial Phaser Overcharge, Polaron Beam Enhancement, etc.) all accumulate into
            // CivData.Effects rather than touching ShipStatCalculator itself - applied here, the one
            // place every ship's stats actually get set, so every spawner/rebuild path picks them up
            // automatically. Structural Integrity Fields (StationHPMultiplier) additionally stacks
            // onto HullMultiplier for OrbitalBattery hulls only - it's the one ShipType that never
            // warps in or moves (CLAUDE.md), so "station HP" unambiguously means this ShipType.
            TechEffects fx = effectiveCivData?.Effects;
            float hullMult   = fx != null ? fx.HullMultiplier : 1f;
            float shieldMult = fx != null ? fx.ShieldMultiplier : 1f;
            float weaponMult = fx != null ? fx.WeaponDamageMultiplier : 1f;
            if (fx != null && shipSO.ShipType == ShipType.OrbitalBattery)
                hullMult *= fx.StationHPMultiplier;

            data.ShieldMaxHealth  = Mathf.Max(1, Mathf.RoundToInt(stats.ShieldMaxHealth * shieldMult));
            data.HullMaxHealth    = Mathf.Max(1, Mathf.RoundToInt(stats.HullMaxHealth   * hullMult));
            data.ShieldHealth     = data.ShieldMaxHealth;
            data.HullHealth       = data.HullMaxHealth;
            data.BeamDamage       = stats.BeamDamage    == 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(stats.BeamDamage    * weaponMult));
            data.TorpedoDamage    = stats.TorpedoDamage == 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(stats.TorpedoDamage * weaponMult));
            data.maxWarpFactor    = stats.MaxWarpFactor;
            data.currentWarpFactor = 0f;
            data.BuildDuration    = stats.BuildDuration;
            data.DilithiumCost    = stats.DilithiumCost;
            data.CargoCapacity    = stats.CargoCapacity;

            Debug.Log($"ShipDataInitializer: Initialized {data.CivEnum} '{data.ShipName}' ({data.ShipType} T{(int)data.TechLevel}) — " +
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
            destination.CargoCapacity = source.CargoCapacity;
            destination.LoadedPopulation = source.LoadedPopulation;
            destination.DesignatedForTerraform = source.DesignatedForTerraform;
            destination.ShipDescription = source.ShipDescription;

            Debug.Log($"ShipDataInitializer: Copied data from '{source.ShipName}'");
        }
    }
}
