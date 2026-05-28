# BOTF3D Project Folder Structure

This document describes the new organized folder structure for improved code maintainability.

## Directory Overview

```
Assets/Script/
├── _Core/                      # Foundational architecture (interfaces, events, utilities)
│   ├── Interfaces/            # IManager, IController, IGameData
│   ├── Events/                # GameEvents - centralized event system
│   ├── Services/              # ServiceLocator - dependency injection
│   ├── Utilities/             # GameLogger and helper classes
│   └── Factories/             # Factory classes for object creation
│
├── Audio/                      # Audio management
│   ├── AudioManager.cs
│   ├── SoundData.cs
│   └── AudioSettingsUI.cs
│
├── Civilization/               # NEW - Civilization systems (renamed from CivSystems)
│   ├── Controllers/           # CivController, etc.
│   ├── Data/                  # CivData
│   ├── Diplomacy/             # DiplomacyController, DiplomacyManager, DiplomacyData
│   └── Intelligence/          # IntelligenceController, IntelligenceManager, IntelligenceData
│
├── Combat/                     # Combat systems
│   ├── Controllers/           # ShipController, CombatController
│   ├── Data/                  # CombatData, ShipData
│   ├── Managers/              # CombatManager, ShipManager, HealthBarManager
│   ├── Systems/               # Movement, Targeting, Formation systems
│   ├── Weapons/               # Weapon controllers and effects
│   └── Camera/                # Combat camera controllers
│
├── Config/                     # NEW - Configuration and settings
│   └── ScriptableObjects/     # GameConfig, balance settings
│
├── Core/                       # Core game managers (non-architecture)
│   ├── TimeManager.cs
│   ├── TechManager.cs
│   └── SceneController.cs
│
├── Data/                       # NEW - Shared data classes
│   └── Persistence/           # Save/load system
│
├── Debug/                      # Debug utilities
│
├── Galaxy/                     # NEW - Galaxy-level gameplay (renamed from Galactic)
│   ├── Fleet/                 # Fleet management
│   ├── StarSystem/            # Star system management (previously InStarSystems)
│   └── Map/                   # Galaxy map, fog of war
│
├── Galactic/                   # OLD - Will gradually migrate to Galaxy/
│
├── InStarSystems/              # OLD - Will migrate to Galaxy/StarSystem/
│
├── Mulitplayer/                # Multiplayer/networking
│
└── UI/                         # User interface
    ├── Screens/               # Full-screen UIs (Main Menu, Galaxy Screen)
    ├── Panels/                # Sub-panels and windows
    └── Widgets/               # Reusable UI components
```

## Migration Strategy

### Phase 1: Foundation (COMPLETE)
- ✅ Created _Core/ directory with interfaces, events, services
- ✅ Created GameLogger for categorized logging
- ✅ Created ServiceLocator for dependency injection
- ✅ Created GameEvents for loose coupling
- ✅ Created GameConfig ScriptableObject

### Phase 2: Organize Combat (Next)
- Move combat files into Controllers/, Data/, Managers/, Systems/ subdirectories
- Keep references intact during moves

### Phase 3: Organize Civilization
- Move CivSystems/ files to Civilization/ with proper subdirectories
- Update namespace from BOTF3D.GamePlay to BOTF3D.Civilization

### Phase 4: Organize Galaxy
- Move Galactic/ and InStarSystems/ to Galaxy/ structure
- Update namespaces

### Phase 5: Organize UI
- Sort UI files into Screens/, Panels/, Widgets/
- Update namespace to BOTF3D.UI

## Namespace Conventions

```csharp
BOTF3D.Core          // Core architecture, interfaces, managers
BOTF3D.Civilization  // Civ, diplomacy, intelligence
BOTF3D.Combat        // Combat, ships, weapons
BOTF3D.Galaxy        // Galaxy map, fleets, star systems
BOTF3D.UI            // All UI controllers
BOTF3D.Audio         // Audio management
BOTF3D.Config        // Configuration ScriptableObjects
BOTF3D.Data          // Shared data classes
BOTF3D.Utilities     // Helper/extension classes
```

## Usage Guidelines

### Creating New Managers
1. Implement `IManager` interface
2. Register with ServiceLocator in Awake()
3. Use GameLogger instead of Debug.Log
4. Fire events via GameEvents instead of direct coupling

### Creating New Controllers
1. Implement `IController` interface
2. Subscribe/unsubscribe to events in OnEnable/OnDisable
3. Use GameLogger for debugging

### Creating New Data Classes
1. Implement `IGameData` interface for serializable data
2. Keep data classes pure (no MonoBehaviour)
3. Place in appropriate Data/ subdirectory

## Benefits

- **Scalability**: Clear organization makes it easy to find and add code
- **Multiple Contributors**: Consistent structure reduces merge conflicts
- **Debugging**: GameLogger with categories makes troubleshooting easier
- **Testing**: ServiceLocator and events make code more testable
- **Maintenance**: Separation of concerns makes refactoring safer
