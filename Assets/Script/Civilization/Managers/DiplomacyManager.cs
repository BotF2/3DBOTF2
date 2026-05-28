using BOTF3D.Combat;

using BOTF3D.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.Civilization
{
    public class DiplomacyManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        public static DiplomacyManager Instance;
        public List<DiplomacyController> DiplomacyControllers { get; private set; } = new List<DiplomacyController>();
        [SerializeField]
        private GameObject diplomacyUIPrefab;
        [SerializeField]
        private GameObject diplomacControllerPrefab;

        private void Awake()
        {
            ServiceLocator.Register<DiplomacyManager>(this);
            if (Instance != null) { Destroy(gameObject); }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        public void Start()
        {
            // galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;

        }

        public DiplomacyController InstantiateDiplomacyController(CivController civSideOne, FleetController fleetSideOne,
        CivController civSideTwo, FleetController fleetSideTwo, StarSysController sysCon)
        {
            List<ShipController> shipsToSeeInLocalPayerDiploUI = new List<ShipController>();
            DiplomacyData diplomacyData = new DiplomacyData(civSideOne.CivData.CivEnum, civSideTwo.CivData.CivEnum);
            DiplomacyController diplomacyCon = Instantiate(diplomacControllerPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity).GetComponent<DiplomacyController>();
            diplomacyCon.enabled = true;
            diplomacyCon.DiplomacyData = diplomacyData;

            if (!DiplomacyControllers.Contains(diplomacyCon))
                DiplomacyControllers.Add(diplomacyCon);
            diplomacyCon.gameObject.SetActive(true);
            diplomacyCon.gameObject.layer = 5;
            diplomacyCon.transform.SetParent(this.transform);
            if (civSideOne.CivData.CivEnum <= CivEnum.TERRAN || civSideTwo.CivData.CivEnum <= CivEnum.TERRAN) // diplomacy only when there is one or more major civ
            {
                // one or two is a major civ so minors do not have diplomacy with other minors
                diplomacyData.CivEnumSideOne = civSideOne.CivData.CivEnum;
                diplomacyData.CivOne = civSideOne;
                diplomacyData.CivEnumSideTwo = civSideTwo.CivData.CivEnum;
                diplomacyData.CivTwo = civSideTwo;
                if (CivManager.Instance.LocalPlayerCivController == civSideOne)
                {
                    if (fleetSideTwo.FleetData != null)
                    {
                        fleetSideTwo.FleetData.ShipsList.RemoveAll(item => item == null);
                        shipsToSeeInLocalPayerDiploUI = fleetSideTwo.FleetData.ShipsList;
                        diplomacyData.FleetContollerCivTwo = fleetSideTwo;
                    }
                    else if (sysCon.StarSysData != null)
                    {
                        sysCon.StarSysData.ShipsList.RemoveAll(item => item == null);
                        shipsToSeeInLocalPayerDiploUI = sysCon.StarSysData.ShipsList;
                        diplomacyData.StarSysController = sysCon;
                    }
                }
                else
                {
                    if (fleetSideOne.FleetData != null)
                    {
                        fleetSideOne.FleetData.ShipsList.RemoveAll(item => item == null);
                        shipsToSeeInLocalPayerDiploUI = fleetSideOne.FleetData.ShipsList;
                    }
                    else
                    {
                        sysCon.StarSysData.ShipsList.RemoveAll(item => item == null);
                        shipsToSeeInLocalPayerDiploUI = sysCon.StarSysData.ShipsList;
                    }
                }
                diplomacyData.FleetControllerCivOne = fleetSideOne;
                diplomacyData.firstContact = true;
                diplomacyData.EncounterType = EncounterType.FirstContact;

            }
            else if (sysCon.StarSysData.SystemType >= GalaxyObjectType.BlackHole) // resolve a non conventional encounter
            {
                diplomacyCon.ResolveFleetToStrangGalacticEncounter(diplomacyCon); // ToDo
            }
            diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs = CalculateDiplomaticStatusOnFirstContact(diplomacyCon);
            diplomacyCon.DiplomacyData.DiplomacyPointsOfCivs = (int)diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs;
            InstantiateDiplomacyUIGameObject(diplomacyCon);
            
            // ✅ Open via GalaxyMenuUIController to ensure other menus close correctly
            GalaxyMenuUIController.Instance.OpenMenu(Menu.ADiplomacyMenu, diplomacyCon.gameObject);
            
            DiplomacyMenuUIController.Instance.SetUpDiplomacyUIElements(diplomacyCon.DiplomacyUIGameObject,
                diplomacyCon.gameObject, shipsToSeeInLocalPayerDiploUI);

            return diplomacyCon;
        }
        private void InstantiateDiplomacyUIGameObject(DiplomacyController diplomacyCon)
        {
            if (diplomacyCon.DiplomacyData.CivEnumSideOne == GameController.Instance.GameData.LocalPlayerCivEnum ||
                         diplomacyCon.DiplomacyData.CivEnumSideTwo == GameController.Instance.GameData.LocalPlayerCivEnum)
            {
                if (diplomacyCon.DiplomacyUIGameObject == null)
                {
                    GameObject uiGO = (GameObject)Instantiate(diplomacyUIPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    uiGO.SetActive(true);
                    uiGO.layer = 5;
                    diplomacyCon.DiplomacyUIGameObject = uiGO;
                    diplomacyCon.DiplomacyUIGameObject.SetActive(true);
                    DiplomacyMenuUIController.Instance.ADiplomacyMenuView.SetActive(true);
                    uiGO.transform.SetParent(DiplomacyMenuUIController.Instance.ADiplomacyMenuView.transform, false);
                    if (DiplomacyMenuUIController.Instance != null && !DiplomacyMenuUIController.Instance.ListOfDiplomacyUiGos.Contains(uiGO))
                    {
                        DiplomacyMenuUIController.Instance.ListOfDiplomacyUiGos.Add(uiGO);
                    }
                }
            }
        }
        public bool FoundADiplomacyController(CivController civPartyOne, CivController civPartyTwo) //, GameObject hitGO)
        {
            bool found = false;
            //List<DiplomacyController> placeholderControllers = new List<DiplomacyController>();
            for (int i = 0; i < DiplomacyControllers.Count; i++)
            {
                if (DiplomacyControllers[i] != null)
                {
                    if (DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == civPartyOne.CivData.CivEnum && DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == civPartyTwo.CivData.CivEnum
                        || DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == civPartyOne.CivData.CivEnum && DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == civPartyTwo.CivData.CivEnum)
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
                if (DiplomacyControllers[i] != null && ((DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == oneSide && DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == otherSide)
                    || (DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == otherSide && DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == oneSide)))
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
                    ourDiplomacyController.DiplomacyData.CivEnumSideOne = civPartyOne.CivData.CivEnum; // local player civ
                    ourDiplomacyController.DiplomacyData.CivEnumSideTwo = civPartyTwo.CivData.CivEnum;
                }
                else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo.CivData.CivEnum))
                {
                    ourDiplomacyController.DiplomacyData.CivEnumSideOne = civPartyTwo.CivData.CivEnum; // local player civ
                    ourDiplomacyController.DiplomacyData.CivEnumSideTwo = civPartyOne.CivData.CivEnum;
                }
                // ✅ Open via GalaxyMenuUIController to ensure other menus close correctly
                GalaxyMenuUIController.Instance.OpenMenu(Menu.ADiplomacyMenu, ourDiplomacyController.gameObject);

                DiplomacyMenuUIController.Instance.SetUpDiplomacyUIElements(ourDiplomacyController.DiplomacyUIGameObject,
                    ourDiplomacyController.gameObject, shipList);
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
            else if (civPartyTwo.CivData.PlayedByAI)
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
                if (DiplomacyControllers[i] != null && ((DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == civPartyOne.CivData.CivEnum &&
                    DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == civPartyTwo.CivData.CivEnum)
                    || (DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == civPartyTwo.CivData.CivEnum && DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == civPartyOne.CivData.CivEnum)))
                {
                    diplomacyController = DiplomacyControllers[i];
                    break;
                }
            }
            return diplomacyController;
        }
        public DiplomacyStatusEnum CalculateDiplomaticStatusOnFirstContact(DiplomacyController ourDiploCon)
        {
            CivController civOne = CivManager.Instance.GetCivControllerByCivEnum(ourDiploCon.DiplomacyData.CivEnumSideOne);
            CivController civTwo = CivManager.Instance.GetCivControllerByCivEnum(ourDiploCon.DiplomacyData.CivEnumSideTwo);
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
            reportingPlayerFleet.FleetData.ShipsList.RemoveAll(item => item == null);
            otherFleet.FleetData.ShipsList.RemoveAll(item => item == null);
            StarSysController sysConEmpty = StarSysManager.Instance.InstantiateEmptyStarSysController();
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
                    DiplomacyController newDiplomacyCon = DiplomacyManager.Instance.InstantiateDiplomacyController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, sysConEmpty);
                    if (!DiplomacyControllers.Contains(newDiplomacyCon))
                        DiplomacyControllers.Add(newDiplomacyCon);
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

            // Instantiate a DiplomacyController MonoBehaviour from prefab (or add component) so it's a Unity-managed component
            DiplomacyController diplomacyController = null;
            if (diplomacControllerPrefab != null)
            {
                GameObject dipGo = Instantiate(diplomacControllerPrefab, Vector3.zero, Quaternion.identity);
                dipGo.SetActive(true);
                dipGo.layer = 5;
                dipGo.transform.SetParent(this.transform, false);
                diplomacyController = dipGo.GetComponent<DiplomacyController>();
                if (diplomacyController == null)
                    diplomacyController = dipGo.AddComponent<DiplomacyController>();
            }
            else
            {
                GameObject dipGo = new GameObject("DiplomacyController");
                dipGo.transform.SetParent(this.transform, false);
                diplomacyController = dipGo.AddComponent<DiplomacyController>();
            }

            diplomacyController.DiplomacyData = diplomacyData;
            diplomacyController.DiplomacyData.firstContact = true;

            if (!DiplomacyControllers.Contains(diplomacyController))
                DiplomacyControllers.Add(diplomacyController);
        }
        private void ExposeCivToLocalPlayer(CivEnum civToExpose)
        {
            Debug.Log($"DiplomacyManager: Exposing {civToExpose} to Local Player.");
            StarSysManager.Instance.ExposeAllSystemName(civToExpose);
            FleetManager.Instance.ExposeAllFleetInsigniaSprites(civToExpose);
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
            diplomacyData.CivEnumSideOne = fleetConA.FleetData.CivEnum;
            diplomacyData.StarSysController = starSysCon;
            diplomacyData.CivTwo = starSysCon.StarSysData.CurrentCivController;
            diplomacyData.CivEnumSideTwo = starSysCon.StarSysData.CurrentOwnerCivEnum;
            diplomacyData.FleetContollerCivTwo = null;
            return diplomacyData;
        }
        private void UpdateDiplomacyEncoutnerType(FleetController fleetA, FleetController fleetB)
        { // *** Will we need this?
            var diplomacyCon = ReturnADiplomacyController(fleetA.FleetData.CivEnum, fleetB.FleetData.CivEnum); // not mono behavior
            diplomacyCon.DiplomacyData.EncounterType = EncounterType.Diplomacy;

        }

        internal void ResolveEncounterOtherCivSystem(FleetController reportingPlayerfleet, StarSysController otherCivSysCon)
        {
            Debug.Log("DiplomacyManager: ResolveEncounterOtherCivSystem called.");
            // already not one of our systems
            FleetController fleetConEmpty = FleetManager.Instance.InsatiateEmptyFleetController();
            int firstUninhabited = (int)CivEnum.ZZUNINHABITED1; // all lower than this are inhabited (including Borg UniComplex and inhabitable Nebula)

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
                        sideTwoFleetCon = fleetConEmpty; // we do not have the other fleet controller, so we use an empty place holder
                    }
                    else // other civ is side one
                    {
                        civSideOne = otherCivSysCon.StarSysData.CurrentCivController;
                        sideOneFleetCon = fleetConEmpty; // we do not have the other fleet controller, so we use an empty one
                        civSideTwo = reportingPlayerfleet.FleetData.CivController;
                        sideTwoFleetCon = reportingPlayerfleet;
                    }

                    //have we met before? Do I know you?
                    if (!FoundADiplomacyController(civSideOne, civSideTwo))
                    { // First Contact
                        DiplomacyController newDiplomacyCon = InstantiateDiplomacyController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, otherCivSysCon);
                        if (!DiplomacyControllers.Contains(newDiplomacyCon))
                            DiplomacyControllers.Add(newDiplomacyCon);
                        IntelligenceManager.Instance.InitializeNewIntelligenceController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, otherCivSysCon);
                    }
                    else
                    { // not first contact
                        CheckForAIDiplomacy(sideOneFleetCon, otherCivSysCon);
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
                        // ToDo: Colonies Option/ UI?
                    }
                }
            }
            Destroy(fleetConEmpty.gameObject);
        }

        private void FeetsUninhabitedSysEncounter(FleetController reportingPlayerfleet, StarSysController uninhabitedSysCon)
        {
            var diplomacyData = EntereDiplomacyData(reportingPlayerfleet, uninhabitedSysCon); // not mono behavior
            diplomacyData.EncounterType = EncounterType.UninhabitedSystem;

            // Instantiate a DiplomacyController MonoBehaviour from prefab (or add component) so it's Unity-managed
            DiplomacyController diplomacyController = null;
            if (diplomacControllerPrefab != null)
            {
                GameObject dipGo = Instantiate(diplomacControllerPrefab, Vector3.zero, Quaternion.identity);
                dipGo.SetActive(true);
                dipGo.layer = 5;
                dipGo.transform.SetParent(this.transform, false);
                diplomacyController = dipGo.GetComponent<DiplomacyController>();
                if (diplomacyController == null)
                    diplomacyController = dipGo.AddComponent<DiplomacyController>();
            }
            else
            {
                GameObject dipGo = new GameObject("DiplomacyController");
                dipGo.transform.SetParent(this.transform, false);
                diplomacyController = dipGo.AddComponent<DiplomacyController>();
            }

            diplomacyController.DiplomacyData = diplomacyData;
            diplomacyController.ResolveUninhabitedSystem(reportingPlayerfleet.FleetData.CivController, uninhabitedSysCon);

            if (!DiplomacyControllers.Contains(diplomacyController))
                DiplomacyControllers.Add(diplomacyController);
            //DiplomacyControllers.Add(diplomacyController);
        }


        public void FeetToSysNotSameCivNotFirstEncounter(FleetController fleetA, StarSysController sysCon)
        {
            var diplomacyData = EntereDiplomacyData(fleetA, sysCon); // not mono behavior
            diplomacyData.EncounterType = EncounterType.Diplomacy;

            // Instantiate a DiplomacyController MonoBehaviour from prefab (or add component)
            DiplomacyController diplomacyController = null;
            if (diplomacControllerPrefab != null)
            {
                GameObject dipGo = Instantiate(diplomacControllerPrefab, Vector3.zero, Quaternion.identity);
                dipGo.SetActive(true);
                dipGo.layer = 5;
                dipGo.transform.SetParent(this.transform, false);
                diplomacyController = dipGo.GetComponent<DiplomacyController>();
                if (diplomacyController == null)
                    diplomacyController = dipGo.AddComponent<DiplomacyController>();
            }
            else
            {
                GameObject dipGo = new GameObject("DiplomacyController");
                dipGo.transform.SetParent(this.transform, false);
                diplomacyController = dipGo.AddComponent<DiplomacyController>();
            }

            diplomacyController.DiplomacyData = diplomacyData;
            if (!DiplomacyControllers.Contains(diplomacyController))
                DiplomacyControllers.Add(diplomacyController);
            //DiplomacyControllers.Add(diplomacyController);
            //GalaxyMenuUIController.Instance.OpenMenu(Menu.ADiplomacyMenu, diplomacyController.DiplomacyUIGameObject);
        }

        internal void ResolveDiplomacyForClickSystemWeKnow(CivController localPlayerCivContoller, StarSysController starSysController)
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
            }
            else
            {
                // no first contact just on clicking on the system
                // maybe some data if you are high tech level?
            }
        }

        internal void ResolveDiplomacyForClickFleetWeKnow(CivController localPlayerCivContoller, FleetController fleetController)
        {
            //already not one of our fleets
            CivController civPartyOne;
            CivController civPartyTwo;

            if ((int)localPlayerCivContoller.CivData.CivEnum < (int)fleetController.FleetData.CivController.CivData.CivEnum)
            {
                civPartyOne = localPlayerCivContoller;
                civPartyTwo = fleetController.FleetData.CivController;
            }
            else // other civ is side one
            {
                civPartyOne = fleetController.FleetData.CivController;
                civPartyTwo = localPlayerCivContoller;
            }
            //have we met before?
            if (DiplomacyManager.Instance.FoundADiplomacyController(civPartyOne, civPartyTwo))
            {   // not First Contact, just by clicking on the system
                DiplomacyManager.Instance.OpenDiplomacyUI(civPartyOne, civPartyTwo, fleetController.FleetData.ShipsList);
            }
            else
            {
                // no first contact just on clicking on the system
                // maybe some data if you are high tech level?
            }
        }
    

        private void OnDestroy()
        {
            ServiceLocator.Unregister<DiplomacyManager>();
        }
}
}