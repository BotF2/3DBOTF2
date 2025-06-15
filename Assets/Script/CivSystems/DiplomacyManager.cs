using Assets.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
public enum NagotiationPoints
{
    DeclarWar,
    Sanctions,
    ThreatenAction,
    OfferTraid,
    OfferCulturalExchange,
    OfferTech,
    OfferAid,
    OfferAlliance
}
public enum SecretService
{
    Sabatoge,
    Disinformation,
    IntellectualTheft,
    GatherIntelligence,
    Combat
}
public enum DiplomaticEventEnum // Diplomacy AI uses to move relations                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
{
    War,
    DiscoveredSabatoge,
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
   // public List<DiplomacyUIController> DiplomacyUIControllerList { get; private set; } = new List<DiplomacyUIController>();
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
        if (diplomacyCon.DiplomacyData.CivMajor.CivData.CivEnum == GameController.Instance.GameData.LocalPlayerCivEnum
             || diplomacyCon.DiplomacyData.CivOther.CivData.CivEnum == GameController.Instance.GameData.LocalPlayerCivEnum)
        {
            if (diplomacyCon.DiplomacyUIGameObject == null)
            {
                GameObject thisDiplomacyUIGameObject = (GameObject)Instantiate(diplomacyUIPrefab, new Vector3(0, 0, 0),
                Quaternion.identity);
                thisDiplomacyUIGameObject.SetActive(true);
                thisDiplomacyUIGameObject.layer = 5;
                diplomacyCon.DiplomacyUIGameObject = thisDiplomacyUIGameObject;
                diplomacyUIGO = thisDiplomacyUIGameObject;
               // var thisDiplomacyUIController = thisDiplomacyUIGameObject.GetComponent<DiplomacyUIController>();
               // thisDiplomacyUIController.DiplomacyController = diplomacyCon; // set the diplomacy controller
                //thisDiplomacyUIController.LoadDiplomacyUI(diplomacyCon); // load the diplomacy UI with the diplomacy controller
                //DiplomacyUIControllerList.Add(thisDiplomacyUIController);
            }
        }
    }
    //public void SpaceCombatScene(FleetController fleetConA, FleetController fleetConB, StarSysController aNull)
    //{
    //    SceneController.Instance.LoadCombatScene();
    //    GalaxyMenuUIController.Instance.CloseMenu(Menu.DiplomacyMenu);
    //    //SubMenuManager.Instance.CloseMenu(Menu.DiplomacyMenu);
    //    ShipManager.Instance.ShipsForCombat(); //shipType, fleetGOinSys, this);
    //    //CombatManager.Instance.InstatniateCombat(controller.DiplomacyData.CivMajor.CivData.FleetControllers, controller.DiplomacyData.CivOther.CivData.FleetControllers);
    //}
    //public void SpaceCombatScene(FleetController fleetConA, FleetController fleetConB)
    //{
    //    SceneController.Instance.LoadCombatScene();
    //    GalaxyMenuUIController.Instance.CloseMenu(Menu.DiplomacyMenu);
    //    ShipManager.Instance.ShipsForCombat(); //shipType, fleetGOinSys, this);
    //    //CombatManager.Instance.InstatniateCombat(controller.DiplomacyData.CivMajor.CivData.FleetControllers, controller.DiplomacyData.CivOther.CivData.FleetControllers);
    //}
    public void FirstContactGetNewDiplomacyContoller(CivController civPartyOne, FleetController fleetPartyOne, CivController civPartyTwo, FleetController fleetPartyTwo, StarSysController sysCon)
    {// is frist contact diplomacy
        bool okForNewDiplomacyController = true;
        for (int i = 0; i < DiplomacyControllerList.Count; i++)
        {
            if (DiplomacyControllerList[i] != null)
            {
                if (DiplomacyControllerList[i].DiplomacyData.CivMajor == civPartyOne && DiplomacyControllerList[i].DiplomacyData.CivOther == civPartyTwo
                    || DiplomacyControllerList[i].DiplomacyData.CivOther == civPartyOne && DiplomacyControllerList[i].DiplomacyData.CivMajor == civPartyTwo)
                {
                    okForNewDiplomacyController = false; // we can create a new one
                }
            }
        }
        if (okForNewDiplomacyController) // we may need a new diplomacy controller
        {
            DiplomacyData diplomacyData = new DiplomacyData();
            if (civPartyOne.CivData.CivEnum <= CivEnum.TERRAN || civPartyTwo.CivData.CivEnum <= CivEnum.TERRAN) // diplomacy only when there is a major civ
            { // one or two major civs
                if (GameController.Instance.AreWeLocalPlayer(civPartyOne.CivData.CivEnum))
                {
                    diplomacyData.CivMajor = civPartyOne; // local player civ
                    diplomacyData.FleetMajor = fleetPartyOne; // local player fleet
                    diplomacyData.CivOther = civPartyTwo;
                    if (fleetPartyTwo.FleetData != null)
                    {
                        diplomacyData.FleetOther = fleetPartyTwo; // other civ fleet
                        diplomacyData.StarSysController = null; // other civ fleet in a star system
                        Destroy(sysCon.gameObject); // destroy the star system controller, we do not need it
                    }
                    else
                    {
                        diplomacyData.StarSysController = sysCon; // other civ fleet in a star system
                        diplomacyData.FleetOther = null; // no other civ fleet
                        Destroy(fleetPartyTwo.gameObject);
                    }
                }
                else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo.CivData.CivEnum))
                {
                    diplomacyData.CivMajor = civPartyTwo; // local player civ
                    diplomacyData.FleetMajor = fleetPartyTwo; // local player fleet
                    diplomacyData.CivOther = civPartyOne;
                    if (fleetPartyOne.FleetData != null)
                    {
                        diplomacyData.FleetOther = fleetPartyOne; // other civ fleet
                        diplomacyData.StarSysController = null; // other civ fleet in a star system
                        Destroy(sysCon.gameObject); // destroy the star system controller, we do not need it
                    }
                    else
                    {
                        diplomacyData.StarSysController = sysCon; // other civ fleet in a star system
                        diplomacyData.FleetOther = null; // no other civ fleet
                        Destroy(fleetPartyOne.gameObject);
                    }
                }
                else // no local player
                { // one or two major civ present, no local player (only have a diplomacy with the non local player major civ with higher civInt first)
                    if (civPartyOne.CivData.CivEnum <= CivEnum.TERRAN && civPartyOne.CivData.CivEnum > civPartyTwo.CivData.CivEnum)
                    {
                        diplomacyData.CivMajor = civPartyOne; // major civ
                        diplomacyData.FleetMajor = fleetPartyOne; // major civ fleet
                        diplomacyData.CivOther = civPartyTwo;
                        if (fleetPartyTwo.FleetData != null)
                        {
                            diplomacyData.FleetOther = fleetPartyTwo; // other civ fleet
                            diplomacyData.StarSysController = null; // other civ fleet in a star system
                            Destroy(sysCon.gameObject); // destroy the star system controller, we do not need it
                        }
                        else
                        {
                            diplomacyData.StarSysController = sysCon; // other civ fleet in a star system
                            diplomacyData.FleetOther = null; // no other civ fleet
                            Destroy(fleetPartyTwo.gameObject);
                        }
                    }
                    else if (civPartyTwo.CivData.CivEnum <= CivEnum.TERRAN && civPartyTwo.CivData.CivEnum > civPartyOne.CivData.CivEnum)
                    {
                        diplomacyData.CivMajor = civPartyTwo; // major civ
                        diplomacyData.FleetMajor = fleetPartyTwo; // major civ fleet
                        diplomacyData.CivOther = civPartyOne;
                        if (fleetPartyOne.FleetData != null)
                        {
                            diplomacyData.FleetOther = fleetPartyOne; // other civ fleet
                            diplomacyData.StarSysController = null; // other civ fleet in a star system
                            Destroy(sysCon.gameObject); // destroy the star system controller, we do not need it
                        }
                        else
                        {
                            diplomacyData.StarSysController = sysCon; // other civ fleet in a star system
                            diplomacyData.FleetOther = null; // no other civ fleet
                            Destroy(fleetPartyOne.gameObject);
                        }
                    }
                }
                DiplomacyController diplomacyController = new DiplomacyController(diplomacyData);
                diplomacyController.DiplomacyData.DiplomacyEnumOfCivs = CalculateDiplomaticStatusOnFirstContact(diplomacyController);
                diplomacyController.DiplomacyData.DiplomacyPointsOfCivs = (int)diplomacyController.DiplomacyData.DiplomacyEnumOfCivs;

                InstantiateDiplomacyUIGameObject(diplomacyController);

                GalaxyMenuUIController.Instance.OpenADiplomacyUI(diplomacyController);

                #region testing auto combat
                // For Testing..... 
                //if ((diplomacyController.DiplomacyData.CivMajor.CivData.CivEnum == CivEnum.FED
                //    && diplomacyController.DiplomacyData.CivOther.CivData.CivEnum == CivEnum.VULCANS)
                //    || (diplomacyController.DiplomacyData.CivMajor.CivData.CivEnum == CivEnum.VULCANS
                //    && diplomacyController.DiplomacyData.CivOther.CivData.CivEnum == CivEnum.FED))
                //{
                //    diplomacyController.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.War;
                //    diplomacyController.DiplomacyData.DiplomacyPointsOfCivs = 0;
                //}
                //else
                //{

                //    diplomacyController.DiplomacyData.DiplomacyEnumOfCivs = DiplomacyStatusEnum.Neutral;
                //    diplomacyController.DiplomacyData.DiplomacyPointsOfCivs = 60; //60 = netural on a -20 to 120 scale
                //}
                #endregion
                DiplomacyControllerList.Add(diplomacyController);
                //UpdateOurDiplomacyController(diplomacyController.DiplomacyData.FleetMajor, diplomacyController.DiplomacyData.FleetOther); //, null);
            }
            else
            {// two minor civs so no diplomacy controller
                //UpdateOurDiplomacyController(diplomacyController.DiplomacyData.FleetMajor, diplomacyController.DiplomacyData.FleetOther)
            }
        }
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
                if (DiplomacyControllerList[i].DiplomacyData.CivMajor == civPartyOne && DiplomacyControllerList[i].DiplomacyData.CivOther == civPartyTwo
                    || DiplomacyControllerList[i].DiplomacyData.CivOther == civPartyOne && DiplomacyControllerList[i].DiplomacyData.CivMajor == civPartyTwo)
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
                ourDiplomacyController.DiplomacyData.CivMajor = civPartyOne; // local player civ
                ourDiplomacyController.DiplomacyData.CivOther = civPartyTwo;
            }
            else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo.CivData.CivEnum))
            {
                ourDiplomacyController.DiplomacyData.CivMajor = civPartyTwo; // local player civ
                ourDiplomacyController.DiplomacyData.CivOther = civPartyOne;
            }
            GalaxyMenuUIController.Instance.OpenADiplomacyUI(ourDiplomacyController); // it opens the ADiplomacy UI
        }
    }
    public void UpdateOurDiplomacyController(FleetController fleetPartyOne, FleetController fleetPartyTwo, StarSysController sysCon)
    {
        CivController civPartyOne = fleetPartyOne.FleetData.CivController;
        CivController civPartyTwo = null;
        if (fleetPartyTwo != null)
        {
            civPartyTwo = fleetPartyTwo.FleetData.CivController;
        }
        else if (fleetPartyTwo == null)
        {
            civPartyTwo =  sysCon.StarSysData.CurrentCivController; // the civ controller of the star system
        }

        DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
        if (ourDiplomacyController != null)
        {
            if (civPartyOne.CivData.CivEnum <= CivEnum.TERRAN || civPartyTwo.CivData.CivEnum <= CivEnum.TERRAN)
            {
                if (GameController.Instance.AreWeLocalPlayer(civPartyOne.CivData.CivEnum))
                {
                    ourDiplomacyController.DiplomacyData.CivMajor = civPartyOne; // local player civ
                    ourDiplomacyController.DiplomacyData.CivOther = civPartyTwo;
                    ourDiplomacyController.DiplomacyData.FleetMajor = fleetPartyOne;
                    ourDiplomacyController.DiplomacyData.FleetOther = fleetPartyTwo;
                }
                else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo.CivData.CivEnum))
                {
                    ourDiplomacyController.DiplomacyData.CivMajor = civPartyTwo; // local player civ
                    ourDiplomacyController.DiplomacyData.CivOther = civPartyOne;
                    ourDiplomacyController.DiplomacyData.FleetMajor = fleetPartyTwo;
                    ourDiplomacyController.DiplomacyData.FleetOther = fleetPartyOne;
                }
                else // no local player, but at least one major civ, no combat UI.
                { // one or two major civ present, no local player (only have a diplomacy with the non local player major civ with higher civInt first)
                    // ToDo: combat without a UI, AI combat in the background
                    if (civPartyOne.CivData.CivEnum <= CivEnum.TERRAN)
                    {
                        ourDiplomacyController.DiplomacyData.CivMajor = civPartyOne; // major civ
                        ourDiplomacyController.DiplomacyData.CivOther = civPartyTwo; // minor or major civ
                        ourDiplomacyController.DiplomacyData.FleetMajor = fleetPartyOne;
                        ourDiplomacyController.DiplomacyData.FleetOther = fleetPartyTwo;
                    }
                    else if (civPartyTwo.CivData.CivEnum <= CivEnum.TERRAN)
                    {
                        ourDiplomacyController.DiplomacyData.CivMajor = civPartyTwo; // major civ
                        ourDiplomacyController.DiplomacyData.CivOther = civPartyOne; // minor or major civ
                        ourDiplomacyController.DiplomacyData.FleetMajor = fleetPartyTwo;
                        ourDiplomacyController.DiplomacyData.FleetOther = fleetPartyOne;
                    }                                
                }
            }
            else 
            {
                // Two minor civs, do nothing
            }
        }
    }
    public void UpdateOurDiplomacyController(FleetController fleetCon, StarSysController sysCon) //, StarSysController sysCon)
    {
        CivController civPartyOne = fleetCon.FleetData.CivController;
        CivController civPartyTwo = sysCon.StarSysData.CurrentCivController;

        DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
        ourDiplomacyController.DiplomacyData.StarSysController = sysCon;
        if (ourDiplomacyController != null)
        {
            if (civPartyOne.CivData.CivEnum <= CivEnum.TERRAN || civPartyTwo.CivData.CivEnum <= CivEnum.TERRAN)
            {
                if (GameController.Instance.AreWeLocalPlayer(civPartyOne.CivData.CivEnum))
                {
                    ourDiplomacyController.DiplomacyData.CivMajor = civPartyOne; // local player civ
                    ourDiplomacyController.DiplomacyData.CivOther = civPartyTwo;
                    ourDiplomacyController.DiplomacyData.FleetMajor = fleetCon;

                }
                else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo.CivData.CivEnum))
                {
                    ourDiplomacyController.DiplomacyData.CivMajor = civPartyTwo; // local player civ
                    ourDiplomacyController.DiplomacyData.CivOther = civPartyOne;
                    ourDiplomacyController.DiplomacyData.FleetOther = fleetCon;
                }
                else // no local player, but at least one major civ, no combat UI.
                { // one or two major civ present, no local player (only have a diplomacy with the non local player major civ with higher civInt first)
                    // ToDo: combat without a UI, AI combat in the background
                    if (civPartyOne.CivData.CivEnum <= CivEnum.TERRAN)
                    {
                        ourDiplomacyController.DiplomacyData.CivMajor = civPartyOne; // major civ
                        ourDiplomacyController.DiplomacyData.CivOther = civPartyTwo; // minor or major civ
                        ourDiplomacyController.DiplomacyData.FleetMajor = fleetCon;
                    }
                    else if (civPartyTwo.CivData.CivEnum <= CivEnum.TERRAN)
                    {
                        ourDiplomacyController.DiplomacyData.CivMajor = civPartyTwo; // major civ
                        ourDiplomacyController.DiplomacyData.CivOther = civPartyOne; // minor or major civ
                        ourDiplomacyController.DiplomacyData.FleetOther = fleetCon;
                    }
                }
            }
            else
            {
                // Two minor civs, do nothing
            }
        }
    }
    public DiplomacyController ReturnADiplomacyController(CivController civPartyOne, CivController civPartyTwo)
    {
        DiplomacyController diplomacyController = null;
        for (int i = 0; i < DiplomacyControllerList.Count; i++)
        {
            if (DiplomacyControllerList[i] != null && ((DiplomacyControllerList[i].DiplomacyData.CivMajor == civPartyOne && DiplomacyControllerList[i].DiplomacyData.CivOther == civPartyTwo)
                || (DiplomacyControllerList[i].DiplomacyData.CivMajor == civPartyTwo && DiplomacyControllerList[i].DiplomacyData.CivOther == civPartyOne)))
            {
                diplomacyController = DiplomacyControllerList[i];
                break;
            }
        }
        return diplomacyController;
    }
    public DiplomacyStatusEnum CalculateDiplomaticStatusOnFirstContact(DiplomacyController ourDiploCon)
    {
        CivController civOne = ourDiploCon.DiplomacyData.CivMajor;
        CivController civTwo = ourDiploCon.DiplomacyData.CivOther;
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


