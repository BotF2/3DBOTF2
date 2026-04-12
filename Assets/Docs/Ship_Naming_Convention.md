# Ship Naming Convention & Tech Unlock System

## Overview
Ships unlock progressively based on **tech points**, not just tech levels. This allows for more granular progression within each tech level.

## Naming Convention

### Format
`CIV_SHIPTYPE_TIER(CLONE)`

Where:
- **CIV**: Civilization code (FED, ROM, KLING, CARD, DOM, BORG, TERRAN)
- **SHIPTYPE**: Ship class (SCOUT, DESTROYER, TRANSPORT, CRUISER, LTCRUISER, HVYCRUISER)
- **TIER**: Roman numeral indicating tech tier (I, II, III, IV)

### Examples
```
FED_SCOUT_I(CLONE)          → Federation Scout, Tier 1 (Early)
FED_DESTROYER_II(CLONE)     → Federation Destroyer, Tier 2 (Developed)
FED_CRUISER_III(CLONE)      → Federation Cruiser, Tier 3 (Advanced)
FED_HVYCRUISER_IV(CLONE)    → Federation Heavy Cruiser, Tier 4 (Supreme)
```

## Ship Availability by Tech Level

### EARLY (0-99 Tech Points) - Tier I (_I)
**Available Ships:**
- Scout_I (0 points)
- Destroyer_I (25 points)
- Transport_I (50 points)

**Example Names:**
```
FED_SCOUT_I(CLONE)
FED_DESTROYER_I(CLONE)
FED_TRANSPORT_I(CLONE)
```

### DEVELOPED (100-299 Tech Points) - Tier II (_II)
**Available Ships:**
- Scout_II (100 points)
- Destroyer_II (100 points)
- Transport_II (100 points)
- **Cruiser_II (150 points)** ← NEW SHIP CLASS

**Example Names:**
```
FED_SCOUT_II(CLONE)
FED_DESTROYER_II(CLONE)
FED_TRANSPORT_II(CLONE)
FED_CRUISER_II(CLONE)
```

### ADVANCED (300-599 Tech Points) - Tier III (_III)
**Available Ships:**
- Scout_III (300 points)
- Destroyer_III (300 points)
- Transport_III (300 points)
- Cruiser_III (400 points)

**Example Names:**
```
FED_SCOUT_III(CLONE)
FED_DESTROYER_III(CLONE)
FED_TRANSPORT_III(CLONE)
FED_CRUISER_III(CLONE)
```

**NOTE:** Regular Cruiser_III is still available in ADVANCED (contrary to initial assumption)

### SUPREME (600+ Tech Points) - Tier IV (_IV)
**Available Ships:**
- Scout_IV (600 points)
- Destroyer_IV (600 points)
- Transport_IV (600 points)
- **LtCruiser_IV (700 points)** ← NEW SHIP CLASS
- **HvyCruiser_IV (850 points)** ← NEW SHIP CLASS

**Example Names:**
```
FED_SCOUT_IV(CLONE)
FED_DESTROYER_IV(CLONE)
FED_TRANSPORT_IV(CLONE)
FED_LTCRUISER_IV(CLONE)
FED_HVYCRUISER_IV(CLONE)
```

**NOTE:** At SUPREME level, regular Cruiser is REPLACED by Lt and Hvy variants

## Tech Point Unlock Thresholds

### Early Tech (0-99 points)
| Ship Type | Unlock Points | Ship Name Example |
|-----------|---------------|-------------------|
| Scout     | 0             | FED_SCOUT_I       |
| Destroyer | 25            | FED_DESTROYER_I   |
| Transport | 50            | FED_TRANSPORT_I   |

### Developed Tech (100-299 points)
| Ship Type | Unlock Points | Ship Name Example |
|-----------|---------------|-------------------|
| Scout     | 100           | FED_SCOUT_II      |
| Destroyer | 100           | FED_DESTROYER_II  |
| Transport | 100           | FED_TRANSPORT_II  |
| Cruiser   | 150           | FED_CRUISER_II    |

