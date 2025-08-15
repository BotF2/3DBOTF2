using System.Collections.Generic;
using UnityEngine;
using Assets.Core;
using System.Linq;
using UnityEngine.UI;

public class ShipData 
{
    public string ShipName;
    public CivEnum CivEnum;
    public int PlayerId; // network player ID, not used in single player
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
    public string ShipDescription;
    public GameObject TargetMeHere;
    public GameObject FireAtThis;

    public ShipData(string name)
    {
        ShipName = name;
    }
    public ShipData()
    {

    }
}
