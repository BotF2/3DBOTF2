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

    public void RegisterPlayer(IPlayerController controller, bool isLocalPlayerArg, string playerName, int playerId, PlayerType playerType)
    {
        GamePlayerInfo playerData = new GamePlayerInfo(playerName)
        {
            PlayerId = playerId,
            PlayerType = playerType,
            PlayerCiv = CivEnum.FED
        };

        PlayerInfoList.Add(playerData);
        Roster.Add(new RosterEntry { PlayerId = playerId, PlayerName = playerName, PlayerCiv = CivEnum.FED, PlayerType = playerType });
        Debug.Log($"PlayerManager: Registered {playerType} player '{playerName}' (ID: {playerId})");
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

        for (int i = 0; i < Roster.Count; i++)
        {
            if (Roster[i].PlayerId == playerID)
            {
                Roster.RemoveAt(i);
                break;
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


        private void OnDestroy()
        {
            ServiceLocator.Unregister<PlayerManager>(); }
    }
}