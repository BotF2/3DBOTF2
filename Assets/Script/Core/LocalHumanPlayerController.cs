using Assets.Core;
using Assets.GamePlay;
using Mirror;
using UnityEngine;

public class LocalHumanPlayerController : NetworkBehaviour, IPlayerController
{
    public GamePlayerInfo PlayerInfo { get; set; }
    public static LocalHumanPlayerController localInstance { get; private set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool controllerIsLocalPlayer => true; // do we need this with Mirror in place?

    //string IPlayerController.PlayerName => ((IPlayerController)localInstance).PlayerName;

    bool hasAuthority;
    [SyncVar] public string playerName = "Local Player";
    public string PlayerName => playerName;

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        // Only the local player can issue commands
        Debug.Log("I have authority");
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        PlayerManager.Instance.RegisterPlayer(this, false, PlayerName, netId.GetHashCode(), PlayerType.Local);
    }
    public override void OnStopServer()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UnregisterPlayer(netId.GetHashCode());
        base.OnStopServer();
    }
    public override void OnStartLocalPlayer()
    {
        if (localInstance != null && localInstance != this)
        {
            Debug.LogError("Multiple LocalHumanPlayerController instances detected. There should only be one per client.");
            return;
        }
        PlayerInfo = new GamePlayerInfo("local Human");
        PlayerInfo.PlayerId = 0;
        PlayerInfo.PlayerType = PlayerType.Local;
        base.OnStartLocalPlayer();
        // Register with the PlayerManager
        PlayerManager.Instance.RegisterPlayer(this, true, PlayerName, netId.GetHashCode(), PlayerType.Local);
        localInstance = this;
    }
    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UnregisterPlayer(netId.GetHashCode());
    }
    public override void OnStopClient()
    {
        PlayerManager.Instance.UnregisterPlayer(netId.GetHashCode());
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
        PlayerInfo.PlayerCiv = civEnum;
    }
}
