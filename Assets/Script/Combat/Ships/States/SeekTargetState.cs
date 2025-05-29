using Assets.Plugins.YAFSM;
using UnityEngine;

namespace Assets.SpaceCombat.AutoBattle.Scripts.Starships.States
{
    public class SeekTargetState : State
    {
        private StarshipController StarshipController => (StarshipController)Machine;

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            // If we are within weapons range, attack _destination
            var targetDistance = Vector3.Distance(StarshipController.CurrentTarget.transform.position, StarshipController.transform.position);
            if (targetDistance <= StarshipController.WeaponsManager.WeaponsRange)
            {
                StarshipController.ChangeState<EngageTargetState>();
            }
        }
    }
}