# Theme System Quick Setup Checklist

## ✅ Step-by-Step Setup Guide

### Part 1: Create Theme Assets (Do Once)

- [ ] **Create ThemeSO for Federation**
  - Right-click Project → Create → ThemeSO
  - Name: `Theme_FED`
  - Assign Fed button sprites to ButtonSprite1-4
  - Set colors (blue, cyan, white)
  - Assign Fed insignia

- [ ] **Create ThemeSO for Romulan**
  - Name: `Theme_ROM`
  - Assign Rom button sprites
  - Set colors (green, silver, dark)
  - Assign Rom insignia

- [ ] **Create ThemeSO for Klingon**
  - Name: `Theme_KLING`
  - Assign Kling button sprites
  - Set colors (red, gold, black)
  - Assign Kling insignia

- [ ] **Create ThemeSO for Cardassian**
  - Name: `Theme_CARD`
  - Assign Card button sprites
  - Set colors (orange, purple, brown)
  - Assign Card insignia

- [ ] **Create ThemeSO for Dominion**
  - Name: `Theme_DOM`
  - Assign Dom button sprites
  - Set colors (purple, white, black)
  - Assign Dom insignia

- [ ] **Create ThemeSO for Borg**
  - Name: `Theme_BORG`
  - Assign Borg button sprites
  - Set colors (green, black, metallic)
  - Assign Borg insignia

- [ ] **Create ThemeSO for Terran**
  - Name: `Theme_TERRAN`
  - Assign Terran button sprites
  - Set colors (gold, red, imperial)
  - Assign Terran insignia

### Part 2: Configure ThemeManager (Do Once)

- [ ] **Open PersistentScene**
  - Find ThemeManager GameObject (should already exist)
  - If missing, create empty GameObject → Add ThemeManager component

- [ ] **Assign ThemeSOs in order:**
  ```
  Theme SOs (Array Size: 7)
    [0] = Theme_FED
    [1] = Theme_ROM
    [2] = Theme_KLING
    [3] = Theme_CARD
    [4] = Theme_DOM
    [5] = Theme_BORG
    [6] = Theme_TERRAN
  ```
  **⚠️ ORDER MATTERS! Must match ThemeEnum.**

### Part 3: Add Theming to UI Elements (Per Scene)

#### MainMenu Scene

- [ ] **Panel-Lobby buttons**
  - Button_SinglePlayer → Add ThemedUIElement
    - Theme Target: Button
    - Button Sprite Slot: 1
    - Apply On Start: ✓
  
  - Button_Multiplayer → Add ThemedUIElement
    - Same settings as above

- [ ] **Panel-CivSelection buttons**
  - All civilization selection buttons → Add ThemedUIElement
    - Theme Target: Button
    - Button Sprite Slot: 1

- [ ] **Panel-GameParameters buttons**
  - Button_StartGame → Add ThemedUIElement
    - Theme Target: Button
    - Button Sprite Slot: 2 (large action button)
  
  - Button_Back → Add ThemedUIElement
    - Theme Target: Button
    - Button Sprite Slot: 1

#### Galaxy Scene

- [ ] **CanvasGalaxy buttons**
  - Button_EndTurn → Add ThemedUIElement (Slot 2)
  - Button_Fleet → Add ThemedUIElement (Slot 1)
  - Button_StarSystem → Add ThemedUIElement (Slot 1)
  - Button_Diplomacy → Add ThemedUIElement (Slot 4)
  - Button_TechTree → Add ThemedUIElement (Slot 4)

- [ ] **Panel backgrounds**
  - Panel_Fleet → Add ThemedUIElement
    - Theme Target: BackgroundColor
    - Color Type: Background

  - Panel_StarSystem → Add ThemedUIElement
    - Same as above

#### Combat Scene (if applicable)

- [ ] **Combat UI buttons**
  - Button_Fire → Add ThemedUIElement (Slot 2)
  - Button_Retreat → Add ThemedUIElement (Slot 1)
  - Button_Shields → Add ThemedUIElement (Slot 1)

### Part 4: Testing

- [ ] **Test Federation theme**
  - Start game
  - Select Federation
  - Check all buttons show Fed sprites
  - Check colors match Fed theme

- [ ] **Test Klingon theme**
  - Return to main menu
  - Select Klingon
  - Verify buttons change to Kling sprites
  - Verify colors change to red/gold

- [ ] **Test all civilizations**
  - Repeat for ROM, CARD, DOM, BORG, TERRAN
  - Ensure each has unique appearance

- [ ] **Test scene transitions**
  - Select Federation
  - Start game → Enter Galaxy scene
  - Verify Galaxy buttons use Fed theme
  - Return to main menu
  - Select different civ
  - Verify theme persists

## Common Button Types & Recommended Slots

| Button Type | Recommended Slot | Examples |
|-------------|------------------|----------|
| Menu Navigation | 1 | Options, Back, Cancel |
| Action Buttons | 2 | Start Game, Deploy, Attack |
| Icon Buttons | 3 | Close, Help, Settings |
| Context Buttons | 4 | Diplomacy, Tech Tree, Combat |

## Verification Checklist

- [ ] ThemeManager exists in PersistentScene
- [ ] ThemeManager has 7 ThemeSOs assigned in correct order
- [ ] Each ThemeSO has at least ButtonSprite1 assigned
- [ ] MainMenu buttons have ThemedUIElement component
- [ ] Galaxy buttons have ThemedUIElement component
- [ ] Theme switches correctly when changing civilizations
- [ ] No console errors when switching themes

## Quick Test Script

Add this to ThemeManager for testing:

```csharp
[ContextMenu("Test: Apply Federation Theme")]
void TestFedTheme()
{
    ApplyTheme(ThemeEnum.Fed);
}

[ContextMenu("Test: Apply Klingon Theme")]
void TestKlingTheme()
{
    ApplyTheme(ThemeEnum.Kling);
}

[ContextMenu("Test: Refresh All Themed Elements")]
void TestRefresh()
{
    RefreshAllThemedElements();
}
```

**Usage:** Right-click ThemeManager in Inspector → Select test command

## Troubleshooting Quick Fixes

### Buttons not changing?
1. Check ThemeSO has sprites assigned
2. Verify ThemedUIElement component exists on button
3. Look for errors in Console

### Wrong colors?
1. Check ThemeSO color values
2. Verify Color Type setting in ThemedUIElement

### Theme not persisting across scenes?
1. Ensure ThemeManager is DontDestroyOnLoad (should be automatic)
2. Add ThemedUIElement to buttons in new scene
3. Call RefreshAllThemedElements() after scene load if needed

## Expected Results

✅ After setup, you should see:
- Buttons change sprites when selecting different civilizations
- Colors match civilization theme
- Insignias update automatically
- Theme persists across scene transitions
- All UI elements feel cohesive to selected civilization

## Time Estimate

- Creating 7 ThemeSOs: ~30-60 minutes
- Adding ThemedUIElement to buttons: ~10-20 minutes per scene
- Testing: ~15-30 minutes
- **Total: 1-2 hours** for complete implementation

## Next Steps After Setup

1. **Federation button sprites** - Refine artwork for each civilization
2. **Add sound effects** - Assign button click sounds per theme
3. **Create variations** - Add more button sprite slots for different contexts
4. **Extend to other UI** - Apply theming to panels, tooltips, dialogs
5. **Add animations** - Theme-specific button hover/press animations

---

**Ready to start?** Begin with Part 1 and work through each checkbox!
