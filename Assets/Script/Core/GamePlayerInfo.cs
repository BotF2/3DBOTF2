using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    public enum PlayerType { Local, AI, Remote }

    /// <summary>
    /// Plain data class for player information. NOT a MonoBehaviour or NetworkBehaviour.
    /// For network sync, individual fields should be synced via PlayerController components.
    /// </summary>
    [System.Serializable]
    public class GamePlayerInfo
    {
        public int PlayerId;
        public string PlayerName;
        public PlayerType PlayerType;
        public CivEnum PlayerCiv;

        // Local-only tracking (not serialized for network)
        [System.NonSerialized]
        public List<GameObject> OwnedGameObjects = new List<GameObject>();

        public GamePlayerInfo()
        {
            PlayerName = string.Empty;
            PlayerId = -1;
            PlayerType = PlayerType.AI;
            PlayerCiv = CivEnum.FED;
            OwnedGameObjects = new List<GameObject>();
        }

        public GamePlayerInfo(string playerName)
        {
            this.PlayerName = playerName;
            PlayerId = -1;
            PlayerType = PlayerType.Local;
            PlayerCiv = CivEnum.FED;
            OwnedGameObjects = new List<GameObject>();
        }

        public GamePlayerInfo(int id, string name, PlayerType type, CivEnum civEnum)
        {
            PlayerId = id;
            PlayerName = name;
            PlayerType = type;
            PlayerCiv = civEnum;
            OwnedGameObjects = new List<GameObject>();
        }

        public void Initialize(int id, string name)
        {
            PlayerId = id;
            PlayerName = name;
        }
    }
}