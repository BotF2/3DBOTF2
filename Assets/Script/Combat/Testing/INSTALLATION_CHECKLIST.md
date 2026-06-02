# Combat Debug Tools - Installation Checklist

Use this checklist to verify the debug tools are properly installed and working.

## File Installation

Check that all files exist:

### Core Files
- `Assets/Script/Combat/Testing/CombatRecorder.cs`
- `Assets/Script/Combat/Debug/CombatDebugUI.cs`
- `Assets/Script/Combat/Testing/CombatTestingHelper.cs`

### Editor Tools
- `Assets/Editor/CombatScenarioEditor.cs`
- `Assets/Editor/CombatDebugMenu.cs`

### Tests
- `Assets/Tests/CombatOrderTests.cs`
- `Assets/Tests/Tests.asmdef`

### Documentation
- `Assets/Script/Combat/Testing/README_COMBAT_TESTING.md`
- `Assets/Script/Combat/Testing/IMPLEMENTATION_SUMMARY.md`

---

## Quick Verification (5 minutes)

### 1. Check Menu Items
Unity Editor → BOTF menu should show:
- Combat Scenario Editor
- Combat Testing (submenu)

### 2. Run Tests
Window → General → Test Runner → EditMode → Run All
**Expected:** All tests pass (green checkmarks)

### 3. Test Debug UI
1. Enter Play Mode
2. Start any combat
3. Press F1
**Expected:** Debug overlay appears with combat stats

### 4. Check Recordings
After completing a combat:
BOTF → Combat Testing → Open Recordings Folder
**Expected:** Folder opens with .json files

---

## If Something Doesn't Work

**Tests don't appear:**
- Right-click Assets/Tests → Reimport
- Close and reopen Test Runner

**F1 does nothing:**
- Check console for errors
- Verify you're in a combat (not galaxy scene)

**No recordings:**
- Check console for "Combat recording saved" message
- Verify combat completed (not just started)

**Menu items missing:**
- Right-click Assets/Editor → Reimport
- Restart Unity Editor

---

## Complete Documentation

For full usage instructions, see:
- **README_COMBAT_TESTING.md** - Complete usage guide
- **IMPLEMENTATION_SUMMARY.md** - Technical overview

---

Last Updated: 2026-06-01