### Advanced Tech (300-599 points)
| Ship Type | Unlock Points | Ship Name Example  |
|-----------|---------------|--------------------|
| Scout     | 300           | FED_SCOUT_III      |
| Destroyer | 300           | FED_DESTROYER_III  |
| Transport | 300           | FED_TRANSPORT_III  |
| Cruiser   | 400           | FED_CRUISER_III    |

### Supreme Tech (600+ points)
| Ship Type   | Unlock Points | Ship Name Example     |
|-------------|---------------|-----------------------|
| Scout       | 600           | FED_SCOUT_IV          |
| Destroyer   | 600           | FED_DESTROYER_IV      |
| Transport   | 600           | FED_TRANSPORT_IV      |
| LtCruiser   | 700           | FED_LTCRUISER_IV      |
| HvyCruiser  | 850           | FED_HVYCRUISER_IV     |

## How Granular Unlocking Works

### Traditional System (Old)
- Reach DEVELOPED → All Tier II ships unlock instantly
- Reach ADVANCED → All Tier III ships unlock instantly
- Feels "step-function" with sudden power spikes

### Granular System (New)
- Reach 100 points (DEVELOPED) → Scout_II, Destroyer_II, Transport_II unlock
- Reach 150 points → Cruiser_II unlocks (mid-DEVELOPED progression)
- Reach 300 points (ADVANCED) → Scout_III, Destroyer_III, Transport_III unlock
- Reach 400 points → Cruiser_III unlocks (mid-ADVANCED progression)
- Reach 600 points (SUPREME) → Basic IV ships unlock
- Reach 700 points → LtCruiser_IV unlocks
- Reach 850 points → HvyCruiser_IV unlocks

**Benefits:**
- ✅ Players feel constant progression within tech levels
- ✅ Encourages continued research investment
- ✅ Reduces "rush to next tier" gameplay
- ✅ Rewards players who invest in research centers

## Setting Up Ship Unlocks

### Automatic Setup (Recommended)
1. Open Unity Editor
2. Go to menu: **BOTF → Ship Tech Setup Helper**
3. Click **"Auto-Setup All Ships"**
4. Tool will scan all ShipSOs and set `MinTechPointsRequired` based on ship names

### Manual Setup
For each ShipSO asset:
1. Open the ShipSO in Inspector
2. Set **TechLevel** to the appropriate tier (EARLY, DEVELOPED, ADVANCED, SUPREME)
3. Set **MinTechPointsRequired** based on table above
4. Save the asset

### Example Setup for Federation Destroyer
```
FED_DESTROYER_I(CLONE):
  - ShipType: Destroyer
  - TechLevel: EARLY
  - MinTechPointsRequired: 25

FED_DESTROYER_II(CLONE):
  - ShipType: Destroyer
  - TechLevel: DEVELOPED
  - MinTechPointsRequired: 100

FED_DESTROYER_III(CLONE):
  - ShipType: Destroyer
  - TechLevel: ADVANCED
  - MinTechPointsRequired: 300

FED_DESTROYER_IV(CLONE):
  - ShipType: Destroyer
  - TechLevel: SUPREME
  - MinTechPointsRequired: 600
```

## Progression Examples

### Example 1: Early Game Federation
```
Tech Points: 0
Available Ships: FED_SCOUT_I

Tech Points: 25
Available Ships: FED_SCOUT_I, FED_DESTROYER_I

Tech Points: 50
Available Ships: FED_SCOUT_I, FED_DESTROYER_I, FED_TRANSPORT_I
```

### Example 2: Mid-Game Federation
```
Tech Points: 100 (just reached DEVELOPED)
Available Ships: All Tier I + FED_SCOUT_II, FED_DESTROYER_II, FED_TRANSPORT_II

Tech Points: 150
Available Ships: All above + FED_CRUISER_II ← NEW!

Tech Points: 200
Available Ships: All Tier I and II ships
```

### Example 3: Late Game Federation
```
Tech Points: 600 (just reached SUPREME)
Available Ships: All Tier I-III + FED_SCOUT_IV, FED_DESTROYER_IV, FED_TRANSPORT_IV

Tech Points: 700
Available Ships: All above + FED_LTCRUISER_IV ← NEW!

Tech Points: 850
Available Ships: All above + FED_HVYCRUISER_IV ← ULTIMATE SHIP!
```

## Code Usage

