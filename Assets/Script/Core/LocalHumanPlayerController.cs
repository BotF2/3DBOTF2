using UnityEngine;
using Assets.Core;
using System.Collections.Generic;
using Mirror;

public class LocalHumanPlayerController : NetworkBehaviour, IPlayerController
{
    public PlayerData PlayerData { get; private set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool IsLocal => false;

    //public override void OnStartAuthority()
    //{
    //    base.OnStartAuthority();
    //    // Only the local player can issue commands
    //    Debug.Log("I have authority");
    //}
    public void GiveCombatOrder(CombatOrders order)
    {
        // Handle user UI input logic, reads Unity input.
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order)
    {
        // Implement logic for handling UI diplomacy orders.    
    }

    public void GiveIntelOrder(SecretActionsEnum order)
    {
        // Implement logic for handling UI intel orders.    
    }
}
