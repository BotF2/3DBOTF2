using BOTF3D.GamePlay;
using System.Collections.Generic;
using UnityEngine;


namespace BOTF3D.Core
{
    public class CombatData
    {
        public CombatOrders OrderSideOne = CombatOrders.Engage;
        public CombatOrders OrderSideTwo = CombatOrders.Engage;
        public CivEnum CivEnumSideOne;
        public CivEnum CivEnumSideTwo;
        public CivController sideOneCiv;
        public CivController sideTwoCiv;
        public CombatType CombatType;
        public int CombatID;
        public List<ShipController> SideOneShipCons = new List<ShipController>();  // updated to current combat
        public List<ShipController> SideTwoShipCons = new List<ShipController>();

        public GameObject cameraEmpty;
        [SerializeField]
        private GameObject animFriend1;
        [SerializeField]
        private GameObject animFriend2;
        [SerializeField]
        private GameObject animFriend3;
        [SerializeField]
        private GameObject animEnemy1;
        [SerializeField]
        private GameObject animEnemy2;
        [SerializeField]
        private GameObject animEnemy3;
        // bool _isFriend; // true if friend, false if enemy
        int _scoutsFriend;
        int _destroyersFriend;
        int _capitalsFriend;
        int _transportsFriend;
        int _scoutsEnemy;
        int _destroyersEnemy;
        int _capitalsEnemy;
        int _transportsEnemy;
        int _totalScoutShips; // the total # of scouts in the list
        int _totalDestroyerShips;
        int _totalCapitalShips;
        int _totalTransportsShips;
        public int rotationOnY = 90; // face right

        public List<GameObject> CameraTargetList; // do not send directly to CameraMultiTarget, send to GameManager first
        private string[] arrayCountShipTypes; // change to array ship type
        private string[] arrayNames; //??? do we need this?

        public List<GameObject> combatShips; // for CameraMultiTarget to use for camera targets

        //public GameObject Friend_0; // prefab empty game object to clone instantiate into the grids
        //public GameObject Enemy_0;


        // ****** Use a running count of ships by type for shipGameOb starting locations, reset to zero on entering first enemy
        int _scoutShips = 0;
        int _destroyerShips = 0;
        int _capitalShips = 0;
        int _utilityShips = 0;

        int zLocation = 0;
        int _zScoutDepth = 0;
        int _zDestroyerDepth = 0;
        int _zCapitalDepth = 0;
        int _zUtilityDepth = 0;

        public CombatData()
        {
            OrderSideOne = CombatOrders.None;
            OrderSideTwo = CombatOrders.None;
            CivEnumSideOne = CivEnum.None;
            CivEnumSideTwo = CivEnum.None;
            sideOneCiv = null;
            sideTwoCiv = null;
            SideOneShipCons = new List<ShipController>();
            SideTwoShipCons = new List<ShipController>();
        }
    }
}
