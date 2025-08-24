using UnityEngine;
using Assets.Core;
using System.Collections.Generic;

public class CivRelationsData
{

    public DiplomacyData DiplomacyData { get; set; }
    public IntelligenceData IntelData { get; set; }


    public CivRelationsData(CivEnum civA, CivEnum civB, FleetController fleetA, FleetController fleetB, StarSysController sysController)
    {
        DiplomacyData = new DiplomacyData(civA, civB);
        IntelData = new IntelligenceData(fleetA, fleetB, sysController);
    }

}
