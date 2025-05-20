using Assets.Core;
using System.Collections.Generic;
using UnityEngine;

public class EncounterController
{
    private EncounterData encounterData;
    public EncounterData EncounterData { get { return encounterData; } set { encounterData = value; } }

    public EncounterController(EncounterData encounterData)
    {
        EncounterData = encounterData;
    }
    public void ResolveFirstContact(EncounterController thisEncounterController)
    {
        //GalaxyMenuUIController.Instance.OpenMenu(Menu.DiplomacyMenu, null);

    }
    public void ResolveEncounter(CivController controllerA, CivController controllerB)
    {  // Do all this in?
        //if (DiplomacyManager.Instance.FoundADiplomacyController(controllerA, controllerB))
        //{
        //    //GalaxyMenuUIController.Instance.OpenMenu(Menu.ADiplomacyMenu, null);
        //    //DiplomacyManager.Instance.ReturnADiplomacyController(controllerA, controllerB).FirstContact(controllerA, controllerB);
        //}
        EncounterData.isCompleted = true;
        // destroy the encounter controller
    }
    public void ResolveCombat()
    {
        //Stop Time
        // call this from DiplomacyControllers, pass the encounter back, find it and load the combat scene
        EncounterData.isCompleted = true;
        // destroy the encounter controller
    }
    public void ResolveFleetToStrangGalacticEncounter(EncounterController strangeEncounter)
    {
        // ToDo: Resolve the encounter with the strange galactic object
    }
    public void ResolveFleetEncounter()
    {
        // Sending this to AI / EncounterManager
        EncounterData.isCompleted = true;
    }
    public void ResolveEnterSystem()
    {
        //Debug.Log($"Espionage operation between {encounterData.FactionA.Name} and {encounterData.FactionB.Name}");
        // Modify faction intelligence levels
        EncounterData.isCompleted = true;
        // destroy the encounter controller
    }
    public void ResolveTrade()
    {
        //Debug.Log($"Trade established between {encounterData.FactionA.Name} and {encounterData.FactionB.Name}");
        // Modify resources
        EncounterData.isCompleted = true;
        // destroy the encounter controller
    }
    public void ResolveDisinformation()
    {
        //Debug.Log($"Espionage operation between {encounterData.FactionA.Name} and {encounterData.FactionB.Name}");
        // Modify faction intelligence levels
        EncounterData.isCompleted = true;
        // destroy the encounter controller
    }
    public void ResolveSabatoge()
    {
        //Debug.Log($"Espionage operation between {encounterData.FactionA.Name} and {encounterData.FactionB.Name}");
        // Modify faction intelligence levels
        EncounterData.isCompleted = true;
        // destroy the encounter controller
    }
    public void ResolveEspionage()
    {
        //Debug.Log($"Espionage operation between {encounterData.FactionA.Name} and {encounterData.FactionB.Name}");
        // Modify faction intelligence levels
        EncounterData.isCompleted = true;
        // destroy the encounter controller
    }
    public void ResolveUninhabitedSystem(CivController realCivController, StarSysController sysCon)
    {
        // UI for uninhabited system management
        if (GameController.Instance.AreWeLocalPlayer(realCivController.CivData.CivEnum))
            sysCon.DoHabitalbeSystemUI(realCivController);
        else
        {
            // do AI uninhabited system management
        }
        EncounterData.isCompleted = true;
        // destroy the encounter controller
    }
}
