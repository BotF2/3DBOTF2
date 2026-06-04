# Combat Testing & Debug Tools

This document explains how to use the combat testing and debugging tools added to the project.

## Table of Contents

1. [Combat Debug UI](#combat-debug-ui)
2. [Combat Recorder](#combat-recorder)
3. [Combat Scenario Editor](#combat-scenario-editor)
4. [Automated Tests](#automated-tests)
5. [Quick Testing Workflow](#quick-testing-workflow)

---

## Combat Debug UI

**What it does:** Provides an in-game overlay showing combat state and test controls.

**How to use:**
1. Start a combat in Play Mode
2. Press **F1** to toggle the debug UI
3. The overlay shows:
   - Current turn and phase
   - Orders for both sides
   - Ship counts and HP totals
   - Last turn damage dealt
   - Order multipliers

**Debug Controls (buttons on the overlay):**
- **Skip Turn**: Immediately end current turn and start next turn (useful for rapid testing)
- **Side 1 Win**: Instantly destroy all Side 2 ships (test victory conditions)
- **Side 2 Win**: Instantly destroy all Side 1 ships (test defeat conditions)
- **End Combat**: Immediately exit combat and return to galaxy (test cleanup)

**Customization:**
- Change toggle key: Edit `CombatDebugUI.cs` → `ToggleKey` field
- Show on start: Set `ShowOnStart = true` in the script

**Location:** `Assets/Script/Combat/Debug/CombatDebugUI.cs`

---

## Combat Recorder

**What it does:** Automatically records every combat as JSON for replay, debugging, and bug reports.

**How to use:**
1. Recordings are **automatic** — every combat is recorded by default
2. Recordings are saved to: `%UserProfile%/AppData/LocalLow/<CompanyName>/<ProjectName>/CombatRecordings/`
3. Files are named: `combat_<CombatID>_<timestamp>.json`

**What's recorded:**
- Initial state: ship composition, orders, HP, positions
- Every turn: orders, damage dealt, ships destroyed, ship positions
- Final state: combat outcome

**Viewing recordings:**
1. Navigate to the recordings folder (see path above)
2. Open a `.json` file in any text editor
3. Use for:
   - Bug reports (attach the recording)
   - Analyzing unexpected outcomes
   - Debugging specific combat scenarios

**Manual save:**
```csharp
// In CombatController or TurnBasedCombatResolver
var recorder = GetComponent<CombatRecorder>();
recorder.SaveRecording("my_custom_name.json");
```

**Load and analyze recordings:**
```csharp
using BOTF3D.Combat.Testing;

// Load a recording
CombatRecording recording = CombatRecorder.LoadRecording("combat_123_2026-06-01_14-30-00.json");

// Analyze turns
foreach (var turn in recording.TurnSnapshots)
{
    Debug.Log($"Turn {turn.TurnNumber}: {turn.SideOneOrder} vs {turn.SideTwoOrder}");
    Debug.Log($"  S1 Damage: {turn.SideOneDamageDealt}, S2 Damage: {turn.SideTwoDamageDealt}");
}
```

**Disable recording:**
```csharp
// In CombatController.InitializeDebugTools()
combatRecorder.IsRecording = false;
```

**Location:** `Assets/Script/Combat/Testing/CombatRecorder.cs`

---

## Combat Scenario Editor

**What it does:** Unity Editor window for quickly setting up and testing combat scenarios without playing through the full game.

**IMPORTANT: Prerequisites**
The Combat Scenario Editor requires game managers to be initialized. You cannot use it from an empty scene.

**Proper Setup:**
1. Load **PersistentScene** in Hierarchy (Unity Editor)
2. Open **MainMenuScene** additively (right-click scene → Open Scene Additive)
3. Press **Play** (Unity Editor)
4. In the **Game window** (running game), click "New Game" or "Load Game" button
5. Wait for the **galaxy map** to appear (gameplay view with stars/planets)
6. Now in Unity Editor, open **BOTF → Combat Scenario Editor**
7. Verify status shows **"✅ Game Managers Ready"** (green)

**How to open:**
- Unity Editor → **BOTF** menu → **Combat Scenario Editor**

**How to use:**

### Create a scenario:
1. Set **Scenario Name** (e.g., "Rush vs Formation Test")
2. Choose civilizations for each side
3. Set initial combat orders
4. Configure ship composition using sliders:
   - Side 1: Scouts, Destroyers, Cruisers, Battleships, Transports
   - Side 2: Same options
5. Click **Save Scenario** to save for later use

### Quick test:
1. Click **Load Quick Test Scenario** to load a pre-configured 3v4 battle
2. Enter Play Mode
3. Click **Start Combat** in the editor window

### Load saved scenarios:
- Saved scenarios appear at the bottom of the window
- Click **Load** to edit the scenario
- Click **Start** to load and immediately start combat
- Click **Delete** to remove from saved list

**Pre-made scenarios:**
The editor includes a "Quick Test" scenario (1 Scout, 1 Destroyer, 1 Cruiser, 1 Transport vs 2 Scouts, 1 Destroyer, 1 Battleship).

**How it works:**
The editor uses `CombatScenarioRunner` to:
1. Create ships based on your scenario configuration
2. Request combat through `CombatManager`
3. Set initial combat orders
4. Load the Combat Scene automatically

The combat will start a few seconds after clicking "Start Combat". Press F1 during combat to see the debug overlay.

**Location:** `Assets/Editor/CombatScenarioEditor.cs`

---

## Automated Tests

**What it does:** Unit tests for combat mechanics using Unity Test Runner.

**How to run tests:**
1. Unity Editor → **Window** → **General** → **Test Runner**
2. Click **EditMode** tab
3. Click **Run All** or select individual tests
4. Tests run instantly (no Play Mode needed)

**Available tests:**

### CombatOrderTests:
- `OrderMultiplier_SameOrders_ReturnsNeutral`: Verify neutral multiplier for mirror matches
- `OrderMultiplier_NoneOrder_ReturnsNeutral`: Verify None orders don't break multipliers
- `OrderDescription_AllOrders_ReturnsValidStrings`: Ensure all orders have descriptions
- `IsRetreating_RetreatOrder_ReturnsTrue`: Test retreat detection
- `OrderProtectsTransports_FormationOrder_ReturnsTrue`: Test transport protection
- `OrderBypassesLOS_AttackTransports_ReturnsTrue`: Test flanking mechanics

### CombatDataTests:
- `CombatData_Constructor_InitializesLists`: Verify CombatData initializes correctly
- `CombatData_DefaultValues_AreCorrect`: Test default values
- `ShipPhaseTracker_Initialization_WorksCorrectly`: Test movement state tracker

### TurnResultTests:
- `TurnResult_Construction_InitializesFields`: Test turn result data structure
- `TurnResult_ShipsDestroyedList_IsInitialized`: Verify destroyed ships list

**Adding new tests:**
1. Open `Assets/Tests/CombatOrderTests.cs`
2. Add a new test method:
```csharp
[Test]
public void MyNewTest()
{
    // Arrange
    var data = new CombatData();
    
    // Act
    bool result = SomeFunction(data);
    
    // Assert
    Assert.IsTrue(result, "Expected true");
}
```
3. Save and run in Test Runner

**Location:** `Assets/Tests/CombatOrderTests.cs`

---

## Quick Testing Workflow

Here's the recommended workflow for testing combat changes:

### 1. Make a code change
Example: Modify ship movement speed in `ShipMovementController.cs`

### 2. Run unit tests
- Open Test Runner
- Click "Run All"
- Verify no tests broke

### 3. Use Scenario Editor
- Open Combat Scenario Editor
- Load "Quick Test" scenario (or create custom)
- Modify scenario to test your change
- Enter Play Mode
- Start combat

### 4. Use Debug UI during combat
- Press F1 to show debug overlay
- Monitor relevant stats
- Use "Skip Turn" to test rapidly
- Use force win buttons to test edge cases

### 5. Check recordings
- After combat ends, navigate to recordings folder
- Open the JSON file
- Verify the recorded data matches expectations
- Attach recording to bug reports if needed

### Example: Testing a new order

**Goal:** Add "Evasive" order that reduces damage taken by 50%

**Steps:**
1. Add `Evasive` to `CombatOrders` enum
2. Write unit test:
```csharp
[Test]
public void OrderMultiplier_Evasive_ReducesDamage()
{
    float mult = CombatOrderHelper.GetOrderMultiplier(CombatOrders.Rush, CombatOrders.Evasive);
    Assert.AreEqual(0.5f, mult);
}
```
3. Implement multiplier logic in `CombatOrderHelper.GetOrderMultiplier()`
4. Run unit test → verify it passes
5. Open Scenario Editor → create "Rush vs Evasive" scenario
6. Start combat → press F1 → verify multipliers show 2.0x for Rush, 0.5x for Evasive
7. Let combat run → check recording → verify Evasive side took less damage

---

## Troubleshooting

**Debug UI not showing:**
- Verify you're in Play Mode
- Press F1 (check console for "Combat Debug UI: SHOWN")
- Check `CombatController` has `CombatDebugUI` component attached

**Recordings not saving:**
- Check console for "Combat recording saved: <path>"
- Verify `AutoSaveOnEnd = true` in `CombatRecorder`
- Check recordings folder exists (it's created automatically)

**Tests not appearing in Test Runner:**
- Verify `Assets/Tests/Tests.asmdef` exists
- Reimport test files: Right-click → Reimport
- Close and reopen Test Runner window

**Scenario Editor "Start Combat" does nothing:**
- Ensure you're in Play Mode
- Integration with full combat system may require additional setup
- Use the dialog to verify scenario parameters are correct

---

## Performance Impact

**Combat Recorder:**
- Minimal impact: ~0.1ms per turn
- Only records on turn boundaries (not per-frame)
- Can be disabled with `IsRecording = false`

**Combat Debug UI:**
- Only updates when visible (press F1 to hide)
- ~0.5ms per frame when visible
- No impact when hidden

**Recommendation:** Leave both enabled during development, disable for production builds.

---

## Future Enhancements

Planned features:
- [ ] Combat replay viewer (load and watch recorded combats)
- [ ] Scenario Editor integration with full combat system
- [ ] Automated test scenarios (run 100 combats, check for crashes)
- [ ] Performance profiler in Debug UI
- [ ] Combat balance analyzer (win rates by order matchup)
- [ ] Ship behavior visualizer (show movement paths, targeting lines)

---

## Questions?

Contact: [Your Name/Team]  
Last Updated: 2026-06-01
