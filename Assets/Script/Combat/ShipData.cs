using Assets.Core;
using UnityEngine;

public class ShipData
{
    public string ShipName;
    public CivEnum CivEnum;
    public TechLevel TechLevel;
    public ShipType ShipType;
    public Sprite ShipSprite;
    public float maxWarpFactor;
    public float currentWarpFactor;
    public int ShieldMaxHealth;
    public int HullMaxHealth;
    public int TorpedoDamage;
    public int BeamDamage;
    public int BuildDuration;
    public GameObject Mesh;

    public ShipData(string name)
    {
        ShipName = name;
    }
    public ShipData()
    {

    }
}