### Check if Ship is Unlocked
```csharp
// Get civilization's tech points
var civData = CivManager.Instance.GetCivDataByCivEnum(CivEnum.FED);
int currentTechPoints = civData.TechPoints;

// Check if specific ship is unlocked
ShipSO destroyer2 = GetShipSO("FED_DESTROYER_II");
bool isUnlocked = TechManager.Instance.IsShipUnlockedByPoints(destroyer2, currentTechPoints);
```

### Get All Unlocked Ships
```csharp
// Get all ships available to Federation at their current tech points
var civData = CivManager.Instance.GetCivDataByCivEnum(CivEnum.FED);
List<ShipSO> availableShips = TechManager.Instance.GetUnlockedShips(
    CivEnum.FED, 
    civData.TechPoints
);

// Display in shipyard UI
foreach (var ship in availableShips)
{
    Debug.Log($"Available: {ship.ShipName}");
}
```

### Show Upcoming Unlocks
```csharp
// Show player what they'll unlock soon
var civData = CivManager.Instance.GetCivDataByCivEnum(CivEnum.FED);
int currentPoints = civData.TechPoints;

var allShips = ShipManager.Instance.GetShipSOListByCiv(CivEnum.FED);
var upcomingShips = allShips
    .Where(s => s.MinTechPointsRequired > currentPoints && 
                s.MinTechPointsRequired <= currentPoints + 100)
    .OrderBy(s => s.MinTechPointsRequired)
    .ToList();

foreach (var ship in upcomingShips)
{
    int pointsNeeded = ship.MinTechPointsRequired - currentPoints;
    Debug.Log($"Coming soon: {ship.ShipName} ({pointsNeeded} points away)");
}
```

## Balancing Recommendations

### Research Center Placement
With granular unlocking, players will want to invest in research:
- **1 Research Center** = 5 points/turn → 20 turns to unlock Cruiser_II (150 points from 100)
- **3 Research Centers** = 16.5 points/turn → ~6 turns to unlock Cruiser_II
- **5 Research Centers** = 27.5 points/turn → ~4 turns to unlock Cruiser_II

### Ship Power Scaling
Ensure ships scale appropriately with their unlock points:
- **Tier I** (0-99): Basic stats
- **Tier II** (100-299): +20-30% stat increase
- **Tier III** (300-599): +50-70% stat increase
- **Tier IV** (600+): +100-150% stat increase

### Unlock Pacing
Current thresholds assume:
- **Early → Developed**: ~20-25 turns with moderate research
- **Developed → Advanced**: ~30-35 turns with good research
- **Advanced → Supreme**: ~40-50 turns with excellent research

Adjust thresholds in TechManager Inspector if progression feels too slow/fast.

## Troubleshooting

### Ships Not Unlocking
1. Check ShipSO has correct `MinTechPointsRequired` value
2. Check civilization's `TechPoints` in CivData
3. Run Ship Tech Setup Helper to verify all ships configured correctly

### Wrong Ships Appearing
1. Verify ship naming follows convention (CIV_SHIPTYPE_TIER)
2. Check ShipSO `TechLevel` matches tier in name
3. Ensure `MinTechPointsRequired` is set correctly

### Debug Commands
Add to TechManager for testing:
```csharp
[ContextMenu("Debug: Show Unlocked Ships")]
public void DebugShowUnlockedShips()
{
    var playerCiv = CivManager.Instance.GetLocalPlayerCivController();
    if (playerCiv != null)
    {
        var ships = GetUnlockedShips(playerCiv.CivData.CivEnum, playerCiv.CivData.TechPoints);
        Debug.Log($"Player has {ships.Count} ships unlocked:");
        foreach (var ship in ships)
        {
            Debug.Log($"  - {ship.ShipName} (unlocked at {ship.MinTechPointsRequired} points)");
        }
    }
}
```

## All Civilizations Follow Same Pattern

This naming convention and unlock system applies to ALL civilizations:
- Federation (FED)
- Romulans (ROM)
- Klingons (KLING)
- Cardassians (CARD)
- Dominion (DOM)
- Borg (BORG)
- Terran Empire (TERRAN)

Each has identical progression, just with their own ship models and names.
