
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
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

