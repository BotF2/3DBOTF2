
using System.Collections.Generic;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Civilization
{
    public class DiplomacyData
    {
        // Pending multi-turn proposals between this civ pair (see DiplomacyProject /
        // DiplomacyManager.CreateDiplomacyProject) - the Diplomacy-side counterpart to
        // IntelligenceData.ActiveProjects.
        public List<DiplomacyProject> ActiveProjects = new List<DiplomacyProject>();
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

        // Human-only convenience toggle (off by default, opted into per civ pair from the Diplomacy
        // card): when true, DiplomacyManager.ProcessAutoImproveRelations automatically sends a new
        // goodwill proposal to this civ every turn a slot is free, using the same tiered logic AI
        // civs already use in ProcessAIDiplomacyForAllCivs, instead of the player having to click a
        // gesture button by hand each time the previous proposal resolves.
        public bool AutoImproveRelations = false;

        // Result message for the most recently resolved proposal (see
        // DiplomacyManager.ResolveDiplomacyProject) - shown in ActiveProposalText once
        // ActiveProjects is empty again, so the display reports "Alliance accepted"/"Trade
        // rejected" instead of silently reverting to "No active proposal" with no explanation.
        // Cleared when a new proposal is sent (see DiplomacyController.SendProposal).
        public string LastProposalOutcome;

        // Per-encounter Fight/Withdraw capture (NOT a persisted standing order - reset with each new
        // encounter, unlike DiplomacyStatusEnumOfCivs which is a long-lived civ-pair relationship).
        // Either side choosing Fight forces combat; both choosing Withdraw releases both fleets to
        // continue their prior movement unimpeded. See DiplomacyController.TryResolveEncounter.
        public enum EncounterResponse { Undecided, Fight, Withdraw }
        public EncounterResponse ResponseSideOne = EncounterResponse.Undecided;
        public EncounterResponse ResponseSideTwo = EncounterResponse.Undecided;

        // Guards DiplomacyController.TryResolveEncounter's action side-effects (forcing combat or
        // releasing both fleets) against firing more than once for the same encounter if the
        // resolving network broadcast is received more than once. Reset alongside the responses
        // above whenever a fresh encounter decision is opened (see DiplomacyManager.OpenDiplomacyUI).
        public bool EncounterResolved = false;

        // Server-side wall-clock stamp of when the current Fight/Withdraw decision became active -
        // reset alongside the responses above (see DiplomacyManager.OpenDiplomacyUI and
        // InstantiateDiplomacyController). Bookkeeping only - resolution is now instant and
        // action-driven (see DiplomacyController.ServerForceOtherSideIfStillUndecided and
        // ServerImplicitlyWithdrawFleet), not timer-based, so nothing currently reads this back.
        public float EncounterStartRealTime;

        public DiplomacyData() { }
        public DiplomacyData(CivEnum civOne, CivEnum civTwo) //, StarSysController starSysController)
        {
            this.CivEnumSideOne = civOne;
            this.CivEnumSideTwo = civTwo;
            // this.CurrentStarSysController = starSysController;
        }
    }
}
