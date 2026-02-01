using Assets.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum GalaxyClickMode
{
    Normal,
    SetDestination,
    SelectForShipDeploy,
    SelectForShipMerge
    // Future extensions could include:
    // Ping, AttackTarget, etc. New Fleet comes from a UI button, not click mode.
}
public enum Menu
{
    None,
    SystemsMenu,
    ASystemMenu,
    BuildMenu,
    FleetMenu,
    AFleetMenu,
    ShipDeployMenu,
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
    //public GameObject ShipDeployPanelGO;
    [SerializeField]
    private FleetMenuUIController fleetMenuUIController;
    [SerializeField]
    private StarSysMenuUIController starSysMenuUIController;
    [SerializeField]
    private DiplomacyMenuUIController diplomacyMenuUIController;
    [SerializeField]
    private ShipDeployMenuUIController shipDeployMenuUIController;
    [SerializeField]
    private GameObject aSystemShipContainer;
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
        intelMenuView.SetActive(false);
        encyclopediaMenuView.SetActive(false);
        closeMenuButton.SetActive(true);
        sysBackground.SetActive(false);
        fleetsBackground.SetActive(false);
        diplomacyBackground.SetActive(false);
        intelBackground.SetActive(false);
        encyclopediaBackground.SetActive(false);
        habitableSysMenu.SetActive(false);
        saveShipDelployButton.gameObject.SetActive(true);
        saveShipDelployButton.onClick.RemoveAllListeners();
        saveShipDelployButton.onClick.AddListener(() => this.CloseButtonPressed());
        HideShipDeployMenu();
        diplomacyControllers = new List<DiplomacyController>();
        //starSysMenuUIController.SetupSystemUIData();//get our system ui game objects to match your system controllers
        // Not For DiplomacyMenuUIController here/now, we do that with each new first contact of civs / fleets
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
            }
        }
        else if (starSysLooking != null)
        {
            var aStarSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
            if (newFleet.FleetUIGameObject != null)
            {
                newFleet.FleetUIGameObject.transform.SetParent(aStarSysView.transform, false);
                newFleet.FleetUIGameObject.transform.SetAsLastSibling();
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

        // CRITICAL FIX: Ensure star system has ShipListUIParent set up
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

        shipDeployMenuUIController.gameObject.SetActive(true);
        shipDeployMenuUIController.ShowShipDeployMenuView();

        // Set up TopSlot with star system's ships
        shipDeployMenuUIController.SetUpTopShipLists(starSystCon.StarSysData.ShipsList);

        // CRITICAL FIX: Set up BottomSlot with the new fleet (currently empty, but sets BottomFleet reference)
        shipDeployMenuUIController.SetUpBottomShipLists(newFleet, true);

        var aSysView = StarSysMenuUIController.Instance.ASystemMenuView.gameObject;
        if (starSystCon.StarSysUIGameObject != null)
        {
            starSystCon.StarSysUIGameObject.transform.SetParent(aSysView.transform, false);
            starSystCon.StarSysUIGameObject.transform.SetAsLastSibling();
        }
        newFleet.FleetUIGameObject.transform.SetParent(aSysView.transform, false);

        Debug.Log($"ShowShipDeployForSystemNewFleet: TopStarSyst ShipListUIParent={(starSystCon.StarSysData?.ShipListUIParent != null ? "SET" : "NULL")}, BottomFleet ShipListUIParent={(shipDeployMenuUIController.BottomFleet?.FleetData?.ShipListUIParent != null ? "SET" : "NULL")}");
    }
    internal void ShowShipDeployForFleetNewFleet(FleetController originalFleetCon, FleetController newFleetController)
    {
        if (shipDeployMenuUIController == null) return;
        // no GalaxyClickMode. this is new fleet button click;
        Debug.Log($"ShowShipDeployForFleetNewFleet: opening deploy UI for original='{originalFleetCon?.name}' new='{newFleetController?.name}'");

        MousePointerChanger.Instance.ResetCursor();

        // CRITICAL FIX: Ensure original fleet has ShipListUIParent set up
        if (originalFleetCon.FleetData.ShipListUIParent == null)
        {
            Debug.LogWarning($"Original fleet '{originalFleetCon.name}' missing ShipListUIParent - setting it up now");

            // Get the FleetUI_Fields from the fleet's UI GameObject
            var uiFields = originalFleetCon.FleetUIGameObject?.GetComponent<FleetUI_Fields>();
            if (uiFields != null && uiFields.FleetShipContentGO != null)
            {
                originalFleetCon.FleetData.ShipListUIParent = uiFields.FleetShipContentGO;
                Debug.Log($"Set ShipListUIParent for original fleet '{originalFleetCon.name}'");
            }
            else
            {
                Debug.LogError($"Cannot find FleetShipContentGO for fleet '{originalFleetCon.name}'!");
            }
        }

        shipDeployMenuUIController.gameObject.SetActive(true);
        shipDeployMenuUIController.ShowShipDeployMenuView();

        // Set up TopSlot with original fleet's ships
        shipDeployMenuUIController.SetUpTopShipLists(originalFleetCon.FleetData.ShipsList);

        // CRITICAL FIX: Set up BottomSlot with the new fleet (currently empty, but sets BottomFleet reference)
        shipDeployMenuUIController.SetUpBottomShipLists(newFleetController, true);

        var aFleetView = FleetMenuUIController.Instance.AFleetMenuView.gameObject;
        if (originalFleetCon.FleetUIGameObject != null)
        {
            originalFleetCon.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);
            originalFleetCon.transform.SetAsLastSibling();
        }

        newFleetController.FleetUIGameObject.transform.SetParent(aFleetView.transform, false);

        Debug.Log($"ShowShipDeployForFleetNewFleet: TopFleet ShipListUIParent={(originalFleetCon.FleetData?.ShipListUIParent != null ? "SET" : "NULL")}, BottomFleet ShipListUIParent={(shipDeployMenuUIController.BottomFleet?.FleetData?.ShipListUIParent != null ? "SET" : "NULL")}");
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
    public void SystemButtonPressed()
    {
        CloseButtonPressed();
        OpenMenu(Menu.SystemsMenu, gameObject);
    }

    public void FleetButtonPressed() // The CanvasGalaxyMenuRibbon/MainGalaxyMenuPanel/FleetButton in the Hierarchy is set to this class.method
    {
        CloseButtonPressed();
        OpenMenu(Menu.FleetMenu, gameObject);
    }
    public void DiplomacyButtonPressed()
    {
        CloseButtonPressed();
        OpenMenu(Menu.DiplomacyMenu, gameObject);
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

    // jump to Home System is in GalaxyCameraDragMoveZoom.cs
    public void CloseButtonPressed()
    {
        if (ShipDeployMenuUIController.Instance != null && ShipDeployMenuUIController.Instance.ShipDeployPanel.activeInHierarchy)
        {
            // Commit the slot state while the slots are still active
            ShipDeployMenuUIController.Instance.CommitShipDeployAndClose();

            // After commit, proceed with the normal close flow (UI move/hide)
            StarSysMenuUIController.Instance.ClickCancelShipManageButton();
            FleetMenuUIController.Instance.ClickCancelShipManageButton();
            CloseMenu(Menu.ShipDeployMenu);
        }
        else
        {
            // No ship-deploy active — normal flow: move UIs back to their original parents.
            FleetMenuUIController.Instance.MoveBackAnyaFleetUIGO();
            StarSysMenuUIController.Instance.MoveBackAnyaSysUIGO();
        }

        HideShipDeployMenu();
        GalaxyMenuUIController.Instance.SetClickMode(GalaxyClickMode.Normal);
        if (diplomacyMenuUIController.IsVisibleA_DiplomacyMenuView || diplomacyMenuUIController.IsVisibleDiplomacyMenuView)
            TimeManager.Instance.ResumeTime();
        if (encyclopediaMenuView.activeSelf)
            CloseMenu(Menu.EncyclopedianMenu);
        if (intelMenuView.activeSelf)
            CloseMenu(Menu.IntellMenu);
        diplomacyMenuUIController.HideDiplomacyMenuView();
        CloseMenu(Menu.DiplomacyMenu);
        diplomacyNoContacts.SetActive(false);
        diplomacyMenuUIController.HideA_DiplomacyMenuView();
        CloseMenu(Menu.ADiplomacyMenu);

        fleetMenuUIController.HideFleetMenuView();
        CloseMenu(Menu.FleetMenu);

        fleetMenuUIController.HideA_FleetMenuView();
        CloseMenu(Menu.AFleetMenu);

        starSysMenuUIController.HideSystemMenuView();
        CloseMenu(Menu.SystemsMenu);

        starSysMenuUIController.HideA_SystemMenuView();
        CloseMenu(Menu.ASystemMenu);
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
                starSysMenuUIController.MoveBackAnyaSysUIGO();
                openMenuWas = null;
                openMenuEnumWas = Menu.SystemsMenu;
                break;
            case Menu.ASystemMenu:
                HideShipDeployMenu();
                starSysMenuUIController.ShowA_SystemMenuView();
                CloseTheBackgrounds();
                starSysMenuUIController.SetActiveSetParentUIGO(callingMenuOrGalaxyObject.GetComponentInChildren<StarSysController>());
                sysBackground.SetActive(true);
                starSysMenuUIController.MoveTheSysUIGO(callingMenuOrGalaxyObject);
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
                fleetMenuUIController.ShowA_FleetMenuView();
                CloseTheBackgrounds();
                fleetMenuUIController.SetActiveSetParentUIGO(callingMenuOrGalaxyObject.GetComponentInChildren<FleetController>());
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
        if (FleetLookingForShipDeploy != null)
        {
            GameObject fleetShipListParentGO = FleetLookingForShipDeploy.FleetData.ShipListUIParent;
            var shipControllers = FleetLookingForShipDeploy.FleetData.ShipsList;
            var shipUIGOs = ShipDeployMenuUIController.Instance.GetTopSlotShipListUIGOs().ToList();
            for (int i = 0; i < shipUIGOs.Count; i++)
            {
                shipUIGOs[i].transform.SetParent(fleetShipListParentGO.transform, false);
            }
            for (int i = 0; i < shipControllers.Count; i++)
            {
                //shipControllers[i].ShipListUIGameObject.transform.SetParent(fleetShipListParentGO.transform, false);
            }
        }
        else if (StarSystLookingForShipDeploy != null)
        {
            GameObject starSysShipListParentGO = StarSystLookingForShipDeploy.StarSysData.ShipListUIParent;
            var shipUIGOs = ShipDeployMenuUIController.Instance.GetTopSlotShipListUIGOs().ToList();
            for (int i = 0; i < shipUIGOs.Count; i++)
            {
                shipUIGOs[i].transform.SetParent(starSysShipListParentGO.transform, false);
            }
        }
        if (FleetSelectedForShipDeploy != null)
        {
            GameObject fleetShipListParentGO = FleetSelectedForShipDeploy.FleetData.ShipListUIParent;
            var shipUIGOs = ShipDeployMenuUIController.Instance.GetBottomSlotShipListUIGOs().ToList();
            for (int i = 0; i < shipUIGOs.Count; i++)
            {
                shipUIGOs[i].transform.SetParent(fleetShipListParentGO.transform, false);
            }
        }
        else if (StarSystSelectedForShipDeploy != null)
        {
            GameObject starSysShipListParentGO = StarSystSelectedForShipDeploy.StarSysData.ShipListUIParent;
            var shipUIGOs = ShipDeployMenuUIController.Instance.GetBottomSlotShipListUIGOs().ToList();
            for (int i = 0; i < shipUIGOs.Count; i++)
            {
                shipUIGOs[i].transform.SetParent(starSysShipListParentGO.transform, false);
            }
        }
    }

    private void InactivateCallingMenu(GameObject callingMenu)
    {
        if (callingMenu != null)
            callingMenu.SetActive(false);
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
                openMenuWas = null;
                break;
            case Menu.ASystemMenu:
                starSysMenuUIController.MoveBackAnyaSysUIGO();
                sysBackground.SetActive(false);
                openMenuWas = null;
                break;
            case Menu.BuildMenu:
                sysBuildMenu.SetActive(false);
                openMenuWas = sysBuildMenu;
                break;
            case Menu.FleetMenu:
                fleetsBackground.SetActive(false);
                fleetMenuUIController.CloseDestinationSelectionCursor();
                openMenuWas = null;
                break;
            case Menu.AFleetMenu:
                fleetMenuUIController.MoveBackAnyaFleetUIGO();
                fleetsBackground.SetActive(false);
                fleetMenuUIController.CloseDestinationSelectionCursor();
                openMenuWas = null;
                break;
            case Menu.ShipDeployMenu:
                MoveBackShipUIGO();
                shipDeployMenuUIController.HideShipDeployMenuView();
                starSysMenuUIController.MoveBackAnyaSysUIGO();
                fleetMenuUIController.MoveBackAnyaFleetUIGO();
                openMenuWas = shipDeployMenuUIController.gameObject;
                break;
            case Menu.DiplomacyMenu:
                diplomacyBackground.SetActive(false);
                TimeManager.Instance.ResumeTime();
                openMenuWas = null;
                break;
            case Menu.ADiplomacyMenu:
                diplomacyMenuUIController.MoveBackAnyDiplomacyUIGO();
                TimeManager.Instance.ResumeTime();
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
        FleetLookingForDestination = fleetLooking;
        SetClickMode(GalaxyClickMode.SelectForShipDeploy);
    }

    public void CompleteSetDestination()
    {
        FleetLookingForDestination = null;
        ResetClickMode();
    }


    private void CurrentClickModeReset()
    {
        GalaxyMenuUIController.Instance.ResetClickMode();
    }


}
