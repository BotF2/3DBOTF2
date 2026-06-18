using BOTF3D.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public enum ThemeEnum
{
    Fed,
    Rom,
    Kling,
    Card,
    Dom,
    Borg,
    Terran
}

namespace BOTF3D.UI
{

    public class ThemeManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        public static ThemeManager Instance;

        [Header("Theme ScriptableObjects")]
        [SerializeField] private ThemeSO[] themeSOs;
        [SerializeField] public ThemeSO CurrentTheme;

        [Header("UI Image References - Assign in Inspector")]
        // ToDo: should it be Sprite here? or Image? if we want to change the sprite of
        // an image component, we need a reference to the Image component, but
        // if we want to change the sprite of a button, we need a reference to the Sprite.
        // Maybe we can have both and assign them in the inspector?
        [SerializeField] private Image imageBackground;
        [SerializeField] private Image spriteInsignia;
        [SerializeField] private Image spriteRace;
        [SerializeField] private Image spriteSystem;
        [SerializeField] private Image spriteFleetShip;
        [SerializeField] private Image spritePowerPlant;
        [SerializeField] private Image spriteFactory;
        [SerializeField] private Image spriteShipyard;
        [SerializeField] private Image spriteShields;
        [SerializeField] private Image spriteOrbitalBatteries;
        [SerializeField] private Image spriteResearchCenter;

        [Header("Text and Button References")]
        [SerializeField] private Font[] fonts;
        [SerializeField] private TMP_Text[] tMP_Texts;
        [SerializeField] private Button[] buttons;

        // Event system for theme changes
        public delegate void ThemeChangedDelegate(ThemeSO newTheme);
        public static event ThemeChangedDelegate OnThemeChanged;

        private void Awake()
        {
            ServiceLocator.Register<ThemeManager>(this);
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (themeSOs != null && themeSOs.Length > 0)
                CurrentTheme = themeSOs[0];
            else
                GameLogger.LogError(GameLogger.LogCategory.UI, "ThemeManager: themeSOs array is empty — assign ThemeSO assets in Inspector.");
        }

        private void Start() { }

        public void ApplyTheme(ThemeEnum themeEnum)
        {
            int index = (int)themeEnum;
            if (themeSOs == null || index < 0 || index >= themeSOs.Length)
            {
                GameLogger.LogError(GameLogger.LogCategory.UI, $"ThemeManager: No ThemeSO at index {index} for {themeEnum}");
                return;
            }
            CurrentTheme = themeSOs[index];
            GameLogger.Log(GameLogger.LogCategory.UI, $"ThemeManager: Applying {themeEnum} theme");

            // IMPORTANT: These Image references are in MainMenuScene and become null after scene unload
            // That's OK - the CurrentTheme is what matters for gameplay UI
            if (CurrentTheme != null)
            {
                // Only apply to images if they exist (they're in MainMenu which might be unloaded)
                if (imageBackground != null)
                    imageBackground.color = CurrentTheme.BackgroundColor;

                if (spriteInsignia != null)
                    spriteInsignia.sprite = CurrentTheme.Insignia;

                if (spriteRace != null)
                    spriteRace.sprite = CurrentTheme.RaceImage;

                if (spriteSystem != null)
                    spriteSystem.sprite = CurrentTheme.SystemImage;

                if (spriteFleetShip != null)
                    spriteFleetShip.sprite = CurrentTheme.FleetShipImage;

                if (spritePowerPlant != null)
                    spritePowerPlant.sprite = CurrentTheme.PowerPlantImage;

                if (spriteFactory != null)
                    spriteFactory.sprite = CurrentTheme.FactoryImage;

                if (spriteShipyard != null)
                    spriteShipyard.sprite = CurrentTheme.ShipyardImage;

                if (spriteShields != null)
                    spriteShields.sprite = CurrentTheme.ShieldImage;

                if (spriteOrbitalBatteries != null)
                    spriteOrbitalBatteries.sprite = CurrentTheme.OrbitalBatteriesImage;

                if (spriteResearchCenter != null)
                    spriteResearchCenter.sprite = CurrentTheme.ResearchCenterImage;

                // ✅ BROADCAST THEME CHANGE TO ALL THEMED UI ELEMENTS
                NotifyThemeChanged();

            }
        }

        /// <summary>
        /// Notify all ThemedUIElement components to update
        /// </summary>
        private void NotifyThemeChanged()
        {
            // Trigger event for components listening
            OnThemeChanged?.Invoke(CurrentTheme);

            // Find all ThemedUIElement components in active scenes and update them
            var themedElements = FindObjectsByType<ThemedUIElement>(FindObjectsSortMode.None);
            foreach (var element in themedElements)
            {
                if (element != null && element.gameObject.activeInHierarchy)
                    element.ApplyTheme();
            }
        }

        /// <summary>
        /// Force refresh all themed UI elements in current scene
        /// </summary>
        public void RefreshAllThemedElements()
        {
            if (CurrentTheme != null)
            {
                NotifyThemeChanged();
            }
        }

        public ThemeSO GetLocalPlayerTheme()
        {
            return CurrentTheme;
        }

        /// <summary>
        /// Get theme for a specific civilization
        /// </summary>
        public ThemeSO GetThemeByCivEnum(CivEnum civEnum)
        {
            int index = (int)civEnum;
            if (index >= 0 && index < themeSOs.Length)
            {
                return themeSOs[index];
            }
            return CurrentTheme;
        }
    

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ThemeManager>(); }
    }
}