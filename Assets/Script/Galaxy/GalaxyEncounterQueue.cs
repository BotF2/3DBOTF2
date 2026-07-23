using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;

namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Collects fleet/system encounter events that fire during TurnProgression
    /// and replays them one at a time during EncounterResolution (InterTurn).
    /// Attach to a persistent Galaxy scene GameObject.
    /// </summary>
    public class GalaxyEncounterQueue : MonoBehaviour, IManager
    {
        public void Initialize() { }
        public void Cleanup() { }

        public static GalaxyEncounterQueue Instance;

        private enum EncounterKind { FleetVsFleet, FleetVsSystem }

        private struct EncounterRecord
        {
            public EncounterKind Kind;
            public FleetController FleetA;
            public FleetController FleetB;      // null for FleetVsSystem
            public StarSysController StarSys;   // null for FleetVsFleet
        }

        private readonly List<EncounterRecord> pending = new List<EncounterRecord>();

        public bool HasPending => pending.Count > 0;
        public int PendingCount => pending.Count;

        private void Awake()
        {
            ServiceLocator.Register<GalaxyEncounterQueue>(this);
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void EnqueueFleetVsFleet(FleetController a, FleetController b)
        {
            if (a == null || b == null) return;

            // Deduplicate — same pair regardless of order
            if (pending.Exists(r =>
                r.Kind == EncounterKind.FleetVsFleet &&
                ((r.FleetA == a && r.FleetB == b) || (r.FleetA == b && r.FleetB == a))))
                return;

            pending.Add(new EncounterRecord { Kind = EncounterKind.FleetVsFleet, FleetA = a, FleetB = b });
            Debug.Log($"EncounterQueue: queued FleetVsFleet — {a.name} vs {b.name} ({pending.Count} pending)");
        }

        public void EnqueueFleetVsSystem(FleetController fleet, StarSysController sys)
        {
            if (fleet == null || sys == null) return;

            if (pending.Exists(r =>
                r.Kind == EncounterKind.FleetVsSystem && r.FleetA == fleet && r.StarSys == sys))
                return;

            pending.Add(new EncounterRecord { Kind = EncounterKind.FleetVsSystem, FleetA = fleet, StarSys = sys });
            Debug.Log($"EncounterQueue: queued FleetVsSystem — {fleet.name} at {sys.name} ({pending.Count} pending)");
        }

        /// <summary>
        /// Resolve and remove the next encounter in the queue.
        /// Call repeatedly (e.g. from a "Next" button on the diplomacy panel) until HasPending is false.
        /// </summary>
        public void ResolveNext()
        {
            if (pending.Count == 0) return;

            var record = pending[0];
            pending.RemoveAt(0);

            // Skip records whose actors were destroyed between queuing and resolution.
            // Resolution is broadcast to every client (see FleetController.ServerNotify*Encounter)
            // instead of calling DiplomacyManager directly here, since DrainAll only ever runs on the
            // server and DiplomacyManager is an unnetworked per-client singleton - calling it directly
            // here would only ever open the Diplomacy UI on the host's own screen.
            if (record.Kind == EncounterKind.FleetVsFleet)
            {
                if (record.FleetA != null && record.FleetB != null)
                    record.FleetA.ServerNotifyFleetVsFleetEncounter(record.FleetB);
                else if (HasPending)
                    ResolveNext();
            }
            else
            {
                if (record.FleetA != null && record.StarSys != null)
                    record.FleetA.ServerNotifyFleetVsSystemEncounter(record.StarSys);
                else if (HasPending)
                    ResolveNext();
            }
        }

        /// <summary>
        /// Drain the entire queue at once (called from TimeManager.ProcessTurnEvents).
        /// </summary>
        public void DrainAll()
        {
            while (HasPending)
                ResolveNext();
        }

        public void Clear() => pending.Clear();

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GalaxyEncounterQueue>();
            if (Instance == this) Instance = null;
        }
    }
}
