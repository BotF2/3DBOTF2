using BOTF3D.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Provides ShipSO (scriptable objects) based on civ, tech level, and ship type.
    /// Central repository for ship template queries.
    /// </summary>
    public class ShipSOProvider
    {
        // Ship lists by civilization
        private readonly List<ShipSO> fedShipSOList;
        private readonly List<ShipSO> romShipSOList;
        private readonly List<ShipSO> klingShipSOList;
        private readonly List<ShipSO> cardShipSOList;
        private readonly List<ShipSO> domShipSOList;
        private readonly List<ShipSO> borgShipSOList;
        private readonly List<ShipSO> terranShipSOList;
        private readonly List<ShipSO> minorShipSOList;

        public ShipSOProvider(
            List<ShipSO> fed,
            List<ShipSO> rom,
            List<ShipSO> kling,
            List<ShipSO> card,
            List<ShipSO> dom,
            List<ShipSO> borg,
            List<ShipSO> terran,
            List<ShipSO> minor)
        {
            fedShipSOList = fed;
            romShipSOList = rom;
            klingShipSOList = kling;
            cardShipSOList = card;
            domShipSOList = dom;
            borgShipSOList = borg;
            terranShipSOList = terran;
            minorShipSOList = minor;
        }

        /// <summary>
        /// Get ship SO list for a specific civilization
        /// </summary>
        public List<ShipSO> GetShipSOListByCiv(CivEnum civEnum)
        {
            switch (civEnum)
            {
                case CivEnum.FED: return fedShipSOList;
                case CivEnum.ROM: return romShipSOList;
                case CivEnum.KLING: return klingShipSOList;
                case CivEnum.CARD: return cardShipSOList;
                case CivEnum.DOM: return domShipSOList;
                case CivEnum.BORG: return borgShipSOList;
                case CivEnum.TERRAN: return terranShipSOList;
                default:
                    // Search minor ship list for the given minor civ
                    if (minorShipSOList == null || minorShipSOList.Count == 0)
                    {
                        Debug.LogWarning($"GetShipSOListByCiv: MinorShipSOList is empty!");
                        return new List<ShipSO>();
                    }

                    var minorCivShips = minorShipSOList
                        .Where(s => s != null && s.CivEnum == civEnum)
                        .ToList();

                    if (minorCivShips.Count == 0)
                    {
                        Debug.LogWarning($"GetShipSOListByCiv: No ships found for minor civ {civEnum}");
                    }

                    return minorCivShips;
            }
        }

        /// <summary>
        /// Get a specific ship SO by type and tech level
        /// </summary>
        public ShipSO GetShipSO(ShipType shipType, TechLevel techLevel, CivEnum civEnum)
        {
            List<ShipSO> civShips = GetShipSOListByCiv(civEnum);

            if (civShips == null || civShips.Count == 0)
            {
                Debug.LogWarning($"GetShipSO: No ships found for {civEnum}");
                return null;
            }

            // Find ship matching type AND tech level
            ShipSO foundShip = civShips.FirstOrDefault(s =>
                s != null && s.ShipType == shipType && s.TechLevel == techLevel);

            if (foundShip == null)
            {
                Debug.LogWarning($"GetShipSO: No {shipType} found for {civEnum} at {techLevel} - searching fallback...");

                // Fallback: Try Scout at EARLY tech
                foundShip = civShips.FirstOrDefault(s =>
                    s != null && s.ShipType == ShipType.Scout && s.TechLevel == TechLevel.EARLY);

                if (foundShip != null)
                {
                    Debug.Log($"  ✅ Using fallback: {foundShip.ShipName}");
                }
            }

            return foundShip;
        }

        /// <summary>
        /// Get ship SO at the BEST available tech level for the civ
        /// Searches from current tech level DOWN
        /// </summary>
        public ShipSO GetShipSOAtBestTechLevel(ShipType shipType, TechLevel maxTechLevel, CivEnum civEnum)
        {
            List<ShipSO> civShips = GetShipSOListByCiv(civEnum);

            if (civShips == null || civShips.Count == 0)
            {
                Debug.LogWarning($"GetShipSOAtBestTechLevel: No ships found for {civEnum}");
                return null;
            }

            // Find ALL ships of this type at or below max tech level
            var candidateShips = civShips
                .Where(s => s != null && s.ShipType == shipType && s.TechLevel <= maxTechLevel)
                .OrderByDescending(s => s.TechLevel) // Highest tech first
                .ToList();

            if (candidateShips.Count > 0)
            {
                ShipSO bestShip = candidateShips[0];
                Debug.Log($"GetShipSOAtBestTechLevel: Found {shipType} for {civEnum} at {bestShip.TechLevel} (max allowed: {maxTechLevel})");
                return bestShip;
            }

            // Not every ship type is unlocked at every tech level (e.g. Cruiser-class hulls
            // don't exist at EARLY for most civs) - this is expected, not a problem.
            return null;
        }

        /// <summary>
        /// Get all ships available to a civilization at their current tech level (and below)
        /// </summary>
        public List<ShipSO> GetAvailableShipsForCiv(CivEnum civEnum, TechLevel currentTechLevel)
        {
            List<ShipSO> allCivShips = GetShipSOListByCiv(civEnum);

            if (allCivShips == null || allCivShips.Count == 0)
            {
                Debug.LogWarning($"GetAvailableShipsForCiv: No ships found for {civEnum}");
                return new List<ShipSO>();
            }

            // Remove null entries
            allCivShips = allCivShips.Where(s => s != null).ToList();

            // Filter: Only include ships at or below current tech level
            List<ShipSO> availableShips = allCivShips
                .Where(s => s.TechLevel <= currentTechLevel)
                .ToList();

            Debug.Log($"GetAvailableShipsForCiv: {civEnum} at {currentTechLevel} has {availableShips.Count}/{allCivShips.Count} ships available");

            return availableShips;
        }

        /// <summary>
        /// Check if a specific ship type is available for a civilization
        /// </summary>
        public bool IsShipTypeAvailable(ShipType shipType, CivEnum civEnum, TechLevel currentTechLevel)
        {
            ShipSO ship = GetShipSO(shipType, currentTechLevel, civEnum);

            if (ship == null)
            {
                // Try to find ANY version of this ship type for this civ
                List<ShipSO> civShips = GetShipSOListByCiv(civEnum);
                ship = civShips?.FirstOrDefault(s => s != null && s.ShipType == shipType);

                if (ship == null)
                {
                    Debug.Log($"IsShipTypeAvailable: {civEnum} has no {shipType} at any tech level");
                    return false;
                }
            }

            bool available = ship.TechLevel <= currentTechLevel;
            Debug.Log($"IsShipTypeAvailable: {shipType} for {civEnum} - Required: {ship.TechLevel}, Current: {currentTechLevel}, Available: {available}");
            return available;
        }

        /// <summary>
        /// Get fallback ship SO (default ship when nothing else works)
        /// </summary>
        public ShipSO GetFallbackShipSO()
        {
            return GetShipSO(ShipType.Destroyer, TechLevel.EARLY, CivEnum.FED);
        }

        // Starting-fleet composition for playable major civs (FED..TERRAN), keyed by tech level:
        // ship type -> how many of that type Fleet 1 starts with. Every major civ is built from
        // this same table, sourced from its own per-civ ShipSO list, so parity across civs is
        // automatic rather than hand-matched per civ. Add an entry here (and the matching
        // TechLevel_ShipSO assets) when a higher tech level unlocks Cruiser-class hulls.
        private static readonly Dictionary<TechLevel, Dictionary<ShipType, int>> MajorStartingFleetComposition =
            new Dictionary<TechLevel, Dictionary<ShipType, int>>
            {
                [TechLevel.EARLY] = new Dictionary<ShipType, int>
                {
                    { ShipType.Scout, 6 },
                    { ShipType.Destroyer, 6 },
                    { ShipType.Transport, 2 },
                },
                // First pass: same Scout/Destroyer/Transport counts as EARLY, plus a Cruiser for every
                // Destroyer (Cruiser is the ship type that newly unlocks at DEVELOPED). Not yet balance
                // -tuned like the EARLY table above - revisit once there's playtest data at this tier.
                [TechLevel.DEVELOPED] = new Dictionary<ShipType, int>
                {
                    { ShipType.Scout, 6 },
                    { ShipType.Destroyer, 6 },
                    { ShipType.Transport, 2 },
                    { ShipType.Cruiser, 6 },
                },
                // Same Scout/Destroyer/Transport/Cruiser counts as DEVELOPED - the TechLevel.ADVANCED
                // lookup key alone is enough for GetStartingFleetShips to resolve each civ's Advanced-
                // tier (_III) ShipSO templates instead of Developed's (_II).
                [TechLevel.ADVANCED] = new Dictionary<ShipType, int>
                {
                    { ShipType.Scout, 6 },
                    { ShipType.Destroyer, 6 },
                    { ShipType.Transport, 2 },
                    { ShipType.Cruiser, 6 },
                },
                // Cruiser-class hull splits into LtCruiser/HvyCruiser at SUPREME (LtCruiser is the
                // direct Cruiser equivalent at this tier, HvyCruiser is new). HvyCruiser count is
                // ~25% of DEVELOPED's Cruiser count (6 * 0.25 = 1.5, rounded to 2), with LtCruiser
                // making up the remaining 4 so the 6-ship total is unchanged.
                [TechLevel.SUPREME] = new Dictionary<ShipType, int>
                {
                    { ShipType.Scout, 6 },
                    { ShipType.Destroyer, 6 },
                    { ShipType.Transport, 2 },
                    { ShipType.LtCruiser, 4 },
                    { ShipType.HvyCruiser, 2 },
                }
            };

        // Per-civ overrides of MajorStartingFleetComposition above, keyed by (civ, tech level).
        // FED/ROM/KLING/TERRAN share QualityScore=5 and are calibrated to near-equal per-ship power
        // (see ShipStatCalculator.Flavor), so the shared 6/6/2 baseline above already balances their
        // first fleets 1-for-1 and none of them need an entry here.
        //
        // CARD/DOM/BORG sit on ShipStatCalculator's QualityScore "quantity vs quality" axis instead
        // (QualityScore 1/8/10 respectively - see CivSO tooltip), which scales per-ship combat power
        // directly (QualCombat[] in ShipStatCalculator) on top of their existing Flavor multipliers.
        // A same-size fleet is therefore never fair for these three; ship COUNT has to move the other
        // way to compensate. These are sized off FULL-FLEET totals (not a single Scout-tier ratio),
        // because Total Offense scales linearly with ship count and Beam fires many times per 30s
        // turn while Torpedo fires once - so per-ship power indices alone under/over-correct.
        //
        // Transport is EXCLUDED from all three ratios below (it carries no Beam/Torp/weapons, so it
        // has zero combat power and shouldn't factor into a combat-power calculation) and is instead
        // fixed at 2 for every playable civ, majors and CARD/DOM/BORG alike - only Scout and
        // Destroyer counts are the balancing lever. Ratios are Scout+Destroyer fleet totals against
        // Romulan's 6 Scout + 6 Destroyer baseline (Transport excluded from that baseline too):
        //   CARD (8/8) → Total Offense ≈0.99x, Beam-only ≈0.82x, EffectiveHP ≈1.07x Romulan's
        //     History of this entry, in order:
        //       9/9, Flavor Beam/Torp 0.98/1.15 → Total Offense ≈1.01x, EffectiveHP ≈1.41x. Playtest
        //       with a live Federation fleet showed that 18-ship fleet consistently wiping FED out
        //       despite the static Total Offense sum reading near-parity - this combat resolver fires
        //       every live ship simultaneously every turn, so a fleet with 50% more shooters (18 vs
        //       FED's 12) gets a compounding focus-fire/attrition advantage a linear total-damage sum
        //       doesn't capture (the same "EffectiveHP compounds faster than a flat edge" lesson
        //       already learned for FED-vs-KLING above, just via ship COUNT instead of per-ship HP).
        //       7/7, Flavor Beam/Torp 1.26/1.48 → Total Offense ≈1.00x, EffectiveHP ≈1.10x. Cut hard
        //       to a 17% count edge over the majors' 12 and raised Beam/Torp flavor to restore Total
        //       Offense parity. Next playtest: ship count now read as too low, even though the
        //       aggregate ratios looked reasonable.
        //       8/8, Flavor Shield/Hull/Beam/Torp 0.95/1.02/1.05/1.25 (was 1.10/1.20/1.26/1.48) →
        //       current. Brought count back up toward (but still below) the original 9/9, and pulled
        //       Shield/Hull/Beam/Torp all back down toward neutral so the extra ship count doesn't
        //       reinflate the fleet-total ratios past where they were at 7/7. Warp (1.05) left
        //       untouched throughout.
        //   DOM  (3/5) → Total Offense ≈0.84x, Beam-only ≈0.78x, EffectiveHP ≈0.99x Romulan's
        //     History of this entry, in order:
        //       4/6, Flavor Beam/Torp 1.15/1.00 → Total Offense ≈0.95x, EffectiveHP ≈1.30x - already
        //       below the majors' own baseline despite Dominion's supposedly stronger ships, because
        //       DOM's per-ship offense edge over Romulan was only ~9%, too thin to carry a 10-ship
        //       fleet. Bumped Beam/Torp to 1.20/1.10 to bring it up to ≈1.02x/1.30x - technically
        //       "balanced" by the linear metric, but per the CARD 9/9 lesson above, EffectiveHP that
        //       far above the majors' ~1.0x still wins attrition fights it shouldn't.
        //       3/5, Flavor Beam/Torp bumped again to 1.31/1.20 - cut ship count from 10 to 8 combat
        //       ships to pull EffectiveHP down from 1.30x into the majors' 1.00x-1.10x band, then
        //       raised Beam/Torp again to bring Total Offense from the resulting ≈0.71x back up into
        //       the majors' 0.87x-1.00x band without re-inflating EffectiveHP (Beam/Torp changes don't
        //       touch Shield/Hull). EffHP≈1.06x, Offense≈0.89x. Playtest reported this as a bit too
        //       strong.
        //       3/5, Flavor Shield/Hull/Beam/Torp trimmed proportionally to 1.22/0.88/1.25/1.15 - all
        //       four pulled down together to bring EffectiveHP from ≈1.06x to ≈1.00x and Total Offense
        //       from ≈0.89x to ≈0.84x. Playtest still read this as a bit too strong even at Romulan
        //       parity / the band's low edge.
        //       3/5, Flavor trimmed again to Shield/Hull/Beam/Torp 1.16/0.84/1.19/1.09 - another ~5%
        //       cut across all four, deliberately landing below the majors' band (EffectiveHP≈0.95x,
        //       Total Offense≈0.80x) rather than at its low edge, the same way Borg's concentration-
        //       effect tuning sits below the band with fewer ships than the majors.
        //       3/5, Flavor given a bit back at Shield/Hull/Beam/Torp 1.19/0.86/1.22/1.12 - the midpoint
        //       between the 1.16/0.84/1.19/1.09 undershoot and the prior "a bit too strong"
        //       1.22/0.88/1.25/1.15, at EffectiveHP≈0.98x, Total Offense≈0.82x - still deliberately
        //       below the majors' band, just less far below it.
        //       3/5 (current), Flavor given a small increase to Shield/Hull/Beam/Torp 1.21/0.87/1.24/1.14
        //       (see ShipStatCalculator.Flavor[CivEnum.DOM]) - a smaller step than the previous
        //       midpoint jump, bringing EffectiveHP≈0.99x and Total Offense≈0.84x right up to the
        //       majors' band's low edge without crossing into it. Ship count (3/5) and Warp (1.00)
        //       untouched.
        //   BORG (2/4) → Total Offense ≈0.94x, Beam-only ≈0.85x, EffectiveHP ≈1.03x Romulan's
        //     History of this entry, in order:
        //       3/5, Flavor Beam/Torp 1.08 → later bumped to 1.40/1.40 → Total Offense ≈1.10x,
        //       EffectiveHP ≈1.42x - same over-tuned problem as DOM's 4/6 entry above, just more
        //       extreme since Borg has the highest per-ship power of any civ (QualityScore 10).
        //       2/4, Flavor Beam/Torp bumped again to 1.48/1.48 (Shield/Hull left at 1.45/1.32) - cut
        //       ship count from 8 to 6 combat ships, the fewest of any civ, to pull EffectiveHP down
        //       to ≈1.08x and Offense to ≈0.89x, both landing inside the majors' band. Playtest still
        //       showed one-sided Borg wins - concentrating that much per-ship power (highest
        //       EffectiveHP AND highest offense of any civ, QualityScore 10) into only 6 ships lets
        //       them out-survive attrition far past what the linear fleet-total ratio predicts, the
        //       same non-linearity as the CARD 9/9 ship-COUNT lesson but via per-ship stat
        //       concentration instead of count.
        //       2/4, Flavor cut to Shield/Hull/Beam/Torp 1.25/1.15/1.32/1.32 - deliberately pushed
        //       EffectiveHP (≈0.92x) and Offense (≈0.78x) BELOW the majors' band (unlike every other
        //       civ's entry above) to offset the concentration effect. Playtest reported this as an
        //       over-correction the other way - too harsh a nerf.
        //       2/4, Flavor walked back up to Shield/Hull/Beam/Torp 1.35/1.24/1.40/1.40 - a midpoint
        //       between the too-strong 1.45/1.32/1.48/1.48 and the too-weak 1.25/1.15/1.32/1.32
        //       (EffHP≈1.00x, Offense≈0.82x). Playtest still reported this as weak.
        //       2/4, Flavor bumped again to Shield/Hull/Beam/Torp 1.40/1.28/1.44/1.44 (EffHP≈1.03x,
        //       Offense≈0.85x) - moved further back toward the original too-strong values. Playtest
        //       reported this as a bit too strong again.
        //       2/4, Flavor settled at Shield/Hull/Beam/Torp 1.38/1.26/1.42/1.42 - the midpoint of the
        //       previous two passes (1.35/1.24/1.40/1.40, too weak, and 1.40/1.28/1.44/1.44, a bit too
        //       strong). Presented but not yet playtested before the next pass superseded it.
        //       2/4, Flavor hand-picked at Shield/Hull/Beam/Torp 1.40/1.27/1.44/1.43 - user-specified
        //       directly, close to but not identical to the "a bit too strong" 1.40/1.28/1.44/1.44 pass
        //       (Hull and Torp trimmed a hair). EffHP≈1.03x, Offense≈0.85x, Beam≈0.76x. Not yet
        //       playtested before the next pass reverted it.
        //       2/4, Flavor back to Shield/Hull/Beam/Torp 1.40/1.28/1.44/1.44 - user re-selected the
        //       earlier "a bit too strong" values directly, undoing the 1.40/1.27/1.44/1.43 trim.
        //       EffHP≈1.03x, Offense≈0.85x, Beam≈0.76x.
        //       2/4, Beam/Torp bumped hard to 1.70/1.70 (Shield/Hull left at 1.40/1.28) - explicit
        //       request to bring Total Offense up to full Romulan parity (1.00x) rather than sitting at
        //       the concentration-effect-adjusted band's low edge. At only 2/4 ships this needed a much
        //       bigger per-ship jump than any other civ's Offense correction required, and pushed
        //       Beam-only to ≈0.90x - above every other civ's 0.78x-0.85x range.
        //       2/4 (current), Beam/Torp backed off to 1.60/1.60 (see
        //       ShipStatCalculator.Flavor[CivEnum.BORG]) - explicit pull-back from the full-parity
        //       pass. Total Offense ≈0.94x (down from 1.00x), Beam-only ≈0.85x (back at the top of the
        //       other six civs' 0.78x-0.85x range instead of above it). EffHP unchanged at ≈1.03x
        //       (Shield/Hull untouched throughout). Ship count held at 2/4 throughout all nine passes
        //       above (still the fewest of any civ) and Warp (0.85) has never been touched by any of
        //       this tuning.
        // Five of seven playable civs land in the same target band: Total Offense ≈0.84x-1.00x,
        // EffectiveHP ≈1.00x-1.10x, Beam-only ≈0.75x-0.85x (Romulan-relative) - CARD reaches it with
        // the MOST ships (8/8, weakest per-ship stats), majors in between. BORG and DOM both field FEWER
        // ships than the majors (2/4 and 3/5, strongest per-ship stats of the seven) and both were
        // deliberately tuned BELOW this band per the concentration-effect lesson above (a linearly-
        // "balanced" fleet-total ratio still reads too strong once that power is concentrated into so
        // few ships) - DOM currently sits at EffectiveHP≈0.99x/Offense≈0.84x/Beam≈0.78x, just below the
        // band. BORG has been pushed back up by explicit request, first to full 1.00x Offense parity,
        // then backed off to the current ≈0.94x Offense/≈0.85x Beam-only - now sitting at or just above
        // the majors' band instead of below it, unlike DOM. Treat both civs' numbers as still a work in
        // progress pending further playtest. DEVELOPED entries below reuse each civ's EARLY
        // Scout/Destroyer/Transport counts unchanged and add Cruisers - CARD gets one per Destroyer
        // (matching MajorStartingFleetComposition's DEVELOPED entry above), but DOM/BORG each get one
        // fewer than their Destroyer count, keeping their smaller-but-stronger fleet concentration
        // intent instead of just mirroring the majors' 1:1 ratio - not balance-tuned yet; treat these
        // as a first pass - like every other number in ShipStatCalculator.Flavor, tune from actual
        // CombatRecordings turn-log results rather than this static estimate alone.
        private static readonly Dictionary<CivEnum, Dictionary<TechLevel, Dictionary<ShipType, int>>> MajorStartingFleetCompositionOverrides =
            new Dictionary<CivEnum, Dictionary<TechLevel, Dictionary<ShipType, int>>>
            {
                [CivEnum.CARD] = new Dictionary<TechLevel, Dictionary<ShipType, int>>
                {
                    [TechLevel.EARLY] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 8 },
                        { ShipType.Destroyer, 8 },
                        { ShipType.Transport, 2 },
                    },
                    [TechLevel.DEVELOPED] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 8 },
                        { ShipType.Destroyer, 8 },
                        { ShipType.Transport, 2 },
                        { ShipType.Cruiser, 8 },
                    },
                    [TechLevel.ADVANCED] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 8 },
                        { ShipType.Destroyer, 8 },
                        { ShipType.Transport, 2 },
                        { ShipType.Cruiser, 8 },
                    },
                    // HvyCruiser = 25% of DEVELOPED's 8 Cruisers (exact), LtCruiser makes up the rest.
                    [TechLevel.SUPREME] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 8 },
                        { ShipType.Destroyer, 8 },
                        { ShipType.Transport, 2 },
                        { ShipType.LtCruiser, 6 },
                        { ShipType.HvyCruiser, 2 },
                    }
                },
                [CivEnum.DOM] = new Dictionary<TechLevel, Dictionary<ShipType, int>>
                {
                    [TechLevel.EARLY] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 3 },
                        { ShipType.Destroyer, 5 },
                        { ShipType.Transport, 2 },
                    },
                    [TechLevel.DEVELOPED] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 3 },
                        { ShipType.Destroyer, 5 },
                        { ShipType.Transport, 2 },
                        // One fewer than Destroyer count (unlike the majors' 1:1 Cruiser:Destroyer
                        // default) - DOM's per-ship stats are deliberately tuned above the majors'
                        // band (see the tuning notes further up this file), so a smaller Developed
                        // fleet keeps that concentration-effect intent instead of just mirroring EARLY.
                        { ShipType.Cruiser, 4 },
                    },
                    [TechLevel.ADVANCED] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 3 },
                        { ShipType.Destroyer, 5 },
                        { ShipType.Transport, 2 },
                        { ShipType.Cruiser, 4 },
                    },
                    // HvyCruiser = 25% of DEVELOPED's 4 Cruisers (exact); LtCruiser trimmed one
                    // further below that (2, not 3) per explicit user request.
                    [TechLevel.SUPREME] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 3 },
                        { ShipType.Destroyer, 5 },
                        { ShipType.Transport, 2 },
                        { ShipType.LtCruiser, 2 },
                        { ShipType.HvyCruiser, 1 },
                    }
                },
                [CivEnum.BORG] = new Dictionary<TechLevel, Dictionary<ShipType, int>>
                {
                    [TechLevel.EARLY] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 2 },
                        { ShipType.Destroyer, 4 },
                        { ShipType.Transport, 2 },
                    },
                    [TechLevel.DEVELOPED] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 2 },
                        { ShipType.Destroyer, 4 },
                        { ShipType.Transport, 2 },
                        // One fewer than Destroyer count, same reasoning as DOM above - Borg has the
                        // highest per-ship power of any civ, so its Developed fleet stays the smallest.
                        { ShipType.Cruiser, 3 },
                    },
                    [TechLevel.ADVANCED] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 2 },
                        { ShipType.Destroyer, 4 },
                        { ShipType.Transport, 2 },
                        { ShipType.Cruiser, 3 },
                    },
                    // HvyCruiser = 25% of DEVELOPED's 3 Cruisers (0.75, rounded to 1); LtCruiser
                    // trimmed one further below that (1, not 2) per explicit user request.
                    [TechLevel.SUPREME] = new Dictionary<ShipType, int>
                    {
                        { ShipType.Scout, 2 },
                        { ShipType.Destroyer, 4 },
                        { ShipType.Transport, 2 },
                        { ShipType.LtCruiser, 1 },
                        { ShipType.HvyCruiser, 1 },
                    }
                },
            };

        /// <summary>
        /// Get ships for a civilization's starting fleet.
        /// Major races (FED..TERRAN) get the composition defined in
        /// MajorStartingFleetCompositionOverrides for their civ+tech level if one exists, otherwise
        /// the shared MajorStartingFleetComposition baseline - minus Transport, which is left out of
        /// Fleet 1 entirely and instead docked at the home system by GetStartingHomeSystemShips.
        /// Minor races get 1 ship (Destroyer, or Scout as fallback).
        /// </summary>
        public List<ShipSO> GetStartingFleetShips(TechLevel techLevel, CivEnum civEnum)
        {
            List<ShipSO> allCivShips = GetShipSOListByCiv(civEnum);

            if (allCivShips == null || allCivShips.Count == 0)
            {
                Debug.LogWarning($"GetStartingFleetShips: No ships found for {civEnum}");
                return new List<ShipSO>();
            }

            // Filter by tech level - only actually needed below for majors. Minors search
            // allCivShips directly across every tech level instead (see that branch), since their
            // sparse per-civ roster (typically just one Destroyer at EARLY and one Cruiser at
            // DEVELOPED, no entries at ADVANCED/SUPREME at all) means techLevelShips can easily be
            // empty at the requested techLevel even though the civ has a perfectly usable Destroyer
            // one or more tiers down - bailing out here on an empty techLevelShips used to swallow
            // every minor race in that situation before it ever reached its own fallback logic.
            var techLevelShips = allCivShips
                .Where(s => s != null && s.TechLevel == techLevel)
                .ToList();

            // Check if major race (FED through TERRAN)
            bool isMajorRace = civEnum >= CivEnum.FED && civEnum <= CivEnum.TERRAN;

            if (isMajorRace)
            {
                if (techLevelShips.Count == 0)
                {
                    Debug.LogWarning($"GetStartingFleetShips: No ships found for {civEnum} at {techLevel}");
                    return new List<ShipSO>();
                }

                Dictionary<ShipType, int> composition;
                if (!(MajorStartingFleetCompositionOverrides.TryGetValue(civEnum, out var civOverrides)
                        && civOverrides.TryGetValue(techLevel, out composition))
                    && !MajorStartingFleetComposition.TryGetValue(techLevel, out composition))
                {
                    Debug.LogWarning($"GetStartingFleetShips: No starting-fleet composition defined for {civEnum} at {techLevel}");
                    return new List<ShipSO>();
                }

                List<ShipSO> startingFleet = new List<ShipSO>();
                foreach (var entry in composition)
                {
                    // Transports dock at the home system instead of joining Fleet 1's roster - see
                    // GetStartingHomeSystemShips, which pulls the same count from this composition
                    // table so the two stay in lockstep.
                    if (entry.Key == ShipType.Transport)
                        continue;

                    ShipSO template = techLevelShips.FirstOrDefault(s => s.ShipType == entry.Key);
                    if (template == null)
                    {
                        Debug.LogWarning($"GetStartingFleetShips: {civEnum} has no {entry.Key} at {techLevel}; skipping");
                        continue;
                    }

                    for (int i = 0; i < entry.Value; i++)
                        startingFleet.Add(template);
                }

                Debug.Log($"✅ GetStartingFleetShips: {civEnum} starting fleet has {startingFleet.Count} ships");
                return startingFleet;
            }
            else
            {
                // Minor races get ONE ship (Destroyer, or Scout as fallback). Searches allCivShips
                // (the civ's full roster across every tech level), not techLevelShips (restricted to
                // an exact match on the requested techLevel) - every minor race is only ever
                // authored a Destroyer at EARLY and a Cruiser at DEVELOPED (no Scout/Destroyer entry
                // at DEVELOPED+), so an exact-tech-level-only search left every minor with nothing
                // to build in any game started above EARLY, despite a perfectly good Destroyer
                // sitting one tier down in that same civ's own roster. Prefers the highest tech
                // Destroyer/Scout at or below techLevel (same "best available" logic as
                // GetShipSOAtBestTechLevel), then falls back to the lowest tech one above it if
                // there's nothing at or below - a minor civ only ends up with an empty starting
                // fleet if it truly has zero Destroyer/Scout ShipSO entries at any tech level.
                ShipSO bestShip = allCivShips
                    .Where(s => s != null && (s.ShipType == ShipType.Destroyer || s.ShipType == ShipType.Scout) && s.TechLevel <= techLevel)
                    .OrderByDescending(s => s.TechLevel)
                    .FirstOrDefault();

                if (bestShip == null)
                {
                    bestShip = allCivShips
                        .Where(s => s != null && (s.ShipType == ShipType.Destroyer || s.ShipType == ShipType.Scout))
                        .OrderBy(s => s.TechLevel)
                        .FirstOrDefault();
                }

                if (bestShip != null)
                {
                    if (bestShip.TechLevel != techLevel)
                    {
                        Debug.LogWarning($"⚠️ GetStartingFleetShips: Minor race {civEnum} has no Destroyer/Scout at {techLevel} - using {bestShip.ShipName} ({bestShip.ShipType} at {bestShip.TechLevel}) instead.");
                    }
                    return new List<ShipSO> { bestShip };
                }

                // Some minor civs have no Destroyer/Scout ShipSO at all for this tech level (e.g. the
                // asset was never authored, or its FBX went missing) - rather than leave them with
                // zero starting ships, fall back to the Federation's earliest destroyer (same asset
                // GetFallbackShipSO uses elsewhere for model-only fallback). Cloning it at runtime and
                // stamping the real civ back on top is deliberate: ShipDataInitializer.InitializeShipData
                // copies ShipSO.CivEnum for everything - name, quality lookup, and stat calculation, not
                // just the model - so returning the Federation asset unmodified would silently turn this
                // minor civ's ship into a Federation one (wrong stats, wrong civ ownership, wrong beam/
                // torpedo prefab selection). The clone keeps {civEnum}'s own identity/stats and only
                // borrows the Federation hull's mesh.
                ShipSO fallbackTemplate = GetFallbackShipSO();
                if (fallbackTemplate != null)
                {
                    ShipSO fallbackForThisCiv = ScriptableObject.Instantiate(fallbackTemplate);
                    fallbackForThisCiv.CivEnum = civEnum;
                    fallbackForThisCiv.ShipName = $"{civEnum}_DESTROYER_FALLBACK";
                    Debug.LogWarning($"⚠️ GetStartingFleetShips: Minor race {civEnum} has no destroyer or scout at {techLevel} - falling back to {fallbackTemplate.ShipName}'s model with {civEnum}'s own stats.");
                    return new List<ShipSO> { fallbackForThisCiv };
                }

                Debug.LogError($"❌ GetStartingFleetShips: Minor race {civEnum} has no destroyer or scout, and the Federation early-destroyer fallback is also unavailable!");
                return new List<ShipSO>();
            }
        }

        /// <summary>
        /// Get the starting transport ships that dock at a civ's home star system rather than
        /// joining Fleet 1's roster (see GetStartingFleetShips, which skips ShipType.Transport for
        /// this reason). Reads the Transport count from the same MajorStartingFleetComposition /
        /// MajorStartingFleetCompositionOverrides tables above, so it always matches whatever Fleet
        /// 1 would have carried. Minor races never get a starting transport.
        /// </summary>
        public List<ShipSO> GetStartingHomeSystemShips(TechLevel techLevel, CivEnum civEnum)
        {
            bool isMajorRace = civEnum >= CivEnum.FED && civEnum <= CivEnum.TERRAN;
            if (!isMajorRace) return new List<ShipSO>();

            List<ShipSO> allCivShips = GetShipSOListByCiv(civEnum);
            if (allCivShips == null || allCivShips.Count == 0) return new List<ShipSO>();

            var techLevelShips = allCivShips
                .Where(s => s != null && s.TechLevel == techLevel)
                .ToList();
            if (techLevelShips.Count == 0) return new List<ShipSO>();

            Dictionary<ShipType, int> composition;
            if (!(MajorStartingFleetCompositionOverrides.TryGetValue(civEnum, out var civOverrides)
                    && civOverrides.TryGetValue(techLevel, out composition))
                && !MajorStartingFleetComposition.TryGetValue(techLevel, out composition))
            {
                Debug.LogWarning($"GetStartingHomeSystemShips: No starting-fleet composition defined for {civEnum} at {techLevel}");
                return new List<ShipSO>();
            }

            if (!composition.TryGetValue(ShipType.Transport, out int transportCount) || transportCount <= 0)
                return new List<ShipSO>();

            ShipSO template = techLevelShips.FirstOrDefault(s => s.ShipType == ShipType.Transport);
            if (template == null)
            {
                Debug.LogWarning($"GetStartingHomeSystemShips: {civEnum} has no Transport at {techLevel}; skipping");
                return new List<ShipSO>();
            }

            List<ShipSO> startingTransports = new List<ShipSO>();
            for (int i = 0; i < transportCount; i++)
                startingTransports.Add(template);

            Debug.Log($"✅ GetStartingHomeSystemShips: {civEnum} home system starts with {startingTransports.Count} transport(s)");
            return startingTransports;
        }

        /// <summary>
        /// Find a usable FBX model prefab to stand in for a ShipSO that has none assigned.
        /// Search order: same civ + same ship type (any tech level) -> the global fallback
        /// ship SO (FED Destroyer EARLY) -> any civ + same ship type.
        /// Returns null if no FBX can be found anywhere.
        /// </summary>
        public GameObject GetFallbackFbx(ShipType shipType, CivEnum civEnum)
        {
            List<ShipSO> civShips = GetShipSOListByCiv(civEnum);
            ShipSO sameTypeSameCiv = civShips?.FirstOrDefault(s =>
                s != null && s.ShipType == shipType && s.ShipFBX_ModelAsGOPrefab != null);
            if (sameTypeSameCiv != null)
            {
                return sameTypeSameCiv.ShipFBX_ModelAsGOPrefab;
            }

            ShipSO globalFallback = GetFallbackShipSO();
            if (globalFallback != null && globalFallback.ShipFBX_ModelAsGOPrefab != null)
            {
                return globalFallback.ShipFBX_ModelAsGOPrefab;
            }

            var allLists = new[]
            {
                fedShipSOList, romShipSOList, klingShipSOList, cardShipSOList,
                domShipSOList, borgShipSOList, terranShipSOList, minorShipSOList
            };

            foreach (var list in allLists)
            {
                ShipSO anyCivSameType = list?.FirstOrDefault(s =>
                    s != null && s.ShipType == shipType && s.ShipFBX_ModelAsGOPrefab != null);
                if (anyCivSameType != null)
                {
                    return anyCivSameType.ShipFBX_ModelAsGOPrefab;
                }
            }

            return null;
        }
    }
}
