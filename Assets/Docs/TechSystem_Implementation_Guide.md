# Technology System Implementation Guide

## Overview
The TechLevel system in BOTF3D provides civilization progression through research and technological advancement.
This system affects:
- Ship unlocking and availability
- Facility build speed and efficiency
- Power generation efficiency
- Shield strength
- Research output (recursive bonus)

## Core Components

### 1. TechManager (Assets\Script\Core\TechManager.cs)
**Singleton manager** that handles all tech-related calculations and progression.

#### Key Features:
- **Tech Point Thresholds**: Defines points needed for each tech level
- **Multiplier System**: Provides bonuses for various game systems
- **Research Generation**: Calculates research points per turn
- **Ship Unlocking**: Determines which ships are available

#### Tech Level Thresholds:
```csharp
EARLY     -> 0-99 points (starting level)
DEVELOPED -> 100-299 points
ADVANCED  -> 300-599 points
SUPREME   -> 600+ points
```

### 2. Tech Multipliers

#### Power Efficiency (Improves power generation)
- **EARLY**: 1.0x (baseline)
- **DEVELOPED**: 1.2x (+20% power)
- **ADVANCED**: 1.5x (+50% power)
- **SUPREME**: 2.0x (+100% power - antimatter)

#### Factory Speed (Reduces build time)
- **EARLY**: 1.0x (baseline)
- **DEVELOPED**: 1.15x (15% faster)
- **ADVANCED**: 1.35x (35% faster)
- **SUPREME**: 1.6x (60% faster)

#### Shipyard Speed (Reduces ship build time)
- **EARLY**: 1.0x (baseline)
- **DEVELOPED**: 1.2x (20% faster)
- **ADVANCED**: 1.4x (40% faster)
- **SUPREME**: 1.8x (80% faster)

#### Shield Strength
- **EARLY**: 1.0x (baseline)
- **DEVELOPED**: 1.25x (+25% shields)
- **ADVANCED**: 1.6x (+60% shields)
- **SUPREME**: 2.0x (+100% shields)

#### Research Output (Recursive bonus)
- **EARLY**: 1.0x (baseline)
- **DEVELOPED**: 1.1x (+10%)
- **ADVANCED**: 1.25x (+25%)
- **SUPREME**: 1.5x (+50%)

## Research Point Generation

### How It Works:
1. Each **Research Center** generates **5 base research points** per turn
2. Total research = `(Number of Research Centers) × 5 × Research Output Multiplier`
3. Research points accumulate in `CivData.TechPoints`
4. When threshold is crossed, civilization advances to next tech level

### Example:
A civilization at **DEVELOPED** tech with **3 Research Centers**:
- Base output: 3 × 5 = 15 points/turn
- Tech multiplier: 1.1x
- **Total: 16.5 points/turn**

To reach ADVANCED (300 points) from DEVELOPED (100):
- Need: 200 more points
- Time: ~12-13 turns

## Ship Unlocking System

### Ship Tech Requirements:
```csharp
Scout & Transport -> EARLY (always available)
Destroyer & Light Cruiser -> DEVELOPED
Cruiser -> ADVANCED
Heavy Cruiser -> SUPREME
```

### How to Check Ship Availability:
```csharp
// Check if civilization can build a specific ship
bool canBuild = TechManager.Instance.IsShipUnlocked(
    CivEnum.FED, 
    ShipType.Cruiser, 
    civData.TechLevel
);

// Get all available ships for a civ
List<ShipSO> availableShips = ShipManager.Instance.GetAvailableShipsForCiv(
    CivEnum.FED, 
    TechLevel.DEVELOPED
);

// Get newly unlocked ships at a specific level
List<ShipSO> newShips = ShipManager.Instance.GetNewlyUnlockedShipsAtLevel(
    CivEnum.FED, 
    TechLevel.ADVANCED
);
```

## Facility Build Time Reduction

### How It Works:
Build times are automatically reduced based on tech level in `StarSysBuildManager.GetBuildTimeDuration()`:

```csharp
// Example: Factory at DEVELOPED tech (1.15x speed)
Base build time: 100 stardates
Adjusted time: 100 / 1.15 = 87 stardates (13% faster)

// Example: Shipyard at SUPREME tech (1.8x speed)
Base build time: 150 stardates
Adjusted time: 150 / 1.8 = 83 stardates (45% faster)
```

## Integration with Existing Systems

### 1. Turn Processing
Add research point generation to your turn manager:

```csharp
public void ProcessTurn()
{
    // ... existing turn logic ...
    
    // ✅ Generate research for all civilizations
    if (TechManager.Instance != null)
    {
        TechManager.Instance.ProcessResearchForAllCivs();
    }
}
```

### 2. Power Calculation
Power output now uses tech multipliers automatically via `CivData.GetPowerTechMultiplier()`:

```csharp
// In StarSysController or power calculation
float techMultiplier = civData.GetPowerTechMultiplier();
float totalPower = numPowerPlants * basePowerPerPlant * techMultiplier;
```

### 3. Ship Building UI
When showing available ships in shipyard UI:

```csharp
// Get ships player can build
List<ShipSO> buildableShips = ShipManager.Instance.GetAvailableShipsForCiv(
    localPlayerCiv, 
    civData.TechLevel
);

// Display in UI with tech level indicators
foreach (var ship in buildableShips)
{
    bool isNewlyUnlocked = (ship.TechLevel == civData.TechLevel);
    // Show ship with "NEW!" badge if isNewlyUnlocked
}
```

