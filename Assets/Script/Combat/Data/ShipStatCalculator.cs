// Ignore Spelling: BOTF warp torp Sys civ

using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Derives all ship combat stats at runtime.
    /// ShipSO holds only identity/visual data; this class owns every numeric value.
    ///
    /// Formula per stat:
    ///   combat = baseStat × tierCombatMult × qualityCombatMult × civFlavorMult
    ///   warp   = baseWarp × tierWarpMult          (quality/civ flavor does not alter warp)
    ///   build  = baseBuild × tierBuildMult × qualityBuildMult
    ///
    /// Calibration note: FED (quality=5) Scout series matches legacy hand-set asset values
    /// within ±1 for shield/hull/beam and ±2 for torpedoes.
    /// </summary>
    public static class ShipStatCalculator
    {
        // ── Base stats at Tier I, QualityScore=5, no civ flavor (minor-race baseline) ──
        // Columns: Shield, Hull, Beam, Torp, Warp, Build, Dilithium
        private static readonly Dictionary<ShipType, BaseStats> Base = new Dictionary<ShipType, BaseStats>
        {
            { ShipType.Scout,     new BaseStats(20, 10, 10,  8, 5.5f,  5, 1) },
            { ShipType.Destroyer, new BaseStats(32, 15, 16, 13, 5.0f,  8, 2) },
            { ShipType.LtCruiser, new BaseStats(44, 22, 22, 18, 4.5f, 11, 3) },
            { ShipType.Cruiser,   new BaseStats(58, 30, 30, 24, 4.0f, 14, 3) },
            { ShipType.HvyCruiser,new BaseStats(76, 42, 40, 32, 3.5f, 18, 4) },
            { ShipType.Transport, new BaseStats(12, 30,  3,  0, 3.5f,  6, 4) },
        };

        // ── Tech-tier multipliers (derived from actual FED Scout_I–IV data) ──
        // Index corresponds to (int)TechLevel: EARLY=0, DEVELOPED=1, ADVANCED=2, SUPREME=3
        private static readonly float[] TierCombat = { 1.00f, 1.38f, 1.82f, 2.38f };
        private static readonly float[] TierWarp   = { 1.00f, 1.36f, 1.45f, 1.78f };
        private static readonly float[] TierBuild  = { 1.00f, 1.20f, 1.40f, 1.60f };

        // ── Transport cargo capacity by tech tier (Transport ShipType only) ──
        private static readonly int[] TransportCargoByTier = { 2, 3, 4, 6 };

        // ── Quality-score multipliers (index = QualityScore 0–10) ──
        // Combat: moderate linear scale so quality doctrine gives noticeable but not overwhelming power.
        // Build: steeper curve — high-quality ships are significantly more costly to produce.
        private static readonly float[] QualCombat =
            { 0.70f, 0.75f, 0.80f, 0.86f, 0.93f, 1.00f, 1.06f, 1.12f, 1.18f, 1.24f, 1.30f };

        private static readonly float[] QualBuild =
            { 0.60f, 0.68f, 0.76f, 0.84f, 0.92f, 1.00f, 1.10f, 1.22f, 1.36f, 1.52f, 1.70f };

        // ── Per-civ flavor ──
        // Asymmetric multipliers applied on top of quality score.
        //   FED:    balanced, slightly above-average shields
        //   ROM:    fragile but devastating disruptor beams, fastest ships
        //   KLING:  weak shields, tough hull, devastating weapons (disruptors + torpedoes)
        //   CARD:   durable hulls, methodical (good torpedoes), slower
        //   DOM:    polaron-powered shields dominate; strong beam weapons
        //   BORG:   near-impenetrable shields and hull; slow to produce
        //   TERRAN: identical ship stats to Federation (same flavor; distinguished only by QualityScore)
        //   MINOR:  baseline (no flavor entry = 1.0 on all stats)
        private static readonly Dictionary<CivEnum, CivFlavor> Flavor = new Dictionary<CivEnum, CivFlavor>
        {
            { CivEnum.FED,    new CivFlavor(1.08f, 1.05f, 1.00f, 1.05f) },
            { CivEnum.ROM,    new CivFlavor(0.90f, 0.90f, 1.20f, 0.92f) },
            { CivEnum.KLING,  new CivFlavor(0.82f, 1.08f, 1.22f, 1.22f) },
            { CivEnum.CARD,   new CivFlavor(1.10f, 1.20f, 0.98f, 1.15f) },
            { CivEnum.DOM,    new CivFlavor(1.28f, 0.92f, 1.15f, 1.00f) },
            { CivEnum.BORG,   new CivFlavor(1.45f, 1.32f, 1.08f, 1.08f) },
            { CivEnum.TERRAN, new CivFlavor(1.08f, 1.05f, 1.00f, 1.05f) },
        };

        // ── Per-civ power plant dilithium cost ─────────────────────────────────────
        // FED = 30 is the baseline. Other civs scale by doctrine:
        //   ROM:    efficient, fragile — less infrastructure needed
        //   KLING:  honour-based, martial — moderate power draw
        //   CARD:   methodical, expansive — slightly above baseline
        //   DOM:    polaron tech is energy-hungry — notably above baseline
        //   BORG:   massive shield/hull infrastructure — highest cost
        //   TERRAN: identical tech base to FED
        private static readonly Dictionary<CivEnum, int> PowerPlantLi2Cost = new Dictionary<CivEnum, int>
        {
            { CivEnum.FED,    30 },
            { CivEnum.ROM,    25 },
            { CivEnum.KLING,  28 },
            { CivEnum.CARD,   32 },
            { CivEnum.DOM,    35 },
            { CivEnum.BORG,   40 },
            { CivEnum.TERRAN, 30 },
        };

        /// <summary>
        /// Returns the dilithium cost to build one power plant for the given civilisation.
        /// Falls back to 10 for minor races.
        /// </summary>
        public static int GetPowerPlantDilithiumCost(CivEnum civ) =>
            PowerPlantLi2Cost.TryGetValue(civ, out int cost) ? cost : 10;

        /// <summary>
        /// Compute all runtime stats.  Call once during ship initialization.
        /// </summary>
        public static ShipStats Calculate(ShipType shipType, TechLevel techTier, CivEnum civEnum, int qualityScore)
        {
            if (!Base.TryGetValue(shipType, out var b))
                b = Base[ShipType.Scout];

            int ti = Mathf.Clamp((int)techTier,   0, TierCombat.Length - 1);
            int qi = Mathf.Clamp(qualityScore,     0, QualCombat.Length - 1);

            float combat    = TierCombat[ti] * QualCombat[qi];
            float warpMult  = TierWarp[ti];
            float buildMult = TierBuild[ti] * QualBuild[qi];

            if (!Flavor.TryGetValue(civEnum, out var f))
                f = CivFlavor.Neutral;

            return new ShipStats
            {
                ShieldMaxHealth = Mathf.Max(1, Mathf.RoundToInt(b.Shield * combat * f.Shield)),
                HullMaxHealth   = Mathf.Max(1, Mathf.RoundToInt(b.Hull   * combat * f.Hull)),
                BeamDamage      = Mathf.Max(1, Mathf.RoundToInt(b.Beam   * combat * f.Beam)),
                TorpedoDamage   = Mathf.Max(0, Mathf.RoundToInt(b.Torp   * combat * f.Torp)),
                MaxWarpFactor   = b.Warp * warpMult,
                BuildDuration   = Mathf.Max(1, Mathf.RoundToInt(b.Build  * buildMult)),
                DilithiumCost   = Mathf.Max(0, Mathf.RoundToInt(b.Dilithium * TierBuild[ti])),
                CargoCapacity   = shipType == ShipType.Transport ? TransportCargoByTier[ti] : 0,
            };
        }

        // ── Supporting types ──

        private readonly struct BaseStats
        {
            public readonly int Shield, Hull, Beam, Torp, Build, Dilithium;
            public readonly float Warp;
            public BaseStats(int shield, int hull, int beam, int torp, float warp, int build, int dilithium)
            { Shield = shield; Hull = hull; Beam = beam; Torp = torp; Warp = warp; Build = build; Dilithium = dilithium; }
        }

        private readonly struct CivFlavor
        {
            public readonly float Shield, Hull, Beam, Torp;
            public CivFlavor(float shield, float hull, float beam, float torp)
            { Shield = shield; Hull = hull; Beam = beam; Torp = torp; }
            public static readonly CivFlavor Neutral = new CivFlavor(1f, 1f, 1f, 1f);
        }
    }

    /// <summary>
    /// Computed ship stats returned by <see cref="ShipStatCalculator.Calculate"/>.
    /// </summary>
    public struct ShipStats
    {
        public int   ShieldMaxHealth;
        public int   HullMaxHealth;
        public int   BeamDamage;
        public int   TorpedoDamage;
        public float MaxWarpFactor;
        public int   BuildDuration;
        public int   DilithiumCost;
        public int   CargoCapacity;
    }
}
