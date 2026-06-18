# Compilation Error Fix

## Issue
After removing circular dependencies from Core files, compilation errors occurred:

```
error CS0246: The type or namespace name 'CombatData' could not be found
error CS0246: The type or namespace name 'CivController' could not be found
```

## Root Cause
The problem was **events trying to pass complex types between layers**, which creates dependencies.

### The Dilemma
```
Option 1: Add using BOTF3D.Combat to GameEvents
  ❌ Creates circular dependency (Core → Combat)

Option 2: Move CombatData to Core
  ❌ Pollutes Core with application-specific types

Option 3: Use primitive types in events ✅
  ✓ Keeps Core clean
  ✓ No circular dependencies
  ✓ Listeners look up data by ID
```

## Solution Applied

### GameEvents.cs
Changed from passing complex objects to passing IDs:

**Before (WRONG):**
```csharp
public static event Action<CombatData> OnCombatStarted;
public static void CombatStarted(CombatData data) => OnCombatStarted?.Invoke(data);
```

**After (CORRECT):**
```csharp
// Pass ID instead of object - listeners can look up data from manager
public static event Action<int> OnCombatStarted; // combatID
public static void CombatStarted(int combatID) => OnCombatStarted?.Invoke(combatID);
```

### CombatData.cs
Added missing namespace for CivController:

**Before:**
```csharp
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
```

**After:**
```csharp
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;  // Added for CivController
```

## Event Pattern: Pass IDs, Not Objects

### ✅ CORRECT Pattern
```csharp
// In GameEvents.cs (Core layer)
public static event Action<int> OnCombatStarted; // Pass ID only
public static void CombatStarted(int combatID) => OnCombatStarted?.Invoke(combatID);

// In CombatManager.cs (Application layer)
public void StartCombat(CombatData data)
{
    // Fire event with ID
    GameEvents.CombatStarted(data.CombatID);
}

// In Listener (Application layer)
private void OnEnable()
{
    GameEvents.OnCombatStarted += HandleCombatStart;
}

private void HandleCombatStart(int combatID)
{
    // Look up the data from manager using ID
    var combatManager = ServiceLocator.Get<CombatManager>();
    var combatData = combatManager.GetCombatDataByID(combatID);
    
    // Now do something with the data
}
```

### ❌ WRONG Pattern
```csharp
// Don't pass complex types through Core events
public static event Action<CombatData> OnCombatStarted; // ❌ Creates dependency
```

## Benefits of ID-Based Events

1. **No Circular Dependencies**
   - Core doesn't need to know about CombatData
   - Core only deals with primitive types (int, string, enums)

2. **Loose Coupling**
   - Listeners decide what data they need
   - Can look up from different sources

3. **Performance**
   - Passing int is cheaper than passing complex objects
   - Less memory pressure

4. **Flexibility**
   - Easy to add new event listeners
   - Listeners can ignore events they don't care about

## Guidelines for Core Events

### ✅ Safe Types to Use in Core Events
- Primitive types: `int`, `float`, `bool`, `string`
- Core enums: `CivEnum` (defined in BOTF3D.Core)
- System types: `Vector3`, `DateTime`
- Generic events: `Action<T>` where T is ID

### ❌ Never Use in Core Events
- Application layer classes: `CombatData`, `ShipController`, `FleetData`
- MonoBehaviour references: `GameObject`, custom controllers
- Complex data structures from application layers

## Updated Event Signatures

```csharp
// Combat Events
public static event Action<int> OnCombatStarted;      // combatID
public static event Action<CivEnum> OnCombatEnded;    // victor
public static event Action<int> OnShipDestroyed;      // shipID

// Civilization Events
public static event Action<CivEnum> OnCivCreated;     // civ enum
public static event Action<CivEnum, CivEnum, DiplomaticState> OnDiplomacyChanged;
public static event Action<CivEnum> OnCivEliminated;  // civ enum

// Galaxy Events
public static event Action<string, CivEnum> OnSystemOwnershipChanged; // systemName, newOwner
public static event Action<int> OnFleetMoved;         // fleetID

// Game State Events
public static event Action OnGameSaved;
public static event Action OnGameLoaded;
public static event Action<int> OnNewTurn;            // turnNumber
```

## Example Usage

### Firing an Event
```csharp
// In CombatManager
public void InitiateCombat(CombatData combatData)
{
    // ... combat setup ...
    
    // Fire event with just the ID
    GameEvents.CombatStarted(combatData.CombatID);
}
```

### Listening to an Event
```csharp
// In any listener class
private CombatManager combatManager;

private void Start()
{
    combatManager = ServiceLocator.Get<CombatManager>();
}

private void OnEnable()
{
    GameEvents.OnCombatStarted += HandleCombatStarted;
}

private void OnDisable()
{
    GameEvents.OnCombatStarted -= HandleCombatStarted;
}

private void HandleCombatStarted(int combatID)
{
    // Look up full data if needed
    var combatData = combatManager.GetCombatByID(combatID);
    
    GameLogger.Log(GameLogger.LogCategory.Combat, 
        $"Combat {combatID} started between {combatData.CivEnumSideOne} and {combatData.CivEnumSideTwo}");
    
    // Handle the event...
}
```

## Files Modified

1. ✅ `_Core/Events/GameEvents.cs` - Changed to pass IDs instead of objects
2. ✅ `Combat/Data/CombatData.cs` - Added using BOTF3D.Civilization

## Compilation Status

✅ All errors resolved
✅ No circular dependencies
✅ Core layer remains clean
✅ Events system fully functional

## Key Takeaway

**Events in the Core layer should pass identifiers (IDs, enums, names), not complex objects.**

This keeps the architecture clean while still providing powerful event-driven communication.
