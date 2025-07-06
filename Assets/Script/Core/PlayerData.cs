using Assets.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    /// <summary>
    /// When a player issues a command(move, attack, etc.), validate that the player owns the unit before executing the command.
    /// </summary>
    public int PlayerId; // Unique for each player
    public string PlayerName;
    public List<GameObject> OwnedUnits = new List<GameObject>(); // star systems, fleets, ships, starbases, etc.
    public CivEnum Civ; // If each player is a civilization CivEnum, CivController, CivData, etc.
                        // Add more player-specific data as needed
    public NetworkVariable<string> NetworkPlayerName = new NetworkVariable<string>(string.Empty);
    public void Initialize(int id, string name)
    {
        PlayerId = id;
        NetworkPlayerName.Value = name;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkPlayerName.Value = $"Player_{OwnerClientId}";
            SpawnInitialFleet();
        }

        if (IsOwner)
        {
            Debug.Log($"Local player initialized: {NetworkPlayerName.Value}");
        }
    }

    void SpawnInitialFleet() // example of network Multiplayer spawning a fleet at a random position 
    {
        // Use a central spawn point or random location
        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10));
       // GameObject fleetInstance = Instantiate(GameManager.Instance.fleetPrefab, spawnPosition, Quaternion.identity);

        //FleetController fleet = fleetInstance.GetComponent<FleetController>();
        //fleet.ownerId = OwnerClientId;

        // Register fleet and spawn to network
        //ownedFleets.Add(fleet);
        //fleetInstance.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
    }

    // Called by UI or input to move a fleet
    [ServerRpc]
    public void RequestMoveFleetServerRpc(ulong fleetNetId, Vector3 destination)
    {
        NetworkObject netObj = NetworkManager.SpawnManager.SpawnedObjects[fleetNetId];

        //if (netObj.TryGetComponent(out FleetController fleet) && fleet.ownerId == OwnerClientId)
        //{
        //    //fleet.SetDestination(destination);
        //}
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