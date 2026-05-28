using System;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
{
    public class UpDownFleetWarpEvents : MonoBehaviour
    {
        public static UpDownFleetWarpEvents current;

        public Action<FleetController, string> FleetOnWarpUpClick;

        private void Awake()
        {
            if (current != null) { Destroy(gameObject); }
            else
            {
                current = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        private void Start()
        {
            FleetOnWarpUpClick += DoFleetOnWarpUp;
        }
        public void DoFleetOnWarpUp(FleetController fleetCon, string name)
        {
            if (FleetOnWarpUpClick != null)
            {
                FleetOnWarpUpClick?.Invoke(fleetCon, name);
            }
        }
    }
}
