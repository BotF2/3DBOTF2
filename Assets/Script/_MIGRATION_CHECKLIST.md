# Migration Checklist

Use this checklist to gradually adopt the new code structure in your project.

## Phase 1: Foundation ✅ COMPLETE

- [x] Create folder structure
- [x] Create core interfaces (IManager, IController, IGameData)
- [x] Create GameEvents system
- [x] Create GameLogger utility
- [x] Create ServiceLocator
- [x] Create GameConfig ScriptableObject
- [x] Write documentation

## Phase 2: Immediate Adoption (Do This Now)

### Start Using New Systems

- [ ] Add ServiceLocator GameObject to PersistentScene
- [ ] Create a GameConfig asset (Right-click > Create > BOTF3D > Game Config)
- [ ] Replace all `Debug.Log` with `GameLogger.Log` in new code
- [ ] Start using events for new features instead of direct manager calls

### Update Existing Managers (One at a time)

For each manager (CombatManager, CivManager, etc.):

- [ ] **CombatManager**
  - [ ] Implement IManager interface
  - [ ] Register with ServiceLocator in Awake()
  - [ ] Replace Debug.Log with GameLogger
  - [ ] Add Cleanup() method
  
- [ ] **CivManager**
  - [ ] Implement IManager interface
  - [ ] Register with ServiceLocator in Awake()
  - [ ] Replace Debug.Log with GameLogger
  - [ ] Add Cleanup() method
  
- [ ] **FleetManager**
  - [ ] Implement IManager interface
  - [ ] Register with ServiceLocator in Awake()
  - [ ] Replace Debug.Log with GameLogger
  - [ ] Add Cleanup() method
  
- [ ] **StarSysManager**
  - [ ] Implement IManager interface
  - [ ] Register with ServiceLocator in Awake()
  - [ ] Replace Debug.Log with GameLogger
  - [ ] Add Cleanup() method
  
- [ ] **AudioManager**
  - [ ] Implement IManager interface
  - [ ] Register with ServiceLocator in Awake()
  - [ ] Replace Debug.Log with GameLogger
  - [ ] Add Cleanup() method

## Phase 3: Organize Combat Files

Move Combat files to appropriate subdirectories:

### Controllers
- [ ] Move ShipController.cs → Combat/Controllers/
- [ ] Move CombatController.cs → Combat/Controllers/
- [ ] Move ShipMovementController.cs → Combat/Controllers/
- [ ] Update using statements in moved files

### Data
- [ ] Move CombatData.cs → Combat/Data/
- [ ] Move ShipData.cs → Combat/Data/
- [ ] Update using statements in moved files

### Managers
- [ ] Move CombatManager.cs → Combat/Managers/
- [ ] Move ShipManager.cs → Combat/Managers/
- [ ] Move CombatQueueManager.cs → Combat/Managers/
- [ ] Move HealthBarManager.cs → Combat/Managers/
- [ ] Move ShipGroupManager.cs → Combat/Managers/
- [ ] Move ShipFormationManager.cs → Combat/Managers/
- [ ] Update using statements in moved files

### Systems
- [ ] Move CombatTargetingSystem.cs → Combat/Systems/
- [ ] Move CombatOrderStateMachine.cs → Combat/Systems/
- [ ] Update using statements in moved files

### Weapons
- [ ] Move BeamWeapon.cs → Combat/Weapons/
- [ ] Move torpedo/projectile scripts → Combat/Weapons/
- [ ] Update using statements in moved files

## Phase 4: Organize Civilization Files

Move CivSystems files to Civilization:

### Controllers
- [ ] Move CivController.cs → Civilization/Controllers/
- [ ] Update namespace to BOTF3D.Civilization
- [ ] Update using statements

### Data
- [ ] Move CivData.cs → Civilization/Data/
- [ ] Move CivSO.cs → Civilization/Data/
- [ ] Update namespace to BOTF3D.Civilization
- [ ] Update using statements

### Diplomacy
- [ ] Move DiplomacyController.cs → Civilization/Diplomacy/
- [ ] Move DiplomacyManager.cs → Civilization/Diplomacy/
- [ ] Move DiplomacyData.cs → Civilization/Diplomacy/
- [ ] Update namespace to BOTF3D.Civilization.Diplomacy
- [ ] Update using statements

### Intelligence
- [ ] Move IntelligenceController.cs → Civilization/Intelligence/
- [ ] Move IntelligenceManager.cs → Civilization/Intelligence/
- [ ] Move IntelligenceData.cs → Civilization/Intelligence/
- [ ] Update namespace to BOTF3D.Civilization.Intelligence
- [ ] Update using statements

## Phase 5: Organize Galaxy Files

Move Galactic and InStarSystems files:

### Fleet
- [ ] Move FleetController.cs → Galaxy/Fleet/
- [ ] Move FleetManager.cs → Galaxy/Fleet/
- [ ] Move FleetData.cs → Galaxy/Fleet/
- [ ] Update namespace to BOTF3D.Galaxy.Fleet

