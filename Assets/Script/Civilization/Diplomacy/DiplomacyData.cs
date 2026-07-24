
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Civilization
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

        // Minor-major "join the Federation" trust process: once true, DiplomacyPointsOfCivs drifts
        // upward every turn (see DiplomacyController.TickCooperationPactDrift) instead of only moving
        // via one-off player/AI gestures, heading toward Membership (full annexation).
        public bool CooperationPactActive = false;

        public DiplomacyData() { }
        public DiplomacyData(CivEnum civOne, CivEnum civTwo) //, StarSysController starSysController)
        {
            this.CivEnumSideOne = civOne;
            this.CivEnumSideTwo = civTwo;
            // this.CurrentStarSysController = starSysController;
        }
    }
}
