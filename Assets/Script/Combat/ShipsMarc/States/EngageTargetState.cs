using Assets.Plugins.YAFSM;
using UnityEngine;

namespace Assets.SpaceCombat.AutoBattle.Scripts.Starships.States
{
    public class EngageTargetState : State
    {
        private StarshipController _starshipTarget;
        private StarshipController StarshipController => (StarshipController)Machine;

        public override void Update()
        {
            base.Update();

            StarshipController.WeaponsManager.FireEverything(StarshipController.CurrentTarget.transform);

            _starshipTarget = StarshipController.CurrentTarget.GetComponent<StarshipController>();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            StarshipController.Steering.SetDesiredVelocity(StarshipController.Steering.MaxVelocity);

            var targetDistance = Vector3.Distance(StarshipController.CurrentTarget.transform.position, StarshipController.transform.position);
            if (targetDistance > StarshipController.WeaponsManager.WeaponsRange)
            {
                StarshipController.ChangeState<SeekTargetState>();
            }

            if (_starshipTarget.HitPoints <= 0)
            {
                StarshipController.ChangeState<FindNextTargetState>();
            }
        }

        public override void Exit()
        {
            base.Exit();

            StarshipController.WeaponsManager.CeaseFire();
        }
    }
}
