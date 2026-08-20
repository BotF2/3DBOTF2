// Ignore Spelling: Anya BOTF

using BOTF3D.Combat;
using BOTF3D.Core;

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    public class FleetMenuUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        public static FleetMenuUIController Instance;

        [Header("Buttons (assign in Inspector)")]
        public Button saveCloseShipDeployButton;
        [Header("References (assign in Inspector)")]
        public GameObject FleetMenuView;
        public GameObject AFleetMenuView;
        public GameObject FleetListContainer; // Will be found at runtime if not assigned

        [Header("Private UI Elements")]
        [SerializeField] private GameObject shipDelployPanel;
        [SerializeField] private GameObject aFleetShipContainer;
        [SerializeField] private TMP_Text fleetName;
        [SerializeField] private TextMeshProUGUI destinationName;
        [SerializeField] private TextMeshProUGUI destinationCoordinates;
        [SerializeField] private GameObject selectDestinationCursorButtonGO;
        [SerializeField] private GameObject cancelDestinationButtonGO;
        [SerializeField] private GameObject dragDestinationTargetButtonGO;
        [SerializeField] private GameObject selectShipManagerCursorButtonGO;
        [SerializeField] private GameObject cancelFleetUIButtonGO;
        [SerializeField] private GameObject warpButtonUpGO;
        [SerializeField] private GameObject warpButtonDownGO;
        [SerializeField] private GameObject newFleetButtonGO;
        [SerializeField] private GameObject mergeFleetButtonGO;
        [SerializeField] private GameObject shipDeployButtonGO;

        [Header("Runtime lists")]
        [SerializeField] private List<GameObject> listOfFleetUiGos = new List<GameObject>();
        private FleetController activeFleetController;
        private FleetController tempFleetController;
        private FleetController lastFleetCon;

        private void Awake()
        {
            // ✅ Scene-based singleton
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("✅ FleetMenuUIController: Instance assigned");
            }
            else if (Instance != this)
            {
                Debug.LogWarning($"❌ Duplicate FleetMenuUIController found! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Find UI containers if not assigned
            FindFleetUIContainers();

            for (int i = 0; i < FleetManager.Instance.FleetControllerList.Count; i++)
            {
                var fleetCon = FleetManager.Instance.FleetControllerList[i];
                if (fleetCon != null && fleetCon.FleetUIGameObject != null)
                {
                    var child = fleetCon.FleetUIGameObject;
                    var childController = child.GetComponent<FleetAndSystemChildController>();
                    if (childController != null && childController.OriginalParentTransform == null)
                    {
                        if (child.transform.parent != null)
                        {
                            childController.OriginalParentTransform = child.transform.parent;
                        }
                        else if (FleetListContainer != null)
                        {
                            childController.OriginalParentTransform = FleetListContainer.transform;
                        }
                        else if (AFleetMenuView != null)
                        {
                            childController.OriginalParentTransform = AFleetMenuView.transform;
                        }
                    }
                }
            }

            // Initially hide views
            if (FleetMenuView != null)
                FleetMenuView.SetActive(false);
            if (AFleetMenuView != null)
                AFleetMenuView.SetActive(false);
        }

        // NEW: Find Fleet UI containers in CanvasGalaxy
        public void FindFleetUIContainers()
        {
            if (FleetListContainer != null)
            {
                Debug.Log("FleetMenuUIController: FleetListContainer already assigned");
                return; // Already found
            }

            var canvasGalaxy = GameObject.Find("CanvasGalaxy");
            if (canvasGalaxy == null)
            {
                Debug.LogWarning("FleetMenuUIController: CanvasGalaxy not found - cannot find FleetListContainer");
                return;
            }

            FleetListContainer = FindInHierarchy(canvasGalaxy.transform, "FleetListContainer");

            if (FleetListContainer == null)
            {
                FleetListContainer = FindInHierarchy(canvasGalaxy.transform, "ContentFleetUIGO");
            }

            if (FleetListContainer == null)
            {
                FleetListContainer = FindInHierarchy(canvasGalaxy.transform, "FleetContent");
            }

            Debug.Log($"FleetMenuUIController: Found FleetListContainer: {FleetListContainer != null}");

            // Also find FleetMenuView if not assigned
            if (FleetMenuView == null)
            {
                FleetMenuView = FindInHierarchy(canvasGalaxy.transform, "FleetMenuView");
                Debug.Log($"FleetMenuUIController: Found FleetMenuView: {FleetMenuView != null}");
            }

            // Also find AFleetMenuView if not assigned
            if (AFleetMenuView == null)
            {
                AFleetMenuView = FindInHierarchy(canvasGalaxy.transform, "AFleetMenuView");
                Debug.Log($"FleetMenuUIController: Found AFleetMenuView: {AFleetMenuView != null}");
            }
        }

        // Helper method for recursive search
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

        public void SetupFleetUIData()
        {
            if (FleetManager.Instance == null) return;

            for (int j = 0; j < FleetManager.Instance.FleetControllerList.Count; j++)
            {
                FleetController fleetCon = FleetManager.Instance.FleetControllerList[j];
                if (fleetCon == null || fleetCon.FleetUIGameObject == null) continue;

                if (GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum))
                {
                    // wire up individual fleet UI (handles both new and existing)
                    SetupFleetUIElements(fleetCon, fleetCon.FleetUIGameObject);

                    // Ensure it is in the tracking list and parented correctly if shown in the list
                    if (!listOfFleetUiGos.Contains(fleetCon.FleetUIGameObject))
                    {
                        listOfFleetUiGos.Add(fleetCon.FleetUIGameObject);
                    }

                    fleetCon.FleetUIGameObject.SetActive(true);

                    var childController = fleetCon.FleetUIGameObject.GetComponent<FleetAndSystemChildController>();
                    if (childController != null && childController.OriginalParentTransform == null)
                    {
                        childController.OriginalParentTransform = FleetListContainer.transform;
                    }

                    // Parent to the scrollable list container if it's not already there
                    if (fleetCon.FleetUIGameObject.transform.parent != FleetListContainer.transform)
                    {
                        fleetCon.FleetUIGameObject.transform.SetParent(FleetListContainer.transform, false);
                    }

                    // Diagnostic for the "fleet owner can't see their own fleet on the galaxy map"
                    // bug: this UI list is confirmed reachable (fleetCon/FleetUIGameObject are both
                    // non-null here) - dump the SEPARATE map-icon state (FleetChildFields/Insignia)
                    // at the same moment, to see whether the two ever actually diverge.
                    var diagFields = fleetCon.GetComponent<FleetChildFields>();
                    GameLogger.Log(GameLogger.LogCategory.Fleet,
                        $"[VisibilityDiag] SetupFleetUIData: fleet '{fleetCon.name}' civ={fleetCon.FleetData.CivEnum} " +
                        $"gameObject.activeInHierarchy={fleetCon.gameObject.activeInHierarchy} " +
                        $"position={fleetCon.transform.position} " +
                        $"InsigniaGO.activeSelf={(diagFields?.InsigniaGO != null ? diagFields.InsigniaGO.activeSelf.ToString() : "null")} " +
                        $"InsigniaSpriteRenderer.enabled={(diagFields?.InsigniaGO != null ? diagFields.InsigniaGO.GetComponent<SpriteRenderer>()?.enabled.ToString() : "null")}",
                        this);
                }
            }
        }
        /// <summary>
        /// Shows the detailed view of a single fleet
        /// CALLED BY: FleetController.OnMouseDown() or GalaxyMenuUIController.OpenMenu(Menu.AFleetMenu)
        /// </summary>
        public void SetActiveSetParentUIGO(FleetController theFleetCon)
        {
            if (theFleetCon == null)
            {
                Debug.LogWarning("SetActiveSetParentUIGO: theFleetCon is null");
                return;
            }

            // CRITICAL: Find containers if needed
            if (FleetListContainer == null || AFleetMenuView == null)
            {
                FindFleetUIContainers();
            }

            // Menu system handles cleanup when transitioning between menus
            // Don't call MoveBack here - it deactivates UIs

            // Evict any OTHER fleet's UI still parented in AFleetMenuView from a previous
            // AFleetMenu view (e.g. a "New Fleet" deploy UI left open, then a different fleet is
            // clicked on the map). GalaxyMenuUIController.HideMenuViews() only toggles
            // AFleetMenuView's own active state during a menu transition - it never moves a stale
            // child back to home storage, so that old fleet's UI silently reappears alongside the
            // new one when this container is reactivated.
            if (AFleetMenuView != null)
            {
                GameObject homeContainer = FleetManager.Instance?.FleetUI_ListContainer ?? FleetListContainer;
                for (int i = AFleetMenuView.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = AFleetMenuView.transform.GetChild(i);
                    if (child.gameObject == theFleetCon.FleetUIGameObject) continue;
                    if (child.GetComponent<FleetUI_Fields>() == null) continue; // leave system UIs alone
                    if (homeContainer != null) child.SetParent(homeContainer.transform, false);
                    child.gameObject.SetActive(false);
                }
            }

            if (theFleetCon.FleetUIGameObject == null)
            {
                Debug.LogError($"SetActiveSetParentUIGO: Fleet '{theFleetCon.name}' has no FleetUIGameObject!");
                return;
            }

            // ✅ Ensure this is actually a FLEET UI
            var fleetUIFields = theFleetCon.FleetUIGameObject.GetComponent<FleetUI_Fields>();
            var starSysUIFields = theFleetCon.FleetUIGameObject.GetComponent<StarSysUI_Fields>();

            if (fleetUIFields == null)
            {
                Debug.LogError($"SetActiveSetParentUIGO: Fleet UI has no FleetUI_Fields component!");
                return;
            }

            if (starSysUIFields != null)
            {
                Debug.LogError($"SetActiveSetParentUIGO: This is a SYSTEM UI, not a fleet UI!");
                return;
            }

            // ✅ Move to detail view and ACTIVATE
            theFleetCon.FleetUIGameObject.transform.SetParent(AFleetMenuView.transform, false);
            theFleetCon.FleetUIGameObject.SetActive(true);

            // ✅ CRITICAL: ALWAYS re-wire buttons and sync ships when opening detail view!
            SetupFleetUIElements(theFleetCon, theFleetCon.FleetUIGameObject);

            // ✅ Set activeFleetController so SetAsDestination() can find it!
            activeFleetController = theFleetCon;
            lastFleetCon = theFleetCon;

            Debug.Log($"SetActiveSetParentUIGO: Set activeFleetController to '{theFleetCon.name}' and re-wired buttons");
        }
        public void MoveTheFleetUIGO(GameObject fleetConGO)
        {
            for (int i = 0; i < listOfFleetUiGos.Count; i++)
            {
                if (listOfFleetUiGos[i] == fleetConGO)
                {
                    listOfFleetUiGos[i].transform.SetParent(AFleetMenuView.transform, false);
                    return;
                }
            }
        }
        public void MoveBackAnyaFleetUIGO()
        {
            Debug.Log("=== MoveBackAnyaFleetUIGO: Starting ===");

            // ✅ Clean up destroyed references
            listOfFleetUiGos.RemoveAll(go => go == null);
            Debug.Log($"  Cleaned tracking list, now has {listOfFleetUiGos.Count} valid entries");

            // ✅ Get home storage container from FleetManager
            GameObject homeContainer = FleetManager.Instance?.FleetUI_ListContainer;

            if (homeContainer == null)
            {
                Debug.LogWarning("  ⚠️ FleetUI_ListContainer not found! Using FleetListContainer fallback.");
                homeContainer = FleetListContainer;
            }

            if (homeContainer == null)
            {
                Debug.LogWarning("  ⚠️ No valid container to move fleet UIs to!");
                return;
            }

            // Move from AFleetMenuView (detail view)
            if (AFleetMenuView != null)
            {
                Debug.Log($"  Checking AFleetMenuView ({AFleetMenuView.transform.childCount} children)");

                for (int i = AFleetMenuView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = AFleetMenuView.transform.GetChild(i);
                    if (child == null) continue;

                    var fleetUIFields = child.GetComponent<FleetUI_Fields>();
                    var starSysUIFields = child.GetComponent<StarSysUI_Fields>();

                    if (fleetUIFields != null && starSysUIFields == null)
                    {
                        // This is a fleet UI - move to home and DEACTIVATE
                        Debug.Log($"    Moving FLEET UI '{child.name}' from AFleetMenuView to home storage");
                        child.SetParent(homeContainer.transform, false);
                        child.gameObject.SetActive(false);
                    }
                    else if (starSysUIFields != null)
                    {
                        Debug.Log($"    Moving SYSTEM UI '{child.name}' from AFleetMenuView back to SysListContainer");
                        var sysListContainer = StarSysMenuUIController.Instance?.SysListContainer;
                        if (sysListContainer != null)
                            child.SetParent(sysListContainer.transform, false);
                        child.gameObject.SetActive(false);
                    }
                }

                AFleetMenuView.SetActive(false);
            }

            // ✅ NEW: Also check ASystemMenuView for fleet UIs (from fleet-to-system merge operations)
            if (StarSysMenuUIController.Instance != null && StarSysMenuUIController.Instance.ASystemMenuView != null)
            {
                var aSysView = StarSysMenuUIController.Instance.ASystemMenuView;
                Debug.Log($"  Checking ASystemMenuView ({aSysView.transform.childCount} children)");

                for (int i = aSysView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = aSysView.transform.GetChild(i);
                    if (child == null) continue;

                    var fleetUIFields = child.GetComponent<FleetUI_Fields>();
                    var starSysUIFields = child.GetComponent<StarSysUI_Fields>();

                    // Only move FLEET UIs (not system UIs, those are handled by MoveBackAnyStarSysUIGO)
                    if (fleetUIFields != null && starSysUIFields == null)
                    {
                        Debug.Log($"    Moving FLEET UI '{child.name}' from ASystemMenuView to home storage");
                        child.SetParent(homeContainer.transform, false);
                        child.gameObject.SetActive(false);
                    }
                }
            }

            // ✅ Clean up FleetManager's fleet list (remove destroyed fleets)
            if (FleetManager.Instance != null)
            {
                int beforeCount = FleetManager.Instance.FleetControllerList.Count;
                FleetManager.Instance.FleetControllerList.RemoveAll(f => f == null);
                int afterCount = FleetManager.Instance.FleetControllerList.Count;

                if (beforeCount != afterCount)
                {
                    Debug.Log($"  Removed {beforeCount - afterCount} destroyed fleet references from FleetManager");
                }
            }

            activeFleetController = null;
            Debug.Log("=== MoveBackAnyaFleetUIGO: Complete - all fleet UIs moved and DEACTIVATED ===");
        }
        public void SetupFleetUIElements(FleetController fleetCon, GameObject newFleetUIGO)
        {
            if (fleetCon == null || newFleetUIGO == null)
            {
                Debug.LogWarning("SetupFleetUIElements: fleetCon or newFleetUIGO is null");
                return;
            }

            // CRITICAL: Ensure FleetListContainer exists
            if (FleetListContainer == null)
            {
                FindFleetUIContainers();

                if (FleetListContainer == null)
                {
                    Debug.LogError($"SetupFleetUIElements: FleetListContainer is NULL! Cannot setup fleet UI for {fleetCon.name}");
                    return;
                }
            }

            FleetUI_Fields uiFields = newFleetUIGO.GetComponent<FleetUI_Fields>();
            if (uiFields == null)
            {
                Debug.LogError($"SetupFleetUIElements: No FleetUI_Fields component found on {newFleetUIGO.name}!");
                return;
            }

            // ✅ 1. Set ShipListUIParent immediately (before any ship UI creation)
            if (uiFields.FleetShipContentGO != null)
            {
                fleetCon.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;

                // 8 ships per row, each cell 140×25. Rows wrap downward when expanded.
                // In collapsed state the ShipScrollView height clips to one visible row.
                var grid = uiFields.FleetShipContentGO.GetComponent<UnityEngine.UI.GridLayoutGroup>()
                           ?? uiFields.FleetShipContentGO.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                grid.cellSize        = new Vector2(140, 25);
                grid.spacing         = new Vector2(4, 4);
                grid.startAxis       = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
                grid.constraint      = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 8;

                // Height controlled by the ShipScrollView RectTransform + Mask (collapsed)
                // or by ContentSizeFitter (expanded). Width fills the container.
                var fitter = uiFields.FleetShipContentGO.GetComponent<UnityEngine.UI.ContentSizeFitter>()
                             ?? uiFields.FleetShipContentGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit   = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;

                // Fix Content RectTransform so the GridLayoutGroup starts items
                // at the top-left of the Viewport rather than below the panel.
                var contentRect = uiFields.FleetShipContentGO.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    contentRect.anchorMin        = new Vector2(0f, 1f); // top-left anchor
                    contentRect.anchorMax        = new Vector2(1f, 1f); // stretch horizontally
                    contentRect.pivot            = new Vector2(0f, 1f); // pivot at top-left
                    contentRect.anchoredPosition = Vector2.zero;        // flush with viewport top
                    contentRect.sizeDelta        = Vector2.zero;        // width = viewport, height by fitter
                }

                if (uiFields.ShipScrollView != null)
                {
                    var sr = uiFields.ShipScrollView.GetComponent<UnityEngine.UI.ScrollRect>();

                    // Viewport must fill the ShipScrollView so the Mask clips correctly
                    RectTransform viewportRect = sr != null && sr.viewport != null
                        ? sr.viewport
                        : uiFields.ShipScrollView.transform.childCount > 0
                            ? uiFields.ShipScrollView.transform.GetChild(0).GetComponent<RectTransform>()
                            : null;

                    if (viewportRect != null)
                    {
                        viewportRect.anchorMin        = Vector2.zero;
                        viewportRect.anchorMax        = Vector2.one;
                        viewportRect.sizeDelta        = Vector2.zero;
                        viewportRect.anchoredPosition = Vector2.zero;
                    }

                    // Disable ScrollRect — prevents scroll-wheel events reaching the galaxy map.
                    // The Viewport Mask stays enabled to clip overflow in collapsed state.
                    if (sr != null) sr.enabled = false;

                    // Collapsed height — one visible row
                    var svRect = uiFields.ShipScrollView.GetComponent<RectTransform>();
                    if (svRect != null)
                        svRect.sizeDelta = new Vector2(svRect.sizeDelta.x, uiFields.CollapsedShipViewHeight);
                }

                // Show the expand button only when the fleet exceeds 6 full rows (48 ships).
                // Smaller fleets fit within the normal Ship Scroll View height.
                if (uiFields.ExpandShipsButton != null)
                {
                    int shipCount   = fleetCon.FleetData?.ShipsList?.Count ?? 0;
                    bool needsExpand = shipCount > 6 * 8;

                    uiFields.ExpandShipsButton.gameObject.SetActive(needsExpand);

                    if (needsExpand)
                    {
                        uiFields.ExpandShipsButton.onClick.RemoveAllListeners();
                        uiFields.ExpandShipsButton.onClick.AddListener(() => ToggleShipListExpansion(fleetCon, uiFields));
                        SetExpandButtonLabel(uiFields, false);
                    }
                }
            }

            // ✅ ONE-TIME SETUP: Only do this if fleet is NEW (not in tracking list)
            if (!listOfFleetUiGos.Contains(fleetCon.FleetUIGameObject))
            {
                Debug.Log($"SetupFleetUIElements: First-time setup for fleet '{fleetCon.name}'");

                listOfFleetUiGos.Add(fleetCon.FleetUIGameObject);

                var fleetAndStarSys = fleetCon.FleetUIGameObject.GetComponent<FleetAndSystemChildController>();
                if (fleetAndStarSys != null)
                {
                    if (fleetAndStarSys.OriginalParentTransform == null)
                    {
                        fleetAndStarSys.OriginalParentTransform = FleetListContainer.transform;
                    }
                }

            }
            else
            {
                Debug.Log($"SetupFleetUIElements: Re-wiring existing fleet '{fleetCon.name}'");
            }

            // Refresh mini map position every time the UI opens (not just first-time setup).
            // This block first runs at fleet-registration time (FleetManager.InstantiateFleetUIGameObject,
            // triggered by the client's OnCivEnumChanged reconstruction) - on a non-host client that's
            // before NetworkTransform has delivered its first position sync for this fleet, so
            // fleetCon.transform.position was still stale there. Re-running on every open (in particular
            // when the player actually clicks the fleet to view it) catches the by-then-synced position.
            // FleetData.Position itself can't be used as the source here - it's never populated for a
            // non-host client's locally-reconstructed fleets (see FleetController.OnCivEnumChanged; it's
            // a plain field, not a SyncVar, and only the server writes it).
            fleetCon.UpdateMinimapPosition();

            // ✅ 2, 3 & 4. Sync ships (Always run for both new and existing fleets to catch any missing UIs)
            Debug.Log($"🧩 SetupFleetUIElements: '{fleetCon.name}' opening with FleetData.ShipsList.Count={fleetCon.FleetData?.ShipsList?.Count ?? -1}, FleetShipContentGO={(uiFields.FleetShipContentGO != null ? "SET" : "NULL")}");
            if (uiFields.FleetShipContentGO != null && fleetCon.FleetData?.ShipsList != null)
            {
                // Grid position follows sibling index, not ShipsList order - sort here (by ShipType)
                // rather than reordering FleetData.ShipsList itself, since other systems (ShipID
                // sequencing, combat order assignment) depend on ships staying in add order.
                var sortedShips = fleetCon.FleetData.ShipsList
                    .Where(s => s != null)
                    .OrderBy(s => (int)s.ShipData.ShipType)
                    .ToList();

                for (int i = 0; i < sortedShips.Count; i++)
                {
                    var shipCon = sortedShips[i];

                    // 2. Create the UI item if it doesn't exist yet
                    if (shipCon.ShipListUIGameObject == null)
                    {
                        ShipManager.Instance?.InstantiateShipListUIGameObject(shipCon, fleetCon.gameObject);
                        Debug.Log($"  Created missing ship UI for '{shipCon.ShipData?.ShipName}'");
                    }

                    // 3. Re-parent if it drifted to the wrong container
                    if (shipCon.ShipListUIGameObject != null &&
                        shipCon.ShipListUIGameObject.transform.parent != uiFields.FleetShipContentGO.transform)
                    {
                        shipCon.ShipListUIGameObject.transform.SetParent(uiFields.FleetShipContentGO.transform, false);
                        shipCon.ShipListUIGameObject.SetActive(true);
                        Debug.Log($"  Re-parented ship UI '{shipCon.ShipData?.ShipName}' to FleetShipContent");
                    }

                    // 4. Keep grid position grouped by ShipType regardless of ShipsList add order
                    if (shipCon.ShipListUIGameObject != null)
                    {
                        shipCon.ShipListUIGameObject.transform.SetSiblingIndex(i);
                    }
                }

                // 5. Flush any items that landed in the pending queue
                ShipManager.Instance?.ProcessPendingShipUIs();
            }

            // ✅ BUTTON WIRING: ALWAYS runs
            // Design intent: one Select Destination button, one Cancel Destination button.
            // The script detects whether a map click targets a fleet (intercept) or a fixed object
            // automatically — the player never needs to choose a different button.
            // SelectDestination, SelectDestinationCursor, and InterceptTargetButton all alias the
            // same Button in the prefab; CancelDestination and CancelInterceptButton do the same.
            // Only wire through SelectDestination and CancelDestination to avoid listener overwrites.
            bool hasActiveDestination = fleetCon.FleetData?.Destination != null
                && fleetCon.FleetData.Destination != FleetManager.Instance?.GalaxyCenter;
            bool showCancel = hasActiveDestination || fleetCon.FleetData?.InterceptTarget != null;

            uiFields.DestinationDragTarget.gameObject.SetActive(!hasActiveDestination);
            uiFields.DestinationDragTarget.onClick.RemoveAllListeners();
            uiFields.DestinationDragTarget.onClick.AddListener(() => fleetCon.GetPlayerDefinedTargetDestination(fleetCon));
            dragDestinationTargetButtonGO = uiFields.DestinationDragTarget.gameObject;

            uiFields.SelectDestination.gameObject.SetActive(true);
            uiFields.SelectDestination.onClick.RemoveAllListeners();
            uiFields.SelectDestination.onClick.AddListener(() => SelectedDestinationCursor(fleetCon));
            selectDestinationCursorButtonGO = uiFields.SelectDestination.gameObject;

            uiFields.CancelDestination.gameObject.SetActive(showCancel);
            uiFields.CancelDestination.onClick.RemoveAllListeners();
            // AbortPendingConvoyMerge() alongside the stop-movement call: this is the only UI-driven
            // cancel, so it's the one place a pending merge should actually be abandoned (see
            // AbortPendingConvoyMerge's doc comment for why this can't live inside
            // ClickCancelDestinationButton itself).
            uiFields.CancelDestination.onClick.AddListener(() => { fleetCon.ClickCancelDestinationButton(); fleetCon.AbortPendingConvoyMerge(); });
            cancelDestinationButtonGO = uiFields.CancelDestination.gameObject;

            uiFields.WarpUp.gameObject.SetActive(true);
            uiFields.WarpUp.onClick.RemoveAllListeners();
            uiFields.WarpUp.onClick.AddListener(() => fleetCon.FleetOnWarpUpClick(fleetCon));
            warpButtonUpGO = uiFields.WarpUp.gameObject;

            uiFields.WarpDown.gameObject.SetActive(true);
            uiFields.WarpDown.onClick.RemoveAllListeners();
            uiFields.WarpDown.onClick.AddListener(() => fleetCon.FleetOnWarpDownClick(fleetCon));
            warpButtonDownGO = uiFields.WarpDown.gameObject;

            saveCloseShipDeployButton.gameObject.SetActive(true);
            saveCloseShipDeployButton.onClick.RemoveAllListeners();
            saveCloseShipDeployButton.onClick.AddListener(() => fleetCon.CloseShipDeploy(fleetCon));

            uiFields.NewFleetButton.gameObject.SetActive(true);
            uiFields.NewFleetButton.onClick.RemoveAllListeners();
            uiFields.NewFleetButton.onClick.AddListener(() => ClickNewFleetButton(fleetCon));
            newFleetButtonGO = uiFields.NewFleetButton.gameObject;

            uiFields.MergeFleetsButton.gameObject.SetActive(true);
            uiFields.MergeFleetsButton.onClick.RemoveAllListeners();
            uiFields.MergeFleetsButton.onClick.AddListener(() => ClickMergeFleetButton(fleetCon));
            mergeFleetButtonGO = uiFields.MergeFleetsButton.gameObject;

            uiFields.ShipDeployButton.gameObject.SetActive(true);
            uiFields.ShipDeployButton.onClick.RemoveAllListeners();
            uiFields.ShipDeployButton.onClick.AddListener(() => FleetClickedShipDeployButton(fleetCon));
            shipDeployButtonGO = uiFields.ShipDeployButton.gameObject;

            uiFields.CancelShipManagerButton.gameObject.SetActive(true);
            uiFields.CancelShipManagerButton.onClick.RemoveAllListeners();
            uiFields.CancelShipManagerButton.onClick.AddListener(() => CancelFleetUIButton());
            cancelFleetUIButtonGO = uiFields.CancelShipManagerButton.gameObject;

            // Both contact fields point at the same system this fleet is contact with at any given
            // moment (only one is ever set - see FleetController.OnTriggerEnter). Evaluate button
            // state off the referenced system's LIVE StarSysData flags rather than which field it
            // happened to land in, so a system that finishes terraforming (IsHabitable flips true)
            // while this fleet is still sitting there is picked up automatically without needing a
            // fresh OnTriggerEnter - see StarSysController.TerraformSystem, which deliberately no
            // longer nulls TerraformableSystem so this reference survives that transition.
            var contactedSystem = fleetCon.FleetData?.ColonizableSystem ?? fleetCon.FleetData?.TerraformableSystem;
            bool systemIsUninhabited = contactedSystem != null
                && (int)contactedSystem.StarSysData.CurrentOwnerCivEnum >= (int)CivEnum.ZZUNINHABITED1;
            bool hasTransport = fleetCon.FleetData.ShipsList.Any(s => s != null && s.ShipData != null
                && s.ShipData.ShipType == ShipType.Transport && !s.ShipData.Distroyed);

            // Colonize: only ever shown once the contacted system is actually habitable (whether it
            // started that way, or just finished terraforming), and not already mid-colonization.
            bool systemIsHabitableNow = contactedSystem != null
                && contactedSystem.StarSysData.IsHabitable
                && !contactedSystem.StarSysData.IsColonizing;
            bool canColonize = systemIsHabitableNow && hasTransport;

            uiFields.ColonizeButton.gameObject.SetActive(systemIsHabitableNow);
            uiFields.ColonizeButton.interactable = canColonize;
            uiFields.ColonizeButton.onClick.RemoveAllListeners();
            uiFields.ColonizeButton.onClick.AddListener(() => ClickColonizeButton(fleetCon));

            // Terraform: only shown while the contacted system still needs terraforming - hidden
            // (not just non-interactable) the moment it's already habitable, whether it arrived that
            // way or just finished terraforming.
            bool systemNeedsTerraforming = contactedSystem != null
                && !contactedSystem.StarSysData.IsHabitable
                && contactedSystem.StarSysData.IsTerraformable == true
                && !contactedSystem.StarSysData.IsTerraforming;
            bool canTerraform = systemNeedsTerraforming && hasTransport;

            uiFields.TerraformButton.gameObject.SetActive(systemNeedsTerraforming);
            uiFields.TerraformButton.interactable = canTerraform;
            uiFields.TerraformButton.onClick.RemoveAllListeners();
            uiFields.TerraformButton.onClick.AddListener(() => ClickTerraformButton(fleetCon));

            // Claim System: shown for as long as the contacted system remains uninhabited (sentinel-
            // owned) - no Transport needed, just plants the fleet's civ's insignia (see
            // StarSysController.ClaimSystem). Hides itself the instant Terraform/Colonize/Claim
            // claims real ownership, since CurrentOwnerCivEnum then stops being a ZZUNINHABITED* value.
            uiFields.ClaimSystemButton.gameObject.SetActive(systemIsUninhabited);
            uiFields.ClaimSystemButton.interactable = systemIsUninhabited;
            uiFields.ClaimSystemButton.onClick.RemoveAllListeners();
            uiFields.ClaimSystemButton.onClick.AddListener(() => ClickClaimSystemButton(fleetCon));

            // ✅ TEXT BINDINGS: Always update
            uiFields.FleetNameText.text = fleetCon.FleetData.FleetName;
            uiFields.DestinationName.gameObject.SetActive(true);
            destinationName = uiFields.DestinationName;
            uiFields.DestinationName.text = "";
            uiFields.DestinationCoordinates.gameObject.SetActive(true);
            destinationCoordinates = uiFields.DestinationCoordinates;
            uiFields.DestinationCoordinates.text = "";
            uiFields.WarpValueText.text = fleetCon.FleetData.CurrentWarpFactor.ToString("0.0");

            // ✅ SLIDER WIRING: Always re-wire
            uiFields.WarpSlider.onValueChanged.RemoveAllListeners();
            uiFields.WarpSlider.value = fleetCon.FleetData.CurrentWarpFactor;
            uiFields.WarpSlider.maxValue = fleetCon.FleetData.MaxWarpFactor;
            uiFields.WarpSlider.onValueChanged.AddListener((value) => fleetCon.SliderOnValueChange(value));

            Debug.Log($"SetupFleetUIElements: Complete for fleet '{fleetCon.name}' - buttons wired");
        }
        private void FleetClickedShipDeployButton(FleetController fleetCon)
        {
            // ✅ Destroy any existing player-defined target for this fleet
            if (fleetCon != null && fleetCon.TargetController != null)
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetCon);
            }

            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                galaxyUI.WhatFleetIsLookingForShipDeploy(fleetCon);
                galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipDeploy);
                MousePointerChanger.Instance.SetShipExchangeCursor();
                ShipDeployMenuUIController.Instance.TopFleet = fleetCon;
            }
        }
        private void ClickColonizeButton(FleetController fleetCon)
        {
            if (fleetCon == null || fleetCon.FleetData == null) return;
            // Falls back to TerraformableSystem so Colonize also works once a previously-terraformed
            // contact system becomes habitable (see the contactedSystem logic in
            // SetupFleetUIElements/ StarSysController.TerraformSystem for why that field is still set).
            var sysCon = fleetCon.FleetData.ColonizableSystem ?? fleetCon.FleetData.TerraformableSystem;
            if (sysCon == null) return;

            var transport = fleetCon.FleetData.ShipsList.FirstOrDefault(s => s != null && s.ShipData != null
                && s.ShipData.ShipType == ShipType.Transport && !s.ShipData.Distroyed);
            if (transport == null) return;

            if (sysCon.ColonizeWithTransport(transport))
                SetupFleetUIData(); // refresh so the Colonize button + ship list reflect the new state
        }
        private void ClickTerraformButton(FleetController fleetCon)
        {
            if (fleetCon == null || fleetCon.FleetData == null) return;
            var sysCon = fleetCon.FleetData.TerraformableSystem;
            if (sysCon == null) return;

            var transport = fleetCon.FleetData.ShipsList.FirstOrDefault(s => s != null && s.ShipData != null
                && s.ShipData.ShipType == ShipType.Transport && !s.ShipData.Distroyed);
            if (transport == null) return;

            if (sysCon.TerraformSystem(transport))
                SetupFleetUIData(); // refresh so the Terraform/Claim buttons reflect the new state
        }
        private void ClickClaimSystemButton(FleetController fleetCon)
        {
            if (fleetCon == null || fleetCon.FleetData == null) return;
            var sysCon = fleetCon.FleetData.ColonizableSystem ?? fleetCon.FleetData.TerraformableSystem;
            if (sysCon == null) return;

            if (sysCon.ClaimSystem(fleetCon.FleetData.CivController))
                SetupFleetUIData(); // refresh so the Claim/Colonize/Terraform buttons reflect the new state
        }
        private void ClickMergeFleetButton(FleetController fleetClickingMerge)
        {
            if (fleetClickingMerge.TargetController != null)
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetClickingMerge);
            }
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                galaxyUI.WhatFleetIsLookingForMerge(fleetClickingMerge);
                galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipMerge);
                MousePointerChanger.Instance.SetShipExchangeCursor();
                ShipDeployMenuUIController.Instance.TopFleet = fleetClickingMerge;
            }
        }
        private void ClickNewFleetButton(FleetController currentFleetCon)
        {
            if (currentFleetCon == null || currentFleetCon.FleetData == null) return;
            if (currentFleetCon.TargetController != null)
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(currentFleetCon);
            if (currentFleetCon.FleetData.ShipsList.Count < 2) return;

            MousePointerChanger.Instance.ResetCursor();
            ShipDeployMenuUIController.Instance.TopFleet = currentFleetCon;

            Debug.Log($"ClickNewFleetButton: requesting server-side split fleet from '{currentFleetCon.name}'.");
            PlayerManager.Instance?.LocalPlayerController?.SubmitCreateSplitFleet(currentFleetCon);
        }

        public void OnSplitFleetCreated(uint sourceFleetNetId, uint newFleetNetId)
        {
            StartCoroutine(ResolveAndShowSplitDeployUI(sourceFleetNetId, newFleetNetId));
        }

        private System.Collections.IEnumerator ResolveAndShowSplitDeployUI(uint sourceFleetNetId, uint newFleetNetId)
        {
            FleetController sourceFleet = null, newFleet = null;
            for (int attempt = 0; attempt < 60; attempt++) // ~1s at 60fps, generous margin for the spawn message to arrive
            {
                if (Mirror.NetworkClient.spawned.TryGetValue(sourceFleetNetId, out var srcIdentity)) sourceFleet = srcIdentity.GetComponent<FleetController>();
                if (Mirror.NetworkClient.spawned.TryGetValue(newFleetNetId, out var newIdentity)) newFleet = newIdentity.GetComponent<FleetController>();
                if (sourceFleet != null && newFleet != null) break;
                yield return null;
            }
            if (sourceFleet == null || newFleet == null)
            {
                Debug.LogError($"ResolveAndShowSplitDeployUI: timed out waiting for fleets to spawn (source={sourceFleetNetId}, new={newFleetNetId}).");
                yield break;
            }

            tempFleetController = newFleet;
            Debug.Log($"ResolveAndShowSplitDeployUI: opening deploy UI for new fleet '{newFleet.name}' split from '{sourceFleet.name}'.");
            GalaxyMenuUIController.Instance.ShowShipDeployForFleetNewFleet(sourceFleet, newFleet);
        }
        private void CancelFleetUIButton()
        {
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                galaxyUI.CloseMenu(Menu.AFleetMenu);
                galaxyUI.CloseMenu(Menu.FleetMenu);
                MousePointerChanger.Instance.ResetCursor();
            }
        }
        public void ClickCancelShipManageButton()
        {
            var sd = ShipDeployMenuUIController.Instance;
            var galaxyUI = GalaxyMenuUIController.Instance;

            // Check if we're in merge mode
            bool isMergeMode = (galaxyUI.FleetLookingForShipMerge != null || galaxyUI.StarSystLookingForShipMerge != null);

            // ✅ Check if we're in NEW FLEET mode (bottom fleet is the temp fleet we just created)
            bool isNewFleetMode = (tempFleetController != null && sd.BottomFleet == tempFleetController);

            if (sd != null && sd.ShipDeployPanel != null && sd.ShipDeployPanel.activeInHierarchy)
            {
                if (isMergeMode)
                {
                    // Use merge commit for merge operations
                    sd.CommitMergeAndClose(CancelShipManageAfterCommit);
                }
                else if (isNewFleetMode)
                {
                    // ✅ NEW FLEET: Ships are already correctly assigned via drag/drop
                    sd.CommitShipDeployForNewFleetAndClose(CancelShipManageAfterCommit);
                }
                else
                {
                    // ✅ REGULAR DEPLOY: Need to reconcile ship lists from TopSlot/BottomSlot
                    sd.CommitShipDeployAndClose(CancelShipManageAfterCommit);
                }
                return;
            }

            // Normal path (panel not active)
            CancelShipManageAfterCommit();
        }

        // New: run the cleanup logic *after* a commit has completed.
        public void CancelShipManageAfterCommit()
        {
            if (tempFleetController != null)
            {
                Debug.Log($"CancelShipManageAfterCommit (Fleet): tempFleetController '{tempFleetController.name}' has {tempFleetController.FleetData.ShipsList.Count} ships");

                // Only destroy the fleet if it has NO ships
                if (tempFleetController.FleetData.ShipsList.Count == 0)
                {
                    Debug.Log($"Destroying empty fleet '{tempFleetController.name}'");

                    if (tempFleetController.FogRevealer != null)
                        FleetManager.Instance.RemoveFogWarRevealer(tempFleetController.FogRevealer);
                    tempFleetController.FogRevealer = null;

                    PlayerManager.Instance?.LocalPlayerController?.SubmitDestroyEmptyFleet(tempFleetController);
                    tempFleetController = null;
                }
                else
                {
                    Debug.Log($"Keeping fleet '{tempFleetController.name}' with {tempFleetController.FleetData.ShipsList.Count} ships");

                    // ✅ NEW: Ensure the fleet has proper UI setup before keeping it
                    if (tempFleetController.FleetData.ShipListUIParent == null)
                    {
                        var uiFields = tempFleetController.FleetUIGameObject != null ? tempFleetController.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
                        if (uiFields != null && uiFields.FleetShipContentGO != null)
                        {
                            tempFleetController.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;
                            Debug.Log($"  Set ShipListUIParent for kept fleet '{tempFleetController.name}'");
                        }
                    }

                    // Ships committed via drag-drop are still parented under the deploy UI's shared
                    // BottomSlot (SetUpBottomShipLists never reparents them out again on commit) - move
                    // them into this fleet's own permanent UI container now, or they'll keep sitting in
                    // BottomSlot and show up as "ghost" ships the next time ANY new-fleet deploy session
                    // reuses that same BottomSlot transform.
                    if (tempFleetController.FleetData.ShipListUIParent != null)
                    {
                        foreach (var ship in tempFleetController.FleetData.ShipsList)
                        {
                            if (ship?.ShipListUIGameObject != null)
                                ship.ShipListUIGameObject.transform.SetParent(tempFleetController.FleetData.ShipListUIParent.transform, false);
                        }
                    }

                    // Fleet has ships, so finalize it and keep it
                    tempFleetController = null; // Clear temp reference but don't destroy
                }
            }
            else
            {
                Debug.Log("CancelShipManageAfterCommit (Fleet): No temp fleet to process, proceeding to UI cleanup");
            }

            // ✅ If dragging ships out (e.g. into a brand-new split fleet) left the source fleet
            // with no ships, it must be removed too - otherwise an empty fleet lingers in the
            // galaxy while the ships it used to hold now live entirely in the new fleet.
            var sourceFleet = ShipDeployMenuUIController.Instance != null ? ShipDeployMenuUIController.Instance.TopFleet : null;
            if (sourceFleet != null && sourceFleet.FleetData != null && sourceFleet.FleetData.ShipsList.Count == 0)
            {
                Debug.Log($"CancelShipManageAfterCommit (Fleet): source fleet '{sourceFleet.name}' left with 0 ships, destroying it");
                PlayerManager.Instance?.LocalPlayerController?.SubmitDestroyEmptyFleet(sourceFleet);
            }

            var galaxyUI = GalaxyMenuUIController.Instance;
            MousePointerChanger.Instance.ResetCursor();

            if (ShipDeployMenuUIController.Instance != null)
                ShipDeployMenuUIController.Instance.gameObject.SetActive(false);

            if (galaxyUI != null)
            {
                galaxyUI.ClickCancelShipDeployButton();
                galaxyUI.ResetClickMode();
                galaxyUI.CompleteShipExchange();
            }

            HideA_FleetMenuView();
        }
        public void UpdateFleetWarpUI(FleetController fleetCon, float theirWarp)
        {
            if (fleetCon == null || fleetCon.FleetUIGameObject == null) return;

            Slider slider = fleetCon.FleetUIGameObject.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                slider.onValueChanged.RemoveAllListeners();
                slider.value = theirWarp;
                slider.maxValue = fleetCon.FleetData.MaxWarpFactor;
                slider.onValueChanged.AddListener((value) => fleetCon.SliderOnValueChange(value));
            }

            TextMeshProUGUI[] OneTMP = fleetCon.FleetUIGameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < OneTMP.Length; i++)
            {
                if ("FleetMaxWarpFactor" == OneTMP[i].name)
                {
                    OneTMP[i].text = fleetCon.FleetData.MaxWarpFactor.ToString("0.0");
                }
                else if ("Warp Value Text (TMP)" == OneTMP[i].name)
                {
                    OneTMP[i].text = theirWarp.ToString("0.0");
                }
            }
        }

        public void UpdateFleetMaxWarpUI(FleetController fleetCon, float theirMaxWarp)
        {
            if (fleetCon == null || fleetCon.FleetUIGameObject == null) return;

            Slider slider = fleetCon.FleetUIGameObject.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                slider.onValueChanged.RemoveAllListeners();
                slider.maxValue = theirMaxWarp;
                if (fleetCon.FleetData.CurrentWarpFactor > theirMaxWarp)
                {
                    fleetCon.FleetData.CurrentWarpFactor = theirMaxWarp;
                    slider.value = fleetCon.FleetData.CurrentWarpFactor;
                }
                slider.onValueChanged.AddListener((value) => fleetCon.SliderOnValueChange(value));
            }

            TextMeshProUGUI[] OneTMP = fleetCon.FleetUIGameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < OneTMP.Length; i++)
            {
                if ("FleetMaxWarpFactor" == OneTMP[i].name)
                {
                    OneTMP[i].text = theirMaxWarp.ToString("0.0");
                }
                else if ("Warp Value Text (TMP)" == OneTMP[i].name)
                {
                    OneTMP[i].text = fleetCon.FleetData.CurrentWarpFactor.ToString("0.0");
                }
            }
        }

        private void ClickInterceptButton(FleetController fleetCon)
        {
            if (fleetCon == null) return;

            FleetController.PendingInterceptFleet = fleetCon;
            GalaxyMenuUIController.Instance?.SetClickMode(GalaxyClickMode.SelectForIntercept);
            MousePointerChanger.Instance?.SetDestinationCursor();

            // Swap button visibility while waiting for target pick
            var fields = fleetCon.FleetUIGameObject != null ? fleetCon.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
            if (fields != null)
            {
                fields.InterceptTargetButton?.gameObject.SetActive(false);
                fields.CancelInterceptButton?.gameObject.SetActive(true);
            }

            Debug.Log($"FleetMenuUIController: Intercept mode — waiting for target fleet click (pursuer: {fleetCon.name})");
        }

        private void ClickCancelInterceptButton(FleetController fleetCon, FleetUI_Fields fields)
        {
            if (fleetCon == null) return;

            fleetCon.CancelIntercept();
            FleetController.PendingInterceptFleet = null;
            GalaxyMenuUIController.Instance?.ResetClickMode();
            MousePointerChanger.Instance?.ResetCursor();

            if (fields != null)
            {
                fields.InterceptTargetButton?.gameObject.SetActive(true);
                fields.CancelInterceptButton?.gameObject.SetActive(false);
            }

            Debug.Log($"FleetMenuUIController: Intercept cancelled for {fleetCon.name}");
        }

        public void SelectedDestinationCursor(FleetController fleetConWaitingForDestination)
        {
            if (fleetConWaitingForDestination == null)
            {
                Debug.LogWarning("SelectedDestinationCursor: fleetConWaitingForDestination is NULL - click ignored.");
                return;
            }

            if (fleetConWaitingForDestination.TargetController != null)
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetConWaitingForDestination);

            bool isLocalPlayer = GameController.Instance != null &&
                                  GameController.Instance.AreWeLocalPlayer(fleetConWaitingForDestination.FleetData.CivEnum);
            if (!isLocalPlayer)
            {
                Debug.LogWarning($"SelectedDestinationCursor: fleet '{fleetConWaitingForDestination.name}' (civ={fleetConWaitingForDestination.FleetData?.CivEnum}) is NOT recognized as the local player's own fleet (GameController.Instance={(GameController.Instance != null ? "OK" : "NULL")}) - SetDestination mode NOT armed, click ignored.");
                return;
            }

            Debug.Log($"SelectedDestinationCursor: arming SetDestination mode for fleet '{fleetConWaitingForDestination.name}'.");

            var fields = fleetConWaitingForDestination.FleetUIGameObject != null ? fleetConWaitingForDestination.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
            if (fields != null)
            {
                if (fields.DestinationDragTarget != null)
                    fields.DestinationDragTarget.gameObject.SetActive(false);
                if (fields.CancelDestination != null)
                    fields.CancelDestination.gameObject.SetActive(true);
                if (fields.SelectDestination != null)
                    fields.SelectDestination.gameObject.SetActive(false);
            }

            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                galaxyUI.BeginSetDestination(fleetConWaitingForDestination);
                MousePointerChanger.Instance.SetDestinationCursor();
            }
        }
        public void ClickSelectDestinationButton(FleetController fleetCon)
        {
            if (fleetCon == null)
            {
                Debug.LogWarning("ClickSelectDestinationButton: fleetCon is NULL - click ignored.");
                return;
            }

            if (fleetCon.TargetController != null)
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetCon);

            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                Debug.Log($"ClickSelectDestinationButton: arming SetDestination mode for fleet '{fleetCon.name}'.");
                galaxyUI.BeginSetDestination(fleetCon);
                MousePointerChanger.Instance?.SetDestinationCursor();
            }
            else
            {
                Debug.LogWarning("ClickSelectDestinationButton: GalaxyMenuUIController.Instance is NULL - could not arm SetDestination mode.");
            }
        }
        public void ClickCancelDestinationButton(FleetController fleetCon)
        {
            if (fleetCon == null) return;

            // Destroy any existing player-defined target for this fleet
            if (fleetCon.TargetController != null)
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetCon);
            }

            MousePointerChanger.Instance.ResetCursor();

            // Get buttons from the specific fleet's UI
            var fields = fleetCon.FleetUIGameObject != null ? fleetCon.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
            if (fields != null)
            {
                if (fields.DestinationName != null)
                    fields.DestinationName.text = BOTF3D.Core.Loc.Get("No Destination");
                if (fields.DestinationCoordinates != null)
                    fields.DestinationCoordinates.text = "";
                if (fields.SelectDestination != null)
                    fields.SelectDestination.gameObject.SetActive(true);
                if (fields.DestinationDragTarget != null)
                    fields.DestinationDragTarget.gameObject.SetActive(true);
                if (fields.CancelDestination != null)
                    fields.CancelDestination.gameObject.SetActive(false);
            }

            // Update the UI in the specific fleet list entry if present
            for (int i = 0; i < listOfFleetUiGos.Count; i++)
            {
                if (listOfFleetUiGos[i] == null) continue; // Skip destroyed entries

                if (listOfFleetUiGos[i].GetComponentInChildren<FleetController>() == fleetCon)
                {
                    TextMeshProUGUI[] ourTMPs = listOfFleetUiGos[i].GetComponentsInChildren<TextMeshProUGUI>(true);
                    for (int j = 0; j < ourTMPs.Length; j++)
                    {
                        var name = ourTMPs[j].name;
                        switch (name)
                        {
                            case "Destination FleetName Text":
                                ourTMPs[j].text = BOTF3D.Core.Loc.Get("No Destination");
                                break;
                            case "Destination Coordinates":
                                ourTMPs[j].text = "";
                                break;
                        }
                    }
                    return;
                }
            }
        }

        public void SetAsDestination(string nameDestination, string newCoordinates)
        {
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI?.FleetLookingForDestination == null)
            {
                Debug.LogError("SetAsDestination: FleetLookingForDestination is NULL");
                return;
            }

            var fleetCon = galaxyUI.FleetLookingForDestination;
            var fields = fleetCon.FleetUIGameObject != null ? fleetCon.FleetUIGameObject.GetComponent<FleetUI_Fields>() : null;
            if (fields == null)
            {
                Debug.LogError($"SetAsDestination: FleetUI_Fields not found on '{(fleetCon.FleetUIGameObject != null ? fleetCon.FleetUIGameObject.name : "NULL")}'");
                return;
            }

            if (fields.DestinationName != null)
                fields.DestinationName.text = nameDestination;
            if (fields.DestinationCoordinates != null)
                fields.DestinationCoordinates.text = newCoordinates;
            if (fields.CancelDestination != null)
                fields.CancelDestination.gameObject.SetActive(true);
            if (fields.DestinationDragTarget != null)
                fields.DestinationDragTarget.gameObject.SetActive(false);

            MousePointerChanger.Instance.ResetCursor();
        }

        // Helper: get buttons from the currently active fleet UI
        private FleetUI_Fields GetActiveFleetUIFields()
        {
            if (activeFleetController == null || activeFleetController.FleetUIGameObject == null)
                return null;
            return activeFleetController.FleetUIGameObject.GetComponent<FleetUI_Fields>();
        }

        public void CloseDestinationSelectionCursor()
        {
            MousePointerChanger.Instance.ResetCursor();

            var fields = GetActiveFleetUIFields();
            if (fields != null)
            {
                if (fields.CancelDestination != null)
                    fields.CancelDestination.gameObject.SetActive(false);
                if (fields.DestinationDragTarget != null)
                    fields.DestinationDragTarget.gameObject.SetActive(true);
            }
        }
        public void GetPlayerDefinedTargetDestination(FleetController fleetCon)
        {
            if (fleetCon == null || fleetCon.FleetUIGameObject == null) return;

            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
                galaxyUI.BeginSetDestination(fleetCon); // sets FleetLookingForDestination AND click mode

            var fields = fleetCon.FleetUIGameObject.GetComponent<FleetUI_Fields>();
            if (fields != null)
            {
                if (fields.DestinationDragTarget != null)
                    fields.DestinationDragTarget.gameObject.SetActive(false);
                if (fields.CancelDestination != null)
                    fields.CancelDestination.gameObject.SetActive(true);
                if (fields.SelectDestination != null)
                    fields.SelectDestination.gameObject.SetActive(true);
            }

            MousePointerChanger.Instance.SetDestinationCursor();
        }
        private void OnEnable()
        {
            // Refresh every local fleet's Colonize/Terraform/Claim buttons whenever any system's
            // habitability flips (e.g. a terraform timer completes) - see the contactedSystem logic
            // in SetupFleetUIElements, which needs this to pick up a system going habitable while a
            // fleet is still sitting in contact with it, without requiring a fresh OnTriggerEnter.
            GameEvents.OnSystemHabitabilityChanged += HandleSystemHabitabilityChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnSystemHabitabilityChanged -= HandleSystemHabitabilityChanged;

            // When the UI menu closes (e.g., switching menus or hiding canvas)
            CleanupDestroyedOrInactiveUIs();
        }

        private void HandleSystemHabitabilityChanged(string systemName, bool isHabitable)
        {
            if (isHabitable)
                SetupFleetUIData();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("FleetMenuUIController: Instance cleared");
            }
        }

        public void CleanupDestroyedOrInactiveUIs()
        {
            // Remove any destroyed or inactive GameObjects from the list
            listOfFleetUiGos.RemoveAll(go => go == null || !go.activeInHierarchy);
            Debug.Log("DiplomacyMenuUIController: Cleaned up destroyed or inactive diplomacy UIs.");
        }
        public void ClearAllFleetUIs()
        {
            foreach (var go in listOfFleetUiGos)
            {
                if (go != null)
                    Destroy(go);
            }
            listOfFleetUiGos.Clear();
            Debug.Log("Cleared all diplomacy UI GameObjects.");
        }

        internal void ClickCancelShipManagerButton(FleetController fleetCon)
        {
            if (fleetCon.TargetController != null)
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetCon);
            }
            MousePointerChanger.Instance.ResetCursor();
            selectShipManagerCursorButtonGO?.SetActive(true);
            dragDestinationTargetButtonGO.SetActive(false);
            cancelDestinationButtonGO?.SetActive(true);
        }

        private void MoveShipView(List<ShipController> upperShipsToMove, List<ShipController> lowerShipsToMove)
        {
            // drag and drop, Can we do this in MovingShipsView class?
        }

        public void ShowFleetMenuView()
        {
            Debug.Log("=== ShowFleetMenuView: Starting ===");

            if (FleetMenuView == null || FleetListContainer == null)
            {
                FindFleetUIContainers();
            }

            if (FleetMenuView == null)
            {
                Debug.LogError("ShowFleetMenuView: FleetMenuView is NULL!");
                return;
            }

            // ✅ Move all local player's fleet UIs to the scrollable FleetListContainer
            if (FleetManager.Instance != null)
            {
                foreach (var fleetCon in FleetManager.Instance.FleetControllerList)
                {
                    if (fleetCon == null || fleetCon.FleetUIGameObject == null) continue;

                    // Only show local player's fleets
                    if (!GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum))
                        continue;

                    // ✅ Move to scrollable list container and activate
                    fleetCon.FleetUIGameObject.transform.SetParent(FleetListContainer.transform, false);
                    fleetCon.FleetUIGameObject.SetActive(true);
                }
            }

            FleetMenuView.SetActive(true);
            Debug.Log("  FleetMenuView activated with scrollable list");

            SetupFleetUIData(); // Wire buttons, update data

            Debug.Log("=== ShowFleetMenuView: Complete ===");
        }

        public void HideFleetMenuView()
        {
            if (FleetMenuView == null) return;

            // ✅ Move all fleet UIs back to home storage
            MoveBackAnyaFleetUIGO();

            FleetMenuView.SetActive(false);
            Debug.Log("FleetMenuView hidden, UIs moved back to storage");
        }

        public void ShowA_FleetMenuView()
        {
            if (AFleetMenuView != null)
            {
                AFleetMenuView.SetActive(true);
                Debug.Log("AFleetMenuView shown");
            }
        }

        public void HideA_FleetMenuView()
        {
            if (AFleetMenuView != null)
            {
                AFleetMenuView.SetActive(false);
                Debug.Log("AFleetMenuView hidden");
            }
        }

        // AFleetMenuView defaults to sitting just under the ribbon (anchoredPosition.y == -70,
        // top-left anchored/pivoted). The Habitable/Terraformable contact popups live on their
        // own Canvas above this one, so they can't push this panel via layout groups. Instead,
        // measure the popup's actual rendered bottom edge at runtime - static offsets kept being
        // wrong because the status text (e.g. "Uninhabited - Requires Terraforming") can wrap to
        // a second line, changing the popup's real height beyond what its RectTransform shows.
        private const float AFleetMenuViewDefaultY = -70f;

        public void SetPopupClearance(RectTransform popupContentRoot)
        {
            if (AFleetMenuView == null || popupContentRoot == null) return;

            var rect = AFleetMenuView.GetComponent<RectTransform>();
            var parentRect = rect != null ? rect.parent as RectTransform : null;
            if (rect == null || parentRect == null) return;

            Canvas.ForceUpdateCanvases();
            Bounds popupBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parentRect, popupContentRoot);

            const float margin = 20f;
            float desiredTopEdge = popupBounds.min.y - margin;
            float pushedY = desiredTopEdge - parentRect.rect.yMax;
            // Never push the panel higher than its default - only ever downward, and only as far as needed.
            float newY = Mathf.Min(AFleetMenuViewDefaultY, pushedY);

            var pos = rect.anchoredPosition;
            pos.y = newY;
            rect.anchoredPosition = pos;
        }

        public void ResetPopupClearance()
        {
            if (AFleetMenuView == null) return;
            var rect = AFleetMenuView.GetComponent<RectTransform>();
            if (rect == null) return;

            var pos = rect.anchoredPosition;
            pos.y = AFleetMenuViewDefaultY;
            rect.anchoredPosition = pos;
        }

        // Toggles the ship list between:
        //   Collapsed — one visible row (Mask clips the rest), ▼ button
        //   Expanded  — ShipScrollView grows to show all rows,  ▲ button
        // Grid is always FixedColumnCount=8 — only height and Mask change.
        private void ToggleShipListExpansion(FleetController fleetCon, FleetUI_Fields uiFields)
        {
            if (uiFields.ShipScrollView == null || uiFields.FleetShipContentGO == null) return;

            var grid   = uiFields.FleetShipContentGO.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            var fitter = uiFields.FleetShipContentGO.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            var svRect = uiFields.ShipScrollView.GetComponent<RectTransform>();
            var mask   = uiFields.ShipScrollView.GetComponentInChildren<UnityEngine.UI.Mask>();

            bool isCollapsed = mask == null || mask.enabled;

            if (isCollapsed)
            {
                // ── Expand ────────────────────────────────────────────────────
                // Calculate rows needed and target height
                int shipCount = fleetCon?.FleetData?.ShipsList?.Count ?? 0;
                int cols      = grid != null ? grid.constraintCount : 8;
                int rows      = Mathf.Max(1, Mathf.CeilToInt(shipCount / (float)cols));
                float rowH    = grid != null ? grid.cellSize.y + grid.spacing.y : 29f;
                float neededH = rows * rowH + 8f;
                float targetH = Mathf.Min(neededH, Screen.height * 0.85f);

                if (svRect != null)
                    svRect.sizeDelta = new Vector2(svRect.sizeDelta.x, targetH);

                // ContentSizeFitter drives height once mask is off
                if (fitter != null)
                    fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

                if (mask != null) mask.enabled = false;

                SetExpandButtonLabel(uiFields, true);
            }
            else
            {
                // ── Collapse ──────────────────────────────────────────────────
                if (fitter != null)
                    fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;

                if (svRect != null)
                    svRect.sizeDelta = new Vector2(svRect.sizeDelta.x, uiFields.CollapsedShipViewHeight);

                if (mask != null) mask.enabled = true;

                SetExpandButtonLabel(uiFields, false);
            }

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
                uiFields.FleetShipContentGO.GetComponent<RectTransform>());
        }

        private static void SetExpandButtonLabel(FleetUI_Fields uiFields, bool expanded)
        {
            if (uiFields.ExpandShipsButton == null) return;
            var txt = uiFields.ExpandShipsButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = expanded ? "▲" : "▼";
        }
    }
}
