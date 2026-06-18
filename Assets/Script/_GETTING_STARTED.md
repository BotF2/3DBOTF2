# Getting Started with New Code Structure

Welcome! This project has been reorganized for better maintainability, debugging, and team collaboration.

## What Changed?

### New Folder Structure
- **_Core/** - Foundational architecture (interfaces, events, services)
- **Civilization/** - Civilization systems (renamed from CivSystems)
- **Combat/** - Combat systems with organized subdirectories
- **Galaxy/** - Galaxy-level gameplay
- **Config/** - Configuration ScriptableObjects
- **Data/** - Shared data and persistence

### New Core Systems
1. **ServiceLocator** - Dependency injection container
2. **GameEvents** - Centralized event system
3. **GameLogger** - Category-based logging
4. **GameConfig** - ScriptableObject for settings

## Quick Reference

### Using Managers
```csharp
// Get a manager
var combatManager = ServiceLocator.Get<CombatManager>();
```

### Logging
```csharp
// Instead of Debug.Log
GameLogger.Log(GameLogger.LogCategory.Combat, "Message", this);
```

### Events
```csharp
// Fire an event
GameEvents.CombatStarted(combatData);

// Subscribe (in OnEnable)
GameEvents.OnCombatStarted += HandleCombat;

// Unsubscribe (in OnDisable) - IMPORTANT!
GameEvents.OnCombatStarted -= HandleCombat;
```

## Documentation Files

📄 **FOLDER_STRUCTURE.md** - Complete folder organization
📄 **_IMPLEMENTATION_GUIDE.md** - Code examples and patterns
📄 **_ARCHITECTURE_DIAGRAM.md** - Visual system diagrams
📄 **_Core/README.md** - Core systems usage

## Next Steps

1. Open Unity and let it regenerate .meta files for new folders
2. Review the implementation guide for code examples
3. Start using GameLogger instead of Debug.Log
4. When creating new managers, implement IManager interface
5. When creating new controllers, implement IController interface

## Migration Strategy

The old folders (CivSystems, Galactic, InStarSystems) still exist.
We'll gradually move files to the new structure:

**Phase 1 (Current):** ✅ Foundation created
**Phase 2 (Next):** Move Combat files to subdirectories
**Phase 3:** Move CivSystems to Civilization
**Phase 4:** Move Galactic/InStarSystems to Galaxy
**Phase 5:** Organize UI into Screens/Panels/Widgets

## Benefits

✅ **Easy to Find Code** - Logical folder organization
✅ **Easy to Debug** - Category-based logging
✅ **Easy to Test** - ServiceLocator enables mocking
✅ **Easy to Collaborate** - Clear structure reduces conflicts
✅ **Easy to Scale** - Loose coupling via events

## Questions?

Check the documentation files listed above or search for examples in the codebase.
