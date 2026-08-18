// Ignore Spelling: BOTF

using BOTF3D.Core;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// UI controller for graphics settings (quality, resolution, fullscreen, vsync, frame rate, anti-aliasing)
    /// Wire this to your settings menu
    /// </summary>
    public class GraphicsSettingsUI : MonoBehaviour
    {
        [Header("Quality")]
        [SerializeField] private TMP_Dropdown qualityDropdown;

        [Header("Resolution")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Performance")]
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private TMP_Dropdown frameRateDropdown;
        [SerializeField] private TMP_Dropdown antiAliasingDropdown;

        private const int MaxResolutionOptions = 4;

        private readonly int[] frameRateOptions = { 30, 60, 120, 144, -1 }; // -1 = unlimited
        private readonly int[] antiAliasingOptions = { 0, 2, 4, 8 };
        private List<Resolution> availableResolutions;

        private void Start()
        {
            GraphicsQualityManager graphics = ServiceLocator.Get<GraphicsQualityManager>();
            if (graphics == null)
            {
                Debug.LogError("GraphicsSettingsUI: GraphicsQualityManager not found!");
                return;
            }

            SetupQualityDropdown(graphics);
            SetupResolutionDropdown(graphics);
            SetupFullscreenToggle(graphics);
            SetupVSyncToggle(graphics);
            SetupFrameRateDropdown(graphics);
            SetupAntiAliasingDropdown(graphics);
        }

        private void SetupQualityDropdown(GraphicsQualityManager graphics)
        {
            if (qualityDropdown == null) return;

            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(QualitySettings.names.ToList());
            qualityDropdown.SetValueWithoutNotify(graphics.GetQualityLevel());
            qualityDropdown.onValueChanged.AddListener(graphics.SetQualityLevel);
        }

        private void SetupResolutionDropdown(GraphicsQualityManager graphics)
        {
            if (resolutionDropdown == null) return;

            availableResolutions = Screen.resolutions
                .Select(r => new Resolution { width = r.width, height = r.height })
                .Distinct()
                .OrderByDescending(r => r.width * r.height)
                .Take(MaxResolutionOptions)
                .ToList();

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(availableResolutions.Select(r => $"{r.width} x {r.height}").ToList());

            int currentIndex = availableResolutions.FindIndex(r =>
                r.width == graphics.GetResolutionWidth() && r.height == graphics.GetResolutionHeight());
            resolutionDropdown.SetValueWithoutNotify(Mathf.Max(currentIndex, 0));

            resolutionDropdown.onValueChanged.AddListener(index =>
            {
                Resolution res = availableResolutions[index];
                graphics.SetResolution(res.width, res.height, graphics.GetFullscreen());
            });
        }

        private void SetupFullscreenToggle(GraphicsQualityManager graphics)
        {
            if (fullscreenToggle == null) return;

            fullscreenToggle.SetIsOnWithoutNotify(graphics.GetFullscreen());
            fullscreenToggle.onValueChanged.AddListener(graphics.SetFullscreen);
        }

        private void SetupVSyncToggle(GraphicsQualityManager graphics)
        {
            if (vsyncToggle == null) return;

            vsyncToggle.SetIsOnWithoutNotify(graphics.GetVSyncCount() > 0);
            vsyncToggle.onValueChanged.AddListener(isOn => graphics.SetVSyncCount(isOn ? 1 : 0));
        }

        private void SetupFrameRateDropdown(GraphicsQualityManager graphics)
        {
            if (frameRateDropdown == null) return;

            frameRateDropdown.ClearOptions();
            frameRateDropdown.AddOptions(frameRateOptions.Select(fps => fps == -1 ? "Unlimited" : $"{fps} FPS").ToList());

            int currentIndex = System.Array.IndexOf(frameRateOptions, graphics.GetTargetFrameRate());
            frameRateDropdown.SetValueWithoutNotify(Mathf.Max(currentIndex, 0));

            frameRateDropdown.onValueChanged.AddListener(index => graphics.SetTargetFrameRate(frameRateOptions[index]));
        }

        private void SetupAntiAliasingDropdown(GraphicsQualityManager graphics)
        {
            if (antiAliasingDropdown == null) return;

            antiAliasingDropdown.ClearOptions();
            antiAliasingDropdown.AddOptions(antiAliasingOptions.Select(samples => samples == 0 ? "Off" : $"{samples}x MSAA").ToList());

            int currentIndex = System.Array.IndexOf(antiAliasingOptions, graphics.GetAntiAliasing());
            antiAliasingDropdown.SetValueWithoutNotify(Mathf.Max(currentIndex, 0));

            antiAliasingDropdown.onValueChanged.AddListener(index => graphics.SetAntiAliasing(antiAliasingOptions[index]));
        }
    }
}
