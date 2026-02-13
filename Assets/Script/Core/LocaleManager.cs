using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
/// <summary>
/// This is for language used, English, German, French....
/// </summary>


namespace Assets.Core
{
    public class LocaleManager : MonoBehaviour
    {

        public void ChangeLanguage(Locale newLocale)
        {
            if (Application.isPlaying)
                LocalizationSettings.SelectedLocale = newLocale;
        }
        /// 
        /// I do not know why the code below does not work but the above does work.
        /// 
        //private bool active = false; // press button only once
        //public void ChangeLocale(int localeID)
        //{
        //    if (active == true)
        //        return;
        //    StartCoroutine((IEnumerator)SetLocale(localeID));
        //}
        //IEnumerable SetLocale(int _locale)
        //{
        //    active = true;
        //    yield return LocalizationSettings.InitializationOperation;
        //    LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index: _locale];
        //    active = false;

        //}

    }
}
