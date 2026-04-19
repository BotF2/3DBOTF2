# GameControlOverlay - Troubleshooting Guide

## Common Issue: Panel/Controls Become Unchecked at Runtime

### Problem Description
- OverlayPanel is checked in Inspector but unchecks at runtime
- StardateText is checked but becomes unchecked and won't activate
- PauseButton turns unchecked and won't come back
- Volume slider works when panel is manually activated

### Root Cause
The `UpdateOverlayVisibility()` method controls visibility based on the current scene. It's working correctly but might be hiding elements you want to see during testing.

---

## Solution 1: Test in the Correct Scene

The overlay is designed to show/hide based on scene:

| Scene | OverlayPanel | PauseButton | StardateText | Why |
|-------|--------------|-------------|--------------|-----|
| **GalaxyScene** | ✅ Visible | ✅ Visible | ✅ Visible | Main gameplay |
| **MainMenu/Lobby** | ❌ Hidden* | ❌ Hidden | ❌ Hidden | No gameplay |
| **CombatScene** | ❌ Hidden* | ❌ Hidden | ❌ Hidden | Time pauses automatically |
| **PersistentScene** | ✅ Visible | ❌ Hidden | ❌ Hidden | Overlay exists but gameplay controls hidden |
| **Other Scenes** | ✅ Visible | ❌ Hidden | ❌ Hidden | Default behavior |

*Can be overridden with Inspector settings

### To Test Properly:
1. **In GalaxyScene**: All controls should be visible
2. **In PersistentScene**: Volume slider visible, pause/stardate hidden
3. **In MainMenu**: Everything hidden (unless you enable `showInMainMenu`)

---

## Solution 2: Override Scene-Based Hiding

If you want the overlay to show in MainMenu or CombatScene:

### In Unity Inspector:
1. Select `GameControlOverlay` GameObject
2. Find **Settings** section
3. Enable these checkboxes:
   - ✅ **Show In Main Menu**: Shows overlay in MainMenu/Lobby
   - ✅ **Show In Combat Scene**: Shows overlay in CombatScene

### Result:
- Overlay will remain visible in those scenes
- PauseButton and StardateText still only show in GalaxyScene (by design)

---

## Solution 3: Fix Hierarchy Structure

The overlay **must have correct parent-child relationships** or controls won't work:

### Correct Hierarchy:
```
GameControlOverlay (GameObject with GameControlOverlay component)
└─ Canvas
    └─ OverlayPanel (This is what gets shown/hidden)
        ├─ StardateLabel (Text: "Stardate:")
        ├─ StardateText (TextMeshProUGUI - ASSIGN TO INSPECTOR)
        ├─ VolumeLabel (Text: "Master Volume:")
        ├─ MasterVolumeSlider (Slider - ASSIGN TO INSPECTOR)
        ├─ VolumeValueText (TextMeshProUGUI - ASSIGN TO INSPECTOR)
        └─ PauseButton (Button - ASSIGN TO INSPECTOR)
            └─ PauseButtonTextTMP (TextMeshProUGUI - ASSIGN TO INSPECTOR)
```

### Key Requirements:
1. **OverlayPanel** must be a child of Canvas
2. **StardateText**, **PauseButton**, **MasterVolumeSlider** must be children of OverlayPanel (or nested deeper)
3. All components must be assigned in the Inspector

### Why This Matters:
- When `overlayPanel.SetActive(false)` is called, ALL children are hidden
- If controls are NOT children of OverlayPanel, they won't be controlled properly

---

## Solution 4: Check Console for Debug Logs

The script outputs detailed logs to help you debug:

### Expected Logs in GalaxyScene:
```
✅ GameControlOverlay: Instance created and set to DontDestroyOnLoad
✅ GameControlOverlay: AudioManager cached
✅ GameControlOverlay: TimeManager cached
GameControlOverlay: Volume slider initialized to 1.00
GameControlOverlay: Pause button initialized
GameControlOverlay: UI initialized, waiting for scene-based visibility update
GameControlOverlay: OverlayPanel set to True
GameControlOverlay: PauseButton set to True
GameControlOverlay: StardateText set to True
GameControlOverlay visibility: Scene=GalaxyScene, MainMenu=False, Combat=False, Galaxy=True, Persistent=False, ShowOverlay=True, ShowGameplayControls=True
```

### Expected Logs in MainMenu:
```
GameControlOverlay: OverlayPanel set to False
GameControlOverlay: PauseButton set to False
GameControlOverlay: StardateText set to False
GameControlOverlay visibility: Scene=MainMenuScene, MainMenu=True, Combat=False, Galaxy=False, Persistent=False, ShowOverlay=False, ShowGameplayControls=False
```

### What to Look For:
- Check if `ShowOverlay=True` in the scene you're testing
- Check if `ShowGameplayControls=True` for pause button and stardate
- If values are wrong, check your scene name or Inspector settings

---

## Solution 5: Manual Override for Testing

If you just want to test the overlay regardless of scene:

### Temporary Code Change:
Open `GameControlOverlay.cs` and find `UpdateOverlayVisibility()` method (around line 340).

