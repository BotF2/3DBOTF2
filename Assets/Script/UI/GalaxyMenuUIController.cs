using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Core;
using System;
using System.Linq;

public enum GalaxyClickMode
{
    Normal,
    SetDestination,
    SelectForShipExchange
    // Future extensions could include:
    // Ping, AttackTarget, MergeFleet, etc.
}
public enum Menu
{
    None,
    SystemsMenu,
    ASystemMenu,
    BuildMenu,
    FleetMenu,
    AFleetMenu,
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
    public static GalaxyMenuUIController Instance;
    private Camera galaxyEventCamera;
    [SerializeField]
    private Canvas parentCanvas;
    [SerializeField]
    private FleetMenuUIController fleetMenuUIController;
    [SerializeField]
    private StarSysMenuUIController starSysMenuUIController;
    [SerializeField]
    private GameObject buildListUI;
    [SerializeField]
    private GameObject systemsMenuView;
    [SerializeField]
    private GameObject sysListContainer;
    [SerializeField]
    private GameObject sysShipListContainer;
    [SerializeField]
    private GameObject aSystemMenuView;
    [SerializeField]
    private GameObject aSystemShipContainer;
    [SerializeField]
    private GameObject sysBuildMenu;
    [SerializeField]
    private RectTransform fleetListContainer;
    [SerializeField]
    private GameObject fleetShipListContainer;
    [SerializeField]
    private GameObject aFleetShipContainer;
    [SerializeField]
    private GameObject manageFleetShipsMenu;
    [SerializeField]
    private GameObject diplomacyMenuView;
    [SerializeField]
    private GameObject diplomacyNoContacts;
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
    private GameObject openMenuWas;
    [SerializeField]
    private Menu openMenuEnumWas;
    [SerializeField]
    private GameObject shipListingUIPrefab;
    [SerializeField]
    private List<ShipData> shipList;
    private bool deltaShipList = false;
    [SerializeField]
    private GameObject selectDestinationCursorButtonGO;
    [SerializeField]
    private GameObject cancelDestinationButtonGO;
    [SerializeField]
    private GameObject selectOtherSysOrFleetButtonGO;
    [SerializeField]
    private GameObject dragDestinationTargetButtonGO;
    public GalaxyClickMode CurrentClickMode { get; private set; } = GalaxyClickMode.Normal;
    public FleetController FleetLookingForDestination { get; set; }
    public StarSysController SystemLookingForDestination { get; private set; }

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
    int _scouts;
    int _destroyers;
    int _cruisters;
    int _ltCruisers;
    int _hvyCruisers;
    int _transports;
    [Header("Grid Settings")]
    public int rows = 100;
    public int cols = 2;
    public Vector2 cellSize = new Vector2(948, 200);
    public Vector2 spacing = new Vector2(1, 1);
    public Vector2 padding = new Vector2(1,1);
    private GameObject[,] gridItems;

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
        manageFleetShipsMenu.SetActive(false);
        diplomacyMenuView.SetActive(false);
        aDiplomacyMenuView.SetActive(false);
        intelMenuView.SetActive(false);
        encyclopediaMenuView.SetActive(false);
        closeMenuButton.SetActive(true);
        sysBackground.SetActive(false);
        fleetsBackground.SetActive(false);
        diplomacyBackground.SetActive(false);
        intelBackground.SetActive(false);
        encyclopediaBackground.SetActive(false);
        gridItems = new GameObject[rows, cols];
        // Add or get GridLayoutGroup
        GridLayoutGroup grid = fleetListContainer.gameObject.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = fleetListContainer.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4; // Number of columns

        //// Populate grid
        //for (int i = 0; i < numberOfItems; i++)
        //{
        //    GameObject newItem = Instantiate(itemPrefab, container);
        //    newItem.name = $"Item {i}";
        //}

