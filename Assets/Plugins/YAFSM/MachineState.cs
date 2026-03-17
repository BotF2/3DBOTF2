using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Plugins.YAFSM
{
    [System.Serializable]
    public abstract class MachineState : State, IMachineBehaviour
    {
        public string Name { get; internal set; }
        private IState _currentState;
        private IState _nextState;
        private IState _initialState;
        private IState _previousState;

        private Dictionary<Type, IState> _states = new();

        private bool _onEnter;
        private bool _onExit;

        public abstract void AddStates();

        public override void Initialize()
        {
            base.Initialize();

            Name = Machine.Name + "." + GetType().ToString();

            AddStates();

            _currentState = _initialState;
            _previousState = _initialState;
            if (null == _currentState)
            {
                throw new System.Exception("\n" + Name + ".nextState is null on Initialize()!\tDid you forget to call SetInitialState()?\n");
            }

            foreach (var (_, value) in _states)
            {
                value.Initialize();
            }

            _onEnter = true;
            _onExit = false;
        }

        public override void Update()
        {
            base.Update();

            if (_onExit)
            {
                _currentState.Exit();
                _currentState = _nextState;
                _nextState = null;

                _onEnter = true;
                _onExit = false;
            }

            if (_onEnter)
            {
                _currentState.Enter();

                _onEnter = false;
            }

            try
            {
                _currentState.Update();
            }
            catch (NullReferenceException e)
            {
                if (null == _initialState)
                {
                    throw new Exception("\n" + Name + ".currentState is null when calling Update()!\tDid you set initial state?\n" + e.Message);
                }
                throw new Exception("\n" + Name + ".currentState is null when calling Update()!\tDid you change state to a valid state?\n" + e.Message);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!(_onEnter && _onExit))
            {
                try
                {
                    _currentState.FixedUpdate();
                }
                catch (NullReferenceException e)
                {
                    if (null == _initialState)
                    {
                        throw new System.Exception("\n" + Name + ".currentState is null when calling Update()!\tDid you set initial state?\n" + e.Message);
                    }
                    throw new System.Exception("\n" + Name + ".currentState is null when calling Update()!\tDid you change state to a valid state?\n" + e.Message);
                }
            }
        }

        public override void LateUpdate()
        {
            base.LateUpdate();

            if (!(_onEnter && _onExit))
            {
                try
                {
                    if (_nextState == null)
                    {
                        _currentState.LateUpdate();
                    }
                }
                catch (System.NullReferenceException e)
                {
                    if (null == _initialState)
                    {
                        throw new System.Exception("\n" + Name + ".currentState is null when calling Update()!\tDid you set initial state?\n" + e.Message);
                    }
                    throw new System.Exception("\n" + Name + ".currentState is null when calling Update()!\tDid you change state to a valid state?\n" + e.Message);
                }
            }
        }

        public override void OnCollisionEnter(Collision collision) { _currentState.OnCollisionEnter(collision); }
        public override void OnCollisionStay(Collision collision) { _currentState.OnCollisionStay(collision); }
        public override void OnCollisionExit(Collision collision) { _currentState.OnCollisionExit(collision); }

        public override void OnTriggerEnter(Collider collider) { _currentState.OnTriggerEnter(collider); }
        public override void OnTriggerStay(Collider collider) { _currentState.OnTriggerStay(collider); }
        public override void OnTriggerExit(Collider collider) { _currentState.OnTriggerExit(collider); }

        public override void OnAnimatorIK(int layerIndex)
        {
            base.OnAnimatorIK(layerIndex);

            if (!(_onEnter && _onExit))
            {
                try
                {
                    _currentState.OnAnimatorIK(layerIndex);
                }
                catch (NullReferenceException e)
                {
                    if (null == _initialState)
                    {
                        throw new Exception("\n" + Name + ".currentState is null when calling Update()!\tDid you set initial state?\n" + e.Message);
                    }
                    throw new Exception("\n" + Name + ".currentState is null when calling Update()!\tDid you change state to a valid state?\n" + e.Message);
                }
            }
        }

        public void SetInitialState<T>() where T : IState { _initialState = _states[typeof(T)]; }
        public void SetInitialState(Type type) { _initialState = _states[type]; }

        public void ChangeState<T>() where T : IState { ChangeState(typeof(T)); }
        public void ChangeState(Type type)
        {
            if (_nextState != null)
            {
                throw new Exception(Name + " is already changing states, you must wait to call ChangeState()!\n");
            }

            try
            {
                _previousState = _currentState;
                _nextState = _states[type];
            }
            catch (System.Collections.Generic.KeyNotFoundException e)
            {
                throw new System.Exception("\n" + Name + ".ChangeState() cannot find the state in the machine!\tDid you add the state you are trying to change to?\n" + e.Message);
            }

            _onExit = true;
        }

        public bool IsCurrentState<T>() where T : IState
        {
            if (_currentState.GetType() == typeof(T))
            {
                return true;
            }

            return false;
        }

        public bool IsCurrentState(Type type)
        {
            if (_currentState.GetType() == type)
            {
                return true;
            }

            return false;
        }

        public void AddState<T>() where T : IState, new()
        {
            if (!ContainsState<T>())
            {
                IState item = new T();
                ((State)item).Machine = this;

                _states.Add(typeof(T), item);
            }
        }
        public void AddState(Type type)
        {
            if (!ContainsState(type))
            {
                State item = (State)Activator.CreateInstance(type);
                item.Machine = this;

                _states.Add(type, item);
            }
        }

        public void RemoveState<T>() where T : IState { _states.Remove(typeof(T)); }
        public void RemoveState(Type type) { _states.Remove(type); }

        public bool ContainsState<T>() where T : IState { return _states.ContainsKey(typeof(T)); }
        public bool ContainsState(Type type) { return _states.ContainsKey(type); }

        public void RemoveAllStates() { _states.Clear(); }

        public T CurrentState<T>() where T : IState { return (T)_currentState; }

        public T PreviousState<T>() where T : IState { return (T)_previousState; }

        public bool IsPreviousState<T>() where T : IState => _previousState.GetType() == typeof(T);

        public T GetState<T>() where T : IState { return (T)_states[typeof(T)]; }
    }
}