using BOTF3D.Combat;
using BOTF3D.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// One row in the ScrapPanel ship list. Populated by ScrapPanelUIController.
    /// Attach to the ScrapShipRow prefab.
    /// </summary>
    public class ScrapShipRowUI : MonoBehaviour
    {
        [SerializeField] private Image shipIcon;
        [SerializeField] private TextMeshProUGUI shipNameText;
        [SerializeField] private TextMeshProUGUI techTierText;
        [SerializeField] private TextMeshProUGUI dilithiumRecoveryText;
        [SerializeField] private GameObject obsoleteBadge;   // "OBSOLETE" label — active when ship is behind civ tech
        [SerializeField] private Image selectionHighlight;   // tinted overlay, active when this row is selected
        [SerializeField] private Button rowButton;

        public ShipController ShipController { get; private set; }

        public void Populate(ShipController shipCon, TechLevel civCurrentTech, System.Action<ScrapShipRowUI> onSelected)
        {
            ShipController = shipCon;
            var data = shipCon.ShipData;

            if (shipIcon != null)
                shipIcon.sprite = data.ShipSprite;

            if (shipNameText != null)
                shipNameText.text = $"{ShipListingUI.ShipTypeDisplayName(data.ShipType)}  {data.ShipName}";

            int builtTier = (int)data.BuiltAtTechLevel;
            int civTier   = (int)civCurrentTech;
            int gap       = civTier - builtTier;

            if (techTierText != null)
                techTierText.text = gap > 0
                    ? $"Tier {builtTier}  (current: {civTier})"
                    : $"Tier {builtTier}";

            if (dilithiumRecoveryText != null)
                dilithiumRecoveryText.text = $"{data.DilithiumCost} Li2";

            if (obsoleteBadge != null)
                obsoleteBadge.SetActive(gap >= 1);

            SetSelected(false);

            if (rowButton != null)
            {
                rowButton.onClick.RemoveAllListeners();
                rowButton.onClick.AddListener(() => onSelected?.Invoke(this));
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.enabled = selected;
        }
    }
}
