using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;
using Mirror;

namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Collects fleet/system encounter events that fire during the same physics tick and resolves
    /// them together at the end of that tick, grouped by connected component (see
    /// ProcessPendingForThisTick), so simultaneous convergence on the same system draws every
    /// participant into one shared decision instead of whichever collider fired first getting an
    /// initiative advantage. Attach to a persistent Galaxy scene GameObject.
    /// </summary>
    [DefaultExecutionOrder(100)] // after FleetController.FixedUpdate, so a tick's OnTriggerEnter calls are all queued first
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

        public void Clear() => pending.Clear();

        // Server-only: this queue's state (and the SyncVar counters it mutates) only means anything
        // authoritatively on the server. Non-host clients never populate `pending` in the first place
        // (EnqueueFleetVsFleet/EnqueueFleetVsSystem are only ever called from FleetController's
        // isServer-gated OnTriggerEnter branches), so this is a no-op on them regardless.
        private void FixedUpdate()
        {
            if (!NetworkServer.active) return;
            ProcessPendingForThisTick();
        }

        /// <summary>
        /// Resolves every encounter queued during this physics tick, grouped by connected component
        /// (union-find over shared fleets/systems) so a 3+-way simultaneous convergence freezes every
        /// participant before any single pair is resolved through the normal 2-party Diplomacy UI.
        /// </summary>
        public void ProcessPendingForThisTick()
        {
            if (pending.Count == 0) return;

            var records = new List<EncounterRecord>(pending);
            pending.Clear();

            // Union-find over participants (FleetController or StarSysController as Object keys).
            var parent = new Dictionary<Object, Object>();
            Object Find(Object x)
            {
                while (parent[x] != x)
                {
                    parent[x] = parent[parent[x]]; // path compression
                    x = parent[x];
                }
                return x;
            }
            void Union(Object a, Object b)
            {
                Object rootA = Find(a);
                Object rootB = Find(b);
                if (rootA != rootB) parent[rootA] = rootB;
            }
            void EnsureRegistered(Object x)
            {
                if (!parent.ContainsKey(x)) parent[x] = x;
            }

            foreach (var record in records)
            {
                if (record.Kind == EncounterKind.FleetVsFleet)
                {
                    if (record.FleetA == null || record.FleetB == null) continue;
                    EnsureRegistered(record.FleetA);
                    EnsureRegistered(record.FleetB);
                    Union(record.FleetA, record.FleetB);
                }
                else
                {
                    if (record.FleetA == null || record.StarSys == null) continue;
                    EnsureRegistered(record.FleetA);
                    EnsureRegistered(record.StarSys);
                    Union(record.FleetA, record.StarSys);
                }
            }

            // Group records by connected component root.
            var groups = new Dictionary<Object, List<EncounterRecord>>();
            foreach (var record in records)
            {
                Object anchor = record.FleetA;
                if (anchor == null) continue;
                if (!parent.ContainsKey(anchor)) continue; // was skipped above (null participant)

                Object root = Find(anchor);
                if (!groups.TryGetValue(root, out var list))
                {
                    list = new List<EncounterRecord>();
                    groups[root] = list;
                }
                list.Add(record);
            }

            foreach (var group in groups.Values)
            {
                // Freeze every participant in this component before resolving any pair within it -
                // this is what removes the "whichever collider fired first" initiative advantage.
                var participants = new HashSet<Object>();
                foreach (var record in group)
                {
                    participants.Add(record.FleetA);
                    if (record.Kind == EncounterKind.FleetVsFleet) participants.Add(record.FleetB);
                    // FleetVsSystem: the StarSysController side has no pending-encounter counter of
                    // its own (only FleetController does - a system can't "move"), so nothing to
                    // increment there.
                }
                foreach (var participant in participants)
                {
                    if (participant is FleetController fleetCon)
                        fleetCon.ServerIncrementPendingEncounters();
                }

                // Resolve each pair. For components of 3+ participants this runs pairwise, sequentially,
                // through the existing 2-party Diplomacy UI while the whole group stays frozen - no new
                // N-way UI (per confirmed scope).
                foreach (var record in group)
                {
                    if (record.Kind == EncounterKind.FleetVsFleet)
                        record.FleetA.ServerNotifyFleetVsFleetEncounter(record.FleetB);
                    else
                        record.FleetA.ServerNotifyFleetVsSystemEncounter(record.StarSys);
                }
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GalaxyEncounterQueue>();
            if (Instance == this) Instance = null;
        }
    }
}
