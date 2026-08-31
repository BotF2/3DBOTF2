using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Core
{
    /// <summary>
    /// Phase II tech tree data model (TechTree_Phase2_Design.md §6, §8 phase II.1). One instance per
    /// civ, overriding display name/description/icon for the 35 *shared* TechDefSOs only - Branch F
    /// techs already carry their own name/flavor directly on their TechDefSO (TechTree_Phase2_Design.md
    /// §5's per-civ table is the source for those, not this asset).
    ///
    /// No source CSV for this data exists yet (TechTree_CommonBranches.csv is the same 35 techs for
    /// every civ by design - only the design doc's prose ever names a civ-flavored variant, e.g.
    /// "Warp Core Stabilization" reads the same for every civ today). Authoring the 7 per-civ
    /// TechFlavorSO assets (one per playable civ) with actual flavor text is still open - this class
    /// only defines the shape §6/§7's TechBalanceValidator checks for ("missing TechFlavorSO
    /// overrides so no civ ships a shared tech under its raw internal name").
    /// </summary>
    [CreateAssetMenu(fileName = "TechFlavorSO", menuName = "BOTF/Tech/TechFlavorSO")]
    public class TechFlavorSO : ScriptableObject
    {
        public CivEnum Civ;
        public List<Entry> Overrides = new();

        [Serializable]
        public class Entry
        {
            // Must match a shared TechDefSO.Id (TechDefSO.IsShared == true).
            public string TechId;
            public string DisplayName;
            [TextArea] public string Description;
            public Sprite Icon;
        }

        public bool TryGetOverride(string techId, out Entry entry)
        {
            for (int i = 0; i < Overrides.Count; i++)
            {
                if (Overrides[i].TechId == techId)
                {
                    entry = Overrides[i];
                    return true;
                }
            }
            entry = null;
            return false;
        }
    }
}
