using Assets.Core;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


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
    { DiplomaticEventEnum.War, DiplomaticEventEnum.DiscoveredSabatoge, DiplomaticEventEnum.DiscoveredDisinformation, DiplomaticEventEnum.DiscoveredIntellectualTheft,
        DiplomaticEventEnum.Trade, DiplomaticEventEnum.ShareTech, DiplomaticEventEnum.GiveAid};
    public GameObject DiplomacyUIGameObject; //The instantiated UI for this civ pair. a prefab clone, not a class but a game object
                                             // instantiated by DiplomacyManager from a prefab and added to DiplomacyController
    public DiplomacyController(DiplomacyData diplomacyData)
    {
        DiplomacyData = diplomacyData;
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
            this.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.Neutral;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Friendly && currentStatusPoints < (int)DiplomacyStatusEnum.Allied)
        {
            this.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.Friendly;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Allied && currentStatusPoints < (int)DiplomacyStatusEnum.Membership)
        {
            this.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.Allied;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Membership && ((int)this.DiplomacyData.CivMajor.CivData.CivInt > 6 || (int)this.DiplomacyData.CivOther.CivData.CivInt > 6))
        {
            // only minors AI civ can become member of a playable major race
            this.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.Membership;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.UnFriendly && currentStatusPoints < (int)DiplomacyStatusEnum.Neutral)
        {
            this.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.UnFriendly;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Hostile && currentStatusPoints < (int)DiplomacyStatusEnum.UnFriendly)
        {
            this.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.Hostile;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.ColdWar && currentStatusPoints < (int)DiplomacyStatusEnum.Hostile)
        {
            this.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.ColdWar;
        }
        else if (currentStatusPoints >= (int)DiplomacyStatusEnum.War)
        {
            this.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.War;
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
        SceneController.Instance.LoadCombatScene();
    }

}
