using BOTF3D.Combat;
using BOTF3D.Core;

using Mirror;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



// This is the actual prefab NetworkManager.playerPrefab spawns for every connecting human
// connection (host's own player object included) - see Assets/PreFabs/NetworkPlayer/PlayerPrefab.prefab.
// isOwned distinguishes "this is my own player object" from "someone else's", same pattern as AiPlayerController.
public class LocalHumanPlayerController : NetworkBehaviour, IPlayerController
{
    public GamePlayerInfo PlayerInfo { get; set; }
    public CivEnum PlayerCiv => playerCiv;
    public bool controllerIsLocalPlayer => isOwned;
    [SyncVar] public string playerName = "Player";
    [SyncVar(hook = nameof(OnPlayerCivChanged))] private CivEnum playerCiv = CivEnum.FED;
    public string PlayerName => playerName;

    // Fires on every client whenever the server-authoritative playerCiv changes (including the
    // initial sync on join). GameData is per-client/local (not networked), so only the owning
    // client's civ pick should ever update it - that's what GameController.GameData.LocalPlayerCivEnum
    // is read from when the galaxy actually loads (see MainMenuUIController.OnNewGameButtonClicked call).
    private void OnPlayerCivChanged(CivEnum oldCiv, CivEnum newCiv)
    {
        if (!isOwned)
            return;
        if (GameController.Instance != null && GameController.Instance.GameData != null)
            GameController.Instance.GameData.LocalPlayerCivEnum = newCiv;
        if (CombatUIManager.Instance != null)
            CombatUIManager.Instance.CivEnumLocalPlayer = newCiv;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (PlayerManager.Instance != null)
        {
            playerCiv = PlayerManager.Instance.RegisterPlayer(this, false, playerName, netId.GetHashCode(), PlayerType.Local);
            PlayerManager.Instance.ClaimHostAuthorityIfUnclaimed(netId.GetHashCode());
        }
    }
    public override void OnStopServer()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UnregisterPlayer(netId.GetHashCode());
            PlayerManager.Instance.ReleaseHostAuthorityIfHeldBy(netId.GetHashCode());
        }
        base.OnStopServer();
    }
    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UnregisterPlayer(netId.GetHashCode());
            if (isOwned && PlayerManager.Instance.LocalPlayerController == this)
                PlayerManager.Instance.SetLocalPlayerController(null);
        }
    }
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        // Only the local player can issue commands
        Debug.Log("I have authority");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[RosterDiag] OnStartClient netId={netId} isOwned={isOwned}");
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log($"[RosterDiag] OnStartLocalPlayer netId={netId} PlayerManager.Instance={(PlayerManager.Instance != null ? "OK" : "NULL")} MainMenuUIController.Instance={(MainMenuUIController.Instance != null ? "OK" : "NULL")}");
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.SetLocalPlayerController(this);
        if (MainMenuUIController.Instance != null)
            MainMenuUIController.Instance.OnLocalPlayerReady(this);
    }

    // Entry points called locally by this player's own client (e.g. from the multiplayer lobby UI,
    // or combat/diplomacy/intel UI once that's wired to route through IPlayerController).
    public void SubmitPlayerName(string requestedName)
    {
        if (isOwned)
            CmdSetPlayerName(requestedName);
    }

    public void SubmitPlayerCiv(CivEnum civ)
    {
        if (isOwned)
            CmdSetPlayerCiv(civ);
    }

    public void SubmitCreateSplitFleet(FleetController sourceFleet)
    {
        if (isOwned && sourceFleet != null)
            CmdCreateSplitFleet(sourceFleet.netIdentity);
    }

    public void SubmitCreateFleetFromSystem(string sysName)
    {
        if (isOwned && !string.IsNullOrEmpty(sysName))
            CmdCreateFleetFromSystem(sysName);
    }

    public void SubmitDestroyEmptyFleet(FleetController fleetCon)
    {
        if (isOwned && fleetCon != null)
            CmdDestroyEmptyFleet(fleetCon.netIdentity);
    }

    public void SubmitTransferShip(FleetController fromFleet, FleetController toFleet, int shipID)
    {
        if (isOwned && fromFleet != null && toFleet != null && shipID != 0)
            CmdTransferShip(fromFleet.netIdentity, toFleet.netIdentity, shipID);
    }

    public void SubmitCombatOrder(CombatOrders order, CivEnum actingCiv, CivEnum opposingCiv)
    {
        if (isOwned)
            CmdSendCombatOrder(order, actingCiv, opposingCiv);
    }

    public void SubmitDiplomacyOrder(NegotiationPloysEnum order, CivEnum actingCiv, CivEnum opposingCiv)
    {
        if (isOwned)
            CmdSendDiplomacyOrder(order, actingCiv, opposingCiv);
    }

    public void SubmitIntelOrder(SecretActionsEnum order, CivEnum actingCiv, CivEnum targetCiv)
    {
        if (isOwned)
            CmdSendIntelOrder(order, actingCiv, targetCiv);
    }

    // Used by MainMenuUIController.LoadGalaxyScene when this client holds host-equivalent
    // authority (PlayerManager.HostAuthorityPlayerId) but isn't the literal server - e.g. a client
    // connected to a true dedicated server. Calling PlayerManager.ServerBroadcastStartGame directly
    // would silently no-op on a non-server machine since it's a [Server]-only method.
    public void SubmitRequestStartGame(int galaxySize, int techLevel, int galaxyType, int seed, bool isSinglePlayer)
    {
        if (isOwned)
            CmdRequestStartGame(galaxySize, techLevel, galaxyType, seed, isSinglePlayer);
    }

    [Command]
    void CmdSetPlayerName(string requestedName)
    {
        string sanitized = string.IsNullOrWhiteSpace(requestedName) ? "Player" : requestedName.Trim();
        if (sanitized.Length > 24)
            sanitized = sanitized.Substring(0, 24);
        playerName = sanitized;
        PlayerManager.Instance?.UpdateRosterEntry(netId.GetHashCode(), playerName, playerCiv);
    }

    [Command]
    void CmdSetPlayerCiv(CivEnum civ)
    {
        int playerId = netId.GetHashCode();
        if (PlayerManager.Instance != null && PlayerManager.Instance.IsCivTakenByAnotherPlayer(civ, playerId))
        {
            Debug.LogWarning($"CmdSetPlayerCiv: {civ} is already taken by another player; rejecting request from player {playerId}.");
            TargetRejectPlayerCiv();
            return;
        }
        playerCiv = civ;
        PlayerManager.Instance?.UpdateRosterEntry(playerId, playerName, playerCiv);
    }

    // Roster doesn't change on a rejected request, so no SyncList callback fires on its own -
    // tell the requesting client's roster UI to redraw so its dropdown snaps back to the
    // still-authoritative civ instead of sticking on the rejected selection.
    [TargetRpc]
    void TargetRejectPlayerCiv()
    {
        ClientRosterPanelUIController.Instance?.RefreshPanel();
    }

    [Command]
    void CmdCreateSplitFleet(NetworkIdentity sourceFleetIdentity)
    {
        FleetController sourceFleet = sourceFleetIdentity != null ? sourceFleetIdentity.GetComponent<FleetController>() : null;
        if (sourceFleet == null || sourceFleet.FleetData.CivEnum != playerCiv)
        {
            Debug.LogWarning($"CmdCreateSplitFleet: player {netId.GetHashCode()} (civ={playerCiv}) not authorized for fleet '{sourceFleet?.name}'.");
            return;
        }
        FleetController newFleet = FleetManager.Instance.ServerCreateSplitFleet(sourceFleet);
        if (newFleet != null)
            TargetSplitFleetCreated(sourceFleet.netId, newFleet.netId);
    }

    [TargetRpc]
    void TargetSplitFleetCreated(uint sourceFleetNetId, uint newFleetNetId)
    {
        FleetMenuUIController.Instance?.OnSplitFleetCreated(sourceFleetNetId, newFleetNetId);
    }

    [Command]
    void CmdCreateFleetFromSystem(string sysName)
    {
        StarSysController sysCon = StarSysManager.Instance.GetStarSysControllerByName(sysName);
        if (sysCon == null || sysCon.StarSysData.CurrentOwnerCivEnum != playerCiv)
        {
            Debug.LogWarning($"CmdCreateFleetFromSystem: player {netId.GetHashCode()} (civ={playerCiv}) not authorized for system '{sysName}'.");
            return;
        }
        FleetController newFleet = FleetManager.Instance.ServerCreateFleetFromSystem(sysCon);
        if (newFleet != null)
            TargetFleetFromSystemCreated(sysName, newFleet.netId);
    }

    [TargetRpc]
    void TargetFleetFromSystemCreated(string sysName, uint newFleetNetId)
    {
        StarSysMenuUIController.Instance?.OnFleetFromSystemCreated(sysName, newFleetNetId);
    }

    // "New Fleet" creates the shell up front (see CmdCreateSplitFleet/CmdCreateFleetFromSystem
    // above) before the player has dragged any ships into it, so cancelling the deploy UI without
    // dragging anything needs to undo that. FleetController is a Mirror NetworkIdentity spawned via
    // NetworkServer.Spawn, so only the server can properly remove it (NetworkServer.Destroy) -
    // a non-host client calling FleetManager.DestroyFleetController directly only ever deleted its
    // own local copy, leaving the empty fleet alive on the server/host forever.
    [Command]
    void CmdDestroyEmptyFleet(NetworkIdentity fleetIdentity)
    {
        FleetController fleetCon = fleetIdentity != null ? fleetIdentity.GetComponent<FleetController>() : null;
        if (fleetCon == null || fleetCon.FleetData.CivEnum != playerCiv)
        {
            Debug.LogWarning($"CmdDestroyEmptyFleet: player {netId.GetHashCode()} (civ={playerCiv}) not authorized for fleet '{fleetCon?.name}'.");
            return;
        }
        if (fleetCon.FleetData.ShipsList.Count > 0)
        {
            Debug.LogWarning($"CmdDestroyEmptyFleet: fleet '{fleetCon.name}' has {fleetCon.FleetData.ShipsList.Count} ship(s) - refusing to destroy.");
            return;
        }
        FleetManager.Instance.DestroyFleetController(fleetCon);
    }

    // Relays a drag-and-drop ship move (see ShipListUI_Item.AddToFleet) to the server so its
    // authoritative FleetData.ShipsList lists stay correct - see FleetManager.ServerTransferShip's
    // comment for why this is needed at all on a non-host client.
    [Command]
    void CmdTransferShip(NetworkIdentity fromFleetIdentity, NetworkIdentity toFleetIdentity, int shipID)
    {
        FleetController fromFleet = fromFleetIdentity != null ? fromFleetIdentity.GetComponent<FleetController>() : null;
        FleetController toFleet = toFleetIdentity != null ? toFleetIdentity.GetComponent<FleetController>() : null;
        if (fromFleet == null || toFleet == null || fromFleet.FleetData.CivEnum != playerCiv || toFleet.FleetData.CivEnum != playerCiv)
        {
            Debug.LogWarning($"CmdTransferShip: player {netId.GetHashCode()} (civ={playerCiv}) not authorized for transfer between '{fromFleet?.name}' and '{toFleet?.name}'.");
            return;
        }
        FleetManager.Instance.ServerTransferShip(fromFleet, toFleet, shipID);
    }

    [Command]
    void CmdSendCombatOrder(CombatOrders order, CivEnum actingCiv, CivEnum opposingCiv)
    {
        RpcApplyCombatOrder(order, actingCiv, opposingCiv);
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

    [Command]
    void CmdSendDiplomacyOrder(NegotiationPloysEnum order, CivEnum actingCiv, CivEnum opposingCiv)
    {
        RpcApplyDiplomacyOrder(order, actingCiv, opposingCiv);
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

    [Command]
    void CmdSendIntelOrder(SecretActionsEnum order, CivEnum actingCiv, CivEnum targetCiv)
    {
        RpcApplyIntelOrder(order, actingCiv, targetCiv);
    }

    // Host-authority check intentionally removed - any connected client can trigger game start
    // (MainMenuUIController.ApplyHostOnlyGating no longer disables the Start button either), so
    // testers on a dedicated server don't need a host-equivalent player to begin the game.
    [Command]
    void CmdRequestStartGame(int galaxySize, int techLevel, int galaxyType, int seed, bool isSinglePlayer)
    {
        if (PlayerManager.Instance == null)
            return;
        PlayerManager.Instance.ServerBroadcastStartGame(galaxySize, techLevel, galaxyType, seed, isSinglePlayer);
    }

    [ClientRpc]
    void RpcApplyIntelOrder(SecretActionsEnum order, CivEnum actingCiv, CivEnum targetCiv)
    {
        GiveIntelOrder(order, actingCiv, targetCiv);
    }

    public void GiveCombatOrder(CombatOrders order, CombatController combatCon, CivEnum civ)
    {
        switch (order)
        {
            case CombatOrders.Engage:
                combatCon.SetShipOrders(CombatOrders.Engage, civ);
                break;
            case CombatOrders.Rush:
                combatCon.SetShipOrders(CombatOrders.Rush, civ);
                break;
            case CombatOrders.Retreat:
                combatCon.SetShipOrders(CombatOrders.Retreat, civ);
                break;
            case CombatOrders.Formation:
                combatCon.SetShipOrders(CombatOrders.Formation, civ);
                break;
            case CombatOrders.AttackTransports:
                combatCon.SetShipOrders(CombatOrders.AttackTransports, civ);
                break;
        }
    }

    public void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diploCon, CivEnum civ)
    {
        // No-op: NegotiationPloysEnum has no consumer anywhere in the codebase yet (diplomacy
        // negotiation logic hasn't been built for single-player either). The RPC above already
        // delivers this order to every client correctly; wire in real diplomacy resolution here
        // once that system exists.
    }

    public void GiveIntelOrder(SecretActionsEnum order, CivEnum actingCiv, CivEnum targetCiv)
    {
        IntelligenceManager.Instance?.CreateIntelProject(order, actingCiv, targetCiv, out _);
    }

    internal void SuggestCombatOrder(CombatData combatData)
    {
        // Implement AI combat logic to evaluate the situation and suggest an appropriate order.
        // This to involve analyzing combatData and making decisions based on various factors.
        //GiveCombatOrder(CombatOrders.Engage); // Example order, replace with actual AI logic
    }
}
