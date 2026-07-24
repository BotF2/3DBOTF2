// Ignore Spelling: Unregister

using BOTF3D.Combat;
using BOTF3D.Core;

using Mirror;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    [System.Serializable]
    public struct RosterEntry
    {
        public int PlayerId;
        public string PlayerName;
        public CivEnum PlayerCiv;
        public PlayerType PlayerType;
    }

    public class PlayerManager : NetworkBehaviour, IManager
    {
        public void Initialize() { }
        public void Cleanup() { }
        public static PlayerManager Instance;

    public LocalHumanPlayerController LocalPlayerController { get; private set; }
    public IPlayerController LocalPlayer { get; private set; }
    public readonly List<IPlayerController> AllPlayerControllers = new List<IPlayerController>();

    // Server-authoritative lobby roster, replicated to every client for the multiplayer lobby/status panel.
    public readonly SyncList<RosterEntry> Roster = new SyncList<RosterEntry>();

    // Who currently has host-equivalent authority (sees/uses the host-only lobby controls and
    // may trigger ServerBroadcastStartGame) - distinct from NetworkServer.active, which is only
    // true on the literal machine running the server process. On a true dedicated server
    // (StartServer() only, no local player - see PersistentSceneBootstrap.StartDedicatedServer)
    // nobody has NetworkServer.active locally, so one connected client needs to stand in as game
    // master instead. -1 = unclaimed.
    [SyncVar] public int HostAuthorityPlayerId = -1;

    // RENAMED: PlayerData → PlayerInfo
    public List<GamePlayerInfo> PlayerInfoList { get; private set; } = new List<GamePlayerInfo>();

    private List<CivEnum> civEnumsForPlayerCons = new List<CivEnum>();

    private void Awake()
    {
            ServiceLocator.Register<PlayerManager>(this);
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void NetworkStatus()
    {
        if (NetworkServer.active && NetworkClient.active)
            Debug.Log("Host");
        else if (NetworkServer.active)
            Debug.Log("Dedicated server");
        else if (NetworkClient.active)
            Debug.Log("Client");
        else
            Debug.Log("Single Player (no network)");
    }

    // Same 7 majors as ClientRosterPanelUIController.SelectableCivs - kept in this order so the
    // first free slot a new player is assigned matches the order civs appear in the roster dropdown.
    private static readonly CivEnum[] AssignableCivs =
    {
        CivEnum.FED, CivEnum.ROM, CivEnum.KLING, CivEnum.CARD, CivEnum.DOM, CivEnum.BORG, CivEnum.TERRAN
    };

    // Returns the assigned civ so callers (LocalHumanPlayerController/AiPlayerController) can set
    // their own authoritative civ field to match - otherwise a new player's [SyncVar] playerCiv
    // stays at its own default (FED) even though the roster entry says otherwise, and the two
    // desync the moment anything reads the controller's civ directly instead of the roster.
    public CivEnum RegisterPlayer(IPlayerController controller, bool isLocalPlayerArg, string playerName, int playerId, PlayerType playerType)
    {
        CivEnum assignedCiv = GetFirstAvailableCiv(playerId);

        GamePlayerInfo playerData = new GamePlayerInfo(playerName)
        {
            PlayerId = playerId,
            PlayerType = playerType,
            PlayerCiv = assignedCiv
        };

        PlayerInfoList.Add(playerData);
        Roster.Add(new RosterEntry { PlayerId = playerId, PlayerName = playerName, PlayerCiv = assignedCiv, PlayerType = playerType });
        Debug.Log($"PlayerManager: Registered {playerType} player '{playerName}' (ID: {playerId}) with civ {assignedCiv}");
        return assignedCiv;
    }

    // Picks the first civ (in AssignableCivs order) not already held by another connected player,
    // so a newly-joined player doesn't collide with an existing player's civ pick (e.g. host on FED).
    private CivEnum GetFirstAvailableCiv(int excludingPlayerId)
    {
        for (int i = 0; i < AssignableCivs.Length; i++)
        {
            if (!IsCivTakenByAnotherPlayer(AssignableCivs[i], excludingPlayerId))
                return AssignableCivs[i];
        }
        return AssignableCivs[0]; // all 7 taken (more players than majors) - fall back, server already rejects the duplicate elsewhere
    }

    public void UnregisterPlayer(int playerID)
    {
        if (AllPlayerControllers != null)
        {
            for (int i = 0; i < AllPlayerControllers.Count; i++)
            {
                if (AllPlayerControllers[i].PlayerInfo.PlayerId == playerID)
                {
                    AllPlayerControllers.RemoveAt(i);
                    break;
                }
            }
        }

        // SyncList writes require an active server/client - during shutdown (e.g. this being
        // called from LocalHumanPlayerController.OnDestroy() after NetworkServer/NetworkClient
        // have already stopped), the whole Roster is being torn down anyway, so skip the mutation
        // rather than let SyncList throw.
        if (NetworkServer.active || NetworkClient.active)
        {
            for (int i = 0; i < Roster.Count; i++)
            {
                if (Roster[i].PlayerId == playerID)
                {
                    Roster.RemoveAt(i);
                    break;
                }
            }
        }

        Debug.Log($"UnregisterPlayer: Processed unregister for player ID {playerID}");
    }

    // Server-authoritative check used by CmdSetPlayerCiv to reject a civ pick already held by
    // a different connected player, so two clients can never end up on the same civilization.
    public bool IsCivTakenByAnotherPlayer(CivEnum civ, int requestingPlayerId)
    {
        for (int i = 0; i < Roster.Count; i++)
        {
            if (Roster[i].PlayerCiv == civ && Roster[i].PlayerId != requestingPlayerId)
                return true;
        }
        return false;
    }

    public void UpdateRosterEntry(int playerId, string playerName, CivEnum civ)
    {
        for (int i = 0; i < Roster.Count; i++)
        {
            if (Roster[i].PlayerId == playerId)
            {
                RosterEntry entry = Roster[i];
                entry.PlayerName = playerName;
                entry.PlayerCiv = civ;
                Roster[i] = entry;
                return;
            }
        }
    }

    public void SetLocalPlayerController(LocalHumanPlayerController controller)
    {
        LocalPlayerController = controller;
    }

    public void AddLocalPlayer(GamePlayerInfo data)
    {
        PlayerInfoList.Add(data);
    }

    public void RemoveLocalPlayer(GamePlayerInfo data)
    {
        PlayerInfoList.Remove(data);
    }

    public void AddPlayer(GamePlayerInfo player)
    {
        if (!PlayerInfoList.Contains(player))
        {
            PlayerInfoList.Add(player);
        }
    }

    public void RemovePlayer(GamePlayerInfo player)
    {
        if (PlayerInfoList.Contains(player))
        {
            PlayerInfoList.Remove(player);
        }
    }

    public GamePlayerInfo GetPlayerById(int playerId)
    {
        return PlayerInfoList.Find(player => player.PlayerId == playerId);
    }

    internal void GetCivsInGameAsGalaxyIsBuilt(List<CivSO> civSOsInGame)
    {
        for (int i = 0; i < civSOsInGame.Count; i++)
        {
            civEnumsForPlayerCons.Add(civSOsInGame[i].CivEnum);
        }
    }

    public void ResetPlayerList()
    {
        if (PlayerInfoList != null)
            PlayerInfoList.Clear();
    }

    public void AssignGoToPlayer(GameObject unitGO, int playerId)
    {
        GamePlayerInfo player = GetPlayerById(playerId);
        if (player != null)
            player.OwnedGameObjects.Add(unitGO);

        StarSysController starSysCon = unitGO.GetComponent<StarSysController>();
        if (starSysCon != null)
        {
            starSysCon.StarSysData.PlayerId = playerId;
        }

        FleetController fleetCon = unitGO.GetComponent<FleetController>();
        if (fleetCon != null)
        {
            fleetCon.FleetData.PlayerId = playerId;
        }

        ShipController shipCon = unitGO.GetComponent<ShipController>();
        if (shipCon != null)
        {
            shipCon.ShipData.PlayerId = playerId;
        }
    }

    private int GetAssignedConnectionIdForCiv(CivEnum civEnum)
    {
        return 0;
    }

    internal void SetMajorCivsInGameForMultiPlayer(List<CivEnum> majorCivsInGameList, CivEnum localPlayerCiv)
    {
        Debug.Log("SetMajorCivsInGameForMultiPlayer: Multiplayer setup pending implementation");
    }

    // Host-only entry point (called from MainMenuUIController.LoadGalaxyScene, which only the host
    // can reach). Broadcasts the host's chosen game parameters - including a shared RNG seed - to
    // every connected client so galaxy generation (CivManager/StarSysManager, both driven by the
    // global UnityEngine.Random) produces an identical result on every machine.
    [Server]
    public void ServerBroadcastStartGame(int galaxySize, int techLevel, int galaxyType, int seed, bool isSinglePlayer)
    {
        RpcStartGame(galaxySize, techLevel, galaxyType, seed, isSinglePlayer);
    }

    [ClientRpc]
    private void RpcStartGame(int galaxySize, int techLevel, int galaxyType, int seed, bool isSinglePlayer)
    {
        MainMenuUIController.Instance?.OnGameStartReceived(galaxySize, techLevel, galaxyType, seed, isSinglePlayer);
    }

    // Called from LocalHumanPlayerController.OnStartServer for every newly-spawned player object -
    // the first connector to find no one holding authority claims it automatically (chosen over an
    // explicit "claim" action so a client just needs to Connect to a dedicated server to become its
    // game master). Harmless when this machine is also the literal host (StartHost()): NetworkServer.active
    // already grants that client full control via MainMenuUIController.ApplyHostOnlyGating, so this
    // SyncVar just tracks it for consistency.
    [Server]
    public void ClaimHostAuthorityIfUnclaimed(int playerId)
    {
        if (HostAuthorityPlayerId == -1)
        {
            HostAuthorityPlayerId = playerId;
            Debug.Log($"PlayerManager: player {playerId} claimed host-equivalent authority (first connector).");
        }
    }

    // Called from LocalHumanPlayerController.OnStopServer. Releases authority rather than
    // reassigning it here - the next player to connect claims it via ClaimHostAuthorityIfUnclaimed above.
    [Server]
    public void ReleaseHostAuthorityIfHeldBy(int playerId)
    {
        if (HostAuthorityPlayerId == playerId)
        {
            HostAuthorityPlayerId = -1;
            Debug.Log($"PlayerManager: player {playerId} disconnected - host-equivalent authority released.");
        }
    }


        private void OnDestroy()
        {
            ServiceLocator.Unregister<PlayerManager>(); }
    }
}