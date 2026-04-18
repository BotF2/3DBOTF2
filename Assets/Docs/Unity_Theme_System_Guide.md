# Unity Theme System Implementation Guide

## Overview
The Theme System allows UI elements to automatically change their appearance based on the selected civilization. Buttons, images, colors, and other UI elements adapt to match the Federation, Klingon, Romulan, or other civilizations.

## System Components

### 1. ThemeSO (ScriptableObject)
**File:** `Assets\Script\Core\ThemeSO.cs`

Contains all visual assets for a single civilization:
- **Button sprites** (ButtonSprite1-4) for different button types
- **Colors** (Background, Foreground, Highlight, etc.)
- **UI images** (Insignia, backgrounds, facility icons)
- **Text settings**

### 2. ThemeManager (Singleton)
**File:** `Assets\Script\UI\ThemeManager.cs`

Manages theme switching and broadcasts changes:
- Stores all civilization ThemeSOs
- Switches themes when civilization is selected
- Notifies all ThemedUIElements when theme changes

### 3. ThemedUIElement (Component)
**File:** `Assets\Script\UI\ThemedUIElement.cs`

Attach to any UI element that should respond to theme changes:
- Automatically updates when theme switches
- Configurable for buttons, images, text, or colors
- No code required - all settings in Inspector

## How to Set Up Themed UI

### Step 1: Create Theme ScriptableObjects

1. **Create ThemeSO for each civilization:**
   - Right-click in Project → Create → ThemeSO
   - Name them: `Theme_FED`, `Theme_ROM`, `Theme_KLING`, etc.

2. **Assign sprites to each ThemeSO:**
   ```
   Theme_FED:
     - ButtonSprite1: FED_Button_Normal
     - ButtonSprite2: FED_Button_Large
     - ButtonSprite3: FED_Button_Small
     - ButtonSprite4: FED_Button_Icon
     - BackgroundColor: Blue
     - HighlightColor: Cyan
     - Insignia: FED_Insignia_Sprite
   ```

3. **Repeat for all 7 civilizations:**
   - FED (Federation)
   - ROM (Romulan)
   - KLING (Klingon)
   - CARD (Cardassian)
   - DOM (Dominion)
   - BORG (Borg)
   - TERRAN (Terran Empire)

### Step 2: Configure ThemeManager

1. **Find ThemeManager in PersistentScene**
   - Should already exist in your PersistentScene
   - If not, create empty GameObject → Add ThemeManager component

2. **Assign ThemeSOs in Inspector:**
   ```
   ThemeManager:
     Theme SOs:
       Element 0: Theme_FED
       Element 1: Theme_ROM
       Element 2: Theme_KLING
       Element 3: Theme_CARD
       Element 4: Theme_DOM
       Element 5: Theme_BORG
       Element 6: Theme_TERRAN
   ```

   **IMPORTANT:** Order must match ThemeEnum in code!

### Step 3: Add ThemedUIElement to Buttons

1. **Select a button in your UI hierarchy**

2. **Add Component → ThemedUIElement**

3. **Configure in Inspector:**
   ```
   ThemedUIElement:
     Theme Target: Button
     Button Sprite Slot: 1 (use 1-4 based on button type)
     Color Type: Primary
     Apply On Start: ✓ (checked)
   ```

4. **Repeat for all buttons you want themed**

### Step 4: Test Theme Switching

1. **Play the game**
2. **Select different civilizations in Main Menu**
3. **Buttons should change appearance automatically**

## Usage Examples

### Example 1: Themed Menu Button
```
GameObject: Button_StartGame
  - Button (Component)
  - Image (Component) ← This will show the themed sprite
  - ThemedUIElement (Component)
      Theme Target: Button
      Button Sprite Slot: 1
      Apply On Start: ✓
```

**Result:** Button shows FED_Button when Federation is selected, ROM_Button when Romulan is selected, etc.

### Example 2: Themed Panel Background
```
GameObject: Panel_StarSystem
  - Image (Component)
  - ThemedUIElement (Component)
      Theme Target: BackgroundColor
      Color Type: Background
      Apply On Start: ✓
```

**Result:** Panel background color changes to civilization colors.

### Example 3: Themed Icon/Image
```
GameObject: Image_Insignia
  - Image (Component)
  - ThemedUIElement (Component)
      Theme Target: Image
      Image Type: Insignia
      Apply On Start: ✓
```

**Result:** Shows Federation insignia, Klingon emblem, etc.

### Example 4: Themed Text Color
```
GameObject: Text_Title
  - TMP_Text (Component)
  - ThemedUIElement (Component)
      Theme Target: Text
      Color Type: Text
      Apply On Start: ✓
```

**Result:** Text color matches civilization theme.

## Button Sprite Slots Explained

ThemeSO has **4 button sprite slots** for different button types:

| Slot | Recommended Use | Example |
|------|----------------|---------|
| 1 | Standard menu buttons | Start Game, Options, Quit |
| 2 | Large action buttons | Build, Deploy, Attack |
| 3 | Small icon buttons | Close, Minimize, Help |
| 4 | Special buttons | Tech Tree, Diplomacy |

**Assignment Strategy:**
- **Slot 1**: Most common menu buttons (80% of buttons)
- **Slot 2**: Important action buttons that need emphasis
- **Slot 3**: Utility/system buttons
- **Slot 4**: Context-specific buttons (combat, galaxy, etc.)

## Theme Switching Flow

