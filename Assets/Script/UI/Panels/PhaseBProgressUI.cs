using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// Shared grayscale-progress helper for the Phase B panels (SiegeDecisionUIController — attacker's
    /// "PanelSystemAssaultDectision", SiegeDefenseUIController — defender's "PanelSiegeDefence"). Each
    /// combat step gets its own sprite that starts normal color and lerps toward gray as that step's
    /// HP pool (PhaseBShieldHP, PhaseBPowerPlantHP, PhaseBTroopHP, PhaseBAttackerTroopHP — all on
    /// StarSysData) drains toward zero. A step the current AssaultMode never reaches (e.g. the power
    /// sprite under Target Troops/Total Destruction) stays normal via SetNormal — per design, it's not
    /// being targeted so it shouldn't visually imply progress.
    ///
    /// Both panels call this from the same PopulateStats() that already runs on OpenPanel and on every
    /// RefreshStats() tick, so no extra update hook is needed.
    /// </summary>
    public static class PhaseBProgressUI
    {
        private static readonly Color Normal = Color.white;
        private static readonly Color Depleted = Color.gray;

        /// <summary>Lerps img's color from Normal (current == max) to Depleted (current == 0). No-op if img is null.</summary>
        public static void SetProgress(Image img, float current, float max)
        {
            if (img == null) return;
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 1f;
            img.color = Color.Lerp(Depleted, Normal, ratio);
        }

        /// <summary>Forces img back to full normal color — this step isn't being targeted this assault.</summary>
        public static void SetNormal(Image img)
        {
            if (img != null) img.color = Normal;
        }

        /// <summary>Shows or hides the Prohibited overlay child Image for a step. No-op if overlay is null.</summary>
        public static void SetEliminated(Image overlay, bool eliminated)
        {
            if (overlay == null) return;
            overlay.gameObject.SetActive(eliminated);
        }
    }
}
