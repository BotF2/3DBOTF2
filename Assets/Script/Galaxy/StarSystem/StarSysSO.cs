using BOTF3D.Core;
using UnityEngine;
using UnityEngine.Serialization;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;
using BOTF3D.Galaxy;



[CreateAssetMenu(menuName = "Galaxy/StarSysSO")]
public class StarSysSO : ScriptableObject
{
    public int StarSysInt;
    public Vector3 Position;
    public string SysName;
    public CivEnum FirstOwner;
    public CivEnum CurrentOwner;
    public GalaxyObjectType StarType;
    public Sprite StarSprit;
    [FormerlySerializedAs("Dilitium")]
    public int Dilithium;
    [Tooltip("Authored starting Antimatter reserve for homeworlds - Power Plants draw from this " +
        "each turn, Factories replenish it. Colonies don't use this field; they get a small " +
        "uniform seed as part of their transport's Colony Kit instead - see " +
        "StarSysController.ColonizeTimerCoroutine.")]
    public int Antimatter;
    public int PowerStations;
    public int Factories;
    public int ResearchCenters;
    public int Shipyards;
    public int ShieldGenerators;
    public int OrbitalBatteries;
    public string Description;
    private string v;
    public Sprite powerPlantSprite;
    public Sprite factorySprite;
    public Sprite shipyardSprite;
    public Sprite shieldSprite;
    public Sprite orbitalSprite;
    public Sprite researchCenterSprite;
    public bool IsHomeworld;
    public bool IsHabitable;
    public bool IsTerraformable;

#if UNITY_EDITOR
    // Instant feedback while hand-editing a position in the Inspector - the full
    // neighbor-distance sweep across all systems lives in the
    // "Tools/Validate Star System Positions" batch check instead, since that's too
    // expensive to run on every field edit.
    private void OnValidate()
    {
        if (!GalaxyPositionBounds.IsWithinBounds(Position))
        {
            Debug.LogWarning(
                $"{name}: Position {Position} is outside the galaxy image bounds " +
                $"(x:[{GalaxyPositionBounds.XMin},{GalaxyPositionBounds.XMax}], " +
                $"z:[{GalaxyPositionBounds.ZMin},{GalaxyPositionBounds.ZMax}]) and may render off the map. " +
                "Run Tools > Validate Star System Positions for full details.", this);
        }
    }
#endif
}
