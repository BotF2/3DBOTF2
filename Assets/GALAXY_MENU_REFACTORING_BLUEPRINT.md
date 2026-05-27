# GalaxyMenuUIController Refactoring Blueprint

## Overview
Refactoring GalaxyMenuUIController.cs from **2,103 lines** to **~400 lines** coordinator by extracting 5 specialized managers.

## ✅ Created Specialized Managers

### 1. **GalaxyUIStateManager.cs** (230 lines)
**Responsibility:** Menu state, transitions, visibility

**Key Methods:**
- `InitializeMenuStates()` - Setup default menu visibility
- `OpenMenu(Menu, GameObject)` - Open specific menu
- `CloseCurrentMenu()` - Close active menu
- `CloseAllMenus()` - Close all menus
- `CloseAllBackgrounds()` - Hide all background panels
- `SetClickMode(GalaxyClickMode)` - Set interaction mode
- `ResetClickMode()` - Reset to normal mode
- `SetSelectOtherButtonVisible(bool)` - Show/hide selection button
- `HideNoContactUI()` / `ShowNoContactUI()` - Diplomacy UI

**Properties:**
- `CurrentOpenMenu` (Menu enum)
- `CurrentOpenMenuObject` (GameObject)
- `CurrentClickMode` (GalaxyClickMode)

---

### 2. **GalaxyCivDisplayManager.cs** (195 lines)
**Responsibility:** Civilization-specific UI (insignias, portraits, names)

**Key Methods:**
- `LoadLocalPlayerCivilizationUI()` - Load player civ display
- `GetCivilizationShortName(CivEnum)` - Convert enum to name
- `GetInsigniaForCivilization(string)` - Get insignia sprite
- `GetRacePortraitForCivilization(string)` - Get race portrait

**Handles:**
- Insignia sprites for all 7 major civilizations
- Race portrait sprites for all 7 major civilizations
- Short name display

---

### 3. **GalaxyShipDeployManager.cs** (220 lines)
**Responsibility:** Ship deployment and transfer operations

**Key Methods:**
- `ShowShipDeployMenuForFleet(FleetController)` - Show deploy menu
- `ShowShipDeployForSystemNewFleet(StarSysController, FleetController)` - System → Fleet
- `ShowShipDeployForFleetNewFleet(FleetController, FleetController)` - Fleet → Fleet
- `HideShipDeployMenu()` - Hide deploy UI
- `SetFleetLookingForShipDeploy(FleetController)` - Track source fleet
- `SetFleetSelectedForShipDeploy(FleetController)` - Track target fleet
- `SetSystemLookingForShipDeploy(StarSysController)` - Track source system
- `SetSystemSelectedForShipDeploy(StarSysController)` - Track target system
- `BeginSetDestination(FleetController)` - Start destination selection
- `CompleteShipExchange()` - Finalize ship transfer
- `CancelShipDeploy()` - Cancel operation

**Properties:**
- `FleetLookingForShipDeploy`
- `FleetSelectedForShipDeploy`
- `StarSystLookingForShipDeploy`
- `StarSystSelectedForShipDeploy`
- `FleetLookingForShipMerge`
- `FleetSelectedForShipMerge`
- `StarSystLookingForShipMerge`
- `StarSystSelectedForShipMerge`
- `FleetLookingForDestination`

---

### 4. **GalaxyListPopulator.cs** (245 lines)
**Responsibility:** Populate and manage UI lists

**Key Methods:**
- `PopulateStarSystemsList()` - Populate systems list
- `PopulateFleetsList()` - Populate fleets list
- `PopulateDiplomacyList()` - Populate diplomacy contacts
- `ClearStarSystemsList()` - Clear systems list
- `ClearFleetsList()` - Clear fleets list
- `ClearDiplomacyList()` - Clear diplomacy list
- `ClearShipUIList()` - Clear ship UI
- `ClearAllLists()` - Clear all lists
- `RefreshAllLists()` - Repopulate all lists
- `GetStarSystemControllerByUI(GameObject)` - Lookup controller
- `GetFleetControllerByUI(GameObject)` - Lookup controller

**Manages:**
- Star system UI list
- Fleet UI list
- Diplomacy UI list
- Ship UI list

---

### 5. **GalaxyCameraManager.cs** (170 lines)
**Responsibility:** Camera setup and event system configuration

**Key Methods:**
- `InitializeGalaxyCamera()` - Find and assign camera
- `FindGalaxyCamera()` - Search for galaxy camera in scene
- `ConfigureEventSystem()` - Setup UI event system
- `DiagnoseCameraSetup()` - Debug camera state
- `SetGalaxyEventCamera(Camera)` - Assign camera

