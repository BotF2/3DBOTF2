// Ignore Spelling: Unregister

using BOTF3D.Core;
using BOTF3D.GamePlay;
using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    public LocalHumanPlayerController LocalPlayerController { get; private set; }
    public IPlayerController LocalPlayer { get; private set; }
    public readonly List<IPlayerController> AllPlayerControllers = new List<IPlayerController>();

    // RENAMED: PlayerData → PlayerInfo
    public List<GamePlayerInfo> PlayerInfoList { get; private set; } = new List<GamePlayerInfo>();

    private List<CivEnum> civEnumsForPlayerCons = new List<CivEnum>();

    private void Awake()
    {
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
        Debug.Log($"PlayerManager: Registered {playerType} player '{playerName}' (ID: {playerId})");
    }

    public void UnregisterPlayer(int playerID)
    {
        if (AllPlayerControllers == null || AllPlayerControllers.Count == 0)
        {
            // ✅ Use Debug.Log instead of LogWarning during app shutdown (it's expected)
            Debug.Log("UnregisterPlayer: No players to unregister (expected in single-player or during shutdown)");
            return;
        }

        for (int i = 0; i < AllPlayerControllers.Count; i++)
        {
            if (AllPlayerControllers[i].PlayerInfo.PlayerId == playerID)
            {
                Debug.Log($"UnregisterPlayer: Removed player ID {playerID}");
                AllPlayerControllers.RemoveAt(i);
                break;
            }
        }
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
}

