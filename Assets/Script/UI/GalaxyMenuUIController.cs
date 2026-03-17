using BOTF3D.Core;
using BOTF3D.GamePlay;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


//Assets.Core              → Managers, Data classes, Game systems
//Assets.UI                → UI Controllers(menu, HUD, panels)
//Assets.GamePlay          → Gameplay controllers(Fleet, Ship, System)
namespace BOTF3D.UI
{
    public class GalaxyMenuUIController : MonoBehaviour
    {
        public static GalaxyMenuUIController Instance;
        [SerializeField]
        private Camera galaxyEventCamera;
        [SerializeField]
        private Canvas parentCanvas;
        [SerializeField]
        Button homeSystemButton;
        // REMOVE [SerializeField] - use Instance singletons instead
        private FleetMenuUIController fleetMenuUIController => FleetMenuUIController.Instance;
        private StarSysMenuUIController starSysMenuUIController => StarSysMenuUIController.Instance;
        private DiplomacyMenuUIController diplomacyMenuUIController => DiplomacyMenuUIController.Instance;
        private ShipDeployMenuUIController shipDeployMenuUIController => ShipDeployMenuUIController.Instance;

        [SerializeField]
        private GameObject sysBuildMenu;
        [SerializeField]
        private GameObject diplomacyNoContacts;
        [SerializeField]
        private GameObject intelMenuView;
        [SerializeField]
        private GameObject encyclopediaMenuView;
        [SerializeField]
        private GameObject aNull;
        [SerializeField]
        private GameObject closeMenuButton;
        [SerializeField] private Button saveShipDelployButton;
        [SerializeField]
        private GameObject sysBackground;
        [SerializeField]
        private GameObject fleetsBackground;
        [SerializeField]
        private GameObject diplomacyBackground;
        [SerializeField]
        private GameObject intelBackground;
        [SerializeField]
        private GameObject encyclopediaBackground;
        [SerializeField]
        private GameObject habitableSysMenu;
        [SerializeField]
        private List<StarSysController> sysControllers;
        [SerializeField]
        private List<FleetController> fleetControllers;
        [SerializeField]
        private List<DiplomacyController> diplomacyControllers;
        [SerializeField]
        private List<GameObject> listOfStarSysUiGos;
        [SerializeField]
        private List<GameObject> listOfSysShipUiGos;
        [SerializeField]
        private List<GameObject> listOfFleetUiGos;
        [SerializeField]
        private List<GameObject> listOfDiplomacyUiGos;
        [SerializeField]
        private GameObject powerOverload;
        [SerializeField]
        private GameObject openMenuWas;
        [SerializeField]
        private Menu openMenuEnumWas;
        [SerializeField]
        private GameObject fleetUI_Prefab;
        public GalaxyClickMode CurrentClickMode { get; set; } = GalaxyClickMode.Normal;
        public FleetController FleetLookingForDestination { get; set; }
        public FleetController FleetLookingForShipDeploy { get; set; }
        public FleetController FleetSelectedForShipDeploy { get; set; }
        public FleetController FleetLookingForShipMerge { get; set; }
        public FleetController FleetSelectedForShipMerge { get; set; }
        public StarSysController StarSystLookingForShipDeploy { get; set; }
        public StarSysController StarSystSelectedForShipDeploy { get; set; }
        public StarSysController StarSystLookingForShipMerge { get; set; }
        public StarSysController StarSystSelectedForShipMerge { get; set; }

        [SerializeField] private GameObject selectOtherSysOrFleetButtonGO; // both fleet and system use this button so controller at GalaxyMenuUIController level

        [SerializeField]
        private GameObject InteractionButtonGO;
        [SerializeField]
        private GameObject tradeButtonGO;
        [SerializeField]
        private GameObject engagementButtonGO;
        [SerializeField]
        private GameObject techButtonGO;
        [SerializeField]
        private GameObject aidButtonGO;
        [SerializeField]
        private GameObject allianceButtonGO;
        [SerializeField]
        private GameObject gatherIntelButtonGO;
        [SerializeField]
        private GameObject theftButtonGO;
        [SerializeField]
        private GameObject disinformationButtonGO;
        [SerializeField]
        private GameObject sabatogeButtonGO;
        [SerializeField]
        private GameObject combatButtonGO;
        [SerializeField]
        private GameObject closeDiplomacyButtonGO;
        private readonly int _scouts;
        private readonly int _destroyers;
        private readonly int _cruisters;
        private readonly int _ltCruisers;
        private readonly int _hvyCruisers;
        private readonly int _transports;


        private void Awake()
        {
            // ✅ Scene-based singleton
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("✅ GalaxyMenuUIController: Instance assigned");
            }
            else if (Instance != this)
            {
                Debug.LogWarning($"❌ Duplicate GalaxyMenuUIController found! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("GalaxyMenuUIController: Instance cleared");
            }
        }

        void Start()
        {
            Debug.Log("GalaxyMenuUIController: Start called - deferring camera setup");

            // Initialize UI states
            if (intelMenuView != null) intelMenuView.SetActive(false);
            if (encyclopediaMenuView != null) encyclopediaMenuView.SetActive(false);
            if (closeMenuButton != null) closeMenuButton.SetActive(true);
            if (sysBackground != null) sysBackground.SetActive(false);
            if (fleetsBackground != null) fleetsBackground.SetActive(false);
            if (diplomacyBackground != null) diplomacyBackground.SetActive(false);
            if (intelBackground != null) intelBackground.SetActive(false);
            if (encyclopediaBackground != null) encyclopediaBackground.SetActive(false);
            if (habitableSysMenu != null) habitableSysMenu.SetActive(false);

            HideShipDeployMenu();
            diplomacyControllers = new List<DiplomacyController>();

            Debug.Log("GalaxyMenuUIController: Start complete");
        }

        // Call this from MainMenuUIController after CanvasGalaxy activates
        public void InitializeGalaxyCamera()
        {
            var xAngle = GalaxyCameraDragMoveZoom.Instance.galaxyXRotation; // Set reference in camera controller as well
            if (galaxyEventCamera == null)
            {
                var mainCameraGO = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCameraGO != null)
                {

                    galaxyEventCamera = mainCameraGO.GetComponent<Camera>();
                    galaxyEventCamera.transform.rotation = Quaternion.Euler(xAngle, galaxyEventCamera.transform.eulerAngles.y, galaxyEventCamera.transform.eulerAngles.z); // Rotate camera to face correct down angle for galaxy view
                    Debug.Log($"GalaxyMenuUIController: Found galaxy camera: {galaxyEventCamera?.name}");
                }
                else
                {
                    Debug.LogWarning("GalaxyMenuUIController: MainCamera not found yet");
                }
            }

            if (parentCanvas != null && galaxyEventCamera != null)
            {
                parentCanvas.worldCamera = galaxyEventCamera;
                galaxyEventCamera.transform.rotation = Quaternion.Euler(xAngle, galaxyEventCamera.transform.eulerAngles.y, galaxyEventCamera.transform.eulerAngles.z); // Rotate camera to face correct down angle for galaxy view
                Debug.Log("GalaxyMenuUIController: Parent canvas camera assigned");
            }

            // Wire up the HomeSystemButton dynamically
            WireHomeSystemButton();

            // ✅ Wire up the close button
            WireCloseMenuButton();
        }

        //Wire HomeSystemButton to the galaxy camera controller
        private void WireHomeSystemButton()
        {
            // Wire it up to the galaxy camera controller
            if (GalaxyCameraDragMoveZoom.Instance != null)
            {
                homeSystemButton.onClick.RemoveAllListeners();
                homeSystemButton.onClick.AddListener(() => GalaxyCameraDragMoveZoom.Instance.SetCameraToLocalPlayerHome());
                Debug.Log("✅ HomeSystemButton wired to GalaxyCameraDragMoveZoom");
            }
            else
            {
                Debug.LogWarning("WireHomeSystemButton: GalaxyCameraDragMoveZoom.Instance is null");
            }
        }

