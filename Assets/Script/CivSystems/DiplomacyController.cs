using Assets.Core;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

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
public class DiplomacyController //not : MonoBehaviour
{
    private DiplomacyData diplomacyData; // holds civOne and two and diplomacy enum
    public DiplomacyData DiplomacyData { get { return diplomacyData; } set { diplomacyData = value; } }
    private static string declareWar = "The A declares war on the B.";
    private static string requestSomething = "The A request X from the B.";
    private static string demandSomething = "The A demand X from the B.";
    private static string offerSomething = "The A offers the B X.";
    private static string demandStopInterferance = "The A demand that the B stop X.";

    private List<string> diplomaticTransmissions = new List<string> { declareWar, requestSomething, demandSomething, offerSomething, demandStopInterferance };
    public List<string> DiplomaticTransmissions { get { return diplomaticTransmissions; } set { diplomaticTransmissions = value; } }
    public List<DiplomaticEventEnum> DiplomaticEvents = new List<DiplomaticEventEnum>
    { DiplomaticEventEnum.War, DiplomaticEventEnum.DiscoveredSabotage, DiplomaticEventEnum.DiscoveredDisinformation, DiplomaticEventEnum.DiscoveredIntellectualTheft,
        DiplomaticEventEnum.Trade, DiplomaticEventEnum.ShareTech, DiplomaticEventEnum.GiveAid};
    public GameObject DiplomacyUIGameObject; //The instantiated UI for this civ pair. a prefab clone, not a class but a game object
                                             // instantiated by DiplomacyManager from a prefab and added to DiplomacyController
    public DiplomacyController(DiplomacyData diplomacyData)
    {
        DiplomacyData = diplomacyData;
    }
    public void DoAIDiplomacy()
    {
        if (GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivSideOne) || GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivSideTwo))
        {
            DiplomacyUIGameObject.SetActive(true);
            //ToDo: AI civ diplomacy actions for on or both civs that are AI.
        }
    }
    public void UpdateDiplomacyControllerData(DiplomacyData diplomacyData)
    {
        this.DiplomacyData = diplomacyData;
        ChangedDiplomacyStatus(this.DiplomacyData.DiplomacyPointsOfCivs);
    }
    public void AddDiplomaticPoints(int points)
    {
        this.DiplomacyData.DiplomacyPointsOfCivs += points;
        ChangedDiplomacyStatus(this.DiplomacyData.DiplomacyPointsOfCivs);
    }
    public void SubtractDiplomaticPoints(int points)
    {
        this.DiplomacyData.DiplomacyPointsOfCivs -= points;
        ChangedDiplomacyStatus(this.DiplomacyData.DiplomacyPointsOfCivs);
    }
    private void ChangedDiplomacyStatus(int currentStatusPoints)
    {
        if (currentStatusPoints < -20)
        {
            currentStatusPoints = -20;
        }

        if (currentStatusPoints >= (int)DiplomacyStatusEnum.Neutral && currentStatusPoints < (int)DiplomacyStatusEnum.Friendly)
        {
            this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Neutral;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Friendly && currentStatusPoints < (int)DiplomacyStatusEnum.Allied)
        {
            this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Friendly;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Allied && currentStatusPoints < (int)DiplomacyStatusEnum.Membership)
        {
            this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Allied;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Membership && ((int)this.DiplomacyData.CivSideOne > 6 || (int)this.DiplomacyData.CivSideTwo > 6))
        {
            // only minors AI civ can become member of a playable major race
            this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Membership;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.UnFriendly && currentStatusPoints < (int)DiplomacyStatusEnum.Neutral)
        {
            this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.UnFriendly;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Hostile && currentStatusPoints < (int)DiplomacyStatusEnum.UnFriendly)
        {
            this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Hostile;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.ColdWar && currentStatusPoints < (int)DiplomacyStatusEnum.Hostile)
        {
            this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.ColdWar;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.War)
        {
            this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.War;
        }
    }
    public void ProposeTrade(DiplomacyController diplomacyData)
    {
        // ToDo: 
    }
    public void Engagement(DiplomacyController dilomacyCon)
    {
        // ToDo: 
    }
    public void ProposeTech(DiplomacyController dilomacyCon)
    {
        // ToDo:
    }
    public void SendAid(DiplomacyController diplomacyController)
    {
        //ToDo:
    }
    public void OfferAlliance(DiplomacyController diplomacyController)
    {
        //ToDo:
    }
    public void GatherIntel(DiplomacyController diplomacyController)
    {
        //ToDo:
    }
    public void Theft(DiplomacyController diplomacyController)
    {
        //ToDo:
    }
    public void Disinformation(DiplomacyController diplomacyController)
    {
        //ToDo:
    }
    public void Sabatoge(DiplomacyController diplomacyController)
    {
        //ToDo:
    }
    public void Combat(DiplomacyController diplomacyController)
    {
        if (diplomacyController.DiplomacyData.CombatIntiated != true)
        {
            diplomacyController.DiplomacyData.CombatIntiated = true;

            GalaxyMenuUIController.Instance.CloseMenu(Menu.DiplomacyMenu);
            SceneController.Instance.LoadCombatScene(diplomacyController);
        }
        //*******load combat menu for local player and do AI civs
       
    }

    internal void ResolveFleetToStrangGalacticEncounter(DiplomacyController diplomacyController)
    {
        // ToDo: Resolve the encounter with the strange galactic object
    }

    internal void ResolveUninhabitedSystem(CivController realCivController, StarSysController uninhabitedSysCon)
    {
        // UI for uninhabited system management
        if (GameController.Instance.AreWeLocalPlayer(realCivController.CivData.CivEnum))
            uninhabitedSysCon.DoHabitalbeSystemUI(realCivController);
        else
        {
            // do AI uninhabited system management
        }
    }
}
