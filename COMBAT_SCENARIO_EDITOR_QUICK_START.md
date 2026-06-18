# Combat Scenario Editor - Quick Start Guide

## ⚠️ IMPORTANT: Proper Startup Required

The Combat Scenario Editor **requires game managers** to be initialized first. You **cannot** use it by just pressing Play in Unity.

---

## Step-by-Step Usage

### 1️⃣ Load Required Scenes (ONE TIME SETUP)

In Unity **Hierarchy window**:

1. Load **PersistentScene** 
   - File → Open Scene → Select `PersistentScene.unity`

2. Open **MainMenuScene** additively
   - Right-click `MainMenuScene.unity` in Project window
   - Select **"Open Scene Additive"**

Both scenes should now be visible in Hierarchy.

---

### 2️⃣ Start the Game (In-Game UI)

1. Press **Play** button in Unity
2. Look at the **Game window** (not Hierarchy - the actual game view)
3. You'll see the main menu UI with buttons
4. Click **"New Game"** button to start a new game, OR
5. Click **"Load Game"** button to load an existing save
6. Wait for the **galaxy map** to appear (the starfield with planets/ships)
7. You should now see the galaxy gameplay view with stars and UI

**✅ Managers are now initialized!**

**Note:** This is all happening in the Game window while Unity is in Play Mode. You're clicking buttons in the running game, not in the Unity Editor.

**Visual Guide:**
```
┌─────────────────────────────────────────────────────┐
│ Unity Editor Layout                                 │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Hierarchy         │  Game Window (LOOK HERE!)     │
│  ├─ PersistentScene│  ┌─────────────────────────┐  │
│  └─ MainMenuScene  │  │  BIRTH OF THE FEDERATION│  │
│                    │  │                         │  │
│  Inspector         │  │  [ New Game ]           │  │ ← Click this!
│                    │  │  [ Load Game ]          │  │ ← Or this!
│                    │  │  [ Options ]            │  │
│  Console           │  └─────────────────────────┘  │
│                    │                                │
│                    │  After clicking, you'll see:  │
│                    │  Galaxy map with stars ✨     │
└─────────────────────────────────────────────────────┘
```

---

### 3️⃣ Open Combat Scenario Editor

1. Unity top menu → **BOTF** → **Combat Scenario Editor**
2. Editor window opens
3. Check the status indicator:
   - ✅ **Green "Game Managers Ready"** → Continue to Step 4
   - ⚠️ **Red "Game Managers Missing"** → Go back to Step 1

---

### 4️⃣ Configure Your Scenario

**Scenario Name:** `"Tech Advantage Test"`

**Side One:**
- Civilization: `FED`
- Tech Level: `EARLY` (Tech I)
- Combat Order: `Engage`
- Ships: 3 Cruisers, 2 Destroyers

**Side Two:**
- Civilization: `KLING`
- Tech Level: `SUPREME` (Tech IV)
- Combat Order: `Engage`
- Ships: 1 Cruiser, 1 Destroyer

---

### 5️⃣ Start Combat

1. Click **"Start Combat"** button
2. Wait 2-3 seconds
3. Combat Scene loads automatically
4. Ships appear and combat begins

---

### 6️⃣ Test & Debug

**Press F1** to toggle debug overlay:
- View turn, orders, HP, damage
- Use "Skip Turn" to fast-forward
- Use "Force Win" buttons to test victory screens
- Use "End Combat" to return immediately

**After Combat:**
- Combat recordings saved to `%AppData%/LocalLow/.../CombatRecordings/`
- Check console for damage logs

---

## Quick Test Scenario

**Want to test immediately?**

1. Complete Steps 1-3 above
2. In Combat Scenario Editor, click **"Load Quick Test Scenario"**
3. Click **"Start Combat"**
4. Press **F1** during combat

Pre-configured test:
- FED (Tech II) vs KLING (Tech II)
- 4 ships vs 4 ships
- Engage vs Rush orders

---

## Common Mistakes

