using Assets.Core;
using UnityEngine;


public class GameController : MonoBehaviour
{
    /// //using Unity.Netcode; //********** install for Multiplayer???

    /// </summary>

    public static GameController Instance;
    private GameData gameData;
    public GameData GameData { get { return gameData; } set { gameData = value; } }

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
