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
            SetLanguageByCode("en"); // or whatever you're using
            StartCoroutine(InitializeLocalization());
        }
        /// <summary>
        /// Wait for Unity Localization to initialize
        /// </summary>
        private System.Collections.IEnumerator InitializeLocalization()
        {
            Debug.Log("LocaleManager: Waiting for Localization to initialize...");

            // ✅ Wait for initialization
            yield return LocalizationSettings.InitializationOperation;

            // ✅ Check if initialization succeeded
            if (LocalizationSettings.InitializationOperation.IsDone)
            {
                if (LocalizationSettings.SelectedLocale != null)
                {
                    Debug.Log($"✅ Localization initialized: {LocalizationSettings.SelectedLocale.Identifier.Code}");
                }
                else
                {
                    Debug.LogWarning("⚠️ Localization initialized but no locale selected - using default");
                    TrySetDefaultLocale();
                }
            }
            else
            {
                Debug.LogError("❌ Localization failed to initialize - disabling localized text");
            }
        }
        /// <summary>
        /// Try to set a default locale if none is selected
        /// </summary>
        private void TrySetDefaultLocale()
        {
            if (LocalizationSettings.AvailableLocales.Locales.Count > 0)
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[0];
                Debug.Log($"  Set default locale to: {LocalizationSettings.SelectedLocale.Identifier.Code}");
            }
            else
            {
                Debug.LogError("  ❌ No locales available! Build Addressables for Localization.");
            }
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
        /// Change the current language
        /// </summary>
        public void SetLocale(string localeCode)
        {
            if (LocalizationSettings.SelectedLocale == null)
            {
                Debug.LogError("Cannot change locale - localization not initialized");
                return;
            }

            var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
                Debug.Log($"Changed locale to: {localeCode}");
            }
            else
            {
                Debug.LogWarning($"Locale '{localeCode}' not found");
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
