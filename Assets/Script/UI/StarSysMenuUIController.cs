// Ignore Spelling: Sys

using Assets.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// The UI controller owns hierarchy and presentation.
/// </summary>
public class StarSysMenuUIController : MonoBehaviour
{
    public static StarSysMenuUIController Instance;
    //private Camera galaxyEventCamera;
    //[SerializeField]
    //private Canvas parentCanvas;
    private StarSysController lastSysCon;
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
    [SerializeField] private GameObject powerOverload;
    public GameObject PowerOverloadImage;
    //[SerializeField] private CoroutineRunner coroutineRunner;
    public Slider ShipSliderBuildProgress;
    public Slider SliderBuildProgress;

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
        CoroutineRunner.FlashPowerOverload();
        // Record the original parent of each StarSysUIGameObject as its current parent (or fall back to SysListContainer / ASystemMenuView).
        for (int i = 0; i < StarSysManager.Instance.StarSysControllerList.Count; i++)
        {
            var sysCon = StarSysManager.Instance.StarSysControllerList[i];
            if (sysCon != null && sysCon.StarSysUIGameObject != null)
            {
                var child = sysCon.StarSysUIGameObject;
                var childController = child.GetComponent<FleetAndSystemChildController>();
                if (childController != null && childController.OriginalParentTransform == null)
                {
                    // Prefer the current hierarchy parent first
                    if (child.transform.parent != null)
                    {
                        childController.OriginalParentTransform = child.transform.parent;
                    }
                    // Next prefer the SysListContainer if available
                    else if (SysListContainer != null)
                    {
                        childController.OriginalParentTransform = SysListContainer.transform;
                    }
                    // Last resort: ASystemMenuView (preserve existing behavior if nothing else)
                    else if (ASystemMenuView != null)
                    {
                        childController.OriginalParentTransform = ASystemMenuView.transform;
                    }
                }
            }
        }
        // Initially hide views
        if (SystemsMenuView != null)
            SystemsMenuView.SetActive(false);
        if (ASystemMenuView != null)
            ASystemMenuView.SetActive(false);
        //galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
        //parentCanvas.worldCamera = galaxyEventCamera

    }
    public void ShowSystemMenuView()
    {
        SystemsMenuView.SetActive(true);
    }
    public void ShowA_SystemMenuView()
    {
        ASystemMenuView.SetActive(true);
    }
    public void HideSystemMenuView()
    {
        SystemsMenuView.SetActive(false);
    }
    public void HideA_SystemMenuView()
    {
        ASystemMenuView.SetActive(false);
    }
    // Public API (moved logic)
    public void SetupSystemUIData()
    {
        if (StarSysManager.Instance == null) return;
        foreach (var sysController in StarSysManager.Instance.StarSysControllerList)
        {
            if (sysController == null) continue;

            if (!listOfStarSysUiGos.Contains(sysController.StarSysUIGameObject) &&
                GameController.Instance.AreWeLocalPlayer(sysController.StarSysData.CurrentOwnerCivEnum))
            {

                var galaxyMenu = GalaxyMenuUIController.Instance;
                galaxyMenu.FleetLookingForShipDeploy = null;
                galaxyMenu.FleetLookingForDestination = null;
                galaxyMenu.StarSystLookingForShipDeploy = sysController;
                // wire up individual star system UI
                sysController.StarSysUIGameObject.SetActive(true);
                sysController.StarSysUIGameObject.transform.SetParent(SysListContainer.transform, false);
                listOfStarSysUiGos.Add(sysController.StarSysUIGameObject);
                if (sysController.StarSysUIGameObject.GetComponent<FleetAndSystemChildController>().OriginalParentTransform == null)
                    sysController.StarSysUIGameObject.GetComponent<FleetAndSystemChildController>().OriginalParentTransform = SysListContainer.transform;

                var sysUIFieldElement = sysController.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (sysUIFieldElement == null)
                {
                    Debug.LogWarning($"SetupSystemUIData: StarSysUI_Fields missing on UI prefab for {sysController.name}");
                    continue;
                }

                // Basic transform bindings that are independent of textual content
                sysUIFieldElement.redDot.anchoredPosition = new Vector2(sysController.StarSysData.GetPosition().x * 0.12f,
                    sysController.StarSysData.GetPosition().z * 0.12f);
                sysUIFieldElement.cancelShipManagerButton.gameObject.SetActive(false);

                // Bind button handlers and images (keep existing wiring; textual content will be initialized centrally)
                sysUIFieldElement.buildButton.onClick.RemoveAllListeners();
                sysUIFieldElement.buildButton.onClick.AddListener(() => sysController.BuildClick(sysController));
                sysUIFieldElement.shipButton.onClick.RemoveAllListeners();
                sysUIFieldElement.shipButton.onClick.AddListener(() => sysController.ShipClick(sysController));
                sysUIFieldElement.shipDeployButton.onClick.RemoveAllListeners();
                sysUIFieldElement.shipDeployButton.onClick.AddListener(() => StarSysClickShipDeployButton(sysController));
                sysUIFieldElement.newFleetButton.onClick.RemoveAllListeners();
                sysUIFieldElement.newFleetButton.onClick.AddListener(() => ClickNewFleetButton(sysController));
                sysUIFieldElement.mergeFleetButton.onClick.RemoveAllListeners();
                sysUIFieldElement.mergeFleetButton.onClick.AddListener(() => StarSysClickMergeShipsButton(sysController));
                sysUIFieldElement.cancelShipManagerButton.onClick.RemoveAllListeners();
                sysUIFieldElement.cancelShipManagerButton.onClick.AddListener(() => galaxyMenu.CloseMenu(Menu.FleetMenu));
                // Ensure SysButtonOnOff components exist and assign types, then wire click handlers
                if (sysUIFieldElement.factoryButtonOn != null)
                {
                    if (sysUIFieldElement.factoryButtonOn.GetComponent<SysButtonOnOff>() != null)
                    {
                        var comp = sysUIFieldElement.factoryButtonOn.GetComponent<SysButtonOnOff>();
                        comp.button = SystemOnOffButtons.FactoryOnButton;
                        sysUIFieldElement.factoryButtonOn.onClick.RemoveAllListeners();
                        sysUIFieldElement.factoryButtonOn.onClick.AddListener(() => sysController.FactoryButtonOnClicked(sysController));
                    }
                }
                if (sysUIFieldElement.factoryButtonOff != null)
                {
                    if (sysUIFieldElement.factoryButtonOff.GetComponent<SysButtonOnOff>() != null)
                    {
                        var comp = sysUIFieldElement.factoryButtonOff.GetComponent<SysButtonOnOff>();
                        comp.button = SystemOnOffButtons.FactoryOffButton;
                        sysUIFieldElement.factoryButtonOff.onClick.RemoveAllListeners();
                        sysUIFieldElement.factoryButtonOff.onClick.AddListener(() => sysController.FactoryButtonOffClicked(sysController));
                    }
                    //var comp = sysUIFieldElement.factoryButtonOff.GetComponent<SysButtonOnOff>() ?? sysUIFieldElement.factoryButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                    //comp.button = SystemOnOffButtons.FactoryOffButton;
                    //sysUIFieldElement.factoryButtonOff.onClick.RemoveAllListeners();
                    //sysUIFieldElement.factoryButtonOff.onClick.AddListener(() => sysController.FactoryButtonOffClicked(sysController));
                }
                if (sysUIFieldElement.yardButtonOn != null)
                {
                    if (sysUIFieldElement.yardButtonOn.GetComponent<SysButtonOnOff>() != null)
                    {
                        var comp = sysUIFieldElement.yardButtonOn.GetComponent<SysButtonOnOff>();
                        comp.button = SystemOnOffButtons.ShipyardOnButton;
                        sysUIFieldElement.yardButtonOn.onClick.RemoveAllListeners();
                        sysUIFieldElement.yardButtonOn.onClick.AddListener(() => sysController.YardButtonOnClicked(sysController));
                    }
                    //var comp = sysUIFieldElement.yardButtonOn.GetComponent<SysButtonOnOff>() ?? sysUIFieldElement.yardButtonOn.gameObject.AddComponent<SysButtonOnOff>();
                    //comp.button = SystemOnOffButtons.ShipyardOnButton;
                    //sysUIFieldElement.yardButtonOn.onClick.RemoveAllListeners();
                    //sysUIFieldElement.yardButtonOn.onClick.AddListener(() => sysController.YardButtonOnClicked(sysController));
                }
                if (sysUIFieldElement.yardButtonOff != null)
                {
                    if (sysUIFieldElement.yardButtonOff.GetComponent<SysButtonOnOff>() != null)
                    {
                        var comp = sysUIFieldElement.yardButtonOff.GetComponent<SysButtonOnOff>();
                        comp.button = SystemOnOffButtons.ShipyardOffbutton;
                        sysUIFieldElement.yardButtonOff.onClick.RemoveAllListeners();
                        sysUIFieldElement.yardButtonOff.onClick.AddListener(() => sysController.YardButtonOffClicked(sysController));
                    }
                    //var comp = sysUIFieldElement.yardButtonOff.GetComponent<SysButtonOnOff>() ?? sysUIFieldElement.yardButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                    //comp.button = SystemOnOffButtons.ShipyardOffbutton;
                    //sysUIFieldElement.yardButtonOff.onClick.RemoveAllListeners();
                    //sysUIFieldElement.yardButtonOff.onClick.AddListener(() => sysController.YardButtonOffClicked(sysController));
                }
                if (sysUIFieldElement.shieldButtonOn != null)
                {
                    if (sysUIFieldElement.shieldButtonOn.GetComponent<SysButtonOnOff>() != null)
                    {
                        var comp = sysUIFieldElement.shieldButtonOn.GetComponent<SysButtonOnOff>();
                        comp.button = SystemOnOffButtons.ShieldGeneratorOnButton;
                        sysUIFieldElement.shieldButtonOn.onClick.RemoveAllListeners();
                        sysUIFieldElement.shieldButtonOn.onClick.AddListener(() => sysController.ShieldButtonOnClicked(sysController));
                    }
                    //var comp = sysUIFieldElement.shieldButtonOn.GetComponent<SysButtonOnOff>() ?? sysUIFieldElement.shieldButtonOn.gameObject.AddComponent<SysButtonOnOff>();
                    //comp.button = SystemOnOffButtons.ShieldGeneratorOnButton;
                    //sysUIFieldElement.shieldButtonOn.onClick.RemoveAllListeners();
                    //sysUIFieldElement.shieldButtonOn.onClick.AddListener(() => sysController.ShieldButtonOnClicked(sysController));
                }
                if (sysUIFieldElement.shieldButtonOff != null)
                {
                    if (sysUIFieldElement.shieldButtonOff.GetComponent<SysButtonOnOff>() != null)
                    {
                        var comp = sysUIFieldElement.shieldButtonOff.GetComponent<SysButtonOnOff>();
                        comp.button = SystemOnOffButtons.ShieldGeneratorOffbutton;
                        sysUIFieldElement.shieldButtonOff.onClick.RemoveAllListeners();
                        sysUIFieldElement.shieldButtonOff.onClick.AddListener(() => sysController.ShieldButtonOffClicked(sysController));
                    }
                    //var comp = sysUIFieldElement.shieldButtonOff.GetComponent<SysButtonOnOff>() ?? sysUIFieldElement.shieldButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                    //comp.button = SystemOnOffButtons.ShieldGeneratorOffbutton;
                    //sysUIFieldElement.shieldButtonOff.onClick.RemoveAllListeners();
                    //sysUIFieldElement.shieldButtonOff.onClick.AddListener(() => sysController.ShieldButtonOffClicked(sysController));
                }
                if (sysUIFieldElement.oBButtonOn != null)
                {
                    if (sysUIFieldElement.oBButtonOn.GetComponent<SysButtonOnOff>() != null)
                    {
                        var comp = sysUIFieldElement.oBButtonOn.GetComponent<SysButtonOnOff>();
                        comp.button = SystemOnOffButtons.OrbitalBatteryOnButton;
                        sysUIFieldElement.oBButtonOn.onClick.RemoveAllListeners();
                        sysUIFieldElement.oBButtonOn.onClick.AddListener(() => sysController.OBButtonOnClicked(sysController));
                        //}
                        //var comp = sysUIFieldElement.oBButtonOn.GetComponent<SysButtonOnOff>() ?? sysUIFieldElement.oBButtonOn.gameObject.AddComponent<SysButtonOnOff>();
                        //comp.button = SystemOnOffButtons.OrbitalBatteryOnButton;
                        //sysUIFieldElement.oBButtonOn.onClick.RemoveAllListeners();
                        //sysUIFieldElement.oBButtonOn.onClick.AddListener(() => sysController.OBButtonOnClicked(sysController));
                    }
                    if (sysUIFieldElement.oBButtonOff != null)
                    {
                        if (sysUIFieldElement.oBButtonOff.GetComponent<SysButtonOnOff>() != null)
                        {
                            var comp = sysUIFieldElement.oBButtonOff.GetComponent<SysButtonOnOff>();
                            comp.button = SystemOnOffButtons.OrbitalBatteryOffButton;
                            sysUIFieldElement.oBButtonOff.onClick.RemoveAllListeners();
                            sysUIFieldElement.oBButtonOff.onClick.AddListener(() => sysController.OBButtonOffClicked(sysController));
                        }
                        //var comp = sysUIFieldElement.oBButtonOff.GetComponent<SysButtonOnOff>() ?? sysUIFieldElement.oBButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                        //comp.button = SystemOnOffButtons.OrbitalBatteryOffButton;
                        //sysUIFieldElement.oBButtonOff.onClick.RemoveAllListeners();
                        //sysUIFieldElement.oBButtonOff.onClick.AddListener(() => sysController.OBButtonOffClicked(sysController));
                    }
                    if (sysUIFieldElement.researchButtonOn != null)
                    {
                        if (sysUIFieldElement.researchButtonOn.GetComponent<SysButtonOnOff>() != null)
                        {
                            var comp = sysUIFieldElement.researchButtonOn.GetComponent<SysButtonOnOff>();
                            comp.button = SystemOnOffButtons.ResearchCenterOnButton;
                            sysUIFieldElement.researchButtonOn.onClick.RemoveAllListeners();
                            sysUIFieldElement.researchButtonOn.onClick.AddListener(() => sysController.ResearchButtonOnClicked(sysController));
                        }
                        //var comp = sysUIFieldElement.researchButtonOn.GetComponent<SysButtonOnOff>() ?? sysUIFieldElement.researchButtonOn.gameObject.AddComponent<SysButtonOnOff>();
                        //comp.button = SystemOnOffButtons.ResearchCenterOnButton;
                        //sysUIFieldElement.researchButtonOn.onClick.RemoveAllListeners();
                        //sysUIFieldElement.researchButtonOn.onClick.AddListener(() => sysController.ResearchButtonOnClicked(sysController));
                    }
                    if (sysUIFieldElement.researchButtonOff != null)
                    {
                        if (sysUIFieldElement.researchButtonOff.GetComponent<SysButtonOnOff>() != null)
                        {
                            var comp = sysUIFieldElement.researchButtonOff.GetComponent<SysButtonOnOff>();
                            comp.button = SystemOnOffButtons.ResearchCenterOffButton;
                            sysUIFieldElement.researchButtonOff.onClick.RemoveAllListeners();
                            sysUIFieldElement.researchButtonOff.onClick.AddListener(() => sysController.ResearchButtonOffClicked(sysController));
                        }
                        //var comp = sysUIFieldElement.researchButtonOff.GetComponent<SysButtonOnOff>() ?? sysUIFieldElement.researchButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                        //comp.button = SystemOnOffButtons.ResearchCenterOffButton;
                        //sysUIFieldElement.researchButtonOff.onClick.RemoveAllListeners();
                        //sysUIFieldElement.researchButtonOff.onClick.AddListener(() => sysController.ResearchButtonOffClicked(sysController));
                    }

                    // Slider bindings
                    Slider[] sliders = sysController.StarSysUIGameObject.GetComponentsInChildren<Slider>();
                    for (int i = 0; i < sliders.Length; i++)
                    {
                        if (sliders[i].name == "BuildProgressSlider")
                        {
                            SliderBuildProgress = sliders[i];
                            SliderBuildProgress.value = 0f;
                        }
                        if (sliders[i].name == "ShipBuildProgressSlider")
                        {
                            ShipSliderBuildProgress = sliders[i];
                            ShipSliderBuildProgress.value = 0f;
                        }
                    }

                    // image binding (these are generic icons from theme; InitializeFromStarSysData will set facility icons/names/ratios)
                    sysUIFieldElement.powerUnitImage.sprite = ThemeManager.Instance.CurrentTheme.PowerPlantImage;
                    sysUIFieldElement.factoryImage.sprite = ThemeManager.Instance.CurrentTheme.FactoryImage;
                    sysUIFieldElement.shipyardImage.sprite = ThemeManager.Instance.CurrentTheme.ShipyardImage;
                    sysUIFieldElement.shieldPlantImage.sprite = ThemeManager.Instance.CurrentTheme.ShieldImage;
                    sysUIFieldElement.orbitalBatteriesImage.sprite = ThemeManager.Instance.CurrentTheme.OrbitalBatteriesImage;
                    sysUIFieldElement.researchImage.sprite = ThemeManager.Instance.CurrentTheme.ResearchCenterImage;

                    // CENTRALIZED UI UPDATE - authoritative: read StarSysData lists and update UI
                    try
                    {
                        sysUIFieldElement.InitializeFromStarSysData(sysController.StarSysData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"SetupSystemUIData: InitializeFromStarSysData failed for {sysController.name}: {ex.Message}");
                    }

                    // Persist references used by menu controller
                    powerOverload = sysUIFieldElement.powerOverload != null ? sysUIFieldElement.powerOverload.gameObject : sysUIFieldElement.PowerOverload;
                    PowerOverloadImage = sysUIFieldElement.powerOverloadImage != null ? sysUIFieldElement.powerOverloadImage.gameObject : sysUIFieldElement.PowerOverload?.gameObject;
                    //coroutineRunner = CoroutineRunner.Instance;

                    // Attach system ships UI if any
                    for (int i = 0; i < sysController.StarSysData.ShipsList.Count; i++)
                    {
                        if (sysController.StarSysData.ShipsList[i].ShipListUIGameObject != null)
                        {
                            if (aSystemShipListContainer != null)
                                sysController.StarSysData.ShipsList[i].ShipListUIGameObject.transform.SetParent(aSystemShipListContainer.transform, false);
                        }
                    }
                }

                if (sysController.StarSysUIGameObject != null)
                {
                    sysController.StarSysUIGameObject.SetActive(true);
                    sysController.StarSysUIGameObject.transform.SetParent(SysListContainer.transform, false);
                }
            }
        }
    }

    public void SetActiveSetParentUIGO(StarSysController theSysCon)
    {
        if (theSysCon == null) return;
        theSysCon.StarSysUIGameObject.SetActive(true);
        theSysCon.StarSysUIGameObject.transform.SetParent(ASystemMenuView.transform, false);
        lastSysCon = theSysCon;
    }
    public void MoveTheSysUIGO(GameObject sysConGO)
    {
        int numFound = 0;
        List<GameObject> foundGoList = new List<GameObject>();
        for (int i = 0; i < ASystemMenuView.transform.childCount; i++)
        {
            numFound = i;
            if (i > 0)
                foundGoList.Add(ASystemMenuView.transform.GetChild(i).gameObject);
        }
        if (numFound > 0)
            for (int j = 0; j < numFound; j++)
                Destroy(foundGoList[j]);

        for (int i = 0; i < listOfStarSysUiGos.Count; i++)
        {
            if (listOfStarSysUiGos[i] == sysConGO)
            {
                listOfStarSysUiGos[i].transform.SetParent(ASystemMenuView.transform, false);
            }
        }
    }

    public void MoveBackAnyaSysUIGO()
    {
        ASystemMenuView.SetActive(true);
        for (int i = 0; i < ASystemMenuView.transform.childCount; i++)
        {
            var child = ASystemMenuView.transform.GetChild(i)?.gameObject;
            if (child == null) continue;

            var childCtrl = child.GetComponent<FleetAndSystemChildController>();
            if (childCtrl != null)
            {
                Transform originalParent = childCtrl.OriginalParentTransform;
                // Fallback: if OriginalParentTransform is null or equals ASystemMenuView, use SysListContainer if available
                if (originalParent == null || originalParent == ASystemMenuView.transform)
                {
                    if (SysListContainer != null)
                        originalParent = SysListContainer.transform;
                }

                if (originalParent != null)
                    child.transform.SetParent(originalParent, false);
            }
        }
        lastSysCon = null;
    }
    public void CloseBuildingQueues()
    {
        GalaxyMenuUIController.Instance.CloseMenu(Menu.BuildMenu);
        GalaxyMenuUIController.Instance.CloseMenu(Menu.ASystemMenu);
        lastSysCon.LoadAStarSystem();
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
        var fileds = sysController.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
        int newFacilityLoad = 0;
        int numOn = 0;
        List<GameObject> facilities = new List<GameObject>();
        switch (facilityType)
        {
            case StarSysFacilityType.Factory:
                newFacilityLoad = sysController.StarSysData.FactoryData.PowerLoad;
                facilities = sysController.StarSysData.Factories;
                numOn = NumFacilitiesTurnedOn(StarSysFacilityType.Factory, facilities);
                fileds.numFactoryRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                fileds.factoryLoad.text = (newFacilityLoad * numOn).ToString();
                break;
            case StarSysFacilityType.Shipyard:
                newFacilityLoad = sysController.StarSysData.ShipyardData.PowerLoad;
                facilities = sysController.StarSysData.Shipyards;
                numOn = NumFacilitiesTurnedOn(StarSysFacilityType.Shipyard, facilities);
                fileds.numYardsOnRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                fileds.yardLoad.text = (newFacilityLoad * numOn).ToString();
                break;
            case StarSysFacilityType.ShieldGenerator:
                newFacilityLoad = sysController.StarSysData.ShieldGeneratorData.PowerLoad;
                facilities = sysController.StarSysData.ShieldGenerators;
                numOn = NumFacilitiesTurnedOn(StarSysFacilityType.ShieldGenerator, facilities);
                fileds.numShieldsRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                fileds.shieldLoad.text = (newFacilityLoad * numOn).ToString();
                break;
            case StarSysFacilityType.OrbitalBattery:
                newFacilityLoad = sysController.StarSysData.OrbitalBatteryData.PowerLoad;
                facilities = sysController.StarSysData.OrbitalBatteries;
                numOn = NumFacilitiesTurnedOn(StarSysFacilityType.OrbitalBattery, facilities);
                fileds.numOBRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                fileds.oBLoad.text = (newFacilityLoad * numOn).ToString();
                break;
            case StarSysFacilityType.ResearchCenter:
                newFacilityLoad = sysController.StarSysData.ResearchCenterData.PowerLoad;
                facilities = sysController.StarSysData.ResearchCenters;
                numOn = NumFacilitiesTurnedOn(StarSysFacilityType.ResearchCenter, facilities);
                fileds.numResearchRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                fileds.researchLoad.text = (newFacilityLoad * numOn).ToString();
                break;
            default:
                break;
        }
    }

    private int NumFacilitiesTurnedOn(StarSysFacilityType factory, List<GameObject> facilities) //, StarSysController sysController, ref int numOn, ref int newFacilityLoad, StarSysUI_Fields fileds)
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
        int load = 0;
        int output = 0;
        for (int i = 0; i < sysCon.StarSysData.PowerPlants.Count; i++)
            output += sysCon.StarSysData.PowerPlantData.PowerOutput;
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
        TextMeshProUGUI[] OneTMP = sysCon.StarSysUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < OneTMP.Length; i++)
        {
            OneTMP[i].enabled = true;
            if ("NumP Load" == OneTMP[i].name)
                OneTMP[i].text = load.ToString();
            if ("NumTotal EOut" == OneTMP[i].name)
                OneTMP[i].text = output.ToString();
        }
    }

    internal void AddSysFacility(StarSysController controller, GameObject faciltyGO, string loadName, string ratioName, StarSysFacilityType facilityType)
    {
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
                    newFacilityLoad = starSysData.PowerPlantData?.PowerOutput ?? 0; // power plants contribute output not load
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
            if (faciltyGO != null && !facilities.Contains(faciltyGO))
                facilities.Add(faciltyGO);

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

                    // We've updated the typed UI; ensure menu-level power load is recomputed
                    StarSysMenuUIController.Instance?.UpdateSystemPowerBalance(controller);
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
                StarSysMenuUIController.Instance.UpdateSystemPowerBalance(controller);
            }
            else
            {
                Debug.LogWarning($"AddSysFacility fallback: StarSysUIGameObject is null for {controller.name} and typed UI update failed.");
            }
        }
    }

    internal void UpdateSystemShipList(StarSysController sysCon)
    {
        // Placeholder for UI update logic specific to a system ship list.
        // The original method threw NotImplementedException.
        // Implement specific incremental updates here when you need them.
    }

    private void OnDisable()
    {
        // When the UI menu closes (e.g., switching menus or hiding canvas)
        CleanupDestroyedOrInactiveUIs();
    }

    private void OnDestroy()
    {
        // When this controller is destroyed (e.g., scene unload)
        ClearAllStarSysUIs();
    }

    public void CleanupDestroyedOrInactiveUIs()
    {
        // Remove any destroyed or inactive GameObjects from the list
        listOfStarSysUiGos.RemoveAll(go => go == null || !go.activeInHierarchy);
        Debug.Log("DiplomacyMenuUIController: Cleaned up destroyed or inactive diplomacy UIs.");
    }
    public void ClearAllStarSysUIs()
    {
        foreach (var go in listOfStarSysUiGos)
        {
            if (go != null)
                Destroy(go);
        }
        listOfStarSysUiGos.Clear();
        Debug.Log("Cleared all diplomacy UI GameObjects.");
    }

    internal void StarSysClickShipDeployButton(StarSysController starSysCon)
    {
        var galaxyUI = GalaxyMenuUIController.Instance;
        if (galaxyUI != null)
        {
            galaxyUI.WhatSystIsLookingForShipDeploy(starSysCon);
            galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipDeploy);
            MousePointerChanger.Instance.SetShipExchangeCursor();
            ShipDeployMenuUIController.Instance.TopStarSyst = starSysCon;
        }
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
        fleetData.FleetInt = fleetManager.GetNewFleetInt(thisCivData.CivEnum);
        //fleetData.Name = $"{thisCivData.CivShortName} Fleet {fleetData.FleetInt}";
        fleetData.Insignia = thisCivData.InsigniaSprite;
        fleetData.ShipsList = new List<ShipController>();
        var galaxyMenuUICon = GalaxyMenuUIController.Instance;
        galaxyMenuUICon.ResetClickMode();

        var newFleet = fleetManager.InstantiateFleet(null, sysController, fleetData, position, true);
        tempFleetController = newFleet;
        galaxyMenuUICon.ShowShipDeployForSystemNewFleet(sysController, newFleet);

        // The GalaxyMenuUIController.ShowShipDeployForFleetNewFleet handles showing the panel and setting up lists,
        // so we do not call ShipDeployMenuUIController methods here again.

        //galaxyMenuUICon.StarSystLookingForShipDeploy = sysController;
        //galaxyMenuUICon.FleetLookingForShipDeploy = null;
        //var shipDelployUICon = ShipDeployMenuUIController.Instance;
        //shipDelployUICon.SetUpTopShipLists(sysController.StarSysData.ShipsList);
        //shipDelployUICon.SetUpBottomShipLists(newFleet);
        //shipDelployUICon.ShowShipDeployMenuView();
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
        galaxyUI.ClickCancelShipDeployButton();
        galaxyUI.ResetClickMode();
        MousePointerChanger.Instance.ResetCursor();
        if (cancelShipManagerButtonGO != null)
            cancelShipManagerButtonGO.SetActive(false);
        ShipDeployMenuUIController.Instance.gameObject.SetActive(false);
    }

    internal void SetBuildProgress(float buildingProgress)
    {
        SliderBuildProgress.value = buildingProgress;
    }

    internal void SetShipBuildProgress(float shipProgress)
    {
        ShipSliderBuildProgress.value = shipProgress;
    }
}
