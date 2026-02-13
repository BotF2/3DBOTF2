using Assets.GamePlay;

namespace Assets.Core
{
    public class DiplomacyData
    {
        public CivEnum CivEnumSideOne; // a major civ and the local player if present
        public int SideOneMultiplayerId; // network player ID, not used in single player
        public CivEnum CivEnumSideTwo; // a minor civ, if any
        public int SideTwoMultiplayerId; // network player ID, not used in single player
        public DiplomacyStatusEnum DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Neutral; // the diplomacy status for this civ pair
        public int DiplomacyPointsOfCivs = 60; // neutral
        public bool CombatIntiated = false; // true if combat has been initiated between these civs
        public CivController CivOne;
        public CivController CivTwo;
        public FleetController FleetControllerCivOne;
        public FleetController FleetContollerCivTwo;
        public StarSysController StarSysController;
        public EncounterType EncounterType;
        public bool firstContact = false;

        public DiplomacyData() { }
        public DiplomacyData(CivEnum civOne, CivEnum civTwo) //, StarSysController starSysController)
        {
            this.CivEnumSideOne = civOne;
            this.CivEnumSideTwo = civTwo;
            // this.CurrentStarSysController = starSysController;
        }
    }
}