        //// Optional: Resize container height for scrolling
        //int rows = Mathf.CeilToInt((float)numberOfItems / grid.constraintCount);
        //container.sizeDelta = new Vector2(container.sizeDelta.x, height);

        // Calculate container size
        float width = cols * (cellSize.x + spacing.x) + padding.x;
        float height = rows * (cellSize.y + spacing.y) + padding.y;
        fleetListContainer.sizeDelta = new Vector2(width, height);

        diplomacyControllers = new List<DiplomacyController>();
        StarSysMenuUIController.Instance.SetupSystemUIData();//get our system ui game objects to match your system controllers
        FleetMenuUIController.Instance.SetupFleetUIData();//get our fleet ui game objects to match your fleet controllers
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
    public void SystemButtonPressed()
    {
        CloseButtonPressed();
        if (starSysMenuUIController.IsVisibleSystemMenuView)
            starSysMenuUIController.HideSystemMenuView();
        else if (starSysMenuUIController.IsVisibleA_SystemMenuView)
            starSysMenuUIController.HideA_SystemMenuView();
        else
        { 
            OpenMenu(Menu.SystemsMenu, null);
            starSysMenuUIController.ShowSystemMenuView();
        }
    }
 
    public void FleetButtonPressed() // The CanvasGalaxyMenuRibbon/MainGalaxyMenuPanel/FleetButton in the Hierarchy is set to this class.method
    {
        CloseButtonPressed();
        if (fleetMenuUIController.IsVisibleFleetMenuView)
            fleetMenuUIController.HideFleetMenuView();
        else if (fleetMenuUIController.IsVisibleA_FleetMenuView)
            fleetMenuUIController.HideA_FleetMenuView();
        else
        {
            OpenMenu(Menu.FleetMenu, gameObject);
            fleetMenuUIController.ShowFleetMenuView();
        }

    }
    public void DiplomacyButtonPressed()
    {
        CloseButtonPressed();
        if (diplomacyMenuView.activeSelf)
            CloseMenu(Menu.DiplomacyMenu);
        else if (aDiplomacyMenuView.activeSelf)
            CloseMenu(Menu.ADiplomacyMenu);
        else
        {
            OpenMenu(Menu.DiplomacyMenu, null);
        }

    }
    public void IntelButtonPressed()
    {
        CloseButtonPressed();
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
        CloseButtonPressed();
        if (encyclopediaMenuView.activeSelf)
            CloseMenu(Menu.EncyclopedianMenu);
        else
        {
            OpenMenu(Menu.EncyclopedianMenu, null);
        }

    }
    // Home System view is in GalaxyCameraDragMoveZoom.cs
    public void CloseButtonPressed()
    {
        if (encyclopediaMenuView.activeSelf)
            CloseMenu(Menu.EncyclopedianMenu);
        if (intelMenuView.activeSelf)
            CloseMenu(Menu.IntellMenu);
        if (diplomacyMenuView.activeSelf)
            CloseMenu(Menu.DiplomacyMenu);
        if (aDiplomacyMenuView.activeSelf)
            CloseMenu(Menu.ADiplomacyMenu);
        if (fleetMenuUIController.IsVisibleFleetMenuView)
        {
            fleetMenuUIController.HideFleetMenuView();
            CloseMenu(Menu.FleetMenu);
        }
        else if (fleetMenuUIController.IsVisibleA_FleetMenuView)
        {
            fleetMenuUIController.HideA_FleetMenuView();
            CloseMenu(Menu.AFleetMenu);
        }
        if (starSysMenuUIController.IsVisibleSystemMenuView)
        {
            starSysMenuUIController.HideSystemMenuView();
            CloseMenu(Menu.SystemsMenu);
        }  
        else if (starSysMenuUIController.IsVisibleA_SystemMenuView)
        {
            starSysMenuUIController.HideA_SystemMenuView();
            CloseMenu(Menu.ASystemMenu);
        }
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
                CloseTheBackgrounds();
                starSysMenuUIController.MoveBackAnySysUIGO();
                starSysMenuUIController.ShowSystemMenuView();
                sysBackground.SetActive(true);
                openMenuWas = null;
                openMenuEnumWas = Menu.SystemsMenu;
                break;
            case Menu.ASystemMenu:
                CloseTheBackgrounds();
                starSysMenuUIController.SetUpASystemUIData(callingMenuOrGalaxyObject.GetComponentInChildren<StarSysController>());
                starSysMenuUIController.ShowA_SystemMenuView();
                sysBackground.SetActive(true);
                starSysMenuUIController.MoveTheSysUIGO(callingMenuOrGalaxyObject);
                openMenuWas = null;
                openMenuEnumWas = Menu.ASystemMenu;
                break;
            case Menu.BuildMenu:
                InactivateCallingMenu(callingMenuOrGalaxyObject);
                sysBuildMenu.SetActive(true);
                openMenuWas = sysBuildMenu;
                openMenuEnumWas = Menu.BuildMenu;
                break;
            case Menu.FleetMenu:
                CloseTheBackgrounds();
                fleetMenuUIController.MoveBackAnyFleetUIGO();
                fleetMenuUIController.ShowFleetMenuView();
                fleetsBackground.SetActive(true);
                openMenuWas = null;
                openMenuEnumWas = Menu.FleetMenu;
                break;
            case Menu.AFleetMenu:
                CloseTheBackgrounds();
                fleetMenuUIController.SetUpAFleetUIData(callingMenuOrGalaxyObject.GetComponentInChildren<FleetController>());
                fleetMenuUIController.ShowA_FleetMenuView();
                fleetsBackground.SetActive(true);
                fleetMenuUIController.MoveTheFleetUIGO(callingMenuOrGalaxyObject);
                openMenuWas = null;
                openMenuEnumWas = Menu.AFleetMenu;
                break;
            case Menu.DiplomacyMenu:
                CloseTheBackgrounds();
                TimeManager.Instance.PauseTime();
                diplomacyMenuView.gameObject.SetActive(true);
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
                encyclopediaBackground.SetActive(true);
                openMenuWas = encyclopediaMenuView;
                openMenuEnumWas = Menu.EncyclopedianMenu;
                break;
            case Menu.HabitableSysMenu:
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

    private void InactivateCallingMenu(GameObject callingMenu)
    {
        if (callingMenu != null)
            callingMenu.SetActive(false);
    }

    public void SetUpASystemRightSideShipsUIData(StarSysController theSysCon) 
    {
        theSysCon.StarSysRightSideShipsUIGameObject.SetActive(true);
        theSysCon.StarSysRightSideShipsUIGameObject.transform.SetParent(fleetMenuUIController.AFleetMenuView.transform, false);
        theSysCon.StarSysRightSideShipsUIGameObject.transform.Translate(new Vector3(0f, 0f, 0f), Space.Self);
    }
    public void CloseSystemShipsUI(StarSysController theSysCon) 
    {
        theSysCon.StarSysRightSideShipsUIGameObject.SetActive(false);
        CloseSystemShipsUI(theSysCon);
        //activeFleetOrSystemControllerForShipExchange = null;

    }
    private void SetUpASystemUIData(StarSysController theSysCon) // now system ui opens a single system view when our system is clicked on galaxy map
    {
        theSysCon.StarSysUIGameObject.SetActive(true);
        theSysCon.StarSysUIGameObject.transform.SetParent(aSystemMenuView.transform, false);
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
                //systemsMenuView.SetActive(false);
                openMenuWas = systemsMenuView;
                break;
            case Menu.ASystemMenu:
                starSysMenuUIController.MoveBackAnySysUIGO();
                sysBackground.SetActive(false);
                //aSystemMenuView.SetActive(false);
                openMenuWas = aSystemMenuView;
                break;
            case Menu.BuildMenu:
                sysBuildMenu.SetActive(false);
                openMenuWas = sysBuildMenu;
                break;
            case Menu.FleetMenu:
                fleetMenuUIController.CloseDestinationSelectionCursor();
                fleetsBackground.SetActive(false);
                fleetMenuUIController.HideFleetMenuView();
                openMenuWas = null;
                break;
            case Menu.AFleetMenu:
                fleetMenuUIController.MoveBackAnyFleetUIGO();
                fleetsBackground.SetActive(false);
                fleetMenuUIController.CloseDestinationSelectionCursor();
                fleetMenuUIController.HideA_FleetMenuView();
                //aFleetMenuView.SetActive(false);
                openMenuWas = null;
                break;
            case Menu.DiplomacyMenu:
                diplomacyBackground.SetActive(false);
                diplomacyMenuView.SetActive(false);
                // TimeManager.Instance.ResumeTime();
                openMenuWas = diplomacyMenuView;
                break;
            case Menu.ADiplomacyMenu:
                MoveBackAnyDiplomacyUIGO();
                diplomacyBackground.SetActive(false);
                aDiplomacyMenuView.SetActive(false);
                //TimeManager.Instance.ResumeTime();
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
                // TimeManager.Instance.ResumeTime();
                habitableSysMenu.SetActive(false);
                openMenuWas = habitableSysMenu;
                break;
            case Menu.Combat:// close combat scenes
                break;
            default:
                break;
        }
    }

