using UnityEngine;

namespace BOTF3D.Plugins.YAFSM
{
    public interface IState
    {
        bool IsActive { get; }
        IMachineBehaviour Machine { get; }

        void Initialize();

        void Enter();

        void CustomExecute();
        void Update();
        void FixedUpdate();
        void LateUpdate();

        void Exit();

        void OnCollisionEnter(Collision collision);
        void OnCollisionStay(Collision collision);
        void OnCollisionExit(Collision collision);

        void OnTriggerEnter(Collider collider);
        void OnTriggerStay(Collider collider);
        void OnTriggerExit(Collider collider);

        void OnAnimatorIK(int layerIndex);

        T GetMachine<T>() where T : IMachineBehaviour;
    }
}