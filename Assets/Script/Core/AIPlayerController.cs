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
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        // Register with the PlayerManager
        PlayerManager.Instance.AddLocalPlayer(new PlayerData { name = PlayerData.PlayerName });
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
        //runs on the server
        Debug.Log($"[Server] Received order: {order}");
        RpcHandleOrder(order);
    }

    [ClientRpc]
    void RpcHandleOrder(string order)
    {
        Debug.Log($"[All Clients] Order executed: {order}");
        // Actual combat logic here
    }
    public void GiveCombatOrder(CombatOrders order, CombatController combatCon, CivEnum civ)
    {
        var combatCons = CombatManager.Instance.CombatControllers;
        CombatController aCombatCon = CombatManager.Instance.CombatControllers[0];
        for (int i = 0; i < combatCons.Count; i++)
        {
            if ( combatCons[i] == combatCon && (combatCon.CombatData.CivEnumSideOne == civ || combatCon.CombatData.CivEnumSideTwo == civ))
                aCombatCon = combatCons[i];
            break;
        }
        switch (order)
        {
            case CombatOrders.Engage:
                aCombatCon.SetCombatOrder(CombatOrders.Engage, PlayerCiv);
                break;
            case CombatOrders.Rush:
                aCombatCon.SetCombatOrder(CombatOrders.Rush, PlayerCiv);
                break;
            case CombatOrders.Retreat:
                aCombatCon.SetCombatOrder(CombatOrders.Retreat, PlayerCiv);
                break;
            case CombatOrders.Formation:
                aCombatCon.SetCombatOrder(CombatOrders.Formation, PlayerCiv);
                break;
            case CombatOrders.TargetTransports:
                aCombatCon.SetCombatOrder(CombatOrders.TargetTransports, PlayerCiv);

                break;
        }
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diploCon, CivEnum civ)
    {
        // Handle AI logic for diplomacy orders.
    }

    public void GiveIntelOrder(SecretActionsEnum order, CivEnum civ)
    {
      
        //***? will we have an IntelController like the combat controller?
        //var combatCons = CombatManager.Instance.CombatControllers;
        //CombatController aCombatCon = CombatManager.Instance.CombatControllers[0];
        //for (int i = 0; i < combatCons.Count; i++)
        //{
        //    if (combatCon.CombatData.CivEnumSideOne == civ || combatCon.CombatData.CivEnumSideTwo == civ)
        //        aCombatCon = combatCons[i];
        //    break;
        //}
        //switch (order)
        //{
        //    case SecretActionsEnum.Disinformation:
        //        aCombatCon.SetCombatOrder(CombatOrders.Engage, PlayerCiv);
        //        break;
        //    case SecretActionsEnum.Combat:
        //        aCombatCon.SetCombatOrder(CombatOrders.Rush, PlayerCiv);
        //        break;
        //    case SecretActionsEnum.IntellectualTheft:
        //        aCombatCon.SetCombatOrder(CombatOrders.Retreat, PlayerCiv);
        //        break;
        //    case SecretActionsEnum.Sabotage:
        //        aCombatCon.SetCombatOrder(CombatOrders.Formation, PlayerCiv);
        //        break;
        //    case SecretActionsEnum.GatherIntelligence:
        //        aCombatCon.SetCombatOrder(CombatOrders.TargetTransports, PlayerCiv);

        //        break;
        //}
    }

    internal void GetAICombatOrder(SecretActionsEnum order, CombatController combatCon, CivEnum civ)
    {
        // do AI combat logic here, Handle AI logic, computes behavior logic.

    }
    //.....???? more orders as needed
}
