using Assets.Core;
using UnityEngine;

public class EncounterData
{
    public CivController CivOne;
    public CivController CivTwo;
    public FleetController FleetControllerCivOne;
    public FleetController FleetContollerCivTwo;
    public StarSysController StarSysController;
    public EncounterType EncounterType;
    public bool isCompleted = false;
    public bool firstContact = false;
}
