using Assets.Core;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DiplomacyData
{
    public CivController CivMajor; // a major civ and the local player if present
    public int PlayerId; // network player ID, not used in single player
    public CivController CivOther; // a minor civ, if any
    public int OtherPlayerId; // network player ID, not used in single player
    public DiplomacyStatusEnum DiplomacyEnumOfCivs = DiplomacyStatusEnum.Neutral; // friendly, allied, at war
    public int DiplomacyPointsOfCivs = 60; // neutral
    public FleetController FleetMajor; // the major civ's fleet
    public FleetController FleetOther; // the minor civ's fleet, if any 
    public StarSysController StarSysController; // a star system controller where the encounter took place  
}
