# GameControlOverlay - Quick Start

## What It Does
✅ **Persistent master volume slider** - Controls all game audio  
✅ **Pause/Resume button** - Stops and resumes game time (GalaxyScene only)  
✅ **Stardate display** - Shows current stardate from TimeManager  
✅ **Keyboard shortcuts** - P to pause (GalaxyScene only), M to toggle overlay  
✅ **Auto scene management** - Shows in GalaxyScene, hides in MainMenu/Combat  
✅ **Persistent across scenes** - Uses DontDestroyOnLoad  

---

## Scene-Specific Behavior

| Scene | Overlay Visible | Pause Button | Stardate Display |
|-------|----------------|--------------|------------------|
| **GalaxyScene** | ✅ Yes | ✅ Yes | ✅ Yes |
| **MainMenu/Lobby** | ❌ No | ❌ No | ❌ No |
| **CombatScene** | ❌ No | ❌ No | ❌ No |

**Why?**
- **GalaxyScene**: Main gameplay - needs volume, pause, and stardate
- **MainMenu**: No gameplay - only volume control needed (can enable with setting)
- **CombatScene**: Time pauses automatically - no pause button needed

---

## 5-Minute Setup

### 1. Create GameObject
```
Hierarchy → Create Empty → Name: "GameControlOverlay"
Add Component → GameControlOverlay
```

### 2. Create UI (under GameControlOverlay)
```
UI → Canvas (Screen Space Overlay)
  └─ Panel (name: OverlayPanel) - Position top-right, 350x150px
      ├─ Text: "Stardate:" (TextMeshProUGUI)
      ├─ Text (name: StardateText) - Text:"12345" (TextMeshProUGUI)
      ├─ Text: "Master Volume:"
      ├─ Slider (name: MasterVolumeSlider) - Min:0, Max:1
      ├─ Text (name: VolumeValueText) - Text:"100%" (TextMeshProUGUI)
      └─ Button (name: PauseButton)
          └─ Text (name: PauseButtonTextTMP) - Text:"Pause" (TextMeshProUGUI)
```

### 3. Assign in Inspector
Select `GameControlOverlay` GameObject:
- **Overlay Panel**: Drag `OverlayPanel`
- **Master Volume Slider**: Drag `MasterVolumeSlider`
- **Volume Value Text**: Drag `VolumeValueText` (TMP)
- **Pause Button**: Drag `PauseButton`
- **Stardate Text**: Drag `StardateText` (TMP)
- **Pause Button Text TMP**: Drag `PauseButton/Text` (TMP)
- **Start Visible**: ✅ Check
- **Show In Main Menu**: ❌ Uncheck
- **Show In Combat Scene**: ❌ Uncheck
- **Use Icons Instead Of Text**: ❌ Uncheck (or ✅ if using icons)

### 4. Done!
Play the game:
- **In GalaxyScene**: See stardate, volume slider, pause button
- **In MainMenu**: Overlay hidden (volume control available if enabled)
- **In CombatScene**: Overlay hidden (time pauses automatically)
- Press **P** to pause (GalaxyScene only)
- Press **M** to hide/show overlay

---

## How It Works

### Master Volume
```
Slider → GameControlOverlay → AudioManager.SetMasterVolume()
Volume saved to PlayerPrefs automatically
All audio (music, SFX, UI) respects master volume
```

### Pause System
```
Button → GameControlOverlay → TimeManager.PauseTime()
Also sets Time.timeScale = 0 (pauses physics/animations)
Button text changes: "Pause" ↔ "Resume"
```

### Persistence
```
DontDestroyOnLoad() - Survives scene changes
Auto-hides in MainMenu/Lobby scenes
Auto-shows in gameplay scenes (Galaxy, Combat)
```

---

## Code Integration

### From Other Scripts
```csharp
// Pause the game
GameControlOverlay.Instance.TogglePause();

// Force unpause (useful for scene transitions)
GameControlOverlay.Instance.ForceUnpause();

// Check pause state
if (GameControlOverlay.Instance.IsPaused())
{
    // Game is paused
}

// Hide overlay during cutscene
GameControlOverlay.Instance.HideOverlay();
```

### Keyboard Shortcuts
- **P Key**: Toggle pause/resume
- **M Key**: Toggle overlay visibility

---

## Customization

### Change Position
```
Select OverlayPanel → Rect Transform
Adjust Anchor Presets (top-left, top-right, etc.)
Adjust Width/Height
```

### Change Colors
```
OverlayPanel → Image → Color (background)
PauseButton → Button → Colors (button states)
```

### Add More Controls
```csharp
// In GameControlOverlay.cs, add new slider:
[SerializeField] private Slider musicVolumeSlider;

private void InitializeMusicSlider()
{
    musicVolumeSlider.value = GetMusicVolume();
    musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
}
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Overlay not visible | Check `Start Visible` is enabled, verify Panel is assigned |
| Volume not changing | Ensure AudioManager exists and is initialized |
| Pause not working | Ensure TimeManager exists and is initialized |
| Overlay appears in MainMenu | Set `Show In Main Menu` to false |
| Can't find managers | Check Console - script uses reflection to find them |

---

## Technical Details

### Dependencies
- **AudioManager** (BOTF3D.Audio) - For volume control
- **TimeManager** (BOTF3D.Core) - For pause/resume
- **Unity UI** (UnityEngine.UI) - For sliders and buttons

### Reflection Usage
Uses reflection to avoid assembly reference issues:
```csharp
// Finds managers at runtime without compile-time dependencies
System.Type.GetType("BOTF3D.Audio.AudioManager, Assembly-CSharp")
System.Type.GetType("BOTF3D.Core.TimeManager, Assembly-CSharp")
```

### Performance
- Managers cached on Awake (no repeated lookups)
- Minimal Update() overhead (only checks input keys)
- No per-frame reflection calls

---

## Files Created
```
Assets/Script/UI/GameControlOverlay.cs           (Main script)
Assets/Docs/GameControlOverlay_Setup_Guide.md    (Full guide)
Assets/Docs/GameControlOverlay_QuickStart.md     (This file)
```

---

## Next Steps

1. ✅ **Create the UI** following steps above
2. ✅ **Test volume slider** - should change audio immediately
3. ✅ **Test pause button** - should stop game time
4. ✅ **Test keyboard shortcuts** - P and M keys
5. ✅ **Test scene transitions** - overlay should persist
6. ⭐ **Customize appearance** to match your game's style
7. ⭐ **Add more controls** (music volume, time speed, etc.)

---

## Support

See full documentation: `Assets/Docs/GameControlOverlay_Setup_Guide.md`

Questions or issues? Check the troubleshooting section or Console for warning messages.
