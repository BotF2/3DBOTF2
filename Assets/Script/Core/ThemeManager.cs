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

public class ThemeManager : MonoBehaviour
{
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

    private void Awake()
    {
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

        // Set default theme (don't apply yet - images might not be ready)
        if (themeSOs != null && themeSOs.Length > 0)
        {
            CurrentTheme = themeSOs[0]; // Fed theme
            Debug.Log("ThemeManager: Default theme set to Fed");
        }
        else
        {
            Debug.LogError("ThemeManager: themeSOs array is empty! Assign ThemeSO assets in Inspector.");
        }
    }

    private void Start()
    {
        // Don't apply theme yet - UI elements might be in a scene that will unload
        // ApplyTheme will be called from MainMenuUIController.SetLocalCivilization()
        Debug.Log("ThemeManager: Start - theme will be applied when civilization is selected");
    }

    public void ApplyTheme(ThemeEnum themeEnum)
    {
        // Select the theme SO
        switch (themeEnum)
        {
            case ThemeEnum.Fed:
                CurrentTheme = themeSOs[0];
                break;
            case ThemeEnum.Rom:
                CurrentTheme = themeSOs[1];
                break;
            case ThemeEnum.Kling:
                CurrentTheme = themeSOs[2];
                break;
            case ThemeEnum.Card:
                CurrentTheme = themeSOs[3];
                break;
            case ThemeEnum.Dom:
                CurrentTheme = themeSOs[4];
                break;
            case ThemeEnum.Borg:
                CurrentTheme = themeSOs[5];
                break;
            case ThemeEnum.Terran:
                CurrentTheme = themeSOs[6];
                break;
            default:
                CurrentTheme = themeSOs.Length > 7 ? themeSOs[7] : themeSOs[0];
                break;
        }

        Debug.Log($"ThemeManager: Applying theme {themeEnum} (CurrentTheme set: {CurrentTheme != null})");

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

            // Note: MainMenu UI elements become null after scene unload - that's expected behavior
            Debug.Log($"ThemeManager: Theme {themeEnum} applied (MainMenu UI refs may be null after scene transition)");
        }
        else
        {
            Debug.LogError("ThemeManager: CurrentTheme is NULL after theme selection!");
        }
    }

    public ThemeSO GetLocalPlayerTheme()
    {
        return CurrentTheme;
    }
}

