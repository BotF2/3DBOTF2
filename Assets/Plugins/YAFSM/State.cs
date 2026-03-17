using System;
using UnityEngine;

namespace BOTF3D.Plugins.YAFSM
{
    [Serializable]
    public abstract class State : IState
    {
        public IMachineBehaviour Machine { get; internal set; }
        public bool IsActive => Machine.IsCurrentState(GetType());

        public virtual void CustomExecute() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void LateUpdate() { }

        public virtual void OnCollisionEnter(Collision collision) { }
        public virtual void OnCollisionStay(Collision collision) { }

        public virtual void OnCollisionExit(Collision collision) { }

        public virtual void OnTriggerEnter(Collider collider) { }
        public virtual void OnTriggerStay(Collider collider) { }
        public virtual void OnTriggerExit(Collider collider) { }

        public virtual void OnAnimatorIK(int layerIndex) { }

        public virtual void Initialize() { }

        public virtual void Enter() { }

        public virtual void Exit() { }

        public T GetMachine<T>() where T : IMachineBehaviour
        {
            try
            {
                return (T)Machine;
            }
            catch (InvalidCastException invalidCastException)
            {
                if (typeof(T) == typeof(MachineState) || typeof(T).IsSubclassOf(typeof(MachineState)))
                {
                    throw new Exception(Machine.Name + ".GetMachine() cannot return the type you requested!\tYour machine is derived from MachineBehaviour not MachineState!" + invalidCastException.Message);
                }
                if (typeof(T) == typeof(MachineBehaviour) || typeof(T).IsSubclassOf(typeof(MachineBehaviour)))
                {
                    throw new Exception(Machine.Name + ".GetMachine() cannot return the type you requested!\tYour machine is derived from MachineState not MachineBehaviour!" + invalidCastException.Message);
                }
                throw new Exception(Machine.Name + ".GetMachine() cannot return the type you requested!\n" + invalidCastException.Message);
            }
        }
    }
}