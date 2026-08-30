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
    ///   combat    = baseStat × tierCombatMult × qualityCombatMult × civFlavorMult
    ///   warp      = baseWarp × tierWarpMult × civFlavorWarpMult    (quality does not alter warp)
    ///   build     = baseBuild × tierBuildMult × qualityBuildMult
    ///   dilithium = baseDilithium × tierBuildMult × qualityBuildMult
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
            { ShipType.Transport, new BaseStats(12, 30,  0,  0, 3.5f,  6, 4) },
            // Stationary system-defense platform: never warps in or moves (see ShipMovementController /
            // CombatOrderStateMachine OrbitalBattery guards), so it trades mobility for raw durability
            // and hitting power vs. a mobile hull of comparable tier — tankier than HvyCruiser, hits
            // roughly as hard, Warp=0 (unused; movement is skipped entirely for this ShipType).
            { ShipType.OrbitalBattery, new BaseStats(95, 90, 38, 26, 0f, 15, 5) },
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
        // Design rule for the FED/ROM/KLING/TERRAN "mid tier" (all QualityScore=5, see CivSO assets):
        // each civ's total combat power is calibrated to land within ~±3% of a common baseline, so
        // no civ is a strictly-better/worse pick — the differences are archetypes (durable vs. burst
        // vs. hit-and-run), not power tiers. Borg/Dominion/Cardassian intentionally sit outside that
        // parity band; they're a real power tier (see QualityScore) and their flavor numbers are
        // unchanged from before.
        //
        // Power model, take 2 (superseded the "Power = EffectiveHP × Offense" product-parity model,
        // itself a fix for an even older additive model — see git history for both). The product
        // model kept EffectiveHP × Offense equal per civ, but let EffectiveHP itself drift far apart
        // (FED's was ~28% above Romulan's, ~17% above Klingon's) as long as the product balanced out.
        // That's wrong for this combat shape: every ship on both sides fires simultaneously across
        // the whole turn, so a per-ship EffectiveHP edge doesn't just mean "harder to kill" - ships
        // that survive longer also get to keep shooting, so the side with more HP ends the turn with
        // more live shooters too. That compounds far faster than a flat Offense edge does, so the
        // product model let mirror FED-vs-KLING fights come out lopsided in ship count (FED regularly
        // finishing a turn with 2x+ the surviving hulls) even in turns where total damage dealt was
        // roughly even or Klingon-favored (confirmed via CombatRecordings turn-log analysis after
        // fixing the separate torpedo-almost-never-fires bug in ShipController.FindNearestTorpedoTarget,
        // which had been suppressing Klingon's Torp-heavy kit and masking this EffectiveHP gap as an
        // offense problem instead of an HP problem).
        //
        // Fix: hold EffectiveHP = Shield + Hull within ~±3% across all four majors directly (not just
        // the EffectiveHP×Offense product), computed once against Scout's base stats (20 Shield, 10
        // Hull) and applied as a uniform per-civ scalar to both Shield and Hull multipliers - this
        // preserves each civ's Shield:Hull ratio (its defensive "shape") at every ship tier, since
        // EffectiveHP scales linearly with that scalar regardless of a hull's base Shield:Hull split.
        // Beam/Torp/Warp are unchanged from the prior pass - those are now realizing roughly as
        // intended post torpedo-targeting-fix and didn't need correction.
        //   FED:    was the tankiest of the four by a wide margin; trimmed toward the pack while
        //           staying the most defensive (Formation play)
        //   ROM:    was the most fragile by a wide margin; raised toward the pack - still the
        //           lowest-EffectiveHP major, compensated by fastest Warp (kiting) and heaviest Beam
        //   KLING:  weakest shields, tough hull, hardest-hitting weapons overall — best burst/alpha
        //           strike (Rush/Engage), worst in a dragged-out attrition fight. Kit leans Torp over
        //           Beam: under Engage, ships fly past each other rather than holding at range, so a
        //           full 15s turn (TurnBasedCombatResolver.ResolutionAnimationDuration) spends only a
        //           brief crossing window inside BeamWeapon.cs's ≤50u full-damage band, with most of
        //           the turn in its falloff zone (down to ~7% of listed damage beyond 350u). Torpedo
        //           damage is range-immune (Torpedo.cs: homing, always full value) but fires only once
        //           per ship — so weighting Klingon's edge toward Torp realizes more of its advantage
        //           every fight than stacking it on a repeatedly-fired, range-diluted Beam. The above-
        //           baseline Warp still pushes ships to point-blank faster (aggressive rush, not
        //           Romulan-style kiting at range).
        //   TERRAN: mirror-Fed militarized doctrine — same chassis as FED but traded defense for
        //           firepower and a slightly faster posture; distinct from FED, not a clone. Already
        //           near the EffectiveHP target, essentially unchanged by this pass.
        //   CARD:   durable hulls, methodical (good torpedoes), slower — cheap/fast to build, so it
        //           fields MORE ships than the majors at EARLY; Beam/Torp bumped alongside its EARLY
        //           starting-fleet resize (see the Flavor entry below) after cutting ship count made
        //           it too weak
        //   DOM:    polaron-powered shields dominate; strong beam weapons — high per-ship power lets
        //           it field FEWER ships than the majors at EARLY (Beam/Torp bumped alongside a
        //           further EARLY starting-fleet cut - see the Flavor entry below)
        //   BORG:   near-impenetrable shields and hull, hardest-hitting weapons — highest per-ship
        //           power of any civ, so it fields the FEWEST ships of any civ at EARLY. Even at a
        //           linearly-"balanced" fleet-total ratio, concentrating that much power into so few
        //           ships still won attrition fights (few, tanky, hard-hitting ships survive focus
        //           fire far longer than the linear sum predicts) - trimming Shield/Hull/Beam/Torp
        //           below the majors' band fixed that, but read as an over-correction in the other
        //           direction (too weak), so it's been walked up and back down since across seven
        //           passes, landing at 1.40/1.28/1.44/1.44 - just inside the majors' band rather than
        //           below it. An eighth pass then deliberately pushed Beam/Torp to 1.70/1.70, to bring
        //           Total Offense up to full Romulan parity (1.00x), overriding the concentration-effect
        //           discount described above - a ninth pass backed that off to 1.60/1.60 (Total Offense
        //           ≈0.94x), still above the concentration-adjusted band but pulling back from full
        //           parity. Warp deliberately excluded from every pass of this tuning - see the Flavor
        //           entry below
        //   MINOR:  baseline (no flavor entry = 1.0 on all stats)
        //
        //   Unifying EARLY starting-fleet philosophy (all seven playable civs): ship COUNT, not raw
        //   per-ship stats, is the primary lever used to land every civ's first fleet inside the same
        //   overall combat-power band (see ShipSOProvider.MajorStartingFleetCompositionOverrides for
        //   the full ratio methodology and history) - civs with weaker per-ship stats get more ships,
        //   civs with stronger per-ship stats get fewer, so "more evenly matched combat and combat
        //   outcomes" holds regardless of which civ's fleet a player is fighting.
        private static readonly Dictionary<CivEnum, CivFlavor> Flavor = new Dictionary<CivEnum, CivFlavor>
        {
            // EffectiveHP(Scout) 1.09/1.06 → 32.4 down to 0.99/0.96 → 29.4
            { CivEnum.FED,    new CivFlavor(0.99f, 0.96f, 1.01f, 1.06f, 1.00f) },
            // EffectiveHP(Scout) 0.83/0.86 → 25.2 up to 0.91/0.94 → 27.6
            { CivEnum.ROM,    new CivFlavor(0.91f, 0.94f, 1.32f, 1.03f, 1.12f) },
            // Shield/Hull 0.80/1.22 → 0.86/1.26: the "EffectiveHP within ~3%" claim above was only
            // ever checked against the unrounded math (28.2 vs FED's 29.4). ShieldMaxHealth and
            // HullMaxHealth are rounded to ints INDEPENDENTLY in Calculate() below, and for Scout-tier
            // ships (the bulk of any test fleet by ship count) that rounding compounds against
            // Klingon: FED's 19.8/9.6 both round UP (→20/10=30), Klingon's 16.0/12.2 round to
            // 16/12=28 - a real 6.7% in-game gap, not ~3%. In continuous simultaneous combat (every
            // ship fires all turn, dead ships stop firing), even that small a persistent edge
            // snowballs turn-over-turn - CombatRecordings after the 1.02/1.35 offense trim above
            // still showed Federation finishing with 5-6/14 ships alive vs Klingon's 2/14 in nearly
            // every battle, despite total damage dealt being close (~7% apart) and per-shot averages
            // already balanced. Retuned so post-rounding EffectiveHP is an exact match at Scout and
            // within ~2.5% (now slightly Klingon-favored, not Federation-favored) at every other
            // hull size - see the verification table in conversation/git history for the full
            // per-ship-type breakdown.
            { CivEnum.KLING,  new CivFlavor(0.86f, 1.26f, 1.02f, 1.35f, 1.10f) },
            { CivEnum.CARD,   new CivFlavor(0.95f, 1.02f, 1.05f, 1.25f, 1.05f) }, // Shield/Hull/Beam/Torp all pulled back down toward neutral (from 1.10/1.20/1.26/1.48) as EARLY starting-fleet ship count went back up 7/7→8/8 - see ShipSOProvider.MajorStartingFleetCompositionOverrides. Warp intentionally left untouched.
            { CivEnum.DOM,    new CivFlavor(1.21f, 0.87f, 1.24f, 1.14f, 1.00f) }, // Small increase from 1.19/0.86/1.22/1.12 - a smaller step this time (not the previous midpoint's full jump), bringing EffHP≈0.99x, Offense≈0.84x right up to the majors' band's low edge without crossing into it. Ship count (3/5) and Warp (1.00, left alone) untouched. See ShipSOProvider.MajorStartingFleetCompositionOverrides for the full history and resulting ratios
            { CivEnum.BORG,   new CivFlavor(1.40f, 1.28f, 1.60f, 1.60f, 0.85f) }, // Beam/Torp backed off from the 1.70/1.70 full-parity pass to 1.60/1.60 - Total Offense now ≈0.94x (down from 1.00x), Beam-only ≈0.85x (back at the top of every other civ's 0.78x-0.85x range instead of above it). Shield/Hull untouched (EffectiveHP stays ≈1.03x), Warp (0.85) untouched by every pass of this tuning. See ShipSOProvider.MajorStartingFleetCompositionOverrides for the full history and resulting ratios
            // EffectiveHP(Scout) 0.95/0.95 → 28.5, already at target - unchanged
            { CivEnum.TERRAN, new CivFlavor(0.95f, 0.95f, 1.12f, 1.12f, 1.03f) },
        };

        // ── Dilithium locked into each power plant reactor by civilisation ──────────
        // Each plant holds this much dilithium permanently in its reactor crystal matrix.
        // Reactor size reflects the civ's power doctrine, not raw strength:
        //   FED:    balanced medium reactors — baseline 25
        //   ROM:    compact, efficient singularity-adjacent design — 22
        //   KLING:  brute-force plasma reactors, higher draw — 28
        //   CARD:   cheap low-output plants compensated by quantity — 15
        //   DOM:    polaron systems are energy-hungry, large reactors — 38
        //   BORG:   each node powers a vast complex, maximum capacity — 55
        //   TERRAN: mirrors Federation baseline — 25
        //   Minors: small single-reactor installation — 8
        private static readonly Dictionary<CivEnum, int> PowerPlantLi2Cost = new Dictionary<CivEnum, int>
        {
            { CivEnum.FED,    25 },
            { CivEnum.ROM,    22 },
            { CivEnum.KLING,  28 },
            { CivEnum.CARD,   15 },
            { CivEnum.DOM,    38 },
            { CivEnum.BORG,   55 },
            { CivEnum.TERRAN, 25 },
        };

        /// <summary>
        /// Returns the dilithium locked into one power plant reactor for the given civilisation.
        /// Falls back to 8 for minor races.
        /// </summary>
        public static int GetPowerPlantDilithiumCost(CivEnum civ) =>
            PowerPlantLi2Cost.TryGetValue(civ, out int cost) ? cost : 8;

        /// <summary>
        /// Public entry point onto the same QualityScore -> multiplier curve (0.70 at score 0, 1.00
        /// at the neutral score 5, 1.30 at score 10) used internally for ship combat/build stats.
        /// Exposed so other systems that scale off a civ's canon "weak/quantity <-> strong/quality"
        /// dial (e.g. TechManager's minor-race research growth) read from the same single curve
        /// instead of inventing a second one that could drift out of sync with this one.
        /// </summary>
        public static float GetQualityScaleFactor(int qualityScore) =>
            QualCombat[Mathf.Clamp(qualityScore, 0, QualCombat.Length - 1)];

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
                // Transports carry no weapons - skip the min-1 floor below (it exists so combat
                // ships never round down to a useless 0-damage beam) or they'd always end up
                // with at least 1 beam damage even though their base Beam stat is 0.
                BeamDamage      = shipType == ShipType.Transport ? 0 : Mathf.Max(1, Mathf.RoundToInt(b.Beam * combat * f.Beam)),
                TorpedoDamage   = Mathf.Max(0, Mathf.RoundToInt(b.Torp   * combat * f.Torp)),
                MaxWarpFactor   = b.Warp * warpMult * f.Warp,
                BuildDuration   = Mathf.Max(1, Mathf.RoundToInt(b.Build  * buildMult)),
                // Quality score now scales dilithium the same way it scales build time, so a
                // low-tier civ (e.g. Cardassian) gets a genuine resource discount and a high-tier
                // civ (e.g. Borg) genuinely costs more — not just a build-time nudge. FED/ROM/KLING/
                // TERRAN share QualityScore=5, so their dilithium cost stays equal, matching their
                // equal build time.
                DilithiumCost   = Mathf.Max(0, Mathf.RoundToInt(b.Dilithium * TierBuild[ti] * QualBuild[qi])),
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
            public readonly float Shield, Hull, Beam, Torp, Warp;
            public CivFlavor(float shield, float hull, float beam, float torp, float warp)
            { Shield = shield; Hull = hull; Beam = beam; Torp = torp; Warp = warp; }
            public static readonly CivFlavor Neutral = new CivFlavor(1f, 1f, 1f, 1f, 1f);
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
