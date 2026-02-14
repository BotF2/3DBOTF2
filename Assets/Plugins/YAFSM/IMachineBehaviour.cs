using System;
using UnityEngine;

namespace BOTF3D.Plugins.YAFSM
{
    public interface IMachineBehaviour
    {
        string Name { get; }

        void SetInitialState<T>() where T : IState;
        void SetInitialState(Type type);

        void ChangeState<T>() where T : IState;
        void ChangeState(Type type);

        bool IsCurrentState<T>() where T : IState;
        bool IsCurrentState(Type type);

        T CurrentState<T>() where T : IState;
        T PreviousState<T>() where T : IState;
        T GetState<T>() where T : IState;

        void AddState<T>() where T : IState, new();
        void AddState(Type type);

        bool IsPreviousState<T>() where T : IState;

        void RemoveState<T>() where T : IState;
        void RemoveState(Type type);

        bool ContainsState<T>() where T : IState;
        bool ContainsState(Type type);

        void OnCollisionEnter(Collision collision);
        void OnCollisionStay(Collision collision);
        void OnCollisionExit(Collision collision);

        void OnTriggerEnter(Collider collider);
        void OnTriggerStay(Collider collider);
        void OnTriggerExit(Collider collider);

        void RemoveAllStates();
    }
}