```
1. Player selects Federation in MainMenu
   ↓
2. MainMenuUIController.SetLocalCivilization(0)
   ↓
3. ThemeManager.Instance.ApplyTheme(ThemeEnum.Fed)
   ↓
4. ThemeManager loads Theme_FED ScriptableObject
   ↓
5. ThemeManager.NotifyThemeChanged() broadcasts change
   ↓
6. All ThemedUIElement components receive notification
   ↓
7. Each ThemedUIElement updates its appearance
   ↓
8. UI now shows Federation-themed buttons/colors
```

## Advanced: Custom Theme Behaviors

### Listen to Theme Changes in Code

```csharp
using BOTF3D.Core;

public class MyCustomUI : MonoBehaviour
{
    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += HandleThemeChanged;
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    private void HandleThemeChanged(ThemeSO newTheme)
    {
        Debug.Log($"Theme changed to: {newTheme.name}");
        // Custom logic here
    }
}
```

### Manually Apply Theme

```csharp
using BOTF3D.UI;

public class MyButton : MonoBehaviour
{
    private ThemedUIElement themedElement;

    private void Start()
    {
        themedElement = GetComponent<ThemedUIElement>();
    }

    public void RefreshTheme()
    {
        themedElement?.ApplyTheme();
    }
}
```

### Get Current Theme

```csharp
ThemeSO currentTheme = ThemeManager.Instance.CurrentTheme;
Sprite insignia = currentTheme.Insignia;
Color primaryColor = currentTheme.ForegroundColor;
```

## Organizing Theme Assets

### Recommended Folder Structure
```
Assets/
├── Art/
│   ├── Themes/
│   │   ├── FED/
│   │   │   ├── Buttons/
│   │   │   │   ├── FED_Button_Normal.png
│   │   │   │   ├── FED_Button_Large.png
│   │   │   │   └── FED_Button_Small.png
│   │   │   ├── Icons/
│   │   │   │   ├── FED_Insignia.png
│   │   │   │   └── FED_Background.png
│   │   ├── ROM/
│   │   │   ├── Buttons/
│   │   │   └── Icons/
│   │   ├── KLING/
│   │   └── ...
│   └── ScriptableObjects/
│       └── Themes/
│           ├── Theme_FED.asset
│           ├── Theme_ROM.asset
│           └── ...
```

## Creating Theme Assets

### Photoshop/Art Guidelines

1. **Button Design:**
   - Create base button in civilization style
   - Save variations: Normal, Highlighted, Pressed
   - Use consistent size (e.g., 200x60px for standard buttons)
   - Include civilization motifs (Federation arrowhead, Klingon emblem, etc.)

2. **Color Schemes:**
   - **Federation**: Blues, silvers, white
   - **Klingon**: Reds, blacks, gold
   - **Romulan**: Greens, silvers, dark
   - **Cardassian**: Oranges, browns, purple
   - **Dominion**: Purples, blacks, white
   - **Borg**: Greens, blacks, metallics
   - **Terran**: Golds, reds, imperial

3. **Export Settings:**
   - Format: PNG with transparency
   - Resolution: 2x for Retina/HD support
   - Compression: Minimal (UI sprites don't need heavy compression)

## Troubleshooting

### Issue: Buttons don't change when switching civilizations

**Solution:**
1. Check ThemeManager has all 7 ThemeSOs assigned
2. Verify buttons have ThemedUIElement component
3. Ensure ThemeSO has sprites in ButtonSprite1-4 fields
4. Check Console for "Theme applied" messages

### Issue: Some buttons themed, others not

**Solution:**
1. Verify ALL buttons have ThemedUIElement component
2. Check "Apply On Start" is enabled
3. Manually call `ThemeManager.Instance.RefreshAllThemedElements()` after scene load

### Issue: Wrong sprites showing

**Solution:**
1. Check Button Sprite Slot number (1-4) matches intended sprite
2. Verify ThemeSO has sprites in correct slots
3. Ensure ThemeSO array order in ThemeManager matches ThemeEnum

### Issue: Theme changes in MainMenu but not in Galaxy scene

**Solution:**
1. Add ThemedUIElement to buttons in ALL scenes
2. ThemedUIElement automatically applies theme OnEnable()
3. If needed, manually call RefreshAllThemedElements() when scene loads

## Performance Considerations

✅ **Efficient:**
- ThemedUIElement only updates when theme actually changes
- Uses event system to avoid polling
- Minimal overhead per frame

⚠️ **Avoid:**
- Don't call `ApplyTheme()` every frame
- Don't search for ThemedUIElements repeatedly
- Don't create new ThemeSOs at runtime

## Integration with Existing Code

Your `MainMenuUIController.SetLocalCivilization()` already calls:
```csharp
ThemeManager.Instance.ApplyTheme((ThemeEnum)index);
```

This automatically triggers theme updates for all ThemedUIElements!

## Next Steps

1. ✅ Create 7 ThemeSO assets (one per civilization)
2. ✅ Assign button sprites to each ThemeSO
3. ✅ Add ThemedUIElement to your buttons
4. ✅ Configure Button Sprite Slots
5. ✅ Test theme switching in game

## Summary

The Theme System provides:
- ✅ **Automatic button sprite switching** based on civilization
- ✅ **Centralized theme management** via ThemeSO ScriptableObjects
- ✅ **Easy to use** - just add ThemedUIElement component
- ✅ **No code required** for basic theming
- ✅ **Extensible** - can add custom behaviors via events
- ✅ **Performance friendly** - updates only when needed

Your buttons will now automatically match the selected civilization's visual style!
