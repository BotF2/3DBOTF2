using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Core;
using System;
using System.Linq;
using UnityEngine.Experimental.XR.Interaction;
using System.Runtime.CompilerServices;
public enum Menu
{
    None,
    SystemsMenu,
    ASystemMenu,
    BuildMenu,
    FleetsMenu,
    AFleetMenu,
    ManageShipsMenu,
    DiplomacyMenu,
    ADiplomacyMenu,
    IntellMenu,
    EncyclopedianMenu,
    FirstContactMenu,
    HabitableSysMenu,
    Combat
}
public class GalaxyMenuUIController : MonoBehaviour
{
    public static GalaxyMenuUIController Instance; // { get; private set; }
    private Camera galaxyEventCamera;
    [SerializeField]
    private Canvas parentCanvas;
    [SerializeField]
    private GameObject buildListUI;
    [SerializeField]
    private GameObject systemsMenuView;
    [SerializeField]
    private GameObject sysListContainer;
    [SerializeField]
    private GameObject aSystemMenuView;
    [SerializeField]
    private GameObject sysBuildMenu;
    [SerializeField]
    private GameObject fleetsMenuView;
    [SerializeField]
    private GameObject fleetListContainer;
    [SerializeField]
    private GameObject aFleetMenuView;
    [SerializeField]
    private GameObject manageFleetShipsMenu;
    [SerializeField]
    private GameObject diplomacyMenuView;
    [SerializeField]
    private GameObject diplomacyListContainter;
    [SerializeField]
    private GameObject aDiplomacyMenuView;
    [SerializeField]
    private GameObject intelMenuView;
    [SerializeField]
    private GameObject encyclopediaMenuView;
    [SerializeField]
    private GameObject aNull;
    [SerializeField]
    private GameObject closeMenuButton;
    [SerializeField]
    private GameObject sysBackground;
    [SerializeField]
    private GameObject fleetBackground;
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
    private GameObject fleetUI_Prefab;// GameObject controlles this active UI on/off
    [SerializeField]
    private Slider warpSlider;

    [SerializeField]
    private float maxSliderValue = 10f;
    [SerializeField]
    private GameObject warpUpButtonGO;
    [SerializeField]
    private GameObject warpDownButtonGO;
    [SerializeField]
    private float warpChange = 0.1f;
    [SerializeField]
    private bool warpButtonPress = false;

    [SerializeField]
    private List<ShipData> shipList;
    private bool deltaShipList = false;
    private TMP_Dropdown shipDropdown;
    [SerializeField]
    private GameObject shipDropdownGO;
    [SerializeField]
    private TMP_Text dropdownShipText;
    [SerializeField]
    private TMP_Text FleetName;
    [SerializeField]
    private GameObject selectDestinationCursorButtonGO;
    [SerializeField]
    private GameObject cancelDestinationButtonGO;
    [SerializeField]
    private GameObject dragDestinationTargetButtonGO;
    public bool MouseClickSetsDestination = false;// used by FleetController and StarSysController
    [SerializeField]
    private TextMeshProUGUI destinationName;
    [SerializeField]
    private TextMeshProUGUI destinationCoordinates;
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
    void Start()
    {
        galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
        parentCanvas.worldCamera = galaxyEventCamera;
        systemsMenuView.SetActive(false);
        aSystemMenuView.SetActive(false);
        buildListUI.SetActive(false);
        fleetsMenuView.SetActive(false);
        aFleetMenuView.SetActive(false);
        manageFleetShipsMenu.SetActive(false);
        diplomacyMenuView.SetActive(false);
        aDiplomacyMenuView.SetActive(false);
        intelMenuView.SetActive(false);
        encyclopediaMenuView.SetActive(false);
        closeMenuButton.SetActive(false);
        sysBackground.SetActive(false);
        fleetBackground.SetActive(false);
        diplomacyBackground.SetActive(false);
        intelBackground.SetActive(false);
        encyclopediaBackground.SetActive(false);
        diplomacyControllers = new List<DiplomacyController>();
        SetupSystemUIData();//get our system ui game objects to match your sys controllers
        SetupFleetUIData();//get our fleet ui game objects to match your fleet controllers
    }

