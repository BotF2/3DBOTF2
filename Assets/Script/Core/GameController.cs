using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    public class GameController : MonoBehaviour, IManager, IController
    {
        public void Initialize() { }
        public void UpdateState() { }
        /// //using Unity.Netcode; //********** install for Multiplayer???

        /// </summary>

        public static GameController Instance;
        private GameData gameData;
        public GameData GameData { get { return gameData; } set { gameData = value; } }

        public void Awake()
        {
            ServiceLocator.Register<GameController>(this);
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
        // GameData.LocalPlayerCivEnum is a cache written by LocalHumanPlayerController.OnPlayerCivChanged's
        // SyncVar hook, which has no ordering guarantee relative to other objects' hooks (e.g. a fleet's
        // own SyncedCivEnum hook - see FleetController). On a non-host client that race can leave the cache
        // stale/default at the moment something asks "is this mine?", misclassifying the local player's own
        // stuff as belonging to someone else. LocalPlayerController.PlayerCiv reads the SyncVar directly (its
        // backing field is already current by the time Mirror invokes any hook), and the reference itself is
        // set during OnStartLocalPlayer - always well before gameplay objects like fleets exist - so prefer it
        // whenever it's available, falling back to the cache only if that reference isn't set yet.
        public bool AreWeLocalPlayer(CivEnum civ)
        {
            LocalHumanPlayerController localPlayerCon = PlayerManager.Instance != null
                ? PlayerManager.Instance.LocalPlayerController
                : null;
            CivEnum ourCiv = localPlayerCon != null ? localPlayerCon.PlayerCiv : this.GameData.LocalPlayerCivEnum;
            return civ == ourCiv;
        }
    

        
        
        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameController>();
        }

        public void Cleanup() { }
    }
}