**Comment out** the entire method and replace with:
```csharp
private void UpdateOverlayVisibility()
{
    // TEMPORARY: Force show everything for testing
    if (overlayPanel != null)
        overlayPanel.SetActive(true);
    if (pauseButton != null)
        pauseButton.gameObject.SetActive(true);
    if (stardateText != null)
        stardateText.gameObject.SetActive(true);

    Debug.Log("GameControlOverlay: TESTING MODE - forcing all visible");
}
```

**WARNING**: This is for testing only! Remove this after confirming controls work.

---

## Solution 6: Verify Inspector Assignments

### Check These Fields Are Assigned:

Select `GameControlOverlay` GameObject in Hierarchy:

**UI References (REQUIRED):**
- ✅ **Overlay Panel**: Must point to Panel GameObject
- ✅ **Master Volume Slider**: Must point to Slider component
- ✅ **Volume Value Text**: Must point to TextMeshProUGUI (showing "100%")
- ✅ **Pause Button**: Must point to Button component
- ✅ **Stardate Text**: Must point to TextMeshProUGUI (showing stardate)

**Pause Button Display (OPTIONAL - Choose ONE):**
- **Pause Button Text TMP**: If using text, assign TextMeshProUGUI
- **Pause Button Image**: If using icons, assign Image component
- **Pause Icon**: If using icons, assign pause sprite
- **Play Icon**: If using icons, assign play sprite

**Settings:**
- ✅ **Start Visible**: Usually checked
- ❌ **Show In Main Menu**: Uncheck unless you want overlay in menu
- ❌ **Show In Combat Scene**: Uncheck unless you want overlay in combat
- ☑️ **Use Icons Instead Of Text**: Check only if using pause/play icons

### Missing Assignment Symptoms:
- **OverlayPanel not assigned**: Nothing shows at all
- **StardateText not assigned**: Stardate doesn't appear
- **PauseButton not assigned**: Can't pause
- **VolumeSlider not assigned**: Volume doesn't change

---

## Solution 7: Ensure Managers Exist

The overlay needs these managers to function:

### Required Managers:
1. **AudioManager** (BOTF3D.Audio) - For volume control
2. **TimeManager** (BOTF3D.Core) - For pause and stardate

### Check Console for Warnings:
```
✅ GameControlOverlay: AudioManager cached
✅ GameControlOverlay: TimeManager cached
```

If you see:
```
⚠️ GameControlOverlay: AudioManager not found
⚠️ GameControlOverlay: TimeManager not found
```

**Solution**: Make sure AudioManager and TimeManager GameObjects exist in your scene and are initialized before GameControlOverlay.

---

## Quick Checklist

Before testing, verify:

- [ ] GameControlOverlay GameObject exists in scene
- [ ] OverlayPanel is assigned in Inspector
- [ ] All UI components (slider, texts, button) are assigned
- [ ] Hierarchy structure is correct (Panel → Controls)
- [ ] Testing in **GalaxyScene** (for full functionality)
- [ ] AudioManager exists in scene
- [ ] TimeManager exists in scene
- [ ] Console shows "AudioManager cached" and "TimeManager cached"
- [ ] Scene name contains "Galaxy" for gameplay controls
- [ ] Console shows "ShowOverlay=True" and "ShowGameplayControls=True"

---

## Still Not Working?

### Step-by-Step Debug:

1. **Start in GalaxyScene** (not PersistentScene or MainMenu)
2. **Play the game**
3. **Check Console** for "GameControlOverlay visibility" log
4. **Look for**: `ShowOverlay=True` and `ShowGameplayControls=True`
5. If `False`, check:
   - Is scene name correct? (must contain "Galaxy")
   - Are Inspector settings correct?
6. **Manually activate** OverlayPanel in Hierarchy (while playing)
7. **Does volume slider work?**
   - YES → Managers are working, visibility logic is the issue
   - NO → Check manager assignments
8. **Check Hierarchy** while playing:
   - Is OverlayPanel checked?
   - Is PauseButton checked?
   - Is StardateText checked?
9. If they're unchecked but should be checked:
   - Scene name might not contain "Galaxy"
   - Inspector settings might be wrong

---

## Expected Behavior Summary

### In GalaxyScene (Main Gameplay):
```
OverlayPanel: ✅ Active
MasterVolumeSlider: ✅ Active (inside panel)
VolumeValueText: ✅ Active (shows "100%")
PauseButton: ✅ Active
PauseButtonText: ✅ Active (shows "Pause" or "Resume")
StardateText: ✅ Active (shows "Stardate: 12345")

Keyboard:
P key → Toggles pause
M key → Hides/shows overlay
```

### In MainMenu/Lobby:
```
OverlayPanel: ❌ Inactive (unless showInMainMenu = true)
PauseButton: ❌ Inactive
StardateText: ❌ Inactive
```

### In CombatScene:
```
OverlayPanel: ❌ Inactive (unless showInCombatScene = true)
PauseButton: ❌ Inactive (time pauses automatically)
StardateText: ❌ Inactive
```

### In PersistentScene (or Unknown Scene):
```
OverlayPanel: ✅ Active (volume control still available)
PauseButton: ❌ Inactive (only shows in Galaxy)
StardateText: ❌ Inactive (only shows in Galaxy)
```

---

## Final Note

The overlay is **working as designed** - it intelligently shows/hides based on the scene. If you're testing in PersistentScene or MainMenu, it's supposed to hide the gameplay controls (pause button and stardate). Test in **GalaxyScene** to see everything working together! 🌟
