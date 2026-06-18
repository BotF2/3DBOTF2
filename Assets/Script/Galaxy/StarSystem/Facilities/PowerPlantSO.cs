using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



[CreateAssetMenu(menuName = "Galaxy/PowerPlantSO")]
/// <summary>
/// This is a type of galactic object that is a 'Facility' and populates fields in the PowerPlantData file 
/// for StarSysManager/StarSysController/'*"Data) 
/// </summary>
public class PowerPlantSO: ScriptableObject
{
    public int CivInt;
    public TechLevel TechLevel;
    public StarSysFacilityType FacilitiesEnumType;
    public string Name;
    public int StartStarDate; //start to build in factory queue
    public int BuildDuration;// duration to build can be reduced by number and output of factories
    public int PowerOutput;
    public Sprite PowerPlantSprite;
    public string Description;
}
