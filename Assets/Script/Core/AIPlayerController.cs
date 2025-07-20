using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Core;
using Mirror;
using System;

public class AiPlayerController : NetworkBehaviour, IPlayerController
{
    public PlayerData PlayerData { get; set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool controllerIsLocalPlayer => false;
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
      
        //CombatController..SetCombatOrder(order, PlayerData.CivEnum);
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order)
    {
        // Handle AI logic for diplomacy orders.
    }

    public void GiveIntelOrder(SecretActionsEnum order)
    {
        // Handle AI logic for intel orders.
    }

    internal void GetAICombatOrder(CombatData order)
    {
        // do AI combat logic here, Handle AI logic, computes behavior logic.
        GiveCombatOrder(CombatOrders.Engage); // Example order, replace with actual AI logic
    }
    //.....???? more orders as needed
}
