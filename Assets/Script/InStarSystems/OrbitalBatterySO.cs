using BOTF3D.Core;
using UnityEngine;

[CreateAssetMenu(menuName = "Galaxy/OrbitalBatterySO")]
public class OrbitalBatterySO : ScriptableObject
{
    public int CivInt;
    public TechLevel TechLevel;
    public StarSysFacilityType FacilitiesEnumType;
    public string Name;
    public int StartStarDate; //start to build in factory queue
    public int BuildDuration;// duration to build can be reduced by number and output of factories
    public int PowerLoad;
    public Sprite OrbitalBatterySprite;
    public string Description;
}

