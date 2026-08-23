using System.Collections;
using System.Collections.Generic;
using BOTF3D.Core;
using BOTF3D.Civilization;
using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Galaxy
{
    public enum TurnEventType
    {
        UninhabitedHabitable,
        UninhabitedTerraformable,
        UninhabitedNonHabitable,
        DiplomacyEncounter
    }

    public struct TurnEvent
    {
        public TurnEventType Type;
        public FleetController Fleet;
        public StarSysController System;
        public System.Action ShowAction; // non-null for DiplomacyEncounter
    }

    /// <summary>
    /// Collects contact events during TurnProgression and presents them to the local player one at
    /// a time at the start of InterTurn. The Advance Turn button stays grayed until the queue is
    /// empty. Handles uninhabited-system contacts (three subtypes) and diplomacy encounters
    /// (fleet-vs-fleet / fleet-vs-system). Combat bypasses this queue — it has its own
    /// time-pause authority via CombatQueueManager.
    /// </summary>
    public class TurnEventQueue : MonoBehaviour
    {
        public static TurnEventQueue Instance;

        private readonly Queue<TurnEvent> _queue = new Queue<TurnEvent>();
        private bool _isDraining;
        private bool _waitingForDismiss;

        /// <summary>True when there are no queued events and the drain coroutine is not running.</summary>
        public bool IsDrained => _queue.Count == 0 && !_isDraining;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
        }

        /// <summary>Records a contact event for InterTurn presentation.</summary>
        public void Enqueue(TurnEvent evt)
        {
            _queue.Enqueue(evt);
            Debug.Log($"[TurnEventQueue] Enqueued {evt.Type} for '{evt.System?.StarSysData?.SysName}' — queue size now {_queue.Count}");
        }

        /// <summary>Called by popup close methods to unblock the drain coroutine.</summary>
        public void NotifyDismissed()
        {
            _waitingForDismiss = false;
        }

        private void OnTurnPhaseChanged(TurnPhase phase)
        {
            if (phase == TurnPhase.InterTurn)
                StartDraining();
        }

        private void StartDraining()
        {
            if (_queue.Count == 0)
            {
                GameEvents.TurnEventQueueDrained();
                return;
            }
            if (!_isDraining)
                StartCoroutine(DrainCoroutine());
        }

        private IEnumerator DrainCoroutine()
        {
            _isDraining = true;
            while (_queue.Count > 0)
            {
                var evt = _queue.Dequeue();
                if (!IsValid(evt))
                {
                    Debug.Log($"[TurnEventQueue] Skipping stale {evt.Type} event — fleet left or system claimed.");
                    continue;
                }
                _waitingForDismiss = true;
                ShowEvent(evt);
                yield return new WaitUntil(() => !_waitingForDismiss);
            }
            _isDraining = false;
            Debug.Log("[TurnEventQueue] Queue drained — enabling Advance Turn.");
            GameEvents.TurnEventQueueDrained();
        }

        private bool IsValid(TurnEvent evt)
        {
            int firstUninhabited = (int)CivEnum.ZZUNINHABITED1;
            switch (evt.Type)
            {
                case TurnEventType.UninhabitedHabitable:
                    return evt.Fleet != null
                        && evt.Fleet.FleetData?.ColonizableSystem == evt.System
                        && (int)evt.System.StarSysData.CurrentOwnerCivEnum >= firstUninhabited;
                case TurnEventType.UninhabitedTerraformable:
                    return evt.Fleet != null
                        && evt.Fleet.FleetData?.TerraformableSystem == evt.System
                        && (int)evt.System.StarSysData.CurrentOwnerCivEnum >= firstUninhabited;
                case TurnEventType.UninhabitedNonHabitable:
                    return evt.System != null
                        && (int)evt.System.StarSysData.CurrentOwnerCivEnum >= firstUninhabited;
                case TurnEventType.DiplomacyEncounter:
                    return evt.ShowAction != null;
                default:
                    return false;
            }
        }

        private void ShowEvent(TurnEvent evt)
        {
            switch (evt.Type)
            {
                case TurnEventType.UninhabitedHabitable:
                case TurnEventType.UninhabitedNonHabitable:
                    HabitableSysUIController.Instance?.LoadHabitableSysUI(evt.System, evt.Fleet.FleetData.CivController);
                    GalaxyMenuUIController.Instance.OpenMenu(Menu.AFleetMenu, evt.Fleet.gameObject);
                    break;
                case TurnEventType.UninhabitedTerraformable:
                    TerraformableSysUIController.Instance?.LoadTerraformableSysUI(evt.System, evt.Fleet.FleetData.CivController);
                    GalaxyMenuUIController.Instance.OpenMenu(Menu.AFleetMenu, evt.Fleet.gameObject);
                    break;
                case TurnEventType.DiplomacyEncounter:
                    evt.ShowAction?.Invoke();
                    break;
            }
        }
    }
}
