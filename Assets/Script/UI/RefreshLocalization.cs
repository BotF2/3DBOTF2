using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    public class RefreshLocalization : MonoBehaviour
    {
        private void OnEnable()
        {
            Debug.Log($"[RefreshLocalization] Panel activated: {gameObject.name}");

            // Subscribe to locale changes
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

            // Refresh immediately
            StartCoroutine(RefreshWhenReady());
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale newLocale)
        {
            Debug.Log($"[RefreshLocalization] {gameObject.name} - Language changed to: {newLocale.Identifier.Code}");
            StartCoroutine(RefreshWhenReady());
        }

        private IEnumerator RefreshWhenReady()
        {
            yield return LocalizationSettings.InitializationOperation;

            var localizers = GetComponentsInChildren<LocalizeStringEvent>(true);
            Debug.Log($"[RefreshLocalization] {gameObject.name} - Found {localizers.Length} localizers on panel.");
        }
    }
}