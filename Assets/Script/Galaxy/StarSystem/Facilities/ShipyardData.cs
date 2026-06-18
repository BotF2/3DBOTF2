using BOTF3D.Core;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



[System.Serializable]
public class ShipyardData
{
    public CivEnum CivEnum;
    public TechLevel TechLevel;
    public StarSysFacilityType FacilitiesEnumType; // what type are we
    public string Name;
    public int StartStarDate; //start to build this factory in an existing factory queue
    public int BuildDuration;// duration to build can be reduced by number and output of factories
    public int PowerLoad;
    public Sprite ShipyardSprite;
    public string Description;
    private string v;
    public GameObject SysGameObject;
    public ShipyardData(string v)
    {
        this.v = v;
        this.Name = v;
    }
}
