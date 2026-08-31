using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Civilization;

namespace BOTF3D.UI
{
    /// <summary>
    /// Research-priority ranking control for the Tech Tree menu (TechTree_Phase2_Design.md §2a,
    /// §8 II.4). Lets the player reorder the 5 shared branches 1st-5th; TechManager.
    /// ApplyBranchPriorityIncome does the rest automatically every turn - this is the ONLY input
    /// the tech tree needs between re-ranks, by design (§2a: "code make it work in the background
    /// without more input"). This is deliberately just the ranking control, not the full node-list
    /// tech browser (tabs per branch, rows per tier, lock/available/in-progress/completed states)
    /// - that's a separate, still-open piece of §8 II.4.
    ///
    /// Lives on the panel assigned to GalaxyMenuUIController's techTreeMenuView slot (see that
    /// file's §8 note) - GalaxyMenuUIController.OpenMenu calls Refresh() every time the panel opens.
    /// </summary>
    public class TechTreeMenuUIController : MonoBehaviour
    {
        public static TechTreeMenuUIController Instance;

        [System.Serializable]
        private class PriorityRow
        {
            public TMP_Text RankLabel;   // "1st", "2nd", ...
            public TMP_Text BranchLabel; // branch name + its live % share of this turn's income
            public Button UpButton;      // swaps with the row above
            public Button DownButton;    // swaps with the row below
            // Optional - if you add a background Image to a row and wire it here, the whole row
            // gets a dimmed tint too, not just the text. Leave unassigned to rely on BranchLabel's
            // text color alone (no Editor changes needed for that).
            public Image RowBackground;
        }

        // One fixed text/background color PAIR per shared branch, chosen to contrast with each
        // other (not the same hue at different alpha - that just fades instead of contrasting,
        // e.g. blue text on a dimmed-blue background still reads as "blue on pale blue"). Text
        // colors are the same 5 hues as before; each background is that hue's complementary
        // pairing (blue text -> gold/yellow background, etc.) so a branch is easy to track by its
        // whole two-color identity, not just its text color, as it moves up/down the ranked list.
        private static readonly Dictionary<TechFieldEnum, (Color Text, Color Background)> BranchColors = new()
        {
            { TechFieldEnum.Propulsion,   (new Color32(0x12, 0x39, 0x5E, 0xFF), new Color32(0xF5, 0xD1, 0x42, 0xFF)) }, // dark navy on gold
            { TechFieldEnum.Tactical,     (new Color32(0x7A, 0x26, 0x20, 0xFF), new Color32(0xBF, 0xF2, 0xEA, 0xFF)) }, // dark brick red on pale cyan
            { TechFieldEnum.Ordnance,     (new Color32(0x7A, 0x4A, 0x12, 0xFF), new Color32(0xCD, 0xEA, 0xFB, 0xFF)) }, // dark amber/brown on pale sky-blue
            { TechFieldEnum.Science,      (new Color32(0x14, 0x5A, 0x50, 0xFF), new Color32(0xF7, 0xC9, 0xDE, 0xFF)) }, // dark teal on pale pink
            { TechFieldEnum.Intelligence, (new Color32(0x47, 0x2A, 0x6E, 0xFF), new Color32(0xE9, 0xEF, 0xC4, 0xFF)) }, // dark purple on pale chartreuse
        };

        [Header("Faction-Unique (always-on, outside the ranking - §2a)")]
        [SerializeField] private TMP_Text factionUniqueLabel;

        [Header("Ranked rows - index 0 is always the 1st-priority slot")]
        [SerializeField] private PriorityRow[] rows = new PriorityRow[5];

        private List<TechFieldEnum> currentOrder;
        private CivController civ;

        private void Awake()
        {
            Instance = this;

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                int index = i; // capture for the closures below
                rows[i].UpButton?.onClick.AddListener(() => MoveRow(index, -1));
                rows[i].DownButton?.onClick.AddListener(() => MoveRow(index, 1));
            }
        }

        /// <summary>Reloads the local player's SharedBranchPriority from CivData and redraws every
        /// row - call whenever the panel becomes visible, since it's a live view onto CivData, not
        /// a one-shot form (a turn advancing, or another client's change in a future multiplayer
        /// pass, could move it out from under a panel left open).</summary>
        public void Refresh()
        {
            civ = CivManager.Instance?.LocalPlayerCivController;
            if (civ?.CivData == null || TechManager.Instance == null)
            {
                Debug.LogWarning("TechTreeMenuUIController: no local player civ or TechManager yet - can't populate priority rows.");
                return;
            }

            currentOrder = new List<TechFieldEnum>(TechManager.Instance.GetSharedBranchPriority(civ));

            if (factionUniqueLabel != null)
                factionUniqueLabel.text = BuildFactionUniqueLabelText();

            RedrawRows();
        }

        /// <summary>Shows which civ this is and what its Faction-Unique branch is currently
        /// researching (TechDefSO.Description = the CSV's EffectSummary column) - the always-on
        /// 15% line has no rank slot to show a branch name in, so it shows the actual tech instead.</summary>
        private string BuildFactionUniqueLabelText()
        {
            TechDefSO target = TechManager.Instance.GetBranchTarget(civ, TechFieldEnum.FactionUnique);
            string civName = civ.CivData.CivLongName;

            if (target == null)
                return $"{civName} — Faction-Unique tree complete ({TechManager.FactionUniqueShare:P0} share, §2a)";

            return $"{civName} — {target.DisplayName}: {target.Description} ({TechManager.FactionUniqueShare:P0} share)";
        }

        private void RedrawRows()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || i >= currentOrder.Count) continue;

                TechFieldEnum field = currentOrder[i];
                float share = i < TechManager.RankShares.Length ? TechManager.RankShares[i] : 0f;

                if (rows[i].RankLabel != null) rows[i].RankLabel.text = OrdinalLabel(i + 1);
                if (rows[i].BranchLabel != null) rows[i].BranchLabel.text = $"{field} — {share:P0}";
                if (rows[i].UpButton != null) rows[i].UpButton.interactable = i > 0;
                if (rows[i].DownButton != null) rows[i].DownButton.interactable = i < rows.Length - 1;

                if (BranchColors.TryGetValue(field, out var colors))
                {
                    if (rows[i].BranchLabel != null) rows[i].BranchLabel.color = colors.Text;
                    if (rows[i].RowBackground != null) rows[i].RowBackground.color = colors.Background;
                }
            }
        }

        /// <summary>Swaps the row at index with its neighbor (direction -1 = up, +1 = down),
        /// commits the new order via TechManager.SetBranchPriority, and redraws immediately -
        /// re-ranking never loses banked progress (§2a), so this can apply live with no confirm
        /// step.</summary>
        private void MoveRow(int index, int direction)
        {
            if (currentOrder == null || civ == null) return;

            int target = index + direction;
            if (target < 0 || target >= currentOrder.Count) return;

            (currentOrder[index], currentOrder[target]) = (currentOrder[target], currentOrder[index]);

            TechManager.Instance.SetBranchPriority(civ, currentOrder);
            RedrawRows();
        }

        private static string OrdinalLabel(int rank)
        {
            switch (rank)
            {
                case 1: return "1st";
                case 2: return "2nd";
                case 3: return "3rd";
                default: return $"{rank}th";
            }
        }
    }
}
