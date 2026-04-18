using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// Attach this component to any UI element that should change based on the active civilization theme
    /// Automatically updates when ThemeManager.ApplyTheme() is called
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class ThemedUIElement : MonoBehaviour
    {
        [Header("Theme Application")]
        [Tooltip("What aspect of this UI element should be themed?")]
        public ThemeTarget ThemeTarget = ThemeTarget.Button;

        [Header("Button Theme Settings")]
        [Tooltip("Which button spriteInsignia slot to use from ThemeSO (1-4)")]
        [Range(0, 3)]
        public int ButtonSpriteSlot = 0;

        [Header("Color Theme Settings")]
        [Tooltip("Which color to apply from ThemeSO")]
        public ThemeColorType ColorType = ThemeColorType.Primary;

        [Header("Image Insignia Theme Settings")]
        [Tooltip("Which themed insignia image to use")]
        public ThemeImageType InsigniaImageType = ThemeImageType.Insignia;

        [Header("Image Race Theme Settings")]
        [Tooltip("Which themed race image to use")]
        public ThemeImageType RaceImageType = ThemeImageType.Race;

        [Header("Auto-Apply")]
        [Tooltip("Automatically apply theme on Start?")]
        public bool ApplyOnStart = true;

        private Button button;
        private Image image;

        private void Awake()
        {
            button = GetComponent<Button>();
            image = GetComponent<Image>();
        }

        private void Start()
        {
            if (ApplyOnStart)
            {
                ApplyTheme();
            }
        }
        private void OnEnable()
        {
            // Re-apply theme when this UI element is enabled
            var themeManager = FindAnyObjectByType<ThemeManager>();
            if (themeManager != null && themeManager.CurrentTheme != null)
            {
                ApplyTheme();
            }
        }

        /// <summary>
        /// Apply the current theme to this UI element
        /// </summary>
        public void ApplyTheme()
        {
            var themeManager = FindAnyObjectByType<ThemeManager>();
            if (themeManager == null || themeManager.CurrentTheme == null)
            {
                Debug.LogWarning($"ThemedUIElement on {gameObject.name}: ThemeManager or CurrentTheme is null");
                return;
            }

            global::ThemeSO theme = themeManager.CurrentTheme;

            switch (ThemeTarget)
            {
                case ThemeTarget.Button:
                    ApplyButtonTheme(theme);
                    break;

                case ThemeTarget.Image:
                    ApplyImageTheme(theme);
                    break;

                case ThemeTarget.Text:
                    ApplyTextTheme(theme);
                    break;

                case ThemeTarget.BackgroundColor:
                    ApplyColorTheme(theme);
                    break;
            }
        }

        private void ApplyButtonTheme(global::ThemeSO theme)
        {
            if (button == null)
            {
                Debug.LogWarning($"ThemedUIElement on {gameObject.name}: No Button component found");
                return;
            }

            // Get the appropriate button sprite based on slot
            Sprite buttonSprite = GetButtonSprite(theme, ButtonSpriteSlot);

            if (buttonSprite == null)
            {
                Debug.LogWarning($"ThemedUIElement on {gameObject.name}: No spriteInsignia in slot {ButtonSpriteSlot}");
                return;
            }

            // Apply sprite to button's Image component
            if (image != null)
            {
                image.sprite = buttonSprite;
            }

            // Optional: Update button colors based on theme
            ColorBlock colors = button.colors;
            colors.normalColor = GetThemeColor(theme, ThemeColorType.Primary);
            colors.highlightedColor = GetThemeColor(theme, ThemeColorType.Highlight);
            colors.pressedColor = GetThemeColor(theme, ThemeColorType.LowLight);
            colors.selectedColor = GetThemeColor(theme, ThemeColorType.Highlight);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            Debug.Log($"Applied {theme.name} button theme (slot {ButtonSpriteSlot}) to {gameObject.name}");
        }

        private void ApplyImageTheme(global::ThemeSO theme)
        {
            if (image == null)
            {
                Debug.LogWarning($"ThemedUIElement on {gameObject.name}: No Image component found");
                return;
            }

            Sprite spriteInsignia = GetImageSprite(theme, InsigniaImageType);
            if (spriteInsignia != null)
            {
                image.sprite = spriteInsignia;
                Debug.Log($"Applied {theme.name} image ({InsigniaImageType}) to {gameObject.name}");
            }
            Sprite spriteRace = GetImageSprite(theme, RaceImageType);
            if (spriteRace != null)
            {
                image.sprite = spriteRace;
                Debug.Log($"Applied {theme.name} image ({RaceImageType}) to {gameObject.name}");
            }
        }

        private void ApplyTextTheme(global::ThemeSO theme)
        {
            // Get TMP_Text component dynamically to avoid TMPro dependency issues
            var textComponent = GetComponentInChildren(System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro"));
            if (textComponent != null)
            {
                var colorProp = textComponent.GetType().GetProperty("color");
                if (colorProp != null)
                {
                    colorProp.SetValue(textComponent, theme.TextColor);
                    Debug.Log($"Applied {theme.name} text color to {gameObject.name}");
                }
            }
        }

        private void ApplyColorTheme(global::ThemeSO theme)
        {
            if (image == null)
            {
                Debug.LogWarning($"ThemedUIElement on {gameObject.name}: No Image component found");
                return;
            }

            image.color = GetThemeColor(theme, ColorType);
            Debug.Log($"Applied {theme.name} color ({ColorType}) to {gameObject.name}");
        }

        private Sprite GetButtonSprite(global::ThemeSO theme, int slot)
        {
            switch (slot) // variants of button sprite.
            {
                case 0: return theme.ButtonSprite0;
                case 1: return theme.ButtonSprite1;
                case 2: return theme.ButtonSprite2;
                case 3: return theme.ButtonSprite3;
                default: return theme.ButtonSprite0;
            }
        }

        private Sprite GetImageSprite(global::ThemeSO theme, ThemeImageType imageType)
        {
            switch (imageType)
            {
                case ThemeImageType.Background: return theme.BackImage;
                case ThemeImageType.Insignia: return theme.Insignia;
                case ThemeImageType.Race: return theme.RaceImage;
                case ThemeImageType.System: return theme.SystemImage;
                case ThemeImageType.FleetShip: return theme.FleetShipImage;
                case ThemeImageType.PowerPlant: return theme.PowerPlantImage;
                case ThemeImageType.Factory: return theme.FactoryImage;
                case ThemeImageType.Shipyard: return theme.ShipyardImage;
                case ThemeImageType.Shield: return theme.ShieldImage;
                case ThemeImageType.OrbitalBattery: return theme.OrbitalBatteriesImage;
                case ThemeImageType.ResearchCenter: return theme.ResearchCenterImage;
                default: return null;
            }
        }

        private Color GetThemeColor(global::ThemeSO theme, ThemeColorType colorType)
        {
            switch (colorType)
            {
                case ThemeColorType.Background: return theme.BackgroundColor;
                case ThemeColorType.Foreground: return theme.ForegroundColor;
                case ThemeColorType.Border: return theme.BoarderColor;
                case ThemeColorType.Highlight: return theme.HighLightColor;
                case ThemeColorType.LowLight: return theme.LowLightColor;
                case ThemeColorType.Text: return theme.TextColor;
                case ThemeColorType.Primary: return theme.ForegroundColor;
                default: return Color.white;
            }
        }
    }

    public enum ThemeTarget
    {
        Button,
        Image,
        Text,
        BackgroundColor
    }

    public enum ThemeColorType
    {
        Background,
        Foreground,
        Border,
        Highlight,
        LowLight,
        Text,
        Primary
    }

    public enum ThemeImageType
    {
        Background,
        Insignia,
        Race,
        System,
        FleetShip,
        PowerPlant,
        Factory,
        Shipyard,
        Shield,
        OrbitalBattery,
        ResearchCenter
    }
}
