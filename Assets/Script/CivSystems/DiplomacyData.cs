using Assets.Core;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DiplomacyData
{
    public CivController CivSideOne; // a major civ and the local player if present
    public int SideOneMultiplayerId; // network player ID, not used in single player
    public CivController CivSideTwo; // a minor civ, if any
    public int SideTwoMultiplayerId; // network player ID, not used in single player
    public DiplomacyStatusEnum DiplomacyEnumOfCivs = DiplomacyStatusEnum.Neutral; // friendly, allied, at war
    public int DiplomacyPointsOfCivs = 60; // neutral
    public FleetController CurrentFleetOfSideOne; // the major civ's fleet
    public FleetController CurrentFleetOfSideTwo; // the minor civ's fleet, if any 
    public StarSysController CurrentStarSysController; // a star system controller where the encounter took place
    public DiplomacyData(CivController civOne, CivController civTwo, StarSysController starSysController)
    {
        this.CivSideOne = civOne;
        this.CivSideTwo = civTwo;
        this.CurrentStarSysController = starSysController;
    }
}
