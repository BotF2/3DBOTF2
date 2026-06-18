# Implementation Guide - New Code Structure

This guide shows how to use the new folder structure and core systems.

## Quick Start Examples

### 1. Creating a New Manager

```csharp
using UnityEngine;
using BOTF3D.Core;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Example manager following new architecture
    /// </summary>
    public class ExampleManager : MonoBehaviour, IManager
    {
        public static ExampleManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Register with ServiceLocator for dependency injection
            ServiceLocator.Register<ExampleManager>(this);

            // Initialize
            Initialize();
        }

        public void Initialize()
        {
            GameLogger.Log(GameLogger.LogCategory.General, 
                "ExampleManager initialized", this);

            // Subscribe to events
            GameEvents.OnCombatStarted += HandleCombatStart;
        }

        public void Cleanup()
        {
            // Unsubscribe from events
            GameEvents.OnCombatStarted -= HandleCombatStart;

            // Cleanup resources
            GameLogger.Log(GameLogger.LogCategory.General, 
                "ExampleManager cleaned up", this);
        }

        private void OnDestroy()
        {
            Cleanup();
            ServiceLocator.Unregister<ExampleManager>();
        }

        private void HandleCombatStart(CombatData data)
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, 
                "Combat started event received", this);
        }
    }
}
```

### 2. Creating a New Controller

```csharp
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Example controller for a game entity
    /// </summary>
    public class ExampleController : MonoBehaviour, IController
    {
        [SerializeField] private ShipData shipData;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            // Setup logic
            GameLogger.Log(GameLogger.LogCategory.Combat, 
                $"Initialized controller for {gameObject.name}", this);

            // Subscribe to events
            GameEvents.OnShipDestroyed += HandleShipDestroyed;
        }

        private void OnEnable()
        {
            // Re-subscribe when enabled
            GameEvents.OnShipDestroyed += HandleShipDestroyed;
        }

        private void OnDisable()
        {
            // Always unsubscribe to prevent memory leaks
            GameEvents.OnShipDestroyed -= HandleShipDestroyed;
        }

        public void UpdateState()
        {
            // State update logic called each frame or as needed
        }

        private void Update()
        {
            UpdateState();
        }

        private void HandleShipDestroyed(int shipID)
        {
            if (shipData.ShipID == shipID)
            {
                GameLogger.Log(GameLogger.LogCategory.Combat, 
                    "This ship was destroyed!", this);
            }
        }
    }
}
```

### 3. Creating a New Data Class

```csharp
using System;
using UnityEngine;
using BOTF3D.Core;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Example data class for serialization
    /// Data classes should NOT inherit from MonoBehaviour
    /// </summary>
    [Serializable]
    public class ExampleData : IGameData
    {
        public int shipID;
        public string shipName;
        public float health;
        public float maxHealth;

        public void SaveState()
        {
            // Prepare for serialization
            // This is where you'd write to PlayerPrefs, JSON, or save file
            GameLogger.Log(GameLogger.LogCategory.Save, 
                $"Saving data for {shipName}");

            // Example: Save to PlayerPrefs
            PlayerPrefs.SetString($"Ship_{shipID}_Name", shipName);
            PlayerPrefs.SetFloat($"Ship_{shipID}_Health", health);
        }

        public void LoadState()
        {
            // Load from persistent storage
            GameLogger.Log(GameLogger.LogCategory.Save, 
                $"Loading data for ship {shipID}");

            // Example: Load from PlayerPrefs
            shipName = PlayerPrefs.GetString($"Ship_{shipID}_Name", "Unknown");
            health = PlayerPrefs.GetFloat($"Ship_{shipID}_Health", maxHealth);
        }

        public bool ValidateData()
        {
            // Validate data integrity
            bool isValid = health >= 0 && health <= maxHealth;

            if (!isValid)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Save, 
                    $"Invalid data for {shipName}: health={health}, max={maxHealth}");
            }

            return isValid;
        }
    }
}
```

### 4. Using GameEvents

