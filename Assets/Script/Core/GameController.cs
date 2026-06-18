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
        public bool AreWeLocalPlayer(CivEnum civ)
        {
            if (civ == this.GameData.LocalPlayerCivEnum)
                return true;
            else
                return false;
        }
    

        
        
        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameController>();
        }

        public void Cleanup() { }
    }
}