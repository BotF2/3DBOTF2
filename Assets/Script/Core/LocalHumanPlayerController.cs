using UnityEngine;
using Assets.Core;
using System.Collections.Generic;
using Mirror;
using System;

public class LocalHumanPlayerController : NetworkBehaviour, IPlayerController
{
    public PlayerData PlayerData { get; set; }
    public static LocalHumanPlayerController localInstance { get; private set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool controllerIsLocalPlayer => true; // do we need this with Mirror in place?
    bool hasAuthority;

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        // Only the local player can issue commands
        Debug.Log("I have authority");
    }
    public override void OnStartLocalPlayer()
    {
        PlayerData = new PlayerData("local Human");
        PlayerData.PlayerId = 0;
        PlayerData.PlayerType = PlayerType.Local;
        base.OnStartLocalPlayer();
        // Register with the PlayerManager
        PlayerManager.Instance.RegisterPlayer(this, true);
        localInstance = this;
    }
    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UnregisterPlayer(this);
    }
    public override void OnStopClient()
    {
        PlayerManager.Instance.UnregisterPlayer(this);
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
        // runs on the server
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
        //var combatCons = CombatManager.Instance.CombatControllers;
        //CombatController aCombatCon = CombatManager.Instance.CombatControllers[0];
        //for (int i = 0; i < combatCons.Count; i++) 
        //{
        //    if (combatCon == combatCons[i] & (combatCon.CombatData.CivEnumSideOne == civ || combatCon.CombatData.CivEnumSideTwo == civ))
        //        aCombatCon = combatCons[i];
        //    break;
        //}
        switch (order)
        {
        case CombatOrders.Engage:
                combatCon.SetCombatOrder(CombatOrders.Engage, civ); //PlayerCiv);
            break;
        case CombatOrders.Rush:
                combatCon.SetCombatOrder(CombatOrders.Rush, civ);//PlayerCiv);
            break;
        case CombatOrders.Retreat:
                combatCon.SetCombatOrder(CombatOrders.Retreat, civ); // PlayerCiv);
            break;
        case CombatOrders.Formation:
                combatCon.SetCombatOrder(CombatOrders.Formation, civ); // PlayerCiv);
            break;
        case CombatOrders.TargetTransports:
            combatCon.SetCombatOrder(CombatOrders.TargetTransports, civ); // PlayerCiv);

            break;
        }
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diplomacyCon, CivEnum civ)
    {
        // Implement logic for handling UI diplomacy orders.    
    }

    public void GiveIntelOrder(SecretActionsEnum order, CivEnum civ)
    {
        // Implement logic for handling UI intel orders.    
    }
    internal void SuggestCombatOrder(CombatData combatData)
    {
        // Implement AI combat logic to evaluate the situation and suggest an appropriate order.
        // This  involve analyzing combatData and making decisions based on various factors.
        //GiveCombatOrder(CombatOrders.Engage); // Example order, replace with actual AI logic
    }

    internal void ResetLocalHumanPlayerCon(CivEnum localPlayerCiv)
    {
        //this.PlayerCiv = localPlayerCiv;
        //PlayerData.PlayerId = 0;
        //PlayerData.PlayerName = "Local Player";
        //hasAuthority = true;
    }

    internal void SetCiv(CivEnum civEnum)
    {
        PlayerData.PlayerCiv = civEnum;
    }
}