```csharp
// Fire an event (pass IDs, not complex objects)
private void StartCombat(CombatData combatData)
{
    // ... populate combat data ...
    
    // Fire event with ID only (keeps Core layer clean)
    GameEvents.CombatStarted(combatData.CombatID);
}

// Subscribe to events in OnEnable
private void OnEnable()
{
    GameEvents.OnCombatStarted += HandleCombatStart;
    GameEvents.OnCombatEnded += HandleCombatEnd;
}

// ALWAYS unsubscribe in OnDisable
private void OnDisable()
{
    GameEvents.OnCombatStarted -= HandleCombatStart;
    GameEvents.OnCombatEnded -= HandleCombatEnd;
}

// Event handlers
private void HandleCombatStart(int combatID)
{
    // Look up data from manager using ID
    var combatManager = ServiceLocator.Get<CombatManager>();
    var combatData = combatManager.GetCombatByID(combatID);
    
    GameLogger.Log(GameLogger.LogCategory.Combat, 
        $"Combat {combatID} started!");
}

private void HandleCombatEnd(CivEnum victor)
{
    GameLogger.Log(GameLogger.LogCategory.Combat, 
        $"Combat ended, winner: {victor}");
}
```

### 5. Using ServiceLocator

```csharp
// Instead of accessing singletons directly:
// OLD WAY:
// CombatManager.Instance.DoSomething();

// NEW WAY - Use ServiceLocator:
var combatManager = ServiceLocator.Get<CombatManager>();
if (combatManager != null)
{
    combatManager.DoSomething();
}

// Or use TryGet for safer access:
if (ServiceLocator.TryGet<CombatManager>(out var manager))
{
    manager.DoSomething();
}
```

### 6. Using GameLogger

```csharp
// Regular log
GameLogger.Log(GameLogger.LogCategory.Combat, 
    "Ship firing weapons", this);

// Warning
GameLogger.LogWarning(GameLogger.LogCategory.Fleet, 
    "Fleet fuel is low", this);

// Error (always shown, even if category disabled)
GameLogger.LogError(GameLogger.LogCategory.Save, 
    "Failed to save game!", this);

// Enable/disable specific categories
GameLogger.SetCategoryEnabled(GameLogger.LogCategory.AI, false);

// Check if category is enabled
if (GameLogger.IsCategoryEnabled(GameLogger.LogCategory.Combat))
{
    // Only compute expensive debug info if logging is enabled
    string debugInfo = ComputeExpensiveDebugInfo();
    GameLogger.Log(GameLogger.LogCategory.Combat, debugInfo, this);
}
```

### 7. Using GameConfig ScriptableObject

```csharp
using UnityEngine;
using BOTF3D.Config;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private GameConfig gameConfig;

    private void Start()
    {
        // Apply config settings
        gameConfig.ApplyLogSettings();

        // Use config values
        int maxShips = gameConfig.maxShipsPerCombat;
        float damageMultiplier = gameConfig.weaponDamageMultiplier;

        GameLogger.Log(GameLogger.LogCategory.General, 
            $"Game initialized with max {maxShips} ships per combat");
    }
}
```

## Migration Path

### Updating Existing Manager Classes

Before:
```csharp
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;
    
    private void Awake()
    {
        Instance = this;
    }
}
```

After:
```csharp
public class CombatManager : MonoBehaviour, IManager
{
    public static CombatManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ServiceLocator.Register<CombatManager>(this);
        Initialize();
    }
    
    public void Initialize()
    {
        GameLogger.Log(GameLogger.LogCategory.Combat, 
            "CombatManager initialized", this);
    }
    
    public void Cleanup()
    {
        GameLogger.Log(GameLogger.LogCategory.Combat, 
            "CombatManager cleanup", this);
    }
}
```

## Best Practices

1. **Always unsubscribe from events** in OnDisable/OnDestroy
2. **Use GameLogger** instead of Debug.Log for easier debugging
3. **Register managers** with ServiceLocator for testability
4. **Fire events** instead of direct manager calls for loose coupling
5. **Keep data classes pure** - no MonoBehaviour on data classes
6. **Validate data** using IGameData.ValidateData() before saving
7. **Use namespaces** consistently based on folder location
8. **Document public APIs** with XML comments

## Common Pitfalls to Avoid

❌ **Don't** forget to unsubscribe from events (causes memory leaks)
❌ **Don't** put MonoBehaviour on data classes
❌ **Don't** access managers directly in tight loops (cache references)
❌ **Don't** fire events from Update() without throttling
❌ **Don't** use Debug.Log - use GameLogger for consistency

✅ **Do** subscribe in OnEnable, unsubscribe in OnDisable
✅ **Do** keep data and logic separate
✅ **Do** cache manager references when needed frequently
✅ **Do** use events for cross-system communication
✅ **Do** use GameLogger with appropriate categories
