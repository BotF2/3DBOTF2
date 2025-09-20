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
    public List<DiplomacyController> DiplomacyControllers { get; private set; } = new List<DiplomacyController>();
    [SerializeField]
    private GameObject diplomacyUIPrefab;
    [SerializeField]
    private GameObject diplomacyUIGO;
    private Camera galaxyEventCamera;


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
    public void Start()
    {
        galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;

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

    public void InitNewDiplomacyContoller(CivController civSideOne, FleetController fleetSideOne,
    CivController civSideTwo, FleetController fleetSideTwo, StarSysController sysCon)
    {
        DiplomacyData diplomacyData = null;
        List<ShipController> notLocalShips;
        diplomacyData = new DiplomacyData(civSideOne.CivData.CivEnum, civSideTwo.CivData.CivEnum);
        if (civSideOne.CivData.CivEnum <= CivEnum.TERRAN || civSideTwo.CivData.CivEnum <= CivEnum.TERRAN) // diplomacy only when there is one major civ
        { 
            // one or two is a major civs
            diplomacyData.CivSideOne = civSideOne.CivData.CivEnum;
            diplomacyData.CivSideTwo = civSideTwo.CivData.CivEnum;
        }
        DiplomacyController diplomacyController = new DiplomacyController(diplomacyData);
        diplomacyController.DiplomacyData.DiplomacyStatusEnumOfCivs = CalculateDiplomaticStatusOnFirstContact(diplomacyController);
        diplomacyController.DiplomacyData.DiplomacyPointsOfCivs = (int)diplomacyController.DiplomacyData.DiplomacyStatusEnumOfCivs;
        DiplomacyControllers.Add(diplomacyController);
        InstantiateDiplomacyUIGameObject(diplomacyController);
        if (CivManager.Instance.LocalPlayerCivContoller == civSideOne)
        {
            if (fleetSideTwo != null)
                notLocalShips = fleetSideTwo.FleetData.ShipsList;
            else
                notLocalShips = sysCon.StarSysData.ShipsList;
        }
        else
        {
            if (fleetSideOne != null)
                notLocalShips = fleetSideOne.FleetData.ShipsList;
            else
                notLocalShips = sysCon.StarSysData.ShipsList;
        } 

            GalaxyMenuUIController.Instance.OpenADiplomacyUI(diplomacyController, notLocalShips);
    }

    public bool FoundADiplomacyController(CivController civPartyOne, CivController civPartyTwo) //, GameObject hitGO)
    {
        bool found = false;
        //List<DiplomacyController> placeholderControllers = new List<DiplomacyController>();
        for (int i = 0; i < DiplomacyControllers.Count; i++)
        {
            if (DiplomacyControllers[i] != null)
            {
                if (DiplomacyControllers[i].DiplomacyData.CivSideOne == civPartyOne.CivData.CivEnum && DiplomacyControllers[i].DiplomacyData.CivSideTwo == civPartyTwo.CivData.CivEnum
                    || DiplomacyControllers[i].DiplomacyData.CivSideTwo == civPartyOne.CivData.CivEnum && DiplomacyControllers[i].DiplomacyData.CivSideOne == civPartyTwo.CivData.CivEnum)
                {
                    found = true;
                    break;
                }
            }
        }
        return found;
    }
    public DiplomacyController ReturnADiplomacyController(CivEnum oneSide, CivEnum otherSide)
    {
        DiplomacyController diplomacyController = null;
        for (int i = 0; i < DiplomacyControllers.Count; i++)
        {
            if (DiplomacyControllers[i] != null && ((DiplomacyControllers[i].DiplomacyData.CivSideOne == oneSide && DiplomacyControllers[i].DiplomacyData.CivSideTwo == otherSide)
                || (DiplomacyControllers[i].DiplomacyData.CivSideOne == otherSide && DiplomacyControllers[i].DiplomacyData.CivSideTwo == oneSide)))
            {
                diplomacyController = DiplomacyControllers[i];
                break;
            }
        }
        return diplomacyController;
    }
    public void OpenDiplomacyUI(CivController civPartyOne, CivController civPartyTwo, List<ShipController> shipList)
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
            GalaxyMenuUIController.Instance.OpenADiplomacyUI(ourDiplomacyController, shipList); // it opens the ADiplomacy UI
        }
    }
    public void CheckForAIDiplomacy(FleetController fleetCon1, FleetController fleetCon2)
    {
        CivController civPartyOne;
        CivController civPartyTwo;
        if (fleetCon1.FleetData.CivEnum < fleetCon2.FleetData.CivEnum)
        {
            civPartyOne = fleetCon1.FleetData.CivController;
            civPartyTwo = fleetCon2.FleetData.CivController;
        }
        else
        {
            civPartyOne = fleetCon2.FleetData.CivController;
            civPartyTwo = fleetCon1.FleetData.CivController;
        }
        DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
        if (civPartyOne.CivData.PlayedByAI)
            ourDiplomacyController.DoAIDiplomacy();
        else if ( civPartyTwo.CivData.PlayedByAI)
        {
            ourDiplomacyController.DoAIDiplomacy();
        }

    }
    public void CheckForAIDiplomacy(FleetController fleetCon, StarSysController sysCon)
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
        if (civPartyOne.CivData.PlayedByAI)
            ourDiplomacyController.DoAIDiplomacy();
        else if (civPartyTwo.CivData.PlayedByAI)
        {
            ourDiplomacyController.DoAIDiplomacy();
        }
    }
    public DiplomacyController ReturnADiplomacyController(CivController civPartyOne, CivController civPartyTwo)
    {
        DiplomacyController diplomacyController = null;
        for (int i = 0; i < DiplomacyControllers.Count; i++)
        {
            if (DiplomacyControllers[i] != null && ((DiplomacyControllers[i].DiplomacyData.CivSideOne == civPartyOne.CivData.CivEnum && 
                DiplomacyControllers[i].DiplomacyData.CivSideTwo == civPartyTwo.CivData.CivEnum)
                || (DiplomacyControllers[i].DiplomacyData.CivSideOne == civPartyTwo.CivData.CivEnum && DiplomacyControllers[i].DiplomacyData.CivSideTwo == civPartyOne.CivData.CivEnum)))
            {
                diplomacyController = DiplomacyControllers[i];
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


    public void FleetControllerVsOtherCivFleet(FleetController reportingPlayerFleet, FleetController otherFleet)
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
                DiplomacyManager.Instance.InitNewDiplomacyContoller(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, sysConEmpty);
                IntelligenceManager.Instance.InitializeNewIntelligenceController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, sysConEmpty);
                FirstContactFleetVsFleet(reportingPlayerFleet, otherFleet); // and add new diplomacy controller
                Destroy(sysConEmpty.gameObject); // we do not need the empty system controller anymore
            }
            else
            {
                DiplomacyManager.Instance.CheckForAIDiplomacy(sideOneFleetCon, sideTwoFleetCon);
                UpdateDiplomacyEncoutnerType(sideOneFleetCon, sideTwoFleetCon); // Will we need this? Is it all done in Diplomacy and FleetControllers?
            }
        }
    }

    private void FirstContactFleetVsFleet(FleetController reportingPlayerFleet, FleetController otherFleet)
    {
        var diplomacyData = EntereDiplomacyData(reportingPlayerFleet, otherFleet);
        diplomacyData.EncounterType = EncounterType.FirstContact;
        DiplomacyController diplomacyController = new DiplomacyController(diplomacyData);
        diplomacyController.DiplomacyData.firstContact = true;
        DiplomacyControllers.Add(diplomacyController);
    }
    private DiplomacyData EntereDiplomacyData(FleetController fleetConA, FleetController fleetConB)
    {
        DiplomacyData diplomacyData = new DiplomacyData();
        diplomacyData.FleetControllerCivOne = fleetConA;
        diplomacyData.CivOne = fleetConA.FleetData.CivController;
        diplomacyData.FleetContollerCivTwo = fleetConB;
        diplomacyData.CivTwo = fleetConB.FleetData.CivController;
        return diplomacyData;
    }
    private DiplomacyData EntereDiplomacyData(FleetController fleetConA, StarSysController starSysCon)
    {
        DiplomacyData diplomacyData = new DiplomacyData();
        diplomacyData.FleetControllerCivOne = fleetConA;
        diplomacyData.CivOne = fleetConA.FleetData.CivController;
        diplomacyData.StarSysController = starSysCon;
        diplomacyData.CivTwo = starSysCon.StarSysData.CurrentCivController;
        return diplomacyData;
    }
    private void UpdateDiplomacyEncoutnerType(FleetController fleetA, FleetController fleetB)
    { // *** Will we need this?
        var diplomacyCon = ReturnADiplomacyController(fleetA.FleetData.CivEnum, fleetB.FleetData.CivEnum); // not mono behavior
        diplomacyCon.DiplomacyData.EncounterType = EncounterType.Diplomacy;

    }

    internal void ResolveEncounterOtherCivSystem(FleetController reportingPlayerfleet, StarSysController otherCivSysCon)
    {
        // already not one of our systems
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
                    FirstContactFleetVsStarSys(reportingPlayerfleet, otherCivSysCon); // do we do something special with system entry here?
                    //IntelligenceManager.Instance.InitializeNewIntelligenceController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, otherCivSysCon);
                }
                else
                { // not first contact
                    DiplomacyManager.Instance.CheckForAIDiplomacy(sideOneFleetCon, otherCivSysCon);
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
            Destroy(fleetConEmpty.gameObject); // we do not need the empty fleet controller anymore
            foreach (ShipController shipController in reportingPlayerfleet.FleetData.GetShipList())
            {
                if (shipController.ShipData.ShipType == ShipType.Transport)
                {
                    // ToDo: Colonies Opption/ UI?
                }
            }
        }
    }

    private void FeetsUninhabitedSysEncounter(FleetController reportingPlayerfleet, StarSysController uninhabitedSysCon)
    {
        var diplomacyData = EntereDiplomacyData(reportingPlayerfleet, uninhabitedSysCon); // not mono behavior
        diplomacyData.EncounterType = EncounterType.UninhabitedSystem;
        DiplomacyController diplomacyController = new DiplomacyController(diplomacyData);
        diplomacyController.ResolveUninhabitedSystem(reportingPlayerfleet.FleetData.CivController, uninhabitedSysCon);
        DiplomacyControllers.Add(diplomacyController);
    }

    private void FirstContactFleetVsStarSys(FleetController fleetCon, StarSysController starSysCon)
    {
        var diplomacyData = EntereDiplomacyData(fleetCon, starSysCon); // not mono behavior
        diplomacyData.EncounterType = EncounterType.FirstContact;
        DiplomacyController diplomacyController = new DiplomacyController(diplomacyData);
        if (starSysCon.StarSysData.SystemType >= GalaxyObjectType.BlackHole) // resolve a non diplomatic encounter
            diplomacyController.ResolveFleetToStrangGalacticEncounter(diplomacyController);
        diplomacyController.DiplomacyData.firstContact = true;
        DiplomacyControllers.Add(diplomacyController);
    }
    public void FeetToSysNotSameCivNotFirstEncounter(FleetController fleetA, StarSysController sysCon)
    {
        var diplomacyData = EntereDiplomacyData(fleetA, sysCon); // not mono behavior
        diplomacyData.EncounterType = EncounterType.Diplomacy;
        DiplomacyController encounterController = new DiplomacyController(diplomacyData);
        DiplomacyControllers.Add(encounterController);
    }

    internal void ResolveDiplomacyForClickSystem(CivController localPlayerCivContoller, StarSysController starSysController)
    {
       //already not one of our fleets
        CivController civPartyOne;
        CivController civPartyTwo;

        if ((int)localPlayerCivContoller.CivData.CivEnum < (int)starSysController.StarSysData.CurrentCivController.CivData.CivEnum)
        {
            civPartyOne = localPlayerCivContoller;
            civPartyTwo = starSysController.StarSysData.CurrentCivController;
        }
        else // other civ is side one
        {
            civPartyOne = starSysController.StarSysData.CurrentCivController;
            civPartyTwo = localPlayerCivContoller;
        }
        //have we met before?
        if (DiplomacyManager.Instance.FoundADiplomacyController(civPartyOne, civPartyTwo))
        {   // not First Contact, just by clicking on the system
            DiplomacyManager.Instance.OpenDiplomacyUI(civPartyOne, civPartyTwo, starSysController.StarSysData.ShipsList);
            //DiplomacyManager.Instance.UpdateOurDiplomacyController(civPartyOne, civPartyTwo);
        }
        else
        {
            // no first contact just on clicking on the system
            // maybe some data if you are high tech level?
        }
    }
}