### StarSystem
- [ ] Move StarSysController.cs → Galaxy/StarSystem/
- [ ] Move StarSysManager.cs → Galaxy/StarSystem/
- [ ] Move StarSysData.cs → Galaxy/StarSystem/
- [ ] Move InStarSystems/* → Galaxy/StarSystem/Buildings/
- [ ] Update namespace to BOTF3D.Galaxy.StarSystem

### Map
- [ ] Move GalaxyMap files → Galaxy/Map/
- [ ] Move FogOfWar/* → Galaxy/Map/FogOfWar/
- [ ] Update namespace to BOTF3D.Galaxy.Map

## Phase 6: Organize UI Files

Sort UI files into subdirectories:

### Screens (Full-screen UIs)
- [ ] Move MainMenuUIController.cs → UI/Screens/
- [ ] Move GalaxyMenuUIController.cs → UI/Screens/
- [ ] Move CombatUIManager.cs → UI/Screens/
- [ ] Update namespace to BOTF3D.UI.Screens

### Panels (Sub-windows)
- [ ] Move DiplomacyMenuUIController.cs → UI/Panels/
- [ ] Move FleetMenuUIController.cs → UI/Panels/
- [ ] Move StarSysMenuUIController.cs → UI/Panels/
- [ ] Move ShipDeployMenuUIController.cs → UI/Panels/
- [ ] Update namespace to BOTF3D.UI.Panels

### Widgets (Reusable components)
- [ ] Move StardateUIController.cs → UI/Widgets/
- [ ] Move tooltip scripts → UI/Widgets/
- [ ] Update namespace to BOTF3D.UI.Widgets

## Phase 7: Add Events to Key Systems

Replace direct manager calls with events:

### Combat Events
- [ ] Fire OnCombatStarted when combat begins
- [ ] Fire OnCombatEnded when combat finishes
- [ ] Fire OnShipDestroyed when ships are destroyed
- [ ] Subscribe to these events in relevant managers

### Civilization Events
- [ ] Fire OnCivCreated when civs are instantiated
- [ ] Fire OnDiplomacyChanged when relations change
- [ ] Fire OnCivEliminated when civs are defeated
- [ ] Subscribe to these events in relevant managers

### Galaxy Events
- [ ] Fire OnSystemOwnershipChanged when systems change hands
- [ ] Fire OnFleetMoved when fleets relocate
- [ ] Subscribe to these events in relevant managers

### Game State Events
- [ ] Fire OnGameSaved when saving
- [ ] Fire OnGameLoaded when loading
- [ ] Fire OnNewTurn when turn advances
- [ ] Subscribe to these events in relevant managers

## Phase 8: Testing & Validation

- [ ] Test all managers still work correctly
- [ ] Test event subscription/unsubscription
- [ ] Test ServiceLocator access
- [ ] Verify no null reference errors
- [ ] Check console for GameLogger output
- [ ] Test save/load functionality
- [ ] Run through full gameplay loop

## Phase 9: Cleanup

- [ ] Remove old Debug.Log statements
- [ ] Remove direct .Instance calls where possible
- [ ] Delete empty old directories
- [ ] Update CLAUDE.md with new structure
- [ ] Update README.md with new structure

## Phase 10: Advanced Features (Future)

- [ ] Add unit tests for data classes
- [ ] Create factory classes for complex instantiation
- [ ] Add object pooling for frequently spawned objects
- [ ] Create additional ScriptableObject configs
- [ ] Implement save/load system using IGameData
- [ ] Add event debugging tools

## Tips for Migration

1. **Go Slowly** - Migrate one system at a time
2. **Test Often** - Test after each move
3. **Keep Notes** - Document any issues you encounter
4. **Use Git** - Commit after each successful phase
5. **Ask for Help** - Check documentation when stuck

## Common Issues & Solutions

### Issue: Missing References After Move
**Solution**: Unity will auto-update references in scenes/prefabs, but you may need to reimport assets (Right-click folder > Reimport)

### Issue: Namespace Errors
**Solution**: Add `using BOTF3D.Core;` to files using new systems

### Issue: ServiceLocator Returns Null
**Solution**: Make sure ServiceLocator GameObject exists in scene and manager is registered in Awake()

### Issue: Events Not Firing
**Solution**: Check that you're using GameEvents.EventName(data) not OnEventName?.Invoke(data)

### Issue: Logs Not Showing
**Solution**: Check GameLogger category is enabled: `GameLogger.SetCategoryEnabled(category, true)`

## Progress Tracking

Mark dates when you complete each phase:

- Phase 1: ✅ [Date: ____________]
- Phase 2: [ ] [Date: ____________]
- Phase 3: [ ] [Date: ____________]
- Phase 4: [ ] [Date: ____________]
- Phase 5: [ ] [Date: ____________]
- Phase 6: [ ] [Date: ____________]
- Phase 7: [ ] [Date: ____________]
- Phase 8: [ ] [Date: ____________]
- Phase 9: [ ] [Date: ____________]
- Phase 10: [ ] [Date: ____________]
