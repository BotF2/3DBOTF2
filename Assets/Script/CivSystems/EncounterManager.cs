using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Core;

public enum EncounterType
{
    FirstContact,
    Diplomacy, // civ to civ and civs can be local player or AI
    Combat,  //? is this a subtype of Diplomacy as seen by Diplomacy
    FleetManagement, // thinking we can do this back in the fleetController without calling it in Encounters
    EnterSystem,
    UninhabitedSystem,
    StrangeGalacticObject,
}
/// <summary>
/// Encoutner decides how is side one and side two for diplomacy and combat.
/// Consider using for AI trade, sabotage, espionage, disinformation, as well as sending on to diplomacy or dealing with colonization, worm holes.....
/// </summary>
public class EncounterManager : MonoBehaviour
{
    public List<EncounterController> EncounterControllers = new List<EncounterController>(); // EncounterController is not MonoBehavior

    public static EncounterManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    public void ResolveEncounterWithOtherCivFleet(FleetController reportingPlayerFleet, FleetController otherFleet)
    { // already not one of our fleets
        StarSysController sysConEmpty = StarSysManager.Instance.InstantiatEmptyStarSysController();
        if (reportingPlayerFleet != null)
        {
            CivController civSideOne;
            CivController civSideTwo;
            FleetController sideOneFleetCon;
            FleetController sideTwoFleetCon;
            if (reportingPlayerFleet.FleetData.CivController.CivData.CivEnum < otherFleet.FleetData.CivController.CivData.CivEnum)
            { 
                civSideOne = reportingPlayerFleet.FleetData.CivController;
                sideOneFleetCon = reportingPlayerFleet;
                civSideTwo = otherFleet.FleetData.CivController;
                sideTwoFleetCon = otherFleet;
            }
            else 
            {
                civSideOne = otherFleet.FleetData.CivController;
                sideOneFleetCon = otherFleet;
                civSideTwo = reportingPlayerFleet.FleetData.CivController;
                sideTwoFleetCon = reportingPlayerFleet;
            }
            if (!DiplomacyManager.Instance.FoundADiplomacyController(civSideOne, civSideTwo))
            { 
                DiplomacyManager.Instance.FirstContactInitNewDiplomacyContoller(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, sysConEmpty);
                IntelligenceManager.Instance.InitializeNewIntelligenceController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, sysConEmpty);
                FirstContactFleetOnFleetEncounterController(reportingPlayerFleet, otherFleet);
                Destroy(sysConEmpty.gameObject); // we do not need the empty system controller anymore
            }
            else
            {
                DiplomacyManager.Instance.UpdateOurDiplomacyController(sideOneFleetCon, sideTwoFleetCon);
                NextFleetToFleetEncounter(sideOneFleetCon, sideTwoFleetCon); // Will we need this? Is it all done in Diplomacy and FleetControllers?
            }
        }
    }
    public void ResolveEncounterOtherCivSystem(FleetController reportingPlayerfleet, StarSysController otherCivSysCon)
    { // already not one of our systems
        FleetController fleetConEmpty = FleetManager.Instance.InstatiateEmptyFleetController();
        int firstUninhabited = (int)CivEnum.ZZUNINHABITED1; // all lower than this are inhabited (including Borg UniComplex and inhabitable Nebulas)

        if ((int)otherCivSysCon.StarSysData.CurrentOwnerCivEnum < firstUninhabited) // it is inhabited
        {
            if (reportingPlayerfleet != null) // it is a FleetController and not a StarSystem or other with collider                                                                                                                                                    leetController
            {
                CivController civSideOne;
                CivController civSideTwo;
                FleetController sideOneFleetCon;
                FleetController sideTwoFleetCon;
                if (reportingPlayerfleet.FleetData.CivController.CivData.CivEnum < otherCivSysCon.StarSysData.CurrentCivController.CivData.CivEnum)
                { // local player is side one
                    civSideOne = reportingPlayerfleet.FleetData.CivController;
                    sideOneFleetCon = reportingPlayerfleet;
                    civSideTwo = otherCivSysCon.StarSysData.CurrentCivController;
                    sideTwoFleetCon = fleetConEmpty; // we do not have the other fleet controller, so we use an empty one
                }
                else // other civ is side one
                {
                    civSideOne = otherCivSysCon.StarSysData.CurrentCivController;
                    sideOneFleetCon = fleetConEmpty; // we do not have the other fleet controller, so we use an empty one
                    civSideTwo = reportingPlayerfleet.FleetData.CivController;
                    sideTwoFleetCon = reportingPlayerfleet;
                }

                //have we met before?
                if (!DiplomacyManager.Instance.FoundADiplomacyController(civSideOne, civSideTwo))
                { // First Contact
                    //DiplomacyManager.Instance.FirstContactInitNewDiplomacyContoller(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, otherCivSysCon);
                    FirstContactFleetOnStarSysNewEncounnterController(reportingPlayerfleet, otherCivSysCon); // do we do something special with system entry here?
                    //IntelligenceManager.Instance.InitializeNewIntelligenceController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, otherCivSysCon);
                }
                else
                { // not first contact
                    DiplomacyManager.Instance.UpdateOurDiplomacyController(sideOneFleetCon, otherCivSysCon);
                    FeetToSysNotSameCivNotFirstEncounter(sideOneFleetCon, otherCivSysCon);
                    //IntelligenceManager.Instance.UpdateOurIntelController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, otherCivSysCon);
                }
            }
            otherCivSysCon.gameObject.SetActive(true);
        }
        else if ((int)otherCivSysCon.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
        {
            //React to Uninhabited system contact and Colonize option
            FeetsUninhabitedSysEncounter(reportingPlayerfleet, otherCivSysCon);

            foreach (ShipController shipController in reportingPlayerfleet.FleetData.GetShipList())
            {
                if (shipController.ShipData.ShipType == ShipType.Transport)
                {
                    // ToDo: Colonies Opption/ UI?
                }
            }
        }
    }
    public void ResolveClickSysstem(CivController localCiv, StarSysController sysCon)
    { // already not one of our fleets
        CivController civPartyOne;
        CivController civPartyTwo;

        if ((int)localCiv.CivData.CivEnum < (int)sysCon.StarSysData.CurrentCivController.CivData.CivEnum)
        { 
        civPartyOne = localCiv;
        civPartyTwo = sysCon.StarSysData.CurrentCivController;
        }
        else // other civ is side one
        {
            civPartyOne = sysCon.StarSysData.CurrentCivController;
            civPartyTwo = localCiv;
        }
        //have we met before?
        if (DiplomacyManager.Instance.FoundADiplomacyController(civPartyOne, civPartyTwo))
        {   // not First Contact, just by clicking on the system
            DiplomacyManager.Instance.OpenDiplomacyUI(civPartyOne, civPartyTwo, sysCon.StarSysData.ShipsList);
            //DiplomacyManager.Instance.UpdateOurDiplomacyController(civPartyOne, civPartyTwo);
        }
        else
        {
            // no first contact just on clicking on the system
            // maybe some data if you are high tech level?
        }
    }

    private void NextFleetToFleetEncounter(FleetController fleetA, FleetController fleetB)
    { // *** Will we need this?
        var encounterData = GetEncounterData(fleetA, fleetB); // not mono behavior
        encounterData.EncounterType = EncounterType.FleetManagement;
        EncounterController encounterController = new EncounterController(encounterData); // not mono behavior
        encounterController.EncounterData.isCompleted = false;
        //encounterController.ResolveFleetEncounter();
        EncounterControllers.Add(encounterController);
    }
    private void FirstContactFleetOnFleetEncounterController(FleetController fleetA, FleetController fleetB)
    { // *** do we need to save this data???
        var encounterData = GetEncounterData(fleetA, fleetB); // not mono behavior
        encounterData.EncounterType = EncounterType.FirstContact;
        EncounterController encounterController = new EncounterController(encounterData);
        encounterController.EncounterData.isCompleted = false;
        //encounterController.ResolveFleetEncounter();
        EncounterControllers.Add(encounterController);
    }
    private void FirstContactFleetOnFleetEncounterController(CivController localCiv, StarSysController sysCon)
    { //ToDo: if we know the system's owner and we click on it what happens? 
        //var encounterData = GetEncounterData(fleetA, fleetB); // not mono behavior
        ////encounterData.EncounterType = EncounterType.FirstContact;
        //EncounterController encounterController = new EncounterController(encounterData);
        //encounterController.EncounterData.isCompleted = false;
        ////encounterController.ResolveFleetEncounter();
        //EncounterControllers.Add(encounterController);
    }
    private void FirstContactFleetOnStarSysNewEncounnterController(FleetController fleetCon, StarSysController starSysCon)
    {
        var encounterData = GetEncounterData(fleetCon, starSysCon); // not mono behavior
        encounterData.EncounterType = EncounterType.FirstContact;
        encounterData.isCompleted = false;
        EncounterController encounterController = new EncounterController(encounterData);
        encounterController.EncounterData.isCompleted = false;
        if (starSysCon.StarSysData.SystemType >= GalaxyObjectType.BlackHole) // resolve a non diplomatic encounter
            encounterController.ResolveFleetToStrangGalacticEncounter(encounterController);
        EncounterControllers.Add(encounterController);
    }

    public void FeetToSysNotSameCivNotFirstEncounter(FleetController fleetA, StarSysController sysCon)
    {
        var encounterData = GetEncounterData(fleetA, sysCon); // not mono behavior
        encounterData.EncounterType = EncounterType.Diplomacy;
        EncounterController encounterController = new EncounterController(encounterData);
        encounterController.EncounterData.isCompleted = false;
        EncounterControllers.Add(encounterController);
    }
    public void FeetsUninhabitedSysEncounter(FleetController fleetA, StarSysController uninhabitedSysCon)
    {
        var encounterData = GetEncounterData(fleetA, uninhabitedSysCon); // not mono behavior
        encounterData.EncounterType = EncounterType.UninhabitedSystem;
        EncounterController encounterController = new EncounterController(encounterData);
        encounterController.EncounterData.isCompleted = false;
        encounterController.ResolveUninhabitedSystem(fleetA.FleetData.CivController, uninhabitedSysCon);
        EncounterControllers.Add(encounterController);

        // ToDo work out claming system in HabitableSysUIController!!
    }
    private EncounterData GetEncounterData(FleetController fleetConA, FleetController fleetConB)
    {
        EncounterData encounterData = new EncounterData();
        encounterData.FleetControllerCivOne = fleetConA;
        encounterData.CivOne = fleetConA.FleetData.CivController;
        encounterData.FleetContollerCivTwo = fleetConB;
        encounterData.CivTwo = fleetConB.FleetData.CivController;
        return encounterData;
    }
    private EncounterData GetEncounterData(FleetController fleetConA, StarSysController starSysCon)
    {
        EncounterData encounterData = new EncounterData();
        encounterData.FleetControllerCivOne = fleetConA;
        encounterData.CivOne = fleetConA.FleetData.CivController;
        encounterData.StarSysController = starSysCon;
        encounterData.CivTwo = starSysCon.StarSysData.CurrentCivController;
        return encounterData;
    }

    public EncounterType GetEncounterType(EncounterType encounter)
    {
        EncounterType encounterType = EncounterType.Diplomacy;


        switch (encounterType)
        {
            case EncounterType.FirstContact:
                break;
            case EncounterType.Diplomacy: // this encoutner sends to DiplomacyManager to decide on combat or other diplomacy.
                break;
            case EncounterType.Combat: // this encoutner sends to CombatManager to decide on combat or other combat tasks.
                break;
            case EncounterType.FleetManagement: // this encoutner sends to FleetManager to decide on redistribution of ships or other fleet management tasks.
                break;
            case EncounterType.EnterSystem:
                break;
            case EncounterType.UninhabitedSystem:
                break;
            default:
                break;
        }
        return encounterType;
    }
}
