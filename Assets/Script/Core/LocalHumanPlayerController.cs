using UnityEngine;
using Assets.Core;
using System.Collections.Generic;
using Mirror;

public class LocalHumanPlayerController : NetworkBehaviour, IPlayerController
{
    public PlayerData PlayerData { get; set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool controllerIsLocalPlayer => true; // do we need this with Mirror in place?
    bool hasAuthority;


    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        // Only the local player can issue commands
        Debug.Log("I have authority");
    }

    public void ExecuteOrder(string order)
    {
        if (hasAuthority)
        {
            CmdSendOrder(order);
        }
    }

    [Command]
    void CmdSendOrder(string order)
    {
        Debug.Log($"[Server] Received order: {order}");
        RpcHandleOrder(order);
    }

    [ClientRpc]
    void RpcHandleOrder(string order)
    {
        Debug.Log($"[All Clients] Order executed: {order}");
        // Actual combat logic here
    }
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
    internal void SuggestCombatOrder(CombatData combatData)
    {
        // Implement AI combat logic to evaluate the situation and suggest an appropriate order.
        // This  involve analyzing combatData and making decisions based on various factors.
        GiveCombatOrder(CombatOrders.Engage); // Example order, replace with actual AI logic
    }
}
