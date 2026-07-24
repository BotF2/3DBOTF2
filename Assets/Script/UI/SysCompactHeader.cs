using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Galaxy;

namespace BOTF3D.UI
{
    /// <summary>
    /// Always-visible 40 px compact header on each SystemUI_Prefab list entry.
    /// Shows system name and dilithium stockpile; hosts the expand/collapse button.
    /// Attach to the CompactHeader child of SystemUI_Prefab and wire the three
    /// serialized references in the Inspector.
    /// </summary>
    public class SysCompactHeader : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI sysNameTMP;
        [SerializeField] public TextMeshProUGUI dilithiumTMP;
        [SerializeField] public Button expandButton;

        private StarSysController _sysCon;

        /// <summary>
        /// Call once when the system UI is added to the list.
        /// Populates text and wires the expand button.
        /// </summary>
        public void Populate(StarSysController sysCon)
        {
            _sysCon = sysCon;

            if (sysNameTMP != null)
                sysNameTMP.text = sysCon.StarSysData.SysName;

            RefreshDilithium();

            if (expandButton != null)
            {
                expandButton.onClick.RemoveAllListeners();
                expandButton.onClick.AddListener(OnExpandClicked);
            }
        }

        /// <summary>
        /// Refreshes the dilithium display from live StarSysData.
        /// Call after any build that spends or awards dilithium.
        /// </summary>
        public void RefreshDilithium()
        {
            if (dilithiumTMP != null && _sysCon != null)
                dilithiumTMP.text = _sysCon.StarSysData.DilithiumStockpile.ToString();
        }

        /// <summary>
        /// Shows/hides the Expand button. Hidden whenever this system's ExpandedContent
        /// is already visible (top of list, or the sole system in the detail view) since
        /// there is nothing left for the button to do.
        /// </summary>
        public void SetExpandButtonActive(bool active)
        {
            if (expandButton != null)
                expandButton.gameObject.SetActive(active);
        }

        private void OnExpandClicked()
        {
            StarSysMenuUIController.Instance?.ExpandSystem(_sysCon);
        }
    }
}
