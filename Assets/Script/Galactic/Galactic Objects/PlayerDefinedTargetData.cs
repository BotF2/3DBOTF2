using Assets.GamePlay;
using UnityEngine;

namespace Assets.Core
{
    public class PlayerDefinedTargetData
    {
        public Sprite Insignia;
        public CivEnum CivOwnerEnum;
        public FleetController FleetController;
        public Vector3 Position;
        public string CivShortName;
        public GalaxyObjectType GalaxyObjectType = GalaxyObjectType.TargetDestination;
        public string Name;
        public string Description;

        public PlayerDefinedTargetData(string name)
        {
            Name = name;
        }
        public PlayerDefinedTargetData()
        {

        }
    }
}

