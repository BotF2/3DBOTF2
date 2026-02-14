using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;

[CreateAssetMenu(menuName = "Galaxy/ShipyardSO")]
public class ShipyardSO : ScriptableObject
{
    public int CivInt;
    public TechLevel TechLevel;
    public StarSysFacilityType FacilitiesEnumType;
    public string Name;
    public int StartStarDate; //start to build in factory queue
    public int BuildDuration;// duration to build can be reduced by number and output of factories
    public int PowerLoad;
    public Sprite ShipyardSprite;
    public string Description;
}
