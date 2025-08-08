using Assets.Core;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditorInternal;

public class GameController : MonoBehaviour
{
    /// //using Unity.Netcode; //********** install for Multiplayer???
    // Move the AreWeLocalPlayer check into a check if NetworkObject.OwnerClientId == NetworkManager.Singleton.LocalClientId 
    /// <summary>
    /// TO DO Steps after install:
    // 1. Add the NetworkObject component to your civ (player) prefab.
    // 2. use this.AreWeLocalPlayer() to do 3.
    // 3. Check if a NetworkObject belongs to the local player by comparing the NetworkObject.OwnerClientId with NetworkManager.Singleton.LocalClientId.
    /// </summary>

    public static GameController Instance;
    private GameData gameData;
    public GameData GameData { get { return gameData; } set { gameData = value; } }
    //private List<IPlayerController> players = new List<IPlayerController>();

    public void Awake()
    {
        gameData = new GameData();

        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
       
    }
    public void Start()
    {
        if (GameManager.Instance != null)
        GameManager.Instance.GameController = this;
        //// Assign player instances (either via inspector or dynamically)
        //players.Add(FindFirstObjectByType<LocalHumanPlayerController>());
        //players.Add(FindFirstObjectByType<AiPlayerController>());
        //players.Add(FindFirstObjectByType<RemoteHumanPlayerController>());

        //foreach (var player in players)
        //{
        //    player.GiveCombatOrder(CombatOrders.Engage);// default combat order
        //    player.GiveDiplomacyOrder(NegotiationPloysEnum.OfferTrade);
        //    player.GiveIntelOrder(SecretActionsEnum.GatherIntelligence);
        //}
    }
    
    public bool DoWeBelongToLocalPlayer(GameObject go)
    {
        // get NetworkObject from go and see if it belongs to the local player by comparing the NetworkObject.OwnerClientId with NetworkManager.Singleton.LocalClientId.
        return true;
        /// ****** Need to use either NetCode to set NetworkManager.Singleton.LocalClientId.
        /// So we can check network objects by comparing the NetworkObject.OwnerClientId with NetworkManager.Singleton.LocalClientId.
        /// currently GameController.GameData hold Local Player selected by useres on each PC 
    }
    public bool AreWeLocalPlayer(CivEnum civ)
    {
        if (civ == this.GameData.LocalPlayerCivEnum)
            return true;
        else
            return false;
    }
}