    public void SetActiveBuildMenu(GameObject prefabMenu)
    {
        sysBuildMenu = prefabMenu;
        sysBuildMenu.SetActive(true);
    }
    public void SetActiveManageFleetsShipMenu(GameObject prefabMenu)
    {
        manageFleetShipsMenu = prefabMenu;
        manageFleetShipsMenu.SetActive(true);
    }
    public void CloseTheBackgrounds()
    {
        closeMenuButton.SetActive(false);
        sysBackground.SetActive(false);
        fleetBackground.SetActive(false);
        diplomacyBackground.SetActive(false);
        intelBackground.SetActive(false);
        encyclopediaBackground.SetActive(false);
    }
    public void SystemButtonPressed()
    {
        if (systemsMenuView.activeSelf)
            CloseMenu(Menu.SystemsMenu);
        else if (aSystemMenuView.activeSelf)
            CloseMenu(Menu.ASystemMenu);
        else
            OpenMenu(Menu.SystemsMenu, null);
    }
    public void FleetButtonPressed() // The CanvasGalaxyMenuRibbon/MainGalaxyMenuPanel/FleetsButton in the Hierarchy is set to this class.method
    {
        if (fleetsMenuView.activeSelf)
        {
            CloseMenu(Menu.FleetsMenu);
        }
        else if (aFleetMenuView.activeSelf)
        {
            CloseMenu(Menu.AFleetMenu);
        }
        else
            OpenMenu(Menu.FleetsMenu, null);
    }
    public void DiplomacyButtonPressed()
    {
        if (diplomacyMenuView.activeSelf)
            CloseMenu(Menu.DiplomacyMenu);
        else if (aDiplomacyMenuView.activeSelf)
            CloseMenu(Menu.ADiplomacyMenu);
        else
            OpenMenu(Menu.DiplomacyMenu, null);
    }
    public void IntelButtonPressed()
    {
        if (intelMenuView.activeSelf)
            CloseMenu(Menu.IntellMenu);
        else
            OpenMenu(Menu.IntellMenu, null);
    }
    public void EncylopediaButtonPressed()
    {
        if (encyclopediaMenuView.activeSelf)
            CloseMenu(Menu.EncyclopedianMenu);
        else
            OpenMenu(Menu.EncyclopedianMenu, null);
    }
    public void CombateButtonPressed()
    {
        if (diplomacyMenuView.activeSelf)
            CloseMenu(Menu.DiplomacyMenu);

    }
    // Home System view is in GalaxyCameraDragMoveZoom.cs
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
                CloseTheBackgrounds();
                MoveBackAnySysUIGO();
                systemsMenuView.SetActive(true);
                sysBackground.SetActive(true);
                openMenuWas = systemsMenuView;
                openMenuEnumWas = Menu.SystemsMenu;
                break;
            case Menu.ASystemMenu:
                CloseTheBackgrounds();
                SetUpASystemUIData(callingMenuOrGalaxyObject.GetComponentInChildren<StarSysController>());
                sysBackground.SetActive(true);
                aSystemMenuView.SetActive(true);
                MoveTheSysUIGO(callingMenuOrGalaxyObject);
                openMenuWas = aSystemMenuView;
                openMenuEnumWas = Menu.ASystemMenu;
                break;
            case Menu.BuildMenu:
                InactivateCallingMenu(callingMenuOrGalaxyObject);
                sysBuildMenu.SetActive(true);
                openMenuWas = sysBuildMenu;
                openMenuEnumWas = Menu.BuildMenu;
                break;
            case Menu.FleetsMenu:
                CloseTheBackgrounds();
                MoveBackAnyFleetUIGO();
                fleetsMenuView.SetActive(true);
                fleetBackground.SetActive(true);
                openMenuWas = fleetsMenuView;
                openMenuEnumWas = Menu.FleetsMenu;
                break;
            case Menu.AFleetMenu:
                CloseTheBackgrounds();
                SetUpAFleetUIData(callingMenuOrGalaxyObject.GetComponentInChildren<FleetController>());
                aFleetMenuView.SetActive(true);
                fleetBackground.SetActive(true);
                MoveTheFleetUIGO(callingMenuOrGalaxyObject);
                openMenuWas = aFleetMenuView;
                openMenuEnumWas = Menu.AFleetMenu;
                break;
            case Menu.ManageShipsMenu:
                InactivateCallingMenu(callingMenuOrGalaxyObject);
                manageFleetShipsMenu.SetActive(true);
                openMenuWas = manageFleetShipsMenu;
                openMenuEnumWas = Menu.ManageShipsMenu;
                break;
            case Menu.DiplomacyMenu:
                CloseTheBackgrounds();
                TimeManager.Instance.PauseTime();
                // ToDo: put a pause indicator on screen
                diplomacyMenuView.SetActive(true);
                diplomacyBackground.SetActive(true);
                openMenuWas = diplomacyMenuView;
                openMenuEnumWas = Menu.DiplomacyMenu;
                MoveBackAnyDiplomacyUIGO();
                break;
            case Menu.ADiplomacyMenu:
                CloseTheBackgrounds();
                TimeManager.Instance.PauseTime();
                aDiplomacyMenuView.SetActive(true);
                diplomacyBackground.SetActive(true);
                MoveTheDiplomacyUIGO(callingMenuOrGalaxyObject);
                openMenuWas = aDiplomacyMenuView;
                openMenuEnumWas = Menu.ADiplomacyMenu;
                break;
            case Menu.IntellMenu:
                CloseTheBackgrounds();
                intelMenuView.SetActive(true);
                intelBackground.SetActive(true);
                openMenuWas = intelMenuView;
                openMenuEnumWas = Menu.IntellMenu;
                break;
            case Menu.EncyclopedianMenu:
                CloseTheBackgrounds();
                InactivateCallingMenu(callingMenuOrGalaxyObject);
                encyclopediaMenuView.SetActive(true);
                intelBackground.SetActive(true);
                openMenuWas = encyclopediaMenuView;
                openMenuEnumWas = Menu.EncyclopedianMenu;
                break;
            case Menu.HabitableSysMenu:
                habitableSysMenu.SetActive(true);
                openMenuWas = habitableSysMenu;
                openMenuEnumWas = Menu.HabitableSysMenu;
                break;
            case Menu.Combat:
                //going to CombatScene...
                break;
            default:
                break;
        }
    }
    private void InactivateCallingMenu(GameObject callingMenu)
    {
        if (callingMenu != null)
            callingMenu.SetActive(false);
    }

    private void SetUpAFleetUIData(FleetController theFleetCon)
    {
        theFleetCon.FleetUIGameObject.SetActive(true);
        theFleetCon.FleetUIGameObject.transform.SetParent(aFleetMenuView.transform, false);
    }
    private void SetUpASystemUIData(StarSysController theSysCon) // now system ui open single system view when our system is clicked on galaxy map
    {
        theSysCon.StarSystUIGameObject.SetActive(true);
        theSysCon.StarSystUIGameObject.transform.SetParent(aSystemMenuView.transform, false);
    }
    private void SetUpADiplomacyUIData(DiplomacyController theDiploCon) // now system ui open single system view when our system is clicked on galaxy map
    {
        theDiploCon.DiplomacyUIGameObject.SetActive(true);
        theDiploCon.DiplomacyUIGameObject.transform.SetParent(aDiplomacyMenuView.transform, false);
    }

    public void CloseMenu(Menu enumMenu)
    {
        switch (enumMenu)
        {
            case Menu.None:
                openMenuWas = null;
                break;
            case Menu.SystemsMenu:
                sysBackground.SetActive(false);
                systemsMenuView.SetActive(false);
                openMenuWas = systemsMenuView;
                break;
            case Menu.ASystemMenu:
                MoveBackAnySysUIGO();
                sysBackground.SetActive(false);
                aSystemMenuView.SetActive(false);
                openMenuWas = aSystemMenuView;
                break;
            case Menu.BuildMenu:
                sysBuildMenu.SetActive(false);
                openMenuWas = sysBuildMenu;
                break;
            case Menu.FleetsMenu:
                CloseDestinationSelectionCursor();
                fleetBackground.SetActive(false);
                fleetsMenuView.SetActive(false);
                openMenuWas = fleetsMenuView;
                break;
            case Menu.AFleetMenu:
                MoveBackAnyFleetUIGO();
                CloseDestinationSelectionCursor();
                fleetBackground.SetActive(false);
                aFleetMenuView.SetActive(false);
                openMenuWas = aFleetMenuView;
                break;
            case Menu.DiplomacyMenu:
                diplomacyBackground.SetActive(false);
                diplomacyMenuView.SetActive(false);
                TimeManager.Instance.ResumeTime();
                openMenuWas = diplomacyMenuView;
                break;
            case Menu.ADiplomacyMenu:
                MoveBackAnyDiplomacyUIGO();
                diplomacyBackground.SetActive(false);
                aDiplomacyMenuView.SetActive(false);
                TimeManager.Instance.ResumeTime();
                openMenuWas = aDiplomacyMenuView;
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
                TimeManager.Instance.ResumeTime();
                habitableSysMenu.SetActive(false);
                openMenuWas = habitableSysMenu;
                break;
            case Menu.Combat:// change scenes
                //combat.SetActive(true);
                break;
            default:
                break;
        }
    }


    #region FleetUI
    private void MoveTheFleetUIGO(GameObject fleetConGO)
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
    private void MoveBackAnyFleetUIGO()
    {
        for (int i = 0; i < aFleetMenuView.transform.childCount; i++)
        {
            if (aFleetMenuView.transform.GetChild(i).gameObject != null)
                aFleetMenuView.transform.GetChild(i).gameObject.transform.SetParent(fleetListContainer.transform, false); ;
        }
    }
    public void SetupFleetUIData()
    { // populate the fleet UIs with the data from the fleetControllers...

        for (int j = 0; j < FleetManager.Instance.FleetControllerList.Count; j++)
        {
            var fleetCon = FleetManager.Instance.FleetControllerList[j];
            if (!listOfFleetUiGos.Contains(fleetCon.FleetUIGameObject) && GameController.Instance.AreWeLocalPlayer(fleetCon.FleetData.CivEnum))
            {
                {
                    fleetCon.FleetUIGameObject.SetActive(true);
                    fleetCon.FleetUIGameObject.transform.SetParent(fleetListContainer.transform, false);
                    fleetControllers.Add(fleetCon);// add to list for ContentFleets GalaxyMenuUI knows 
                    listOfFleetUiGos.Add(fleetCon.FleetUIGameObject); // add to list of FleetUI Game Objects GalaxyMenuUI knows
                    RectTransform[] rectTransforms = fleetCon.FleetUIGameObject.GetComponentsInChildren<RectTransform>();
                    for (int i = 0; i < rectTransforms.Length; i++)
                    {
                        switch (rectTransforms[i].name)
                        {
                            case "RedDot":
                                rectTransforms[i].gameObject.SetActive(true);
                                float x = fleetCon.FleetData.Position.x * 0.12f; // 0.12f is our cosmologic constant, fudge factor
                                float y = 0f;
                                float z = fleetCon.FleetData.Position.z * 0.12f;
                                rectTransforms[i].Translate(new Vector3(x, z, y), Space.Self); // flip z and y from main galaxy map to UI mini map
                                break;

                            case "DestinationDragTarget Button":
                                rectTransforms[i].gameObject.SetActive(true); // Ensure the GameObject is active
                                dragDestinationTargetButtonGO = rectTransforms[i].gameObject; // Assign the GameObject reference to the variable
                                break;
                            case "Cancel Destination Button":
                                rectTransforms[i].gameObject.SetActive(true);
                                cancelDestinationButtonGO = rectTransforms[i].gameObject;
                                break;
                            case "ButtonWarpUp":
                                rectTransforms[i].gameObject.SetActive(true);
                                warpUpButtonGO = rectTransforms[i].gameObject;
                                break;
                            case "ButtonWarpDown":
                                rectTransforms[i].gameObject.SetActive(true);
                                warpDownButtonGO = rectTransforms[i].gameObject;
                                break;
                            case "SelectDestinationCursorButton":
                                rectTransforms[i].gameObject.SetActive(true);
                                selectDestinationCursorButtonGO = rectTransforms[i].gameObject;
                                break;
                            case "Destination Coordinates":
                                rectTransforms[i].gameObject.SetActive(true);
                                destinationCoordinates = rectTransforms[i].GetComponent<TextMeshProUGUI>();
                                break;
                            case "Destination Name Text":
                                rectTransforms[i].gameObject.SetActive(true);
                                destinationName = rectTransforms[i].GetComponent<TextMeshProUGUI>();
                                break;
                            case "WarpSlider":
                                rectTransforms[i].gameObject.SetActive(true);
                                warpSlider = rectTransforms[i].GetComponent<Slider>();
                                //fleetConWaitingForDestination.destinationName = destinationName;
                                break;
                            default:
                                break;
                        }
                    }
                }

                TextMeshProUGUI[] ourTMPs = fleetCon.FleetUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
                for (int i = 0; i < ourTMPs.Length; i++)
                {
                    int techLevelInt = (int)CivManager.Instance.LocalPlayerCivContoller.CivData.TechLevel / 100; // Early Tech level = 100, Supreme = 900;
                    ourTMPs[i].enabled = true;
                    var name = ourTMPs[i].name;

                    switch (name)
                    {
                        case "Text FleetName (TMP)":
                            ourTMPs[i].text = fleetCon.FleetData.Name;
                            break;
                        case "Destination Name Text":
                            ourTMPs[i].text = "No Destination";
                            break;
                        case "Destination Coordinates":
                            ourTMPs[i].text = "";
                            break;
                        case "Warp Value Text (TMP)":
                            ourTMPs[i].text = fleetCon.FleetData.CurrentWarpFactor.ToString("0.0");
                            break;
                        case "FleetMaxWarpFactor":
                            ourTMPs[i].text = fleetCon.FleetData.MaxWarpFactor.ToString("0.0");
                            break;
                    }
                }
                Slider slider = fleetCon.FleetUIGameObject.GetComponentInChildren<Slider>();
                if (slider != null)
                {
                    slider.onValueChanged.RemoveAllListeners();
                    slider.value = fleetCon.FleetData.CurrentWarpFactor;
                    slider.maxValue = fleetCon.FleetData.MaxWarpFactor;
                    slider.enabled = true;
                    slider.onValueChanged.AddListener((value) => fleetCon.SliderOnValueChange(value));
                }
                Button[] listButtons = fleetCon.FleetUIGameObject.GetComponentsInChildren<Button>();
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
                            listButton.onClick.AddListener(() => fleetCon.ClickCancelDestinationButton(fleetCon));
                            break;
                        case "DestinationDragTarget Button":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => fleetCon.GetPlayerDefinedTargetDestination(fleetCon));
                            break;
                        case "ButtonShipManager":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => fleetCon.ShipManageClick(fleetCon));
                            break;
                        case "ButtonWarpUp":
                            fleetCon.FleetData.FleetButtonUp = listButton;
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => fleetCon.FleetOnWarpUpClick(fleetCon));
                            break;
                        case "ButtonWarpDown":
                            fleetCon.FleetData.FleetButtonDown = listButton;
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => fleetCon.FleetOnWarpDownClick(fleetCon));
                            break;
                        case "ButtonCloseFleetUI":
                            fleetCon.FleetData.FleetButtonUIClose = listButton;
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => fleetCon.CloseUnLoadFleetUI());  //fleetCon));
                            break;
                        default:
                            break;
                    }
                }
                //ship dropdown
                var shipDropdown = fleetCon.FleetUIGameObject.GetComponentInChildren<TMP_Dropdown>();
                shipDropdown.options.Clear();
                shipDropdown.captionText.text = "Ship List";
                List<TMP_Dropdown.OptionData> newShipItems = new List<TMP_Dropdown.OptionData>();
                string nameShip;
                for (int i = 0; i < fleetCon.FleetData.ShipsList.Count; i++)
                {
                    if (fleetCon.FleetData.ShipsList[i] != null)
                    {
                        TMP_Dropdown.OptionData newDataItem = new TMP_Dropdown.OptionData();
                        nameShip = fleetCon.FleetData.ShipsList[i].name;
                        nameShip = nameShip.Replace("(CLONE)", string.Empty);
                        newDataItem.text = nameShip;
                        newShipItems.Add(newDataItem);
                    }
                }
                shipDropdown.AddOptions(newShipItems);
                shipDropdown.RefreshShownValue();
            }
            if (fleetCon.FleetUIGameObject != null)
            {
                fleetCon.FleetUIGameObject.SetActive(true);

                fleetCon.FleetUIGameObject.transform.SetParent(fleetListContainer.transform, false);
            }
        }

    }
    public void OnClickShipManager()
    {
        GameObject notAMenu = new GameObject();
        OpenMenu(Menu.AFleetMenu, notAMenu);
        //Destroy(notAMenu);
    }
    public void CloseDestinationSelectionCursor()
    {
        MouseClickSetsDestination = false;
        MousePointerChanger.Instance.ResetCursor();
    }
    private void ReorderDropdownOptions(TMP_Dropdown dropdown)
    {
        List<TMP_Dropdown.OptionData> options = dropdown.options;
        options.Reverse();
        // Update the UI
        dropdown.RefreshShownValue();
    }
    public void UpdateFleetMaxWarpUI(FleetController fleetCon, float theirMaxWarp)
    {
        float maxSliderValue = theirMaxWarp;

        Slider slider = fleetCon.FleetUIGameObject.GetComponentInChildren<Slider>();
        if (slider != null)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.maxValue = theirMaxWarp;
            if (fleetCon.FleetData.CurrentWarpFactor > theirMaxWarp)
            {
                fleetCon.FleetData.CurrentWarpFactor = theirMaxWarp;
                slider.value = fleetCon.FleetData.CurrentWarpFactor;
                slider.onValueChanged.AddListener((value) => fleetCon.SliderOnValueChange(value));
            }
        }

        TextMeshProUGUI[] OneTMP = fleetCon.FleetUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        bool areWeDoneYet = false;
        bool areWeDone = false;
        for (int i = 0; i < OneTMP.Length; i++)
        {
            OneTMP[i].enabled = true;
            var itemName = OneTMP[i].name.ToString();

            if ("FleetMaxWarpFactor" == OneTMP[i].name)
            {
                OneTMP[i].text = maxSliderValue.ToString("0.0");
                areWeDoneYet = true;
            }
            else if ("Warp Value Text (TMP)" == OneTMP[i].name)
            {
                OneTMP[i].text = fleetCon.FleetData.CurrentWarpFactor.ToString("0.0");
            }
            if (areWeDoneYet && areWeDone)
            {

                return;
            }
        }
    }
    public void UpdateFleetWarpUI(FleetController fleetCon, float theirWarp)
    {
        float warpSliderValue = theirWarp;
        Slider slider = fleetCon.FleetUIGameObject.GetComponentInChildren<Slider>();
        if (slider != null)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.value = warpSliderValue;
            slider.maxValue = fleetCon.FleetData.MaxWarpFactor;
            slider.onValueChanged.AddListener((value) => fleetCon.SliderOnValueChange(value));
        }

        TextMeshProUGUI[] OneTMP = fleetCon.FleetUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        bool areWeThereYet = false;
        bool areWeDoneYet = false;
        for (int i = 0; i < OneTMP.Length; i++)
        {
            OneTMP[i].enabled = true;
            var itemName = OneTMP[i].name.ToString();

            // ToDo: work in tech levels
            if ("FleetMaxWarpFactor" == OneTMP[i].name)
            {
                OneTMP[i].text = fleetCon.FleetData.MaxWarpFactor.ToString("0.0");
                areWeDoneYet = true;
            }
            if ("Warp Value Text (TMP)" == OneTMP[i].name)
            {
                OneTMP[i].text = warpSliderValue.ToString("0.0");
                areWeThereYet = true;
            }
            else if (areWeThereYet && areWeDoneYet)
            {
                return;
            }
        }
    }

    public void SelectedDestinationCursor(FleetController fleetConWaitingForDestination)
    {
        if (GameController.Instance.AreWeLocalPlayer(fleetConWaitingForDestination.FleetData.CivEnum))
        {
            if (MousePointerChanger.Instance.HaveGalaxyMapCursor == false)
            {
                // FleetConsLookingForDestination.Add(fleetConWaitingForDestination); // list for mulitplayer games
                dragDestinationTargetButtonGO.SetActive(false); // to see cancel destination button below
                cancelDestinationButtonGO.SetActive(true);
                selectDestinationCursorButtonGO.SetActive(false);
                MouseClickSetsDestination = true;
                MousePointerChanger.Instance.ChangeToGalaxyMapCursor(fleetConWaitingForDestination);
                MousePointerChanger.Instance.HaveGalaxyMapCursor = true;
            }
        }
    }
    public void ClickCancelDestinationButton(FleetController fleetCon)
    {
        UpdateFleetWarpUI(fleetCon, 0f);
        MousePointerChanger.Instance.ResetCursor();
        MousePointerChanger.Instance.HaveGalaxyMapCursor = false;
        destinationName.text = "No Destination";
        destinationCoordinates.text = "";
        MouseClickSetsDestination = false;
        selectDestinationCursorButtonGO.SetActive(true);
        dragDestinationTargetButtonGO.SetActive(true);
        cancelDestinationButtonGO.SetActive(false);
        for (int i = 0; i < listOfFleetUiGos.Count; i++)
        {
            if (listOfFleetUiGos[i].GetComponentInChildren<FleetController>() == fleetCon)
            {
                TextMeshProUGUI[] ourTMPs = listOfFleetUiGos[i].GetComponentsInChildren<TextMeshProUGUI>();
                for (int j = 0; j < ourTMPs.Length; j++)
                {
                    ourTMPs[j].enabled = true;
                    var name = ourTMPs[i].name;
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
        destinationName.text = nameDestination;
        destinationCoordinates.text = newCoordinates;
        MouseClickSetsDestination = false;
        cancelDestinationButtonGO.SetActive(true);
        dragDestinationTargetButtonGO.SetActive(false);
    }
    #endregion FleetUI

    #region Star System UI
    public void RemoveSystem(StarSysController sysController)
    {
        sysControllers.Remove(sysController);
        listOfStarSysUiGos.Remove(sysController.StarSystUIGameObject);
    }
    private void MoveTheSysUIGO(GameObject sysConGO)
    {
        int numFound = 0;
        List<GameObject> foundGoList = new List<GameObject>();
        for (int i = 0; i < aSystemMenuView.transform.childCount; i++)
        {
            numFound = i;
            if (i > 0)
                foundGoList.Add(aSystemMenuView.transform.GetChild(i).gameObject);
        }
        if (numFound > 0)
            for (int j = 0; j < numFound; j++)
                Destroy(foundGoList[j]);
        for (int i = 0; i < listOfStarSysUiGos.Count; i++)
        {
            if (listOfStarSysUiGos[i] == sysConGO)
            {
                listOfStarSysUiGos[i].transform.SetParent(aSystemMenuView.transform, false);
            }
        }
    }
    private void MoveBackAnySysUIGO()
    {

        for (int i = 0; i < aSystemMenuView.transform.childCount; i++)
        {
            if (aSystemMenuView.transform.GetChild(i).gameObject != null)
                aSystemMenuView.transform.GetChild(i).gameObject.transform.SetParent(sysListContainer.transform, false); ;
        }
    }
    public void SetupSystemUIData()
    { // populate the system UI with the data from the sysController

        foreach (var sysController in StarSysManager.Instance.StarSysControllerList)
        {
            if (!listOfStarSysUiGos.Contains(sysController.StarSystUIGameObject) && GameController.Instance.AreWeLocalPlayer(sysController.StarSysData.CurrentOwnerCivEnum))
            {
                sysController.StarSystUIGameObject.SetActive(true);
                sysController.StarSystUIGameObject.transform.SetParent(sysListContainer.transform, false);
                sysControllers.Add(sysController);// add to list for the ContentSystems (queue) folder
                listOfStarSysUiGos.Add(sysController.StarSystUIGameObject);
                RectTransform[] minMapDotTransfor = sysController.StarSystUIGameObject.GetComponentsInChildren<RectTransform>();
                for (int i = 0; i < minMapDotTransfor.Length; i++)
                {
                    if (minMapDotTransfor[i].name == "RedDot")
                    {
                        float x = sysController.StarSysData.GetPosition().x * 0.12f; // 0.12f is our cosmologic constant, fudge factor
                        float y = 0f;
                        float z = sysController.StarSysData.GetPosition().z * 0.12f;
                        minMapDotTransfor[i].Translate(new Vector3(x, z, y), Space.Self); // flip z and y from main galaxy map to UI mini map
                        break;
                    }
                }

                TextMeshProUGUI[] OneTMP = sysController.StarSystUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
                for (int i = 0; i < OneTMP.Length; i++)
                {
                    int techLevelInt = (int)CivManager.Instance.LocalPlayerCivContoller.CivData.TechLevel / 100; // Early Tech level = 100, Supreme = 900;
                    OneTMP[i].enabled = true;
                    var name = OneTMP[i].name;

                    switch (name)
                    {
                        case "SysName": // text in the button for sys activation
                            OneTMP[i].text = sysController.StarSysData.SysName;
                            break;
                        case "HeaderPowerUnitText":
                            //if (sysController.StarSysData.PowerPlants.Count > 0)  
                            OneTMP[i].text = sysController.StarSysData.PowerPlantData.Name;
                            //ToDo: can make it race specific here, not defaul "Plasma Reactor"
                            break;
                        case "NumPUnits":
                            OneTMP[i].text = (sysController.StarSysData.PowerPlants.Count).ToString();
                            break;
                        case "NumTotalEOut":
                            sysController.StarSysData.TotalSysPowerOutput = sysController.StarSysData.PowerPlants.Count * sysController.StarSysData.PowerPlantData.PowerOutput;
                            OneTMP[i].text = (sysController.StarSysData.TotalSysPowerOutput).ToString();
                            break;
                        // ToDo: use techLevelInt in power output 
                        //case "NumP Load": *** do this last to get total power load sum with UpdateSystemPowerLoad(sysController);

                        case "NameFactory":
                            OneTMP[i].text = sysController.StarSysData.FactoryData.Name;
                            break;
                        case "NumFactoryRatio":
                            int count = 0;
                            foreach (var item in sysController.StarSysData.Factories)
                            {
                                TextMeshProUGUI TheText = item.GetComponent<TextMeshProUGUI>();
                                if (TheText.text == "1") // 1 = on and 0 = off
                                    count++;
                            }
                            OneTMP[i].text = count.ToString() + "/" + (sysController.StarSysData.Factories.Count).ToString();
                            break;
                        case "FactoryLoad":
                            // for now all are turned on
                            OneTMP[i].text = (sysController.StarSysData.FactoryData.PowerLoad * sysController.StarSysData.Factories.Count).ToString();
                            // ToDo: work in tech levels
                            break;

                        case "ShipyardName":
                            OneTMP[i].text = sysController.StarSysData.ShipyardData.Name;
                            break;
                        case "NumYardsOnRatio":
                            int count1 = 0;
                            foreach (var item in sysController.StarSysData.Shipyards)
                            {
                                TextMeshProUGUI TheText = item.GetComponent<TextMeshProUGUI>();
                                if (TheText.text == "1")
                                    count1++;
                            }
                            OneTMP[i].text = count1.ToString() + "/" + (sysController.StarSysData.Shipyards.Count).ToString();
                            break;
                        case "YardLoad":
                            // for now all are turned on
                            OneTMP[i].text = (sysController.StarSysData.ShipyardData.PowerLoad * sysController.StarSysData.Shipyards.Count).ToString();
                            // ToDo: work in tech levels
                            break;
                        //ToDo: Yard's build Queue here
                        case "ShieldName":
                            OneTMP[i].text = sysController.StarSysData.ShieldGeneratorData.Name;
                            break;
                        case "NumShieldRatio":
                            int count2 = 0;
                            foreach (var item in sysController.StarSysData.ShieldGenerators)
                            {
                                TextMeshProUGUI TheText = item.GetComponent<TextMeshProUGUI>();
                                if (TheText.text == "1")
                                    count2++;
                            }
                            OneTMP[i].text = count2.ToString() + "/" + (sysController.StarSysData.ShieldGenerators.Count).ToString();
                            break;
                        case "ShieldLoad":
                            OneTMP[i].text = (sysController.StarSysData.ShieldGeneratorData.PowerLoad * sysController.StarSysData.ShieldGenerators.Count).ToString();
                            // ToDo: work in tech levels
                            break;
                        case "OBName":
                            OneTMP[i].text = sysController.StarSysData.OrbitalBatteryData.Name;
                            break;
                        case "NumOBRatio":
                            int count3 = 0;
                            foreach (var item in sysController.StarSysData.OrbitalBatteries)
                            {
                                TextMeshProUGUI TheText = item.GetComponent<TextMeshProUGUI>();
                                if (TheText.text == "1")
                                    count3++;
                            }
                            OneTMP[i].text = count3.ToString() + "/" + (sysController.StarSysData.OrbitalBatteries.Count).ToString();
                            break;
                        case "OBLoad":
                            OneTMP[i].text = (sysController.StarSysData.OrbitalBatteryData.PowerLoad * sysController.StarSysData.OrbitalBatteries.Count).ToString();
                            // ToDo: work in tech levels
                            break;
                        case "ResearchName":
                            OneTMP[i].text = sysController.StarSysData.ResearchCenterData.Name;
                            break;
                        case "NumResearchRatio":
                            int count4 = 0;
                            foreach (var item in sysController.StarSysData.ResearchCenters)
                            {
                                TextMeshProUGUI TheText = item.GetComponent<TextMeshProUGUI>();
                                if (TheText.text == "1")
                                    count4++;
                            }
                            OneTMP[i].text = count4.ToString() + "/" + (sysController.StarSysData.ResearchCenters.Count).ToString();
                            break;
                        case "ResearchLoad":
                            OneTMP[i].text = (sysController.StarSysData.ResearchCenterData.PowerLoad * sysController.StarSysData.ResearchCenters.Count).ToString();
                            // ToDo: work in tech levels
                            break;
                        case "PowerOverload":
                            OneTMP[i].gameObject.SetActive(false);
                            powerOverload = OneTMP[i].gameObject; //.SetActive(true);
                            break;
                        default:
                            break;

                    }
                    if (true)//GameController.Instance.AreWeLocalPlayer(sysController.StarSysData.CurrentOwnerCivEnum))
                    {
                        //UpdateSystemPowerLoad(sysController);
                    }
                }
                Image[] listOfImages = sysController.StarSystUIGameObject.GetComponentsInChildren<Image>();
                for (int i = 0; i < listOfImages.Length; i++)
                {
                    // int techLevelInt = (int)CivManager.Instance.LocalPlayerCivContoller.CivData.StartingTechLevel / 100; // Early Tech level = 100, Supreme = 900;
                    listOfImages[i].enabled = true;
                    var name = listOfImages[i].name.ToString();
                    switch (name)
                    {
                        case "PowerUnitImage":
                            listOfImages[i].sprite = ThemeManager.Instance.CurrentTheme.PowerPlantImage;

                            break;
                        case "FactoryImage":
                            listOfImages[i].sprite = ThemeManager.Instance.CurrentTheme.FactoryImage;

                            break;
                        case "shipyardImage":
                            listOfImages[i].sprite = ThemeManager.Instance.CurrentTheme.ShipyardImage;

                            break;
                        case "ShieldPlantImage":
                            listOfImages[i].sprite = ThemeManager.Instance.CurrentTheme.ShieldImage;

                            break;
                        case "OrbitalBatteriesImage":
                            listOfImages[i].sprite = ThemeManager.Instance.CurrentTheme.OrbitalBatteriesImage;

                            break;
                        case "ResearchImage":
                            listOfImages[i].sprite = ThemeManager.Instance.CurrentTheme.ResearchCenterImage;
                            break;
                        case "PowerOverload":
                            powerOverload = listOfImages[i].gameObject;
                            listOfImages[i].gameObject.SetActive(false);
                            break;
                        default:
                            break;
                    }
                }

                Button[] listButtons = sysController.StarSystUIGameObject.GetComponentsInChildren<Button>();
                foreach (var listButton in listButtons)
                {
                    switch (listButton.name)
                    {
                        case "BuildButton":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.BuildClick(sysController));
                            break;
                        case "ShipButton":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.ShipClick(sysController));
                            break;
                        case "FactoryButtonOn":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "FactoryButtonOff":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "YardButtonOn":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "YardButtonOff":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "ShieldButtonOn":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "ShieldButtonOff":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "OBButtonOn":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "OBButtonOff":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "ResearchButtonOn":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "ResearchButtonOff":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        default:
                            break;
                    }
                }
            }
            if (sysController.StarSystUIGameObject != null)
            {
                sysController.StarSystUIGameObject.SetActive(true);

                sysController.StarSystUIGameObject.transform.SetParent(sysListContainer.transform, false);
            }

        }
    }
    public void UpdateFacilityUI(StarSysController sysController, int plusMinus, string loadName, string ratioName, StarSysFacilities facilityType)
    {
        if (GameController.Instance.AreWeLocalPlayer(sysController.StarSysData.CurrentOwnerCivEnum))
        {
            int newFacilityLoad = 0;
            List<GameObject> facilities = new List<GameObject>();
            switch (facilityType)
            {
                case StarSysFacilities.Factory:
                    newFacilityLoad = sysController.StarSysData.FactoryData.PowerLoad;
                    facilities = sysController.StarSysData.Factories;
                    break;
                case StarSysFacilities.Shipyard:
                    newFacilityLoad = sysController.StarSysData.ShipyardData.PowerLoad;
                    facilities = sysController.StarSysData.Shipyards;
                    break;
                case StarSysFacilities.ShieldGenerator:
                    newFacilityLoad = sysController.StarSysData.ShieldGeneratorData.PowerLoad;
                    facilities = sysController.StarSysData.ShieldGenerators;
                    break;
                case StarSysFacilities.OrbitalBattery:
                    newFacilityLoad = sysController.StarSysData.OrbitalBatteryData.PowerLoad;
                    facilities = sysController.StarSysData.OrbitalBatteries;
                    break;
                case StarSysFacilities.ResearchCenter:
                    newFacilityLoad = sysController.StarSysData.ResearchCenterData.PowerLoad;
                    facilities = sysController.StarSysData.ResearchCenters;
                    break;
                default:
                    break;
            }
            int numOn = 0;
            TextMeshProUGUI[] OneTMP = sysController.StarSystUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < OneTMP.Length; i++)
            {
                OneTMP[i].enabled = true;
                var itemName = OneTMP[i].name.ToString();
                bool areWeDoneLooking = false;
                // ToDo: work in tech levels
                if (loadName == OneTMP[i].name)
                {
                    int load = Int32.Parse(OneTMP[i].text);
                    load += plusMinus * newFacilityLoad;
                    OneTMP[i].text = load.ToString();
                }
                if (ratioName == OneTMP[i].name)
                {
                    for (int j = 0; j < facilities.Count; j++)
                    {
                        TextMeshProUGUI TheText = facilities[j].GetComponent<TextMeshProUGUI>();
                        if (TheText.text == "1")
                            numOn++;
                    }
                    OneTMP[i].text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    areWeDoneLooking = true;
                }
                else if (areWeDoneLooking)
                {
                    break;
                }
            }
            //UpdateSystemPowerLoad(sysController);
        }
    }

    public void UpdateSystemPowerLoad(StarSysController sysCon)
    {
        int load = 0;
        for (int i = 0; i < sysCon.StarSysData.Factories.Count; i++)
        {
            if (sysCon.StarSysData.Factories[i].GetComponent<TextMeshProUGUI>().text == "1")
            {
                load += sysCon.StarSysData.FactoryData.PowerLoad;
            }
        }
        for (int i = 0; i < sysCon.StarSysData.Shipyards.Count; i++)
        {
            if (sysCon.StarSysData.Shipyards[i].GetComponent<TextMeshProUGUI>().text == "1")
            {
                load += sysCon.StarSysData.ShipyardData.PowerLoad;
            }
        }
        for (int i = 0; i < sysCon.StarSysData.ShieldGenerators.Count; i++)
        {
            if (sysCon.StarSysData.ShieldGenerators[i].GetComponent<TextMeshProUGUI>().text == "1")
            {
                load += sysCon.StarSysData.ShieldGeneratorData.PowerLoad;
            }
        }
        for (int i = 0; i < sysCon.StarSysData.OrbitalBatteries.Count; i++)
        {
            if (sysCon.StarSysData.OrbitalBatteries[i].GetComponent<TextMeshProUGUI>().text == "1")
            {
                load += sysCon.StarSysData.OrbitalBatteryData.PowerLoad;
            }
        }
        for (int i = 0; i < sysCon.StarSysData.ResearchCenters.Count; i++)
        {
            if (sysCon.StarSysData.ResearchCenters[i].GetComponent<TextMeshProUGUI>().text == "1")
            {
                load += sysCon.StarSysData.ResearchCenterData.PowerLoad;
            }
        }
        sysCon.StarSysData.TotalSysPowerLoad = load;
        TextMeshProUGUI[] OneTMP = sysCon.StarSystUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < OneTMP.Length; i++)
        {
            int techLevelInt = (int)CivManager.Instance.LocalPlayerCivContoller.CivData.TechLevel / 100; // Early Tech level = 100, Supreme = 900;
            OneTMP[i].enabled = true;
            // if ()
            if ("NumP Load" == OneTMP[i].name)
            {
                OneTMP[i].text = load.ToString();
            }
        }
    }
    public void FlashPowerOverload()
    {
        StartCoroutine(FlashRoutine());
    }
    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < 3; i++)
        {
            powerOverload.SetActive(true); // Show image
            yield return new WaitForSeconds(0.5f);
            powerOverload.SetActive(false); // Hide image
            yield return new WaitForSeconds(0.5f);
        }
    }
    #endregion Star System UI

    #region Player Defined Drag Target Destination
    public void GetPlayerDefinedTargetDestination(FleetController fleetCon)
    {
        dragDestinationTargetButtonGO.SetActive(false); // to see cancel destination button
        cancelDestinationButtonGO.SetActive(true);
        selectDestinationCursorButtonGO.SetActive(true);
        //selectDestinationBttonText.text = "Select Destination";
        MouseClickSetsDestination = true;
        MousePointerChanger.Instance.ChangeToGalaxyMapCursor(fleetCon);
        MousePointerChanger.Instance.HaveGalaxyMapCursor = true;
    }
    #endregion Player Defined Drag Target Destination

    #region Diplomacy UI

    // ToDo: Build out Encounters and Diplomacy to work with traites for AI and human players
    private void MoveTheDiplomacyUIGO(GameObject fleetConGO)
    {
        for (int i = 0; i < listOfDiplomacyUiGos.Count; i++)
        {
            if (listOfDiplomacyUiGos[i] == fleetConGO)
            {
                listOfDiplomacyUiGos[i].transform.SetParent(aDiplomacyMenuView.transform, false);
                return;
            }
        }
    }
    private void MoveBackAnyDiplomacyUIGO()
    {
        for (int i = 0; i < aDiplomacyMenuView.transform.childCount; i++)
        {
            if (aDiplomacyMenuView.transform.GetChild(i).gameObject != null)
                aDiplomacyMenuView.transform.GetChild(i).gameObject.transform.SetParent(diplomacyListContainter.transform, false); ;
        }
    }
    public void SetUpDiplomacyUIData(DiplomacyController diplomacyCon)
    {
        if (diplomacyCon != null)
        {
            CivController partyOne = diplomacyCon.DiplomacyData.CivMajor;
            CivController partyTwo = diplomacyCon.DiplomacyData.CivOther;
            CivController notLocalPlayerCiv;
            CivController localPlayerCiv;
            StarSysController homeSysController;
            diplomacyCon.DiplomacyUIGameObject.SetActive(true);
            diplomacyCon.DiplomacyUIGameObject.transform.SetParent(diplomacyListContainter.transform, false);
            diplomacyControllers.Add(diplomacyCon);// add to list so GalaxyMenuUI has it
            listOfDiplomacyUiGos.Add(diplomacyCon.DiplomacyUIGameObject); // add to list of DiplomacyUI Game Objects for GalaxyMenuUI
            if (GameController.Instance.AreWeLocalPlayer(partyOne.CivData.CivEnum))
            {
                notLocalPlayerCiv = partyTwo;
                localPlayerCiv = partyOne;
                FindTheirHomeSystem(partyTwo, out homeSysController);
                //LoadCivDataInUI(ourDiplomacyController.DiplomacyData.CivOther, ourDiplomacyController);
            }
            else
            {
                notLocalPlayerCiv = partyOne;
                localPlayerCiv = partyTwo;
                FindTheirHomeSystem(partyOne, out homeSysController);
                //LoadCivDataInUI(ourDiplomacyController.DiplomacyData.CivMajor, ourDiplomacyController);
            }
            Image[] listOfImages = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Image>();
            for (int q = 0; q < listOfImages.Length; q++)
            {
                // int techLevelInt = (int)CivManager.Instance.LocalPlayerCivContoller.CivData.StartingTechLevel / 100; // Early Tech level = 100, Supreme = 900;
                bool foundRaceImage = false;
                bool foundInsigniaImage = false;
                listOfImages[q].enabled = true;
                var name = listOfImages[q].name.ToString();
                switch (name)
                {
                    case "RaceImage":
                        listOfImages[q].sprite = notLocalPlayerCiv.CivData.CivRaceSprite;
                        foundRaceImage = true;
                        break;
                    case "InsigniaTheirCiv (TMP)":
                        listOfImages[q].sprite = notLocalPlayerCiv.CivData.InsigniaSprite;
                        foundInsigniaImage = true;
                        break;
                    default:
                        break;
                }
                if (foundRaceImage && foundInsigniaImage)
                {
                    break;
                }
            }
            RectTransform[] rectTransforms = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<RectTransform>();
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                switch (rectTransforms[i].name)
                {
                    case "RedDot":
                        rectTransforms[i].gameObject.SetActive(true);
                        float x = homeSysController.StarSysData.GetPosition().x * 0.12f; // 0.12f is our cosmologic constant, fudge factor to mini map
                        float y = 0f;
                        float z = homeSysController.StarSysData.GetPosition().z * 0.12f;
                        rectTransforms[i].Translate(new Vector3(x, z, y), Space.Self); // flip z and y from main galaxy map to UI mini map
                        break;
                    case "InteractionButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        InteractionButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "TradeButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        tradeButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "EngagementButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        engagementButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "TechButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        techButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "AidButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        aidButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "AllianceButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        allianceButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "GatherIntel":
                        rectTransforms[i].gameObject.SetActive(true);
                        gatherIntelButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "Theft":
                        rectTransforms[i].gameObject.SetActive(true);
                        theftButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "Disinformation":
                        rectTransforms[i].gameObject.SetActive(true);
                        disinformationButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "SabatogeButton":
                        rectTransforms[i].gameObject.SetActive(true);
                        sabatogeButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "Combat":
                        rectTransforms[i].gameObject.SetActive(true);
                        combatButtonGO = rectTransforms[i].gameObject;
                        break;
                    case "ButtonCloseDiplomacytUI": // do we need this?
                        rectTransforms[i].gameObject.SetActive(true);
                        closeDiplomacyButtonGO = rectTransforms[i].gameObject;
                        break;
                    //case "Destination Name Text":
                    //    rectTransforms[i].gameObject.SetActive(true);
                    //    destinationName = rectTransforms[i].GetComponent<TextMeshProUGUI>();
                    //    break;

                    default:
                        break;
                }
            }
            TextMeshProUGUI[] ourTMPs = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < ourTMPs.Length; i++)
            {
                int techLevelInt = (int)CivManager.Instance.LocalPlayerCivContoller.CivData.TechLevel / 100; // Early Tech level = 100, Supreme = 900;
                ourTMPs[i].enabled = true;
                var aName = ourTMPs[i].name;

                switch (aName)
                {
                    case "ThierNameText":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.CivLongName;
                        break;
                    case "RelationText":
                        ourTMPs[i].text = diplomacyCon.DiplomacyData.DiplomacyEnumOfCivs.ToString();
                        break;
                    case "Text Points (TMP)":
                        ourTMPs[i].text = diplomacyCon.DiplomacyData.DiplomacyPointsOfCivs.ToString();
                        break;
                    case "TraitText (1)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Warlike.ToString();
                        break;
                    case "TraitText (2)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Xenophbia.ToString();
                        break;
                    case "TraitText (3)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Ruthelss.ToString();
                        break;
                    case "TraitText (4)":
                        ourTMPs[i].text = notLocalPlayerCiv.CivData.Greedy.ToString();
                        break;
                    case "OurTraitText (1)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Warlike.ToString();
                        break;
                    case "OurTraitText (2)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Xenophbia.ToString();
                        break;
                    case "OurTraitText (3)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Ruthelss.ToString();
                        break;
                    case "OurTraitText (4)":
                        ourTMPs[i].text = localPlayerCiv.CivData.Greedy.ToString();
                        break;
                    //case "FleetMaxWarpFactor":
                    //    ourTMPs[i].text = fleetCon.FleetData.MaxWarpFactor.ToString("0.0");
                    //    break;
                    default:
                        break;
                }
            }
            Button[] listButtons = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Button>();
            foreach (var listButton in listButtons)
            {
                switch (listButton.name)
                {
                    case "TradeButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.ProposeTrade(diplomacyCon));
                        break;
                    case "EngagementButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Engagement(diplomacyCon));
                        break;
                    case "TechButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.ProposeTech(diplomacyCon));
                        break;
                    case "AidButton":
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.SendAid(diplomacyCon));
                        break;
                    case "AllianceButton":
                        //fleetCon.FleetData.FleetButtonUp = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.OfferAlliance(diplomacyCon));
                        break;
                    case "GatherIntel":
                        // fleetCon.FleetData.FleetButtonDown = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.GatherIntel(diplomacyCon));
                        break;
                    case "Theft":
                        //fleetCon.FleetData.FleetButtonUIClose = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Theft(diplomacyCon));
                        break;
                    case "Disinformation":
                        //fleetCon.FleetData.FleetButtonUIClose = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Disinformation(diplomacyCon));
                        break;
                    case "Sabatoge":
                        //fleetCon.FleetData.FleetButtonUIClose = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Sabatoge(diplomacyCon));
                        break;
                    case "Combat":
                        //fleetCon.FleetData.FleetButtonUIClose = listButton;
                        listButton.onClick.RemoveAllListeners();
                        listButton.onClick.AddListener(() => diplomacyCon.Combat(diplomacyCon));
                        break;
                    default:
                        break;
                }
            }
            OpenMenu(Menu.ADiplomacyMenu, diplomacyCon.DiplomacyUIGameObject);
        }
    }
    private void FindTheirHomeSystem(CivController civCon, out StarSysController homeSysController)
    {
        homeSysController = null;
        List<StarSysController> SystemCons = civCon.CivData.StarSysOwned;
        for (int i = 0; i < SystemCons.Count; i++)
        {
            if (SystemCons[i].StarSysData.SysName == civCon.CivData.CivHomeSystemName)
            {
                homeSysController = SystemCons[i];
                return;
            }
        }
    }
    #endregion Diplomacy
}


