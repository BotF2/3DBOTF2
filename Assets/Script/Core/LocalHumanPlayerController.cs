using UnityEngine;
using Assets.Core;
using System.Collections.Generic;
using Mirror;

public class LocalHumanPlayerController : NetworkBehaviour, IPlayerController
{
    public PlayerData PlayerData { get; set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool controllerIsLocalPlayer => false; // do we need this with Mirror in place?
    bool hasAuthority;

    void Update()
    {
        // Handle local player input here
        if (!isLocalPlayer) // network local player, not bool controllerIsLocalPlayer
        {
            return; // Skip if not the local player
        }
        else
        {
            ExecuteOrder("Engage"); // Example order, replace with actual input handling
            GiveCombatOrder(CombatOrders.Engage);
        }
    }
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
}