## UI Display Recommendations

### Tech Progress Display
Show players their research progress:

```csharp
// Get progress to next level
float progress = TechManager.Instance.GetProgressToNextLevel(
    civData.TechPoints, 
    civData.TechLevel
);

// Display: "Research Progress: 65% to ADVANCED"
```

### Research Output Display
Show research generation:

```csharp
int researchPerTurn = TechManager.Instance.CalculateResearchPointsPerTurn(civData);
// Display: "Research: +25 points/turn"
```

### Tech Benefits Tooltip
Show what benefits a tech level provides:

```csharp
TechLevel level = TechLevel.ADVANCED;
float powerBonus = TechManager.Instance.GetPowerEfficiencyMultiplier(level);
float factoryBonus = TechManager.Instance.GetFactorySpeedMultiplier(level);
float shipyardBonus = TechManager.Instance.GetShipyardSpeedMultiplier(level);

// Display:
// "ADVANCED Technology:"
// "• Power Generation: +50%"
// "• Factory Speed: +35%"
// "• Shipyard Speed: +40%"
```

## Setup Instructions

### 1. Add TechManager to PersistentScene
1. Create empty GameObject in PersistentScene
2. Name it "TechManager"
3. Add `TechManager` component
4. Configure thresholds in Inspector (defaults are good)

### 2. Configure Starting Tech
In CivData initialization:

```csharp
// Pre-warp civs (minor races)
civData.TechPoints = 10;
civData.TechLevel = TechLevel.EARLY;

// Playable major civs (warp-capable)
civData.TechPoints = 100; // Start at DEVELOPED
civData.TechLevel = TechLevel.DEVELOPED;
```

### 3. Add Research Centers to Systems
Ensure systems have ResearchCenter facilities that can be built. These automatically contribute to research output.

## Balancing Considerations

### Tech Point Progression
- **Early Game**: Players at DEVELOPED start with moderate bonuses
- **Mid Game**: Reaching ADVANCED (300 points) requires ~15-20 turns of research
- **Late Game**: SUPREME (600 points) is a long-term goal (~40-50 turns)

### Multiplier Balance
- Multipliers are **multiplicative** (bonuses compound)
- At SUPREME tech, a civilization with 10 research centers generates:
  - Base: 10 × 5 = 50 points/turn
  - With 1.5x multiplier: **75 points/turn**
  
### Ship Unlock Balance
Ships unlocked at higher tech levels should be significantly more powerful:
- **Heavy Cruiser** (SUPREME) should justify the 600+ research point investment
- Consider adding tech-scaled stats to ShipSO (higher tech = better stats)

## Future Enhancements

### 1. Tech-Specific Bonuses
Add civilization-specific tech bonuses:

```csharp
// Example: Borg get +50% research, Federation gets +25% factory speed
public float GetCivSpecificTechBonus(CivEnum civ, TechBonusType type)
{
    switch (civ)
    {
        case CivEnum.BORG:
            return type == TechBonusType.Research ? 1.5f : 1.0f;
        case CivEnum.FED:
            return type == TechBonusType.FactorySpeed ? 1.25f : 1.0f;
        default:
            return 1.0f;
    }
}
```

### 2. Tech Trees
Replace linear progression with branching tech trees:
- Military techs (weapons, shields)
- Economic techs (factories, power)
- Science techs (research, exploration)

### 3. Tech Trading
Allow civilizations to trade tech points or tech levels through diplomacy.

### 4. Research Focus
Let players allocate research to specific areas:
- Military Research (ship unlocks faster)
- Economic Research (build speed bonuses)
- Scientific Research (faster tech progression)

## Testing Commands

### Debug Commands (add to TechManager for testing)

```csharp
[ContextMenu("Debug: Add 100 Tech Points to Player")]
public void DebugAddTechPoints()
{
    var playerCiv = CivManager.Instance.GetLocalPlayerCivData();
    if (playerCiv != null)
    {
        AddResearchPoints(playerCiv, 100);
        Debug.Log($"Added 100 points. Now at {playerCiv.TechPoints} ({playerCiv.TechLevel})");
    }
}

[ContextMenu("Debug: Advance to Next Tech Level")]
public void DebugAdvanceTechLevel()
{
    var playerCiv = CivManager.Instance.GetLocalPlayerCivData();
    if (playerCiv != null)
    {
        int needed = GetPointsNeededForNextLevel(playerCiv.TechLevel);
        playerCiv.TechPoints = needed;
        playerCiv.TechLevel = GetTechLevelFromPoints(needed);
        Debug.Log($"Advanced to {playerCiv.TechLevel}");
    }
}
```

## Summary

The TechLevel system is now fully integrated with:
✅ **Ship unlocking** - Ships available based on tech level
✅ **Facility efficiency** - Build times reduced by tech multipliers
✅ **Power generation** - Output scales with tech level
✅ **Research progression** - Points accumulate from Research Centers
✅ **Automatic advancement** - Civilizations auto-progress when thresholds reached

**Next Steps:**
1. Add TechManager to PersistentScene
2. Configure starting tech points in CivData initialization
3. Add turn processing call to generate research
4. Add UI displays for research progress
5. Test ship unlocking in shipyard UI
