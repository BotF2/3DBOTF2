# Combat Scenario Editor - Now Fully Functional

## Summary

The **Combat Scenario Editor** is now fully functional! The missing `CombatScenarioRunner` component has been implemented, allowing you to create and run combat scenarios directly from the Unity Editor.

---

## What Was Added

### 1. **CombatScenarioRunner.cs** (NEW)
**Location:** `Assets/Script/Combat/Testing/CombatScenarioRunner.cs`

**What it does:**
- Creates ships based on scenario configuration
- Registers ships with `ShipManager`
- Requests combat through `CombatManager`
- Sets initial combat orders for both sides
- Handles all the plumbing between the editor and the combat system

**Key Features:**
- Singleton pattern (Instance)
- Supports test scenarios defined in editor
- Optional `AutoStartOnPlay` for quick testing
- Full validation of managers before running
- Uses `GameLogger` for all logging (Combat category)

### 2. **Updated CombatScenarioEditor.cs**
**Location:** `Assets/Editor/CombatScenarioEditor.cs`

**Changes:**
- `StartCombatScenario()` now creates/finds `CombatScenarioRunner` and calls `RunScenario()`
- Shows success dialog with ship counts and reminder to press F1
- Removed duplicate `CombatScenario` and `ScenarioList` classes (now in runner)
- All references updated to use `BOTF3D.Combat.Testing.CombatScenario`

### 3. **Updated README_COMBAT_TESTING.md**
**Location:** `Assets/Script/Combat/Testing/README_COMBAT_TESTING.md`

**Changes:**
- Removed "not yet implemented" warning
- Added explanation of how the scenario runner works
- Updated instructions for using the editor

---

## How to Use the Combat Scenario Editor

### Step 1: Start the Game Properly
**CRITICAL:** Game managers must be initialized first!

**In Unity Editor:**
1. Load **PersistentScene** in Hierarchy
2. Open **MainMenuScene** additively (right-click → Open Scene Additive)
3. Press **Play**
4. In the **Game window** (the running game), use the in-game menu:
   - Click **"New Game"** button and start a new game, OR
   - Click **"Load Game"** button and load an existing save
