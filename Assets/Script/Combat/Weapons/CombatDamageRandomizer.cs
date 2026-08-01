using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Shared per-hit damage variance for BeamWeapon and Torpedo. Ship stats (ShipStatCalculator)
    /// are now calibrated so the four major civs' fleets are within ~±3% of each other in total
    /// power - without any per-hit randomness, a mirror-composition fight between two majors would
    /// still resolve almost deterministically turn after turn (the only variance was in shot timing
    /// and AI order choice), so the "better" side by a hair would win essentially every time. This
    /// adds a modest, symmetric roll so an evenly matched fight can genuinely go either way, while
    /// staying small enough that a real numbers/type advantage (see conversation - "do not make
    /// these random features so strong that a clear ship advantage is significantly overcome by
    /// random chance") isn't at risk of being overturned by variance alone.
    /// </summary>
    public static class CombatDamageRandomizer
    {
        private const float VarianceFraction = 0.10f; // ±10% per hit

        public static int ApplyVariance(int baseDamage)
        {
            if (baseDamage <= 0) return baseDamage;

            float roll = Random.Range(1f - VarianceFraction, 1f + VarianceFraction);
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * roll));
        }
    }
}
