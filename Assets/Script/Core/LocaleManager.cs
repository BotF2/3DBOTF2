using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace BOTF3D.Core
{
    /// <summary>
    /// Manages language localization using Unity's Localization package.
    /// Supports English, German, French, and other languages.
    /// </summary>
    public class LocaleManager : MonoBehaviour
    {
        public static LocaleManager Instance;

        [Header("Current Language")]
        [SerializeField] private string currentLanguageCode = "en";

        [Header("Localization")]
        [SerializeField] private Button buttonEnglish;
        [SerializeField] private Button buttonFrench;
        [SerializeField] private Button buttonGerman;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            // ✅ Set default language (English) at startup
            SetLanguageByCode(currentLanguageCode);
        }

        /// <summary>
        /// Changes the game language to the specified locale.
        /// </summary>
        public void ChangeLanguage(Locale newLocale)
        {
            if (newLocale == null)
            {
                Debug.LogError("LocaleManager.ChangeLanguage: newLocale is NULL!");
                return;
            }

            if (!Application.isPlaying)
            {
                Debug.LogWarning("LocaleManager.ChangeLanguage: Can only change language during play mode");
                return;
            }

            LocalizationSettings.SelectedLocale = newLocale;
            currentLanguageCode = newLocale.Identifier.Code;

            Debug.Log($"✅ Language changed to: {newLocale.name} ({newLocale.Identifier.Code})");
        }

        /// <summary>
        /// Changes language using language code (en, fr, de, etc.)
        /// </summary>
        public void SetLanguageByCode(string code)
        {
            var locale = GetLocaleByCode(code);
            if (locale != null)
            {
                ChangeLanguage(locale);
            }
            else
            {
                Debug.LogError($"LocaleManager: Could not find locale for code '{code}'");
            }
        }

        /// <summary>
        /// Gets a Locale by ISO language code.
        /// </summary>
        public Locale GetLocaleByCode(string code)
        {
            if (LocalizationSettings.AvailableLocales == null)
            {
                Debug.LogError("LocaleManager: LocalizationSettings.AvailableLocales is NULL! Is the Localization package configured?");
                return null;
            }

            var availableLocales = LocalizationSettings.AvailableLocales.Locales;

            foreach (var locale in availableLocales)
            {
                if (locale.Identifier.Code == code)
                {
                    return locale;
                }
            }

            Debug.LogWarning($"Locale '{code}' not found! Available: {string.Join(", ", availableLocales.Select(l => l.Identifier.Code))}");
            return null;
        }

        /// <summary>
        /// Gets the current language code.
        /// </summary>
        public string GetCurrentLanguageCode()
        {
            return LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
        }

        /// <summary>
        /// Lazy property for LocaleManager
        /// </summary>
        //private LocaleManager LocaleManager
        //{
        //    get
        //    {
        //        if (LocaleManager.Instance == null)
        //        {
        //            Debug.LogError("MainMenuUIController: LocaleManager.Instance is NULL!");
        //        }
        //        return LocaleManager.Instance;
        //    }
        //}
    }
}
