using Assets.Plugins.YAFSM;
using UnityEngine;

namespace Assets.SpaceCombat.AutoBattle.Scripts.Starships.States
{
    public class GotoWaypointState : State
    {
        private int _waypointCount;
        private GameObject _waypointsGameObject;
        private int _waypointIndex;
        private StarshipController StarshipController => (StarshipController)Machine;


        public override void Enter()
        {
            base.Enter();

            _waypointsGameObject = GameObject.Find("Waypoints");
            _waypointCount = _waypointsGameObject.transform.childCount;
        }

        public override void Update()
        {
            base.Update();

            var waypoint = _waypointsGameObject.transform.GetChild(_waypointIndex);

            float distance = Vector3.Distance(waypoint.position, StarshipController.transform.position);

            if (distance < 1)
            {
                //_waypointIndex++;
                //if (_waypointIndex > _waypointCount)
                //{
                //    _waypointIndex = 0;
                //}

                _waypointIndex = (_waypointIndex + 1) % _waypointCount;
            }

            StarshipController.CurrentTarget = waypoint.gameObject;
        }
    }
}
