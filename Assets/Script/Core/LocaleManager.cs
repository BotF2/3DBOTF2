using System.Collections;
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
    public class LocaleManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        public static LocaleManager Instance;

        [Header("Current Language")]
        [SerializeField] private string currentLanguageCode = "en";

        [Header("Localization")]
        [SerializeField] private Button buttonEnglish;
        [SerializeField] private Button buttonFrench;
        [SerializeField] private Button buttonGerman;
        [SerializeField] private Button buttonSpanish;
        [SerializeField] private Button buttonItalian;
        [SerializeField] private Button buttonPolish;
        [SerializeField] private Button buttonPortuguese;

        private void Awake()
        {
            ServiceLocator.Register<LocaleManager>(this);
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("✅ LocaleManager initialized");
            }
            else
            {
                Destroy(gameObject);
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
        /// 
        private IEnumerator InitializeLocalization()
        {
            Debug.Log("LocaleManager: Waiting for Localization system to initialize...");

            // ✅ Wait for localization system to be ready
            yield return LocalizationSettings.InitializationOperation;

            Debug.Log("✅ Localization system initialized");

            // ✅ Force a locale refresh to ensure all UI updates
            RefreshAllLocalizedStrings();
        }
        /// <summary>
        /// Force all LocalizeStringEvent components to refresh
        /// </summary>
        /// <summary>
        /// Force all LocalizeStringEvent components to refresh
        /// </summary>
        public void RefreshAllLocalizedStrings()
        {
            Debug.Log("RefreshAllLocalizedStrings: Forcing update on all active UI...");

            // Find all active LocalizeStringEvent components
            var localizedStrings = FindObjectsByType<UnityEngine.Localization.Components.LocalizeStringEvent>();

            Debug.Log($"  Found {localizedStrings.Length} LocalizeStringEvent components");

            foreach (var localizedString in localizedStrings)
            {
                if (localizedString != null && localizedString.StringReference != null && !localizedString.StringReference.IsEmpty)
                {
                    localizedString.RefreshString();
                }
            }

            Debug.Log("RefreshAllLocalizedStrings: Complete");
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
    

        private void OnDestroy()
        {
            ServiceLocator.Unregister<LocaleManager>(); }
    }
}