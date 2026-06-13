namespace BOTF3D.Combat
{
using BOTF3D.Core;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



[CreateAssetMenu(fileName = "ShipSO", menuName = "ShipSO", order = 1)]
public class ShipSO : ScriptableObject
{
    [Header("3D Model")]
    public GameObject ShipFBX_ModelAsGOPrefab;  // ✅ Assign in Inspector
    public string ShipName;
    public CivEnum CivEnum;
    public TechLevel TechLevel;
    public ShipType ShipType;

    [Header("Tech Requirements")]
    [Tooltip("Minimum tech points required to unlock this ship (e.g., 0, 150, 350, 700)")]
    public int MinTechPointsRequired = 0;

    [Header("Dilithium")]
    [Tooltip("Dilithium units this ship holds. 0 = derive from ShipTypeProfiles (recommended).")]
    public int DilithiumCostOverride = 0;

    [Header("Ship Stats")]
    public Sprite shipSprite;
    public Color shipColor;
    public float maxWarpFactor;
    public int ShieldMaxHealth;
    public int HullMaxHealth;
    public int TorpedoDamage;
    public int BeamDamage;
    public int BuildDuration;
    public string ShipDescription;

    /// <summary>
    /// Get the tech tier suffix from ship name (_I, _II, _III, _IV)
    /// </summary>
    public int GetTechTier()
    {
        if (ShipName.Contains("_IV")) return 4;
        if (ShipName.Contains("_III")) return 3;
        if (ShipName.Contains("_II")) return 2;
        if (ShipName.Contains("_I")) return 1;
        return 1; // Default to tier 1
    }
}

}