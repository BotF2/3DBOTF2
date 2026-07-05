using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// A single orbital battery entry instantiated into OrbitalBatterGrid.
    /// Sprite comes from the sibling ThemedUIElement (ImageType.OrbitalBattery); this
    /// component only tints the icon based on the battery's on/off state.
    /// </summary>
    public class OrbitalBatteryIconUI : MonoBehaviour
    {
        private static readonly Color OnColor = Color.white;
        private static readonly Color OffColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [HideInInspector] public GameObject SourceFacilityGO;

        private Image icon;

        private void Awake()
        {
            icon = GetComponent<Image>();
        }

        public void SetOnOff(bool isOn)
        {
            if (icon != null)
                icon.color = isOn ? OnColor : OffColor;
        }
    }
}
