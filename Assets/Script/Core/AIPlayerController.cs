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
    public bool controllerIsLocalPlayer => false;
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
