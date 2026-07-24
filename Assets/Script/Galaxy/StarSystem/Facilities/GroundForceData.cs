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
}
