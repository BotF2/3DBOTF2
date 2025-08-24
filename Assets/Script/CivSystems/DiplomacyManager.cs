using Assets.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum DiplomacyStatusEnum // between two civs held in the DiplomacyData
{
    War = -20,
    ColdWar = 0,
    Hostile = 20,
    UnFriendly = 40,
    Neutral = 60,
    Friendly = 80,
    Allied = 100,
    Membership = 120
}
public enum NegotiationPloysEnum // Diplomacy AI uses to change relations
{
    OfferTrade, // default
    DeclareWar,
    Sanctions,
    ThreatenAction,
    OfferCulturalExchange,
    OfferTech,
    OfferAid,
    OfferAlliance
}
public enum SecretActionsEnum // Secret actions that can be used by the AI or player to change relations
{
    GatherIntelligence, // default
    Sabotage,
    Disinformation,
    IntellectualTheft,
    Combat
}
public enum DiplomaticEventEnum // Diplomacy AI uses to move relations                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
{
    War,
    DiscoveredSabotage,
    DiscoveredDisinformation,
    DiscoveredIntellectualTheft,
    CulturalExchange,
    Trade,
    ShareTech,
    GiveAid,
    Alliance
}
public enum WarLikeEnum // held by the civ
{
    Warlike = -2,
    Aggressive = -1,
    Neutral = 0,
    Peaceful = 1,
    Pacifist = 2
}
public enum XenophobiaEnum // held by the civ
{
    Xenophobia = -2,
    Intolerant = -1,
    Indifferent = 0,
    Sympathetic = 1,
    Compassion = 2
}
public enum RuthlessEnum
{
    Ruthless = -2,
    Callous = -1,
    Regulated = 0,
    Ethical = 1,
    Honorable = 2
}
public enum GreedyEnum
{
    Greedy = -2,
    Materialistic = -1,
    Transactional = 0,
    Egaliterian = 1,
    Idealistic = 2
}

public class DiplomacyManager : MonoBehaviour
{
    public static DiplomacyManager Instance;
    public List<DiplomacyController> DiplomacyControllerList { get; private set; } = new List<DiplomacyController>();
    [SerializeField]
    private GameObject diplomacyUIPrefab;
    [SerializeField]
    private GameObject diplomacyUIGO;


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

    private void InstantiateDiplomacyUIGameObject(DiplomacyController diplomacyCon)
    {
        if (diplomacyCon.DiplomacyData.CivSideOne == GameController.Instance.GameData.LocalPlayerCivEnum
             || diplomacyCon.DiplomacyData.CivSideTwo == GameController.Instance.GameData.LocalPlayerCivEnum)
        {
            if (diplomacyCon.DiplomacyUIGameObject == null)
            {
                GameObject thisDiplomacyUIGameObject = (GameObject)Instantiate(diplomacyUIPrefab, new Vector3(0, 0, 0),
                Quaternion.identity);
                thisDiplomacyUIGameObject.SetActive(true);
                thisDiplomacyUIGameObject.layer = 5;
                diplomacyCon.DiplomacyUIGameObject = thisDiplomacyUIGameObject;
                diplomacyUIGO = thisDiplomacyUIGameObject;
            }
        }
    }

    public void FirstContactInitNewDiplomacyContoller(CivController civSideOne, FleetController fleetSideOne,
    CivController civSideTwo, FleetController fleetSideTwo, StarSysController sysCon)
    {
        DiplomacyData diplomacyData = null; 

        diplomacyData = new DiplomacyData(civSideOne.CivData.CivEnum, civSideTwo.CivData.CivEnum);
        if (civSideOne.CivData.CivEnum <= CivEnum.TERRAN || civSideTwo.CivData.CivEnum <= CivEnum.TERRAN) // diplomacy only when there is one major civ
        { // one or two is a major civs

            diplomacyData.CivSideOne = civSideOne.CivData.CivEnum;
            diplomacyData.CivSideTwo = civSideTwo.CivData.CivEnum;
        }

        DiplomacyController diplomacyController = new DiplomacyController(diplomacyData);
        diplomacyController.DiplomacyData.DiplomacyStatusEnumOfCivs = CalculateDiplomaticStatusOnFirstContact(diplomacyController);
        diplomacyController.DiplomacyData.DiplomacyPointsOfCivs = (int)diplomacyController.DiplomacyData.DiplomacyStatusEnumOfCivs;
        DiplomacyControllerList.Add(diplomacyController);
        InstantiateDiplomacyUIGameObject(diplomacyController);

        GalaxyMenuUIController.Instance.OpenADiplomacyUI(diplomacyController);
    }
    
