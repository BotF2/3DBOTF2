# Dependency Rules - IMPORTANT

## ⚠️ Critical Architecture Rule: NO CIRCULAR DEPENDENCIES

### The Problem
Unity's auto-import feature (and some linters) will automatically add `using` statements to files. **This can break the architecture by creating circular dependencies.**

### Dependency Flow (One Direction Only)

```
┌─────────────────────────────────────────────────┐
│  APPLICATION LAYER                              │
│  Combat, Civilization, Galaxy, UI, Audio        │
│                                                  │
│  ✓ CAN import from Core                        │
│  ✓ CAN import from each other                  │
└─────────────────────────────────────────────────┘
                    │
                    │ Depends on
                    ▼
┌─────────────────────────────────────────────────┐
│  CORE LAYER                                     │
│  _Core (Interfaces, Events, Services)           │
│                                                  │
│  ✗ MUST NOT import from application layers     │
│  ✓ Only imports System, UnityEngine             │
└─────────────────────────────────────────────────┘
```

## ✅ CORRECT Dependencies

### Core Files (_Core/)
```csharp
// ✓ CORRECT - Core only imports Unity/System
using System;
using UnityEngine;
using BOTF3D.Core;  // Can import other Core files

namespace BOTF3D.Core
{
    // Core code
}
```

### Application Files (Combat/, Civilization/, etc.)
```csharp
// ✓ CORRECT - Application can import Core
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;

namespace BOTF3D.Combat
{
    // Combat code
}
```

## ❌ INCORRECT Dependencies

### Core Files with Application Imports
```csharp
// ❌ WRONG - Core importing application layers!
using BOTF3D.Combat;        // NO!
using BOTF3D.Civilization;  // NO!
using BOTF3D.Galaxy;        // NO!
using BOTF3D.UI;            // NO!
using BOTF3D.Audio;         // NO!

namespace BOTF3D.Core
{
    // This creates circular dependencies
}
```

## 🔧 How to Fix Auto-Imported Using Statements

When Unity/linter adds unwanted using statements to Core files:

1. **Identify the problem:**
   - Core files should NOT have `using BOTF3D.Combat` etc.
   
2. **Remove the unwanted using statements:**
   ```csharp
   // Remove these from Core files:
   using BOTF3D.Combat;
   using BOTF3D.Civilization;
   using BOTF3D.Galaxy;
   using BOTF3D.UI;
   using BOTF3D.Audio;
   ```

3. **Keep only valid imports:**
   ```csharp
   // These are OK in Core files:
   using System;
   using System.Collections.Generic;
   using UnityEngine;
   using BOTF3D.Core;  // Other Core files
   ```

## 📋 Checklist: Files That Must NOT Import Application Layers

### _Core/Interfaces/
- [ ] IManager.cs
- [ ] IController.cs
- [ ] IGameData.cs

### _Core/Events/
- [ ] GameEvents.cs

### _Core/Services/
- [ ] ServiceLocator.cs

### _Core/Utilities/
- [ ] GameLogger.cs
- [ ] GameEnums.cs
- [ ] Any other utility classes

### _Core/Factories/
- [ ] Any factory classes (when added)

## 🎯 Why This Matters

### Problem: Circular Dependencies
```
Core imports Combat
  ↓
Combat imports Core
  ↓
CIRCULAR DEPENDENCY = COMPILE ERROR or RUNTIME ISSUES
```

### Solution: Layered Architecture
```
Application Layer (Combat, Civilization, etc.)
  ↓ depends on
Core Layer (Interfaces, Events, Services)
  ↓ depends on
Unity/System (UnityEngine, System)
```

## 🛠️ Unity Auto-Import Settings

To prevent Unity from auto-adding unwanted using statements:

1. **Visual Studio:**
   - Tools → Options → Text Editor → C# → Advanced
   - Uncheck "Place 'System' directives first when sorting usings"
   - Uncheck "Suggest usings for types in reference assemblies"
   - Uncheck "Suggest usings for types in NuGet packages"

2. **Rider:**
   - Settings → Editor → Code Style → C#
   - Disable "Auto-import" or configure namespace rules

3. **Manual Review:**
   - Always check using statements when files are modified
   - Remove unnecessary imports from Core files

## 🔍 How to Detect Issues

### Git Diff Check
```bash
# Check if Core files have been modified
git diff Assets/Script/_Core/
```

### Look for these patterns in Core files:
```csharp
// ❌ RED FLAG in any _Core/ file:
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;
```

## 📝 When Adding New Core Files

Every new file in `_Core/` should:

1. **Only import:**
   - `System` namespaces
   - `UnityEngine` (if needed)
   - `BOTF3D.Core` (other core files)

2. **Never import:**
   - `BOTF3D.Combat`
   - `BOTF3D.Civilization`
   - `BOTF3D.Galaxy`
   - `BOTF3D.UI`
   - `BOTF3D.Audio`
   - Any other application-layer namespace

## ✅ Review Checklist

Before committing changes to Core files:

- [ ] Check all using statements at the top
- [ ] Remove any application layer imports
- [ ] Verify file compiles without errors
- [ ] Run git diff to see what changed
- [ ] Confirm no circular dependencies

## 🚨 Common Mistakes

### Mistake 1: Letting IDE Auto-Add Imports
**Problem:** IDE sees `CombatData` and adds `using BOTF3D.Combat;`
**Solution:** Manually remove it, don't rely on auto-import

### Mistake 2: Copying Code Between Layers
**Problem:** Copy from Combat file to Core file, brings imports
**Solution:** Always review and clean using statements

### Mistake 3: "It Compiles, So It's Fine"
**Problem:** May compile but creates tight coupling
**Solution:** Follow dependency rules even if it compiles

## 📚 Summary

**GOLDEN RULE:** Core layer files must never depend on application layer files.

If you see `using BOTF3D.Combat` (or similar) in any `_Core/` file, **remove it immediately**.

This keeps the architecture clean, testable, and maintainable.
