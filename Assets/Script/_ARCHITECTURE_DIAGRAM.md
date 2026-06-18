# BOTF3D Architecture Diagram

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        UNITY SCENE                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              ServiceLocator (GameObject)             │  │
│  │           Manages all Manager instances              │  │
│  └──────────────────────────────────────────────────────┘  │
│                             │                               │
│  ┌─────────────────────────┼─────────────────────────────┐ │
│  │                         │                             │ │
│  │   ┌─────────────────────▼──────────────────┐         │ │
│  │   │         MANAGERS LAYER                 │         │ │
│  │   │  (Singleton MonoBehaviours)            │         │ │
│  │   │                                        │         │ │
│  │   │  • CombatManager                      │         │ │
│  │   │  • CivManager                         │         │ │
│  │   │  • FleetManager                       │         │ │
│  │   │  • StarSysManager                     │         │ │
│  │   │  • AudioManager                       │         │ │
│  │   │  • TimeManager                        │         │ │
│  │   │                                        │         │ │
│  │   │  Registered with ServiceLocator        │         │ │
│  │   │  Implement IManager interface          │         │ │
│  │   └────────────────────────────────────────┘         │ │
│  │                         │                             │ │
│  └─────────────────────────┼─────────────────────────────┘ │
│                             │                               │
│  ┌─────────────────────────▼─────────────────────────────┐ │
│  │              EVENT BUS (GameEvents)                   │ │
│  │         Loose coupling via pub/sub pattern            │ │
│  │                                                        │ │
│  │  OnCombatStarted ◄──┬──► OnCombatEnded               │ │
│  │  OnCivCreated    ◄──┼──► OnFleetMoved                │ │
│  │  OnGameSaved     ◄──┴──► OnDiplomacyChanged          │ │
│  └────────────────────────────────────────────────────────┘ │
│                             │                               │
│  ┌─────────────────────────▼─────────────────────────────┐ │
│  │            CONTROLLERS LAYER                          │ │
│  │       (MonoBehaviour - Game Object Logic)             │ │
│  │                                                        │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌────────────┐  │ │
│  │  │ CivController│  │ShipController│  │FleetControl│  │ │
│  │  │              │  │              │  │            │  │ │
│  │  │ - Logic      │  │ - Movement   │  │ - Commands │  │ │
│  │  │ - Behavior   │  │ - Combat     │  │ - Orders   │  │ │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬─────┘  │ │
│  │         │                  │                  │        │ │
│  │         │ References       │ References       │        │ │
│  │         │                  │                  │        │ │
│  │  ┌──────▼──────────────────▼──────────────────▼─────┐ │ │
│  │  │              DATA LAYER                          │ │ │
│  │  │      (Pure C# - No MonoBehaviour)                │ │ │
│  │  │                                                   │ │ │
│  │  │  ┌──────────┐  ┌──────────┐  ┌───────────┐      │ │ │
│  │  │  │ CivData  │  │ ShipData │  │ FleetData │      │ │ │
│  │  │  │          │  │          │  │           │      │ │ │
│  │  │  │ [Serial- │  │ [Serial- │  │ [Serial-  │      │ │ │
│  │  │  │  izable] │  │  izable] │  │  izable]  │      │ │ │
│  │  │  └──────────┘  └──────────┘  └───────────┘      │ │ │
│  │  │                                                   │ │ │
│  │  │  Implements IGameData                            │ │ │
│  │  │  SaveState() / LoadState() / ValidateData()      │ │ │
│  │  └───────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              SCRIPTABLE OBJECTS                        │ │
│  │         (Configuration & Asset Data)                   │ │
│  │                                                        │ │
│  │  • GameConfig (balance, settings)                     │ │
│  │  • CivSO (civilization definitions)                   │ │
│  │  • ShipSO (ship templates)                            │ │
│  │  • SoundData (audio clips)                            │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Data Flow Example: Combat Start

```
1. User Action
   │
   ▼
2. FleetController.InitiateCombat()
   │
   ▼
3. Get CombatManager via ServiceLocator
   ServiceLocator.Get<CombatManager>()
   │
   ▼
4. CombatManager creates CombatData
   │
   ▼
5. Fire Event
   GameEvents.CombatStarted(combatData)
   │
   ├──► 6a. CombatUIManager (subscribed)
   │        └─► Show combat UI
   │
   ├──► 6b. AudioManager (subscribed)
   │        └─► Play combat music
   │
   └──► 6c. TimeManager (subscribed)
           └─► Pause galaxy time
```

## Component Communication Patterns

### ✅ GOOD - Event-Based (Loose Coupling)
```
FleetController                GameEvents               AudioManager
     │                             │                        │
     ├─ Fire: FleetMoved()────────►│                        │
     │                             ├──► OnFleetMoved ──────►│
     │                             │                    Play Sound
```

### ✅ GOOD - ServiceLocator
```
ShipController
     │
     ├─ ServiceLocator.Get<CombatManager>()
     │                     │
     ├────────────────────►│
     │◄───── manager ──────┤
     │
     └─ manager.RegisterShip(this)
```

### ❌ BAD - Direct Singleton Access
```
ShipController
     │
     └─ CombatManager.Instance.RegisterShip(this)
         ▲
         └─── Tight coupling, hard to test
```

### ❌ BAD - Cross-Manager Direct Calls
```
CombatManager ────► FleetManager.Instance.DisableFleet()
     ▲
     └─── Creates circular dependencies
```

## Class Hierarchy

```
MonoBehaviour
     │
     ├─► IManager
     │      │
     │      ├─► CombatManager
     │      ├─► CivManager
     │      ├─► FleetManager
     │      └─► AudioManager
     │
     └─► IController
            │
            ├─► ShipController
            ├─► CivController
            └─► FleetController

[No Inheritance]
     │
     └─► IGameData
            │
            ├─► CombatData
            ├─► CivData
            ├─► ShipData
            └─► FleetData

ScriptableObject
     │
     ├─► CivSO
     ├─► ShipSO
     ├─► GameConfig
     └─► SoundData
```

## Namespace Organization

```
BOTF3D
│
├─ Core
│  ├─ IManager, IController, IGameData
│  ├─ ServiceLocator
│  ├─ GameEvents
│  ├─ GameLogger
│  └─ TimeManager, SceneController
│
├─ Civilization
│  ├─ Controllers (CivController)
│  ├─ Data (CivData)
│  └─ Diplomacy (DiplomacyManager, DiplomacyData)
│
├─ Combat
│  ├─ Controllers (ShipController, CombatController)
│  ├─ Data (ShipData, CombatData)
│  ├─ Managers (CombatManager, ShipManager)
│  └─ Systems (MovementSystem, TargetingSystem)
│
├─ Galaxy
│  ├─ Fleet (FleetController, FleetManager)
│  ├─ StarSystem (StarSysManager, StarSysController)
│  └─ Map (GalaxyMap, FogOfWar)
│
├─ UI
│  ├─ Screens (MainMenuUI, GalaxyScreenUI)
│  ├─ Panels (DiplomacyPanel, FleetPanel)
│  └─ Widgets (Button, Tooltip)
│
├─ Audio
│  └─ AudioManager, SoundData
│
└─ Config
   └─ GameConfig, BalanceSettings
```

## Lifecycle Flow

```
Game Start
    │
    ▼
Awake()
    │
    ├─► ServiceLocator created
    │
    ├─► Managers Awake
    │      │
    │      ├─► Set Instance
    │      ├─► DontDestroyOnLoad
    │      ├─► Register with ServiceLocator
    │      └─► Call Initialize()
    │
    ▼
Start()
    │
    ├─► Controllers Start
    │      │
    │      ├─► Get Manager references from ServiceLocator
    │      └─► Subscribe to GameEvents
    │
    ▼
OnEnable()
    │
    └─► Subscribe to events
    
─── GAME RUNNING ───

OnDisable()
    │
    └─► Unsubscribe from events
    
OnDestroy()
    │
    ├─► Call Cleanup()
    │
    └─► Unregister from ServiceLocator
```

## Folder Structure Visual

```
Assets/Script/
│
├── _Core/                    ◄── Foundation layer
│   ├── Interfaces/          (Used by everything)
│   ├── Events/
│   ├── Services/
│   ├── Utilities/
│   └── Factories/
│
├── Config/                   ◄── Configuration layer
│   └── ScriptableObjects/   (Tuning/balance)
│
├── Data/                     ◄── Persistence layer
│   └── Persistence/         (Save/load)
│
├── Core/                     ◄── Core game systems
│   └── TimeManager, TechManager
│
├── Civilization/             ◄── Gameplay layer
├── Combat/
├── Galaxy/
│
├── UI/                       ◄── Presentation layer
│
└── Audio/                    ◄── Cross-cutting concern
```

## Testing Strategy

```
Unit Tests
    │
    ├─► Test Data Classes (pure logic)
    │   └─ No Unity dependencies
    │
    ├─► Test Managers (with ServiceLocator mock)
    │   └─ Replace dependencies with mocks
    │
    └─► Test Events
        └─ Verify pub/sub behavior

Integration Tests
    │
    └─► Test Manager + Controller interaction
        └─ Use PlayMode tests in Unity

End-to-End Tests
    │
    └─► Full gameplay scenarios
        └─ Combat start to finish
```

## Key Benefits Summary

✅ **Loose Coupling**: Events instead of direct references
✅ **Testability**: ServiceLocator enables dependency injection
✅ **Debuggability**: GameLogger with categories
✅ **Scalability**: Clear separation of concerns
✅ **Maintainability**: Easy to find and modify code
✅ **Collaboration**: Reduced merge conflicts
