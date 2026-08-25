using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;



namespace BOTF3D.UI
{
    /// <summary>
    /// Attach this component to any UI element that should change based on the active civilization theme
    /// Automatically updates when ThemeManager.ApplyTheme() is called
    /// </summary>
    public class ThemedUIElement : MonoBehaviour
    {
        [Header("Theme Application")]
        [Tooltip("What aspect of this UI element should be themed?")]
        public ThemeTarget ThemeTarget = ThemeTarget.Button;

        [Header("Button Theme Settings")]
        [Tooltip("Which button sprite slot to use from ThemeSO (0-3)")]
        [Range(0, 3)]
        public int ButtonSpriteSlot = 0;

        [Header("Color Theme Settings")]
        [Tooltip("Which color to apply from ThemeSO")]
        public ThemeColorType ColorType = ThemeColorType.Primary;

        [Header("Sprite / Image Settings")]
        [Tooltip("Which image from ThemeSO to display (used when ThemeTarget is Image)")]
        public ThemeImageType ImageType = ThemeImageType.Insignia;

        [Header("Border Settings (only used when ImageType is Border)")]
        [Tooltip("Transform whose localScale this element's localScale should match (e.g. GalaxyImage), so the border always frames it exactly")]
        public Transform ScaleSyncSource;
        [Tooltip("Metallic value applied to this element's material when ImageType is Border")]
        [Range(0, 1)]
        public float BorderMetallic = 0.8f;
        [Tooltip("Smoothness value applied to this element's material when ImageType is Border")]
        [Range(0, 1)]
        public float BorderSmoothness = 0.6f;

        [Header("Auto-Apply")]
        [Tooltip("Automatically apply theme on Start?")]
        public bool ApplyOnStart = true;

        private Button button;
        private Image image;
        private SpriteRenderer spriteRenderer;

        // Reused across ApplyTheme calls to avoid cloning a new Material instance every time
        // (see ApplyBorderTransformAndMaterial). Constructed in Awake(), not as a field
        // initializer — Unity disallows native object construction in a MonoBehaviour's
        // implicit constructor/field initializers.
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private MaterialPropertyBlock borderPropertyBlock;

        private void Awake()
        {
            button          = GetComponent<Button>();
            image           = GetComponent<Image>() ?? GetComponentInChildren<Image>();
            spriteRenderer  = GetComponent<SpriteRenderer>();
            borderPropertyBlock = new MaterialPropertyBlock();
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
            if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
                ApplyTheme();
        }

        public void ApplyTheme()
        {
            if (ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.UI, $"ThemedUIElement on {gameObject.name}: ThemeManager or CurrentTheme is null");
                return;
            }

            global::ThemeSO theme = ThemeManager.Instance.CurrentTheme;

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
                GameLogger.LogWarning(GameLogger.LogCategory.UI, $"ThemedUIElement on {gameObject.name}: No Button component found");
                return;
            }

            Sprite buttonSprite = GetButtonSprite(theme, ButtonSpriteSlot);
            if (buttonSprite == null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.UI, $"ThemedUIElement on {gameObject.name}: No sprite in slot {ButtonSpriteSlot}");
                return;
            }

            if (image != null)
                image.sprite = buttonSprite;

            ColorBlock colors = button.colors;
            colors.normalColor = GetThemeColor(theme, ThemeColorType.Primary);
            colors.highlightedColor = GetThemeColor(theme, ThemeColorType.Highlight);
            colors.pressedColor = GetThemeColor(theme, ThemeColorType.LowLight);
            colors.selectedColor = GetThemeColor(theme, ThemeColorType.Highlight);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;
        }

        private void ApplyImageTheme(global::ThemeSO theme)
        {
            Sprite sprite = GetImageSprite(theme, ImageType);
            if (sprite == null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.UI, $"ThemedUIElement on {gameObject.name}: No sprite for {ImageType} in current theme");
                return;
            }

            if (image != null)
            {
                image.sprite = sprite;
                return;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                if (ImageType == ThemeImageType.Border)
                    ApplyBorderTransformAndMaterial();
                return;
            }

            GameLogger.LogWarning(GameLogger.LogCategory.UI, $"ThemedUIElement on {gameObject.name}: No Image or SpriteRenderer found");
        }

        private void ApplyBorderTransformAndMaterial()
        {
            if (ScaleSyncSource != null)
                transform.localScale = ScaleSyncSource.localScale;

            // Use a MaterialPropertyBlock instead of spriteRenderer.material so re-applying the
            // theme doesn't clone a new Material instance per border element every time.
            spriteRenderer.GetPropertyBlock(borderPropertyBlock);
            borderPropertyBlock.SetFloat(MetallicId, BorderMetallic);
            borderPropertyBlock.SetFloat(SmoothnessId, BorderSmoothness);
            spriteRenderer.SetPropertyBlock(borderPropertyBlock);
        }

        private void ApplyTextTheme(global::ThemeSO theme)
        {
            TMP_Text tmp = GetComponent<TMP_Text>() ?? GetComponentInChildren<TMP_Text>();
            if (tmp == null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.UI, $"ThemedUIElement on {gameObject.name}: No TMP_Text component found");
                return;
            }

            tmp.color = theme.TextColor;
            if (theme.CivFont != null)
                tmp.font = theme.CivFont;
        }

        private void ApplyColorTheme(global::ThemeSO theme)
        {
            if (image == null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.UI, $"ThemedUIElement on {gameObject.name}: No Image component found");
                return;
            }

            image.color = GetThemeColor(theme, ColorType);
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
                case ThemeImageType.SystemMenuCompactBackground: return theme.SystemMenuCompactBackground;
                case ThemeImageType.FleetShip: return theme.FleetShipImage;
                case ThemeImageType.PowerPlant: return theme.PowerPlantImage;
                case ThemeImageType.Factory: return theme.FactoryImage;
                case ThemeImageType.Shipyard: return theme.ShipyardImage;
                case ThemeImageType.Shield: return theme.ShieldImage;
                case ThemeImageType.OrbitalBattery: return theme.OrbitalBatteriesImage;
                case ThemeImageType.ResearchCenter: return theme.ResearchCenterImage;
                case ThemeImageType.Border: return theme.BorderImage;
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
        ResearchCenter,
        Border,
        SystemMenuCompactBackground
    }
}
