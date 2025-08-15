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
    { // **** need to set local player civ for this code

        var combatCons = CombatManager.Instance.CombatControllers;
        CombatController aCombatCon = CombatManager.Instance.CombatControllers[0];
        for (int i = 0; i < combatCons.Count; i++) 
        {
            if (combatCon == combatCons[i] & (combatCon.CombatData.CivEnumSideOne == civ || combatCon.CombatData.CivEnumSideTwo == civ))
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
}
