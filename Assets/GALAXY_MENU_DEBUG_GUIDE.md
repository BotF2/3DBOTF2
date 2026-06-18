# Galaxy Menu Debug Guide

## Architecture Overview

### Two Separate Object Types:

1. **3D Galaxy Objects** (The visual stars/fleets in 3D space)
   - Located in the galaxy scene
   - Have MeshRenderer, Collider components
   - Have StarSysController or FleetController components
   - These handle mouse clicks (OnMouseDown)
   - These should NEVER be deactivated during normal menu operations

2. **UI Panel GameObjects** (The info panels shown in menus)
   - Located in Canvas hierarchy (UI layer 5)
   - Created from sysUIPrefab / fleetUIPrefab
   - Referenced by StarSysController.StarSysUIGameObject / FleetController.FleetUIGameObject
   - These get moved between containers and activated/deactivated

### UI Panel Lifecycle:

1. **Creation**: Instantiated inactive, parented to home storage (StarSysUI_ListContainer / FleetUI_ListContainer)
2. **List View**: Moved to SysListContainer / FleetListContainer, activated
3. **Detail View**: Moved to ASystemMenuView / AFleetMenuView, activated
4. **Storage**: Moved back to home storage, **deactivated**

## Current Issue

User reports: "Star systems/fleets disappear from galaxy map when switching between menus"

### Hypothesis 1: 3D Objects Being Deactivated
❌ UNLIKELY - UI GameObjects are separate from 3D objects

### Hypothesis 2: Click Detection Broken
❓ POSSIBLE - Maybe raycasts or colliders are being affected?

### Hypothesis 3: Camera/View Issue
❓ POSSIBLE - Maybe camera is moving or something is blocking the view?

### Hypothesis 4: Wrong Objects Being Moved
✅ LIKELY - Maybe we're accidentally moving/deactivating 3D objects instead of UI panels?

## Debug Steps

1. Add logging to OnMouseDown in StarSysController to see if clicks are being detected
2. Check if 3D GameObjects are actually being deactivated (they shouldn't be)
3. Verify that StarSysUIGameObject is NOT a child of the 3D star GameObject
4. Check if colliders on 3D objects are being disabled somehow

## Code Flow: Switching from System to Fleet

```
User clicks fleet in galaxy
  → FleetController.OnMouseDown()
  → GalaxyMenuUIController.OpenMenu(Menu.AFleetMenu, fleet.gameObject)
  → CloseCurrentMenu() // Was Menu.ASystemMenu
    → HideMenuViews(Menu.ASystemMenu)
      → starSysMenuUIController.HideA_SystemMenuView()
        → (Should just hide the view, not deactivate UIs)
  → Open Menu.AFleetMenu
    → fleetMenuUIController.ShowA_FleetMenuView()
    → fleetMenuUIController.SetActiveSetParentUIGO(fleetCon)
```

## Expected Behavior

- 3D star objects should ALWAYS remain visible and clickable in galaxy
- UI panels should move between containers as needed
- Only UI panels should be activated/deactivated, never 3D objects

## Files to Check

- StarSysController.cs - Check if StarSysUIGameObject could be the 3D object itself
- StarSysManager.cs - Check InstantiateStarSysUI to verify it creates separate UI
- StarSysMenuUIController.cs - Check MoveBackAnyStarSysUIGO doesn't touch 3D objects
- FleetMenuUIController.cs - Same for fleets

