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
    public bool controllerIsLocalPlayer => false;
    //Listens for synced remote input via networking

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        // Only the local player can issue commands
        Debug.Log("I have authority");
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
