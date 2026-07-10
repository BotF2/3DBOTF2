using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// A single orbital battery entry instantiated into OrbitalBatteryGrid (OB_Element prefab).
    /// Tints the icon image based on the battery's on/off state — powered-off batteries stay
    /// visible in the grid (still counting toward the grid's element limit) but grayed out.
    /// </summary>
    public class OrbitalBatteryIconUI : MonoBehaviour
    {
        private static readonly Color OnColor = Color.white;
        private static readonly Color OffColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Tooltip("Icon image tinted based on on/off state. Defaults to this GameObject's own Image if unassigned.")]
        [SerializeField] private Image iconImage;

        [HideInInspector] public GameObject SourceFacilityGO;

        private void Awake()
        {
            if (iconImage == null)
                iconImage = GetComponent<Image>();
        }

        public void SetOnOff(bool isOn)
        {
            if (iconImage != null)
                iconImage.color = isOn ? OnColor : OffColor;
        }
    }
}
