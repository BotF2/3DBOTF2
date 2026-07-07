using BOTF3D.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Manages the combat queue system - ensures one combat at a time.
    /// Handles queuing, dequeuing, and sequential processing of combats.
    /// </summary>
    public class CombatQueueManager : IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        private Queue<PendingCombat> combatQueue = new Queue<PendingCombat>();
        private bool isProcessingCombat = false;
        private MonoBehaviour coroutineRunner;

        public CombatController ActiveCombatController { get; set; }
        public bool IsProcessingCombat => isProcessingCombat;
        public int QueueCount => combatQueue.Count;

        public CombatQueueManager(MonoBehaviour runner)
        {
            coroutineRunner = runner;
        }

        /// <summary>
        /// Request a combat - will be queued if another is active
        /// </summary>
        public void RequestCombat(List<ShipController> sideOneShips, List<ShipController> sideTwoShips, CombatType combatType, StarSysController starSysCon = null)
        {
            var pendingCombat = new PendingCombat
            {
                SideOneShips = sideOneShips,
                SideTwoShips = sideTwoShips,
                CombatType = combatType,
                StarSysCon = starSysCon
            };

            combatQueue.Enqueue(pendingCombat);
            Debug.Log($"⏸️ Combat queued. Total in queue: {combatQueue.Count}");

            // Start processing if not already doing so
            if (!isProcessingCombat)
            {
                coroutineRunner.StartCoroutine(ProcessCombatQueue());
            }
        }

        /// <summary>
        /// Process one combat at a time from the queue
        /// </summary>
        private IEnumerator ProcessCombatQueue()
        {
            isProcessingCombat = true;

            while (combatQueue.Count > 0)
            {
                var pendingCombat = combatQueue.Dequeue();
                Debug.Log($"🎮 Starting combat. Remaining in queue: {combatQueue.Count}");

                yield return null;

                // Create combat controller through callback
                CombatController combatController = null;
                yield return coroutineRunner.StartCoroutine(
                    OnRequestCombatController(pendingCombat, (controller) => combatController = controller)
                );

                if (combatController != null)
                {
                    combatController.CombatData.CombatType = pendingCombat.CombatType;
                    ActiveCombatController = combatController;

                    // Notify that setup should begin
                    OnCombatSetupNeeded?.Invoke();

                    // Wait for combat to finish
                    while (ActiveCombatController != null && !ActiveCombatController.isClosing)
                    {
                        yield return null;
                    }

                    Debug.Log("✅ Combat finished. Processing next in queue...");
                    yield return new WaitForSeconds(0.5f);
                }
            }

            isProcessingCombat = false;
            Debug.Log("✅ Combat queue empty.");
        }

        /// <summary>
        /// Coroutine to request combat controller creation
        /// </summary>
        private IEnumerator OnRequestCombatController(PendingCombat pendingCombat, System.Action<CombatController> callback)
        {
            // Wait one frame to ensure scene objects are fully initialized
            yield return null;

            // Request controller creation through event
            CombatController controller = null;
            OnCombatControllerRequested?.Invoke(pendingCombat.SideOneShips, pendingCombat.SideTwoShips, pendingCombat.StarSysCon, (c) => controller = c);

            callback?.Invoke(controller);
        }

        /// <summary>
        /// Called when a combat ends
        /// </summary>
        public void OnCombatEnded(CombatController controller)
        {
            Debug.Log($"🏁 Combat {controller.CombatID} ended");

            if (ActiveCombatController == controller)
            {
                ActiveCombatController = null;
            }
        }

        // Events for decoupling
        public event System.Action<List<ShipController>, List<ShipController>, StarSysController, System.Action<CombatController>> OnCombatControllerRequested;
        public event System.Action OnCombatSetupNeeded;

        /// <summary>
        /// Data structure for queued combats
        /// </summary>
        public class PendingCombat
        {
            public List<ShipController> SideOneShips;
            public List<ShipController> SideTwoShips;
            public CombatType CombatType;
            public StarSysController StarSysCon;
        }
    }
}
