using Assets.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "ShipSO", menuName = "ShipSO", order = 1)]
public class ShipSO : ScriptableObject
{
    public string ShipName;
    public CivEnum CivEnum;
    public TechLevel TechLevel;
    public ShipType ShipType;
    public Sprite shipSprite;
    public Color shipColor;
    public float maxWarpFactor;
    public int ShieldMaxHealth;
    public int HullMaxHealth;
    public int TorpedoDamage;
    public int BeamDamage;
    public int BuildDuration;
    public GameObject Mesh;

}
