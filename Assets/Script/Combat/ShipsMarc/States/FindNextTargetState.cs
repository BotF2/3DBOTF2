using Assets.Plugins.YAFSM;
using System.Linq;
using UnityEngine;

namespace Assets.SpaceCombat.AutoBattle.Scripts.Starships.States
{
    public class FindNextTargetState : State
    {
        private StarshipController StarshipController => (StarshipController)Machine;

        public override void Enter()
        {
            base.Enter();

            StarshipController.CurrentTarget = FindNearestTarget();

            if (StarshipController.CurrentTarget != null)
            {
                StarshipController.ChangeState<SeekTargetState>();
            }
            else
            {
                StarshipController.ChangeState<IdleState>();
            }
        }

        private GameObject FindNearestTarget()
        {
            GameObject foundTarget = null;

            float minimumDistance = Mathf.Infinity;

            foreach (var availableTarget in StarshipController.AvailableTargets.Where(x => x.HitPoints > 0))
            {
                float distance = Vector3.Distance(availableTarget.transform.position, StarshipController.transform.position);

                if (distance < minimumDistance)
                {
                    foundTarget = availableTarget.gameObject;
                    minimumDistance = distance;
                }
            }

            return foundTarget;
        }
    }
}
