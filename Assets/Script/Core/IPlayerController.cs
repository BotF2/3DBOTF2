using UnityEngine;
using Assets.Core;
using System.Collections.Generic;

public interface IPlayerController
{
    PlayerData PlayerData { get; set; }
    CivEnum PlayerCiv { get; }
    bool controllerIsLocalPlayer { get; }

    void GiveCombatOrder(CombatOrders order, CombatController combatCon, CivEnum civ);
    void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diploCon, CivEnum civ);
    void GiveIntelOrder(SecretActionsEnum order, CivEnum civ);
    //......more orders as needed

    // or [ServerRpc] ??
    //public void SubmitOrdersServerRpc(Orders orders)
    //{
    //    // Apply to game state, broadcast results
    //}

}
