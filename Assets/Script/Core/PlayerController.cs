using UnityEngine;
using Assets.Core;

public class PlayerController : MonoBehaviour
{ /// <summary>
  /// *** PlayerController Will we need this?????
  /// PlayerManager has a list of PlayerData, not PlayerController
  /// </summary>

    public PlayerData PlayerData; // Reference to the player's data
    private void Start()
    {
        //// Initialize player data here or through a network call
        //PlayerData = new PlayerData();
        //PlayerData.Initialize(1, "Player1"); // Example initialization, us a prefab?
        //PlayerManager.Instance.AddPlayer(this); // Register this player with the PlayerManager
    }
    // Add methods to manage player actions, like adding units, changing name, etc.
    
}
