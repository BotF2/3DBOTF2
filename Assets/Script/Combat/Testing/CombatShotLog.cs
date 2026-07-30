using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Combat.Testing
{
    /// <summary>
    /// Per-shot instrumentation for one combat turn. Beam and torpedo hits report here so
    /// CombatRecorder can persist shot-level timing/targeting data alongside the existing
    /// turn-level HP-delta summary — needed to diagnose imbalances that aggregate damage
    /// totals can't explain (e.g. whether one side's shots land earlier/more often, not just
    /// whether its per-shot damage is higher).
    /// </summary>
    public static class CombatShotLog
    {
        public static List<ShotRecord> Shots = new List<ShotRecord>();

        private static float turnStartTime;

        public static void BeginTurn()
        {
            Shots = new List<ShotRecord>();
            turnStartTime = Time.unscaledTime;
        }

        public static void LogShot(ShipController shooter, ShipController target, string weaponType, int damage, float distance, bool targetDestroyed)
        {
            if (shooter?.ShipData == null || target?.ShipData == null) return;

            Shots.Add(new ShotRecord
            {
                TimeInTurn = Time.unscaledTime - turnStartTime,
                ShooterName = shooter.ShipData.ShipName,
                ShooterID = shooter.ShipData.ShipID,
                ShooterCiv = shooter.ShipData.CivEnum.ToString(),
                TargetName = target.ShipData.ShipName,
                TargetID = target.ShipData.ShipID,
                TargetCiv = target.ShipData.CivEnum.ToString(),
                WeaponType = weaponType,
                Damage = damage,
                Distance = distance,
                TargetDestroyed = targetDestroyed
            });
        }

        /// <summary>
        /// Records a kill that arrived via ShipController.ApplyDestroyedFromServer (network
        /// reconciliation) rather than a locally-simulated weapon hit - see the call site for why
        /// this needs its own entry instead of a LogShot call.
        /// </summary>
        public static void LogReconciledKill(ShipController target)
        {
            if (target?.ShipData == null) return;

            Shots.Add(new ShotRecord
            {
                TimeInTurn = Time.unscaledTime - turnStartTime,
                ShooterName = "SERVER_RECONCILE",
                ShooterID = -1,
                ShooterCiv = "N/A",
                TargetName = target.ShipData.ShipName,
                TargetID = target.ShipData.ShipID,
                TargetCiv = target.ShipData.CivEnum.ToString(),
                WeaponType = "Reconciled",
                Damage = 0,
                Distance = 0f,
                TargetDestroyed = true
            });
        }
    }

    [System.Serializable]
    public class ShotRecord
    {
        public float TimeInTurn;
        public string ShooterName;
        public int ShooterID;
        public string ShooterCiv;
        public string TargetName;
        public int TargetID;
        public string TargetCiv;
        public string WeaponType;
        public int Damage;
        public float Distance;
        public bool TargetDestroyed;
    }
}