5. Wait for the **galaxy map** to appear (you'll see stars, planets, and the galaxy UI)
6. You're now in the galaxy gameplay view

**Why?** The Combat Scenario Editor requires:
- `ShipManager` (creates ships)
- `CombatManager` (handles combat)
- `TimeManager` (pauses time)
- `SceneController` (loads Combat Scene)

These only initialize during normal game startup, not from an empty scene.

### Step 2: Open the Editor
**Unity → BOTF menu → Combat Scenario Editor**

The editor will show a status indicator:
- ✅ **Green "Game Managers Ready"** → You can start combat
- ⚠️ **Red "Game Managers Missing"** → Follow Step 1 first

### Step 3: Configure Scenario
1. **Scenario Name:** Give it a descriptive name (e.g., "Rush vs Formation Test")
2. **Side One:**
   - Civilization: `FED`, `KLING`, `ROM`, `CARD`, `DOM`, `BORG`, `TERRAN`
   - Tech Level: `EARLY` (I), `DEVELOPED` (II), `ADVANCED` (III), `SUPREME` (IV)
   - Combat Order: `Engage`, `Rush`, `Formation`, `Retreat`, `AttackTransports`
   - Ship counts: Use sliders (0-10 for most, 0-5 for Battleships)
3. **Side Two:**
   - Same configuration options

**Tech Level Impact:**
- Higher tech levels have stronger ships with better stats
- Tech level determines which ship variants are available
- Example: FED Tech I Scout vs FED Tech IV Scout have different HP/damage

### Step 3: Save (Optional)
Click **"Save Scenario"** to save it for later use. Saved scenarios appear at the bottom of the window.

### Step 4: Run the Scenario
1. Verify status shows **"✅ Game Managers Ready"** (green)
2. Click **"Start Combat"** in the editor window
3. Combat will start in a few seconds
4. Press **F1** to see the debug overlay

**If you see ⚠️ Red Warning:**
- Click "Show Details" to see which managers are missing
- Follow the instructions in the dialog to properly start the game

### Step 5: Test Features
During combat:
- **F1** → Toggle debug UI overlay
- **Debug UI buttons:**
  - Skip Turn → Fast-forward to next turn
  - Side 1/2 Win → Test victory conditions
  - End Combat → Return to galaxy

---

## Quick Test Workflow

**Scenario:** Test Tech I vs Tech IV balance

1. **BOTF → Combat Scenario Editor**
2. Click **"Load Quick Test Scenario"** (pre-configured)
3. Change Side 1 Tech Level to `EARLY` (Tech I)
4. Change Side 2 Tech Level to `SUPREME` (Tech IV)
5. Enter Play Mode
6. Click **"Start Combat"**
7. Press **F1** to watch damage comparison
8. Use "Skip Turn" to see if numbers overcome tech advantage

---

## What Happens Under the Hood

When you click "Start Combat":

1. **Validation:**
   - Checks if in Play Mode
   - Validates `ShipManager`, `CombatManager`, `CivManager`

2. **Ship Creation:**
   - For each ship type (Scouts, Destroyers, etc.):
     - Finds appropriate `ShipSO` template for civ and tech level
     - Creates `ShipController` via `ShipManager.CreateGalaxyShip()`
     - Registers ship in `ShipManager`
     - Sets ship name: `{Civ}_{ShipType}_{Index}`
     - Deactivates until combat starts

3. **Combat Initialization:**
   - Pauses game time (`TimeManager.PauseTime()`)
   - Calls `CombatManager.RequestCombat()`
   - Waits for `ActiveCombatController` to be created
   - Sets combat orders for both sides

4. **Scene Loading:**
   - `CombatManager` loads Combat Scene
   - Ships are positioned and activated
   - Warp-in animation plays
   - Turn-based combat begins

---

## Saved Scenarios

**Storage:** Scenarios are saved in Unity Editor Preferences (persists across sessions)

**Management:**
- **Load:** Click "Load" to edit the scenario
- **Start:** Click "Start" to load and immediately run
- **Delete:** Click "Delete" to remove from saved list

---

## Technical Details

### CombatScenario Class
```csharp
public class CombatScenario
{
    public string name;
    public CivEnum sideOneCiv, sideTwoCiv;
    public TechLevel sideOneTechLevel, sideTwoTechLevel;
    public CombatOrders sideOneOrder, sideTwoOrder;
    public int s1Scouts, s1Destroyers, s1Cruisers, s1Battleships, s1Transports;
    public int s2Scouts, s2Destroyers, s2Cruisers, s2Battleships, s2Transports;
}
```

### Ship Creation Process
- Tech level explicitly set by scenario configuration (no longer defaults)
- Uses `ShipManager.GetShipSOAtBestTechLevel(shipType, techLevel, civEnum)` to find template
- Falls back to `GetFallbackShipSO()` if no template found for specified tech level
- Ships created with full HP/shields appropriate for their tech level
- Example: Tech IV Cruiser has significantly more HP than Tech I Cruiser

### Combat Flow Integration
The scenario runner integrates seamlessly with the existing combat system:
- Ships → `ShipManager.CreateGalaxyShip()` → registered in `ShipRegistry`
- Combat → `CombatManager.RequestCombat()` → queued via `CombatQueueManager`
- Scene → `CombatSceneLoader` finds UI canvases
- Controller → `CombatInstantiator` creates `CombatController`

---

## Known Limitations

1. **Must be in Play Mode:** Cannot start combat from Edit Mode (Unity limitation)
2. **No Fleet Context:** Ships created without parent fleet (combat-only)
3. **No Persistence:** Test ships are destroyed after combat ends
4. **Single Combat Only:** Cannot queue multiple scenarios
5. **Ship Availability:** If a ShipSO doesn't exist for a given civ/tech/type combo, fallback ship is used

---

## Troubleshooting

### "CombatManager.Instance is null" / "ShipManager.Instance is null"
**Root Cause:** Managers haven't been initialized yet.

**Solution:**
1. **DO NOT** just press Play in an empty scene or single scene
2. **DO** follow the proper startup:
   - Load PersistentScene
   - Open MainMenuScene additively
   - Press Play
   - Start or load a game
   - Wait for Galaxy scene to load
   - Then open Combat Scenario Editor

**Why?** Managers are created during game initialization and marked as `DontDestroyOnLoad`. They don't exist if you skip normal startup.

### "No ShipSO found for X at Y"
**Solution:** 
- Check that ship lists are populated in `ShipManager` Inspector
- Run "Auto-Populate" if needed
- Some civ/tech/type combinations may not have ship templates defined
- Fallback ship will be used if specific template is missing

### Combat doesn't start
**Solution:** 
1. Check Console for error messages
2. Verify `CombatManager` GameObject exists
3. Ensure Combat Scene is in Build Settings

### Tech level not affecting ships
**Solution:** 
- Verify `ShipManager` has ShipSO templates for the tech level you selected
- Check console for "No ShipSO found" warnings
- Some civs may not have all tech level variants defined

---

## Future Enhancements

Possible improvements (not yet implemented):

1. ✅ ~~**Tech Level Override:**~~ IMPLEMENTED - Tech level now configurable per side
2. **Position Override:** Set custom starting positions for ships
3. **Multiple Rounds:** Run same scenario N times and average results
4. **Scenario Library:** Pre-built scenarios for common test cases
5. **Replay Support:** Save and replay completed scenarios
6. **AI vs AI:** Run scenarios with both sides AI-controlled
7. **Batch Testing:** Run multiple scenarios sequentially
8. **Ship Variant Selection:** Choose specific ship variants (e.g., Defiant vs Galaxy for FED Cruiser)

---

## Files Modified/Created

### Created:
- `Assets/Script/Combat/Testing/CombatScenarioRunner.cs` (~300 lines)
- `Assets/Script/Combat/Testing/CombatScenarioRunner.cs.meta`
- `COMBAT_SCENARIO_EDITOR_IMPLEMENTATION.md` (this file)

### Modified:
- `Assets/Editor/CombatScenarioEditor.cs` (replaced TODO with working implementation)
- `Assets/Script/Combat/Testing/README_COMBAT_TESTING.md` (updated instructions)

---

## Summary

The Combat Scenario Editor is now **production-ready** for testing combat mechanics. You can:

✅ Create custom scenarios with any ship composition  
✅ Configure tech levels per side (Early, Developed, Advanced, Supreme)  
✅ Test all combat orders (Engage, Rush, Formation, etc.)  
✅ Save and reload scenarios  
✅ Run scenarios instantly (after proper game startup)  
✅ Use debug tools (F1 overlay, skip turns, force wins)  
✅ Analyze results via combat recorder  

**Complete Workflow:**
1. Load PersistentScene + MainMenuScene (additive)
2. Press Play → Start/Load Game → Wait for Galaxy
3. Unity → BOTF → Combat Scenario Editor
4. Verify "✅ Game Managers Ready" shows (green)
5. Click "Load Quick Test Scenario"
6. Click "Start Combat"
7. Press F1 for debug overlay

**Critical:** Never skip Step 1-2 or managers won't be initialized!

Happy testing! 🚀
