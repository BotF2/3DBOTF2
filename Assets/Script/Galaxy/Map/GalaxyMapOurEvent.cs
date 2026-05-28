using System;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



public class GalaxyMapOurEvent : MonoBehaviour
{
    /// <summary>
    /// Not using this yet, Will see what we can/should use it for
    /// I think we need a c# Event (not Unity Event) because the Instance of FleetController is not there before code runs (FleetManager instantiates FleetControllers).
    /// If the FleetController is not yet there before runtime we cannot asign game objects in the inspector 
    /// </summary>
    public static GalaxyMapOurEvent current;
    public event Action<GameObject, int> onSetDestination; // a C# Event, not Unity Event and used by FleetController and set in FleetManager instatiate FleetController to have GalaxyMapOurEvent
    public event Action<GameObject, int> onRemoveDestination;

    private void Awake()
    {
        current = this;
    }


    public void DestinationSet(GameObject destination, int destinationInt)
    {
        onSetDestination?.Invoke(destination, destinationInt); // delegate action invocation with gameObject destination, (?)if registered (so is not null) then do it
    }

    public void RemoveDestination(GameObject destination, int destinationInt)
    {
        onRemoveDestination?.Invoke(destination, destinationInt);
    }
}
