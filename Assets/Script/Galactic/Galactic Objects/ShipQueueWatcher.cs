using UnityEngine;

namespace Assets.GamePlay
{
    /// <summary>
    /// Watches the ship build queue UI grid for structural changes
    /// and notifies the owning StarSysController.
    /// </summary>
    public class ShipQueueWatcher : MonoBehaviour
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

            controller.GridShipQueueUpdate();
        }
    }
}


