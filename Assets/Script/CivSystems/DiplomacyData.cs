using Assets.Core;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DiplomacyData
{
    public CivController CivMajor; // a major civ and the local player if present
    public CivController CivOther; // a mionr civ if not a major civ that is not the local player
    public DiplomacyStatusEnum DiplomacyEnumOfCivs = DiplomacyStatusEnum.Neutral; // friendly, allied, at war
    public int DiplomacyPointsOfCivs = 60; // neutral
    public FleetController currentFleetControllerA;
    public FleetController currentFleetControllerB;
    public StarSysController currentStarSysController;
}
