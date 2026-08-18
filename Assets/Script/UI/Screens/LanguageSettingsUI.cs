// Ignore Spelling: BOTF

using BOTF3D.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace BOTF3D.UI
{
    /// <summary>
    /// UI controller for language selection. Wire this to your settings menu.
    /// </summary>
    public class LanguageSettingsUI : MonoBehaviour
    {
        [Header("Language")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        private List<Locale> availableLocales;

        private void Start()
        {
            StartCoroutine(SetupLanguageDropdown());
        }

        private IEnumerator SetupLanguageDropdown()
        {
            if (languageDropdown == null) yield break;

            LocaleManager locale = ServiceLocator.Get<LocaleManager>();
            if (locale == null)
            {
                Debug.LogError("LanguageSettingsUI: LocaleManager not found!");
                yield break;
            }

            // Locales aren't available until the Localization system finishes initializing
            yield return LocalizationSettings.InitializationOperation;

            availableLocales = locale.GetAvailableLocales();
            if (availableLocales.Count == 0)
            {
                Debug.LogError("LanguageSettingsUI: No available locales found!");
                yield break;
            }

            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(availableLocales.Select(l => l.Identifier.CultureInfo?.NativeName ?? l.LocaleName).ToList());

            string currentCode = locale.GetCurrentLanguageCode();
            int currentIndex = availableLocales.FindIndex(l => l.Identifier.Code == currentCode);
            languageDropdown.SetValueWithoutNotify(Mathf.Max(currentIndex, 0));

            languageDropdown.onValueChanged.AddListener(index => locale.ChangeLanguage(availableLocales[index]));
        }
    }
}
