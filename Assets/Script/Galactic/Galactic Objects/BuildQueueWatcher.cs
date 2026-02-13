using Assets.GamePlay;
using UnityEngine;

namespace Assets.Core
{
    /// <summary>
    /// Watches the facility build queue UI grid for structural changes
    /// and notifies the owning StarSysController.
    /// </summary>
    public class BuildQueueWatcher : MonoBehaviour
    {
        private StarSysController controller;

        // Called explicitly after UI instantiation
        public void Initialize(StarSysController owner)
        {
            controller = owner;
        }

        private void OnTransformChildrenChanged()
        {
            if (controller == null)
                return;

            controller.GridFactoryQueueUpdate();
        }
    }
}


