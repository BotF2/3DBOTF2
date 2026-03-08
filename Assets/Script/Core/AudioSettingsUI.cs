using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.Core
{
    /// <summary>
    /// UI controller for audio settings sliders
    /// Wire this to your settings menu
    /// </summary>
    public class AudioSettingsUI : MonoBehaviour
    {
        [Header("Volume Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider uiVolumeSlider;

        [Header("Optional: Test Buttons")]
        [SerializeField] private Button testMusicButton;
        [SerializeField] private Button testSFXButton;
        [SerializeField] private Button testUIButton;

        private void Start()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("AudioSettingsUI: AudioManager not found!");
                return;
            }

            // Load current values into sliders
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (uiVolumeSlider != null)
            {
                uiVolumeSlider.value = AudioManager.Instance.GetUIVolume();
                uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
            }

            // Wire test buttons
            if (testMusicButton != null)
                testMusicButton.onClick.AddListener(OnTestMusic);

            if (testSFXButton != null)
                testSFXButton.onClick.AddListener(OnTestSFX);

            if (testUIButton != null)
                testUIButton.onClick.AddListener(OnTestUI);
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }

        private void OnUIVolumeChanged(float value)
        {
            AudioManager.Instance.SetUIVolume(value);
        }

        private void OnTestMusic()
        {
            AudioManager.Instance.PlayMusic("MainTheme");
        }

        private void OnTestSFX()
        {
            AudioManager.Instance.PlayRandomSFX("Explosion");
        }

        private void OnTestUI()
        {
            AudioManager.Instance.PlayUI("ButtonClick");
        }
    }
}