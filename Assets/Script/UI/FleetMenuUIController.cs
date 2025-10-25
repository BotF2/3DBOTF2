
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Core;

public class FleetMenuUIController : MonoBehaviour
{
    public static FleetMenuUIController Instance;
    //private Camera galaxyEventCamera;
    //[SerializeField]
    //private Canvas parentCanvas;
    [Header("References (assign in Inspector)")]
    [SerializeField] private GameObject fleetMenuView;
    [SerializeField] private RectTransform fleetListContainer;
    [SerializeField] private GameObject fleetShipListContainer;
    [SerializeField] private GameObject aFleetMenuView;
    public GameObject AFleetMenuView => aFleetMenuView;
    [SerializeField] private GameObject aFleetShipContainer;
    [SerializeField] private TMP_Text fleetName;
    [Header("Destination UI (optional)")]
    [SerializeField] private TextMeshProUGUI destinationName;
    [SerializeField] private TextMeshProUGUI destinationCoordinates;
    //public float WarpValue;
    [SerializeField] private GameObject selectDestinationCursorButtonGO;
    [SerializeField] private GameObject cancelDestinationButtonGO;
    [SerializeField] private GameObject dragDestinationTargetButtonGO;

    [Header("Runtime lists")]
    [SerializeField] private List<GameObject> listOfFleetUiGos = new List<GameObject>();
    //public bool IsVisibleA_FleetMenuView => aFleetMenuView.activeSelf;
    //public bool IsVisibleFleetMenuView => fleetMenuView.activeSelf;

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
        // Initially hide fleet menu views
        if (fleetMenuView != null)
            fleetMenuView.SetActive(false);
        if (aFleetMenuView != null) 
            aFleetMenuView.SetActive(false);
        //galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
        //parentCanvas.worldCamera = galaxyEventCamera;
    }
    public void ShowFleetMenuView()
    {
        fleetMenuView.SetActive(true);
    }
    public void ShowA_FleetMenuView()
    {
        aFleetMenuView.SetActive(true);
    }
    public void HideFleetMenuView()
    {
        fleetMenuView.SetActive(false);
    }
    public void HideA_FleetMenuView()
    {
        aFleetMenuView.SetActive(false);
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
                fleetCon.FleetUIGameObject.transform.SetParent(fleetListContainer.transform, false);
            }
        }
    }
    public void SetUpAFleetUIData(FleetController theFleetCon)
    {
        theFleetCon.FleetUIGameObject.SetActive(true);
        theFleetCon.FleetUIGameObject.transform.SetParent(aFleetMenuView.transform, false);
    }
    public void MoveTheFleetUIGO(GameObject fleetConGO)
    {
        for (int i = 0; i < listOfFleetUiGos.Count; i++)
        {
            if (listOfFleetUiGos[i] == fleetConGO)
            {
                listOfFleetUiGos[i].transform.SetParent(aFleetMenuView.transform, false);
                return;
            }
        }
    }

    public void MoveBackAnyFleetUIGO()
    {
        for (int i = 0; i < aFleetMenuView.transform.childCount; i++)
        {
            var child = aFleetMenuView.transform.GetChild(i)?.gameObject;
            if (child != null)
                child.transform.SetParent(fleetListContainer.transform, false);
        }
    }

    public void SetupFleetUIElements(FleetController fleetCon, GameObject newFleetUIGO)
    {
        if (fleetCon == null || newFleetUIGO == null) return;
        if (!listOfFleetUiGos.Contains(fleetCon.FleetUIGameObject) && GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum))
        {
            newFleetUIGO.SetActive(true);
            fleetCon.FleetUIGameObject.transform.SetParent(fleetListContainer.transform, false);
            listOfFleetUiGos.Add(fleetCon.FleetUIGameObject);

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
                        listButton.onClick.AddListener(() => fleetCon.SelectedDestinationCursor(fleetCon));
                        break;
                    case "Cancel Destination Button":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => fleetCon.ClickCancelDestinationButton());
                        break;
                    case "DestinationDragTarget Button":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => fleetCon.GetPlayerDefinedTargetDestination(fleetCon));
                        break;
                    case "SelectOtherSysOrFleetForShipsButton": // ToDo; Implement functionality
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => fleetCon.SelectedOtherForShips(fleetCon));
                        break;
                    case "CancelSysOrFleetForShipsButton":
                        listButton.onClick.RemoveAllListeners();// ToDo; Implement functionality
                        listButton.onClick.AddListener(() => fleetCon.ClickCancelForShipsButton());
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

    public void LoadRightSideShipManagerFleetUIPrefab(GameObject fleetGo)
    {
        var fleetCon = fleetGo.GetComponent<FleetController>();
        if (fleetCon == null) return;
        if (GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum))
        {
            SetupFleetUIElements(fleetCon, fleetCon.RightSideShipManagementFleetUIGO);
            fleetCon.RightSideShipManagementFleetUIGO.SetActive(true);
            fleetCon.RightSideShipManagementFleetUIGO.transform.SetParent(aFleetMenuView.transform, false);
        }
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
}
