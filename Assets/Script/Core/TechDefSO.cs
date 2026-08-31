using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Core
{
    /// <summary>
    /// Phase II tech tree data model (TechTree_Phase2_Design.md §6, §8 phase II.1). One asset per
    /// row of TechTree_CommonBranches.csv (35 shared, IsShared=true) or TechTree_FactionUnique.csv
    /// (49 unique, IsShared=false) - see TechDefSOImporter for the generator that authors all 84
    /// from those two CSVs. Pure data, no logic - TechManager owns interpreting it (StartResearch,
    /// ApplyTechEffect, etc., none of which exist yet - II.2/II.3 still open).
    /// </summary>
    [CreateAssetMenu(fileName = "TechDefSO", menuName = "BOTF/Tech/TechDefSO")]
    public class TechDefSO : ScriptableObject
    {
        [Header("Identity")]
        // Stable id this tech is referenced by everywhere else (CivData.ResearchedTechIds,
        // BankedTechPointsByTechId, ActiveTechId) - never rename after data referencing it exists.
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;

        [Header("Placement")]
        public TechFieldEnum Field;
        // 0 = InnateFromStart (Branch F only, never queued). 1-7 = the shared threshold ladder
        // (CivData.TechThresholds-equivalent: T1=0, T2=100, T3=200, T4=300, T5=450, T6=600, T7=900).
        [Range(0, 7)] public int Tier;
        // TechTree_*.csv "TechPoints" column - the flat CivData.TechPoints a civ needs to have
        // banked before this tech is even offered, per the existing 7-stage ladder (§3.3). 0 for
        // every Tier-1 tech and for InnateFromStart entries.
        public int TechPointsThreshold;
        // TechTree_*.csv "TimeLine" column - turns to complete once queued as the active project,
        // baseline for one dedicated, unshared Research Center (§3.3). 0 for InnateFromStart, which
        // is never queued.
        public int ResearchCost;

        [Header("Unlock")]
        public TechUnlockMode UnlockMode;
        // True for the 35 Branch A-E techs (one asset, identical effect for all 7 civs - only
        // display flavor may vary, via TechFlavorSO). False for Branch F's 49 civ-specific entries.
        public bool IsShared = true;
        // Only meaningful when IsShared is false - which civ this Branch F entry belongs to.
        public CivEnum RestrictedToCiv;

        [Header("Effect")]
        public TechEffectHook EffectHook;
        // Placeholder magnitude - TechTree_Phase2_Design.md's suggested curve (§4: T1 1.00 -> T7
        // 1.65 for shared "+stat%" techs) hasn't been assigned per-tech yet. Real numbers are an
        // §8 II.5 balance-pass task, not part of II.1's data model.
        public float EffectMagnitude = 1f;

        [Header("Prerequisites (unused - see TechTree_Phase2_Design.md §10)")]
        // §10 "still open": current recommendation is threshold-only gating with no cross-branch
        // prerequisites, so this stays empty for every authored TechDefSO today. Reserved for if
        // that decision changes.
        public List<string> Prerequisites = new();
    }
}
