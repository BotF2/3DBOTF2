using Assets.Core;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    /// <summary>
    /// Using IPlayerController interface to define player actions and properties.
    /// Using PlayerManager to manage/instantiate new players in the game.
    /// Using PlayerData class to hold player-specific data.
    /// Using LocalHumanPlayerController to implement the interface for local player actions/orders.
    /// Using AiPlayerController to implement the interface for AI player actions/orders.
    /// Using RemotePlayerController to implement the interface for remote player actions/orders.
    /// Start creates 7 PlayerData objects, one for each playable slot in the game.
    /// <summary Multiplayer issues>
    /// ??? Unity ToggleGroup by default only allows one toggle to be active so:
    /// Will each remote player make a unique selection in their own Toggle group or
    /// is it better to just have buttons, or toggles, not in a group for remotes to select?
    /// Need to sort out and define local player for the host and from each remote player PC in multiplayer lobby
    /// We are trying to using (Mirror Networking; with GameObject LocalPlayerCivEnum = NetworkClient.LocalPlayerCivEnum.gameObject;)
    /// https://www.youtube.com/watch?v=FSVn57wOWfk
    /// ToDo this...
    /// 
    /// ? Use [ServerRpc] for client-to-server input (remote human).
    /// Server handles resolution, sends results via[ClientRpc].
    /// </summary>

    public static PlayerManager Instance; // Singleton instance
    readonly bool isLocalPlayer = false; // Flag to check if this is the local player
    public class SynchListPlayerData : SyncList<PlayerData> { }
    public readonly SynchListPlayerData AllPlayerDatas = new SynchListPlayerData(); // Synchronized list of player data for multiplayer
    public LocalHumanPlayerController LocalPlayerController { get; private set; } // Local player controller instance on this PC machine
    public IPlayerController LocalPlayer { get; private set; }
    public readonly List<IPlayerController> AllPlayerControllers = new List<IPlayerController>(); // List of all player controllers
    public List<PlayerData> PlayerDatas { get; private set; } = new List<PlayerData>(); // List of all players data in the game, local, AI, and remote players
    private List<CivEnum> civEnumsForPlayerCons = new List<CivEnum>(); // List of major civilizations in the game


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
    public void RegisterPlayer(IPlayerController player, bool isLocal, string playerName, int playerId, PlayerType type)
    {
        if (player == null)
        {
            Debug.LogError("Attempted to register a null player.");
            return;
        }
        AllPlayerControllers.RemoveAll(p => p == null); // Clean up any null references
        var playerData = new PlayerData(playerName)
        {
            PlayerId = playerId,
            PlayerName = playerName,
            PlayerType = type,
            // PlayerCiv = ?
        };
        var majorCivList = GameController.Instance.GameData.MajorCivsInGameList;
        for (int i = 0; i < majorCivList.Count; i++)
        {
            if (type == PlayerType.AI)
            {
                playerData.PlayerCiv = majorCivList[i];
                playerData.PlayerName = majorCivList.ToString();
                break;
            }
        }

        player.PlayerData = playerData;
        if (!AllPlayerControllers.Contains(player))
        {
            AllPlayerControllers.Add(player);
        }
    }
    public void UnregisterPlayer(int playerID) //IPlayerController player)
    {
        if (AllPlayerControllers == null || AllPlayerControllers.Count == 0)
        {
            Debug.LogWarning("No players to unregister.");
            return;
        }
        for (int i = 0; i < AllPlayerControllers.Count; i++)
        {
            if (AllPlayerControllers[i].PlayerData.PlayerId == playerID)
            {
                AllPlayerControllers.RemoveAt(i);
                //if (LocalPlayerController == player)
                //    LocalPlayerController = null;
                //if (LocalPlayer == AllPlayerControllers[i])
                //    LocalPlayer = null;
                break;
            }
        }
    }

    public void AddLocalPlayer(PlayerData data)
    {
        PlayerDatas.Add(data);
    }

    public void RemoveLocalPlayer(PlayerData data)
    {
        PlayerDatas.Remove(data);
    }
    public void AddPlayer(PlayerData player)
    {
        if (!PlayerDatas.Contains(player))
        {
            PlayerDatas.Add(player);
            // Optionally, you can initialize player data here
        }
    }
    public void RemovePlayer(PlayerData player)
    {
        if (PlayerDatas.Contains(player))
        {
            PlayerDatas.Remove(player);
            // Optionally, clean up player data here
        }
    }
    public PlayerData GetPlayerById(int playerId)
    {

        return PlayerDatas.Find(player => player.PlayerId == playerId);
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
        if (PlayerDatas != null)
            PlayerDatas.Clear();
    }
 
    public void AssignGoToPlayer(GameObject unitGO, int playerId)
    {
        PlayerData player = GetPlayerById(playerId);
        if (player != null)
            player.OwnedGameObjects.Add(unitGO);
        // Optionally, set a reference on the unit itself

        StarSysController starSysCon = unitGO.GetComponent<StarSysController>();
        if (starSysCon != null)
        {
            starSysCon.StarSysData.PlayerId = playerId;
        }
        FleetController fleetCon = unitGO.GetComponent<FleetController>();
        if (fleetCon != null)
        {
            fleetCon.FleetData.PlayerId = playerId; // changed PlayerID to PlayerId 
        }
        ShipController shipCon = unitGO.GetComponent<ShipController>();
        if (shipCon != null)
        {
            shipCon.ShipData.PlayerId = playerId; // changed PlayerID to PlayerId
        }
    }
    private int GetAssignedConnectionIdForCiv(CivEnum civEnum)
    {
        // Placeholder implementation. Replace with your actual lobby logic / network.
        // For now, just return 0 for the local player and increment for others.
        // Need to map CivEnum to connectionId based on your game's lobby state.
        return 0;
    }

    internal void SetMajorCivsInGameForMultiPlayer(List<CivEnum> majorCivsInGameList, CivEnum localPlayerCiv)
    {
        //if (playerDataPrefab == null && !NetworkServer.active)
        //{
        //    Debug.LogError("[PlayerManager] No playerPrefab assigned for spawning.");
        //    return;
        //}
        //for (int i = 0; i < majorCivsInGameList.Count; i++)
        //{
        //    if (majorCivsInGameList[i] != localPlayerCiv) // Avoid adding the local player's civ again
        //    {
                // ********** Code here for muliplayer setup
                //PlayerData aiPlayerData = new PlayerData
                //{
                //    PlayerId = i + 1, // Start from 1 since 0 is the local player
                //    PlayerName = $"AI Player {i + 1}",
                //    PlayerCiv = majorsInGame[i],
                //    PlayerType = PlayerType.AI // Set the player type to AI
                //};
                //allPlayerDatas.Add(aiPlayerData);
                //AiPlayerController aiController = gameObject.AddComponent<AiPlayerController>();
                //aiController.PlayerData = aiPlayerData;
                //AIPlayerControllers.Add(aiController);
                //civEnumsForPlayerCons.Remove(majorsInGame[i]);
            //}

        //}
    }
}

