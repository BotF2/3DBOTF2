using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Galaxy;

namespace BOTF3D.UI
{
    /// <summary>
    /// Always-visible 40 px compact header on each SystemUI_Prefab list entry.
    /// Shows system name, dilithium stockpile, and antimatter stockpile; hosts the
    /// expand/collapse button. Attach to the CompactHeader child of SystemUI_Prefab
    /// and wire the serialized references in the Inspector.
    /// </summary>
    public class SysCompactHeader : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI sysNameTMP;
        [SerializeField] public TextMeshProUGUI dilithiumTMP;
        [SerializeField] public TextMeshProUGUI antimatterTMP;
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
            RefreshAntimatter();

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
        /// Refreshes the antimatter display from live StarSysData. Antimatter only ever
        /// changes at the turn boundary (StarSysManager.ProcessAntimatterFuelLoop banks
        /// Factory production and draws Power Plant consumption once per turn - see
        /// Docs/Design/Economy_Phase1_FuelLoop_FacilityCaps.md §1), which calls this
        /// directly for every system so the value stays live without the player needing
        /// to close/reopen or expand the panel. Also called from Populate/expand for the
        /// same reason RefreshDilithium is - so a freshly-opened panel is never stale.
        /// </summary>
        public void RefreshAntimatter()
        {
            if (antimatterTMP != null && _sysCon != null)
                antimatterTMP.text = _sysCon.StarSysData.AntimatterStockpile.ToString();
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
