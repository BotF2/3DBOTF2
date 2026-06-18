using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace BOTF3D.UI
{
    /// <summary>
    /// Bridges a LocalizeStringEvent to the TMP_Text on the same GameObject.
    /// Add this alongside LocalizeStringEvent so locale changes automatically update the text.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(LocalizeStringEvent))]
    public class LocalizeTMP : MonoBehaviour
    {
        private void Awake()
        {
            var lse = GetComponent<LocalizeStringEvent>();
            var tmp = GetComponent<TextMeshProUGUI>();
            lse.OnUpdateString.AddListener(tmp.SetText);
        }
    }
}
