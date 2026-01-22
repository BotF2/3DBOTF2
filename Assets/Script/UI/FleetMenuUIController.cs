// Ignore Spelling: Anya

using Assets.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// The UI controller owns hierarchy and presentation.
/// </summary>

public class FleetMenuUIController : MonoBehaviour
{
    public static FleetMenuUIController Instance;

    [Header("Buttons (assign in Inspector)")]
    public Button saveCloseShipDeployButton;
    [Header("References (assign in Inspector)")]
    public GameObject FleetMenuView;
    public GameObject AFleetMenuView;
    public GameObject FleetListContainer;
    [Header("Private UI Elements")]
    [SerializeField] private GameObject shipDelployPanel;
    [SerializeField] private GameObject aFleetShipContainer;
    [SerializeField] private TMP_Text fleetName;
    //private GameObject fleetShipListContainer;
    [SerializeField] private TextMeshProUGUI destinationName;
    [SerializeField] private TextMeshProUGUI destinationCoordinates;
    [SerializeField] private GameObject selectDestinationCursorButtonGO;
    [SerializeField] private GameObject cancelDestinationButtonGO;
    [SerializeField] private GameObject dragDestinationTargetButtonGO;
    [SerializeField] private GameObject selectShipManagerCursorButtonGO;
    [SerializeField] private GameObject cancelShipManagerButtonGO;
    [SerializeField] private GameObject warpButtonUpGO;
    [SerializeField] private GameObject warpButtonDownGO;
    [SerializeField] private GameObject mergeFleetButtonGO;
    [SerializeField] private GameObject closeFleetUIButtonGO;

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
        for (int i = 0; i < FleetManager.Instance.FleetControllerList.Count; i++)
        {
            var fleetCon = FleetManager.Instance.FleetControllerList[i];
            if (fleetCon != null && fleetCon.FleetUIGameObject != null)
            {
                var child = fleetCon.FleetUIGameObject;
                var childController = child.GetComponent<FleetAndSystemChildController>();
                if (childController != null && childController.OriginalParentTransform == null)
                {
                    // Prefer the current hierarchy parent first
                    if (child.transform.parent != null)
                    {
                        childController.OriginalParentTransform = child.transform.parent;
                    }
                    // Next prefer the SysListContainer if available
                    else if (FleetListContainer != null)
                    {
                        childController.OriginalParentTransform = FleetListContainer.transform;
                    }
                    // Last resort: ASystemMenuView (preserve existing behavior if nothing else)
                    else if (AFleetMenuView != null)
                    {
                        childController.OriginalParentTransform = AFleetMenuView.transform;
                    }
                }
            }
        }
        // Initially hide fleet menu views
        if (FleetMenuView != null)
            FleetMenuView.SetActive(false);
        if (AFleetMenuView != null)
            AFleetMenuView.SetActive(false);
        //galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
        //parentCanvas.worldCamera = galaxyEventCamera;
    }
    public void ShowFleetMenuView()
    {
        FleetMenuView.SetActive(true);
    }
    public void ShowA_FleetMenuView()
    {
        AFleetMenuView.SetActive(true);
    }
    public void HideFleetMenuView()
    {
        FleetMenuView.SetActive(false);
    }
    public void HideA_FleetMenuView()
    {

        AFleetMenuView.SetActive(false);
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
        theFleetCon.FleetUIGameObject.SetActive(true);
        theFleetCon.FleetUIGameObject.transform.SetParent(AFleetMenuView.transform, false);
        activeFleetController = theFleetCon;
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
        AFleetMenuView.SetActive(true);
        for (int i = 0; i < AFleetMenuView.transform.childCount; i++)
        {
            var child = AFleetMenuView.transform.GetChild(i)?.gameObject;
            if (child != null)
            {
                if (child.gameObject.GetComponent<FleetAndSystemChildController>() != null)
                {
                    Transform originalParent = child.gameObject.GetComponent<FleetAndSystemChildController>().OriginalParentTransform;
                    child.transform.SetParent(originalParent, false);
                }
            }
        }
        activeFleetController = null;
    }
    public void SetupFleetUIElements(FleetController fleetCon, GameObject newFleetUIGO)
    {
        if (fleetCon == null || newFleetUIGO == null) return;
        if (!listOfFleetUiGos.Contains(fleetCon.FleetUIGameObject) && GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum))
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
            float x = fleetCon.FleetData.Position.x * 0.12f; // 0.12f is our cosmologic constant, fudge factor to mini map
            float y = 0f;
            float z = fleetCon.FleetData.Position.z * 0.12f;
            uiFields.RedDot.Translate(new Vector3(x, z, y), Space.Self); // flip z and y from main galaxy map to UI mini map
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
            uiFields.WarpUp.onClick.AddListener(() => fleetCon.FleetOnWarpDownClick(fleetCon));
            warpButtonDownGO = uiFields.WarpDown.gameObject;
            saveCloseShipDeployButton.gameObject.SetActive(true);
            saveCloseShipDeployButton.onClick.RemoveAllListeners();
            saveCloseShipDeployButton.onClick.AddListener(() => fleetCon.saveCloseShipDelplyButton(fleetCon));
            uiFields.NewFleetButton.gameObject.SetActive(true);
            uiFields.NewFleetButton.onClick.RemoveAllListeners();
            uiFields.NewFleetButton.onClick.AddListener(() => ClickNewFleetButton(fleetCon));
            uiFields.MergeFleetsButton.gameObject.SetActive(true);
            uiFields.MergeFleetsButton.onClick.RemoveAllListeners();
            uiFields.MergeFleetsButton.onClick.AddListener(() => ClickMergeFleetButton());
            mergeFleetButtonGO = uiFields.MergeFleetsButton.gameObject;
            uiFields.ShipDeployButton.gameObject.SetActive(true);
            uiFields.ShipDeployButton.onClick.RemoveAllListeners();
            uiFields.ShipDeployButton.onClick.AddListener(() => FleetClickedShipDeployCursor(fleetCon));
            selectShipManagerCursorButtonGO = uiFields.ShipDeployButton.gameObject;
            uiFields.CancelShipManagerButton.gameObject.SetActive(false);
            uiFields.CancelShipManagerButton.onClick.RemoveAllListeners();
            uiFields.CancelShipManagerButton.onClick.AddListener(() => ClickCancelShipManageButton());
            cancelDestinationButtonGO = uiFields.CancelShipManagerButton.gameObject;
            // Text bindings
            uiFields.FleetNameText.text = fleetCon.FleetData.Name;
            destinationName = uiFields.DestinationName;
            uiFields.DestinationName.text = "No Destination";
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

    private void ClickMergeFleetButton()
    {
        // Merge fleet code here;
    }


    private void ClickNewFleetButton(FleetController oldFleetCon)
    {
        if (oldFleetCon.FleetData.ShipsList.Count < 2) return;
        MousePointerChanger.Instance.ResetCursor();
        var fleetManager = FleetManager.Instance;
        FleetSO fleetSO = fleetManager.GetFleetSO_byInt((int)oldFleetCon.FleetData.CivEnum);
        var position = oldFleetCon.FleetData.GetPosition();

        CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum); // new CivData();
        FleetData fleetData = new FleetData(fleetSO);
        fleetData.CurrentWarpFactor = 0f;
        fleetData.CivLongName = thisCivData.CivLongName; //.CivLongName;
        fleetData.CivShortName = thisCivData.CivShortName;
        var galaxyMenuUICon = GalaxyMenuUIController.Instance;
        galaxyMenuUICon.ResetClickMode();
        galaxyMenuUICon.FleetLookingForShipDeploy = oldFleetCon;
        galaxyMenuUICon.StarSystSelectedForShipDeploy = null;
        galaxyMenuUICon.FleetSelectedForShipDeploy = null;
        galaxyMenuUICon.StarSystLookingForShipDeploy = null;
        ShipDeployMenuUIController.Instance.TopFleet = oldFleetCon;
        //var emptyStarSysCon = StarSysManager.Instance.InstantiateEmptyStarSysController();
        var newFleet = fleetManager.InstantiateFleet(oldFleetCon, null, fleetData, position, true);
        newFleet.FleetData.ShipsList.Clear();
        tempFleetController = newFleet;
        ShipDeployMenuUIController.Instance.SetUpTopShipLists(oldFleetCon.FleetData.ShipsList);
        ShipDeployMenuUIController.Instance.SetUpBottomShipLists(newFleet);
        //ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();
        //galaxyMenuUICon.ShowShipDeployForFleetNewFleet(oldFleetCon, newFleet);
    }
    private void FleetClickedShipDeployCursor(FleetController fleetCon)
    {
        var galaxyUI = GalaxyMenuUIController.Instance;
        if (galaxyUI != null)
        {
            galaxyUI.WhatFleetIsLookingForShipDeploy(fleetCon);
            galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipExchange);
            MousePointerChanger.Instance.SetShipExchangeCursor();
            ShipDeployMenuUIController.Instance.TopFleet = fleetCon;
        }
    }
    public void ClickCancelShipManageButton()
    {
        if (tempFleetController == null) return;
        if (tempFleetController.FleetData.ShipsList.Count == 0)
        {
            if (FleetManager.Instance.TempFogRevealerFleet != null)
                FleetManager.Instance.RemoveFogWarRevealer(FleetManager.Instance.TempFogRevealerFleet);
            FleetManager.Instance.TempFogRevealerFleet = null;

            FleetManager.Instance.DestroyFleetController(tempFleetController);
            tempFleetController = null;
        }
        var galaxyUI = GalaxyMenuUIController.Instance;
        MousePointerChanger.Instance.ResetCursor();
        if (cancelShipManagerButtonGO != null)
            cancelShipManagerButtonGO.SetActive(false);
        ShipDeployMenuUIController.Instance.gameObject.SetActive(false);
        galaxyUI.ClickCancelShipDeployButton();
        galaxyUI.ResetClickMode();
        galaxyUI.CompleteShipExchange();
    }
    public void UpdateFleetWarpUI(FleetController fleetCon, float theirWarp)
    {
        if (fleetCon != null ? fleetCon.FleetUIGameObject : null == null) return;


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
        if (fleetCon != null || fleetCon.FleetUIGameObject == null) return;

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
        if (GameController.Instance.AreWeLocalPlayer(fleetConWaitingForDestination.FleetData.CivEnum))
        {
            dragDestinationTargetButtonGO?.SetActive(false);
            if (cancelDestinationButtonGO != null)
                cancelDestinationButtonGO.SetActive(true);
            selectDestinationCursorButtonGO?.SetActive(false);
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.BeginSetDestination(fleetConWaitingForDestination);
            galaxyUI.SetClickMode(GalaxyClickMode.SetDestination);
            galaxyUI.FleetLookingForDestination = fleetConWaitingForDestination;
            MousePointerChanger.Instance.SetDestinationCursor();
        }
    }

    public void ClickCancelDestinationButton(FleetController fleetCon)
    {
        MousePointerChanger.Instance.ResetCursor();
        destinationName.text = "No Destination";
        destinationCoordinates.text = "";
        selectDestinationCursorButtonGO?.SetActive(true);
        dragDestinationTargetButtonGO?.SetActive(true);
        cancelDestinationButtonGO?.SetActive(false);

        // Update the UI in the specific fleet list entry if present
        for (int i = 0; i < listOfFleetUiGos.Count; i++)
        {
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
        if (destinationName != null) destinationName.text = nameDestination;
        if (destinationCoordinates != null) destinationCoordinates.text = newCoordinates;
        if (cancelDestinationButtonGO != null)
            cancelDestinationButtonGO.SetActive(true);
        if (dragDestinationTargetButtonGO != null)
            dragDestinationTargetButtonGO.SetActive(false);
        MousePointerChanger.Instance.ResetCursor();
    }

    public void CloseDestinationSelectionCursor()
    {
        MousePointerChanger.Instance.ResetCursor();
    }
    public void GetPlayerDefinedTargetDestination(FleetController fleetCon)
    {
        dragDestinationTargetButtonGO.SetActive(false); // to see cancel destination button
        cancelDestinationButtonGO.SetActive(true);
        selectDestinationCursorButtonGO.SetActive(true);
        GalaxyMenuUIController.Instance.CurrentClickMode = GalaxyClickMode.SetDestination;
        MousePointerChanger.Instance.SetDestinationCursor();//ChangeToGalaxyMapCursorForLocalPlayer(fleetCon);
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
        MousePointerChanger.Instance.ResetCursor();
        selectShipManagerCursorButtonGO?.SetActive(true);
    }

    private void MoveShipView(List<ShipController> upperShipsToMove, List<ShipController> lowerShipsToMove)
    {
        // drag and drop, Can we do this in MovingShipsView class?
    }
}
