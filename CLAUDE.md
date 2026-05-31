# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development

There are no build scripts. Development happens entirely in the Unity Editor (Unity 6000.x — check `ProjectSettings/ProjectVersion.txt` for exact version).

- **Open project**: Unity Hub → Add from disk → select this folder. First import takes 5–15 minutes.
- **Run**: Load `PersistentScene` in the Hierarchy, then open `MainMenuScene` additive (right-click → Open Scene Additive). Press Play.
- **Script errors**: Check Unity Console first. If IntelliSense is broken in Visual Studio, go to Edit → Preferences → External Tools → Regenerate project files.
- **Hard reset**: Delete `Library/` and reopen the project in Unity Hub.

There is no automated test runner. Verification is done by entering Play Mode in the Unity Editor.

## Architecture

The project uses a strict **Manager / Controller / Data** separation:

| Role | Inherits | Responsibility |
|------|----------|----------------|
| `XxxManager` | `MonoBehaviour`, `IManager` | Singleton factory + registry; owns the list of all instances of a type |
| `XxxController` | `MonoBehaviour`, `IController` | Logic for one game object |
| `XxxData` | `IGameData` (not MonoBehaviour) | Pure serialization state; no logic |
| `XxxSO` | `ScriptableObject` | Configuration templates (ship classes, sounds, etc.) |

**Namespaces map to folders:**
- `BOTF3D.Core` → `Assets/Script/Core/` — GameManager, TimeManager, SceneController
- `BOTF3D.Combat` → `Assets/Script/Combat/` — all 3D combat
- `BOTF3D.Civilization` → `Assets/Script/Civilization/`
- `BOTF3D.Galaxy` → `Assets/Script/Galaxy/`
- `BOTF3D.UI` → `Assets/Script/UI/`
- `BOTF3D.Audio` → `Assets/Script/Audio/`

## Critical: Core Layer Dependency Rule

Files in `Assets/Script/_Core/` (interfaces, events, services, utilities) **must never import application namespaces**. The dependency flows one way only:

```
Combat / Civilization / Galaxy / UI / Audio
    ↓ can import
_Core (Interfaces, Events, Services, Utilities)
    ↓ can import
UnityEngine / System
```

If you see `using BOTF3D.Combat;` (or any other application namespace) inside `_Core/`, remove it. IDEs sometimes add these automatically — always review `using` statements when editing Core files.

## Cross-System Communication

- **Events** (`_Core/Events/GameEvents.cs`): Fire with primitive IDs, not complex objects, to keep Core clean. Subscribe in `OnEnable`, unsubscribe in `OnDisable` — skipping unsubscribe causes memory leaks.
- **ServiceLocator** (`_Core/Services/ServiceLocator.cs`): Preferred over direct singleton access (`ServiceLocator.Get<CombatManager>()`). Managers register themselves in `Awake`.
- **GameLogger** (`_Core/Utilities/GameLogger.cs`): Use this instead of `Debug.Log`. Supports per-category toggling via `GameConfig` ScriptableObject.

## Combat System

The combat system is the most complex part of the codebase. Understanding the flow requires reading across several files:

**Trigger → CombatScene flow:**
1. Galaxy scene fleet encounter → `CombatManager` queues combat
2. `CombatController` (MonoBehaviour in CombatScene) initializes all combat sub-systems
3. `WarpAnimationController` plays ship warp-in; ships land at `±200` (combat) or `±400` (transports) on the x-axis
4. `TurnBasedCombatResolver` runs 30-second turns: order selection → `AnimateShipPositioning()` → results
5. `CombatController.EndCombat()` returns control to the Galaxy scene

**Movement authority split** (this is non-obvious without reading both files):
- `ShipMovementController` owns movement for **Engage, Rush, AttackTransports**: phase-based x-axis travel (100 accel → 200 cruise+rotate → 100 decel → return)
- `CombatOrderStateMachine` (per-ship MonoBehaviour) owns movement for **Formation** (wall positioning) and **Retreat** (180° Y-turn then warp-out)

**Phase-based movement** uses `ShipPhaseTracker` (added at runtime as a MonoBehaviour component per ship). Key constants in `ShipMovementController.cs`: `ACCEL_DIST=100`, `CRUISE_DIST=200`, `DECEL_DIST=100`. The 180° rotation happens only during the cruise sub-leg; `HandleCruiseRotation` returns early during accel and decel.

**Combat uses `Time.unscaledDeltaTime` throughout** — game time is paused during combat resolution.

**Ship Y-rotation convention** (from `ShipSetupManager.SetupSingleShip`):
- Side 1 faces +X: `Quaternion.Euler(0, 90, 0)`
- Side 2 faces -X: `Quaternion.Euler(0, -90, 0)`

**Orders do not use speed multipliers** — ships always move at or below `ship.ShipData.maxWarpFactor`. Rush ships move at their own max speed; the order advantage is tactical (flanking, damage multipliers via `CombatOrderHelper`), not a speed boost.

**CombatData** (`Assets/Script/Combat/Data/CombatData.cs`) is the shared data object passed to all combat sub-systems. It holds both sides' `List<ShipController>` and their current `CombatOrders`.
