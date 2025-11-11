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
    public FleetController FleetLookingForShipExchange { get; set; }
    public StarSysController StarSysLookingForShipExchange{ get; set; }
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
    int _scouts;
    int _destroyers;
    int _cruisters;
    int _ltCruisers;
    int _hvyCruisers;
    int _transports;


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
        shipDeployMenuUIController.HideShipDeployMenuView();
        diplomacyControllers = new List<DiplomacyController>();
        starSysMenuUIController.SetupSystemUIData();//get our system ui game objects to match your system controllers
        fleetMenuUIController.SetupFleetUIData();//get our fleet ui game objects to match your fleet controllers
        // Not For DiplomacyMenuUIController, we do that with each new first contact of civs / fleets
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
    // Home System view is in GalaxyCameraDragMoveZoom.cs
    public void CloseButtonPressed()
    {
        FleetMenuUIController.Instance.MoveBackAnyaFleetUIGO();
        StarSysMenuUIController.Instance.MoveBackAnyaSysUIGO();
        shipDeployMenuUIController.gameObject.SetActive(false);
        shipDeployMenuUIController.HideShipDeployMenuView();
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
                shipDeployMenuUIController.HideShipDeployMenuView();
                starSysMenuUIController.ShowSystemMenuView();
                CloseTheBackgrounds();
                sysBackground.SetActive(true);
                starSysMenuUIController.MoveBackAnyaSysUIGO();
                openMenuWas = null;
                openMenuEnumWas = Menu.SystemsMenu;
                break;
            case Menu.ASystemMenu:
                shipDeployMenuUIController.HideShipDeployMenuView();
                starSysMenuUIController.ShowA_SystemMenuView();
                CloseTheBackgrounds();
                starSysMenuUIController.SetActiveSetParentUIGO(callingMenuOrGalaxyObject.GetComponentInChildren<StarSysController>());
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
                shipDeployMenuUIController.HideShipDeployMenuView();
                fleetMenuUIController.ShowFleetMenuView();
                CloseTheBackgrounds();
                fleetsBackground.SetActive(true);
                fleetMenuUIController.MoveBackAnyaFleetUIGO();
                openMenuWas = null;
                openMenuEnumWas = Menu.FleetMenu;
                break;
            case Menu.AFleetMenu:
                shipDeployMenuUIController.HideShipDeployMenuView();
                fleetMenuUIController.ShowA_FleetMenuView();
                CloseTheBackgrounds();
                fleetMenuUIController.SetActiveSetParentUIGO(callingMenuOrGalaxyObject.GetComponentInChildren<FleetController>());
                fleetsBackground.SetActive(true);
                fleetMenuUIController.MoveTheFleetUIGO(callingMenuOrGalaxyObject);
                openMenuWas = null;
                openMenuEnumWas = Menu.AFleetMenu;
                break;
            case Menu.DiplomacyMenu:
                shipDeployMenuUIController.HideShipDeployMenuView();
                diplomacyMenuUIController.ShowDiplomacyMenuView();
                CloseTheBackgrounds();
                diplomacyBackground.SetActive(true);
                TimeManager.Instance.PauseTime();
                diplomacyMenuUIController.MoveBackAnyDiplomacyUIGO();
                openMenuWas = null;
                openMenuEnumWas = Menu.DiplomacyMenu;
                break;
            case Menu.ADiplomacyMenu:
                shipDeployMenuUIController.HideShipDeployMenuView();
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
                shipDeployMenuUIController.HideShipDeployMenuView();
                CloseTheBackgrounds();
                intelMenuView.SetActive(true);
                intelBackground.SetActive(true);
                openMenuWas = intelMenuView;
                openMenuEnumWas = Menu.IntellMenu;
                break;
            case Menu.EncyclopedianMenu:
                shipDeployMenuUIController.HideShipDeployMenuView();
                CloseTheBackgrounds();
                InactivateCallingMenu(callingMenuOrGalaxyObject);
                encyclopediaMenuView.SetActive(true);
                encyclopediaBackground.SetActive(true);
                openMenuWas = encyclopediaMenuView;
                openMenuEnumWas = Menu.EncyclopedianMenu;
                break;
            case Menu.HabitableSysMenu:
                shipDeployMenuUIController.HideShipDeployMenuView();
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

    //public void SetUpASystemRightSideShipsUIData(StarSysController theSysCon) 
    //{
    //    theSysCon.StarSysShipsUIGameObject.SetActive(true);
    //    theSysCon.StarSysShipsUIGameObject.transform.SetParent(fleetMenuUIController.AFleetMenuView.transform, false);
    //    theSysCon.StarSysShipsUIGameObject.transform.Translate(new Vector3(0f, 0f, 0f), Space.Self);
    //}
    public void CloseSystemShipsUI(StarSysController theSysCon) 
    {
        theSysCon.StarSysShipsUIGameObject.SetActive(false);
        CloseSystemShipsUI(theSysCon);
        //activeFleetOrSystemControllerForShipExchange = null;
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

    public void FindTheirHomeSystem(CivController civCon, out StarSysController homeSysController)
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

            case GalaxyClickMode.SelectForShipExchange:
                MousePointerChanger.Instance.SetShipExchangeCursor();
                break;
        }
    }

    public void ClickCancelShipManageButton() // button is both in fleet and system UI
    {
        MousePointerChanger.Instance.ResetCursor();
        CurrentClickMode = GalaxyClickMode.Normal;
       // sele.SetActive(true);
    }

    public void WhatFleetIsLookingForShips(FleetController fleetConLooking)
    {
        FleetLookingForShipExchange = fleetConLooking;
        StarSysLookingForShipExchange = null;
        SetClickMode(GalaxyClickMode.SelectForShipExchange);
    }
    public void WhatSysIsLookingForShips(StarSysController starSysConLooking)
    {
        StarSysLookingForShipExchange = starSysConLooking;
        FleetLookingForShipExchange = null;
        SetClickMode(GalaxyClickMode.SelectForShipExchange);
    }
    public void CompleteShipExchange()
    {
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


    private void CurrentClickModeReset()
    {
        GalaxyMenuUIController.Instance.ResetClickMode();
    }

}




