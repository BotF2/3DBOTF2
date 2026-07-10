// Ignore Spelling: Sys Anya

using BOTF3D.Civilization;
using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.Galaxy;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



namespace BOTF3D.UI
{
    /// <summary>
    /// The UI controller owns hierarchy and presentation.
    /// </summary>
    public class StarSysMenuUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        public static StarSysMenuUIController Instance;
        private StarSysController lastSysCon;
        public StarSysController ActiveStarSysController;
        [Header("References (assign in Inspector)")]
        public GameObject SystemsMenuView;
        public GameObject ASystemMenuView;
        public GameObject SysListContainer;
        [Header("Private")]
        [SerializeField] private GameObject sysShipListContainer;
        [SerializeField] private GameObject aSystemShipListContainer;
        [SerializeField] private FleetMenuUIController fleetMenuUIController; // used for parenting right-side ship UI
        [Header("Runtime lists")]
        [SerializeField] private List<GameObject> listOfStarSysUiGos = new List<GameObject>();
        [SerializeField] private List<GameObject> listOfSysShipUiGos = new List<GameObject>();
        [SerializeField] private GameObject cancelShipManagerButtonGO;
        [SerializeField] private FleetController tempFleetController;
        [Header("Power overload visuals")]
        public GameObject PowerOverloadImage;
        public Slider ShipSliderBuildProgress;
        public Slider SliderBuildProgress;

        [Header("Build Queue Display Prefabs")]
        [SerializeField] private GameObject shipyardQueueItemPrefab;
        [SerializeField] private GameObject factoryQueueItemPrefab;

        // Tracks which system currently has its ExpandedContent visible in the list
        private StarSysController _currentExpandedSysCon;