**Handles:**
- Camera finding (by name, tag, fallback to main)
- Canvas camera assignment
- EventSystem configuration
- StandaloneInputModule setup

---

## Refactored GalaxyMenuUIController Structure

### **New Structure (~400 lines):**

```csharp
public class GalaxyMenuUIController : MonoBehaviour
{
    public static GalaxyMenuUIController Instance;
    
    // SerializeField references (unchanged)
    [Header("UI References")]
    [SerializeField] private Camera galaxyEventCamera;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private GameObject sysBuildMenu;
    // ... (all existing SerializeFields remain)
    
    // Specialized managers
    private GalaxyUIStateManager uiStateManager;
    private GalaxyCivDisplayManager civDisplayManager;
    private GalaxyShipDeployManager shipDeployManager;
    private GalaxyListPopulator listPopulator;
    private GalaxyCameraManager cameraManager;
    
    // Properties for backward compatibility
    public GalaxyClickMode CurrentClickMode 
    { 
        get => uiStateManager?.CurrentClickMode ?? GalaxyClickMode.Normal;
        set => uiStateManager?.SetClickMode(value);
    }
    
    public FleetController FleetLookingForDestination 
    { 
        get => shipDeployManager?.FleetLookingForDestination;
        set => shipDeployManager.BeginSetDestination(value);
    }
    
    // ... (other properties delegate to managers)
    
    private void Awake()
    {
        // Singleton setup (unchanged)
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeManagers();
    }
    
    private void Start()
    {
        // Guard against re-initialization
        if (_isInitialized) return;
        _isInitialized = true;
        
        // Initialize UI using managers
        uiStateManager.InitializeMenuStates();
        cameraManager.InitializeGalaxyCamera();
        civDisplayManager.LoadLocalPlayerCivilizationUI();
        
        // Wire buttons
        WireButtons();
    }
    
    private void InitializeManagers()
    {
        Debug.Log("GalaxyMenuUIController: Initializing specialized managers");
        
        // Create UI state manager
        uiStateManager = new GalaxyUIStateManager(
            sysBuildMenu,
            intelMenuView,
            encyclopediaMenuView,
            habitableSysMenu,
            diplomacyNoContacts,
            sysBackground,
            fleetsBackground,
            diplomacyBackground,
            intelBackground,
            encyclopediaBackground,
            closeMenuButton,
            selectOtherSysOrFleetButtonGO
        );
        
        // Create civ display manager
        var insigniaImage = insigniaGO?.GetComponent<Image>();
        var raceImage = raceGO?.GetComponent<Image>();
        
        civDisplayManager = new GalaxyCivDisplayManager(
            insigniaImage,
            raceImage,
            civShortNameText,
            federationInsignia,
            romulanInsignia,
            klingonInsignia,
            cardassianInsignia,
            dominionInsignia,
            borgInsignia,
            terranInsignia,
            federationRace,
            romulanRace,
            klingonRace,
            cardassianRace,
            dominionRace,
            borgRace,
            terranRace
        );
        
        // Create ship deploy manager
        shipDeployManager = new GalaxyShipDeployManager(
            ShipDeployMenuUIController.Instance
        );
        
        // Create list populator
        listPopulator = new GalaxyListPopulator(
            listOfStarSysUiGos,
            listOfFleetUiGos,
            listOfDiplomacyUiGos,
            listOfSysShipUiGos,
            sysControllers,
            fleetControllers,
            diplomacyControllers,
            fleetUI_Prefab
        );
        
        // Create camera manager
        cameraManager = new GalaxyCameraManager(
            parentCanvas,
            galaxyEventCamera
        );
        
        Debug.Log("✅ GalaxyMenuUIController: All managers initialized");
    }
    
    // === Public API (delegates to managers) ===
    
    public void OpenMenu(Menu menuEnum, GameObject callingMenuOrGalaxyObject)
    {
        uiStateManager.OpenMenu(menuEnum, callingMenuOrGalaxyObject);
        
        // Populate list based on menu type
        switch (menuEnum)
        {
            case Menu.StarSys:
                listPopulator.PopulateStarSystemsList();
                break;
            case Menu.Fleet:
                listPopulator.PopulateFleetsList();
                break;
            case Menu.Diplomacy:
                listPopulator.PopulateDiplomacyList();
                break;
        }
    }
    
    public void CloseCurrentMenu()
    {
        uiStateManager.CloseCurrentMenu();
    }
    
    public void CloseAllMenus()
    {
        uiStateManager.CloseAllMenus();
        listPopulator.ClearAllLists();
    }
    
    public void ShowShipDeployMenuForFleet(FleetController fleet)
    {
        shipDeployManager.ShowShipDeployMenuForFleet(fleet);
    }
    
    public void HideShipDeployMenu()
    {
        shipDeployManager.HideShipDeployMenu();
    }
    
    public void SetClickMode(GalaxyClickMode mode)
    {
        uiStateManager.SetClickMode(mode);
        UpdateCursorForClickMode();
    }
    
    public void ResetClickMode()
    {
        uiStateManager.ResetClickMode();
        UpdateCursorForClickMode();
    }
    
    // === Button Handlers ===
    
    public void SystemButtonPressed()
    {
        OpenMenu(Menu.StarSys, null);
    }
    
    public void FleetButtonPressed()
    {
        OpenMenu(Menu.Fleet, null);
    }
    
    public void DiplomacyButtonPressed()
    {
        OpenMenu(Menu.Diplomacy, null);
    }
    
    public void IntelButtonPressed()
    {
        OpenMenu(Menu.Intel, null);
    }
    
    public void EncyclopediaButtonPressed()
    {
        OpenMenu(Menu.Encyclopedia, null);
    }
    
    // ... (other button handlers remain simple delegations)
}
```

