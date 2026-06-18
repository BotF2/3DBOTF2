# Game Control Overlay Setup Guide

## Overview
The `GameControlOverlay` provides persistent **master volume control** and **pause/resume** functionality across all gameplay scenes.

---

## Unity Inspector Setup

### Step 1: Create the UI GameObject

1. In your **PersistentScene** (or whichever scene loads first), create a new GameObject:
   - Right-click in Hierarchy → **Create Empty**
   - Name it: `GameControlOverlay`

2. Add the `GameControlOverlay` component:
   - Select the GameObject
   - Click **Add Component**
   - Search for `GameControlOverlay` and add it

---

### Step 2: Create the UI Panel

1. **Create a Canvas** (if you don't have one already):
   - Right-click `GameControlOverlay` → **UI** → **Canvas**
   - Set Canvas **Render Mode** to **Screen Space - Overlay**
   - Set Canvas Scaler **UI Scale Mode** to **Scale With Screen Size**

2. **Create the Overlay Panel**:
   - Right-click Canvas → **UI** → **Panel**
   - Name it: `OverlayPanel`
   - Position it in the **top-right corner** of the screen
   - Resize to about **300 x 150** pixels

3. **Style the Panel** (optional):
   - Set Panel **Image Color** to semi-transparent (e.g., `R:0, G:0, B:0, A:180`)

---

### Step 3: Create Volume Slider

1. **Add Slider**:
   - Right-click `OverlayPanel` → **UI** → **Slider**
   - Name it: `MasterVolumeSlider`
   - Position at top of panel

2. **Configure Slider**:
   - Min Value: `0`
   - Max Value: `1`
   - Whole Numbers: `OFF` (unchecked)
   - Value: `1` (default 100%)

3. **Add Volume Label** (optional):
   - Right-click `OverlayPanel` → **UI** → **Text**
   - Name it: `VolumeLabel`
   - Text: `"Master Volume:"`
   - Position above slider

4. **Add Volume Value Text**:
   - Right-click `OverlayPanel` → **UI** → **Text**
   - Name it: `VolumeValueText`
   - Text: `"100%"`
   - Position to the right of slider

---

### Step 4: Create Pause Button

1. **Add Button**:
   - Right-click `OverlayPanel` → **UI** → **Button**
   - Name it: `PauseButton`
   - Position below volume slider

2. **Configure Button Text**:
   - Select `PauseButton/Text` child
   - Set Text to: `"Pause"`

---

### Step 5: Link Components in Inspector

Select the `GameControlOverlay` GameObject and assign:

**UI References:**
- **Overlay Panel**: Drag `OverlayPanel` here
- **Master Volume Slider**: Drag `MasterVolumeSlider` here
- **Volume Value Text**: Drag `VolumeValueText` here
- **Pause Button**: Drag `PauseButton` here
- **Pause Button Text**: Drag `PauseButton/Text` here

**Settings:**
- **Start Visible**: ✅ (checked) - Overlay starts visible
- **Show In Main Menu**: ❌ (unchecked) - Hide overlay in main menu

---

## Features

### 1. Master Volume Control
- **Slider Range**: 0% to 100%
- **Real-time Updates**: Volume changes immediately
- **Persistent**: Saves to PlayerPrefs automatically
- **Synchronized**: All audio sources (music, SFX, UI) respect master volume

### 2. Pause/Resume
- **Pause**: Stops game time and TimeManager progression
- **Resume**: Restarts time at previous speed
- **Visual Feedback**: Button text changes between "Pause" and "Resume"
- **Auto-Unpause**: Automatically unpauses when returning to main menu

### 3. Keyboard Shortcuts
- **P Key**: Toggle pause/resume
- **M Key**: Toggle overlay visibility (show/hide)

### 4. Scene Management
- **Persistent**: Uses `DontDestroyOnLoad()` - survives scene changes
- **Auto-Hide**: Hides in MainMenu/Lobby scenes (unless `showInMainMenu = true`)
- **Auto-Show**: Shows when entering gameplay scenes

---

## Advanced Customization

### Using TextMeshPro Instead of Legacy Text

If you prefer TMP_Text, manually modify the prefab:
1. Replace `Text` components with `TextMeshProUGUI` components
2. The script uses legacy `UnityEngine.UI.Text` to avoid assembly issues
3. You can change the serialized field types if needed

### Custom Styling

**Panel Background:**
```csharp
// In Inspector: OverlayPanel → Image → Color
Color: R:0.2, G:0.2, B:0.3, A:0.8
```

**Button Colors:**
```csharp
// In Inspector: PauseButton → Button → Colors
Normal: White
Highlighted: Light Blue
Pressed: Dark Blue
```

### Adding More Controls

You can extend the overlay to include:
- Music volume slider (separate from master)
- SFX volume slider
- Time speed controls (1x, 2x, 4x, etc.)
- Quick save/load buttons

Example:
```csharp
[SerializeField] private Slider musicVolumeSlider;

private void InitializeMusicSlider()
{
    if (musicVolumeSlider != null)
    {
        musicVolumeSlider.value = GetMusicVolume();
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
    }
}
```

---

## Integration with Existing Systems

### AudioManager Integration
- Automatically finds `BOTF3D.Audio.AudioManager` using reflection
- Calls `SetMasterVolume(float)` when slider changes
- Calls `GetMasterVolume()` to initialize slider value

### TimeManager Integration
- Automatically finds `BOTF3D.Core.TimeManager` using reflection
- Calls `PauseTime()` when pausing
- Calls `ResumeTime()` when resuming
- Also sets `Time.timeScale = 0` for Unity physics pause

### No Code Changes Required
- Uses reflection to avoid assembly reference issues
- Works with existing AudioManager and TimeManager
- No modifications needed to other scripts

---

## Troubleshooting

### "AudioManager not found"
- Ensure `AudioManager` exists in the scene
- Check that AudioManager is initialized before GameControlOverlay
- Verify AudioManager has `Instance` singleton property

### "TimeManager not found"
- Ensure `TimeManager` exists in the scene
- Check that TimeManager is initialized before GameControlOverlay
- Verify TimeManager has `Instance` singleton property

### Overlay not appearing
- Check `Start Visible` is enabled in Inspector
- Verify `Overlay Panel` is assigned in Inspector
- Check if current scene is MainMenu (overlay auto-hides there)

### Pause not working
- Check Console for warning messages
- Verify TimeManager has `PauseTime()` and `ResumeTime()` methods
- Ensure TimeManager's `timeRunning` bool is being used correctly

### Volume not changing
- Check Console for warning messages
- Verify AudioManager has `SetMasterVolume(float)` method
- Check that audio sources are configured to respect volume

---

## API Reference

### Public Methods

```csharp
// Toggle pause state
GameControlOverlay.Instance.TogglePause();

// Force unpause (useful for scene transitions)
GameControlOverlay.Instance.ForceUnpause();

// Check if game is paused
bool isPaused = GameControlOverlay.Instance.IsPaused();

// Show/hide overlay
GameControlOverlay.Instance.ShowOverlay();
GameControlOverlay.Instance.HideOverlay();
GameControlOverlay.Instance.ToggleOverlayVisibility();
```

### Usage Example

```csharp
// In your scene transition code:
void OnReturnToMainMenu()
{
    if (GameControlOverlay.Instance != null)
    {
        GameControlOverlay.Instance.ForceUnpause();
        GameControlOverlay.Instance.HideOverlay();
    }
}
```

---

## Best Practices

1. **Place in PersistentScene**: Create the overlay in your persistent scene so it's always available
2. **Single Instance**: Only create one GameControlOverlay GameObject
3. **UI Layer**: Put overlay UI on the highest sort order to ensure it's always on top
4. **Keyboard Shortcuts**: Keep default shortcuts (P for pause, M for menu) or customize as needed
5. **Scene Management**: Let the script auto-hide/show based on scene - don't manually control visibility

---

## Future Enhancements

Consider adding:
- **Settings menu integration**: Link to full settings panel
- **Quick settings**: Add graphics quality, resolution toggles
- **Audio mixer control**: Separate music/SFX/UI volume
- **Save/Load buttons**: Quick access to save system
- **Time speed controls**: 0.5x, 1x, 2x, 4x speed buttons
- **Notifications**: Display when game is paused with full-screen overlay
