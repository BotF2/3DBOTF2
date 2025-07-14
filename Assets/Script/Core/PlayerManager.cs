using Assets.Core;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    /// <summary>
    /// Start creates 7 PlayerData objects, one for each playable slot in the game.
    /// </summary> 

    public static PlayerManager Instance; // Singleton instance
    public List<PlayerData> Players = new List<PlayerData>(); // List of all players in the game
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this instance across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }
    void Start()
    {
        // Setup the 7 player slots for 7 major civilizations
        for (int i = 0; i < 7; i++)
        {
            PlayerData playerData = new PlayerData
            {
                PlayerId = i,// network player ID, not used in single player
                PlayerName = $"Player {i + 1}",
                Civ = (CivEnum)i, // Example mapping

            };
            Players.Add(playerData);
        }
    }
    public void AddPlayer(PlayerData player)
    {
        if (!Players.Contains(player))
        {
            Players.Add(player);
            // Optionally, you can initialize player data here
        }
    }
    public void RemovePlayer(PlayerData player)
    {
        if (Players.Contains(player))
        {
            Players.Remove(player);
            // Optionally, clean up player data here
        }
    }
    public PlayerData GetPlayerById(int playerId)
    {
        return Players.Find(player => player.PlayerId == playerId);
    }
    public List<PlayerData> GetAllPlayers()
    {
        return new List<PlayerData>(Players); // Return a copy of the list
    }
    public void ClearPlayers()
    {
        Players.Clear(); // Clear the list of players
        // Optionally, you can also reset player data here
    }
    public void AssignUnitToPlayer(GameObject unitGO, int playerId)
    {
        PlayerData player = GetPlayerById(playerId);
        if (player != null)
            player.OwnedUnits.Add(unitGO);
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
        //DiplomacyController diplomacyCon = unitGO.GetComponent<DiplomacyController>();
        //if (diplomacyCon != null)
        //{
        //    diplomacyCon.DiplomacyData.PlayerSideOneId = playerId;????
        //    //    diplomacyCon.DiplomacyData.PlayerSideTwoId = playerId;
        //}
    }
}

