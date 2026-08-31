
using BOTF3D.Core;
using BOTF3D.Galaxy;
using System.Collections.Generic;
using UnityEngine;



namespace BOTF3D.Civilization
{
    public class CivData // has list of civsInGame, it's starSytems Data
    {
        public int CivInt;
        public CivEnum CivEnum;
        public int PlayerId; // network player ID, not used in single player
        public string CivShortName;
        public string CivLongName;
        public string CivHomeSystemName;
        public Vector3 HomeStarSystemPosition;
        public WarLikeEnum Warlike;// a enum scale from most work like 0 to neutral 3 and most peaceful 5
        public XenophobiaEnum Xenophobia; // XenophobiaEnum
        public RuthlessEnum Ruthless; //XenophobiaEnum
        public GreedyEnum Greedy; //XenophobiaEnum
        public Sprite CivRaceSprite;
        public Sprite InsigniaSprite;
        public static readonly Dictionary<TechLevel, int> TechThresholds = new()
        {
            { TechLevel.EARLY, 0 },
            { TechLevel.DEVELOPED, 100 },
            { TechLevel.ADVANCED, 300 },
            { TechLevel.SUPREME, 600 }
        };
        public int TechPoints { get; set; } = 0;

        // Derived from TechPoints — no separate field to go out of sync.
        public TechLevel CurrentTechLevel => TechLevelForPoints(TechPoints);

        public static TechLevel TechLevelForPoints(int points)
        {
            if (points >= 600) return TechLevel.SUPREME;
            if (points >= 300) return TechLevel.ADVANCED;
            if (points >= 100) return TechLevel.DEVELOPED;
            return TechLevel.EARLY;
        }

        // Continuous 0-10 scale: EARLY 0-2.5 | DEVELOPED 2.5-5 | ADVANCED 5-7.5 | SUPREME 7.5-10
        public float TechRating
        {
            get
            {
                const float band = 2.5f;
                if (TechPoints >= 600) return band * 3f + Mathf.Clamp01((TechPoints - 600) / 400f) * band;
                if (TechPoints >= 300) return band * 2f + Mathf.Clamp01((TechPoints - 300) / 300f) * band;
                if (TechPoints >= 100) return band       + Mathf.Clamp01((TechPoints - 100) / 200f) * band;
                return Mathf.Clamp01(TechPoints / 100f) * band;
            }
        }
        // Average of the four -2..+2 personality traits. All four enums share the convention
        // that +2 is the more cooperative/trusting pole (Pacifist/Compassion/Honorable/Idealistic),
        // so this lands Federation-like civs near +1.5/+2 and Romulan/Cardassian/Dominion-like
        // civs near -1/-1.5, scaling every diplomacy-menu gesture (trade, aid, alliance, etc.).
        public float DiplomaticAptitude => ((int)Warlike + (int)Xenophobia + (int)Ruthless + (int)Greedy) / 4f;

        public bool Playable;
        public bool PlayedByAI = true;

        // Set once by CivManager.CheckForEliminatedCivs when a playable civ owns zero star systems
        // and has zero fleets left. An eliminated civ is never removed from the game (its
        // CivController/CivData keep existing for save/report/UI purposes) - this flag is instead
        // read by TimeManager to auto-ready it every InterTurn (see IsCivEliminated) so it can never
        // block or slow turn advancement for everyone else. Never cleared once set - there is no
        // "un-eliminate" path today (no facility/ground-invasion recapture mechanic exists yet).
        public bool IsEliminated;
        public CivEnum LocalPlayerCivEnum;
        public bool HasWarp;
        public string Decription = "We are the Borg";
        public List<StarSysController> StarSysWeOwn;
        //public List<CivController> CivControllersWeKnow;
        //public List<CivEnum> CivEnumsWeKnow;
        public float IntelPoints;
        public int QualityScore = 5;
        private object SystemsOwned;
        public int PendingBuildTimeReduction = 0; // Consumed by next ship build at any owned shipyard; set from captured-ship BuildDuration / 2

        // Phase II tech tree (TechTree_Phase2_Design.md §2a, §6, §8 II.2) - weighted-parallel
        // branch-priority model. Written/read by TechManager's per-turn research tick.
        //
        // Every TechDefSO.Id this civ has completed - checked once per tech, never cleared.
        public HashSet<string> ResearchedTechIds = new();
        // Every tech's banked TechPoints, including every branch's simultaneously - this is what
        // makes re-ranking SharedBranchPriority lossless (§2a): a branch's target resumes from here
        // rather than 0 when its rank (or the player's priority order) changes. Keyed by TechDefSO.Id.
        public Dictionary<string, int> BankedTechPointsByTechId = new();
        // The player's 1st-5th rank over the 5 *shared* branches (Propulsion/Tactical/Ordnance/
        // Science/Intelligence) - FactionUnique is never in this list, it gets its own always-on
        // fixed share regardless of ranking (§2a). TechManager seeds this with a default order the
        // first time it processes a civ that hasn't set one yet.
        public List<TechFieldEnum> SharedBranchPriority;
        // Optional manual override, one per TechFieldEnum, letting a player target a later tech in
        // a branch instead of that branch's natural lowest-not-yet-researched pick (§2a/§6). Absent
        // key = use the natural pick. Cleared automatically the moment the pinned tech completes.
        public Dictionary<TechFieldEnum, string> ManualTechPinByField = new();

        /// <summary>
        /// Get power efficiency multiplier based on tech level
        /// Uses TechManager for centralized tech bonuses
        /// </summary>
        public float GetPowerTechMultiplier()
        {
            if (TechManager.Instance != null)
            {
                return TechManager.Instance.GetPowerEfficiencyMultiplier(CurrentTechLevel);
            }

            // Fallback if TechManager not available
            switch (CurrentTechLevel)
            {
                case TechLevel.EARLY: return 1.0f;
                case TechLevel.DEVELOPED: return 1.2f;
                case TechLevel.ADVANCED: return 1.5f;
                case TechLevel.SUPREME: return 2f;
                default: return 1.0f;
            }
        }
        /// <summary>
        /// Calculate total power output across all owned systems
        /// </summary>
        public float CalculateTotalEmpirePower()
        {
            float totalPower = 0f;
            float techMultiplier = GetPowerTechMultiplier();

            // Loop through all owned systems
            var civController = CivManager.Instance?.GetCivControllerByCivEnum(CivEnum);
            if (civController?.CivData?.StarSysWeOwn != null)
            {
                foreach (var system in civController.CivData.StarSysWeOwn)
                {
                    if (system?.StarSysData != null)
                    {
                        totalPower += system.StarSysData.CalculateTotalPower(techMultiplier);
                    }
                }
            }

            return totalPower;
        }

        /// <summary>
        /// Add tech points and check for level advancement
        /// Convenience method that delegates to TechManager
        /// </summary>
        public void AddTechPoints(int points)
        {
            if (TechManager.Instance != null)
                TechManager.Instance.AddResearchPoints(CivManager.Instance.GetCivControllerByCivEnum(CivEnum), points);
            else
                TechPoints += points; // level-up event skipped when TechManager absent
        }

        /// <summary>
        /// Get progress toward next tech level (0-1)
        /// </summary>
        public float GetTechProgress()
        {
            if (TechManager.Instance == null) return 0f;
            return TechManager.Instance.GetProgressToNextLevel(this);
        }
    }
}

