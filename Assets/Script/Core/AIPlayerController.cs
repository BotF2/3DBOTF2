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
    bool hasAuthority;
    //private static int aiNumber = 1; // Static counter to differentiate AI players
    [SyncVar] public string playerName = "Ai Player";
    public string PlayerName => playerName;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.RegisterPlayer(this, false, PlayerName, netId.GetHashCode(), PlayerType.AI);

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
        //        aCombatCon.SetCombatOrder(CombatOrders.AttackTransports, PlayerCiv);

        //        break;
        //}
    }

    internal void GetAICombatOrder(SecretActionsEnum order, CombatController combatCon, CivEnum civ)
    {
        // do AI combat logic here, Handle AI logic, computes behavior logic.

    }
    //.....???? more orders as needed
}

