//csharp Assets/Script/Utils/PrefabPool.cs
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Core.Utils
{
    /// <summary>
    /// Minimal prefab pool for Component prefabs. Keeps instances inactive under a pool parent when released.
    /// Designed for low-risk incremental replacement of Instantiate/Destroy.
    /// </summary>
    /// <typeparam name="T">Component type on the prefab (e.g. FleetController)</typeparam>
    public class PrefabPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _poolParent;
        private readonly Stack<T> _stack = new Stack<T>();

        public PrefabPool(T prefab, Transform poolParent = null)
        {
            _prefab = prefab;
            if (poolParent != null)
            {
                _poolParent = poolParent;
            }
            else
            {
                var go = new GameObject(prefab != null ? prefab.name + "_Pool" : "PrefabPool");
                _poolParent = go.transform;
                // Keep pool object out of scene view by default (optional)
#if UNITY_EDITOR
                go.hideFlags = HideFlags.HideInHierarchy;
#endif
            }
        }

        public T Get()
        {
            T instance;
            if (_stack.Count > 0)
            {
                instance = _stack.Pop();
                instance.gameObject.SetActive(true);
            }
            else
            {
                instance = Object.Instantiate(_prefab, Vector3.zero, Quaternion.identity, _poolParent);
                instance.gameObject.name = _prefab.gameObject.name;
            }

            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null) return;

            // Reset common transform state — keep world position but reparent under pool parent
            instance.transform.SetParent(_poolParent, true);
            instance.gameObject.SetActive(false);

            _stack.Push(instance);
        }
    }
}