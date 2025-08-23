using Assets.Core;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum PlayerType { Local, AI, Remote }
public class PlayerData : NetworkBehaviour
{
    /// <summary>
    /// holds player-specific data for multiplayer games. (Player logic is in the PlayerController classes)
    /// When a player issues a command(move, attack, etc.), validate that the player owns the unit before executing the command.
    /// </summary>
    [SyncVar] public int PlayerId; // Unique for each player
    [SyncVar] public string PlayerName;
    [SyncVar] public PlayerType PlayerType; // Local, AI, or Remote
    [SyncVar] public CivEnum PlayerCiv; // Civilization enum for the player
    public List<GameObject> OwnedGameObjects = new List<GameObject>(); // star systems, fleets, ships, starbases, etc.
    // Add more player-specific data as needed

    // Replacing NetworkVariable with a Mirror-compatible alternative
    [SyncVar]
    public string NetworkPlayerName = string.Empty;
    private string v;

    public PlayerData(string v)
    {
        this.v = v;
    }

    public void Initialize(int id, string name)
    {
        PlayerId = id;
        NetworkPlayerName = name;
    }

    public override void OnStartServer()
    {
        NetworkPlayerName = $"Player_{connectionToClient.connectionId}";
        SpawnInitialFleet();
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"Local player initialized: {NetworkPlayerName}");
    }

    void SpawnInitialFleet() // example of network Multiplayer spawning a fleet at a random position 
    {
        // Use a central spawn point or random location
        //Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10));
        //GameObject fleetInstance = Instantiate(GameManager.Instance.fleetPrefab, spawnPosition, Quaternion.identity);

        //FleetController fleet = fleetInstance.GetComponent<FleetController>();
        //fleet.ownerId = connectionToClient.connectionId;

        // Register fleet and spawn to network
        //ownedFleets.Add(fleet);
        //NetworkServer.Spawn(fleetInstance, connectionToClient);
    }

    // Called by UI or input to move a fleet
    [Command]
    public void CmdRequestMoveFleet(uint fleetNetId, Vector3 destination) // Changed parameter type from ulong to uint
    {
        if (NetworkServer.spawned.TryGetValue(fleetNetId, out NetworkIdentity netObj)) // Added TryGetValue for safer access
        {
            if (netObj.TryGetComponent(out FleetController fleet) && fleet.ownerId == connectionToClient.connectionId)
            {
                // do fleet orders;
            }
        }
        else
        {
            Debug.LogWarning($"Fleet with NetId {fleetNetId} not found.");
        }
    }

    public void RegisterFleet(FleetController fleet)
    {
        //if (!ownedFleets.Contains(fleet))
        //{
        //    ownedFleets.Add(fleet);
        //}
    }

    public void UnregisterFleet(FleetController fleet)
    {
        //ownedFleets.Remove(fleet);
    }
}