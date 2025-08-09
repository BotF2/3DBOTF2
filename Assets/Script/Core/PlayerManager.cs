using Assets.Core;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
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
    new bool isLocalPlayer = false; // Flag to check if this is the local player
    //new bool isServer = false; // Flag to check if this is the server
    //new bool isAI = false; // Flag to check if this is an AI player
    public LocalHumanPlayerController LocalPlayerCon; // Local player controller instance on this PC machine
    public List<AiPlayerController> AIPlayerControllers; // AI controller instances
    public List<RemoteHumanPlayerController> RemoteHumanPlayerControllers; // Remote player controller instances
    public List<PlayerData> allPlayerDatas = new List<PlayerData>(); // List of all players in the game, local, AI, and remote players
    private List<CivEnum> civEnumsForPlayerCons = new List<CivEnum>(); // List of major civilizations in the game
    [Header("Prefab to spawn for players")] 
    public GameObject playerDataPrefab; // assigned in the inspector
    public List<PlayerData> Players = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // Example: spawn the player object from the server side
    [Server]
    public void SpawnPlayer(NetworkConnectionToClient conn)
    {
        if (playerDataPrefab == null)
        {
            Debug.LogError("[PlayerManager] No playerDataPrefab assigned for spawning.");
            return;
        }

        GameObject playerInstance = Instantiate(playerDataPrefab);
        NetworkServer.AddPlayerForConnection(conn, playerInstance);
    }
    public void SetPlayerCivs()
    {

    }
    public void AddLocalPlayer(PlayerData data)
    {
        Players.Add(data);
    }

    public void RemoveLocalPlayer(PlayerData data)
    {
        Players.Remove(data);
    }
    public void AddPlayer(PlayerData player)
    {
        if (!allPlayerDatas.Contains(player))
        {
            allPlayerDatas.Add(player);
            // Optionally, you can initialize player data here
        }
    }
    public void RemovePlayer(PlayerData player)
    {
        if (allPlayerDatas.Contains(player))
        {
            allPlayerDatas.Remove(player);
            // Optionally, clean up player data here
        }
    }
    public PlayerData GetPlayerById(int playerId)
    {

        return allPlayerDatas.Find(player => player.PlayerId == playerId);
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
        if (allPlayerDatas != null)
            allPlayerDatas.Clear();
    }
    public void SetLocalPlayer(CivEnum civLocal)// List<CivEnum> majorsInGame)
     {
        if (playerDataPrefab == null && !NetworkServer.active)
        {
            Debug.LogError("[PlayerManager] No playerDataPrefab assigned for spawning.");
            return;
        }
        GameObject playerDataGO = Instantiate(playerDataPrefab);
        NetworkServer.Spawn(playerDataGO);
        NetworkIdentity networkIdentity = playerDataGO.GetComponent<NetworkIdentity>(); 
        PlayerData playerData = playerDataGO.GetComponent<PlayerData>();
        playerData.PlayerId = 0; // local player is always Player 0
        playerData.PlayerName = "Local Player";
        playerData.PlayerCiv = civLocal;
        playerData.PlayerType = PlayerType.Local; // Set the player type to Local                                              
        LocalHumanPlayerController localController = gameObject.AddComponent<LocalHumanPlayerController>();
        localController.PlayerData = playerData;
        LocalPlayerCon = localController;
        isLocalPlayer = true; // Set the flag to indicate this is the local player's PlayerManager
        allPlayerDatas.Add(playerData);
    } 
    public void SetMajorCivsInGameForSinglePlayer(List<CivEnum> majorsInGame, CivEnum localPlayerCiv)
    { 
        if (playerDataPrefab == null && !NetworkServer.active)
        {
            Debug.LogError("[PlayerManager] No playerDataPrefab assigned for spawning.");
            return;
        }
        for (int i = 0; i < majorsInGame.Count; i++)
        {
            if (majorsInGame[i] != localPlayerCiv) // Avoid adding the local player's civ again
            {
                GameObject playerDataGO = Instantiate(playerDataPrefab);
                NetworkServer.Spawn(playerDataGO);
                NetworkIdentity networkIdentity = playerDataGO.GetComponent<NetworkIdentity>();
                PlayerData playerData = playerDataGO.GetComponent<PlayerData>();
                playerData.PlayerId = 0; // local player is always Player 0
                playerData.PlayerName = "AI Player";
                playerData.PlayerCiv = majorsInGame[i];
                playerData.PlayerType = PlayerType.AI; // Set the player type to AI
                allPlayerDatas.Add(playerData);
                AiPlayerController aiController = gameObject.AddComponent<AiPlayerController>();
                aiController.PlayerData = playerData;
                AIPlayerControllers.Add(aiController);
            }
            //else
            //{
            //     if (majorsInGame[i] == localPlayerCiv)
            //     {
            //        GameObject playerDataGO = Instantiate(playerDataPrefab);
            //        PlayerData playerData = playerDataGO.GetComponent<PlayerData>();
            //        playerData.PlayerId = 0; // local player is always Player 0
            //        playerData.PlayerName = "Local Player";
            //        playerData.PlayerCiv = majorsInGame[i];
            //        playerData.PlayerType = PlayerType.Local; // Set the player type to Local    
            //        LocalHumanPlayerController localController = gameObject.AddComponent<LocalHumanPlayerController>();
            //        localController.PlayerData = playerData;
            //        LocalPlayerCon = localController;
            //        isLocalPlayer = true; // Set the flag to indicate this is the local player's PlayerManager
            //        allPlayerDatas.Add(playerData);
            //     }
            //}
        }
    }

    internal void SetPlayerIds()
    {
        if (playerDataPrefab == null && !NetworkServer.active)
        {
            Debug.LogError("[PlayerManager] No playerDataPrefab assigned for spawning.");
            return;
        }
        for (int i = 0; i < civEnumsForPlayerCons.Count; i++) // local player has taken PlayerId 0 above
        {
            CivEnum civEnum = civEnumsForPlayerCons[i];
            // Assuming nextId starts from 1 for AI players, increment it for each player
            int nextId = allPlayerDatas.Count; // Use the current count as the next ID
            {
                bool isRemote = false;
                int assignedConnectionId = GetAssignedConnectionIdForCiv(civEnumsForPlayerCons[i]); // ***Your lobby logic / network is needed from this method call
                // Host's connectionId is usually 0 (or use NetworkServer.localConnection)
                if (assignedConnectionId != NetworkServer.localConnection.connectionId)
                {
                    isRemote = true;
                }
                if (isRemote)
                {
                    GameObject playerDataGO = Instantiate(playerDataPrefab);
                    NetworkServer.Spawn(playerDataGO);
                    NetworkIdentity networkIdentity = playerDataGO.GetComponent<NetworkIdentity>();
                    PlayerData playerData = playerDataGO.GetComponent<PlayerData>();
                    playerData.PlayerId = nextId;
                    playerData.PlayerName = $"Player {nextId} Remote";
                    playerData.PlayerCiv = civEnum;
                    playerData.PlayerType = PlayerType.Remote; // Set the player type to Remote
                    RemoteHumanPlayerController remoteController = gameObject.AddComponent<RemoteHumanPlayerController>();
                    remoteController.PlayerData = playerData;
                    RemoteHumanPlayerControllers.Add(remoteController);
                    allPlayerDatas.Add(playerData);
                }
                else
                {
                    GameObject playerDataGO = Instantiate(playerDataPrefab);
                    NetworkServer.Spawn(playerDataGO);
                    NetworkIdentity networkIdentity = playerDataGO.GetComponent<NetworkIdentity>();
                    PlayerData playerData = playerDataGO.GetComponent<PlayerData>();
                    playerData.PlayerId = 0; // local player is always Player 0
                    playerData.PlayerName = "AI Player";
                    playerData.PlayerCiv = civEnumsForPlayerCons[i];
                    playerData.PlayerType = PlayerType.AI; // Set the player type to AI
                    AiPlayerController aiController = gameObject.AddComponent<AiPlayerController>();
                    aiController.PlayerData = playerData;
                    AIPlayerControllers.Add(aiController);
                    allPlayerDatas.Add(playerData);
                }
                nextId++;
            }
        }
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
        if (playerDataPrefab == null && !NetworkServer.active)
        {
            Debug.LogError("[PlayerManager] No playerDataPrefab assigned for spawning.");
            return;
        }
        for (int i = 0; i < majorCivsInGameList.Count; i++)
        {
            if (majorCivsInGameList[i] != localPlayerCiv) // Avoid adding the local player's civ again
            {
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
            }
            //else
            //{
            //    if (majorCivsInGameList[i] == localPlayerCiv)
            //    {
            //        GameObject playerDataGO = Instantiate(playerDataPrefab);
            //NetworkServer.Spawn(playerDataGO);
            //        PlayerData playerData = playerDataGO.GetComponent<PlayerData>();
            //        playerData.PlayerId = 0; // local player is always Player 0
            //        playerData.PlayerName = "Local Player";
            //        playerData.PlayerCiv = majorCivsInGameList[i];
            //        playerData.PlayerType = PlayerType.Local; // Set the player type to Local    
            //        LocalHumanPlayerController localController = gameObject.AddComponent<LocalHumanPlayerController>();
            //        localController.PlayerData = playerData;
            //        LocalPlayerCon = localController;
            //        isLocalPlayer = true; // Set the flag to indicate this is the local player's PlayerManager
            //        allPlayerDatas.Add(playerData);
            //    }
            //}
        }
    }
}

