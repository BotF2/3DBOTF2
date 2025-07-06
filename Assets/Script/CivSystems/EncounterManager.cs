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
    public void ResolveEncounterWithOtherCiv(FleetController localPlayerFleet, FleetController otherFleet)
    { // already not one of our fleets
        StarSysController sysConEmpty = StarSysManager.Instance.InstantiatEmptyStarSysController();
        if (localPlayerFleet != null)
        {
            var civLocalPlayer = localPlayerFleet.FleetData.CivController;
            var civOther = otherFleet.FleetData.CivController;
            //have we met before?
            if (!DiplomacyManager.Instance.FoundADiplomacyController(civLocalPlayer, civOther))
            {   // First Contact
                DiplomacyManager.Instance.FirstContactGetNewDiplomacyContoller(civLocalPlayer,localPlayerFleet, civOther, otherFleet, sysConEmpty);
                FirstContactFleetOnFleetEncounterController(localPlayerFleet, otherFleet);
                // will we need this? Will AI need to remember this encounter outside of diplomacy?
            }
            else // Not First Contact and we do fleets of same civ management back in FleetController
            {
                DiplomacyManager.Instance.UpdateOurDiplomacyController(localPlayerFleet, otherFleet, sysConEmpty);// placeHolderStarSysCon);
                NextFleetToFleetEncounter(localPlayerFleet, otherFleet); // Will we need this? Is it all done in Diplomacy and FleetControllers?
            }
        }
    }
    public void ResolveEncounterWithOtherCiv(StarSysController localPlayerSysCon, FleetController otherFleet)
    { // already not one of our fleets
        FleetController fleetConEmpty = FleetManager.Instance.InstantiatEmptyFleetController();
        if (localPlayerSysCon != null)
        {
            var civLocalPlayer = localPlayerSysCon.StarSysData.CurrentCivController;
            var civOther = otherFleet.FleetData.CivController;
            //have we met before?
            if (!DiplomacyManager.Instance.FoundADiplomacyController(civLocalPlayer, civOther))
            {   // First Contact
                DiplomacyManager.Instance.FirstContactGetNewDiplomacyContoller(civLocalPlayer, fleetConEmpty, civOther, otherFleet, localPlayerSysCon);
                FirstContactFleetOnStarSysNewEncounnterController(fleetConEmpty, localPlayerSysCon);
                // will we need this? Will AI need to remember this encounter outside of diplomacy?
            }
            else // Not First Contact and we do fleets of same civ management back in FleetController
            {
                DiplomacyManager.Instance.UpdateOurDiplomacyController(fleetConEmpty, otherFleet, localPlayerSysCon);// placeHolderStarSysCon);
                //NextFleetToFleetEncounter(localPlayerSysCon, otherFleet); // Will we need this? Is it all done in Diplomacy and FleetControllers?
                //ToDo: do we need to resolve the fleet to sys encounter here?
            }
        }
    }
    public void ResolveClickSysstem(CivController localCiv, StarSysController sysCon)
    { // already not one of our fleets
        //FleetController placeHolderFleetCon = Instantiate(fleetPrefab, Vector3.zero, Quaternion.identity);
        //placeHolderFleetCon.FleetData = null; // this is a placeholder fleet controller
        var civPartyOne = localCiv;
        var civPartyTwo = sysCon.StarSysData.CurrentCivController;
        //have we met before?
        if (DiplomacyManager.Instance.FoundADiplomacyController(civPartyOne, civPartyTwo))
        {   // not First Contact, just by clicking on the system
            DiplomacyManager.Instance.OpenDiplomacyUI(civPartyOne, sysCon.StarSysData.CurrentCivController);
            //DiplomacyManager.Instance.UpdateOurDiplomacyController(civPartyOne, civPartyTwo);
        }
    }
    public void ResolveEncounter(FleetController localPlayerfleet, StarSysController otherCivSysCon)
    { // already not one of our systems
        FleetController fleetConB = FleetManager.Instance.InstatiateEmptyFleetController();
        int firstUninhabited = (int)CivEnum.ZZUNINHABITED1; // all lower than this are inhabited (including Borg UniComplex and inhabitable Nebulas)
        //FleetController placeHolderFleetCon = Instantiate(fleetPrefab, Vector3.zero, Quaternion.identity);
        //placeHolderFleetCon.FleetData = null; // this is a placeholder fleet controller
        if ((int)otherCivSysCon.StarSysData.CurrentOwnerCivEnum < firstUninhabited) // it is inhabited
        {

            if (localPlayerfleet != null) // it is a FleetController and not a StarSystem or other with collider                                                                                                                                                    leetController
            {
                var civLocalPlayer = localPlayerfleet.FleetData.CivController;
                var civOther = otherCivSysCon.StarSysData.CurrentCivController;
                //have we met before?
                if (!DiplomacyManager.Instance.FoundADiplomacyController(otherCivSysCon.StarSysData.CurrentCivController, localPlayerfleet.FleetData.CivController))
                { // First Contact
                    DiplomacyManager.Instance.FirstContactGetNewDiplomacyContoller(civLocalPlayer, localPlayerfleet, civOther,fleetConB, otherCivSysCon);
                    FirstContactFleetOnStarSysNewEncounnterController(localPlayerfleet, otherCivSysCon); // do we do something specila with system entry here?
                }
                else
                { // not first contact
                    DiplomacyManager.Instance.UpdateOurDiplomacyController(localPlayerfleet, fleetConB, otherCivSysCon);
                    FeetToSysNotSameCivNotFirstEncounter(localPlayerfleet, otherCivSysCon);
                }
            }
            otherCivSysCon.gameObject.SetActive(true);
        }
        else if ((int)otherCivSysCon.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
        {
            //React to Uninhabited system contact and Colonize option
            FeetsUninhabitedSysEncounter(localPlayerfleet, otherCivSysCon);



            foreach (ShipController shipController in localPlayerfleet.FleetData.GetShipList())
            {
                if (shipController.ShipData.ShipType == ShipType.Transport)
                {
                    // ToDo: Colonies Opption/ UI?
                }
            }
        }
    }
    private void NextFleetToFleetEncounter(FleetController fleetA, FleetController fleetB)
    { // Will we need this?
        var encounterData = GetEncounterData(fleetA, fleetB); // not mono behavior
        encounterData.EncounterType = EncounterType.FleetManagement;
        EncounterController encounterController = new EncounterController(encounterData); // not mono behavior
        encounterController.EncounterData.isCompleted = false;
        //encounterController.ResolveFleetEncounter();
        EncounterControllers.Add(encounterController);
    }
    private void FirstContactFleetOnFleetEncounterController(FleetController fleetA, FleetController fleetB)
    {
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
