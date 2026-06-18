# _Core Directory

This directory contains the foundational architecture for the BOTF3D project.

## Structure

### Interfaces/
Contains base interfaces that define contracts for core systems:
- `IManager.cs` - Interface for singleton manager classes
- `IController.cs` - Interface for entity controller classes
- `IGameData.cs` - Interface for data/serialization classes

### Events/
Contains the centralized event system:
- `GameEvents.cs` - Game-wide event definitions for loose coupling between systems

### Services/
Contains dependency injection and service location:
- `ServiceLocator.cs` - Simple DI container for accessing managers without static singletons

### Utilities/
Contains helper classes and utilities:
- `GameLogger.cs` - Category-based logging system for easier debugging

### Factories/
Contains factory classes for object instantiation:
- (Add factory classes here as needed)

## Usage Guidelines

### For Managers
Managers should implement `IManager` and register themselves with the ServiceLocator:

```csharp
public class MyManager : MonoBehaviour, IManager
{
    private void Awake()
    {
        ServiceLocator.Register<MyManager>(this);
        Initialize();
    }

    public void Initialize()
    {
        // Initialization logic
    }

    public void Cleanup()
    {
        // Cleanup logic
    }
}
```

### For Controllers
Controllers should implement `IController`:

```csharp
public class MyController : MonoBehaviour, IController
{
    public void Initialize()
    {
        // Setup logic
    }

    public void UpdateState()
    {
        // State update logic
    }
}
```

### For Logging
Use GameLogger instead of Debug.Log:

```csharp
GameLogger.Log(GameLogger.LogCategory.Combat, "Combat started", this);
GameLogger.LogWarning(GameLogger.LogCategory.Fleet, "Fleet low on fuel");
GameLogger.LogError(GameLogger.LogCategory.Save, "Save failed!");
```

### For Events
Subscribe to events in OnEnable/OnDisable:

```csharp
private void OnEnable()
{
    GameEvents.OnCombatStarted += HandleCombatStart;
}

private void OnDisable()
{
    GameEvents.OnCombatStarted -= HandleCombatStart;
}

private void HandleCombatStart(CombatData data)
{
    // Handle the event
}
```

Fire events using the static methods:

```csharp
GameEvents.CombatStarted(combatData);
```
