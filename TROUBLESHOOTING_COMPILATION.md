# Troubleshooting Compilation Errors

## Quick Fix Steps

### 1. Check Unity Console
Open Unity Editor and look at the **Console** tab (Window > General > Console).
- Red error messages = compilation errors
- Yellow warnings = safe to ignore (for now)

### 2. Force Recompile
```
Unity Editor → Assets → Refresh (or Ctrl+R)
```

### 3. Common Issues & Fixes

#### Error: "The type or namespace name 'CombatRecorder' could not be found"
**Fix:**
1. Right-click `Assets/Script/Combat/Testing/` → Reimport
2. Assets → Refresh

#### Error: "The type or namespace name 'GameLogger' could not be found"
**Fix:**
- Verify `Assets/Script/_Core/Utilities/GameLogger.cs` exists
- Check that `using BOTF3D.Core;` is at the top of the file

#### Error: "CivEnum does not contain a definition for 'Federation'"
**Fix:** Already fixed! The enum uses abbreviations:
- ❌ `CivEnum.Federation`
- ✅ `CivEnum.FED`
- ❌ `CivEnum.Klingon`
- ✅ `CivEnum.KLING`

#### Error: "Assembly 'BOTF3D.Combat' not found"
**Fix:**
- Your project doesn't use assembly definitions for game code
- `Assets/Tests/Tests.asmdef` has been updated to use `autoReferenced: true`
- Right-click `Assets/Tests/` → Reimport

#### Error: "MonoBehaviour already has an AddComponent method"
**Cause:** Conflict with existing method
**Fix:** This shouldn't happen, but if it does:
- Check for duplicate class names
- Verify namespace declarations

### 4. Check Modified Files

Only 2 files were modified. Verify they compile:

**CombatController.cs:**
```csharp
// Around line 74-75, should see:
private BOTF3D.Combat.Testing.CombatRecorder combatRecorder;
private BOTF3D.Combat.Debug.CombatDebugUI combatDebugUI;

// Around line 106-113, should see:
combatRecorder = gameObject.AddComponent<BOTF3D.Combat.Testing.CombatRecorder>();
```

**TurnBasedCombatResolver.cs:**
```csharp
// Around line 535, should see:
var recorder = combatController.GetComponent<BOTF3D.Combat.Testing.CombatRecorder>();
```

### 5. Verify New Files Exist

Run this checklist in Unity Project window:

- [ ] `Assets/Script/Combat/Testing/CombatRecorder.cs`
- [ ] `Assets/Script/Combat/Testing/CombatTestingHelper.cs`
- [ ] `Assets/Script/Combat/Debug/CombatDebugUI.cs`
- [ ] `Assets/Editor/CombatScenarioEditor.cs`
- [ ] `Assets/Editor/CombatDebugMenu.cs`
- [ ] `Assets/Tests/CombatOrderTests.cs`
- [ ] `Assets/Tests/Tests.asmdef`

### 6. Check for Syntax Errors

Open each new file and look for red squiggles in your IDE (Visual Studio, Rider, etc.).

Common issues:
- Missing semicolons
- Unclosed braces `{}`
- Missing `using` statements

### 7. Clear Unity Cache (Nuclear Option)

If nothing else works:
```
1. Close Unity
2. Delete these folders:
   - <ProjectRoot>/Library/
   - <ProjectRoot>/Temp/
3. Reopen project in Unity Hub
4. Wait 5-15 minutes for reimport
```

---

## Specific Error Messages

### "CS0246: The type or namespace name 'X' could not be found"

**Cause:** Missing reference or wrong namespace

**Check:**
1. Is the file in the correct folder?
   - Testing scripts → `Assets/Script/Combat/Testing/`
   - Debug scripts → `Assets/Script/Combat/Debug/`
   - Editor scripts → `Assets/Editor/`

2. Does the namespace match?
   ```csharp
   // CombatRecorder.cs should have:
   namespace BOTF3D.Combat.Testing { ... }
   
   // CombatDebugUI.cs should have:
   namespace BOTF3D.Combat.Debug { ... }
   ```

3. Is the using statement correct?
   ```csharp
   using BOTF3D.Combat.Testing;
   using BOTF3D.Combat.Debug;
   ```

### "CS0229: Ambiguity between 'X' and 'Y'"

**Cause:** Two classes with the same name in different namespaces

**Fix:** Use fully qualified names:
```csharp
// Instead of:
var recorder = GetComponent<CombatRecorder>();

// Use:
var recorder = GetComponent<BOTF3D.Combat.Testing.CombatRecorder>();
```

### "CS0103: The name 'GameLogger' does not exist in the current context"

**Fix:** Add using statement:
```csharp
using BOTF3D.Core;
```

---

## Verification Commands

### In Unity Editor

**Menu: BOTF > Check Compilation**
- Runs: `Assets → Refresh`
- Check console after

**Menu: Window > General > Test Runner**
- Click "EditMode" tab
- Should show test categories
- If empty, right-click `Assets/Tests/` → Reimport

### In Console

After opening Unity, you should see:
```
✅ CombatRecorder.cs compiled
✅ CombatDebugUI.cs compiled
✅ CombatTestingHelper.cs compiled
✅ CombatScenarioEditor.cs compiled
✅ CombatDebugMenu.cs compiled
✅ CombatOrderTests.cs compiled
```

If you see red error messages instead, those are the compilation errors.

---

## Still Having Issues?

### Get Detailed Error Info

1. Open Unity Console
2. Click on the **red error message**
3. It will show:
   - File name
   - Line number
   - Exact error message
4. Take a screenshot or copy the exact error text

### Common Patterns

**If ALL new files fail to compile:**
- Check Unity version (should be Unity 6000.x)
- Check that TextMeshPro is imported (Package Manager)
- Try closing and reopening Unity

**If ONLY test files fail:**
- Issue with `Tests.asmdef`
- Right-click `Assets/Tests/` → Reimport
- Check Unity Test Framework is installed (Package Manager)

**If ONLY editor files fail:**
- Issue with UnityEditor namespace
- Verify files are in `Assets/Editor/` folder
- Check for `using UnityEditor;` at top

---

## Expected Console Output (Success)

When everything compiles successfully, starting a combat should show:

```
╔════════════════════════════════════════════════════════╗
║         COMBAT DEBUG TOOLS ACTIVE                      ║
╠════════════════════════════════════════════════════════╣
║ • Press F1 → Toggle Debug UI                          ║
║ • Combat automatically recorded to JSON               ║
...
╚════════════════════════════════════════════════════════╝

🐛 Debug tools initialized (F1 for debug UI)
📹 CombatRecorder initialized: combat_<id>
🐛 CombatDebugUI initialized
```

If you see this, everything is working!

---

## Contact Info

If you've tried everything above and still have errors, please provide:
1. Unity version
2. Exact error message from console
3. File name and line number of the error
4. Screenshot of the error

Last Updated: 2026-06-01
