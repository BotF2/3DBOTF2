# GameControlOverlay - TextMeshPro & Icon Setup Guide

## Overview
The `GameControlOverlay` now supports:
- ✅ **TextMeshProUGUI** for volume percentage display
- ✅ **Pause/Play Icons** instead of text buttons
- ✅ Flexible configuration - use text OR icons

---

## Option 1: Using TextMeshProUGUI (Recommended)

### Setup Volume Text with TMP

1. **Create Volume Value Text**:
   - Right-click `OverlayPanel` → **UI** → **Text - TextMeshPro**
   - Name it: `VolumeValueTextTMP`
   - Set Text to: `"100%"`
   - Position to the right of volume slider

2. **Assign in Inspector**:
   - Select `GameControlOverlay` GameObject
   - **Volume Value Text**: Drag `VolumeValueTextTMP` here
   - Leave legacy Text field empty

### Setup Pause Button with TMP Text

1. **Create/Modify Button Text**:
   - Select `PauseButton/Text` (delete it if it's legacy Text)
   - Right-click `PauseButton` → **UI** → **Text - TextMeshPro**
   - Name it: `Text` (or `PauseButtonTextTMP`)
   - Set Text to: `"Pause"`

2. **Assign in Inspector**:
   - Select `GameControlOverlay` GameObject
   - **Pause Button Text TMP**: Drag the TextMeshProUGUI component
   - Leave legacy `Pause Button Text` field empty
   - **Use Icons Instead Of Text**: ❌ Uncheck

---

## Option 2: Using Pause/Play Icons (Recommended for Modern UI)

### Step 1: Prepare Icons

You need two sprites:
- **Pause Icon**: ⏸️ (shows when game is running - click to pause)
- **Play Icon**: ▶️ (shows when game is paused - click to resume)

Common icon sizes: 32x32, 64x64, or 128x128 pixels

### Step 2: Import Icons to Unity

1. Place icons in: `Assets/Art/UI/Icons/`
2. Select each icon in Project window
3. Set **Texture Type** to: `Sprite (2D and UI)`
4. Click **Apply**

### Step 3: Setup Button with Image

1. **Remove/Hide Button Text**:
   - Select `PauseButton/Text` → Delete it (or just disable it)

2. **Add Image Component**:
   - Select `PauseButton` GameObject
   - In Inspector, find **Image** component
   - This will be your icon display

   OR create a child Image:
   - Right-click `PauseButton` → **UI** → **Image**
   - Name it: `Icon`
   - Position: Center (0, 0, 0)
   - Size: 64x64 (or match your icon size)

3. **Assign Icons in Inspector**:
   - Select `GameControlOverlay` GameObject
   - **Pause Button Image**: Drag the Image component (from PauseButton or PauseButton/Icon)
   - **Pause Icon**: Drag your pause sprite (⏸️)
   - **Play Icon**: Drag your play sprite (▶️)
   - **Use Icons Instead Of Text**: ✅ Check

### Step 4: Style the Button (Optional)

Make the button transparent so only the icon shows:
1. Select `PauseButton` GameObject
2. **Image** component → **Color** → Set Alpha to 0 (transparent)
3. Or remove the Image component entirely if using a child Icon Image

---

## Configuration Options

### Inspector Settings

Select `GameControlOverlay` GameObject:

**UI References:**
```
Overlay Panel: [OverlayPanel GameObject]
Master Volume Slider: [MasterVolumeSlider]
Volume Value Text: [TextMeshProUGUI component]
Pause Button: [Button component]
```

**Pause Button Display (Choose Text OR Image):**
```
Pause Button Text: [Empty] (legacy Text - optional)
Pause Button Text TMP: [TextMeshProUGUI component] (if using text)
Pause Button Image: [Image component] (if using icons)
Pause Icon: [Sprite ⏸️] (shows when game running)
Play Icon: [Sprite ▶️] (shows when game paused)
```

**Settings:**
```
✅ Start Visible: true
❌ Show In Main Menu: false
☑️ Use Icons Instead Of Text: true (if using icons)
```

---

## Example Configurations

### Configuration A: TMP Text Only
```
Volume Value Text: TextMeshProUGUI component ✅
Pause Button Text TMP: TextMeshProUGUI component ✅
Pause Button Image: Empty
Pause Icon: Empty
Play Icon: Empty
Use Icons Instead Of Text: ❌
```

### Configuration B: Icons Only (Modern UI)
```
Volume Value Text: TextMeshProUGUI component ✅
Pause Button Text: Empty
Pause Button Text TMP: Empty
Pause Button Image: Image component ✅
Pause Icon: Pause sprite ✅
Play Icon: Play sprite ✅
Use Icons Instead Of Text: ✅
```

### Configuration C: Mixed (Volume TMP + Text Button)
```
Volume Value Text: TextMeshProUGUI component ✅
Pause Button Text TMP: TextMeshProUGUI component ✅
Pause Button Image: Empty
Use Icons Instead Of Text: ❌
```

---

## Finding/Creating Icons

### Free Icon Resources
- **Unity Default Icons**: Check Unity's built-in UI sprites
- **Font Awesome**: https://fontawesome.com (download as SVG, convert to PNG)
- **Google Material Icons**: https://fonts.google.com/icons
- **Kenney Assets**: https://kenney.nl/assets (free game UI packs)

### Creating Simple Icons in Unity

You can create basic pause/play icons using Unity UI:

**Pause Icon (⏸️):**
1. Create Image → Add two vertical rectangles side by side
2. Take screenshot → Save as sprite

**Play Icon (▶️):**
1. Create Image → Draw a triangle pointing right
2. Take screenshot → Save as sprite

### Recommended Icon Style
- **Solid color** (white or your theme color)
- **Simple shapes** (pause = ||, play = ▶)
- **Transparent background**
- **Square aspect ratio** (64x64 or 128x128)

---

## How It Works

### Icon Logic
```csharp
// When game is RUNNING (unpaused):
pauseButtonImage.sprite = pauseIcon; // Show pause icon ⏸️

// When game is PAUSED:
pauseButtonImage.sprite = playIcon; // Show play icon ▶️
```

### Text Logic (TMP)
```csharp
// When game is RUNNING:
pauseButtonTextTMP.text = "Pause";

// When game is PAUSED:
pauseButtonTextTMP.text = "Resume";
```

### Auto-Hide Unused Elements
The script automatically hides text when using icons, and hides icons when using text.

---

## Styling Tips

### Icon Button Style
```
Button → Image:
  Color: Transparent (0, 0, 0, 0)

Icon Image (child):
  Color: White (or your theme color)
  Size: 64x64
  Preserve Aspect: ✅
```

### Icon Hover Effect
```
Button → Colors:
  Normal: Color(1, 1, 1, 1) - White
  Highlighted: Color(0.8, 0.8, 1, 1) - Light blue tint
  Pressed: Color(0.5, 0.5, 0.5, 1) - Dark gray
```

### Icon with Glow Effect
Add an **Outline** component to the Icon Image:
```
Outline:
  Effect Color: Yellow/Gold
  Effect Distance: (2, -2)
```

---

## Troubleshooting

### Icons not showing
1. Check `Use Icons Instead Of Text` is ✅ checked
2. Verify `Pause Button Image` is assigned
3. Verify both `Pause Icon` and `Play Icon` sprites are assigned
4. Check Image component is enabled and has correct sprite

### Icons not changing when paused
1. Check Console for warnings
2. Verify TimeManager is working (test with pause button)
3. Check that icons are assigned to correct fields (pause vs play)

### Text showing instead of icons
1. Check `Use Icons Instead Of Text` is ✅ checked
2. Verify text fields are empty or components are disabled

### TMP not available in dropdown
1. Import TextMesh Pro: **Window** → **TextMeshPro** → **Import TMP Essential Resources**
2. Restart Unity if needed

### Volume text not updating
1. Verify `Volume Value Text` is assigned to TextMeshProUGUI component
2. Check slider `On Value Changed` event is calling the right method

---

## Advanced: Animated Icons

You can animate the pause/play transition:

```csharp
// Add to UpdatePauseButtonText() method:
if (useIconsInsteadOfText && pauseButtonImage != null)
{
    pauseButtonImage.sprite = isPaused ? playIcon : pauseIcon;

    // Add bounce animation
    pauseButtonImage.transform.localScale = Vector3.one * 1.2f;
    LeanTween.scale(pauseButtonImage.gameObject, Vector3.one, 0.2f)
        .setEaseOutBack();
}
```

---

## Complete Setup Example

### Hierarchy Structure
```
GameControlOverlay
└─ Canvas
    └─ OverlayPanel
        ├─ VolumeLabel (Text: "Master Volume:")
        ├─ MasterVolumeSlider
        ├─ VolumeValueTextTMP (TextMeshProUGUI: "100%")
        └─ PauseButton
            └─ Icon (Image component - shows pause/play sprite)
```

### Inspector Assignments
```
GameControlOverlay:
  Overlay Panel: OverlayPanel
  Master Volume Slider: MasterVolumeSlider
  Volume Value Text: VolumeValueTextTMP
  Pause Button: PauseButton
  Pause Button Image: PauseButton/Icon (Image component)
  Pause Icon: sprite_pause_icon
  Play Icon: sprite_play_icon
  Start Visible: ✅
  Use Icons Instead Of Text: ✅
```

---

## Summary

✅ **TextMeshProUGUI** - Works perfectly for volume percentage  
✅ **Icon Support** - Use pause/play sprites instead of text  
✅ **Flexible** - Mix and match text and icons  
✅ **Auto-Hide** - Unused elements automatically hidden  
✅ **Simple Setup** - Just assign sprites and check the box  

Choose the option that fits your game's visual style! 🎮
