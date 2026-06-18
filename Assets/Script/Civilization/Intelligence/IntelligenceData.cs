
using System.Collections.Generic;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Civilization
{
    public class IntelligenceData
    {
        public CivEnum CivSideOne { get; internal set; }
        public CivEnum CivSideTwo { get; internal set; }
        public object IntelligenceStatusEnumOfCivs { get; internal set; }

        public List<IntelProject> ActiveProjects = new List<IntelProject>();

        public FleetController LastSeenFleetOfSideOne; // the major civ's fleet, know this for combat ships in the fleet
        public FleetController LastSeenFleetOfSideTwo; // the minor civ's fleet, if any 
        public StarSysController LastSeenStarSysController; // a star system controller with ships from where the encounter took place

        public IntelligenceData(FleetController lastSeenFleetOfSideOne, FleetController lastSeenFleetOfSideTwo, StarSysController lastSeenStarSysController)
        {
            LastSeenFleetOfSideOne = lastSeenFleetOfSideOne;
            LastSeenFleetOfSideTwo = lastSeenFleetOfSideTwo;
            LastSeenStarSysController = lastSeenStarSysController;
        }

        public IntelligenceData(CivEnum civA, CivEnum civB)
        {
            this.CivSideOne = civA;
            this.CivSideTwo = civB;
        }
    }
}
