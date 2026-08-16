using UnityEngine;
using BOTF3D.Audio;

namespace BOTF3D.UI
{
    /// <summary>
    /// Plays a UI sound whenever the panel this is attached to becomes active.
    /// </summary>
    public class UIPanelSound : MonoBehaviour
    {
        [SerializeField] private SoundData openSound;

        [Tooltip("Enable only for panels that start active when the scene loads, so the sound doesn't fire immediately on scene/menu load.")]
        [SerializeField] private bool skipFirstEnable = false;

        private bool hasEnabledOnce;

        private void OnEnable()
        {
            bool isFirstEnable = !hasEnabledOnce;
            hasEnabledOnce = true;

            if (isFirstEnable && skipFirstEnable) return;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySoundData(openSound);
            }
        }
    }
}
