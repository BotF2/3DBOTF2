using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Plugins.YAFSM
{
    public abstract class MachineBehaviour : MonoBehaviour, IMachineBehaviour
    {
        public string Name => name;
        private IState _currentState;
        private IState _nextState;
        private IState _initialState;
        private IState _previousState;

        private bool _onEnter;
        private bool _onExit;

        private readonly Dictionary<Type, IState> _states = new();

        protected abstract void AddStates();

        public virtual void Awake()
        {
            //Initialize();
        }

        public virtual void Start()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            AddStates();

            _currentState = _initialState;
            _previousState = _initialState;
            if (null == _currentState)
            {
                throw new Exception("\n" + name + ".nextState is null on Initialize()!\tDid you forget to call SetInitialState()?\n");
            }

            foreach (var pair in _states)
            {
                pair.Value.Initialize();
            }

            _onEnter = true;
            _onExit = false;
        }

        public virtual void Custom()
        {
            if (!(_onEnter && _onExit))
            {
                try
                {
                    _currentState.CustomExecute();
                }
                catch (NullReferenceException e)
                {
                    if (null == _initialState)
                    {
                        throw new Exception("\n" + name + ".currentState is null when calling CustomExecute()!\tDid you set initial state?\n" + e);
                    }
                    else
                    {
                        throw new Exception("\n" + name + ".currentState is null when calling CustomExecute()!\tDid you change state to a valid state?\n" + e);
                    }
                }
            }
        }

        public virtual void Update()
        {
            if (_currentState == null)
            {
                return;
            }
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
                    throw new Exception("\n" + name + ".currentState is null when calling Update()!\tDid you set initial state?\n" + e.Message);
                }
                throw new Exception("\n" + name + ".currentState is null when calling Update()!\tDid you change state to a valid state?\n" + e);
            }
        }

        public virtual void FixedUpdate()
        {
            if (!(_onEnter && _onExit))
            {
                try
                {
                    if (_currentState != null && _nextState == null)
                    {
                        _currentState.FixedUpdate();
                    }
                }
                catch (NullReferenceException e)
                {
                    if (null == _initialState)
                    {
                        throw new Exception("\n" + name + ".currentState is null when calling Update()!\tDid you set initial state?\n" + e.Message);
                    }
                    throw new Exception("\n" + name + ".currentState is null when calling Update()!\tDid you change state to a valid state?\n" + e.Message);
                }
            }
        }

        public virtual void LateUpdate()
        {
            if (!(_onEnter && _onExit))
            {
                try
                {
                    if (_currentState != null && _nextState == null)
                    {
                        _currentState.LateUpdate();
                    }
                }
                catch (NullReferenceException e)
                {
                    if (null == _initialState)
                    {
                        throw new Exception("\n" + name + ".currentState is null when calling Update()!\tDid you set initial state?\n" + e);
                    }
                    throw new Exception("\n" + name + ".currentState is null when calling Update()!\tDid you change state to a valid state?\n" + e);
                }
            }
        }

        public virtual void OnCollisionEnter(Collision collision) { _currentState.OnCollisionEnter(collision); }
        public virtual void OnCollisionStay(Collision collision) { _currentState.OnCollisionStay(collision); }
        public virtual void OnCollisionExit(Collision collision) { _currentState.OnCollisionExit(collision); }

        public virtual void OnTriggerEnter(Collider collider) { _currentState.OnTriggerEnter(collider); }
        public virtual void OnTriggerStay(Collider collider) { _currentState.OnTriggerStay(collider); }
        public virtual void OnTriggerExit(Collider collider) { _currentState.OnTriggerExit(collider); }

        public void OnAnimatorIK(int layerIndex)
        {
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
                        throw new Exception("\n" + name + ".currentState is null when calling Update()!\tDid you set initial state?\n" + e.Message);
                    }
                    throw new Exception("\n" + name + ".currentState is null when calling Update()!\tDid you change state to a valid state?\n" + e.Message);
                }
            }
        }

        public void SetInitialState<T>() where T : IState { _initialState = _states[typeof(T)]; }

        public void SetInitialState(Type type)
        {
            _initialState = _states[type];
            //Debug.Log($"Setting initial state to {initialState}");
        }

        public void ChangeState<T>() where T : IState { ChangeState(typeof(T)); }
        public void ChangeState(Type type)
        {
            //if (currentState != null && currentState.GetType() == type)
            //{
            //    Debug.LogWarning("Trying to change to en existing state");
            //    // Stop setting the same state over and over agian
            //    return;
            //    //throw new System.Exception(name + " is already changing states, you must wait to call ChangeState()!\n");
            //}

            try
            {
                _previousState = _currentState;
                _nextState = _states[type];
                //Debug.Log($"Changing state to {nextState}");
            }
            catch (KeyNotFoundException e)
            {
                throw new Exception("\n" + name + ".ChangeState() cannot find state " + type + " in the machine!\tDid you add the state you are trying to change to?\n" + e);
            }

            _onExit = true;
        }

        public bool IsCurrentState<T>() where T : IState
        {
            if (_currentState == null)
            {
                return false;
            }
            return _currentState.GetType() == typeof(T);
        }

        public bool IsCurrentState(Type type)
        {
            return _currentState.GetType() == type;
        }

        public bool IsPreviousState<T>() where T : IState
        {
            return _previousState.GetType() == typeof(T);
        }

        public bool IsPreviousState(Type type)
        {
            return _previousState.GetType() == type;
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

        public T GetState<T>() where T : IState { return (T)_states[typeof(T)]; }
    }
}