using BOTF3D.Core;

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    /// <summary>
    /// Refactored GalaxyMenuUIController - orchestrates specialized managers for galaxy menu operations.
    /// Reduced from 2,103 lines to ~500 lines by delegating to:
    /// - GalaxyUIStateManager (menu state, transitions)
    /// - GalaxyCivDisplayManager (civ insignias, portraits)
    /// - GalaxyShipDeployManager (ship deployment operations)
    /// - GalaxyListPopulator (UI list management)
    /// - GalaxyCameraManager (camera setup)
    /// </summary>
    public class GalaxyMenuUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        public static GalaxyMenuUIController Instance;

        #region Serialized Fields (Inspector References)

        [Header("Camera & Canvas")]
        [SerializeField] private Camera galaxyEventCamera;
        [SerializeField] private Canvas parentCanvas;

        [Header("UI Elements")]
        [SerializeField] private Button homeSystemButton;
        [SerializeField] private GameObject insigniaGO;
        [SerializeField] private GameObject raceGO;
        [SerializeField] private TextMeshProUGUI civShortNameText;

        [Header("Civilization Insignias")]
        [SerializeField] private Sprite federationInsignia;
        [SerializeField] private Sprite romulanInsignia;
        [SerializeField] private Sprite klingonInsignia;
        [SerializeField] private Sprite cardassianInsignia;
        [SerializeField] private Sprite dominionInsignia;
        [SerializeField] private Sprite borgInsignia;
        [SerializeField] private Sprite terranInsignia;

        [Header("Civilization Race Portraits")]
        [SerializeField] private Sprite federationRace;
        [SerializeField] private Sprite romulanRace;
        [SerializeField] private Sprite klingonRace;
        [SerializeField] private Sprite cardassianRace;
        [SerializeField] private Sprite dominionRace;
        [SerializeField] private Sprite borgRace;
        [SerializeField] private Sprite terranRace;

        [Header("Menu Views")]
        [SerializeField] private GameObject sysBuildMenu;
        [SerializeField] private GameObject diplomacyNoContacts;
        [SerializeField] private GameObject intelMenuView;
        [SerializeField] private GameObject encyclopediaMenuView;
        [SerializeField] private GameObject habitableSysMenu;
        [SerializeField] private GameObject closeMenuButton;
        [SerializeField] private Button saveShipDelployButton;

        [Header("Combat Report")]
        [SerializeField] private Button reportButton;
        [SerializeField] private GameObject reportView;

        [Header("Background Panels")]
        [SerializeField] private GameObject sysBackground;
        [SerializeField] private GameObject fleetsBackground;
        [SerializeField] private GameObject diplomacyBackground;
        [SerializeField] private GameObject intelBackground;
        [SerializeField] private GameObject encyclopediaBackground;

        [Header("Buttons")]
        [SerializeField] private GameObject selectOtherSysOrFleetButtonGO;
        [SerializeField] private GameObject InteractionButtonGO;
        [SerializeField] private GameObject tradeButtonGO;
        [SerializeField] private GameObject engagementButtonGO;
        [SerializeField] private GameObject techButtonGO;
        [SerializeField] private GameObject aidButtonGO;
        [SerializeField] private GameObject allianceButtonGO;
        [SerializeField] private GameObject gatherIntelButtonGO;
        [SerializeField] private GameObject theftButtonGO;
        [SerializeField] private GameObject disinformationButtonGO;
        [SerializeField] private GameObject sabatogeButtonGO;
        [SerializeField] private GameObject combatButtonGO;
        [SerializeField] private GameObject closeDiplomacyButtonGO;

        [Header("Data Lists")]
        [SerializeField] private List<StarSysController> sysControllers;
        [SerializeField] private List<FleetController> fleetControllers;
        [SerializeField] private List<DiplomacyController> diplomacyControllers;
        [SerializeField] private List<GameObject> listOfStarSysUiGos;
        [SerializeField] private List<GameObject> listOfSysShipUiGos;
        [SerializeField] private List<GameObject> listOfFleetUiGos;
        [SerializeField] private List<GameObject> listOfDiplomacyUiGos;

        [Header("Prefabs")]
        [SerializeField] private GameObject fleetUI_Prefab;

        #endregion

        #region Specialized Managers

        private GalaxyUIStateManager uiStateManager;
        private GalaxyCivDisplayManager civDisplayManager;
        private GalaxyShipDeployManager shipDeployManager;
        private GalaxyListPopulator listPopulator;
        private GalaxyCameraManager cameraManager;

        #endregion

        #region References to Other UI Controllers

        private FleetMenuUIController fleetMenuUIController => FleetMenuUIController.Instance;
        private StarSysMenuUIController starSysMenuUIController => StarSysMenuUIController.Instance;
        private DiplomacyMenuUIController diplomacyMenuUIController => DiplomacyMenuUIController.Instance;
        private ShipDeployMenuUIController shipDeployMenuUIController => ShipDeployMenuUIController.Instance;

        #endregion

        #region Properties (Backward Compatibility via Delegation)

        public GalaxyClickMode CurrentClickMode
        {
            get => uiStateManager?.CurrentClickMode ?? GalaxyClickMode.Normal;
            set
            {
                if (uiStateManager != null)
                {
                    uiStateManager.SetClickMode(value);
                    UpdateCursorForClickMode();
                }
            }
        }

        public FleetController FleetLookingForDestination
        {
            get => shipDeployManager?.FleetLookingForDestination;
            set => shipDeployManager?.BeginSetDestination(value);
        }

        public FleetController FleetLookingForShipDeploy
        {
            get => shipDeployManager?.FleetLookingForShipDeploy;
            set => shipDeployManager?.SetFleetLookingForShipDeploy(value);
        }

        public FleetController FleetSelectedForShipDeploy
        {
            get => shipDeployManager?.FleetSelectedForShipDeploy;
            set => shipDeployManager?.SetFleetSelectedForShipDeploy(value);
        }

        public FleetController FleetLookingForShipMerge
        {
            get => shipDeployManager?.FleetLookingForShipMerge;
            set => shipDeployManager?.SetFleetLookingForMerge(value);
        }

        public FleetController FleetSelectedForShipMerge
        {
            get => shipDeployManager?.FleetSelectedForShipMerge;
            set => shipDeployManager?.SetFleetSelectedForMerge(value);
        }

        public StarSysController StarSystLookingForShipDeploy
        {
            get => shipDeployManager?.StarSystLookingForShipDeploy;
            set => shipDeployManager?.SetSystemLookingForShipDeploy(value);
        }

        public StarSysController StarSystSelectedForShipDeploy
        {
            get => shipDeployManager?.StarSystSelectedForShipDeploy;
            set => shipDeployManager?.SetSystemSelectedForShipDeploy(value);
        }

        public StarSysController StarSystLookingForShipMerge
        {
            get => shipDeployManager?.StarSystLookingForShipMerge;
            set => shipDeployManager?.SetSystemLookingForMerge(value);
        }

        public StarSysController StarSystSelectedForShipMerge
        {
            get => shipDeployManager?.StarSystSelectedForShipMerge;
            set => shipDeployManager?.SetSystemSelectedForMerge(value);
        }

        #endregion

        #region Initialization

        private bool _isInitialized = false;

        private void Awake()
        {
            // Scene-based singleton
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("✅ GalaxyMenuUIController: Instance assigned");
            }
            else if (Instance != this)
            {
                Debug.LogWarning("❌ Duplicate GalaxyMenuUIController found! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            InitializeManagers();
        }

        private void Start()
        {
            // Guard against re-initialization
            if (_isInitialized)
            {
                Debug.Log("GalaxyMenuUIController.Start() skipped - already initialized");
                return;
            }
            _isInitialized = true;

            Debug.Log("GalaxyMenuUIController: Start called");

            // Initialize UI using managers
            uiStateManager.InitializeMenuStates();
            civDisplayManager.LoadLocalPlayerCivilizationUI();

            // Wire buttons
            WireButtons();

            // Activate main galaxy menu
            ActivateMainGalaxyMenu();

            Debug.Log("GalaxyMenuUIController: Start complete");
        }

        /// <summary>
        /// Initialize all specialized managers
        /// </summary>
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
            shipDeployManager = new GalaxyShipDeployManager(shipDeployMenuUIController);

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
            cameraManager = new GalaxyCameraManager(parentCanvas, galaxyEventCamera);

            Debug.Log("✅ GalaxyMenuUIController: All managers initialized");
        }

        #endregion

        #region Button Wiring

        private void WireButtons()
        {
            WireHomeSystemButton();
            WireCloseMenuButton();
            WireReportButton();
        }

        private void WireHomeSystemButton()
        {
            if (homeSystemButton != null)
            {
                homeSystemButton.onClick.RemoveAllListeners();
                homeSystemButton.onClick.AddListener(OnHomeSystemButtonClicked);
                Debug.Log("✅ HomeSystemButton wired");
            }
        }

        private void WireCloseMenuButton()
        {
            if (closeMenuButton != null)
            {
                Button closeButton = closeMenuButton.GetComponent<Button>();
                if (closeButton != null)
                {
                    closeButton.onClick.RemoveAllListeners();
                    closeButton.onClick.AddListener(CloseCurrentMenu);
                    Debug.Log("✅ CloseMenuButton wired");
                }
            }
        }

        private void WireReportButton()
        {
            if (reportButton != null)
            {
                reportButton.onClick.RemoveAllListeners();
                reportButton.onClick.AddListener(OnReportButtonClicked);
                Debug.Log("✅ ReportButton wired");
            }

            // Ensure the report view starts hidden
            if (reportView != null)
                reportView.SetActive(false);
        }

        #endregion

        #region Button Handlers (Main Menu Ribbon)

        private void OnReportButtonClicked()
        {
            if (reportView == null) return;

            bool opening = !reportView.activeSelf;
            reportView.SetActive(opening);

            if (opening)
                PopulateReportView();
        }

        private void PopulateReportView()
        {
            if (reportView == null) return;

            string report = CombatUIManager.LastCombatReport;
            if (string.IsNullOrEmpty(report))
                report = "No combat report available.";

            TextMeshProUGUI[] tmps = reportView.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp.name == "Report(TMP)" || tmp.name == "Report")
                {
                    tmp.text = report;
                    return;
                }
            }

            Debug.LogWarning("PopulateReportView: Report(TMP) text component not found inside Report View.");
        }

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

        private void OnHomeSystemButtonClicked()
        {
            Debug.Log("GalaxyMenuUIController: Home System button clicked");

            // ✅ Close all open menus to ensure a clear view
            CloseAllMenus();

            // Move camera to home system only - do NOT open the star system UI
            if (GalaxyCameraDragMoveZoom.Instance != null)
            {
                GalaxyCameraDragMoveZoom.Instance.SetCameraToLocalPlayerHome();
            }
            else
            {
                Debug.LogError("OnHomeSystemButtonClicked: GalaxyCameraDragMoveZoom.Instance is null!");
            }
        }

        #endregion

        #region Menu Operations (Delegate to Managers)

        public void OpenMenu(Menu menuEnum, GameObject callingMenuOrGalaxyObject)
        {
            Debug.Log($"GalaxyMenuUIController: OpenMenu({menuEnum})");

            // Close currently open menu
            CloseCurrentMenu();

            // Open new menu via state manager
            // ✅ CRITICAL: Do NOT pass world objects (Systems/Fleets) as the tracked menu object!
            // If we do, the state manager will call SetActive(false) on them when the menu closes,
            // making them disappear from the galaxy map. Their UI is managed by sub-controllers.
            GameObject trackedUIObject = callingMenuOrGalaxyObject;
            if (trackedUIObject != null &&
                (trackedUIObject.GetComponent<StarSysController>() != null ||
                 trackedUIObject.GetComponent<FleetController>() != null))
            {
                trackedUIObject = null;
            }

            uiStateManager.OpenMenu(menuEnum, trackedUIObject);

            // Populate appropriate list
            switch (menuEnum)
            {
                case Menu.SystemsMenu:
                case Menu.StarSys:
                    if (starSysMenuUIController != null)
                    {
                        starSysMenuUIController.ShowSystemMenuView();
                        starSysMenuUIController.PopulateSystemsList(); // Moves UIs to list
                    }
                    break;

                case Menu.ASystemMenu:
                    // Open a specific system menu
                    if (callingMenuOrGalaxyObject == null)
                    {
                        Debug.LogError("OpenMenu(ASystemMenu): callingMenuOrGalaxyObject is NULL!");
                        return;
                    }

                    var sysCon = callingMenuOrGalaxyObject.GetComponentInChildren<StarSysController>();
                    if (sysCon == null)
                    {
                        // Try getting it directly (not in children)
                        sysCon = callingMenuOrGalaxyObject.GetComponent<StarSysController>();
                    }

                    if (sysCon != null && starSysMenuUIController != null)
                    {
                        Debug.Log($"OpenMenu(ASystemMenu): Opening system '{sysCon.name}'");
                        starSysMenuUIController.ShowA_SystemMenuView();
                        starSysMenuUIController.SetActiveSetParentUIGO(sysCon);
                    }
                    else
                    {
                        Debug.LogError($"OpenMenu(ASystemMenu): GameObject '{callingMenuOrGalaxyObject?.name}' has no StarSysController! (sysCon={sysCon}, starSysMenuUIController={starSysMenuUIController})");
                        return;
                    }
                    break;

                case Menu.FleetMenu:
                case Menu.Fleet:
                    if (fleetMenuUIController != null)
                    {
                        fleetMenuUIController.ShowFleetMenuView();
                        // No population needed - fleets stay visible in the galaxy
                    }
                    break;

                case Menu.AFleetMenu:
                    // Open a specific fleet menu
                    if (callingMenuOrGalaxyObject == null)
                    {
                        Debug.LogError("OpenMenu(AFleetMenu): callingMenuOrGalaxyObject is NULL!");
                        return;
                    }

                    var fleetCon = callingMenuOrGalaxyObject.GetComponentInChildren<FleetController>();
                    if (fleetCon == null)
                    {
                        // Try getting it directly (not in children)
                        fleetCon = callingMenuOrGalaxyObject.GetComponent<FleetController>();
                    }

                    if (fleetCon != null && fleetMenuUIController != null)
                    {
                        Debug.Log($"OpenMenu(AFleetMenu): Opening fleet '{fleetCon.name}'");
                        fleetMenuUIController.ShowA_FleetMenuView();
                        fleetMenuUIController.SetActiveSetParentUIGO(fleetCon);
                        fleetMenuUIController.MoveTheFleetUIGO(callingMenuOrGalaxyObject);
                    }
                    else
                    {
                        Debug.LogError($"OpenMenu(AFleetMenu): GameObject '{callingMenuOrGalaxyObject?.name}' has no FleetController! (fleetCon={fleetCon}, fleetMenuUIController={fleetMenuUIController})");
                        return;
                    }
                    break;

                case Menu.Diplomacy:
                    // ✅ Sync diplomacy controllers from manager
                    if (DiplomacyManager.Instance != null)
                    {
                        if (diplomacyControllers == null) diplomacyControllers = new List<DiplomacyController>();
                        diplomacyControllers.Clear();
                        diplomacyControllers.AddRange(DiplomacyManager.Instance.DiplomacyControllers);
                        diplomacyControllers.Reverse(); // ✅ Most recent at the top
                    }

                    listPopulator.PopulateDiplomacyList();
                    if (diplomacyControllers != null && diplomacyControllers.Count > 0)
                    {
                        uiStateManager.HideNoContactUI();
                        if (diplomacyMenuUIController != null)
                        {
                            diplomacyMenuUIController.ShowDiplomacyMenuView();
                        }
                    }
                    else
                    {
                        uiStateManager.ShowNoContactUI();
                    }
                    break;

                case Menu.Intel:
                    if (intelMenuView != null) intelMenuView.SetActive(true);
                    break;

                case Menu.Encyclopedia:
                    if (encyclopediaMenuView != null) encyclopediaMenuView.SetActive(true);
                    break;
            }
        }

        public void CloseCurrentMenu()
        {
            Debug.Log("GalaxyMenuUIController: CloseCurrentMenu");

            // Get the current menu before closing it
            Menu currentMenu = uiStateManager.CurrentOpenMenu;

            // Just hide the views, don't move UIs back (they might be needed for next menu)
            HideMenuViews(currentMenu);

            // Close via state manager
            uiStateManager.CloseCurrentMenu();
        }

        /// <summary>
        /// Hide menu views without moving UIs back to storage
        /// Used when transitioning between menus
        /// Just hides the view panels, keeps UIs where they are
        /// </summary>
        private void HideMenuViews(Menu enumMenu)
        {
            Debug.Log($"GalaxyMenuUIController: HideMenuViews({enumMenu}) - transition mode");

            switch (enumMenu)
            {
                case Menu.None:
                    break;

                case Menu.SystemsMenu:
                case Menu.StarSys:
                    // Just hide the view, don't move UIs
                    if (starSysMenuUIController != null && starSysMenuUIController.SystemsMenuView != null)
                        starSysMenuUIController.SystemsMenuView.SetActive(false);
                    break;

                case Menu.ASystemMenu:
                    // Just hide the view, don't move UIs back
                    if (starSysMenuUIController != null && starSysMenuUIController.ASystemMenuView != null)
                        starSysMenuUIController.ASystemMenuView.SetActive(false);
                    break;

                case Menu.FleetMenu:
                case Menu.Fleet:
                    // Just hide the view, don't move UIs
                    if (fleetMenuUIController != null && fleetMenuUIController.FleetMenuView != null)
                    {
                        fleetMenuUIController.FleetMenuView.SetActive(false);
                        fleetMenuUIController.CloseDestinationSelectionCursor();
                    }
                    break;

                case Menu.AFleetMenu:
                    // Just hide the view, don't move UIs back
                    if (fleetMenuUIController != null && fleetMenuUIController.AFleetMenuView != null)
                    {
                        fleetMenuUIController.AFleetMenuView.SetActive(false);
                        fleetMenuUIController.CloseDestinationSelectionCursor();
                    }
                    break;

                case Menu.DiplomacyMenu:
                case Menu.Diplomacy:
                    if (diplomacyMenuUIController != null)
                    {
                        diplomacyMenuUIController.HideDiplomacyMenuView();
                        diplomacyMenuUIController.HideA_DiplomacyMenuView();
                    }
                    break;

                case Menu.ADiplomacyMenu:
                    if (diplomacyMenuUIController != null)
                    {
                        diplomacyMenuUIController.HideA_DiplomacyMenuView();
                        diplomacyMenuUIController.HideDiplomacyMenuView();
                    }
                    break;

                case Menu.IntellMenu:
                case Menu.Intel:
                    if (intelMenuView != null)
                        intelMenuView.SetActive(false);
                    break;

                case Menu.EncyclopedianMenu:
                case Menu.Encyclopedia:
                    if (encyclopediaMenuView != null)
                        encyclopediaMenuView.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// Fully close a menu and move UIs back to storage
        /// Used when truly exiting the menu system
        /// </summary>
        public void CloseMenu(Menu enumMenu)
        {
            Debug.Log($"GalaxyMenuUIController: CloseMenu({enumMenu})");

            switch (enumMenu)
            {
                case Menu.None:
                    break;

                case Menu.SystemsMenu:
                case Menu.StarSys:
                    if (starSysMenuUIController != null)
                    {
                        starSysMenuUIController.MoveBackAnyStarSysUIGO();
                        starSysMenuUIController.HideSystemMenuView();
                    }
                    break;

                case Menu.ASystemMenu:
                    if (starSysMenuUIController != null)
                    {
                        starSysMenuUIController.MoveBackAnyStarSysUIGO();
                        starSysMenuUIController.HideA_SystemMenuView();
                    }
                    break;

                case Menu.FleetMenu:
                case Menu.Fleet:
                    if (fleetMenuUIController != null)
                    {
                        fleetMenuUIController.MoveBackAnyaFleetUIGO();
                        fleetMenuUIController.HideFleetMenuView();
                        fleetMenuUIController.CloseDestinationSelectionCursor();
                    }
                    break;

                case Menu.AFleetMenu:
                    if (fleetMenuUIController != null)
                    {
                        fleetMenuUIController.MoveBackAnyaFleetUIGO();
                        fleetMenuUIController.HideA_FleetMenuView();
                        fleetMenuUIController.CloseDestinationSelectionCursor();
                    }
                    break;

                case Menu.DiplomacyMenu:
                case Menu.Diplomacy:
                case Menu.ADiplomacyMenu:
                    if (diplomacyMenuUIController != null)
                    {
                        diplomacyMenuUIController.MoveBackAnyDiplomacyUIGO();
                        diplomacyMenuUIController.HideDiplomacyMenuView();
                        diplomacyMenuUIController.HideA_DiplomacyMenuView();
                    }
                    break;

                case Menu.IntellMenu:
                case Menu.Intel:
                    if (intelMenuView != null)
                        intelMenuView.SetActive(false);
                    break;

                case Menu.EncyclopedianMenu:
                case Menu.Encyclopedia:
                    if (encyclopediaMenuView != null)
                        encyclopediaMenuView.SetActive(false);
                    break;
            }
        }

        public void CloseAllMenus()
        {
            Debug.Log("GalaxyMenuUIController: CloseAllMenus");

            uiStateManager.CloseAllMenus();
            listPopulator.ClearAllLists();

            // Close all sub-menus and restore galaxy objects
            if (starSysMenuUIController != null)
            {
                starSysMenuUIController.MoveBackAnyStarSysUIGO();
                starSysMenuUIController.HideSystemMenuView();
                starSysMenuUIController.HideA_SystemMenuView();
            }

            if (fleetMenuUIController != null)
            {
                fleetMenuUIController.MoveBackAnyaFleetUIGO();
                fleetMenuUIController.HideFleetMenuView();
                fleetMenuUIController.HideA_FleetMenuView();
            }

            if (diplomacyMenuUIController != null)
            {
                diplomacyMenuUIController.MoveBackAnyDiplomacyUIGO();
                diplomacyMenuUIController.HideDiplomacyMenuView();
                diplomacyMenuUIController.HideA_DiplomacyMenuView();
            }

            HideShipDeployMenu();
        }

        #endregion

        #region Ship Deploy Operations (Delegate to Manager)

        public void ShowShipDeployMenuForFleet(FleetController fleet)
        {
            shipDeployManager.ShowShipDeployMenuForFleet(fleet);
        }

        public void ShowShipDeployForSystemNewFleet(StarSysController system, FleetController newFleet)
        {
            shipDeployManager.ShowShipDeployForSystemNewFleet(system, newFleet);
        }

        public void ShowShipDeployForFleetNewFleet(FleetController originalFleet, FleetController newFleet)
        {
            shipDeployManager.ShowShipDeployForFleetNewFleet(originalFleet, newFleet);
        }

        public void HideShipDeployMenu()
        {
            shipDeployManager.HideShipDeployMenu();
        }

        public void CloseShipDeployMenu()
        {
            shipDeployManager.CompleteShipExchange();
            uiStateManager.ResetClickMode();
        }

        public void ClickCancelShipDeployButton()
        {
            shipDeployManager.CancelShipDeploy();
            uiStateManager.ResetClickMode();
        }

        #endregion

        #region Click Mode Operations (Delegate to Manager)

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

        private void UpdateCursorForClickMode()
        {
            // Cursor update logic based on click mode
            switch (uiStateManager.CurrentClickMode)
            {
                case GalaxyClickMode.Normal:
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    break;
                case GalaxyClickMode.SetDestination:
                    // Could set a custom cursor here
                    break;
                case GalaxyClickMode.SelectForShipDeploy:
                    // Could set a custom cursor here
                    break;
            }
        }

        #endregion

        #region Ship Deploy Context Setters (Delegate to Manager)

        internal void WhatFleetIsSelectedForShipDiploy(FleetController fleetController)
        {
            shipDeployManager.SetFleetSelectedForShipDeploy(fleetController);
        }

        internal void WhatFleetIsSelectedForShipMerge(FleetController fleetController)
        {
            shipDeployManager.SetFleetSelectedForMerge(fleetController);
        }

        internal void WhatSystemIsSelectedForShipDeploy(StarSysController starSysController)
        {
            shipDeployManager.SetSystemSelectedForShipDeploy(starSysController);
        }

        internal void WhatSystemIsSelectedForShipMerge(StarSysController starSysController)
        {
            shipDeployManager.SetSystemSelectedForMerge(starSysController);
        }

        public void WhatFleetIsLookingForMerge(FleetController fleetConLooking)
        {
            shipDeployManager.SetFleetLookingForMerge(fleetConLooking);
        }

        public void WhatFleetIsLookingForShipDeploy(FleetController fleetConLooking)
        {
            shipDeployManager.SetFleetLookingForShipDeploy(fleetConLooking);
        }

        public void WhatSystIsLookingForMerge(StarSysController starSystConLooking)
        {
            shipDeployManager.SetSystemLookingForMerge(starSystConLooking);
        }

        public void WhatSystIsLookingForShipDeploy(StarSysController starSystConLooking)
        {
            shipDeployManager.SetSystemLookingForShipDeploy(starSystConLooking);
        }

        public void BeginSetDestination(FleetController fleetLooking)
        {
            shipDeployManager.BeginSetDestination(fleetLooking);
            SetClickMode(GalaxyClickMode.SetDestination);
        }

        public void CompleteSetDestination()
        {
            shipDeployManager.CompleteSetDestination();
            ResetClickMode();
        }

        public void CompleteShipExchange()
        {
            shipDeployManager.CompleteShipExchange();
            ResetClickMode();
        }

        #endregion

        #region Camera Operations (Delegate to Manager)

        public void InitializeGalaxyCamera()
        {
            cameraManager.InitializeGalaxyCamera();
        }

        private void DiagnoseGalaxyUI()
        {
            cameraManager.DiagnoseCameraSetup();
        }

        #endregion

        #region Utility Methods

        private void ActivateMainGalaxyMenu()
        {
            // Activate the main galaxy menu ribbon/panel if needed
            // This is where you'd show the main UI panel
            Debug.Log("GalaxyMenuUIController: Main galaxy menu activated");
        }

        public void FindTheirHomeSystem(CivController civCon, out StarSysController homeSystController)
        {
            homeSystController = null;

            if (civCon?.CivData == null)
            {
                Debug.LogWarning($"No CivData found for {civCon?.name}");
                return;
            }

            // Find home system by name from the list of owned systems
            if (civCon.CivData.StarSysWeOwn != null && !string.IsNullOrEmpty(civCon.CivData.CivHomeSystemName))
            {
                homeSystController = civCon.CivData.StarSysWeOwn.Find(
                    sys => sys != null && sys.name == civCon.CivData.CivHomeSystemName
                );

                if (homeSystController != null)
                {
                    Debug.Log($"Found home system: {homeSystController.name}");
                }
                else
                {
                    Debug.LogWarning($"No home system found for {civCon.name} with name '{civCon.CivData.CivHomeSystemName}'");
                }
            }
            else
            {
                Debug.LogWarning($"No home system data available for {civCon.name}");
            }
        }

        internal void HideNoContactUI()
        {
            uiStateManager.HideNoContactUI();
        }

        public void SetActiveBuildMenu(GameObject prefabMenu)
        {
            if (prefabMenu != null)
            {
                prefabMenu.SetActive(true);
                Debug.Log($"Activated build menu: {prefabMenu.name}");
            }
        }

        public void CloseTheBackgrounds()
        {
            uiStateManager.CloseAllBackgrounds();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("GalaxyMenuUIController: Instance cleared");
            }
        }

        #endregion
    }
}
