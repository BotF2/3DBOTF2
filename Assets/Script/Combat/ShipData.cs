using System.Collections.Generic;
using UnityEngine;
using Assets.Core;
using System.Linq;
using UnityEngine.UI;

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
    public string ShipDescription;
    //public int Cost;
    //public int CrewCapacity;            
    //public float FuelCapacity;                      
    //public float CurrentFuel;
    //public bool IsPowered; 

    public ShipData(string name)
    {
        ShipName = name;
    }
    public ShipData()
    {

    }
}