    #region Player Defined Drag Target Destination
    public void GetPlayerDefinedTargetDestination(FleetController fleetCon)
    {
        dragDestinationTargetButtonGO.SetActive(false); // to see cancel destination button
        cancelDestinationButtonGO.SetActive(true);
        selectDestinationCursorButtonGO.SetActive(true);
        //selectDestinationBttonText.text = "Select Destination";
        CurrentClickMode = GalaxyClickMode.SetDestination;
        MousePointerChanger.Instance.SetDestinationCursor();//ChangeToGalaxyMapCursorForLocalPlayer(fleetCon);
        //MousePointerChanger.Instance.HaveGalaxyMapCursor = true;
    }
    #endregion Player Defined Drag Target Destination

    #region Diplomacy UI

    // ToDo: Build out Diplomacy to work with traits for AI and human players
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
    public void OpenADiplomacyUI(DiplomacyController diplomacyCon, List<ShipController> shipList)
    {
        HideNoContactUI();
        CivController partyOne = CivManager.Instance.GetCivControllerByCivEnum(diplomacyCon.DiplomacyData.CivSideOne);
        CivController partyTwo = CivManager.Instance.GetCivControllerByCivEnum(diplomacyCon.DiplomacyData.CivSideTwo);
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
                case "CombatButton":
                    rectTransforms[i].gameObject.SetActive(true);
                    combatButtonGO = rectTransforms[i].gameObject;
                    break;
                case "ButtonCloseDiplomacytUI":
                    rectTransforms[i].gameObject.SetActive(true);
                    closeDiplomacyButtonGO = rectTransforms[i].gameObject;
                    break;
                default:
                    break;
            }
        }
        TextMeshProUGUI[] ourTMPs = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < ourTMPs.Length; i++)
        {
            int techLevelInt = (int)notLocalPlayerCiv.CivData.TechLevel / 100; // Early Tech level = 100, Supreme = 900;
            ourTMPs[i].enabled = true;
            var aName = ourTMPs[i].name;
            var sysCiv = homeSysController.StarSysData.CurrentOwnerCivEnum;
            string nameOfWhatWeSee = notLocalPlayerCiv.CivData.CivShortName;
            CountShips(shipList);
            //   ourTMPs[i].text = "No Intel";
            //    continue;
            switch (aName)
            {
                case "ThierNameText":
                    ourTMPs[i].text = notLocalPlayerCiv.CivData.CivLongName;
                    break;
                case "RelationText":
                    ourTMPs[i].text = diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs.ToString();
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
                case "WhoWeHit":
                    ourTMPs[i].text = nameOfWhatWeSee;
                    break;
                case "NumS":
                    ourTMPs[i].text = _scouts.ToString();
                    break;
                case "NumD":
                    ourTMPs[i].text = _destroyers.ToString();
                    break;
                case "NumC":
                    ourTMPs[i].text = _cruisters.ToString();
                    break;
                case "NumLC":
                    ourTMPs[i].text = _ltCruisers.ToString();
                    break;
                case "NumHC":
                    ourTMPs[i].text = _hvyCruisers.ToString();
                    break;
                case "NumT":
                    ourTMPs[i].text = _transports.ToString();
                    break;
                default:
                    break;
            }
        }
        Button[] listButtons = diplomacyCon.DiplomacyUIGameObject.GetComponentsInChildren<Button>();
        foreach (var listButton in listButtons)
        {
            switch (listButton.name)
            {
                case "InteractionButton":
                    listButton.onClick.RemoveAllListeners();
                    listButton.onClick.AddListener(() => diplomacyCon.ProposeTrade(diplomacyCon));
                    break;
                case "OpenDiscriptionButton":
                    listButton.onClick.RemoveAllListeners();
                    listButton.onClick.AddListener(() => diplomacyCon.ProposeTrade(diplomacyCon));
                    break;
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
                case "GatherIntelButton":
                    // fleetCon.FleetData.FleetButtonDown = listButton;
                    listButton.onClick.RemoveAllListeners();
                    listButton.onClick.AddListener(() => diplomacyCon.GatherIntel(diplomacyCon));
                    break;
                case "TheftButton":
                    //fleetCon.FleetData.FleetButtonUIClose = listButton;
                    listButton.onClick.RemoveAllListeners();
                    listButton.onClick.AddListener(() => diplomacyCon.Theft(diplomacyCon));
                    break;
                case "DisinformationButton":
                    //fleetCon.FleetData.FleetButtonUIClose = listButton;
                    listButton.onClick.RemoveAllListeners();
                    listButton.onClick.AddListener(() => diplomacyCon.Disinformation(diplomacyCon));
                    break;
                case "SabatogeButton":
                    //fleetCon.FleetData.FleetButtonUIClose = listButton;
                    listButton.onClick.RemoveAllListeners();
                    listButton.onClick.AddListener(() => diplomacyCon.Sabatoge(diplomacyCon));
                    break;
                case "CombatButton":
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
    private void CountShips(List<ShipController> ships)
    {
        _scouts = ships.Count(s => s.ShipData.ShipType == ShipType.Scout);
        _destroyers = ships.Count(s => s.ShipData.ShipType == ShipType.Destroyer);
        _cruisters = ships.Count(s => s.ShipData.ShipType == ShipType.Cruiser);
        _ltCruisers = ships.Count(s => s.ShipData.ShipType == ShipType.LtCruiser);
        _hvyCruisers = ships.Count(s => s.ShipData.ShipType == ShipType.HvyCruiser);
        _cruisters = ships.Count(s => s.ShipData.ShipType == ShipType.Transport);
    }

    internal void HideNoContactUI()
    {
        diplomacyNoContacts.SetActive(false);
    }
    #endregion Diplomacy

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

            case GalaxyClickMode.SelectForShipExchange:
                MousePointerChanger.Instance.SetShipExchangeCursor();
                break;
        }
    }

    public void ClickCancelShipManageButton()
    {
        MousePointerChanger.Instance.ResetCursor();
        CurrentClickMode = GalaxyClickMode.Normal;
        selectOtherSysOrFleetButtonGO.SetActive(true);
    }
    public void InactivateSelectOtherSystemOrFleetButton()
    {
        selectOtherSysOrFleetButtonGO.SetActive(false);
    }
    public void BeginShipExchange(FleetController fleetConLooking)
    {
        FleetLookingForDestination = fleetConLooking;
        SetClickMode(GalaxyClickMode.SelectForShipExchange);
    }
    public void BeginShipExchange(StarSysController starSysConLooking)
    {
        SystemLookingForDestination = starSysConLooking;
        SetClickMode(GalaxyClickMode.SelectForShipExchange);
    }
    public void CompleteShipExchange()
    {
        FleetLookingForDestination = null;
        SystemLookingForDestination = null;
        ResetClickMode();
    }

    public void BeginSetDestination(FleetController fleetLooking)
    {
        FleetLookingForDestination = fleetLooking;
        SetClickMode(GalaxyClickMode.SelectForShipExchange);
    }

    public void CompleteSetDestination()
    {
        FleetLookingForDestination = null;
        ResetClickMode();
    }
    private void HandleShipExchange(MonoBehaviour origin, MonoBehaviour target)
    {
        // Cast to the appropriate type to handle logic
        var originFleet = origin as FleetController;
        var originSystem = origin as StarSysController;

        var targetFleet = target as FleetController;
        var targetSystem = target as StarSysController;

        // ✅ Examples of how you might handle this:
        if (originFleet != null && targetFleet != null)
        {
            Debug.Log($"Exchange ships between fleet {originFleet.name} and fleet {targetFleet.name}");
            // ExchangeFleetToFleet(originFleet, targetFleet);
        }
        else if (originFleet != null && targetSystem != null)
        {
            Debug.Log($"Exchange ships between fleet {originFleet.name} and system {targetSystem.name}");
            // ExchangeFleetToSystem(originFleet, targetSystem);
        }
        else if (originSystem != null && targetFleet != null)
        {
            Debug.Log($"Exchange ships between system {originSystem.name} and fleet {targetFleet.name}");
            // ExchangeSystemToFleet(originSystem, targetFleet);
        }
    }

    private void SetupFleetElements(FleetController fleetCon, GameObject fleetPrefabGO)
    {
        RectTransform[] rectTransforms = fleetPrefabGO.GetComponentsInChildren<RectTransform>();
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            switch (rectTransforms[i].name)
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

        // TMP bindings / text updates (kept minimal)
        TextMeshProUGUI[] ourTMPs = fleetCon.FleetUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < ourTMPs.Length; i++)
        {
            var name = ourTMPs[i].name;
            switch (name)
            {
                case "Text FleetName (TMP)":
                    ourTMPs[i].text = fleetCon.FleetData.Name;
                    break;
                case "Destination Name Text":
                    ourTMPs[i].text = "No Destination";
                    break;
                case "Warp Value Text (TMP)":
                    ourTMPs[i].text = fleetCon.FleetData.CurrentWarpFactor.ToString("0.0");
                    break;
            }
        }

        // slider wiring
        Slider slider = fleetCon.FleetUIGameObject.GetComponentInChildren<Slider>();
        if (slider != null)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.value = fleetCon.FleetData.CurrentWarpFactor;
            slider.maxValue = fleetCon.FleetData.MaxWarpFactor;
            slider.onValueChanged.AddListener((value) => fleetCon.SliderOnValueChange(value));
        }

        // Buttons wiring (partial)
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
                    listButton.onClick.AddListener(() => fleetCon.ClickCancelDestinationButton());
                    break;
                case "DestinationDragTarget Button":
                    listButton.onClick.RemoveAllListeners();
                    listButton.onClick.AddListener(() => fleetCon.GetPlayerDefinedTargetDestination(fleetCon));
                    break;
            }
        }

        // Attach existing ship list UIs
        for (int i = 0; i < fleetCon.FleetData.ShipsList.Count; i++)
        {
            if (fleetCon.FleetData.ShipsList[i].ShipListUIGameObject != null)
            {
                var transforms = fleetCon.FleetUIGameObject.transform.GetComponentsInChildren<Transform>();
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

    private void CurrentClickModeReset()
    {
        GalaxyMenuUIController.Instance.ResetClickMode();
    }
}



