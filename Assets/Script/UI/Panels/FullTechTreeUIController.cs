using System.Collections.Generic;
using TMPro;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;

namespace BOTF3D.UI
{
    /// <summary>
    /// Full tech-tree browser (TechTree_Phase2_Design.md §8 II.4) - a read-only listing of every
    /// tech in every branch, color-coded by state (completed / currently researching / not yet
    /// reached). Complements TechTreeMenuUIController's ranking control, which only ever shows the
    /// single "what's next" pick per branch; this shows the whole ladder at once.
    ///
    /// Fixed 6-column x 7-row grid (5 shared branches + Faction-Unique, 7 techs each - Branch F's
    /// free Tier-0 innate plus its 6 researched entries) built once in the Editor, not instantiated
    /// at runtime - the count never changes for a given civ, so there's nothing a prefab+scroll
    /// pattern would buy here (see DiplomacyMenuUIController's own pre-placed-array convention for
    /// the same reasoning applied to a different fixed-size panel).
    ///
    /// Lives as a child of TechTreePanel (opened by ButtonTechTree there) so it closes automatically
    /// whenever the Tech Tree menu itself closes, rather than needing its own top-level Menu entry.
    /// </summary>
    public class FullTechTreeUIController : MonoBehaviour
    {
        public static FullTechTreeUIController Instance;

        [Header("Column order - must match the 6 header texts left-to-right in the Editor")]
        [SerializeField]
        private TechFieldEnum[] columnFields =
        {
            TechFieldEnum.Propulsion, TechFieldEnum.Tactical, TechFieldEnum.Ordnance,
            TechFieldEnum.Science, TechFieldEnum.Intelligence, TechFieldEnum.FactionUnique
        };

        [Header("Civ-specific header - text is replaced with the local civ's short name on Refresh")]
        [SerializeField] private TMP_Text civSpecificHeader;

        [System.Serializable]
        private class Column
        {
            // Index 0 = this branch's lowest tier (Tier 1, or FactionUnique's Innate slot) through
            // index 6 = Tier 7 - same top-to-bottom order GetBranchTechs returns them in.
            public TMP_Text[] Rows = new TMP_Text[7];
        }

        [Header("6 columns x 7 rows, same left-to-right order as Column Fields above")]
        [SerializeField] private Column[] columns = new Column[6];

        // Fixed state colors, independent of branch identity (that's TechTreeMenuUIController's
        // BranchColors job) - this panel is about WHAT STATE a tech is in, not which branch it's in.
        // Four states, not three: a tech can be not-yet-researched for two very different reasons -
        // its tier is already unlocked and it's simply queued behind an earlier tech in the same
        // branch (QueuedColor), or its TechPointsThreshold hasn't even been reached yet (LockedColor)
        // - these used to render identically, which is what prompted splitting them out.
        private static readonly Color CompletedColor = new Color32(0x2E, 0x7D, 0x32, 0xFF);   // green
        private static readonly Color ResearchingColor = new Color32(0xE0, 0xA0, 0x30, 0xFF); // amber
        private static readonly Color QueuedColor = new Color32(0x8F, 0xA9, 0xC2, 0xFF);      // light steel-blue - unlocked, waiting its turn
        private static readonly Color LockedColor = new Color32(0x3C, 0x3C, 0x3C, 0xFF);      // dark grey - tier not reached yet

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>Shows the panel and redraws every column from the local player's current
        /// research state. Wire ButtonTechTree's OnClick directly to this in the Inspector.</summary>
        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        /// <summary>Redraws every cell - safe to call anytime the panel is open (e.g. after a turn
        /// advances while the player left it open), not just on first show.</summary>
        public void Refresh()
        {
            CivController civ = CivManager.Instance?.LocalPlayerCivController;
            if (civ?.CivData == null || TechManager.Instance == null)
            {
                Debug.LogWarning("FullTechTreeUIController: no local player civ or TechManager yet - can't populate the tech grid.");
                return;
            }

            if (civSpecificHeader != null)
                civSpecificHeader.text = civ.CivData.CivShortName;

            for (int c = 0; c < columns.Length && c < columnFields.Length; c++)
            {
                if (columns[c] == null) continue;

                TechFieldEnum field = columnFields[c];
                List<TechDefSO> defs = TechManager.Instance.GetBranchTechs(civ, field);
                TechDefSO current = TechManager.Instance.GetBranchTarget(civ, field);

                for (int r = 0; r < columns[c].Rows.Length; r++)
                {
                    TMP_Text rowText = columns[c].Rows[r];
                    if (rowText == null) continue;

                    if (defs == null || r >= defs.Count)
                    {
                        rowText.text = string.Empty;
                        continue;
                    }

                    TechDefSO def = defs[r];
                    rowText.text = def.Tier == 0 ? $"Innate — {def.DisplayName}" : $"T{def.Tier} — {def.DisplayName}";

                    if (civ.CivData.ResearchedTechIds.Contains(def.Id))
                        rowText.color = CompletedColor;
                    else if (current != null && current.Id == def.Id)
                        rowText.color = ResearchingColor;
                    else if (civ.CivData.TechPoints >= def.TechPointsThreshold)
                        rowText.color = QueuedColor; // tier unlocked, just waiting behind an earlier tech in this branch
                    else
                        rowText.color = LockedColor; // tier not reached yet
                }
            }
        }
    }
}
