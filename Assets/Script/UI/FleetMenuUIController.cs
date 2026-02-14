// Ignore Spelling: Anya

using BOTF3D.Core;
using BOTF3D.GamePlay;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    public class FleetMenuUIController : MonoBehaviour
    {
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

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
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
                if (fleetCon == null) continue;

                if (!listOfFleetUiGos.Contains(fleetCon.FleetUIGameObject) &&
                    GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum))
                {
                    // wire up individual fleet UI
                    SetupFleetUIElements(fleetCon, fleetCon.FleetUIGameObject);
                    listOfFleetUiGos.Add(fleetCon.FleetUIGameObject);
                    fleetCon.FleetUIGameObject.SetActive(true);
                    if (fleetCon.FleetUIGameObject.GetComponent<FleetAndSystemChildController>().OriginalParentTransform == null)
                        fleetCon.FleetUIGameObject.GetComponent<FleetAndSystemChildController>().OriginalParentTransform = FleetListContainer.transform;
                    fleetCon.FleetUIGameObject.transform.SetParent(FleetListContainer.transform, false);
                }
            }
        }
        public void SetActiveSetParentUIGO(FleetController theFleetCon)
        {
            if (GameController.Instance.AreWeLocalPlayer(theFleetCon.FleetData.CivEnum))
            {
                SetupFleetUIData();
                theFleetCon.FleetUIGameObject.SetActive(true);
                theFleetCon.FleetUIGameObject.transform.SetParent(AFleetMenuView.transform, false);
                activeFleetController = theFleetCon;
            }
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
            if (AFleetMenuView == null)
            {
                Debug.LogWarning("FleetMenuUIController.MoveBackAnyaFleetUIGO: AFleetMenuView is null, skipping");
                return;
            }

            AFleetMenuView.SetActive(true);

            // ✅ Move back BOTH fleet UIs AND star system UIs that might be in this container
            for (int i = AFleetMenuView.transform.childCount - 1; i >= 0; i--)
            {
                var child = AFleetMenuView.transform.GetChild(i);
                if (child == null) continue;

                var childController = child.GetComponent<FleetAndSystemChildController>();
                if (childController != null && childController.OriginalParentTransform != null)
                {
                    child.SetParent(childController.OriginalParentTransform, false);
                    Debug.Log($"FleetMenuUIController: Moved '{child.name}' back to '{childController.OriginalParentTransform.name}'");
                }
            }

            // Hide AFleetMenuView after moving children back
            AFleetMenuView.SetActive(false);
            activeFleetController = null;

            Debug.Log("FleetMenuUIController: Moved all UIs back and closed AFleetMenuView");
        }
        public void SetupFleetUIElements(FleetController fleetCon, GameObject newFleetUIGO)
        {
            if (fleetCon == null || newFleetUIGO == null) return;

            // CRITICAL: Ensure FleetListContainer exists
            if (FleetListContainer == null)
            {
                FindFleetUIContainers();

                if (FleetListContainer == null)
                {
                    Debug.LogError($"FleetMenuUIController.SetupFleetUIElements: FleetListContainer is NULL! Cannot setup fleet UI for {fleetCon.name}");
                    return;
                }
            }

            if (!listOfFleetUiGos.Contains(fleetCon.FleetUIGameObject))
            {
                newFleetUIGO.SetActive(true);
                fleetCon.FleetUIGameObject.transform.SetParent(FleetListContainer.transform, false);
                listOfFleetUiGos.Add(fleetCon.FleetUIGameObject);

                var fleetAndStarSys = fleetCon.FleetUIGameObject.GetComponent<FleetAndSystemChildController>();
                if (fleetAndStarSys != null)
                {
                    if (fleetAndStarSys.OriginalParentTransform == null)
                    {
                        fleetAndStarSys.OriginalParentTransform = FleetListContainer.transform;
                    }
                }

                FleetUI_Fields uiFields = newFleetUIGO.GetComponent<FleetUI_Fields>();
                fleetCon.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;

                Debug.Log($"SetupFleetUIElements: Set ShipListUIParent for fleet '{fleetCon.name}' to {(uiFields.FleetShipContentGO != null ? "SET" : "NULL")}");

                float x = fleetCon.FleetData.Position.x * 0.12f;
                float z = fleetCon.FleetData.Position.z * 0.12f;
                RectTransform dot = uiFields.MinimapRedDot.GetComponent<RectTransform>();
                dot.anchoredPosition = new Vector2(x, z);

                // Button bindings
                uiFields.DestinationDragTarget.gameObject.SetActive(true);
                uiFields.DestinationDragTarget.onClick.RemoveAllListeners();
                uiFields.DestinationDragTarget.onClick.AddListener(() => fleetCon.GetPlayerDefinedTargetDestination(fleetCon));
                dragDestinationTargetButtonGO = uiFields.DestinationDragTarget.gameObject;
                uiFields.CancelDestination.gameObject.SetActive(false);
                uiFields.CancelDestination.onClick.RemoveAllListeners();
                uiFields.CancelDestination.onClick.AddListener(() => fleetCon.ClickCancelDestinationButton());
                cancelDestinationButtonGO = uiFields.CancelDestination.gameObject;
                uiFields.SelectDestination.gameObject.SetActive(true);
                uiFields.SelectDestination.onClick.RemoveAllListeners();
                uiFields.SelectDestination.onClick.AddListener(() => SelectedDestinationCursor(fleetCon));
                selectDestinationCursorButtonGO = uiFields.SelectDestination.gameObject;
                uiFields.WarpUp.gameObject.SetActive(true);
                uiFields.WarpUp.onClick.RemoveAllListeners();
                uiFields.WarpUp.onClick.AddListener(() => fleetCon.FleetOnWarpUpClick(fleetCon));
                warpButtonUpGO = uiFields.WarpUp.gameObject;
                uiFields.WarpDown.gameObject.SetActive(true);
                uiFields.WarpDown.onClick.RemoveAllListeners();
                uiFields.WarpDown.onClick.AddListener(() => fleetCon.FleetOnWarpDownClick(fleetCon));
                warpButtonDownGO = uiFields.WarpDown.gameObject;
                //uiFields.CancelShipManagerButton.gameObject.SetActive(true);
                //uiFields.CancelShipManagerButton.onClick.RemoveAllListeners();
                //uiFields.CancelShipManagerButton.onClick.AddListener(() => fleetCon.ClickCancelShipManageButton());
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
                // Text bindings
                uiFields.FleetNameText.text = fleetCon.FleetData.Name;
                uiFields.DestinationName.gameObject.SetActive(true);
                destinationName = uiFields.DestinationName;
                uiFields.DestinationName.text = "";
                uiFields.DestinationCoordinates.gameObject.SetActive(true);
                destinationCoordinates = uiFields.DestinationCoordinates;
                uiFields.DestinationCoordinates.text = "";
                uiFields.WarpValueText.text = fleetCon.FleetData.CurrentWarpFactor.ToString("0.0");
                // Slider wiring
                uiFields.WarpSlider.onValueChanged.RemoveAllListeners();
                uiFields.WarpSlider.value = fleetCon.FleetData.CurrentWarpFactor;
                uiFields.WarpSlider.maxValue = fleetCon.FleetData.MaxWarpFactor;
                uiFields.WarpSlider.onValueChanged.AddListener((value) => fleetCon.SliderOnValueChange(value));
            }
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
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(currentFleetCon);
            }
            if (currentFleetCon.FleetData.ShipsList.Count < 2) return;

            MousePointerChanger.Instance.ResetCursor();
            var fleetManager = FleetManager.Instance;
            FleetSO fleetSO = fleetManager.GetFleetSO_byInt((int)currentFleetCon.FleetData.CivEnum);
            var position = currentFleetCon.FleetData.GetPosition();

            CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum);
            FleetData fleetData = new FleetData(fleetSO);
            fleetData.CurrentWarpFactor = 0f;
            fleetData.CivLongName = thisCivData.CivLongName;
            fleetData.CivShortName = thisCivData.CivShortName;
            fleetData.CivEnum = thisCivData.CivEnum;
            fleetData.PlayerId = thisCivData.PlayerId;
            //Not fleetData.FleetInt, wait to get fleet num from instantiate fleet in = fleetManager.GetNewFleetInt(thisCivData.CivEnum);
            //Same goes for fleetData.Name = $"{thisCivData.CivLongName} Fleet {fleetData.FleetInt}";
            fleetData.Insignia = thisCivData.InsigniaSprite;
            fleetData.ShipsList = new List<ShipController>();
            //Not fleetData.Position, wait for it = position;

            var galaxyMenuUICon = GalaxyMenuUIController.Instance;

            // TopFleet (source) for deploy UI
            ShipDeployMenuUIController.Instance.TopFleet = currentFleetCon;

            // Create an empty star system placeholder used by InstantiateFleet
            var emptyStarSysCon = StarSysManager.Instance.InstantiateEmptyStarSysController();

            // Create the new fleet (split off) in FleetManager
            var newFleet = fleetManager.InstantiateFleet(currentFleetCon, emptyStarSysCon, fleetData, position, true);

            tempFleetController = newFleet;

            // CRITICAL: Ensure the new fleet has its ShipListUIParent set up
            if (newFleet.FleetData.ShipListUIParent == null)
            {
                Debug.LogWarning($"New fleet '{newFleet?.name}' has no ShipListUIParent! Creating temporary container.");
                // This should ideally be set up in InstantiateFleet or ShowShipDeployForFleetNewFleet
            }

            Debug.Log($"ClickNewFleetButton: New fleet '{newFleet?.name}' created with ShipListUIParent={(newFleet.FleetData?.ShipListUIParent != null ? "SET" : "NULL")}");

            // Use the central GalaxyMenuUIController method so it performs full UI life-cycle and parents correctly.
            Debug.Log($"ClickNewFleetButton: requesting ShipDeploy UI for new fleet '{newFleet?.name}' (from {currentFleetCon?.name})");
            galaxyMenuUICon.ShowShipDeployForFleetNewFleet(currentFleetCon, newFleet);

            Destroy(emptyStarSysCon.gameObject);
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

            if (sd != null && sd.ShipDeployPanel != null && sd.ShipDeployPanel.activeInHierarchy)
            {
                if (isMergeMode)
                {
                    // Use merge commit for merge operations
                    sd.CommitMergeAndClose(CancelShipManageAfterCommit);
                }
                else
                {
                    // Use proper commit flow for deploy/new fleet
                    sd.CommitShipDeployForNewFleetAndClose(CancelShipManageAfterCommit);
                }
                return;
            }

            // Normal path (panel not active)
            CancelShipManageAfterCommit();
        }

        // New: run the cleanup logic *after* a commit has completed.
        public void CancelShipManageAfterCommit()
        {
            if (tempFleetController == null) return;

            Debug.Log($"CancelShipManageAfterCommit (Fleet): tempFleetController '{tempFleetController.name}' has {tempFleetController.FleetData.ShipsList.Count} ships");

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

            var galaxyUI = GalaxyMenuUIController.Instance;
            MousePointerChanger.Instance.ResetCursor();
            //if (cancelFleetUIButtonGO != null)
            //    GalaxyMenuUIController.Instance.CloseButtonPressed();
            //cancelFleetUIButtonGO.SetActive(false);
            if (ShipDeployMenuUIController.Instance != null)
                ShipDeployMenuUIController.Instance.gameObject.SetActive(false);
            if (galaxyUI != null)
            {
                galaxyUI.ClickCancelShipDeployButton();
                galaxyUI.ResetClickMode();
                galaxyUI.CompleteShipExchange();
            }
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

        public void SelectedDestinationCursor(FleetController fleetConWaitingForDestination)
        {
            if (fleetConWaitingForDestination == null) return;

            if (fleetConWaitingForDestination.TargetController != null)
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetConWaitingForDestination);
            }

            if (GameController.Instance.AreWeLocalPlayer(fleetConWaitingForDestination.FleetData.CivEnum))
            {
                // Get buttons from the active fleet UI instead of using stale references
                var fields = fleetConWaitingForDestination.FleetUIGameObject?.GetComponent<FleetUI_Fields>();
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
                    galaxyUI.SetClickMode(GalaxyClickMode.SetDestination);
                    galaxyUI.FleetLookingForDestination = fleetConWaitingForDestination;
                }
                MousePointerChanger.Instance.SetDestinationCursor();
            }
        }
        public void ClickSelectDestinationButton(FleetController fleetCon)
        {
            if (fleetCon == null) return;

            // Destroy any existing player-defined target for this fleet
            if (fleetCon.TargetController != null)
            {
                PlayerDefinedTargetManager.Instance?.DestroyPlayerTarget(fleetCon);
            }

            // Change to destination selection mode
            GalaxyMenuUIController.Instance?.SetClickMode(GalaxyClickMode.SetDestination);
            GalaxyMenuUIController.Instance.FleetLookingForDestination = fleetCon;

            // Update cursor
            MousePointerChanger.Instance?.SetDestinationCursor();

            Debug.Log($"FleetMenuUIController: Select Destination mode for fleet '{fleetCon.name}'");
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
            var fields = fleetCon.FleetUIGameObject?.GetComponent<FleetUI_Fields>();
            if (fields != null)
            {
                if (fields.DestinationName != null)
                    fields.DestinationName.text = "No Destination";
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
                            case "Destination Name Text":
                                ourTMPs[j].text = "No Destination";
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
            // Get text fields from the active fleet UI instead of cached references
            var fields = GetActiveFleetUIFields();
            if (fields != null)
            {
                if (fields.DestinationName != null)
                    fields.DestinationName.text = nameDestination;
                if (fields.DestinationCoordinates != null)
                    fields.DestinationCoordinates.text = newCoordinates;
                if (fields.CancelDestination != null)
                    fields.CancelDestination.gameObject.SetActive(true);
                if (fields.DestinationDragTarget != null)
                    fields.DestinationDragTarget.gameObject.SetActive(false);
            }
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

            // Get buttons from the specific fleet's UI
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

            GalaxyMenuUIController.Instance.CurrentClickMode = GalaxyClickMode.SetDestination;
            MousePointerChanger.Instance.SetDestinationCursor();
        }
        private void OnDisable()
        {
            // When the UI menu closes (e.g., switching menus or hiding canvas)
            CleanupDestroyedOrInactiveUIs();
        }

        private void OnDestroy()
        {
            // When this controller is destroyed (e.g., scene unload)
            ClearAllFleetUIs();
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

            if (FleetManager.Instance != null)
            {
                var fleets = FleetManager.Instance.FleetControllerList;
                Debug.Log($"  FleetManager has {fleets?.Count ?? 0} total fleets");

                if (fleets != null && fleets.Count > 0)
                {
                    int localPlayerFleets = 0;
                    foreach (var fleet in fleets)
                    {
                        if (fleet != null && GameController.Instance.AreWeLocalPlayer(fleet.FleetData.CivEnum))
                        {
                            localPlayerFleets++;
                        }
                    }

                    Debug.Log($"  Local player owns {localPlayerFleets} fleets");
                }
            }
            else
            {
                Debug.LogError("  FleetManager.Instance is NULL!");
            }

            // Show the menu
            if (FleetMenuView != null)
            {
                FleetMenuView.SetActive(true);
                Debug.Log("  FleetMenuView activated");

                // CRITICAL: Populate the fleet list
                SetupFleetUIData();
            }
            else
            {
                Debug.LogError("  FleetMenuView is NULL!");
            }

            Debug.Log("=== ShowFleetMenuView: Complete ===");
        }

        public void HideFleetMenuView()
        {
            if (FleetMenuView != null)
            {
                FleetMenuView.SetActive(false);
                Debug.Log("FleetMenuView hidden");
            }
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
    }
}
