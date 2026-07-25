using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;

namespace BOTF3D.Combat
{
    public class CombatData
    {
        public CombatOrders OrderSideOne = CombatOrders.Engage;
        public CombatOrders OrderSideTwo = CombatOrders.Engage;
        public CivEnum CivEnumSideOne;
        public CivEnum CivEnumSideTwo;
        public CivController sideOneCiv;
        public CivController sideTwoCiv;
        // Set only for SystemVsFleet/FleetVsSystem combats; null for FleetVsFleet. Lets post-combat
        // resolution (e.g. Borg assimilation) know which system is being fought over.
        public StarSysController StarSysCon;
        public CombatOrders SideOneOrder;
        public CombatOrders SideTwoOrder;
        public CombatType CombatType;
        public int CombatID;
        public List<ShipController> SideOneShipCons = new List<ShipController>();  // updated to current combat
        public List<ShipController> SideTwoShipCons = new List<ShipController>();
        public List<ShipController> CapturedShips = new List<ShipController>(); // Ships captured this combat (for post-combat rewards)
        public List<ShipData> DestroyedShips = new List<ShipData>(); // ShipData snapshots of ships destroyed this combat

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

        private string[] arrayCountShipTypes; // change to array ship type
        private string[] arrayNames; //??? do we need this?

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
        internal float SideOneSpeedMultiplier;
        internal float SideTwoSpeedMultiplier;

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
