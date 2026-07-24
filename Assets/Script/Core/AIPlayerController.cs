using BOTF3D.Combat;
using BOTF3D.Core;

using Mirror;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;




public class AiPlayerController : NetworkBehaviour, IPlayerController
{
    public GamePlayerInfo PlayerInfo { get; set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool controllerIsLocalPlayer => false;
    //private static int aiNumber = 1; // Static counter to differentiate AI players
    [SyncVar] public string playerName = "Ai Player";
    public string PlayerName => playerName;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (PlayerManager.Instance != null)
            PlayerCiv = PlayerManager.Instance.RegisterPlayer(this, false, PlayerName, netId.GetHashCode(), PlayerType.AI);

    }
    public override void OnStopServer()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UnregisterPlayer(netId.GetHashCode());
        base.OnStopServer();
    }
    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UnregisterPlayer(netId.GetHashCode());
    }
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        // Only the local player can issue commands
        Debug.Log("I have authority");
    }
    public override void OnStartLocalPlayer()
    {
        //base.OnStartLocalPlayer();
        //// Register with the PlayerManager
        //PlayerManager.Instance.AddLocalPlayer(new PlayerData { name = PlayerData.PlayerName });
    }
    // AI decisions are made on the server (there's no owning client to command), so these
    // just broadcast the server's decision out to every client via ClientRpc.
    public void SubmitCombatOrder(CombatOrders order, CivEnum actingCiv, CivEnum opposingCiv)
    {
        if (isServer)
            RpcApplyCombatOrder(order, actingCiv, opposingCiv);
    }

    public void SubmitDiplomacyOrder(NegotiationPloysEnum order, CivEnum actingCiv, CivEnum opposingCiv)
    {
        if (isServer)
            RpcApplyDiplomacyOrder(order, actingCiv, opposingCiv);
    }

    public void SubmitIntelOrder(SecretActionsEnum order, CivEnum actingCiv, CivEnum targetCiv)
    {
        if (isServer)
            RpcApplyIntelOrder(order, actingCiv, targetCiv);
    }

    [ClientRpc]
    void RpcApplyCombatOrder(CombatOrders order, CivEnum actingCiv, CivEnum opposingCiv)
    {
        CombatController combatCon = CombatManager.Instance?.GetActiveCombatControllerForCivs(actingCiv, opposingCiv);
        if (combatCon == null)
        {
            Debug.LogWarning($"RpcApplyCombatOrder: no active combat found between {actingCiv} and {opposingCiv}");
            return;
        }
        GiveCombatOrder(order, combatCon, actingCiv);
    }

    [ClientRpc]
    void RpcApplyDiplomacyOrder(NegotiationPloysEnum order, CivEnum actingCiv, CivEnum opposingCiv)
    {
        DiplomacyController diploCon = DiplomacyManager.Instance?.ReturnADiplomacyController(actingCiv, opposingCiv);
        if (diploCon == null)
        {
            Debug.LogWarning($"RpcApplyDiplomacyOrder: no diplomacy record between {actingCiv} and {opposingCiv}");
            return;
        }
        GiveDiplomacyOrder(order, diploCon, actingCiv);
    }

    [ClientRpc]
    void RpcApplyIntelOrder(SecretActionsEnum order, CivEnum actingCiv, CivEnum targetCiv)
    {
        GiveIntelOrder(order, actingCiv, targetCiv);
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
                // run AI combat code to decide on new order based on data from combatcontroller
                combatCon.SetShipOrders(CombatOrders.Engage, civ); //PlayerCiv);
                break;
            case CombatOrders.Rush:
                combatCon.SetShipOrders(CombatOrders.Rush, civ);//PlayerCiv);
                break;
            case CombatOrders.Retreat:
                combatCon.SetShipOrders(CombatOrders.Retreat, civ); // PlayerCiv);
                break;
            case CombatOrders.Formation:
                combatCon.SetShipOrders(CombatOrders.Formation, civ); // PlayerCiv);
                break;
            case CombatOrders.AttackTransports:
                combatCon.SetShipOrders(CombatOrders.AttackTransports, civ); // PlayerCiv);

                break;
        }
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diploCon, CivEnum civ)
    {
        // No-op: NegotiationPloysEnum has no consumer anywhere in the codebase yet (diplomacy
        // negotiation logic hasn't been built for single-player either). The RPC above already
        // delivers this order to every client correctly; wire in real AI diplomacy logic here
        // once that system exists.
    }

    public void GiveIntelOrder(SecretActionsEnum order, CivEnum actingCiv, CivEnum targetCiv)
    {
        IntelligenceManager.Instance?.CreateIntelProject(order, actingCiv, targetCiv, out _);
    }

    internal void GetAICombatOrder(SecretActionsEnum order, CombatController combatCon, CivEnum civ)
    {
        // do AI combat logic here, Handle AI logic, computes behavior logic.

    }
    //.....???? more orders as needed
}