    private void DoDiplomacyForAI(DiplomacyController diploCon) //, GameObject weHitGO)
    {
        //Do SpaceCombatScene or so some other diplomacy without a UI by/for either civ
    }
    public bool FoundADiplomacyController(CivController civPartyOne, CivController civPartyTwo) //, GameObject hitGO)
    {
        bool found = false;
        //List<DiplomacyController> placeholderControllers = new List<DiplomacyController>();
        for (int i = 0; i < DiplomacyControllerList.Count; i++)
        {
            if (DiplomacyControllerList[i] != null)
            {
                if (DiplomacyControllerList[i].DiplomacyData.CivSideOne == civPartyOne.CivData.CivEnum && DiplomacyControllerList[i].DiplomacyData.CivSideTwo == civPartyTwo.CivData.CivEnum
                    || DiplomacyControllerList[i].DiplomacyData.CivSideTwo == civPartyOne.CivData.CivEnum && DiplomacyControllerList[i].DiplomacyData.CivSideOne == civPartyTwo.CivData.CivEnum)
                {
                    found = true;
                    break;
                }
            }
        }
        return found;
    }
    public void OpenDiplomacyUI(CivController civPartyOne, CivController civPartyTwo)
    {
        DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
        if (ourDiplomacyController != null)
        {
            if (GameController.Instance.AreWeLocalPlayer(civPartyOne.CivData.CivEnum))
            {
                ourDiplomacyController.DiplomacyData.CivSideOne = civPartyOne.CivData.CivEnum; // local player civ
                ourDiplomacyController.DiplomacyData.CivSideTwo = civPartyTwo.CivData.CivEnum;
            }
            else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo.CivData.CivEnum))
            {
                ourDiplomacyController.DiplomacyData.CivSideOne = civPartyTwo.CivData.CivEnum; // local player civ
                ourDiplomacyController.DiplomacyData.CivSideTwo = civPartyOne.CivData.CivEnum;
            }
            GalaxyMenuUIController.Instance.OpenADiplomacyUI(ourDiplomacyController); // it opens the ADiplomacy UI
        }
    }
    public void UpdateOurDiplomacyController(FleetController fleetPartyOne, FleetController fleetPartyTwo)
    {// already sorted to civSideOne and civSideTwo
        CivController civPartyOne = fleetPartyOne.FleetData.CivController;
        CivController civPartyTwo = fleetPartyTwo.FleetData.CivController;

        DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
        //if (ourDiplomacyController != null) // do this on combat only from diplomacy UI
        //{
        //    ourDiplomacyController.DiplomacyData.CurrentFleetOfSideOne = fleetPartyOne;
        //    ourDiplomacyController.DiplomacyData.CurrentFleetOfSideTwo = fleetPartyTwo;
        //}
    }
    public void UpdateOurDiplomacyController(FleetController fleetCon, StarSysController sysCon) //, StarSysController sysCon)
    {
        CivController civPartyOne;
        CivController civPartyTwo;
        if (fleetCon.FleetData.CivEnum < sysCon.StarSysData.CurrentOwnerCivEnum)
        {
            civPartyOne = fleetCon.FleetData.CivController;
            civPartyTwo = sysCon.StarSysData.CurrentCivController;
        }
        else
        {
            civPartyOne = sysCon.StarSysData.CurrentCivController;
            civPartyTwo = fleetCon.FleetData.CivController;
        }
        DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
        //if (ourDiplomacyController != null) // do this on combat only from diplomacy UI
        //{
        //    ourDiplomacyController.DiplomacyData.CurrentStarSysController = sysCon;
        //    ourDiplomacyController.DiplomacyData.CurrentFleetOfSideOne = fleetCon;
        //}

        //else // A minor civs, do no diplomacy update
        //{

        //}

    }
    public DiplomacyController ReturnADiplomacyController(CivController civPartyOne, CivController civPartyTwo)
    {
        DiplomacyController diplomacyController = null;
        for (int i = 0; i < DiplomacyControllerList.Count; i++)
        {
            if (DiplomacyControllerList[i] != null && ((DiplomacyControllerList[i].DiplomacyData.CivSideOne == civPartyOne.CivData.CivEnum && 
                DiplomacyControllerList[i].DiplomacyData.CivSideTwo == civPartyTwo.CivData.CivEnum)
                || (DiplomacyControllerList[i].DiplomacyData.CivSideOne == civPartyTwo.CivData.CivEnum && DiplomacyControllerList[i].DiplomacyData.CivSideTwo == civPartyOne.CivData.CivEnum)))
            {
                diplomacyController = DiplomacyControllerList[i];
                break;
            }
        }
        return diplomacyController;
    }
    public DiplomacyStatusEnum CalculateDiplomaticStatusOnFirstContact(DiplomacyController ourDiploCon)
    {
        CivController civOne = CivManager.Instance.GetCivControllerByCivEnum(ourDiploCon.DiplomacyData.CivSideOne);
        CivController civTwo = CivManager.Instance.GetCivControllerByCivEnum(ourDiploCon.DiplomacyData.CivSideTwo);
        DiplomacyStatusEnum diplomacyStatus = DiplomacyStatusEnum.Neutral;
        int warLike = Math.Abs((int)civOne.CivData.Warlike - (int)civTwo.CivData.Warlike);
        int xenophobia = Math.Abs((int)civOne.CivData.Xenophbia - (int)civTwo.CivData.Xenophbia);
        int ruthless = Math.Abs((int)civOne.CivData.Ruthelss - (int)civTwo.CivData.Ruthelss);
        int greedy = Math.Abs((int)civOne.CivData.Greedy - (int)civTwo.CivData.Greedy);
        int degreesOfSparation = warLike + xenophobia + ruthless + greedy;
        switch (degreesOfSparation)
        {
            case 0:
                diplomacyStatus = DiplomacyStatusEnum.Friendly;
                break;
            case 1:
            case 2:
            case 3:
            case 4:
                diplomacyStatus = DiplomacyStatusEnum.Neutral;
                break;
            case 5:
            case 6:
            case 7:
            case 8:
                diplomacyStatus = DiplomacyStatusEnum.UnFriendly;
                break;
            case 9:
            case 10:
            case 11:
            case 12:
                diplomacyStatus = DiplomacyStatusEnum.Hostile;
                break;
            case 13:
            case 14:
            case 15:
            case 16:
                diplomacyStatus = DiplomacyStatusEnum.ColdWar;
                break;
            default:
                diplomacyStatus = DiplomacyStatusEnum.Neutral;
                break;
        }
        return diplomacyStatus;
    }
}


