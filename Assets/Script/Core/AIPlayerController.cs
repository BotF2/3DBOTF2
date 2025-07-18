using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Core;
using Mirror;

public class AIPlayerController : NetworkBehaviour, IPlayerController
{
    public PlayerData PlayerData { get; private set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool IsLocal => false;

    public void GiveCombatOrder(CombatOrders order)
    {
        // Handle AI logic, computes behavior logic.
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order)
    {
        // Handle AI logic for diplomacy orders.
    }

    public void GiveIntelOrder(SecretActionsEnum order)
    {
        // Handle AI logic for intel orders.
    }
    //.....???? more orders as needed
}
