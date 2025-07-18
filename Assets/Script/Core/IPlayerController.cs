using UnityEngine;
using Assets.Core;
using System.Collections.Generic;

public interface IPlayerController
{
    PlayerData PlayerData { get; }

    // How many of these do we need?
    void GiveCombatOrder(CombatOrders order);
    void GiveDiplomacyOrder(NegotiationPloysEnum order);
    void GiveIntelOrder(SecretActionsEnum order);
    //......more orders as needed

    // or [ServerRpc] ??????????
    //public void SubmitOrdersServerRpc(Orders orders)
    //{
    //    Apply to game state, broadcast results
    //}
    CivEnum PlayerCiv { get; }
    bool IsLocal { get; }
}
