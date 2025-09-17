using Assets.Core;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DiplomacyData
{
    public CivEnum CivSideOne; // a major civ and the local player if present
    public int SideOneMultiplayerId; // network player ID, not used in single player
    public CivEnum CivSideTwo; // a minor civ, if any
    public int SideTwoMultiplayerId; // network player ID, not used in single player
    public DiplomacyStatusEnum DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Neutral; // the diplomacy status for this civ pair
    public int DiplomacyPointsOfCivs = 60; // neutral
    public bool CombatIntiated = false; // true if combat has been initiated between these civs

    public DiplomacyData(CivEnum civOne, CivEnum civTwo) //, StarSysController starSysController)
    {
        this.CivSideOne = civOne;
        this.CivSideTwo = civTwo; 
        // this.CurrentStarSysController = starSysController;
    }
}
