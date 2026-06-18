# Enhanced Tech System - Quick Reference Guide

## What's Been Added

### 1. Granular Ship Unlocking
Ships now unlock at specific tech point thresholds, not just when reaching a new tech level.

**Example:**
- Old system: Reach DEVELOPED (100 points) → All Tier II ships unlock instantly
- New system: 
  - 100 points → Scout_II, Destroyer_II, Transport_II unlock
  - 150 points → Cruiser_II unlocks (mid-progression reward!)

### 2. Ship Naming Convention Support
System recognizes ship tiers from names:
- `FED_DESTROYER_I` → Tier 1 (Early, 25 points)
- `FED_DESTROYER_II` → Tier 2 (Developed, 100 points)
- `FED_DESTROYER_III` → Tier 3 (Advanced, 300 points)
- `FED_DESTROYER_IV` → Tier 4 (Supreme, 600 points)

### 3. Improved Ship Progression
**EARLY (0-99 points):**
- 0 pts: Scout_I
- 25 pts: Destroyer_I
- 50 pts: Transport_I

**DEVELOPED (100-299 points):**
- 100 pts: Scout_II, Destroyer_II, Transport_II
- 150 pts: **Cruiser_II** ← Mid-tier unlock!

**ADVANCED (300-599 points):**
- 300 pts: Scout_III, Destroyer_III, Transport_III
- 400 pts: **Cruiser_III** ← Mid-tier unlock!

**SUPREME (600+ points):**
- 600 pts: Scout_IV, Destroyer_IV, Transport_IV
- 700 pts: **LtCruiser_IV** ← First capital ship!
- 850 pts: **HvyCruiser_IV** ← Ultimate ship!

## How to Set Up Ships

### Option 1: Automatic (Recommended)
1. In Unity, go to **BOTF → Ship Tech Setup Helper**
2. Click **"Auto-Setup All Ships"**
3. Tool automatically sets tech point requirements based on ship names

### Option 2: Manual
For each ShipSO:
1. Set **TechLevel** (EARLY, DEVELOPED, ADVANCED, SUPREME)
2. Set **MinTechPointsRequired** using table above
3. Ensure ship name matches convention: `CIV_SHIPTYPE_TIER(CLONE)`

## Code Examples

### Check Available Ships in Shipyard UI
```csharp
// Get ships player can build right now
var civData = CivManager.Instance.GetLocalPlayerCivController().CivData;
List<ShipSO> availableShips = TechManager.Instance.GetUnlockedShips(
    civData.CivEnum, 
    civData.TechPoints
);

// Display in shipyard
foreach (var ship in availableShips)
{
    // Create shipyard button
    Debug.Log($"Can build: {ship.ShipName}");
}
```

### Show "Coming Soon" Ships
```csharp
// Show upcoming unlocks to encourage research
var civData = CivManager.Instance.GetLocalPlayerCivController().CivData;
var allShips = ShipManager.Instance.GetShipSOListByCiv(civData.CivEnum);

var upcomingShips = allShips
    .Where(s => s.MinTechPointsRequired > civData.TechPoints && 
                s.MinTechPointsRequired <= civData.TechPoints + 100)
    .OrderBy(s => s.MinTechPointsRequired)
    .ToList();

foreach (var ship in upcomingShips)
{
    int pointsNeeded = ship.MinTechPointsRequired - civData.TechPoints;
    // Show in UI: "FED_CRUISER_II unlocks in 50 tech points!"
}
```

### Check Specific Ship Availability
```csharp
// Before allowing player to build a ship
ShipSO destroyer2 = ShipManager.Instance.GetShipSO(
    CivEnum.FED, 
    TechLevel.DEVELOPED, 
    ShipType.Destroyer
);

var civData = CivManager.Instance.GetLocalPlayerCivController().CivData;
bool canBuild = TechManager.Instance.IsShipUnlockedByPoints(
    destroyer2, 
    civData.TechPoints
);

if (!canBuild)
{
    int pointsNeeded = destroyer2.MinTechPointsRequired - civData.TechPoints;
    // Show message: "Need 25 more tech points to unlock FED_DESTROYER_II"
}
```

## Tech Point Thresholds Reference

| Unlock Event | Tech Points | Example Ship |
|--------------|-------------|--------------|
| Game Start | 0 | FED_SCOUT_I |
| First Destroyer | 25 | FED_DESTROYER_I |
| First Transport | 50 | FED_TRANSPORT_I |
| **DEVELOPED Tier** | **100** | **FED_SCOUT_II** |
| First Cruiser | 150 | FED_CRUISER_II |
| **ADVANCED Tier** | **300** | **FED_SCOUT_III** |
| Advanced Cruiser | 400 | FED_CRUISER_III |
| **SUPREME Tier** | **600** | **FED_SCOUT_IV** |
| Light Cruiser | 700 | FED_LTCRUISER_IV |
| Heavy Cruiser | 850 | FED_HVYCRUISER_IV |

## Benefits of Granular System

✅ **Constant Progression**: Players feel rewarded continuously, not just at tier transitions
✅ **Encourages Research**: More reason to build Research Centers
✅ **Strategic Depth**: Players can specialize (rush for Cruiser) or diversify
✅ **Better Pacing**: Reduces "rush to next tier" gameplay
✅ **Clearer Goals**: "50 points to Cruiser!" is more tangible than "DEVELOPED tier soon"

## Adjusting Balance

All thresholds are configurable in **TechManager** Inspector:
- `EarlyScoutUnlock` (default: 0)
- `EarlyDestroyerUnlock` (default: 25)
- `DevelopedCruiserUnlock` (default: 150)
- `SupremeLtCruiserUnlock` (default: 700)
- etc.

Change these if progression feels too slow/fast for your game.

## Testing

### Debug Commands
Add these to TechManager for testing:

```csharp
[ContextMenu("Debug: Add 100 Tech Points")]
void DebugAddPoints()
{
    var player = CivManager.Instance.GetLocalPlayerCivController();
    if (player != null)
    {
        AddResearchPoints(player.CivData, 100);
    }
}

[ContextMenu("Debug: Show All Unlocked Ships")]
void DebugShowUnlocked()
{
    var player = CivManager.Instance.GetLocalPlayerCivController();
    if (player != null)
    {
        var ships = GetUnlockedShips(player.CivData.CivEnum, player.CivData.TechPoints);
        Debug.Log($"Unlocked ships at {player.CivData.TechPoints} points:");
        foreach (var ship in ships)
        {
            Debug.Log($"  - {ship.ShipName}");
        }
    }
}
```

### In Unity Editor
1. Add TechManager to PersistentScene
2. Play game
3. Right-click TechManager component
4. Select debug command from context menu

## Documentation Files

- **`TechSystem_Implementation_Guide.md`** - Full system documentation
- **`Ship_Naming_Convention.md`** - Detailed ship naming and unlock rules (this file)

## Summary

Your tech system now supports:
- ✅ Granular unlocking based on tech points (not just levels)
- ✅ Ship naming convention recognition (`CIV_SHIPTYPE_TIER`)
- ✅ Progressive ship unlocks within each tech level
- ✅ Easy-to-use editor tool for setup
- ✅ All civilizations follow same pattern

The system is **backwards compatible** - ships without `MinTechPointsRequired` set will use old tech level-based unlocking.
