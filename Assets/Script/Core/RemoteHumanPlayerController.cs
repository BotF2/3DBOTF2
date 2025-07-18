using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Core;
using Mirror;

public class RemoteHumanPlayerController : NetworkBehaviour, IPlayerController
{
    public PlayerData PlayerData { get; private set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool IsLocal => false;
    //Listens for synced remote input via networking
    public void GiveCombatOrder(CombatOrders order)
    {
        // Handle remote user UI input logic, reads Unity input.
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order)
    {
        // Implement logic for handling remote UI diplomacy orders.
    }

    public void GiveIntelOrder(SecretActionsEnum order)
    {
        // Implement logic for handling remote UI intel orders.
    }
}