        private void Awake()
        {
            // ✅ Simple scene-based singleton - no DontDestroyOnLoad!
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("✅ StarSysMenuUIController: Instance assigned");
            }
            else if (Instance != this)
            {
                Debug.LogWarning($"❌ Duplicate StarSysMenuUIController found! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // DON'T call FindSysUIContainers() here - GalaxyScene doesn't exist yet!

            if (StarSysManager.Instance != null)
            {
                for (int i = 0; i < StarSysManager.Instance.StarSysControllerList.Count; i++)
                {
                    var sysCon = StarSysManager.Instance.StarSysControllerList[i];
                    if (sysCon != null && sysCon.StarSysUIGameObject != null)
                    {
                        var child = sysCon.StarSysUIGameObject;
                        var childController = child.GetComponent<FleetAndSystemChildController>();
                        if (childController != null && childController.OriginalParentTransform == null)
                        {
                            if (child.transform.parent != null)
                            {
                                childController.OriginalParentTransform = child.transform.parent;
                            }
                            else if (SysListContainer != null)
                            {
                                childController.OriginalParentTransform = SysListContainer.transform;
                            }
                            else if (ASystemMenuView != null)
                            {
                                childController.OriginalParentTransform = ASystemMenuView.transform;
                            }
                        }
                    }
                }
            }

            // Initially hide views (they might not exist yet)
            if (SystemsMenuView != null)
                SystemsMenuView.SetActive(false);
            if (ASystemMenuView != null)
                ASystemMenuView.SetActive(false);
        }
        public void SetUIReferences(GameObject systemListContainer, GameObject canvasGalaxy)
        {
            // SysListContainer must NOT be set from systemListContainer here — that object
            // is the 3D home-storage container under GalaxyCenter used by StarSysManager.
            // FindSysUIContainers() locates the correct canvas-based SysListContainer
            // inside CanvasGalaxy/SystemsMenuView/Viewport when ShowSystemMenuView is called.
        }
        /// <summary>
        /// Shows the scrollable list view of all local player's systems
        /// CALLED BY: GalaxyMenuUIController.SystemButtonPressed()
        /// </summary>
        public void ShowSystemMenuView()
        {
            Debug.Log("=== ShowSystemMenuView: Starting ===");

            if (SystemsMenuView == null || SysListContainer == null)
            {
                FindSysUIContainers();
            }

            if (SystemsMenuView == null)
            {
                Debug.LogError("ShowSystemMenuView: SystemsMenuView is NULL!");
                return;
            }

            // ✅ ACTIVATE PARENT FIRST - This is critical!
            SystemsMenuView.SetActive(true);

            if (SysListContainer != null)
                SysListContainer.SetActive(true);

            // ✅ Move and activate each local player's system UI into the list container
            if (SysListContainer != null && StarSysManager.Instance != null)
            {
                foreach (var sysCon in StarSysManager.Instance.StarSysControllerList)
                {
                    if (sysCon == null || sysCon.StarSysUIGameObject == null) continue;

                    // Only show local player's systems
                    if (!GameController.Instance.AreWeLocalPlayer(sysCon.StarSysData.CurrentOwnerCivEnum))
                        continue;

                    sysCon.StarSysUIGameObject.transform.SetParent(SysListContainer.transform, false);
                    sysCon.StarSysUIGameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("ShowSystemMenuView: SysListContainer is null — check Inspector assignment on StarSysMenuUIController.");
            }

            Debug.Log("  SystemMenuView activated with scrollable list");

            SetupSystemUIData();

            Debug.Log("=== ShowSystemMenuView: Complete ===");
        }

        /// <summary>
        /// Hides the scrollable list view and moves UIs back to home storage
        /// CALLED BY: GalaxyMenuUIController.CloseMenu(Menu.SystemMenu)
        /// </summary>
        public void HideSystemMenuView()
        {
            if (SystemsMenuView == null)
            {
                Debug.LogWarning("HideSystemMenuView: SystemsMenuView is null, skipping");
                return;
            }

            // ✅ Move all system UIs back to home storage
            MoveSystemsToHomeStorage();

            SystemsMenuView.SetActive(false);
            Debug.Log("SystemMenuView hidden, UIs moved back to storage");
        }

        /// <summary>
        /// Shows the detailed view of a single system
        /// CALLED BY: GalaxyMenuUIController.OpenMenu(Menu.ASystemMenu) when clicking a system
        /// </summary>
        public void ShowA_SystemMenuView()
        {
            if (ASystemMenuView == null)
            {
                Debug.LogWarning("ShowA_SystemMenuView: ASystemMenuView is null, skipping");
                return;
            }

            ASystemMenuView.SetActive(true);
            Debug.Log("ASystemMenuView shown (single system detail)");
        }

        /// <summary>
        /// Hides the single system detail view and moves UI back to home storage
        /// </summary>
        public void HideA_SystemMenuView()
        {
            if (ASystemMenuView == null)
            {
                Debug.LogWarning("HideA_SystemMenuView: ASystemMenuView is null, skipping");
                return;
            }

            // ✅ Move system UI from detail view back to home storage
            MoveSystemsToHomeStorage();

            ASystemMenuView.SetActive(false);
            Debug.Log("ASystemMenuView hidden (single system detail)");
        }

        public void SetupSystemUIData()
        {
            Debug.Log("SetupSystemUIData: Wiring buttons and updating data");

            if (StarSysManager.Instance == null)
            {
                Debug.LogError("  StarSysManager.Instance is null!");
                return;
            }

            if (SysListContainer == null)
            {
                FindSysUIContainers();

                if (SysListContainer == null)
                {
                    Debug.LogError("  SysListContainer is null! Cannot display systems.");
                    return;
                }
            }

            var systems = StarSysManager.Instance.StarSysControllerList;
            if (systems == null || systems.Count == 0)
            {
                Debug.LogWarning("  No systems in StarSysManager!");
                return;
            }

            int setupCount = 0;

            foreach (var sysCon in systems)
            {
                if (sysCon == null || sysCon.StarSysUIGameObject == null) continue;

                // Only setup local player's systems
                if (!GameController.Instance.AreWeLocalPlayer(sysCon.StarSysData.CurrentOwnerCivEnum))
                    continue;

                var sysUIFieldElement = sysCon.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (sysUIFieldElement == null)
                {
                    Debug.LogWarning($"  System '{sysCon.name}' UI has no StarSysUI_Fields component - skipping");
                    continue;
                }

                // ✅ Set ShipListUIParent and Ensure Layout
                if (sysUIFieldElement.shipContent != null)
                {
                    sysCon.StarSysData.ShipListUIParent = sysUIFieldElement.shipContent.gameObject;

                    // Grid: 140×25 cells, 2 columns
                    var grid = sysUIFieldElement.shipContent.GetComponent<UnityEngine.UI.GridLayoutGroup>()
                               ?? sysUIFieldElement.shipContent.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                    grid.cellSize = new Vector2(140, 25);
                    grid.spacing = new Vector2(4, 4);
                    grid.padding = new RectOffset(5, 0, 0, 0);
                    grid.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
                    grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = 2;

                    var fitter = sysUIFieldElement.shipContent.GetComponent<UnityEngine.UI.ContentSizeFitter>()
                                 ?? sysUIFieldElement.shipContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                    fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                    fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

                    // Anchor content to top-left so GridLayoutGroup places items downward
                    var contentRect = sysUIFieldElement.shipContent;
                    contentRect.anchorMin = new Vector2(0f, 1f);
                    contentRect.anchorMax = new Vector2(1f, 1f);
                    contentRect.pivot = new Vector2(0f, 1f);
                    contentRect.anchoredPosition = Vector2.zero;
                    contentRect.sizeDelta = Vector2.zero;

                    // Fix Viewport to fill ShipScrollView
                    if (sysUIFieldElement.ShipScrollView != null)
                    {
                        var svRect = sysUIFieldElement.ShipScrollView.GetComponent<RectTransform>();
                        if (svRect != null)
                            svRect.sizeDelta = new Vector2(sysUIFieldElement.CollapsedShipScrollViewWidth, svRect.sizeDelta.y);

                        var viewport = sysUIFieldElement.ShipScrollView.transform.Find("Viewport");
                        if (viewport != null)
                        {
                            var vpRect = viewport.GetComponent<RectTransform>();
                            if (vpRect != null)
                            {
                                vpRect.anchorMin = Vector2.zero;
                                vpRect.anchorMax = Vector2.one;
                                vpRect.sizeDelta = Vector2.zero;
                                vpRect.anchoredPosition = Vector2.zero;
                            }
                        }

                        var sr = sysUIFieldElement.ShipScrollView.GetComponent<UnityEngine.UI.ScrollRect>();
                        if (sr != null)
                        {
                            sr.enabled = true;
                            // Prevent scroll events from propagating to the FactoryScrollViewQueue
                            if (sysUIFieldElement.ShipScrollView.GetComponent<ScrollRectIsolator>() == null)
                                sysUIFieldElement.ShipScrollView.AddComponent<ScrollRectIsolator>();
                        }
                    }

                    // Expand button: show whenever ships are present
                    int shipCount = sysCon.StarSysData?.ShipsList?.Count ?? 0;
                    bool needsExpand = shipCount > 0;
                    if (sysUIFieldElement.ExpandShipsButton != null)
                    {
                        sysUIFieldElement.ExpandShipsButton.gameObject.SetActive(needsExpand);
                        if (needsExpand)
                        {
                            sysUIFieldElement.ExpandShipsButton.onClick.RemoveAllListeners();
                            var capSysCon = sysCon;
                            var capFields = sysUIFieldElement;
                            sysUIFieldElement.ExpandShipsButton.onClick.AddListener(
                                () => ToggleSysShipListExpansion(capSysCon, capFields));
                            SetSysExpandButtonLabel(sysUIFieldElement, false);
                        }
                    }

                    // ✅ SYNC SHIPS (Always run to ensure UIs are present and correctly parented)
                    if (sysCon.StarSysData.ShipsList != null)
                    {
                        foreach (var shipCon in sysCon.StarSysData.ShipsList)
                        {
                            if (shipCon == null) continue;

                            // Create missing UI
                            if (shipCon.ShipListUIGameObject == null)
                            {
                                ShipManager.Instance?.InstantiateShipListUIGameObject(shipCon, sysCon.gameObject);
                                Debug.Log($"  Created missing ship UI for '{shipCon.ShipData?.ShipName}' in system '{sysCon.name}'");
                            }

                            // Ensure correct parent
                            if (shipCon.ShipListUIGameObject != null &&
                                shipCon.ShipListUIGameObject.transform.parent != sysUIFieldElement.shipContent)
                            {
                                shipCon.ShipListUIGameObject.transform.SetParent(sysUIFieldElement.shipContent, false);
                                shipCon.ShipListUIGameObject.SetActive(true);
                                Debug.Log($"  Re-parented ship UI '{shipCon.ShipData?.ShipName}' to StarSys shipContent");
                            }
                        }
                        // Rescue any queued items
                        ShipManager.Instance?.ProcessPendingShipUIs();
                    }
                }

                // ✅ FIRST TIME ONLY: Wire buttons and set original parent
                if (!listOfStarSysUiGos.Contains(sysCon.StarSysUIGameObject))
                {
                    // Set OriginalParentTransform — system UIs live in SysListContainer permanently
                    var childController = sysCon.StarSysUIGameObject.GetComponent<FleetAndSystemChildController>();
                    if (childController != null)
                    {
                        childController.OriginalParentTransform = SysListContainer?.transform;
                    }

                    // Wire all buttons (BuildButton, ShipButton, etc.)
                    WireSystemUIButtons(sysCon, sysUIFieldElement);

                    // Add to tracking list
                    listOfStarSysUiGos.Add(sysCon.StarSysUIGameObject);
                }

                // Position red dot on mini-map — always refresh using live transform position
                if (sysUIFieldElement.redDot != null)
                {
                    Vector3 sysPos = sysCon.transform.position;
                    sysUIFieldElement.redDot.anchoredPosition = new Vector2(sysPos.x * 0.12f, sysPos.z * 0.12f);
                }

                // ✅ CRITICAL: Calculate power balance BEFORE updating UI
                UpdateSystemPowerBalance(sysCon);

                // ✅ EVERY TIME: Update facility data
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.Factory);
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.Shipyard);
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.ShieldGenerator);
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.OrbitalBattery);
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.ResearchCenter);

                // ✅ NOW initialize UI from calculated data
                try
                {
                    sysUIFieldElement.InitializeFromStarSysData(sysCon.StarSysData);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"SetupSystemUIData: InitializeFromStarSysData failed for {sysCon.name}: {ex.Message}");
                }

                RefreshQueueDisplays(sysCon, sysUIFieldElement);

                setupCount++;
            } // ✅ MOVED: End of foreach loop is HERE

            Debug.Log($"SetupSystemUIData: Configured {setupCount} systems");
        }

        /// <summary>
        /// Helper: Wires all buttons on a system UI (called once per UI)
        /// </summary>
        private void WireSystemUIButtons(StarSysController sysCon, StarSysUI_Fields fields)
        {
            this.ActiveStarSysController = sysCon;
            // Hide cancel button initially
            if (fields.cancelShipManagerButton != null)
            {
                fields.cancelShipManagerButton.gameObject.SetActive(false);
            }

            // Wire action buttons
            if (fields.buildButton != null)
            {
                fields.buildButton.onClick.RemoveAllListeners();
                fields.buildButton.onClick.AddListener(() => sysCon.BuildClick(sysCon));
            }

            if (fields.shipButton != null)
            {
                fields.shipButton.onClick.RemoveAllListeners();
                fields.shipButton.onClick.AddListener(() => sysCon.ShipClick(sysCon));
            }

            if (fields.shipDeployButton != null)
            {
                fields.shipDeployButton.onClick.RemoveAllListeners();
                fields.shipDeployButton.onClick.AddListener(() => StarSysClickShipDeployButton(sysCon));
            }

            if (fields.cargoButton != null)
            {
                fields.cargoButton.onClick.RemoveAllListeners();
                fields.cargoButton.onClick.AddListener(() => StarSysClickCargoButton(sysCon));
            }

            if (fields.newFleetButton != null)
            {
                fields.newFleetButton.onClick.RemoveAllListeners();
                fields.newFleetButton.onClick.AddListener(() => ClickNewFleetButton(sysCon));
            }

            if (fields.mergeFleetButton != null)
            {
                fields.mergeFleetButton.onClick.RemoveAllListeners();
                fields.mergeFleetButton.onClick.AddListener(() => StarSysClickMergeShipsButton(sysCon));
            }

            // Wire facility On/Off buttons
            WireFacilityButton(fields.factoryButtonOn, SystemOnOffButtons.FactoryOnButton, () => sysCon.FactoryButtonOnClicked(sysCon));
            WireFacilityButton(fields.factoryButtonOff, SystemOnOffButtons.FactoryOffButton, () => sysCon.FactoryButtonOffClicked(sysCon));

            WireFacilityButton(fields.yardButtonOn, SystemOnOffButtons.ShipyardOnButton, () => sysCon.YardButtonOnClicked(sysCon));
            WireFacilityButton(fields.yardButtonOff, SystemOnOffButtons.ShipyardOffbutton, () => sysCon.YardButtonOffClicked(sysCon));

            WireFacilityButton(fields.shieldButtonOn, SystemOnOffButtons.ShieldGeneratorOnButton, () => sysCon.ShieldButtonOnClicked(sysCon));
            WireFacilityButton(fields.shieldButtonOff, SystemOnOffButtons.ShieldGeneratorOffbutton, () => sysCon.ShieldButtonOffClicked(sysCon));

            WireFacilityButton(fields.oBButtonOn, SystemOnOffButtons.OrbitalBatteryOnButton, () => sysCon.OBButtonOnClicked(sysCon));
            WireFacilityButton(fields.oBButtonOff, SystemOnOffButtons.OrbitalBatteryOffButton, () => sysCon.OBButtonOffClicked(sysCon));

            WireFacilityButton(fields.researchButtonOn, SystemOnOffButtons.ResearchCenterOnButton, () => sysCon.ResearchButtonOnClicked(sysCon));
            WireFacilityButton(fields.researchButtonOff, SystemOnOffButtons.ResearchCenterOffButton, () => sysCon.ResearchButtonOffClicked(sysCon));

            // Set theme images
            if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
            {
                if (fields.powerUnitImage != null)
                    fields.powerUnitImage.sprite = ThemeManager.Instance.CurrentTheme.PowerPlantImage;
                if (fields.factoryImage != null)
                    fields.factoryImage.sprite = ThemeManager.Instance.CurrentTheme.FactoryImage;
                if (fields.shipyardImage != null)
                    fields.shipyardImage.sprite = ThemeManager.Instance.CurrentTheme.ShipyardImage;
                if (fields.shieldPlanetImage != null)
                    fields.shieldPlanetImage.sprite = ThemeManager.Instance.CurrentTheme.ShieldImage;
                if (fields.orbitalBatteriesImage != null)
                    fields.orbitalBatteriesImage.sprite = ThemeManager.Instance.CurrentTheme.OrbitalBatteriesImage;
                if (fields.researchImage != null)
                    fields.researchImage.sprite = ThemeManager.Instance.CurrentTheme.ResearchCenterImage;
            }

            // Assign PowerOverloadImage from first system (singleton pattern)
            if (PowerOverloadImage == null && fields.PowerOverload != null)
            {
                PowerOverloadImage = fields.PowerOverload;
            }
        }

        /// <summary>
        /// Helper: Wires a single facility button with its component and listener
        /// </summary>
        private void WireFacilityButton(Button button, SystemOnOffButtons buttonType, UnityEngine.Events.UnityAction listener)
        {
            if (button == null) return;

            // Set SysButtonOnOff component type if exists
            var comp = button.GetComponent<SysButtonOnOff>();
            if (comp != null)
            {
                comp.button = buttonType;
            }

            // Wire click listener
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(listener);
        }
        /// <summary>
        /// Populates the SystemsMenuView with all local player's star system UIs
        /// </summary>
        public void PopulateSystemsList()
        {
            Debug.Log("PopulateSystemsList: Starting...");

            if (SystemsMenuView == null || !SystemsMenuView.activeSelf)
            {
                Debug.LogWarning("PopulateSystemsList: SystemsMenuView is not active!");
                return;
            }

            // Find the SysListContainer
            Transform sysListContainer = SystemsMenuView.transform.Find("Viewport/SysListContainer");
            if (sysListContainer == null)
            {
                Debug.LogError("PopulateSystemsList: SysListContainer not found! Check prefab hierarchy.");
                return;
            }

            // Get all local player's systems from StarSysManager
            if (StarSysManager.Instance == null)
            {
                Debug.LogError("PopulateSystemsList: StarSysManager.Instance is NULL!");
                return;
            }

            var allSystems = StarSysManager.Instance.StarSysControllerList;
            int populatedCount = 0;

            foreach (var sysCon in allSystems)
            {
                if (sysCon == null)
                {
                    Debug.LogWarning("  Skipping null system reference");
                    continue;
                }

                // ✅ CRITICAL: Only show LOCAL PLAYER's systems
                if (!GameController.Instance.AreWeLocalPlayer(sysCon.StarSysData.CurrentOwnerCivEnum))
                {
                    Debug.Log($"  Skipping non-player system: {sysCon.name}");
                    continue;
                }

                // ✅ Ensure system has UI
                if (sysCon.StarSysUIGameObject == null)
                {
                    Debug.LogWarning($"  System '{sysCon.name}' has no UI GameObject - skipping");
                    continue;
                }

                // ✅ Move system UI from home storage to list container
                sysCon.StarSysUIGameObject.transform.SetParent(sysListContainer, false);
                sysCon.StarSysUIGameObject.SetActive(true);

                // Wire compact header and set initial collapse state
                var fields = sysCon.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (fields != null)
                {
                    fields.compactHeader?.Populate(sysCon);
                    fields.WireAIModeToggles(sysCon);

                    // The system at the top of the list defaults to expanded; its Expand
                    // button is hidden since there's nothing left to promote it to.
                    bool isTopOfList = sysCon.StarSysUIGameObject.transform.GetSiblingIndex() == 0;
                    fields.expandedContent?.SetActive(isTopOfList);
                    fields.compactHeader?.SetExpandButtonActive(!isTopOfList);
                    if (isTopOfList)
                        _currentExpandedSysCon = sysCon;
                }

                populatedCount++;
                Debug.Log($"  ✅ Added system '{sysCon.name}' to systems list");
            }

            Debug.Log($"PopulateSystemsList: Complete - {populatedCount} systems added to list");
        }
        /// <summary>
        /// Hides all system UIs. Any in AStarSysMenuView are returned to SysListContainer first.
        /// CALLED BY: HideSystemMenuView(), HideA_SystemMenuView()
        /// </summary>
        private void MoveSystemsToHomeStorage()
        {
            // Return any system UI from the detail view back to SysListContainer, then deactivate it
            if (ASystemMenuView != null)
            {
                for (int i = ASystemMenuView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = ASystemMenuView.transform.GetChild(i);
                    if (child == null) continue;
                    if (child.GetComponent<StarSysUI_Fields>() != null)
                    {
                        if (SysListContainer != null)
                            child.SetParent(SysListContainer.transform, false);
                        child.gameObject.SetActive(false);
                    }
                }
            }

            // Deactivate all system UIs in the list container
            if (SysListContainer != null)
            {
                for (int i = 0; i < SysListContainer.transform.childCount; i++)
                {
                    var child = SysListContainer.transform.GetChild(i);
                    if (child != null && child.GetComponent<StarSysUI_Fields>() != null)
                        child.gameObject.SetActive(false);
                }
            }
        }

        public void SetActiveSetParentUIGO(StarSysController theSysCon)
        {
            // CRITICAL: Find containers if needed
            if (SysListContainer == null || ASystemMenuView == null)
            {
                FindSysUIContainers();
            }

            // Menu system handles cleanup when transitioning between menus
            // Don't call MoveBack here - it deactivates UIs

            if (theSysCon == null)
            {
                Debug.LogWarning("SetActiveSetParentUIGO: theSysCon is null");
                return;
            }

            // ✅ Check if StarSysUIGameObject is null
            if (theSysCon.StarSysUIGameObject == null)
            {
                Debug.LogError($"SetActiveSetParentUIGO: StarSysUIGameObject is null for system '{theSysCon.name}'!");
                return;
            }

            // Check ASystemMenuView exists
            if (ASystemMenuView == null)
            {
                Debug.LogError("SetActiveSetParentUIGO: ASystemMenuView is null!");
                return;
            }

            // ✅ Setup UI data (calculates power balance internally before displaying)
            SetupSystemUIData();

            // ✅ Move to detail view and ACTIVATE
            try
            {
                theSysCon.StarSysUIGameObject.transform.SetParent(ASystemMenuView.transform, false);
                theSysCon.StarSysUIGameObject.SetActive(true); // ✅ Ensure it's active
                lastSysCon = theSysCon;

                // Single-system detail view: always show ExpandedContent and hide the
                // Expand button since there's no list to move within.
                var soloFields = theSysCon.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (soloFields != null)
                {
                    soloFields.expandedContent?.SetActive(true);
                    // Populate (not just RefreshDilithium) since this system's compact header may
                    // never have been populated via the systems-list path (PopulateSystemsList) —
                    // e.g. when the player opens the solo detail view straight from the galaxy map.
                    soloFields.compactHeader?.Populate(theSysCon);
                    soloFields.compactHeader?.SetExpandButtonActive(false);
                }

                Debug.Log($"SetActiveSetParentUIGO: Successfully displayed system '{theSysCon.name}'");
                Debug.Log($"  Power Output: {theSysCon.StarSysData.TotalSysPowerOutput}, Load: {theSysCon.StarSysData.TotalSysPowerLoad}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SetActiveSetParentUIGO: Exception displaying system '{theSysCon.name}': {ex.Message}");
                return;
            }

            // ✅ Update PowerOverloadImage to this system's power overload visual
            var sysUIFieldElement = theSysCon.StarSysUIGameObject?.GetComponent<StarSysUI_Fields>();
            if (sysUIFieldElement != null && sysUIFieldElement.PowerOverload != null)
            {
                PowerOverloadImage = sysUIFieldElement.PowerOverload;
                // ✅ Ensure it starts OFF when first displayed
                PowerOverloadImage.SetActive(false);
            }
        }

        public void CloseBuildingQueues()
        {
            Debug.Log("CloseBuildingQueues: Starting");

            // Refresh the System UI queue display before hiding, so newly added items appear immediately
            var buildSysCon = StarSysManager.Instance?.CurrentBuildUISysCon;
            if (buildSysCon != null)
                RefreshQueueForSystem(buildSysCon);

            // Hide (don't destroy) so in-progress builds remain visible if the same system is reopened
            StarSysManager.Instance?.HideBuildUI();

            // Inspector-wired (persistent) listeners on the CloseBuilding button cannot be cleared
            // in code and may close ASystemMenuView as a side-effect.  Re-assert it open here.
            if (ASystemMenuView != null)
                ASystemMenuView.SetActive(true);

            Debug.Log("CloseBuildingQueues: Complete");
        }
        public void RemoveSystem(StarSysController sysController)
        {
            if (sysController == null) return;
            if (SysControllersContains(sysController))
            {
                listOfStarSysUiGos.Remove(sysController.StarSysUIGameObject);
            }
        }

        private bool SysControllersContains(StarSysController sysController)
        {
            // safe helper - originally GalaxyMenu had its own list; here keep list tracking by GameObject
            return listOfStarSysUiGos.Contains(sysController.StarSysUIGameObject);
        }

        public void UpdateFacilityUI(StarSysController sysController, int plusMinus, StarSysFacilityType facilityType)
        {
            if (!GameController.Instance.AreWeLocalPlayer(sysController.StarSysData.CurrentOwnerCivEnum)) return;

            sysController.StarSysUIGameObject.SetActive(true);
            var fields = sysController.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();

            if (fields == null)
            {
                Debug.LogWarning($"UpdateFacilityUI: StarSysUI_Fields not found on {sysController.name}");
                return;
            }
            float newPowerOutput;
            int newFacilityLoad = 0;
            int numOn = 0;
            int numOff = 0;
            List<GameObject> facilities = new List<GameObject>();

            Button onButton = null;
            Button offButton = null;

            switch (facilityType)
            {
                case StarSysFacilityType.PowerPlanet:
                    var techMulitplyer = sysController.StarSysData.CurrentCivController.CivData.GetPowerTechMultiplier();
                    newPowerOutput = sysController.StarSysData.CalculateTotalPower(techMulitplyer);
                    facilities = sysController.StarSysData.PowerPlants;

                    fields.numPUnits.text = sysController.StarSysData.PowerPlants.Count.ToString();
                    fields.numTotalEOut.text = newPowerOutput.ToString();
                    break;
                case StarSysFacilityType.Factory:
                    newFacilityLoad = sysController.StarSysData.FactoryData.PowerLoad;
                    facilities = sysController.StarSysData.Factories;
                    onButton = fields.factoryButtonOn;
                    offButton = fields.factoryButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.Factory, facilities);
                    numOff = facilities.Count - numOn;
                    fields.numFactoryRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.factoryLoad.text = (newFacilityLoad * numOn).ToString();
                    break;
                case StarSysFacilityType.Shipyard:
                    newFacilityLoad = sysController.StarSysData.ShipyardData.PowerLoad;
                    facilities = sysController.StarSysData.Shipyards;
                    onButton = fields.yardButtonOn;
                    offButton = fields.yardButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.Shipyard, facilities);
                    numOff = facilities.Count - numOn;

                    Debug.Log($"📊 UpdateFacilityUI: {facilityType} - Total: {facilities.Count}, On: {numOn}, Off: {numOff}");

                    fields.numYardsOnRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.yardLoad.text = (newFacilityLoad * numOn).ToString();

                    Debug.Log($"✏️ UpdateFacilityUI: Set ratio text to '{fields.numYardsOnRatio.text}'");
                    break;
                case StarSysFacilityType.ShieldGenerator:
                    newFacilityLoad = sysController.StarSysData.ShieldGeneratorData.PowerLoad;
                    facilities = sysController.StarSysData.ShieldGenerators;
                    onButton = fields.shieldButtonOn;
                    offButton = fields.shieldButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.ShieldGenerator, facilities);
                    numOff = facilities.Count - numOn;
                    fields.numShieldsRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.shieldLoad.text = (newFacilityLoad * numOn).ToString();
                    break;
                case StarSysFacilityType.OrbitalBattery:
                    newFacilityLoad = sysController.StarSysData.OrbitalBatteryData.PowerLoad;
                    facilities = sysController.StarSysData.OrbitalBatteries;
                    onButton = fields.oBButtonOn;
                    offButton = fields.oBButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.OrbitalBattery, facilities);
                    numOff = facilities.Count - numOn;
                    fields.numOBRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.oBLoad.text = (newFacilityLoad * numOn).ToString();
                    fields.SyncOrbitalBatteryGrid(facilities, sysController.StarSysData.OrbitalBatteryData?.OrbitalBatterySprite);
                    break;
                case StarSysFacilityType.ResearchCenter:
                    newFacilityLoad = sysController.StarSysData.ResearchCenterData.PowerLoad;
                    facilities = sysController.StarSysData.ResearchCenters;
                    onButton = fields.researchButtonOn;
                    offButton = fields.researchButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.ResearchCenter, facilities);
                    numOff = facilities.Count - numOn;
                    fields.numResearchRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.researchLoad.text = (newFacilityLoad * numOn).ToString();
                    break;
                default:
                    break;
            }

            // Show ON button only when there are facilities to turn on AND power headroom allows it.
            if (onButton != null)
            {
                int headroom = sysController.StarSysData.TotalSysPowerOutput - sysController.StarSysData.TotalSysPowerLoad;
                bool shouldShowOnButton = numOff > 0 && newFacilityLoad <= headroom;
                onButton.gameObject.SetActive(shouldShowOnButton);
                Debug.Log($"🔘 UpdateFacilityUI: {facilityType} Power ON button -> {(shouldShowOnButton ? "VISIBLE" : "HIDDEN")} (numOff={numOff}, headroom={headroom}, load={newFacilityLoad})");
            }

            // ✅ NEW: Show/Hide Off button based on whether there are facilities to turn off
            if (offButton != null)
            {
                bool shouldShowOffButton = numOn > 0;
                offButton.gameObject.SetActive(shouldShowOffButton);
                Debug.Log($"🔘 UpdateFacilityUI: {facilityType} Power OFF button -> {(shouldShowOffButton ? "VISIBLE" : "HIDDEN")} (numOn={numOn})");
            }
        }

        private int NumFacilitiesTurnedOn(StarSysFacilityType factory, List<GameObject> facilities) //, StarSysController sysController, ref int numOn, ref int newFacilityLoad, StarSysUI_Fields fields)
        {
            int numOn = 0;
            for (int j = 0; j < facilities.Count; j++)
            {
                TextMeshProUGUI TheText = facilities[j].GetComponent<TextMeshProUGUI>();
                if (TheText.text == "1")
                    numOn++;
            }
            return numOn;
        }

        public void UpdateSystemPowerBalance(StarSysController sysCon)
        {
            if (sysCon == null) return;

            // ✅ NEW: Early exit if no UI exists (for non-player systems)
            if (sysCon.StarSysUIGameObject == null)
            {
                // Still update the data values even without UI
                int load = 0;
                int output = 0;

                for (int i = 0; i < sysCon.StarSysData.PowerPlants.Count; i++)
                    output += sysCon.StarSysData.PowerPlantData.BasePowerOutput;
                for (int i = 0; i < sysCon.StarSysData.Factories.Count; i++)
                    if (sysCon.StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text == "1")
                        load += sysCon.StarSysData.FactoryData.PowerLoad;

                for (int i = 0; i < sysCon.StarSysData.Shipyards.Count; i++)
                    if (sysCon.StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text == "1")
                        load += sysCon.StarSysData.ShipyardData.PowerLoad;

                for (int i = 0; i < sysCon.StarSysData.ShieldGenerators.Count; i++)
                    if (sysCon.StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text == "1")
                        load += sysCon.StarSysData.ShieldGeneratorData.PowerLoad;

                for (int i = 0; i < sysCon.StarSysData.OrbitalBatteries.Count; i++)
                    if (sysCon.StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text == "1")
                        load += sysCon.StarSysData.OrbitalBatteryData.PowerLoad;

                for (int i = 0; i < sysCon.StarSysData.ResearchCenters.Count; i++)
                    if (sysCon.StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text == "1")
                        load += sysCon.StarSysData.ResearchCenterData.PowerLoad;

                sysCon.StarSysData.TotalSysPowerLoad = load;
                sysCon.StarSysData.TotalSysPowerOutput = output;
                return; // ✅ Exit - no UI to update
            }

            // ✅ ORIGINAL CODE: UI exists, calculate and update UI
            int loadUI = 0;
            int outputUI = 0;
            for (int i = 0; i < sysCon.StarSysData.PowerPlants.Count; i++)
                outputUI += sysCon.StarSysData.PowerPlantData.BasePowerOutput;
            for (int i = 0; i < sysCon.StarSysData.Factories.Count; i++)
                if (sysCon.StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text == "1")
                    loadUI += sysCon.StarSysData.FactoryData.PowerLoad;

            for (int i = 0; i < sysCon.StarSysData.Shipyards.Count; i++)
                if (sysCon.StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text == "1")
                    loadUI += sysCon.StarSysData.ShipyardData.PowerLoad;

            for (int i = 0; i < sysCon.StarSysData.ShieldGenerators.Count; i++)
                if (sysCon.StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text == "1")
                    loadUI += sysCon.StarSysData.ShieldGeneratorData.PowerLoad;

            for (int i = 0; i < sysCon.StarSysData.OrbitalBatteries.Count; i++)
                if (sysCon.StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text == "1")
                    loadUI += sysCon.StarSysData.OrbitalBatteryData.PowerLoad;

            for (int i = 0; i < sysCon.StarSysData.ResearchCenters.Count; i++)
                if (sysCon.StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text == "1")
                    loadUI += sysCon.StarSysData.ResearchCenterData.PowerLoad;

            sysCon.StarSysData.TotalSysPowerLoad = loadUI;
            sysCon.StarSysData.TotalSysPowerOutput = outputUI;

            // ✅ Update PowerOverload UI state and ONLY flash when overloaded
            var uiFields = sysCon.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
            if (uiFields != null && uiFields.PowerOverload != null)
            {
                if (PowerOverloadImage != null)
                    PowerOverloadImage.SetActive(false); // Default to OFF
                bool isOverloaded = loadUI > outputUI;
                bool notWhenFirstOpening = false;
                if (isOverloaded && notWhenFirstOpening && PowerOverloadImage != null)
                {
                    Debug.Log($"⚠️ POWER OVERLOAD: System '{sysCon.name}' - Load {loadUI} > Output {outputUI}");
                    // Flash the warning ONLY when overloaded
                    PowerOverloadImage.SetActive(true);
                    CoroutineRunner.FlashPowerOverload();
                }
                else
                {
                    // No overload - ensure PowerOverload image is OFF
                    if (PowerOverloadImage != null)
                        PowerOverloadImage.SetActive(false);
                    if (!notWhenFirstOpening)
                        notWhenFirstOpening = true;
                }
            }

            // ✅ Update text displays (NOW SAFE - we know StarSysUIGameObject exists)
            TextMeshProUGUI[] OneTMP = sysCon.StarSysUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < OneTMP.Length; i++)
            {
                OneTMP[i].enabled = true;
                if ("NumP Load" == OneTMP[i].name)
                    OneTMP[i].text = loadUI.ToString();
                if ("NumTotal EOut" == OneTMP[i].name)
                    OneTMP[i].text = outputUI.ToString();
            }

            // Re-evaluate all facility ON button visibility now that TotalSysPowerLoad/Output
            // are up to date. This handles the case where turning off facility type B frees
            // power headroom that should re-enable the ON button for facility type A.
            UpdateFacilityUI(sysCon, 0, StarSysFacilityType.Factory);
            UpdateFacilityUI(sysCon, 0, StarSysFacilityType.Shipyard);
            UpdateFacilityUI(sysCon, 0, StarSysFacilityType.ShieldGenerator);
            UpdateFacilityUI(sysCon, 0, StarSysFacilityType.OrbitalBattery);
            UpdateFacilityUI(sysCon, 0, StarSysFacilityType.ResearchCenter);
        }

        internal void AddSysFacility(StarSysController controller, GameObject facilityGO, string loadName, string ratioName, StarSysFacilityType facilityType)
        {
            Debug.Log($"🏗️ AddSysFacility CALLED: {facilityType} for system {controller?.name}");

            // Ensure StarSysData exists
            var starSysData = controller.StarSysData;
            if (starSysData == null)
            {
                Debug.LogWarning($"AddSysFacility: StarSysData is null for controller {controller.name}.");
                return;
            }
            if (GameController.Instance.AreWeLocalPlayer(controller.StarSysData.CurrentOwnerCivEnum))
            {
                // Resolve list and per-facility load
                int newFacilityLoad = 0;
                List<GameObject> facilities = null;
                switch (facilityType)
                {
                    case StarSysFacilityType.Factory:
                        newFacilityLoad = starSysData.FactoryData?.PowerLoad ?? 0;
                        facilities = starSysData.Factories;
                        break;
                    case StarSysFacilityType.Shipyard:
                        newFacilityLoad = starSysData.ShipyardData?.PowerLoad ?? 0;
                        facilities = starSysData.Shipyards;
                        break;
                    case StarSysFacilityType.ShieldGenerator:
                        newFacilityLoad = starSysData.ShieldGeneratorData?.PowerLoad ?? 0;
                        facilities = starSysData.ShieldGenerators;
                        break;
                    case StarSysFacilityType.OrbitalBattery:
                        newFacilityLoad = starSysData.OrbitalBatteryData?.PowerLoad ?? 0;
                        facilities = starSysData.OrbitalBatteries;
                        break;
                    case StarSysFacilityType.ResearchCenter:
                        newFacilityLoad = starSysData.ResearchCenterData?.PowerLoad ?? 0;
                        facilities = starSysData.ResearchCenters;
                        break;
                    case StarSysFacilityType.PowerPlanet:
                        newFacilityLoad = starSysData.PowerPlantData?.BasePowerOutput ?? 0;
                        facilities = starSysData.PowerPlants;
                        break;
                    default:
                        Debug.LogWarning($"AddSysFacility: unsupported facilityType {facilityType}.");
                        break;
                }

                // Defensive: ensure list exists
                if (facilities == null)
                {
                    Debug.LogWarning($"AddSysFacility: facilities list for {facilityType} is null on system {controller.name}. Creating new list.");
                    facilities = new List<GameObject>();
                    switch (facilityType)
                    {
                        case StarSysFacilityType.Factory: starSysData.Factories = facilities; break;
                        case StarSysFacilityType.Shipyard: starSysData.Shipyards = facilities; break;
                        case StarSysFacilityType.ShieldGenerator: starSysData.ShieldGenerators = facilities; break;
                        case StarSysFacilityType.OrbitalBattery: starSysData.OrbitalBatteries = facilities; break;
                        case StarSysFacilityType.ResearchCenter: starSysData.ResearchCenters = facilities; break;
                        case StarSysFacilityType.PowerPlanet: starSysData.PowerPlants = facilities; break;
                    }
                }

                // Add the facility GameObject to the list if not already present
                if (facilityGO != null && !facilities.Contains(facilityGO))
                {
                    facilities.Add(facilityGO);
                    Debug.Log($"✅ AddSysFacility: Added {facilityType} to {controller.name}. New count: {facilities.Count}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ AddSysFacility: Facility already in list or null for {controller.name}");
                }

                // Try to update typed UI first
                var uiElement = controller.StarSysUIGameObject?.GetComponent<StarSysUI_Fields>();
                if (uiElement == null)
                {
                    Debug.LogWarning($"AddSysFacility: StarSysUI_Fields not found for system {controller.name}. Falling back to string-based updates.");
                }
                else
                {
                    StarSysUI_Fields.FacilityUI facUI = null;
                    try
                    {
                        facUI = uiElement.GetFacility(facilityType);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"AddSysFacility: facility UI of type {facilityType} not found on StarSysUI_Fields for {controller.name}. Exception: {ex.Message}");
                        facUI = null;
                    }

                    if (facUI != null)
                    {
                        // Set icon & name from StarSysData where possible
                        switch (facilityType)
                        {
                            case StarSysFacilityType.Factory:
                                if (facUI.icon != null) facUI.icon.sprite = starSysData.FactoryData?.FactorySprite;
                                if (facUI.nameText != null) facUI.nameText.text = starSysData.FactoryData?.Name ?? string.Empty;
                                break;
                            case StarSysFacilityType.Shipyard:
                                if (facUI.icon != null) facUI.icon.sprite = starSysData.ShipyardData?.ShipyardSprite;
                                if (facUI.nameText != null) facUI.nameText.text = starSysData.ShipyardData?.Name ?? string.Empty;
                                break;
                            case StarSysFacilityType.ShieldGenerator:
                                if (facUI.icon != null) facUI.icon.sprite = starSysData.ShieldGeneratorData?.ShieldGeneratorSprite;
                                if (facUI.nameText != null) facUI.nameText.text = starSysData.ShieldGeneratorData?.Name ?? string.Empty;
                                break;
                            case StarSysFacilityType.OrbitalBattery:
                                if (facUI.icon != null) facUI.icon.sprite = starSysData.OrbitalBatteryData?.OrbitalBatterySprite;
                                if (facUI.nameText != null) facUI.nameText.text = starSysData.OrbitalBatteryData?.Name ?? string.Empty;
                                break;
                            case StarSysFacilityType.ResearchCenter:
                                if (facUI.icon != null) facUI.icon.sprite = starSysData.ResearchCenterData?.ResearchCenterSprite;
                                if (facUI.nameText != null) facUI.nameText.text = starSysData.ResearchCenterData?.Name ?? string.Empty;
                                break;
                            case StarSysFacilityType.PowerPlanet:
                                if (facUI.icon != null) facUI.icon.sprite = starSysData.PowerPlantData?.PowerPlantSprite;
                                if (facUI.nameText != null) facUI.nameText.text = starSysData.PowerPlantData?.Name ?? string.Empty;
                                break;
                        }

                        // Compute ratio and load using the canonical facilities list
                        int numOn = 0;
                        int load = 0;
                        for (int i = 0; i < facilities.Count; i++)
                        {
                            var txt = facilities[i]?.GetComponent<TextMeshProUGUI>()?.text;
                            if (txt == "1")
                            {
                                numOn++;
                                load += newFacilityLoad;
                            }
                        }

                        if (facUI.ratioText != null)
                            facUI.ratioText.text = $"{numOn}/{facilities.Count}";

                        if (facUI.loadText != null)
                            facUI.loadText.text = load.ToString();

                        // ✅ MOVED: Update system power balance and facility UI
                        UpdateSystemPowerBalance(controller);
                        UpdateFacilityUI(controller, 0, facilityType);
                        return;
                    }
                }

                // Fallback: original string-based behavior (keeps backwards compatibility)
                if (controller.StarSysUIGameObject != null)
                {
                    TextMeshProUGUI[] theTextItems = controller.StarSysUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
                    bool allDone = false;
                    for (int j = 0; j < theTextItems.Length; j++)
                    {
                        theTextItems[j].enabled = true;
                        if (theTextItems[j].name == loadName)
                        {
                            int load = 0;
                            for (int k = 0; k < facilities.Count; k++)
                            {
                                if (facilities[k].GetComponent<TextMeshProUGUI>().text == "1")
                                {
                                    load += newFacilityLoad;
                                }
                            }
                            theTextItems[j].text = load.ToString();
                        }
                        else if (theTextItems[j].name == ratioName)
                        {
                            int numOn = 0;
                            for (int i = 0; i < facilities.Count; i++)
                            {
                                TextMeshProUGUI TheText = facilities[i].GetComponent<TextMeshProUGUI>();
                                if (TheText.text == "1") // 1 = on and 0 = off
                                    numOn++;
                            }
                            theTextItems[j].text = numOn.ToString() + "/" + (facilities.Count).ToString();
                            allDone = true;
                        }
                        else if (allDone)
                            break;
                    }

                    // ✅ Update both power balance and facility UI in fallback path too
                    UpdateSystemPowerBalance(controller);
                    UpdateFacilityUI(controller, 0, facilityType);
                }
                else
                {
                    Debug.LogWarning($"AddSysFacility fallback: StarSysUIGameObject is null for {controller.name} and typed UI update failed.");
                }
            }

            // ✅ At the very end, force UI rebuild
            UpdateSystemPowerBalance(controller);
            UpdateFacilityUI(controller, 0, facilityType);

            // Force layout rebuild
            if (controller.StarSysUIGameObject != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
                    controller.StarSysUIGameObject.GetComponent<RectTransform>());
            }
        }

        private void OnDisable()
        {
            // When the UI menu closes (e.g., switching menus or hiding canvas)
            CleanupDestroyedOrInactiveUIs();
        }

        // Only destroy system UIs when:
        // 1. Scene unload - DON'T DO THIS! Let Unity handle it
        private void OnDestroy()
        {
            // ✅ Only clear Instance if we're the current instance
            if (Instance == this)
            {
                Debug.Log("⚠️ StarSysMenuUIController SINGLETON is being destroyed!");
                Debug.Log($"  GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
                Debug.Log($"  Stack trace:\n{System.Environment.StackTrace}");

                Instance = null;
            }
            else
            {
                Debug.Log("StarSysMenuUIController: Duplicate instance destroyed (this is normal)");
            }

            // ❌ NEVER destroy system UIs here - they belong to the scene!
        }

        // 2. Save/load game (recreate galaxy)
        public void OnLoadGame()
        {
            Debug.LogWarning("⚠️ OnLoadGame: Clearing all system UIs for galaxy rebuild");
            ClearAllStarSysUiGos(); // This is OK - we're rebuilding the galaxy

            // Recreate UIs will happen in SetupSystemUIData()
        }
        private void CleanupDestroyedOrInactiveUIs()
        {
            // Inactive UIs are VALID - they're just in hidden containers
            listOfStarSysUiGos.RemoveAll(go => go == null);

            Debug.Log($"CleanupDestroyedOrInactiveUIs: Removed destroyed entries, {listOfStarSysUiGos.Count} valid UIs remain");
        }

        /// <summary>
        /// ONLY call this on scene unload or new game!
        /// </summary>
        private void ClearAllStarSysUiGos()
        {
            // ✅ LOG STACK TRACE TO FIND WHAT'S CALLING THIS
            Debug.LogError("❌❌❌ ClearAllStarSysUiGos CALLED! This should ONLY happen on scene unload/load game!");
            Debug.LogError($"Stack trace:\n{System.Environment.StackTrace}");

            foreach (var go in listOfStarSysUiGos)
            {
                if (go != null)
                    Destroy(go);
            }
            listOfStarSysUiGos.Clear();

            // ✅ CRITICAL: Also clear references in StarSysControllers
            if (StarSysManager.Instance != null)
            {
                foreach (var sysCon in StarSysManager.Instance.StarSysControllerList)
                {
                    if (sysCon != null)
                    {
                        sysCon.StarSysUIGameObject = null;
                    }
                }
            }
        }

        private void StarSysClickShipDeployButton(StarSysController sysController)
        {
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                galaxyUI.WhatSystIsLookingForShipDeploy(sysController);
                galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipDeploy);
                MousePointerChanger.Instance.SetShipExchangeCursor();
                ShipDeployMenuUIController.Instance.TopStarSyst = sysController;
            }
        }
        private void StarSysClickCargoButton(StarSysController sysController)
        {
            if (CargoDeployMenuUIController.Instance != null)
                CargoDeployMenuUIController.Instance.ShowCargoMenuView(sysController);
        }
        private void StarSysClickMergeShipsButton(StarSysController starSysController)
        {
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                galaxyUI.WhatSystIsLookingForMerge(starSysController);
                galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipMerge);
                MousePointerChanger.Instance.SetShipExchangeCursor();
                ShipDeployMenuUIController.Instance.TopStarSyst = starSysController;
            }
        }
        private void ClickNewFleetButton(StarSysController sysController)
        {
            if (sysController.StarSysData.ShipsList.Count == 0) return;
            MousePointerChanger.Instance.ResetCursor();
            var fleetManager = FleetManager.Instance;
            FleetSO fleetSO = fleetManager.GetFleetSO_byInt((int)sysController.StarSysData.CurrentOwnerCivEnum);
            var position = sysController.StarSysData.GetPosition();

            CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum); // new CivData();
            FleetData fleetData = new FleetData(fleetSO);
            fleetData.CurrentWarpFactor = 0f;
            fleetData.CivLongName = thisCivData.CivLongName; //.CivLongName;
            fleetData.CivShortName = thisCivData.CivShortName;
            fleetData.CivEnum = thisCivData.CivEnum;
            fleetData.PlayerId = thisCivData.PlayerId;
            //fleetData.FleetInt = fleetManager.GetNewFleetInt(thisCivData.CivEnum);
            //fleetData.Name = $"{thisCivData.CivShortName} Fleet {fleetData.FleetInt}";
            fleetData.Insignia = thisCivData.InsigniaSprite;
            fleetData.ShipsList = new List<ShipController>();
            var galaxyMenuUICon = GalaxyMenuUIController.Instance;
            galaxyMenuUICon.ResetClickMode();

            var newFleet = fleetManager.InstantiateFleet(null, sysController, fleetData, position, true);
            tempFleetController = newFleet;
            galaxyMenuUICon.ShowShipDeployForSystemNewFleet(sysController, newFleet);

        }
        public void ClickCancelShipManageButton()
        {
            var sd = ShipDeployMenuUIController.Instance;
            var galaxyUI = GalaxyMenuUIController.Instance;

            // Check if we're in merge mode
            bool isMergeMode = (galaxyUI.FleetLookingForShipMerge != null || galaxyUI.StarSystLookingForShipMerge != null);

            if (sd != null && sd.ShipDeployPanel != null && sd.ShipDeployPanel.activeInHierarchy)
            {
                if (isMergeMode)
                {
                    // Use merge commit for merge operations
                    sd.CommitMergeAndClose(CancelShipManageAfterCommit);
                }
                else
                {
                    // Use deploy commit for normal deploy operations
                    sd.CommitShipDeployForNewFleetAndClose(CancelShipManageAfterCommit);
                }

                return;
            }

            // Normal path
            CancelShipManageAfterCommit();
        }

        // New: run the cleanup logic *after* a commit has completed.
        public void CancelShipManageAfterCommit()
        {
            if (tempFleetController != null)
            {
                Debug.Log($"CancelShipManageAfterCommit (System): tempFleetController '{tempFleetController.name}' has {tempFleetController.FleetData.ShipsList.Count} ships");

                // Only destroy the fleet if it has NO ships
                if (tempFleetController.FleetData.ShipsList.Count == 0)
                {
                    Debug.Log($"Destroying empty fleet '{tempFleetController.name}'");

                    if (FleetManager.Instance.TempFogRevealerFleet != null)
                        FleetManager.Instance.RemoveFogWarRevealer(FleetManager.Instance.TempFogRevealerFleet);
                    FleetManager.Instance.TempFogRevealerFleet = null;

                    FleetManager.Instance.DestroyFleetController(tempFleetController);
                    tempFleetController = null;
                }
                else
                {
                    Debug.Log($"Keeping fleet '{tempFleetController.name}' with {tempFleetController.FleetData.ShipsList.Count} ships");
                    // Fleet has ships, so finalize it and keep it
                    tempFleetController = null; // Clear temp reference but don't destroy
                }
            }
            else
            {
                Debug.Log("CancelShipManageAfterCommit (System): No temp fleet to process, proceeding to UI cleanup");
            }

            var galaxyUI = GalaxyMenuUIController.Instance;
            MousePointerChanger.Instance.ResetCursor();
            if (cancelShipManagerButtonGO != null)
                cancelShipManagerButtonGO.SetActive(false);
            if (ShipDeployMenuUIController.Instance != null)
                ShipDeployMenuUIController.Instance.gameObject.SetActive(false);
            if (galaxyUI != null)
            {
                galaxyUI.ClickCancelShipDeployButton();
                galaxyUI.ResetClickMode();
                galaxyUI.CompleteShipExchange();
            }

            HideA_SystemMenuView();
        }

        /// <summary>
        /// Sets the build progress slider for facility construction.
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1.</param>
        public void SetBuildProgress(float progress)
        {
            if (SliderBuildProgress != null)
            {
                SliderBuildProgress.value = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Sets the build progress slider for ship construction.
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1.</param>
        public void SetShipBuildProgress(float progress)
        {
            if (ShipSliderBuildProgress != null)
            {
                ShipSliderBuildProgress.value = Mathf.Clamp01(progress);
            }
        }

        public void FindSysUIContainers()
        {
            if (SystemsMenuView != null && ASystemMenuView != null && SysListContainer != null)
            {
                Debug.Log("StarSysMenuUIController: All containers already assigned");
                return;
            }

            var canvasGalaxy = GameObject.Find("CanvasGalaxy");
            if (canvasGalaxy == null)
            {
                Debug.LogWarning("StarSysMenuUIController: CanvasGalaxy not found");
                return;
            }

            // ✅ Find SystemsMenuView (list view with scroll)
            if (SystemsMenuView == null)
            {
                SystemsMenuView = FindInHierarchy(canvasGalaxy.transform, "SystemsMenuView");
                Debug.Log($"StarSysMenuUIController: Found SystemsMenuView: {SystemsMenuView != null}");
            }

            // ✅ Find AStarSysMenuView (single system detail view)
            if (ASystemMenuView == null)
            {
                ASystemMenuView = FindInHierarchy(canvasGalaxy.transform, "AStarSysMenuView");
                Debug.Log($"StarSysMenuUIController: Found AStarSysMenuView: {ASystemMenuView != null}");
            }

            // ✅ Find SysListContainer (INSIDE SystemsMenuView/Viewport)
            if (SysListContainer == null && SystemsMenuView != null)
            {
                // Try to find inside SystemsMenuView first
                SysListContainer = FindInHierarchy(SystemsMenuView.transform, "SysListContainer");

                if (SysListContainer != null)
                {
                    Debug.Log($"StarSysMenuUIController: ✅ Found SysListContainer inside SystemsMenuView");
                }
                else
                {
                    // Fallback: search entire CanvasGalaxy
                    SysListContainer = FindInHierarchy(canvasGalaxy.transform, "SysListContainer");
                    Debug.Log($"StarSysMenuUIController: Found SysListContainer (fallback): {SysListContainer != null}");
                }
            }
        }

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
        public void MoveBackAnyStarSysUIGO()
        {
            listOfStarSysUiGos.RemoveAll(go => go == null);

            // Return any system UI from the detail view to SysListContainer and deactivate
            if (ASystemMenuView != null)
            {
                for (int i = ASystemMenuView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = ASystemMenuView.transform.GetChild(i);
                    if (child == null) continue;

                    var starSysUIFields = child.GetComponent<StarSysUI_Fields>();
                    var fleetUIFields = child.GetComponent<FleetUI_Fields>();

                    if (starSysUIFields != null && fleetUIFields == null)
                    {
                        if (SysListContainer != null)
                            child.SetParent(SysListContainer.transform, false);
                        child.gameObject.SetActive(false);
                    }
                }
                ASystemMenuView.SetActive(false);
            }

            // Deactivate all system UIs in the list container
            if (SysListContainer != null)
            {
                for (int i = 0; i < SysListContainer.transform.childCount; i++)
                {
                    var child = SysListContainer.transform.GetChild(i);
                    if (child != null && child.GetComponent<StarSysUI_Fields>() != null)
                        child.gameObject.SetActive(false);
                }
            }

            // Guard: stray system UIs in fleet view
            var aFleetView = FleetMenuUIController.Instance?.AFleetMenuView;
            if (aFleetView != null)
            {
                for (int i = aFleetView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = aFleetView.transform.GetChild(i);
                    if (child == null) continue;
                    if (child.GetComponent<StarSysUI_Fields>() != null)
                    {
                        Debug.LogError($"❌ SYSTEM UI '{child.name}' found in AFleetMenuView!");
                        if (SysListContainer != null)
                            child.SetParent(SysListContainer.transform, false);
                        child.gameObject.SetActive(false);
                    }
                }
            }

            ActiveStarSysController = null;
        }
        // ✅ Add this method to display power plant build info
        public void UpdatePowerPlantBuildUI(StarSysController selectedSystem)
        {
            if (selectedSystem == null || selectedSystem.StarSysData == null)
                return;

            var sysData = selectedSystem.StarSysData;
            var civData = CivManager.Instance.GetCivDataByCivEnum(sysData.CurrentOwnerCivEnum);

            // Check if build is allowed
            bool canBuild = PowerPlantBuildValidator.CanBuildPowerPlant(selectedSystem, out string reason);

            // Update UI elements (you'll need to add these to your UI prefab)
            // powerPlantCapacityText.text = PowerPlantBuildValidator.GetCapacityInfo(sysData);
            // powerOutputText.text = PowerPlantBuildValidator.GetPowerOutputInfo(sysData, civData);
            // buildPowerPlantButton.interactable = canBuild;
            // buildConstraintText.text = reason;

            Debug.Log($"Power Plant UI: {PowerPlantBuildValidator.GetCapacityInfo(sysData)}");
            Debug.Log($"  {PowerPlantBuildValidator.GetPowerOutputInfo(sysData, civData)}");
            Debug.Log($"  Can Build: {canBuild} - {reason}");
        }

        private void ToggleSysShipListExpansion(StarSysController sysCon, StarSysUI_Fields fields)
        {
            if (fields.ShipScrollView == null) return;
            var svRect = fields.ShipScrollView.GetComponent<RectTransform>();
            if (svRect == null) return;
            var grid = fields.shipContent?.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid == null) return;

            bool isCollapsed = svRect.sizeDelta.x <= fields.CollapsedShipScrollViewWidth + 1f;

            if (isCollapsed)
            {
                var canvas = fields.GetComponentInParent<Canvas>();
                float scale = canvas != null ? canvas.scaleFactor : 1f;

                // Left screen-space edge of the ShipScrollView (overlay canvas: world pos = screen pos)
                var corners = new Vector3[4];
                svRect.GetWorldCorners(corners);
                float leftScreenX = corners[0].x;

                float availableWidth = (Screen.width - leftScreenX - 20f) / scale;
                int newCols = Mathf.Max(2, Mathf.FloorToInt(
                    (availableWidth + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));

                svRect.sizeDelta = new Vector2(availableWidth, svRect.sizeDelta.y);
                grid.constraintCount = newCols;

                // Render on top of other system UI elements; button must come after scroll view
                fields.ShipScrollView.transform.SetAsLastSibling();
                fields.transform.SetAsLastSibling();
                // Move button last so it renders on top of the expanded ShipScrollView
                if (fields.ExpandShipsButton != null)
                    fields.ExpandShipsButton.transform.SetAsLastSibling();

                SetSysExpandButtonLabel(fields, true);
            }
            else
            {
                svRect.sizeDelta = new Vector2(fields.CollapsedShipScrollViewWidth, svRect.sizeDelta.y);
                grid.constraintCount = 2;
                SetSysExpandButtonLabel(fields, false);
            }

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(svRect);
        }

        private static void SetSysExpandButtonLabel(StarSysUI_Fields fields, bool expanded)
        {
            if (fields.ExpandShipsButton == null) return;
            var tmp = fields.ExpandShipsButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmp != null) tmp.text = expanded ? "◄" : "►";
        }

        public void RefreshQueueForSystem(StarSysController sysCon)
        {
            if (sysCon?.StarSysUIGameObject == null) return;
            var fields = sysCon.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
            if (fields != null)
                RefreshQueueDisplays(sysCon, fields);
        }

        public void RefreshQueueDisplays(StarSysController sysCon, StarSysUI_Fields fields)
        {
            RefreshShipQueue(sysCon, fields);
            RefreshFacilityQueue(sysCon, fields);
        }

        private void RefreshShipQueue(StarSysController sysCon, StarSysUI_Fields fields)
        {
            if (fields.yardQueueContent == null || shipyardQueueItemPrefab == null) return;

            // Content grows vertically; no horizontal scroll
            var fitter = fields.yardQueueContent.GetComponent<UnityEngine.UI.ContentSizeFitter>()
                         ?? fields.yardQueueContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;

            var sr = fields.yardQueueContent.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if (sr != null) sr.horizontal = false;

            // Destroy previous display items
            var toDestroy = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in fields.yardQueueContent)
                toDestroy.Add(child.gameObject);
            foreach (var go in toDestroy)
                Destroy(go);

            var queue = sysCon.sysShipBuildQueueList;
            if (queue == null || queue.Count == 0) return;

            int startIndex = sysCon.StarSysBuildManager.IsBuildingShip ? 1 : 0;
            for (int i = startIndex; i < queue.Count; i++)
            {
                var queueItemTransform = queue[i];
                if (queueItemTransform == null) continue;

                var drag = queueItemTransform.GetComponent<ShipBuildDrag>();
                if (drag == null) continue;

                var displayItem = Instantiate(shipyardQueueItemPrefab, fields.yardQueueContent);

                var rootImg = displayItem.GetComponent<Image>();
                if (rootImg != null && drag.ShipSprite != null)
                    rootImg.sprite = drag.ShipSprite;

                var nameTmp = displayItem.transform.Find("Ship Name (TMP)")?.GetComponent<TMPro.TMP_Text>();
                if (nameTmp != null)
                    nameTmp.text = drag.ShipType.ToString();

                var cancelBtn = displayItem.transform.Find("CancelButton")?.GetComponent<Button>();
                if (cancelBtn != null)
                {
                    var capturedItem = queueItemTransform;
                    var capturedSysCon = sysCon;
                    var capturedFields = fields;
                    cancelBtn.onClick.AddListener(() =>
                    {
                        capturedSysCon.sysShipBuildQueueList.Remove(capturedItem);
                        if (capturedItem != null)
                            Destroy(capturedItem.gameObject);
                        RefreshShipQueue(capturedSysCon, capturedFields);
                    });
                }
            }
        }

        private void RefreshFacilityQueue(StarSysController sysCon, StarSysUI_Fields fields)
        {
            if (fields.factoryQueueContent == null || factoryQueueItemPrefab == null) return;

            var fitter = fields.factoryQueueContent.GetComponent<UnityEngine.UI.ContentSizeFitter>()
                         ?? fields.factoryQueueContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;

            var sr = fields.factoryQueueContent.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if (sr != null) sr.horizontal = false;

            var toDestroy = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in fields.factoryQueueContent)
                toDestroy.Add(child.gameObject);
            foreach (var go in toDestroy)
                Destroy(go);

            var queue = sysCon.sysBuildQueueList;
            if (queue == null || queue.Count == 0) return;

            int startIndex = sysCon.StarSysBuildManager.IsBuildingFacility ? 1 : 0;
            for (int i = startIndex; i < queue.Count; i++)
            {
                var queueItemTransform = queue[i];
                if (queueItemTransform == null) continue;

                var drag = queueItemTransform.GetComponent<FactoryBuildItemDrag>();
                if (drag == null) continue;

                var displayItem = Instantiate(factoryQueueItemPrefab, fields.factoryQueueContent);

                var rootImg = displayItem.GetComponent<Image>();
                if (rootImg != null && drag.ShipSprite != null)
                    rootImg.sprite = drag.ShipSprite;

                var nameTmp = displayItem.transform.Find("Item Name (TMP)")?.GetComponent<TMPro.TMP_Text>();
                if (nameTmp != null)
                    nameTmp.text = drag.FacilityType.ToString();

                var cancelBtn = displayItem.transform.Find("CancelButton")?.GetComponent<Button>();
                if (cancelBtn != null)
                {
                    var capturedItem = queueItemTransform;
                    var capturedSysCon = sysCon;
                    var capturedFields = fields;
                    cancelBtn.onClick.AddListener(() =>
                    {
                        capturedSysCon.sysBuildQueueList.Remove(capturedItem);
                        if (capturedItem != null)
                            Destroy(capturedItem.gameObject);
                        RefreshFacilityQueue(capturedSysCon, capturedFields);
                    });
                }
            }
        }

        // ── Compact-row expand / collapse ────────────────────────────────────────────

        /// <summary>
        /// Expands the given system's full UI in the list, collapsing whichever system
        /// was previously expanded.  Clicking the already-expanded system collapses it.
        /// Called by SysCompactHeader when the player presses the expand button.
        /// </summary>
        public void ExpandSystem(StarSysController sysCon)
        {
            if (sysCon == null || sysCon == _currentExpandedSysCon) return;

            // Collapse whatever was open before and restore its Expand button
            if (_currentExpandedSysCon != null)
                CollapseSystemUI(_currentExpandedSysCon);

            var sysUI = sysCon.StarSysUIGameObject;
            if (sysUI == null) return;

            // Move to the top of the scroll list
            sysUI.transform.SetSiblingIndex(0);

            // Show full content, hide its own Expand button (nothing left to promote it
            // to), and refresh the dilithium value in the compact header
            var fields = sysUI.GetComponent<StarSysUI_Fields>();
            if (fields != null)
            {
                fields.expandedContent?.SetActive(true);
                fields.compactHeader?.SetExpandButtonActive(false);
                fields.compactHeader?.RefreshDilithium();
                fields.WireAIModeToggles(sysCon);
            }

            _currentExpandedSysCon = sysCon;
            RebuildListLayout();
        }

        private void CollapseSystemUI(StarSysController sysCon)
        {
            var sysUI = sysCon?.StarSysUIGameObject;
            if (sysUI == null) return;
            var fields = sysUI.GetComponent<StarSysUI_Fields>();
            fields?.expandedContent?.SetActive(false);
            fields?.compactHeader?.SetExpandButtonActive(true);
        }

        private void RebuildListLayout()
        {
            var rt = (SysListContainer != null)
                ? SysListContainer.GetComponent<RectTransform>()
                : null;
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
