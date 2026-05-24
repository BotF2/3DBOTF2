using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.GamePlay;
using Mirror;
using UnityEngine;

public class RemoteHumanPlayerController : NetworkBehaviour, IPlayerController
{
    public GamePlayerInfo PlayerInfo { get; set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool controllerIsLocalPlayer => false;
    bool hasAuthority;
    //private static int remoteHumanNumber = 1; // Static counter to differentiate Remote Human players
    [SyncVar] public string playerName = "Remote Player";
    public string PlayerName => playerName;

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
    [Command]
    void CmdSendOrder(string order)
    {
        // Runs on the server
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
    public void ExecuteOrder(string order)
    {
        if (hasAuthority)
        {
            CmdSendOrder(order);
        }
    }
    public void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diploCon, CivEnum civ)
    {
        // Implement logic for handling remote UI diplomacy orders.
    }

    public void GiveIntelOrder(SecretActionsEnum order, CivEnum civ)
    {
        // Implement logic for handling remote UI intel orders.
    }

    internal void SuggestCombatOrder(CombatData combatData)
    {
        // Implement AI combat logic to evaluate the situation and suggest an appropriate order.
        // This to involve analyzing combatData and making decisions based on various factors.
        //GiveCombatOrder(CombatOrders.Engage); // Example order, replace with actual AI logic
    }
}
