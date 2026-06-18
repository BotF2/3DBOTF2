using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace BOTF3D.UI
{
    public class RefreshLocalization : MonoBehaviour
    {
        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            StartCoroutine(RefreshWhenReady());
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale _)
        {
            StartCoroutine(RefreshWhenReady());
        }

        private IEnumerator RefreshWhenReady()
        {
            yield return LocalizationSettings.InitializationOperation;

            foreach (var lse in GetComponentsInChildren<LocalizeStringEvent>(true))
                lse.RefreshString();
        }
    }
}
