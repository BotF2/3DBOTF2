using Assets.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

public class IntelligenceManager : MonoBehaviour
{
    public static IntelligenceManager Instance;
    [SerializeField]
    private GameObject intelligenceUIPrefab;
    public List<IntelligenceController> IntelligenceControllerList { get; private set; } = new List<IntelligenceController>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void InitializeNewIntelligenceController(CivController civSideOne, FleetController fleetControllerSideOne, CivController civSideTwo, FleetController fleetControllerSideTwo, StarSysController sysCon)
    {
        IntelligenceData intelData = null;
        intelData = new IntelligenceData(fleetControllerSideOne, fleetControllerSideTwo, sysCon);
        if (civSideOne.CivData.CivEnum <= CivEnum.TERRAN || civSideTwo.CivData.CivEnum <= CivEnum.TERRAN) // diplomacy only when there is one major civ
        { // one or two is a major civs

            intelData.CivSideOne = civSideOne.CivData.CivEnum; 
            intelData.LastSeenFleetOfSideOne = fleetControllerSideOne; // could be null, do this on combat form Diplomacy UI
            if (fleetControllerSideOne != null && fleetControllerSideOne.FleetData != null && fleetControllerSideOne.FleetData.ShipsList != null)
            {
                intelData.LastSeenFleetOfSideOne.FleetData.ShipsList = fleetControllerSideOne.FleetData.ShipsList; // make a copy of the list
            }
            intelData.CivSideTwo = civSideTwo.CivData.CivEnum;
            intelData.LastSeenFleetOfSideTwo = fleetControllerSideTwo; // do this on combat
            if ( fleetControllerSideTwo != null && fleetControllerSideTwo.FleetData != null && fleetControllerSideTwo.FleetData.ShipsList != null)
            {
                intelData.LastSeenFleetOfSideTwo.FleetData.ShipsList = fleetControllerSideTwo.FleetData.ShipsList;
            }
            intelData.LastSeenStarSysController = sysCon; // do this on combat
        }

        IntelligenceController intelligenceController = new IntelligenceController(intelData);
        //intelligenceController.IntelligenceData.IntelligenceStatusEnumOfCivs = CalculateIntelligenceStatusOnFirstContact(intelligenceController);
        //intelligenceController.IntelligenceData.IntelligencePointsOfCivs = (int)intelligenceController.IntelligenceData.IntelligenceStatusEnumOfCivs;
        IntelligenceControllerList.Add(intelligenceController);
        InstantiateIntelligenceUIGameObject(intelligenceController);
    }


    private void InstantiateIntelligenceUIGameObject(IntelligenceController intelligenceCon)
    {
        if (intelligenceCon.IntelligenceData.CivSideOne == GameController.Instance.GameData.LocalPlayerCivEnum
             || intelligenceCon.IntelligenceData.CivSideTwo == GameController.Instance.GameData.LocalPlayerCivEnum)
        {
            if (intelligenceCon.IntelligenceUIGameObject == null)
            {//*** to do - make the intel UI prefab and instantiate it here
                //GameObject thisIntelUIGameObject = (GameObject)Instantiate(intelligenceUIPrefab, new Vector3(0, 0, 0),
                //Quaternion.identity);
                //thisIntelUIGameObject.SetActive(true);
                //thisIntelUIGameObject.layer = 5;
                //intelligenceCon.IntelligenceUIGameObject = thisIntelUIGameObject;
            }
        }
    }
    public bool FoundAnIntelController(CivController civPartyOne, CivController civPartyTwo) //, GameObject hitGO)
    {
        bool found = false;
        //List<DiplomacyController> placeholderControllers = new List<DiplomacyController>();
        for (int i = 0; i < IntelligenceControllerList.Count; i++)
        {
            if (IntelligenceControllerList[i] != null)
            {
                if (IntelligenceControllerList[i].IntelligenceData.CivSideOne == civPartyOne.CivData.CivEnum && IntelligenceControllerList[i].IntelligenceData.CivSideTwo == civPartyTwo.CivData.CivEnum
                    || IntelligenceControllerList[i].IntelligenceData.CivSideTwo == civPartyOne.CivData.CivEnum && IntelligenceControllerList[i].IntelligenceData.CivSideOne == civPartyTwo.CivData.CivEnum)
                {
                    found = true;
                    break;
                }
            }
        }
        return found;
    }
    public void OpenIntelligenceUI(CivEnum civPartyOne, CivEnum civPartyTwo)
    {
        IntelligenceController ourIntelligenceController = ReturnAnIntelligenceController(civPartyOne, civPartyTwo);
        if (ourIntelligenceController != null)
        {
            if (GameController.Instance.AreWeLocalPlayer(civPartyOne))
            {
                ourIntelligenceController.IntelligenceData.CivSideOne = civPartyOne; // local player civ
                ourIntelligenceController.IntelligenceData.CivSideTwo = civPartyTwo;
            }
            else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo))
            {
                ourIntelligenceController.IntelligenceData.CivSideOne = civPartyTwo; // local player civ
                ourIntelligenceController.IntelligenceData.CivSideTwo = civPartyOne;
            }
           // *** build intel UI for this!!
           // GalaxyMenuUIController.Instance.OpenAIntelligenceUI(ourIntelligenceController); // it opens the AIntelligence UI
        }
    }
    public void UpdateOurIntelController(CivController civPartyOne, FleetController fleetConA, CivController civPartyTwo, FleetController fleetConB, StarSysController sysCon) //, StarSysController sysCon)
    {
        //CivController civPartyOne;
        //CivController civPartyTwo;
        //if (fleetConA.FleetData.CivEnum < sysCon.StarSysData.CurrentOwnerCivEnum)
        //{
        //    civPartyOne = fleetCon.FleetData.CivController;
        //    civPartyTwo = sysCon.StarSysData.CurrentCivController;
        //}
        //else
        //{
        //    civPartyOne = sysCon.StarSysData.CurrentCivController;
        //    civPartyTwo = fleetCon.FleetData.CivController;
        //}
        //IntelligenceController ourIntelligenceController = ReturnAnIntelligenceController(civPartyOne, civPartyTwo);
    }
    public IntelligenceController ReturnAnIntelligenceController(CivEnum civPartyOne, CivEnum civPartyTwo)
    {
        IntelligenceController intelligenceController = null;
        for (int i = 0; i < IntelligenceControllerList.Count; i++)
        {
            if (IntelligenceControllerList[i] != null && ((IntelligenceControllerList[i].IntelligenceData.CivSideOne == civPartyOne &&
                IntelligenceControllerList[i].IntelligenceData.CivSideTwo == civPartyTwo)
                || (IntelligenceControllerList[i].IntelligenceData.CivSideOne == civPartyTwo && IntelligenceControllerList[i].IntelligenceData.CivSideTwo == civPartyOne)))
            {
                intelligenceController = IntelligenceControllerList[i];
                //if (intelligenceController.IntelligenceData.LastSeenFleetOfSideOne != null)
                //{
                //    intelligenceController.IntelligenceData.LastSeenFleetOfSideOne.FleetData.ShipsList.;
                //}
                break;
            }
        }
        return intelligenceController;
    }
}
