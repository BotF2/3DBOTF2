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
    public const int PopulationPerUnit = 8;

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

    public static float GetUnitAttackPower(CivEnum civ, TechLevel tech, int qualityScore, bool onCombatFooting)
    {
        var s = ShipStatCalculator.Calculate(ShipType.HvyCruiser, tech, civ, qualityScore);
        float full = s.BeamDamage + s.TorpedoDamage;
        return onCombatFooting ? full : full * MaintenanceFraction;
    }

    public static float GetUnitMaxHP(CivEnum civ, TechLevel tech, int qualityScore)
    {
        var s = ShipStatCalculator.Calculate(ShipType.HvyCruiser, tech, civ, qualityScore);
        return s.ShieldMaxHealth + s.HullMaxHealth;
    }
}
