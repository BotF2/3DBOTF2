using BOTF3D.Core;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



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
    public int Dilitium;
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
}