### ❌ Pressing Play in Empty Scene
**Problem:** Just pressing Play without loading PersistentScene + MainMenuScene
**Result:** "CombatManager.Instance is null" error
**Solution:** Follow Step 1-2 above

### ❌ Not Starting the Game
**Problem:** Loading scenes and pressing Play, but not clicking "New Game" or "Load Game" **in the Game window**
**Result:** Managers exist but aren't fully initialized
**Solution:** Must actually start a game (click the button in the running game) to reach the galaxy map

### ❌ Looking in Wrong Place for "New Game" Button
**Problem:** Looking in Unity Editor menus/hierarchy instead of Game window
**Result:** Confusion about where to click
**Solution:** The "New Game" button is in the **Game window** (the actual running game view), not in Unity Editor UI

### ❌ Using Editor Window Before Ready
**Problem:** Opening Combat Scenario Editor too early
**Result:** Red warning "Game Managers Missing"
**Solution:** Wait for galaxy map to appear in Game window first

---

## Manager Status Meanings

**✅ Green "Game Managers Ready"**
- All required managers initialized
- ShipManager, CombatManager, TimeManager, SceneController all present
- Safe to start combat scenarios

**⚠️ Red "Game Managers Missing"**
- One or more managers not initialized
- Click "Show Details" to see which ones
- Follow proper startup (Steps 1-3)

---

## Workflow Summary

```
┌─────────────────────────────────────────────┐
│ 1. Load PersistentScene                     │
│ 2. Open MainMenuScene (additive)            │
│ 3. Press Play → Start Game → Galaxy Scene   │
│ 4. Open BOTF → Combat Scenario Editor       │
│ 5. Verify ✅ Green Status                   │
│ 6. Configure scenario                       │
│ 7. Click "Start Combat"                     │
│ 8. Press F1 for debug overlay               │
└─────────────────────────────────────────────┘
```

---

## What You Can Test

✅ **Tech Level Balance**
- Early vs Supreme tech comparison
- Ship HP/damage scaling

✅ **Combat Orders**
- Rush vs Formation
- Engage vs Retreat
- AttackTransports vs defensive orders

✅ **Ship Composition**
- Many weak ships vs few strong ships
- All Scouts vs Mixed fleet
- Transports with escort

✅ **Civ Differences**
- FED ships vs KLING ships
- Same tech level, different civs

✅ **Edge Cases**
- 1v1 combat
- 10v10 massive battles
- All transports vs combat ships

---

## Saving Scenarios

**To save a scenario for later:**
1. Configure all settings in editor
2. Click **"Save Scenario"** button
3. Scenario appears in "Saved Scenarios" list at bottom

**To load saved scenario:**
- Click **"Load"** → Edit the scenario
- Click **"Start"** → Load and immediately run combat

**To delete saved scenario:**
- Click **"Delete"** (red button)

Scenarios persist across Unity sessions (saved in EditorPrefs).

---

## Need Help?

**Check these files:**
- `COMBAT_SCENARIO_EDITOR_IMPLEMENTATION.md` - Full technical docs
- `Assets/Script/Combat/Testing/README_COMBAT_TESTING.md` - All testing tools
- `COMBAT_DEBUG_TOOLS_SUMMARY.md` - Debug overlay guide

**Common Issues:**
- Managers not initialized → Follow Steps 1-3
- Ships not appearing → Check console for "No ShipSO found" warnings
- Combat doesn't start → Verify Combat Scene in Build Settings

---

## Pro Tips

💡 **Use "Load Quick Test Scenario"** first to verify everything works

💡 **Save scenarios before testing** so you can retry exact same setup

💡 **Press F1 immediately** after combat starts to see opening stats

💡 **Use "Skip Turn"** to test 10+ rounds quickly without waiting

💡 **Check recordings folder** after testing to analyze damage over time

💡 **Test one variable at a time** (e.g., only change tech level, keep everything else same)

---

## You're Ready! 🚀

Once you see **✅ Green "Game Managers Ready"**, you can start testing combat scenarios!