        // ✅ ADD: New method to wire close button
        private void WireCloseMenuButton()
        {
            if (closeMenuButton != null)
            {
                var button = closeMenuButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => CloseCurrentMenu());
                    Debug.Log("✅ Close Menu Button wired to CloseCurrentMenu()");
                }
                else
                {
                    Debug.LogWarning("WireCloseMenuButton: Button component not found on closeMenuButton GameObject!");
                }
            }
            else
            {
                Debug.LogWarning("WireCloseMenuButton: closeMenuButton is null!");
            }
        }

        // ✅ ADD: New method to close whatever menu is currently open
        public void CloseCurrentMenu()
        {
            Debug.Log($"CloseCurrentMenu: Closing {openMenuEnumWas}");

            // Close the currently tracked open menu
            if (openMenuEnumWas != Menu.None)
            {
                CloseMenu(openMenuEnumWas);
                openMenuEnumWas = Menu.None;
            }

            // Also close all backgrounds for safety
            CloseTheBackgrounds();

            // Explicitly hide all menu views
            if (diplomacyMenuUIController != null)
            {
                diplomacyMenuUIController.HideDiplomacyMenuView();
                diplomacyMenuUIController.HideA_DiplomacyMenuView();
            }

            if (starSysMenuUIController != null)
            {
                starSysMenuUIController.HideSystemMenuView();
                starSysMenuUIController.HideA_SystemMenuView();
            }

            if (fleetMenuUIController != null)
            {
                fleetMenuUIController.HideFleetMenuView();
                fleetMenuUIController.HideA_FleetMenuView();
            }

            // Close other views
            if (intelMenuView != null && intelMenuView.activeSelf)
                intelMenuView.SetActive(false);

            if (encyclopediaMenuView != null && encyclopediaMenuView.activeSelf)
                encyclopediaMenuView.SetActive(false);

            if (habitableSysMenu != null && habitableSysMenu.activeSelf)
                habitableSysMenu.SetActive(false);

            // Resume time if diplomacy paused it
            if (TimeManager.Instance != null && !TimeManager.Instance.timeRunning)
            {
                TimeManager.Instance.ResumeTime();
            }

            Debug.Log("CloseCurrentMenu: All menus closed");
        }

        // Helper for recursive search (if not already present)
        private GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name)
                return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject found = FindInHierarchy(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        public void SystemButtonPressed()
        {
            Debug.Log("=== SystemButtonPressed: Starting ===");
            Debug.Log($"  Instance={Instance != null}, Camera={galaxyEventCamera != null}");

            // Check if systems exist
            if (StarSysManager.Instance != null)
            {
                int systemCount = StarSysManager.Instance.StarSysControllerList?.Count ?? 0;
                Debug.Log($"  StarSysManager has {systemCount} systems");

                if (systemCount == 0)
                {
                    Debug.LogError("  ❌ NO SYSTEMS EXIST! Systems weren't created.");
                }
            }
            else
            {
                Debug.LogError("  ❌ StarSysManager.Instance is NULL!");
            }

            // Check if StarSysMenuUIController exists
            if (starSysMenuUIController != null)
            {
                Debug.Log($"  StarSysMenuUIController exists");
            }
            else
            {
                Debug.LogError("  ❌ starSysMenuUIController is NULL!");
            }

            // Ensure camera is initialized
            if (galaxyEventCamera == null)
            {
                InitializeGalaxyCamera();
            }

            CloseShipDeployMenu();
            OpenMenu(Menu.SystemsMenu, gameObject);

            Debug.Log("=== SystemButtonPressed: Complete ===");
        }

        // ShipDeploy menu life cycle helpers — central control point
        public void ShowShipDeployMenuForFleet(FleetController newFleet)
        {
            if (shipDeployMenuUIController == null) return;
            MousePointerChanger.Instance.ResetCursor();

            // move the fleet UI under the active AFleet/A_System view if appropriate
            var fleetLooking = newFleet;
            var starSysLooking = StarSystLookingForShipDeploy;
            if (fleetLooking != null)
            {
                var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
                if (newFleet.FleetUIGameObject != null)
                {
                    newFleet.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                    newFleet.FleetUIGameObject.transform.SetAsLastSibling();
                    newFleet.FleetUIGameObject.SetActive(true); // ✅ ACTIVATE!
                    Debug.Log($"  Fleet UI '{newFleet.FleetUIGameObject.name}' parented to AFleetMenuView and ACTIVATED");
                }
            }
            else if (starSysLooking != null)
            {
                var aStarSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
                if (newFleet.FleetUIGameObject != null)
                {
                    newFleet.FleetUIGameObject.transform.SetParent(aStarSysView.transform, false);
                    newFleet.FleetUIGameObject.transform.SetAsLastSibling();
                    newFleet.FleetUIGameObject.SetActive(true); // ✅ ACTIVATE!
                    Debug.Log($"  Fleet UI '{newFleet.FleetUIGameObject.name}' parented to ASystemMenuView and ACTIVATED");
                }

                // ✅ ALSO: Make sure ASystemMenuView itself is visible
                if (!aStarSysView.activeSelf)
                {
                    aStarSysView.SetActive(true);
                    Debug.Log($"  ASystemMenuView ACTIVATED");
                }
            }

            shipDeployMenuUIController.SetUpBottomShipLists(newFleet, true);
            SetClickMode(GalaxyClickMode.SelectForShipDeploy);

            shipDeployMenuUIController.gameObject.SetActive(true);
            shipDeployMenuUIController.ShowShipDeployMenuView();
        }

        public void ShowShipDeployForSystemNewFleet(StarSysController starSystCon, FleetController newFleet)
        {
            if (shipDeployMenuUIController == null) return;
            // no GalaxyClickMode. this is new fleet button click;
            Debug.Log($"ShowShipDeployForSystemNewFleet: opening deploy UI for system='{starSystCon?.name}' new fleet='{newFleet?.name}'");

            MousePointerChanger.Instance.ResetCursor();

            // ✅ CRITICAL: Ensure star system has ShipListUIParent set up
            if (starSystCon.StarSysData.ShipListUIParent == null)
            {
                Debug.LogWarning($"Star system '{starSystCon.name}' missing ShipListUIParent - setting it up now");

                var uiFields = starSystCon.StarSysUIGameObject?.GetComponent<StarSysUI_Fields>();
                if (uiFields != null && uiFields.shipContent != null)
                {
                    starSystCon.StarSysData.ShipListUIParent = uiFields.shipContent.gameObject;
                    Debug.Log($"Set ShipListUIParent for system '{starSystCon.name}'");
                }
                else
                {
                    Debug.LogError($"Cannot find shipContent for system '{starSystCon.name}'!");
                }
            }

            // ✅ CRITICAL: Ensure NEW fleet has ShipListUIParent set up BEFORE opening panel
            if (newFleet.FleetData.ShipListUIParent == null)
            {
                Debug.LogWarning($"New fleet '{newFleet.name}' missing ShipListUIParent - setting it up now");

                var uiFields = newFleet.FleetUIGameObject?.GetComponent<FleetUI_Fields>();
                if (uiFields != null && uiFields.FleetShipContentGO != null)
                {
                    newFleet.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;
                    Debug.Log($"✅ Set ShipListUIParent for new fleet '{newFleet.name}': {uiFields.FleetShipContentGO.name}");
                }
                else
                {
                    Debug.LogError($"❌ Cannot find FleetShipContentGO for new fleet '{newFleet.name}'! uiFields={(uiFields != null ? "EXISTS" : "NULL")}");
                }
            }

            shipDeployMenuUIController.gameObject.SetActive(true);
            shipDeployMenuUIController.ShowShipDeployMenuView();

            // Set up TopSlot with star system's ships - cast to resolve namespace issue
            shipDeployMenuUIController.SetUpTopShipLists(starSystCon.StarSysData.ShipsList.Cast<BOTF3D.GamePlay.ShipController>().ToList());

            // Set up BottomSlot with the new fleet (currently empty, but sets BottomFleet reference)
            shipDeployMenuUIController.SetUpBottomShipLists(newFleet, true);

            var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
            if (starSystCon.StarSysUIGameObject != null)
            {
                starSystCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
                starSystCon.StarSysUIGameObject.transform.SetAsLastSibling();
                starSystCon.StarSysUIGameObject.SetActive(true); // ✅ ACTIVATE!
            }

            newFleet.FleetUIGameObject.transform.SetParent(aSysView.transform, false);
            newFleet.FleetUIGameObject.SetActive(true); // ✅ ACTIVATE!

            Debug.Log($"ShowShipDeployForSystemNewFleet: TopStarSyst ShipListUIParent={(starSystCon.StarSysData?.ShipListUIParent != null ? "SET" : "NULL")}, BottomFleet ShipListUIParent={(newFleet.FleetData?.ShipListUIParent != null ? "SET" : "NULL")}");
        }
        internal void ShowShipDeployForFleetNewFleet(FleetController originalFleetCon, FleetController newFleetController)
        {
            if (shipDeployMenuUIController == null) return;
            // no GalaxyClickMode. this is new fleet button click;
            Debug.Log($"ShowShipDeployForFleetNewFleet: opening deploy UI for original='{originalFleetCon?.name}' new='{newFleetController?.name}'");

            MousePointerChanger.Instance.ResetCursor();

            // ✅ CRITICAL: Ensure BOTH fleets have ShipListUIParent set up BEFORE proceeding
            bool originalFleetReady = EnsureFleetShipListUIParent(originalFleetCon);
            bool newFleetReady = EnsureFleetShipListUIParent(newFleetController);

            if (!originalFleetReady || !newFleetReady)
            {
                Debug.LogError($"ShowShipDeployForFleetNewFleet: Fleet(s) not ready! originalReady={originalFleetReady}, newReady={newFleetReady}");
                // Wait a frame and try again
                StartCoroutine(RetryShowShipDeployForFleetNewFleet(originalFleetCon, newFleetController));
                return;
            }

            shipDeployMenuUIController.gameObject.SetActive(true);
            shipDeployMenuUIController.ShowShipDeployMenuView();

            // Set up TopSlot with original fleet's ships - cast to resolve namespace issue
            shipDeployMenuUIController.SetUpTopShipLists(originalFleetCon.FleetData.ShipsList.Cast<BOTF3D.GamePlay.ShipController>().ToList());

            // CRITICAL FIX: Set up BottomSlot with the new fleet (currently empty, but sets BottomFleet reference)
            shipDeployMenuUIController.SetUpBottomShipLists(newFleetController, true);

            var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
            if (originalFleetCon.FleetUIGameObject != null)
            {
                originalFleetCon.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                originalFleetCon.FleetUIGameObject.transform.SetAsLastSibling();
                originalFleetCon.FleetUIGameObject.SetActive(true); // ✅ ACTIVATE!

                Debug.Log($"  Original fleet UI '{originalFleetCon.FleetUIGameObject.name}' parented to AFleetMenuView and ACTIVATED");
            }

            if (newFleetController.FleetUIGameObject != null)
            {
                newFleetController.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
                newFleetController.FleetUIGameObject.transform.SetAsLastSibling();
                newFleetController.FleetUIGameObject.SetActive(true); // ✅ ACTIVATE!

                Debug.Log($"  New fleet UI '{newFleetController.FleetUIGameObject.name}' parented to AFleetMenuView and ACTIVATED");
            }

            Debug.Log($"ShowShipDeployForFleetNewFleet: TopFleet ShipListUIParent={(originalFleetCon.FleetData?.ShipListUIParent != null ? "SET" : "NULL")}, BottomFleet ShipListUIParent={(shipDeployMenuUIController.BottomFleet?.FleetData?.ShipListUIParent != null ? "SET" : "NULL")}");
        }

        /// <summary>
        /// Ensures a fleet has its ShipListUIParent set up
        /// </summary>
        /// <returns>True if setup succeeded, false otherwise</returns>
        private bool EnsureFleetShipListUIParent(FleetController fleet)
        {
            if (fleet == null) return false;
            if (fleet.FleetData == null) return false;

            // Already set up?
            if (fleet.FleetData.ShipListUIParent != null) return true;

            Debug.LogWarning($"Fleet '{fleet.name}' missing ShipListUIParent - setting it up now");

            var uiFields = fleet.FleetUIGameObject?.GetComponent<FleetUI_Fields>();
            if (uiFields != null && uiFields.FleetShipContentGO != null)
            {
                fleet.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;
                Debug.Log($"✅ Set ShipListUIParent for fleet '{fleet.name}'");
                return true;
            }
            else
            {
                Debug.LogError($"❌ Cannot find FleetShipContentGO for fleet '{fleet.name}'!");
                return false;
            }
        }

        /// <summary>
        /// Retry showing ship deploy after a frame delay (allows UI to fully initialize)
        /// </summary>
        private System.Collections.IEnumerator RetryShowShipDeployForFleetNewFleet(FleetController originalFleetCon, FleetController newFleetController)
        {
            Debug.Log("RetryShowShipDeployForFleetNewFleet: Waiting one frame...");
            yield return null; // Wait one frame

            // Try again
            ShowShipDeployForFleetNewFleet(originalFleetCon, newFleetController);
        }

        public void HideShipDeployMenu()
        {
            if (shipDeployMenuUIController == null) return;
            shipDeployMenuUIController.HideShipDeployMenuView();
            shipDeployMenuUIController.gameObject.SetActive(false);
            ResetClickMode();
            MousePointerChanger.Instance.ResetCursor();
        }

        public void SetActiveBuildMenu(GameObject prefabMenu)
        {
            sysBuildMenu = prefabMenu;
            sysBuildMenu.SetActive(true);
        }

        public void CloseTheBackgrounds()
        {
            sysBackground.SetActive(false);
            fleetsBackground.SetActive(false);
            diplomacyBackground.SetActive(false);
            intelBackground.SetActive(false);
            encyclopediaBackground.SetActive(false);
        }

        public void FleetButtonPressed() // The CanvasGalaxyMenuRibbon/MainGalaxyMenuPanel/FleetButton in the Hierarchy is set to this class.method
        {
            CloseShipDeployMenu();
            OpenMenu(Menu.FleetMenu, gameObject);
        }
        public void DiplomacyButtonPressed()
        {
            CloseShipDeployMenu();
            OpenMenu(Menu.DiplomacyMenu, gameObject);
        }
        public void IntelButtonPressed()
        {
            CloseShipDeployMenu();
            if (intelMenuView.activeSelf)
                CloseMenu(Menu.IntellMenu);
            else
            {
                CloseMenu(Menu.IntellMenu);
                OpenMenu(Menu.IntellMenu, null);
            }

        }
        public void EncyclopediaButtonPressed()
        {
            CloseShipDeployMenu();
            if (encyclopediaMenuView.activeSelf)
                CloseMenu(Menu.EncyclopedianMenu);
            else
            {
                OpenMenu(Menu.EncyclopedianMenu, null);
            }
        }

        // jump to Home System is in GalaxyCameraDragMoveZoom.cs
        public void CloseShipDeployMenu()
        {
            Debug.Log("=== CloseShipDeployMenu: Starting ===");

            // ✅ CRITICAL: If ship deploy panel is open, COMMIT changes first!
            if (ShipDeployMenuUIController.Instance != null &&
                ShipDeployMenuUIController.Instance.ShipDeployPanel != null &&
                ShipDeployMenuUIController.Instance.ShipDeployPanel.activeSelf)
            {
                Debug.Log("  Ship Deploy Panel IS OPEN - checking if changes need to be committed");

                // ✅ NEW: Check if any ships were actually moved between slots
                bool hasChanges = CheckIfShipsWereMoved();

                if (hasChanges)
                {
                    Debug.Log("  Changes detected - committing");

                    // Determine if this is merge or deploy
                    bool isMergeMode = (FleetLookingForShipMerge != null || StarSystLookingForShipMerge != null);

                    if (isMergeMode)
                    {
                        Debug.Log("  Mode: MERGE");
                        ShipDeployMenuUIController.Instance.CommitMergeAndClose(AfterCommitCleanup);
                    }
                    else  // ✅ UNCOMMENTED!
                    {
                        Debug.Log("  Mode: DEPLOY/NEW FLEET");
                        ShipDeployMenuUIController.Instance.CommitShipDeployAndClose(AfterCommitCleanup);
                    }
                }
                else
                {
                    Debug.Log("  No changes detected - just canceling");
                    // Just cancel without committing - ships stay where they were
                    AfterCommitCleanup();
                }

                return; // Exit early - commit methods handle the rest
            }
            else
            {
                Debug.Log("  Ship Deploy Panel NOT open - normal cleanup");
            }

            // Normal cleanup when ship deploy isn't open
            AfterCommitCleanup();
        }

        /// <summary>
        /// Cleanup after ship deploy commits OR when ship deploy wasn't open
        /// </summary>
        private void AfterCommitCleanup()
        {
            Debug.Log("=== AfterCommitCleanup: Starting ===");

            // ✅ CRITICAL: Move UIs back BEFORE hiding views
            // This ensures fleet/system UIs are in their containers before we close the detail views
            FleetMenuUIController.Instance?.MoveBackAnyaFleetUIGO();
            StarSysMenuUIController.Instance?.MoveBackAnyStarSysUIGO(); // ✅ FIXED: Correct method name

            // ✅ Now close ALL menu views (both list AND detail views)
            if (FleetMenuUIController.Instance != null)
            {
                FleetMenuUIController.Instance.HideFleetMenuView(); // Close fleet list view
                FleetMenuUIController.Instance.HideA_FleetMenuView(); // Close fleet detail view
                Debug.Log("  Closed FleetMenuView and AFleetMenuView");
            }

            if (StarSysMenuUIController.Instance != null)
            {
                StarSysMenuUIController.Instance.HideSystemMenuView(); // Close system list view
                StarSysMenuUIController.Instance.HideA_SystemMenuView(); // Close system detail view
                Debug.Log("  Closed SystemMenuView and ASystemMenuView");
            }

            // ✅ Double-check that no system UIs are children of fleet containers
            ValidateUIHierarchy();

            // ✅ Force refresh of ship UIs in fleet and system containers
            RefreshShipUIsInContainers();


            // Hide ship deploy if still visible
            if (ShipDeployMenuUIController.Instance != null)
            {
                ShipDeployMenuUIController.Instance.HideShipDeployMenuView();
                ShipDeployMenuUIController.Instance.gameObject.SetActive(false);
            }

            // ✅ Close the backgrounds since all views are closed
            CloseTheBackgrounds();

            // ✅ Update openMenuEnumWas to reflect that all views are closed
            openMenuEnumWas = Menu.None;
            openMenuWas = null;
            Debug.Log("  Set openMenuEnumWas to None (all views closed)");

            // Reset all tracking variables
            FleetLookingForShipDeploy = null;
            FleetSelectedForShipDeploy = null;
            StarSystLookingForShipDeploy = null;
            StarSystSelectedForShipDeploy = null;
            FleetLookingForShipMerge = null;
            FleetSelectedForShipMerge = null;
            StarSystLookingForShipMerge = null;
            StarSystSelectedForShipMerge = null;

            // Reset cursor and click mode
            ResetClickMode();
            MousePointerChanger.Instance?.ResetCursor();

            Debug.Log("=== AfterCommitCleanup: Complete ===");
        }

        /// <summary>
        /// Forces ship UIs to be visible and properly parented in their fleet/system containers
        /// </summary>
        private void RefreshShipUIsInContainers()
        {
            Debug.Log("RefreshShipUIsInContainers: Starting...");

            if (FleetManager.Instance != null)
            {
                var fleets = FleetManager.Instance.FleetControllerList.ToList();

                foreach (var fleetCon in fleets)
                {
                    if (fleetCon == null)
                    {
                        Debug.Log("  Skipping null fleet reference");
                        continue;
                    }

                    if (fleetCon.FleetData == null)
                    {
                        Debug.LogWarning($"  Fleet '{fleetCon.name}' has null FleetData - skipping");
                        continue;
                    }

                    // ✅ If fleet has no ships, mark for destruction
                    if (fleetCon.FleetData.ShipsList.Count == 0)
                    {
                        Debug.Log($"  Fleet '{fleetCon.name}' has no ships - will be destroyed");
                        FleetManager.Instance.DestroyFleetController(fleetCon);
                        continue;
                    }

                    var shipListParent = fleetCon.FleetData.ShipListUIParent;
                    if (shipListParent == null)
                    {
                        Debug.LogWarning($"  Fleet '{fleetCon.name}' has null ShipListUIParent - skipping");
                        continue;
                    }

                    Debug.Log($"  Fleet '{fleetCon.name}': {fleetCon.FleetData.ShipsList.Count} ships in data");

                    // ✅ CRITICAL: Use ShipController hierarchy as source of truth
                    var shipsInGalaxy = fleetCon.GetComponentsInChildren<ShipController>(true);
                    Debug.Log($"    Found {shipsInGalaxy.Length} ShipControllers in hierarchy");

                    foreach (var ship in shipsInGalaxy)
                    {
                        if (ship == null) continue;

                        // ✅ Ensure ship is in the fleet's ShipsList
                        if (!fleetCon.FleetData.ShipsList.Contains(ship))
                        {
                            Debug.LogWarning($"    Ship '{ship.ShipData.ShipName}' in hierarchy but NOT in ShipsList - adding!");
                            fleetCon.FleetData.ShipsList.Add(ship);
                        }

                        // ✅ Ensure ship UI exists
                        if (ship.ShipListUIGameObject == null)
                        {
                            Debug.LogWarning($"    Ship '{ship.ShipData.ShipName}' has no UI - creating!");
                            ShipManager.Instance?.InstantiateShipListUIGameObject(ship, fleetCon.gameObject);
                            ShipManager.Instance?.ProcessPendingShipUIs();
                        }

                        // ✅ Ensure ship UI is parented correctly
                        if (ship.ShipListUIGameObject != null)
                        {
                            var currentParent = ship.ShipListUIGameObject.transform.parent;

                            if (currentParent != shipListParent.transform)
                            {
                                Debug.Log($"    Moving ship UI '{ship.ShipData.ShipName}' from '{currentParent?.name}' to correct parent '{shipListParent.name}'");
                                ship.ShipListUIGameObject.transform.SetParent(shipListParent.transform, false);
                            }

                            // ✅ Ensure ship UI is active and visible
                            if (!ship.ShipListUIGameObject.activeSelf)
                            {
                                ship.ShipListUIGameObject.SetActive(true);
                                Debug.Log($"    Activated ship UI '{ship.ShipData.ShipName}'");
                            }
                        }
                    }

                    // ✅ Also check ShipsList and remove any null entries
                    fleetCon.FleetData.ShipsList.RemoveAll(s => s == null);
                }
            }

            if (StarSysManager.Instance != null)
            {
                foreach (var sysCon in StarSysManager.Instance.StarSysControllerList)
                {
                    if (sysCon == null)
                    {
                        Debug.LogError("  ❌ NULL STAR SYSTEM FOUND! This should never happen!");
                        continue;
                    }

                    if (sysCon.StarSysData == null)
                    {
                        Debug.LogError($"  ❌ System '{sysCon.name}' has null StarSysData!");
                        continue;
                    }

                    var shipListParent = sysCon.StarSysData.ShipListUIParent;
                    if (shipListParent == null) continue;

                    Debug.Log($"  System '{sysCon.name}': {sysCon.StarSysData.ShipsList.Count} ships in data");

                    // ✅ CRITICAL: Use ShipController hierarchy as source of truth
                    var shipsInGalaxy = sysCon.GetComponentsInChildren<ShipController>(true);
                    Debug.Log($"    Found {shipsInGalaxy.Length} ShipControllers in hierarchy");

                    foreach (var ship in shipsInGalaxy)
                    {
                        if (ship == null) continue;

                        // ✅ Ensure ship is in the system's ShipsList
                        if (!sysCon.StarSysData.ShipsList.Contains(ship))
                        {
                            Debug.LogWarning($"    Ship '{ship.ShipData.ShipName}' in hierarchy but NOT in ShipsList - adding!");
                            sysCon.StarSysData.ShipsList.Add(ship);
                        }

                        // ✅ Ensure ship UI exists
                        if (ship.ShipListUIGameObject == null)
                        {
                            Debug.LogWarning($"    Ship '{ship.ShipData.ShipName}' has no UI - creating!");
                            ShipManager.Instance?.InstantiateShipListUIGameObject(ship, sysCon.gameObject);
                            ShipManager.Instance?.ProcessPendingShipUIs();
                        }

                        // ✅ Ensure ship UI is parented correctly
                        if (ship.ShipListUIGameObject != null)
                        {
                            var currentParent = ship.ShipListUIGameObject.transform.parent;

                            if (currentParent != shipListParent.transform)
                            {
                                Debug.Log($"    Moving ship UI '{ship.ShipData.ShipName}' from '{currentParent?.name}' to correct parent '{shipListParent.name}'");
                                ship.ShipListUIGameObject.transform.SetParent(shipListParent.transform, false);
                            }

                            // ✅ Ensure ship UI is active and visible
                            if (!ship.ShipListUIGameObject.activeSelf)
                            {
                                ship.ShipListUIGameObject.SetActive(true);
                                Debug.Log($"    Activated ship UI '{ship.ShipData.ShipName}'");
                            }
                        }
                    }

                    // ✅ Also check ShipsList and remove any null entries
                    sysCon.StarSysData.ShipsList.RemoveAll(s => s == null);
                }
            }

            Debug.Log("RefreshShipUIsInContainers: Complete");
        }

        /// <summary>
        /// Validates that system UIs are NOT children of fleet containers and vice versa.
        /// ONLY validates LOCAL PLAYER's systems/fleets!
        /// </summary>
        private void ValidateUIHierarchy()
        {
            Debug.Log("ValidateUIHierarchy: Checking for cross-contamination...");

            var aSystemView = StarSysMenuUIController.Instance?.ASystemMenuView;
            var aFleetView = FleetMenuUIController.Instance?.AFleetMenuView;

            // ✅ Check AFleetMenuView for any system UIs (WRONG!)
            if (aFleetView != null)
            {
                for (int i = aFleetView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = aFleetView.transform.GetChild(i);
                    if (child == null)
                    {
                        Debug.Log("  Skipping null child reference");
                        continue;
                    }

                    var sysUIFields = child.GetComponent<StarSysUI_Fields>();

                    if (sysUIFields != null)
                    {
                        Debug.LogError($"  ❌ SYSTEM UI '{child.name}' found in AFleetMenuView! Moving to home storage.");

                        // Move to home storage
                        var homeContainer = StarSysManager.Instance?.StarSysUI_ListContainer;
                        if (homeContainer != null)
                        {
                            child.SetParent(homeContainer.transform, false);
                            child.gameObject.SetActive(false);
                        }
                    }
                }
            }

            // ✅ Check ASystemMenuView for any fleet UIs (could be valid during ship deploy!)
            if (aSystemView != null)
            {
                for (int i = aSystemView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = aSystemView.transform.GetChild(i);
                    if (child == null) continue;

                    var fleetUIFields = child.GetComponent<FleetUI_Fields>();

                    // Fleet UI in system view is OK during ship deploy/merge operations
                    if (fleetUIFields != null)
                    {
                        Debug.Log($"  Fleet UI '{child.name}' in ASystemMenuView (OK during ship operations)");
                    }
                }
            }

            // ✅ CRITICAL FIX: Check all LOCAL PLAYER star systems (skip non-player systems!)
            if (StarSysManager.Instance != null)
            {
                foreach (var sysCon in StarSysManager.Instance.StarSysControllerList)
                {
                    // ✅ Star systems should NEVER be null
                    if (sysCon == null)
                    {
                        Debug.LogError("  ❌ NULL STAR SYSTEM in StarSysControllerList! This is a critical bug!");
                        continue;
                    }

                    // ✅ CRITICAL: Skip non-player systems - they SHOULD NOT have UIs!
                    if (!GameController.Instance.AreWeLocalPlayer(sysCon.StarSysData.CurrentOwnerCivEnum))
                    {
                        // This is normal - non-player systems don't have UIs
                        continue;
                    }

                    // ✅ NOW check if LOCAL PLAYER system has null UI (this IS an error!)
                    if (sysCon.StarSysUIGameObject == null)
                    {
                        Debug.LogError($"  ❌ LOCAL PLAYER system '{sysCon.name}' has null StarSysUIGameObject! This should NEVER happen!");

                        // Try to recover from tracking list
                        if (StarSysMenuUIController.Instance != null)
                        {
                            var foundUI = TryRecoverSystemUI(sysCon);

                            if (foundUI != null)
                            {
                                sysCon.StarSysUIGameObject = foundUI;
                                Debug.Log($"    ✅ RECOVERED UI for '{sysCon.name}'");
                            }
                            else
                            {
                                Debug.LogError($"    ❌ RECOVERY FAILED for '{sysCon.name}'! UI must be recreated.");
                                // UI was destroyed - should recreate it
                                if (StarSysManager.Instance != null)
                                {
                                    StarSysManager.Instance.InstantiateStarSysUI(sysCon);
                                    Debug.Log($"    ✅ RECREATED UI for '{sysCon.name}'");
                                }
                            }
                        }

                        continue;
                    }

                    // ✅ Check if UI is wrongly parented
                    var parent = sysCon.StarSysUIGameObject.transform.parent;
                    if (parent == null) continue;

                    // Check if parent is a fleet UI (individual fleet UI, not AFleetMenuView)
                    var fleetUIFields = parent.GetComponent<FleetUI_Fields>();
                    if (fleetUIFields != null)
                    {
                        Debug.LogError($"  ❌ SYSTEM UI '{sysCon.StarSysUIGameObject.name}' is a child of FLEET UI '{parent.name}'! Fixing...");

                        var homeContainer = StarSysManager.Instance?.StarSysUI_ListContainer;
                        if (homeContainer != null)
                        {
                            sysCon.StarSysUIGameObject.transform.SetParent(homeContainer.transform, false);
                            sysCon.StarSysUIGameObject.SetActive(false);
                            Debug.Log($"    Moved back to home storage");
                        }
                    }
                }
            }

            // ✅ Check all LOCAL PLAYER fleets (skip non-player fleets!)
            if (FleetManager.Instance != null)
            {
                // ✅ Work on a copy and remove nulls
                var fleets = FleetManager.Instance.FleetControllerList.ToList();
                FleetManager.Instance.FleetControllerList.RemoveAll(f => f == null);

                int nullCount = fleets.Count - FleetManager.Instance.FleetControllerList.Count;
                if (nullCount > 0)
                {
                    Debug.Log($"  Removed {nullCount} destroyed fleet references from FleetControllerList");
                }

                foreach (var fleetCon in FleetManager.Instance.FleetControllerList)
                {
                    if (fleetCon == null) continue;

                    // ✅ CRITICAL: Skip non-player fleets - they SHOULD NOT have UIs!
                    if (!GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum))
                    {
                        // This is normal - non-player fleets don't have UIs
                        continue;
                    }

                    // ✅ Local player fleet with null UI - this IS an error
                    if (fleetCon.FleetUIGameObject == null)
                    {
                        Debug.LogWarning($"  ⚠️ LOCAL PLAYER fleet '{fleetCon.name}' has null FleetUIGameObject");
                        continue;
                    }

                    var parent = fleetCon.FleetUIGameObject.transform.parent;
                    if (parent == null) continue;

                    // Check if parent is a system UI (individual system UI, not ASystemMenuView)
                    var sysUIFields = parent.GetComponent<StarSysUI_Fields>();
                    if (sysUIFields != null)
                    {
                        Debug.LogError($"  ❌ FLEET UI '{fleetCon.FleetUIGameObject.name}' is a child of SYSTEM UI '{parent.name}'! Fixing...");

                        var homeContainer = FleetManager.Instance?.FleetUI_ListContainer;
                        if (homeContainer != null)
                        {
                            fleetCon.FleetUIGameObject.transform.SetParent(homeContainer.transform, false);
                            fleetCon.FleetUIGameObject.SetActive(false);
                            Debug.Log($"    Moved back to home storage");
                        }
                    }
                }
            }

            Debug.Log("ValidateUIHierarchy: Complete");
        }

        /// <summary>
        /// Helper: Tries to find an orphaned UI in the tracking list
        /// </summary>
        private GameObject TryRecoverSystemUI(StarSysController sysCon)
        {
            if (StarSysMenuUIController.Instance == null) return null;

            // Use reflection to access private listOfStarSysUiGos field
            var trackingList = StarSysMenuUIController.Instance.GetType()
                .GetField("listOfStarSysUiGos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(StarSysMenuUIController.Instance) as List<GameObject>;

            if (trackingList != null)
            {
                foreach (var ui in trackingList)
                {
                    if (ui == null) continue;

                    var uiFields = ui.GetComponent<StarSysUI_Fields>();
                    if (uiFields != null && uiFields.sysName?.text == sysCon.StarSysData?.SysName)
                    {
                        return ui;
                    }
                }
            }

            return null;
        }

        public void OpenMenu(Menu menuEnum, GameObject callingMenuOrGalaxyObject)
        {
            if (openMenuWas != null)
            {
                openMenuWas.SetActive(false);
                CloseMenu(openMenuEnumWas);
            }
            switch (menuEnum)
            {
                case Menu.None:
                    openMenuWas = null;
                    break;
                case Menu.SystemsMenu:
                    HideShipDeployMenu();
                    starSysMenuUIController.ShowSystemMenuView();
                    CloseTheBackgrounds();
                    sysBackground.SetActive(true);
                    starSysMenuUIController.MoveBackAnyStarSysUIGO(); // ✅ FIXED: Correct method name
                    openMenuWas = null;
                    openMenuEnumWas = Menu.SystemsMenu;
                    break;
                case Menu.ASystemMenu:
                    HideShipDeployMenu();

                    // ✅ VALIDATE: Ensure calling object has StarSysController, not FleetController
                    var starSysCon = callingMenuOrGalaxyObject?.GetComponentInChildren<StarSysController>();
                    if (starSysCon == null)
                    {
                        Debug.LogError($"OpenMenu(ASystemMenu): GameObject '{callingMenuOrGalaxyObject?.name}' has no StarSysController! Cannot open system menu.");
                        return;
                    }

                    starSysMenuUIController.ShowA_SystemMenuView();
                    CloseTheBackgrounds();
                    starSysMenuUIController.SetActiveSetParentUIGO(starSysCon); // This already moves the UI!
                    sysBackground.SetActive(true);
                    // ✅ REMOVED: starSysMenuUIController.MoveTheSysUIGO(callingMenuOrGalaxyObject);
                    // SetActiveSetParentUIGO() already handles parenting to ASystemMenuView
                    openMenuWas = null;
                    openMenuEnumWas = Menu.ASystemMenu;
                    break;
                case Menu.BuildMenu:
                    HideShipDeployMenu();
                    InactivateCallingMenu(callingMenuOrGalaxyObject);
                    sysBuildMenu.SetActive(true);
                    openMenuWas = sysBuildMenu;
                    openMenuEnumWas = Menu.BuildMenu;
                    break;
                case Menu.FleetMenu:
                    HideShipDeployMenu();
                    fleetMenuUIController.ShowFleetMenuView();
                    CloseTheBackgrounds();
                    fleetsBackground.SetActive(true);
                    fleetMenuUIController.MoveBackAnyaFleetUIGO();
                    openMenuWas = null;
                    openMenuEnumWas = Menu.FleetMenu;
                    break;
                case Menu.AFleetMenu:
                    HideShipDeployMenu();

                    // ✅ VALIDATE: Ensure calling object has FleetController, not StarSysController
                    var fleetCon = callingMenuOrGalaxyObject?.GetComponentInChildren<FleetController>();
                    if (fleetCon == null)
                    {
                        Debug.LogError($"OpenMenu(AFleetMenu): GameObject '{callingMenuOrGalaxyObject?.name}' has no FleetController! Cannot open fleet menu.");
                        return;
                    }

                    // ✅ VALIDATE: Ensure it's not a system pretending to be a fleet
                    var wrongStarSys = callingMenuOrGalaxyObject?.GetComponent<StarSysController>();
                    if (wrongStarSys != null)
                    {
                        Debug.LogError($"OpenMenu(AFleetMenu): GameObject '{callingMenuOrGalaxyObject?.name}' IS A STAR SYSTEM! Opening system menu instead.");
                        OpenMenu(Menu.ASystemMenu, callingMenuOrGalaxyObject);
                        return;
                    }

                    fleetMenuUIController.ShowA_FleetMenuView();
                    CloseTheBackgrounds();
                    fleetMenuUIController.SetActiveSetParentUIGO(fleetCon);
                    fleetsBackground.SetActive(true);
                    fleetMenuUIController.MoveTheFleetUIGO(callingMenuOrGalaxyObject);
                    openMenuWas = null;
                    openMenuEnumWas = Menu.AFleetMenu;
                    break;
                case Menu.ShipDeployMenu:
                    //HideShipDeployMenu();
                    shipDeployMenuUIController.ShowShipDeployMenuView();
                    openMenuWas = shipDeployMenuUIController.gameObject;
                    openMenuEnumWas = Menu.ShipDeployMenu;
                    break;
                case Menu.DiplomacyMenu:
                    HideShipDeployMenu();
                    diplomacyMenuUIController.ShowDiplomacyMenuView();
                    CloseTheBackgrounds();
                    diplomacyBackground.SetActive(true);
                    TimeManager.Instance.PauseTime();
                    diplomacyMenuUIController.MoveBackAnyDiplomacyUIGO();
                    openMenuWas = null;
                    openMenuEnumWas = Menu.DiplomacyMenu;
                    break;
                case Menu.ADiplomacyMenu:
                    HideShipDeployMenu();
                    diplomacyMenuUIController.ShowA_DiplomacyMenuView();
                    CloseTheBackgrounds();
                    TimeManager.Instance.PauseTime();
                    diplomacyMenuUIController.SetActiveSetParentADiplomacyUIData(callingMenuOrGalaxyObject.GetComponentInChildren<DiplomacyController>());
                    diplomacyBackground.SetActive(true);
                    diplomacyMenuUIController.MoveTheDiplomacyUIGO(callingMenuOrGalaxyObject);
                    openMenuWas = null;
                    openMenuEnumWas = Menu.ADiplomacyMenu;
                    break;
                case Menu.IntellMenu:
                    HideShipDeployMenu();
                    CloseTheBackgrounds();
                    intelMenuView.SetActive(true);
                    intelBackground.SetActive(true);
                    openMenuWas = intelMenuView;
                    openMenuEnumWas = Menu.IntellMenu;
                    break;
                case Menu.EncyclopedianMenu:
                    HideShipDeployMenu();
                    CloseTheBackgrounds();
                    InactivateCallingMenu(callingMenuOrGalaxyObject);
                    encyclopediaMenuView.SetActive(true);
                    encyclopediaBackground.SetActive(true);
                    openMenuWas = encyclopediaMenuView;
                    openMenuEnumWas = Menu.EncyclopedianMenu;
                    break;
                case Menu.HabitableSysMenu:
                    HideShipDeployMenu();
                    habitableSysMenu.SetActive(true);
                    openMenuWas = habitableSysMenu;
                    openMenuEnumWas = Menu.HabitableSysMenu;
                    break;
                case Menu.Combat:
                    break;
                default:
                    break;
            }
        }
        internal void WhatFleetIsSelectedForShipDiploy(FleetController fleetController)
        {
            FleetSelectedForShipDeploy = fleetController;
            StarSystSelectedForShipDeploy = null;
        }
        internal void WhatFleetIsSelectedForShipMerge(FleetController fleetController)
        {
            FleetSelectedForShipMerge = fleetController;
            StarSystSelectedForShipMerge = null;
        }
        internal void WhatSystemIsSelectedForShipDeploy(StarSysController starSysController)
        {
            StarSystSelectedForShipDeploy = starSysController;
            FleetSelectedForShipDeploy = null;
        }
        internal void WhatSystemIsSelectedForShipMerge(StarSysController starSysController)
        {
            StarSystSelectedForShipMerge = starSysController;
            FleetSelectedForShipMerge = null;
        }
        private void MoveBackShipUIGO()
        {
            Debug.Log($"MoveBackShipUIGO: FleetLooking={FleetLookingForShipDeploy?.name}, StarSystLooking={StarSystLookingForShipDeploy?.name}, FleetSelected={FleetSelectedForShipDeploy?.name}, StarSystSelected={StarSystSelectedForShipDeploy?.name}");

            // ✅ NEW: Get TopFleet/TopStarSyst and BottomFleet/BottomStarSyst from ShipDeployMenuUIController
            var shipDeployUI = ShipDeployMenuUIController.Instance;
            if (shipDeployUI != null)
            {
                // Process Top slot ships
                if (shipDeployUI.TopFleet != null)
                {
                    Debug.Log($"  Moving TopFleet '{shipDeployUI.TopFleet.name}' ships back");
                    GameObject shipListParent = shipDeployUI.TopFleet.FleetData?.ShipListUIParent;
                    if (shipListParent != null)
                    {
                        var shipUIGOs = shipDeployUI.GetTopSlotShipListUIGOs();
                        foreach (var shipUI in shipUIGOs)
                        {
                            if (shipUI != null)
                            {
                                shipUI.transform.SetParent(shipListParent.transform, false);
                                Debug.Log($"    Moved ship UI back to TopFleet ShipListUIParent");
                            }
                        }
                    }
                }
                else if (shipDeployUI.TopStarSyst != null)
                {
                    Debug.Log($"  Moving TopStarSyst '{shipDeployUI.TopStarSyst.name}' ships back");
                    GameObject shipListParent = shipDeployUI.TopStarSyst.StarSysData?.ShipListUIParent;
                    if (shipListParent != null)
                    {
                        var shipUIGOs = shipDeployUI.GetTopSlotShipListUIGOs();
                        foreach (var shipUI in shipUIGOs)
                        {
                            if (shipUI != null)
                            {
                                shipUI.transform.SetParent(shipListParent.transform, false);
                                Debug.Log($"    Moved ship UI back to TopStarSyst ShipListUIParent");
                            }
                        }
                    }
                }

                // Process Bottom slot ships
                if (shipDeployUI.BottomFleet != null)
                {
                    Debug.Log($"  Moving BottomFleet '{shipDeployUI.BottomFleet.name}' ships back");
                    GameObject shipListParent = shipDeployUI.BottomFleet.FleetData?.ShipListUIParent;
                    if (shipListParent != null)
                    {
                        var shipUIGOs = shipDeployUI.GetBottomSlotShipListUIGOs();
                        foreach (var shipUI in shipUIGOs)
                        {
                            if (shipUI != null)
                            {
                                shipUI.transform.SetParent(shipListParent.transform, false);
                                Debug.Log($"    Moved ship UI back to BottomFleet ShipListUIParent");
                            }
                        }
                    }
                }
                else if (shipDeployUI.BottomStarSyst != null)
                {
                    Debug.Log($"  Moving BottomStarSyst '{shipDeployUI.BottomStarSyst.name}' ships back");
                    GameObject shipListParent = shipDeployUI.BottomStarSyst.StarSysData?.ShipListUIParent;
                    if (shipListParent != null)
                    {
                        var shipUIGOs = shipDeployUI.GetBottomSlotShipListUIGOs();
                        foreach (var shipUI in shipUIGOs)
                        {
                            if (shipUI != null)
                            {
                                shipUI.transform.SetParent(shipListParent.transform, false);
                                Debug.Log($"    Moved ship UI back to BottomStarSyst ShipListUIParent");
                            }
                        }
                    }
                }
            }

            // ✅ Keep existing logic as fallback (in case the new approach misses something)
            if (FleetLookingForShipDeploy != null)
            {
                GameObject fleetShipListParentGO = FleetLookingForShipDeploy.FleetData.ShipListUIParent;
                if (fleetShipListParentGO != null)
                {
                    var shipUIGOs = ShipDeployMenuUIController.Instance.GetTopSlotShipListUIGOs().ToList();
                    for (int i = 0; i < shipUIGOs.Count; i++)
                    {
                        shipUIGOs[i].transform.SetParent(fleetShipListParentGO.transform, false);
                    }
                }
            }
            else if (StarSystLookingForShipDeploy != null)
            {
                GameObject starSysShipListParentGO = StarSystLookingForShipDeploy.StarSysData.ShipListUIParent;
                if (starSysShipListParentGO != null)
                {
                    var shipUIGOs = ShipDeployMenuUIController.Instance.GetTopSlotShipListUIGOs().ToList();
                    for (int i = 0; i < shipUIGOs.Count; i++)
                    {
                        shipUIGOs[i].transform.SetParent(starSysShipListParentGO.transform, false);
                    }
                }
            }

            if (FleetSelectedForShipDeploy != null)
            {
                GameObject fleetShipListParentGO = FleetSelectedForShipDeploy.FleetData.ShipListUIParent;
                if (fleetShipListParentGO != null)
                {
                    var shipUIGOs = ShipDeployMenuUIController.Instance.GetBottomSlotShipListUIGOs().ToList();
                    for (int i = 0; i < shipUIGOs.Count; i++)
                    {
                        shipUIGOs[i].transform.SetParent(fleetShipListParentGO.transform, false);
                    }
                }
            }
            else if (StarSystSelectedForShipDeploy != null)
            {
                GameObject starSysShipListParentGO = StarSystSelectedForShipDeploy.StarSysData.ShipListUIParent;
                if (starSysShipListParentGO != null)
                {
                    var shipUIGOs = ShipDeployMenuUIController.Instance.GetBottomSlotShipListUIGOs().ToList();
                    for (int i = 0; i < shipUIGOs.Count; i++)
                    {
                        shipUIGOs[i].transform.SetParent(starSysShipListParentGO.transform, false);
                    }
                }
            }

            Debug.Log("MoveBackShipUIGO: Complete");
        }

        private void InactivateCallingMenu(GameObject callingMenu)
        {
            if (callingMenu != null)
                callingMenu.SetActive(false);
        }
        public void CloseAllMenus()
        {
            // Close all menu backgrounds
            CloseTheBackgrounds();

            // Close ship deploy if open
            CloseShipDeployMenu();

            // Deactivate any open menu views

            if (sysBuildMenu != null && sysBuildMenu.activeSelf)
                sysBuildMenu.SetActive(false);

            if (habitableSysMenu != null && habitableSysMenu.activeSelf)
                habitableSysMenu.SetActive(false);

            if (diplomacyNoContacts != null && diplomacyNoContacts.activeSelf)
                diplomacyNoContacts.SetActive(false);

            if (intelMenuView != null && intelMenuView.activeSelf)
                intelMenuView.SetActive(false);

            if (encyclopediaMenuView != null && encyclopediaMenuView.activeSelf)
                encyclopediaMenuView.SetActive(false);

            // Reset click mode
            ResetClickMode();
        }
        public void CloseMenu(Menu enumMenu)
        {
            switch (enumMenu)
            {
                case Menu.None:
                    openMenuWas = null;
                    break;
                case Menu.SystemsMenu:
                    starSysMenuUIController?.HideSystemMenuView(); // ✅ Explicit hide
                    sysBackground.SetActive(false);
                    openMenuWas = null;
                    break;
                case Menu.ASystemMenu:
                    starSysMenuUIController?.MoveBackAnyStarSysUIGO(); // ✅ FIXED: Correct method name
                    starSysMenuUIController?.HideA_SystemMenuView(); // ✅ Explicit hide
                    sysBackground.SetActive(false);
                    openMenuWas = null;
                    break;
                case Menu.BuildMenu:
                    sysBuildMenu.SetActive(false);
                    openMenuWas = sysBuildMenu;
                    break;
                case Menu.FleetMenu:
                    fleetMenuUIController?.HideFleetMenuView(); // ✅ Explicit hide
                    fleetsBackground.SetActive(false);
                    fleetMenuUIController?.CloseDestinationSelectionCursor();
                    openMenuWas = null;
                    break;
                case Menu.AFleetMenu:
                    fleetMenuUIController?.MoveBackAnyaFleetUIGO();
                    fleetMenuUIController?.HideA_FleetMenuView(); // ✅ Explicit hide
                    fleetsBackground.SetActive(false);
                    fleetMenuUIController?.CloseDestinationSelectionCursor();
                    openMenuWas = null;
                    break;
                case Menu.ShipDeployMenu:
                    // ✅ CHANGED: Don't call MoveBackShipUIGO here - it happens in CloseShipDeploy
                    // which already commits changes
                    Debug.Log("CloseMenu(ShipDeployMenu): Skipping - should use CloseShipDeployMenu() instead");
                    openMenuWas = shipDeployMenuUIController?.gameObject;
                    break;
                case Menu.DiplomacyMenu:
                    diplomacyMenuUIController?.HideDiplomacyMenuView(); // ✅ Explicit hide
                    diplomacyBackground.SetActive(false);
                    TimeManager.Instance?.ResumeTime();
                    openMenuWas = null;
                    break;
                case Menu.ADiplomacyMenu:
                    diplomacyMenuUIController?.MoveBackAnyDiplomacyUIGO();
                    diplomacyMenuUIController?.HideA_DiplomacyMenuView(); // ✅ Explicit hide
                    TimeManager.Instance?.ResumeTime();
                    diplomacyBackground.SetActive(false);
                    openMenuWas = null;
                    break;
                case Menu.IntellMenu:
                    intelBackground.SetActive(false);
                    intelMenuView.SetActive(false);
                    openMenuWas = intelMenuView;
                    break;
                case Menu.EncyclopedianMenu:
                    encyclopediaBackground.SetActive(false);
                    encyclopediaMenuView.SetActive(false);
                    openMenuWas = encyclopediaMenuView;
                    break;
                case Menu.HabitableSysMenu:
                    habitableSysMenu.SetActive(false);
                    openMenuWas = habitableSysMenu;
                    break;
                case Menu.Combat:// close combat scenes
                    break;
                default:
                    break;
            }
        }

        public void FindTheirHomeSystem(CivController civCon, out StarSysController homeSystController)
        {
            homeSystController = null;
            List<StarSysController> SystemCons = civCon.CivData.StarSysOwned;
            for (int i = 0; i < SystemCons.Count; i++)
            {
                if (SystemCons[i].StarSysData.SysName == civCon.CivData.CivHomeSystemName)
                {
                    homeSystController = SystemCons[i];
                    return;
                }
            }
        }

        internal void HideNoContactUI()
        {
            diplomacyNoContacts.SetActive(false);
        }


        public void SetClickMode(GalaxyClickMode mode)
        {
            CurrentClickMode = mode;
            UpdateCursorForClickMode();
        }

        public void ResetClickMode()
        {
            SetClickMode(GalaxyClickMode.Normal);
        }

        private void UpdateCursorForClickMode()
        {
            switch (CurrentClickMode)
            {
                case GalaxyClickMode.Normal:
                    MousePointerChanger.Instance.ResetCursor();
                    break;

                case GalaxyClickMode.SetDestination:
                    MousePointerChanger.Instance.SetDestinationCursor();
                    break;

                case GalaxyClickMode.SelectForShipDeploy:
                    MousePointerChanger.Instance.SetShipExchangeCursor();
                    break;
            }
        }

        public void ClickCancelShipDeployButton() // button is both in fleet and system UI
        {
            MousePointerChanger.Instance.ResetCursor();
            CurrentClickMode = GalaxyClickMode.Normal;
            // sele.SetActive(true);
        }
        public void WhatFleetIsLookingForMerge(FleetController fleetConLooking)
        {
            FleetLookingForShipMerge = fleetConLooking;
            StarSystLookingForShipMerge = null;
            SetClickMode(GalaxyClickMode.SelectForShipMerge);
        }
        public void WhatFleetIsLookingForShipDeploy(FleetController fleetConLooking)
        {
            FleetLookingForShipDeploy = fleetConLooking;
            StarSystLookingForShipDeploy = null;
            SetClickMode(GalaxyClickMode.SelectForShipDeploy);
        }
        public void WhatSystIsLookingForMerge(StarSysController starSystConLooking)
        {
            StarSystLookingForShipMerge = starSystConLooking;
            FleetLookingForShipMerge = null;
            SetClickMode(GalaxyClickMode.SelectForShipMerge);
        }
        public void WhatSystIsLookingForShipDeploy(StarSysController starSystConLooking)
        {
            StarSystLookingForShipDeploy = starSystConLooking;
            FleetLookingForShipDeploy = null;
            SetClickMode(GalaxyClickMode.SelectForShipDeploy);
        }
        public void CompleteShipExchange()
        {
            ResetClickMode();
        }

        public void BeginSetDestination(FleetController fleetLooking)
        {
            if (fleetLooking == null) return;

            // Destroy any existing player-defined target
            if (fleetLooking.TargetController != null)
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetLooking);
            }
            FleetLookingForDestination = fleetLooking;
            SetClickMode(GalaxyClickMode.SelectForShipDeploy);
        }

        public void CompleteSetDestination()
        {
            FleetLookingForDestination = null;
            ResetClickMode();
        }


        /// <summary>
        /// Checks if any ships were actually moved between top and bottom slots
        /// </summary>
        private bool CheckIfShipsWereMoved()
        {
            var shipDeployUI = ShipDeployMenuUIController.Instance;
            if (shipDeployUI == null) return false;

            // Get the original ship lists
            List<ShipController> originalTopShips = new List<ShipController>();
            List<ShipController> originalBottomShips = new List<ShipController>();

            if (shipDeployUI.TopFleet != null)
            {
                originalTopShips = shipDeployUI.TopFleet.FleetData?.ShipsList ?? new List<ShipController>();
            }
            else if (shipDeployUI.TopStarSyst != null)
            {
                originalTopShips = shipDeployUI.TopStarSyst.StarSysData?.ShipsList ?? new List<ShipController>();
            }

            if (shipDeployUI.BottomFleet != null)
            {
                originalBottomShips = shipDeployUI.BottomFleet.FleetData?.ShipsList ?? new List<ShipController>();
            }
            else if (shipDeployUI.BottomStarSyst != null)
            {
                originalBottomShips = shipDeployUI.BottomStarSyst.StarSysData?.ShipsList ?? new List<ShipController>();
            }

            // Get current ship UIs in slots
            var topSlotShipUIs = shipDeployUI.GetTopSlotShipListUIGOs();
            var bottomSlotShipUIs = shipDeployUI.GetBottomSlotShipListUIGOs();

            // Count ships in each slot
            int topSlotCount = topSlotShipUIs?.Length ?? 0;
            int bottomSlotCount = bottomSlotShipUIs?.Length ?? 0;

            // If bottom started empty and still is, no changes
            if (originalBottomShips.Count == 0 && bottomSlotCount == 0)
            {
                Debug.Log($"  No changes: Bottom was empty and still is (top has {topSlotCount} ships)");
                return false;
            }

            // If counts changed, there were definitely changes
            if (topSlotCount != originalTopShips.Count || bottomSlotCount != originalBottomShips.Count)
            {
                Debug.Log($"  Changes detected: Top {originalTopShips.Count}→{topSlotCount}, Bottom {originalBottomShips.Count}→{bottomSlotCount}");
                return true;
            }

            Debug.Log($"  No changes detected");
            return false;
        }

        private void CurrentClickModeReset()
        {
            GalaxyMenuUIController.Instance.ResetClickMode();
        }
    }
}
