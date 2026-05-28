using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
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


