using Assets.Core;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    public LocalHumanPlayerController LocalPlayerController { get; private set; } // Local player controller instance on this PC machine
    public IPlayerController LocalPlayer { get; private set; }
    public List<IPlayerController> AllPlayerControllers { get; private set; } = new List<IPlayerController>(); // List of all player controllers
    public List<PlayerData> PlayerDatas { get; private set; } = new List<PlayerData>(); // List of all players in the game, local, AI, and remote players
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

    public void RegisterPlayer(IPlayerController player, bool isLocal)
    {
        if (player == null)
        {
            Debug.LogError("Attempted to register a null player.");
            return;
        }
        AllPlayerControllers.RemoveAll(p => p == null); // Clean up any null references
        if (!AllPlayerControllers.Contains(player))
        {
            AllPlayerControllers.Add(player);
        }
        if (isLocal)
        {
            LocalPlayerController = player as LocalHumanPlayerController;
            isLocalPlayer = true;
            LocalPlayer = player;
        }
    }
    public void UnregisterPlayer(IPlayerController player)
    {
        AllPlayerControllers.Remove(player);
        if (LocalPlayerController == player)
            LocalPlayerController = null;
        if (LocalPlayer == player)
            LocalPlayer = null;
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

