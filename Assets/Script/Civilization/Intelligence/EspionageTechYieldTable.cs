using System.Collections.Generic;
using BOTF3D.Core;

namespace BOTF3D.Civilization
{
    /// <summary>
    /// Star Trek lore-based tech yield per successful espionage project against each minor civ.
    /// Yield = approximate tech points a single successful project steals.
    /// Values reflect each civ's scientific sophistication in canon.
    /// Non-warp civs return 0 (enforced by HasWarp check in IntelligenceManager).
    /// </summary>
    public static class EspionageTechYieldTable
    {
        // Default for warp-capable civs not listed below
        public const int DefaultWarpCivYield = 50;

        private static readonly Dictionary<string, int> Yields = new Dictionary<string, int>
        {
            // ── Tier 1: Highly Advanced (180-250 TP) ──────────────────────────────
            { "VORGONS",    250 }, // 27th-century time travellers; technology centuries ahead
            { "TALOSIANS",  200 }, // Ancient mental-tech civilisation, illusion masters
            { "VULCANS",    200 }, // Millenia of logic-based science; warp pioneers
            { "VISSIANS",   200 }, // Early 22nd-century species already ahead of Starfleet
            { "THOLIANS",   180 }, // Crystalline entities with unique subspace/energy tech
            { "SIKARIANS",  180 }, // Spatial-folding trajector tech; 40,000 ly in an instant
            { "XINDI",      160 }, // Multi-species coalition; built a weapon to destroy Earth
            { "VAADWAUR",   160 }, // Ancient subspace-corridor network users
            { "SULIBAN",    140 }, // Genetically enhanced by temporal cold-war benefactors
            { "SONA",       140 }, // Ba'ku offshoots; metaphasic radiation research

            // ── Tier 2: Well Developed (100-150 TP) ──────────────────────────────
            { "ANDORIANS",  150 }, // Militaristic founders of the Federation; strong military tech
            { "TRILL",      150 }, // Joined species; millennia of accumulated knowledge
            { "ZAKDORN",    130 }, // Considered greatest strategic minds in the galaxy
            { "TELLARITES", 120 }, // Founding Federation members; sturdy engineering tradition
            { "YRIDIANS",   100 }, // Information brokers; broad access to galactic technology
            { "ZIBALIANS",  100 }, // Information traders of similar calibre to Yridians
            { "FERENGI",    100 }, // Business-focused but developed unique energy-whip and ship tech
            { "ORIONS",      90 }, // Pirate culture; accumulated diverse stolen tech
            { "BETAZOIDS",  100 }, // Peaceful Federation allies; advanced bioscience and telepathy
            { "TRABE",      100 }, // Once ruled the Kazon; decent pre-exile technology
            { "RISIANS",     50 }, // Pleasure planet; minimal military or research tech
            { "OCAMPA",      60 }, // Short-lived species guided by the Caretaker's technology
            { "TALARIANS",   80 }, // Militaristic human-like species; adequate warship tech
            { "TALAXIANS",   80 }, // Delta-quadrant scavengers; functional warp, basic shields

            // ── Tier 3: Basic Warp Civilisations (20-80 TP) ──────────────────────
            { "BAJORANS",    80 }, // Ancient spiritual culture; moderate industrial tech
            { "SELAY",       50 }, // Reptilian antagonists of the Anticans; basic warp only
            { "ANTICANS",    50 }, // Carnivorous species seeking Federation membership
            { "WADI",        80 }, // Game-obsessed species; some unique holosimulation tech
            { "ANGOSIANS",   60 }, // Federation candidate; enhanced soldiers, moderate science
            { "ANKARI",      40 }, // Small traders known for nucleogenic lifeform attraction
            { "KAZON",       40 }, // Barely comprehend the Trabe tech they stole; low innovation
            { "PAKLED",      20 }, // "We look for things to make us go." Almost zero original science
            { "SKRREEA",     40 }, // Refugee species; basic warp transport capability
            { "XANTHANS",    60 }, // Moderate tech; known for minor trade in the Alpha Quadrant
            { "ULIANS",      50 }, // Telepathic historians; limited material science
            { "ULLIANS",     50 }, // Same as above (alternate spelling in asset)
            { "ORNARANS",    40 }, // Drug-dependent species; minimal scientific progress
            { "NUMIRI",      50 }, // Delta-quadrant aggressors; functional warships, little more
            { "NUUBARI",     40 }, // Low-tech warp culture
            { "NYRIANS",     70 }, // Sophisticated enough to maintain biosphere habitats
            { "QUARREN",     60 }, // Industrial species; brainwashing technology notable
            { "ALGOLIANS",   60 }, // Attended Federation conferences; moderate science
            { "ALSAURIANS",  50 }, // Dominated by Mokra Order; basic tech
            { "ALDEANS",     90 }, // Custodians of a powerful defensive computer; tech forgotten/recovered
        };

        /// <summary>
        /// Returns lore-appropriate tech yield for the named civ.
        /// Returns 0 if non-warp; uses DefaultWarpCivYield for unlisted warp civs.
        /// </summary>
        public static int GetYield(string civShortName, bool hasWarp)
        {
            if (!hasWarp) return 0;
            if (Yields.TryGetValue(civShortName.ToUpper(), out int yield))
                return yield;
            return DefaultWarpCivYield;
        }
    }
}
