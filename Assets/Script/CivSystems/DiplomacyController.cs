using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections.Generic;
using UnityEngine;

public enum EncounterType
{
    FirstContact,
    Diplomacy, // civ to civ and civs can be local player or AI
    Combat,  //? is this a subtype of Diplomacy as seen by Diplomacy
    FleetManagement, // thinking we can do this back in the fleetController
    EnterSystem,
    UninhabitedSystem,
    StrangeGalacticObject,
}

namespace BOTF3D.GamePlay
{
    public class DiplomacyController : MonoBehaviour
    {
        private DiplomacyData diplomacyData; // holds civOne and two, diplomacy enum...
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
        public GameObject DiplomacyUIGameObject;

        // MonoBehaviour should not rely on parameterized constructors. Use Init(...) after AddComponent/Instantiate.
        public void Init(DiplomacyData data)
        {
            DiplomacyData = data;
        }

        public void DoAIDiplomacy()
        {
            if (GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivEnumSideOne) || GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivEnumSideTwo))
            {
                if (DiplomacyUIGameObject != null)
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
            else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Membership && ((int)this.DiplomacyData.CivEnumSideOne > 6 || (int)this.DiplomacyData.CivEnumSideTwo > 6))
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
            // ToDo: include orbital batteries and shields in combat, see ValidCombatCheck()
            if (diplomacyController.DiplomacyData.CombatIntiated != true && ValidCombatCheck(diplomacyController.DiplomacyData))
            {

                diplomacyController.DiplomacyData.CombatIntiated = true;

                GalaxyMenuUIController.Instance.CloseMenu(Menu.DiplomacyMenu);

                SceneController.Instance.LoadCombatScene(
                    diplomacyController.DiplomacyData.FleetControllerCivOne,
                    diplomacyController.DiplomacyData.FleetContollerCivTwo,
                    diplomacyController.DiplomacyData.StarSysController
                );
            }
            //*******load combat menu for local player and do AI civs
        }

        private bool ValidCombatCheck(DiplomacyData diplomacyData)
        {
            bool _result = false;
            if ((diplomacyData.FleetControllerCivOne != null && diplomacyData.FleetControllerCivOne.FleetData.ShipsList.Count > 0) &&
                (diplomacyData.FleetContollerCivTwo != null && diplomacyData.FleetContollerCivTwo.FleetData.ShipsList.Count > 0))
                _result = true;
            if ((diplomacyData.FleetControllerCivOne != null && diplomacyData.FleetControllerCivOne.FleetData.ShipsList.Count > 0) &&
                (diplomacyData.StarSysController != null && diplomacyData.StarSysController.StarSysData.ShipsList.Count > 0))
                _result = true;
            if ((diplomacyData.FleetContollerCivTwo != null && diplomacyData.FleetContollerCivTwo.FleetData.ShipsList.Count > 0) &&
                (diplomacyData.StarSysController != null && diplomacyData.StarSysController.StarSysData.ShipsList.Count > 0))
                _result = true;

            return _result;
        }

        internal void ResolveFleetToStrangGalacticEncounter(DiplomacyController diplomacyController)
        {
            GalaxyMenuUIController.Instance.OpenMenu(Menu.ADiplomacyMenu, this.DiplomacyUIGameObject);
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
        public void CleanupDestroyedUIs()
        {
            foreach (var diplomacyCon in DiplomacyManager.Instance.DiplomacyControllers)
            {
                if (diplomacyCon.DiplomacyUIGameObject == null)
                    continue;

                if (!diplomacyCon.DiplomacyUIGameObject.activeInHierarchy)
                {
                    diplomacyCon.DiplomacyUIGameObject = null;
                }
            }
        }
    }
}
