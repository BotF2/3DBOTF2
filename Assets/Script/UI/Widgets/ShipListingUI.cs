using BOTF3D.Combat;
using BOTF3D.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// Attached to ShipListingUI and ShipListingTransportUI prefabs.
    /// Assign shipTypeText, warpNumText, and hullBar in the Inspector.
    /// Call Populate(shipData) from ShipUICreator after instantiation.
    /// OnEnable re-reads the stored ShipData reference so the health bar
    /// updates automatically whenever the fleet/system menu panel is shown.
    /// </summary>
    public class ShipListingUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI shipTypeText;
        [SerializeField] private TextMeshProUGUI warpNumText;
        [SerializeField] private Image hullBar;

        private ShipData _shipData;

        public void Populate(ShipData shipData)
        {
            _shipData = shipData;
            if (shipData == null) return;

            if (shipTypeText != null)
                shipTypeText.text = FormatShipTypeLabel(shipData.ShipType, shipData.BuiltAtTechLevel);

            if (warpNumText != null)
                warpNumText.text = shipData.maxWarpFactor.ToString("0.##");

            ApplyHealthBar(hullBar, shipData);
        }

        private void OnEnable()
        {
            // Re-read live ShipData each time this listing becomes visible (post-combat, turn advance, etc.)
            if (_shipData != null)
                ApplyHealthBar(hullBar, _shipData);
        }

        public void RefreshHealth(ShipData shipData)
        {
            ApplyHealthBar(hullBar, shipData);
        }

        public static void ApplyHealthBar(Image bar, ShipData shipData)
        {
            if (bar == null || shipData == null) return;
            float fill = shipData.HullMaxHealth > 0
                ? Mathf.Clamp01((float)shipData.HullHealth / shipData.HullMaxHealth)
                : 1f;
            ApplyHealthBarFill(bar, fill);
        }

        /// <summary>
        /// Shared green/yellow/orange/red health-bar look, driven by a raw 0-1 fill fraction
        /// instead of a ShipData reference. Lets other callers (e.g. CombatScene's
        /// ShipController, which tracks shield+hull combined rather than hull alone) reuse the
        /// same color grading instead of maintaining their own copy of these thresholds.
        /// </summary>
        public static void ApplyHealthBarFill(Image bar, float fill)
        {
            if (bar == null) return;
            fill = Mathf.Clamp01(fill);
            bar.fillAmount = fill;
            bar.color = fill >= 0.99f ? new Color(0.1f, 0.8f, 0.1f)   // green  — full
                      : fill >= 0.66f ? new Color(1f,   0.8f, 0f)      // yellow — light damage
                      : fill >= 0.33f ? new Color(1f,   0.45f, 0f)     // orange — moderate damage
                      :                 new Color(0.9f,  0.1f, 0.1f);  // red    — heavy damage
        }

        public static string FormatShipTypeLabel(ShipType shipType, TechLevel techLevel) =>
            $"{ShipTypeDisplayName(shipType)} {TechLevelRoman(techLevel)}";

        public static string ShipTypeDisplayName(ShipType shipType)
        {
            switch (shipType)
            {
                case ShipType.LtCruiser:      return "Lt Cruiser";
                case ShipType.HvyCruiser:     return "Hvy Cruiser";
                case ShipType.OrbitalBattery: return "Orbital Bat.";
                default:                      return shipType.ToString();
            }
        }

        public static string TechLevelRoman(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.EARLY:     return "I";
                case TechLevel.DEVELOPED: return "II";
                case TechLevel.ADVANCED:  return "III";
                case TechLevel.SUPREME:   return "IV";
                default:                  return "I";
            }
        }
    }
}
