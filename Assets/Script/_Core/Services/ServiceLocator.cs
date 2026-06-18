using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Core
{
    /// <summary>
    /// Simple service locator for dependency injection
    /// Provides centralized access to manager instances without static singletons
    /// Usage: ServiceLocator.Register<ManagerType>(instance) to add
    ///        ServiceLocator.Get<ManagerType>() to retrieve
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator instance;

        private static ServiceLocator Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<ServiceLocator>();
                    if (instance == null)
{
                        GameObject go = new GameObject("ServiceLocator");
                        instance = go.AddComponent<ServiceLocator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Register a service instance
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            Type serviceType = typeof(T);

            if (Instance.services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"Service {serviceType.Name} is already registered. Overwriting...");
                Instance.services[serviceType] = service;
            }
            else
            {
                Instance.services.Add(serviceType, service);
                GameLogger.Log(GameLogger.LogCategory.General, $"Registered service: {serviceType.Name}");
            }
        }

        /// <summary>
        /// Get a registered service instance
        /// </summary>
        public static T Get<T>() where T : class
        {
            Type serviceType = typeof(T);

            if (Instance.services.TryGetValue(serviceType, out object service))
            {
                return service as T;
            }

            GameLogger.LogWarning(GameLogger.LogCategory.General, $"Service {serviceType.Name} not found in ServiceLocator");
            return null;
        }

        /// <summary>
        /// Try to get a service without warnings if it doesn't exist
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            service = null;
            Type serviceType = typeof(T);

            if (Instance.services.TryGetValue(serviceType, out object foundService))
            {
                service = foundService as T;
                return service != null;
            }

            return false;
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            if (instance == null) return;

            Type serviceType = typeof(T);

            if (Instance.services.ContainsKey(serviceType))
            {
                Instance.services.Remove(serviceType);
                GameLogger.Log(GameLogger.LogCategory.General, $"Unregistered service: {serviceType.Name}");
            }
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            return Instance.services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Clear all registered services - use with caution
        /// </summary>
        public static void ClearAll()
        {
            Instance.services.Clear();
            GameLogger.Log(GameLogger.LogCategory.General, "ServiceLocator cleared all services");
        }
    }
}
