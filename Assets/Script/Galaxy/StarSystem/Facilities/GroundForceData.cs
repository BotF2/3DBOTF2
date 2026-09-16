using System.Collections.Generic;
using BOTF3D.Core;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



[System.Serializable]
public class GroundForceData
{
    // Shared by StarSysManager (cap sizing) and PopulationManager (per-turn conversion) so the
    // two stay in lockstep: every this-many population units supports one fielded ground force unit.
    public const int PopulationPerUnit = 2;

    // Power upkeep (System Invasion Phase 1 follow-up, 2026-09): unlike other facility types,
    // ground forces have no per-civ SO to author these from, so the two rates live here as flat
    // constants - first-pass numbers pending the phase 2.5 balance pass (Docs/Design/
    // SystemInvasion_Phase1_Design.md). PeacetimePowerLoadPerUnit is always drawn just for having
    // troops on the roster (StarSysMenuUIController.UpdateSystemPowerBalance); the higher
    // CombatPowerLoadPerUnit applies once OnCombatFooting is true, switched on by
    // StarSysManager.ReallocatePowerForCombat at Phase A entry and back off by
    // CombatController.EndCombat.
    public const int PeacetimePowerLoadPerUnit = 1;
    public const int CombatPowerLoadPerUnit = 4;
    public bool OnCombatFooting;

    public CivEnum CivEnum;
    public TechLevel TechLevel;
    public StarSysFacilityType FacilitiesEnumType;
    public string Name;
    public int StartStarDate; //start to build in factory queue
    public int BuildDuration;// duration to build can be reduced by number and output of factories
    public Sprite GroundForceSprite;
    public string Description;
    private string v;
    public GameObject SysGameObject;

    public GroundForceData(string v)
    {
        this.v = v;
        this.Name = v;
    }

    /// <summary>Total power load for troopCount fielded units at the current footing (peacetime or combat).</summary>
    public int CurrentPowerLoad(int troopCount) =>
        troopCount * (OnCombatFooting ? CombatPowerLoadPerUnit : PeacetimePowerLoadPerUnit);

    // Phase B abstract attrition stats — derived from HvyCruiser so they scale with civ tech and
    // quality automatically, matching "comparable to the best combat ship" without a new SO asset.
    // The 0.25 maintenance fraction matches the 1:4 power-load ratio already in this class.
    private const float MaintenanceFraction = 0.25f;

    // Ground-force-specific per-civ power tier (2026-09-16 balance pass, Vulcan-too-strong
    // playtest feedback) - deliberately separate from ShipStatCalculator's CivFlavor table, which
    // encodes ship-combat *archetype* (tanky vs. burst vs. hit-and-run, calibrated to near-parity
    // for the FED/ROM/KLING/TERRAN band) rather than the flat strength ladder ground troops need.
    // Applied as a single scalar to both attack and HP together so a civ's ground troops are
    // proportionally harder-hitting AND tougher, never one without the other.
    //   FED/ROM/TERRAN: 1.00 baseline.
    //   KLING:          slightly above baseline (weapon + hull together).
    //   BORG/DOM:       a bit further above KLING - same relative ordering as their ship power tier.
    //   CARD:           slightly below baseline.
    //   Minors not listed here fall through to WarpMinorGroundFlavor/PreWarpMinorGroundFlavor below,
    //   selected by the civ's current CivData.HasWarp (passed in by the caller, since it can change
    //   mid-game as a minor researches warp) - warp-capable minors are cut hard (0.25x) so a small
    //   early-game fleet can conquer a system like Vulcan without an extreme force; pre-warp minors
    //   are only mildly reduced (0.75x) since they have no fleet of their own to fall back on.
    private static readonly Dictionary<CivEnum, float> GroundFlavor = new Dictionary<CivEnum, float>
    {
        { CivEnum.FED,    1.00f },
        { CivEnum.ROM,    1.00f },
        { CivEnum.TERRAN, 1.00f },
        { CivEnum.KLING,  1.05f },
        { CivEnum.CARD,   0.95f },
        { CivEnum.DOM,    1.10f },
        { CivEnum.BORG,   1.15f },
    };
    private const float WarpMinorGroundFlavor = 0.25f;
    private const float PreWarpMinorGroundFlavor = 0.75f;

    private static float GetGroundFlavor(CivEnum civ, bool hasWarp) =>
        GroundFlavor.TryGetValue(civ, out var f) ? f : (hasWarp ? WarpMinorGroundFlavor : PreWarpMinorGroundFlavor);

    public static float GetUnitAttackPower(CivEnum civ, TechLevel tech, int qualityScore, bool onCombatFooting, bool hasWarp)
    {
        var s = ShipStatCalculator.Calculate(ShipType.HvyCruiser, tech, civ, qualityScore);
        float full = (s.BeamDamage + s.TorpedoDamage) * GetGroundFlavor(civ, hasWarp);
        return onCombatFooting ? full : full * MaintenanceFraction;
    }

    public static float GetUnitMaxHP(CivEnum civ, TechLevel tech, int qualityScore, bool hasWarp)
    {
        var s = ShipStatCalculator.Calculate(ShipType.HvyCruiser, tech, civ, qualityScore);
        return (s.ShieldMaxHealth + s.HullMaxHealth) * GetGroundFlavor(civ, hasWarp);
    }
}
