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
    //private Camera galaxyEventCamera;
    //[SerializeField]
    //private Canvas parentCanvas;
    [Header("References (assign in Inspector)")]
    public GameObject FleetMenuView;
    public GameObject AFleetMenuView;
    public GameObject FleetListContainer;
    [Header("Private UI Elements")]
    [SerializeField] private GameObject aFleetShipContainer;
    [SerializeField] private TMP_Text fleetName;
    private GameObject fleetShipListContainer;
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
    public FleetController ActiveFleetController;
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
        ActiveFleetController = theFleetCon;
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
        ActiveFleetController = null;
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

            RectTransform[] rectTransforms = newFleetUIGO.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                if (rectTransforms[i].name == "RedDot")
                {
                    float x = fleetCon.FleetData.Position.x * 0.12f; // 0.12f is our cosmologic constant, fudge factor to mini map
                    float y = 0f;
                    float z = fleetCon.FleetData.Position.z * 0.12f;
                    rectTransforms[i].Translate(new Vector3(x, z, y), Space.Self); // flip z and y from main galaxy map to UI mini map
                }

                var name = rectTransforms[i].name;
                switch (name)
                {
                    case "DestinationDragTarget Button":
                        rectTransforms[i].gameObject.SetActive(true);
                        dragDestinationTargetButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "Cancel Destination Button":
                        rectTransforms[i].gameObject.SetActive(true);
                        cancelDestinationButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "SelectDestinationCursorButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        selectDestinationCursorButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "WarpSlider":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    case "ButtonWarpUp":
                        rectTransforms[i].gameObject.SetActive(true);
                        warpButtonUpGO = rectTransforms[i].gameObject;
                        break;
                    case "ButtonWarpDown":
                        rectTransforms[i].gameObject.SetActive(true);
                        warpButtonDownGO = rectTransforms[i].gameObject;
                        break;
                    //case "ButtonCloseFleetUI":
                    //    rectTransforms[i].gameObject.SetActive(true);
                    //    closeFleetUIButtonGO = rectTransforms[i].gameObject;
                    //    break;
                    case "NewFleetButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        break;
                    case "MergeFleetButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        mergeFleetButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "ShipDeployButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        selectShipManagerCursorButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "CancelShipManagerButton":
                        rectTransforms[i].gameObject.SetActive(false);
                        cancelShipManagerButtonGO = rectTransforms[i].gameObject;
                        break;
                }
            }

            // Text bindings
            TextMeshProUGUI[] ourTMPs = fleetCon.FleetUIGameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < ourTMPs.Length; i++)
            {
                var name = ourTMPs[i].name;
                switch (name)
                {
                    case "Text FleetName (TMP)":
                        fleetName = ourTMPs[i];
                        ourTMPs[i].text = fleetCon.FleetData.Name;
                        break;
                    case "Destination Name Text":
                        destinationName = ourTMPs[i];
                        ourTMPs[i].text = "No Destination";
                        break;
                    case "Destination Coordinates":
                        destinationCoordinates = ourTMPs[i];
                        ourTMPs[i].text = "";
                        break;
                    case "Warp Value Text (TMP)":
                        ourTMPs[i].text = fleetCon.FleetData.CurrentWarpFactor.ToString("0.0");
                        break;
                }
            }

            // Slider wiring
            Slider slider = fleetCon.FleetUIGameObject.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                slider.onValueChanged.RemoveAllListeners();
                slider.value = fleetCon.FleetData.CurrentWarpFactor;
                slider.maxValue = fleetCon.FleetData.MaxWarpFactor;
                slider.onValueChanged.AddListener((value) => fleetCon.SliderOnValueChange(value));
            }

            // Buttons wiring
            Button[] listButtons = fleetCon.FleetUIGameObject.GetComponentsInChildren<Button>(true);
            foreach (var listButton in listButtons)
            {
                switch (listButton.name)
                {
                    case "SelectDestinationCursorButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => SelectedDestinationCursor(fleetCon));
                        break;
                    case "Cancel Destination Button":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => fleetCon.ClickCancelDestinationButton());
                        break;
                    case "ButtonWarpUp":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => fleetCon.FleetOnWarpUpClick(fleetCon));
                        break;
                    case "ButtonWarpDown":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => fleetCon.FleetOnWarpDownClick(fleetCon));
                        break;
                    case "DestinationDragTarget Button":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => fleetCon.GetPlayerDefinedTargetDestination(fleetCon));
                        break;
                    case "ButtonCloseFleetUI":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => fleetCon.CloseUnLoadFleetUI(fleetCon));
                        break;
                    case "NewFleetButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => ClickNewFleetButton(fleetCon));
                        break;
                    case "MergeFleetButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => ClickMergeFleetButton());
                        break;
                    case "ShipDeployButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => ClickShipDeployCursor(fleetCon));
                        break;
                    case "CancelShipManagerButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => ClickCancelShipManageButton());
                        break;
                    default:
                        break;

                }
            }

            // Attach existing ship list UIs (if present)
            for (int i = 0; i < fleetCon.FleetData.ShipsList.Count; i++)
            {
                if (fleetCon.FleetData.ShipsList[i].ShipListUIGameObject != null)
                {
                    var transforms = fleetCon.FleetUIGameObject.GetComponentsInChildren<Transform>(true);
                    for (int k = 0; k < transforms.Length; k++)
                    {
                        if (transforms[k].gameObject.name == "FleetShipContent")
                        {
                            fleetShipListContainer = transforms[k].gameObject;
                            break;
                        }
                    }
                    fleetCon.FleetData.ShipsList[i].ShipListUIGameObject.transform.SetParent(fleetShipListContainer.transform, false);
                }
            }
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
        fleetData.CurrentWarpFactor = 3f;
        fleetData.CivLongName = thisCivData.CivLongName; //.CivLongName;
        fleetData.CivShortName = thisCivData.CivShortName;
        var galaxyMenuUICon = GalaxyMenuUIController.Instance;
        galaxyMenuUICon.ResetClickMode();
        galaxyMenuUICon.FleetLookingForShipDeploy = oldFleetCon;
        galaxyMenuUICon.StarSystLookingForShipDeploy = null;
        ShipDeployMenuUIController.Instance.TopFleet = oldFleetCon;
        var emptyStarSysCon = StarSysManager.Instance.InstantiatEmptyStarSysController();
        var newFleet = fleetManager.InstantiateFleet(oldFleetCon, emptyStarSysCon, fleetData, position, true);
        galaxyMenuUICon.FleetConSelectedForShipDeploy = newFleet;
        galaxyMenuUICon.StarSystConSelectedForShipDeploy = null;
        tempFleetController = newFleet;
        ShipDeployMenuUIController.Instance.SetUpTopShipLists();
        ShipDeployMenuUIController.Instance.SetUpBottomShipLists(newFleet);
        ShipDeployMenuUIController.Instance.ShowShipDeployMenuView();
    }

    private void ClickShipDeployCursor(FleetController fleetCon)
    {
        var galaxyUI = GalaxyMenuUIController.Instance;
        galaxyUI.WhatFleetIsLookingForShipDeploy(fleetCon);
        galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipExchange);
        MousePointerChanger.Instance.SetShipExchangeCursor(fleetCon);
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
        cancelShipManagerButtonGO?.SetActive(false);
        galaxyUI.ClickCancelShipDeployButton();
        galaxyUI.ResetClickMode();
        galaxyUI.CompleteShipExchange();
    }
    public void UpdateFleetWarpUI(FleetController fleetCon, float theirWarp)
    {
        if (fleetCon?.FleetUIGameObject == null) return;


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
        if (fleetCon?.FleetUIGameObject == null) return;

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
            cancelDestinationButtonGO?.SetActive(true);
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
        cancelDestinationButtonGO?.SetActive(true);
        dragDestinationTargetButtonGO?.SetActive(false);
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
        //selectDestinationButtonText.text = "Select Destination";
        GalaxyMenuUIController.Instance.CurrentClickMode = GalaxyClickMode.SetDestination;
        MousePointerChanger.Instance.SetDestinationCursor();//ChangeToGalaxyMapCursorForLocalPlayer(fleetCon);
        //MousePointerChanger.Instance.HaveGalaxyMapCursor = true;
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
        //cancelShipManagerButtonGO?.SetActive(false);
    }

    private void MoveShipView(List<ShipController> upperShipsToMove, List<ShipController> lowerShipsToMove)
    {
        // drag and drop, Can we do this in MovingShipsView class?
    }
}