---

## Benefits of Refactoring

### **Before:**
- 2,103 lines in single file
- Mixed concerns: UI state, data binding, list management, camera setup, ship deployment
- Difficult to test
- Hard to debug
- Tight coupling between systems

### **After:**
- **400 lines** in coordinator
- **1,060 lines** in 5 specialized managers
- Clear separation of concerns
- Each manager independently testable
- Easy to locate bugs
- Loose coupling via delegation

---

## Migration Strategy

### **Phase 1: Create Managers** ✅ COMPLETE
- [x] Create GalaxyUIStateManager
- [x] Create GalaxyCivDisplayManager
- [x] Create GalaxyShipDeployManager
- [x] Create GalaxyListPopulator
- [x] Create GalaxyCameraManager

### **Phase 2: Refactor GalaxyMenuUIController** (Next Step)
1. Add manager fields
2. Implement `InitializeManagers()`
3. Update `Start()` to use managers
4. Replace direct field access with manager properties
5. Update button handlers to delegate to managers
6. Remove duplicate code now in managers

### **Phase 3: Test & Verify**
1. Test all menu open/close operations
2. Verify ship deployment workflow
3. Check camera initialization
4. Validate list population
5. Confirm civ display loads correctly

### **Phase 4: Clean Up**
1. Remove commented-out code
2. Update documentation
3. Add XML comments to public methods
4. Verify backward compatibility

---

## Estimated Timeline

- **Phase 1:** ✅ Complete (1 hour)
- **Phase 2:** 2-3 hours
- **Phase 3:** 1-2 hours
- **Phase 4:** 30 minutes

**Total:** 4-6 hours for complete refactoring

---

## Next Steps

1. **Backup current GalaxyMenuUIController.cs**
2. **Begin Phase 2:** Refactor the controller to use managers
3. **Test incrementally:** Test after each manager integration
4. **Commit frequently:** Small commits for each manager integration

---

## Backward Compatibility

All existing code that calls `GalaxyMenuUIController.Instance` methods will continue to work. The refactoring is internal only - public API remains unchanged.

Example:
```csharp
// External code (unchanged)
GalaxyMenuUIController.Instance.OpenMenu(Menu.StarSys, null);
GalaxyMenuUIController.Instance.ShowShipDeployMenuForFleet(fleet);

// Internal implementation (delegates to managers)
public void OpenMenu(Menu menu, GameObject caller)
{
    uiStateManager.OpenMenu(menu, caller);
}
```

---

## Additional Optimizations

While refactoring, consider:

1. **Remove Debug.Log Spam:**
   - Replace verbose logs with conditional logging
   - Use `[Conditional("DEBUG_GALAXY_UI")]` attribute

2. **Cache Component References:**
   - Cache `GetComponent<>` calls in Awake()
   - Store frequently accessed components

3. **Event-Based Communication:**
   - Replace direct `Instance` calls with events
   - Example: `GalaxyMenuEvents.OnMenuOpened`

4. **Object Pooling for UI:**
   - Pool list item GameObjects
   - Reuse instead of Instantiate/Destroy

---

## Files Created

1. `Assets/Script/UI/GalaxyUIStateManager.cs`
2. `Assets/Script/UI/GalaxyCivDisplayManager.cs`
3. `Assets/Script/UI/GalaxyShipDeployManager.cs`
4. `Assets/Script/UI/GalaxyListPopulator.cs`
5. `Assets/Script/UI/GalaxyCameraManager.cs`

**Next:** Refactor `Assets/Script/UI/GalaxyMenuUIController.cs` to use these managers.
