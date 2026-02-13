using Assets.GamePlay;
using System.Collections.Generic;
using UnityEngine;


namespace Assets.Core

{
    [CreateAssetMenu(menuName = "Galaxy/FleetSO")]
    public class FleetSO : ScriptableObject
    {
        public int CivIndex;
        public Sprite Insignia;
        public CivEnum CivOwnerEnum;
        public Vector3 Location;
        public List<ShipController> ShipsList;
        public float MaxWarpFactor = 0f;
        public float CurrentWarpFactor = 0f;
        public string Name;
        public string Description;
        public GameObject Destination;
    }
}




