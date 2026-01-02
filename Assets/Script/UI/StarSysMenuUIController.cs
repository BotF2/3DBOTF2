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
    [SerializeField] private CoroutineRunner coroutineRunner;
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
                // wire up individual star system UI
                sysController.StarSysUIGameObject.SetActive(true);
                sysController.StarSysUIGameObject.transform.SetParent(SysListContainer.transform, false);
                listOfStarSysUiGos.Add(sysController.StarSysUIGameObject);
                if (sysController.StarSysUIGameObject.GetComponent<FleetAndSystemChildController>().OriginalParentTransform == null)
                    sysController.StarSysUIGameObject.GetComponent<FleetAndSystemChildController>().OriginalParentTransform = SysListContainer.transform;

                var tmpElement = sysController.StarSysUIGameObject.GetComponent<StarSysUIElement>();
                if (tmpElement == null)
                {
                    Debug.LogWarning($"SetupSystemUIData: StarSysUIElement missing on UI prefab for {sysController.name}");
                    continue;
                }

                // Basic transform bindings that are independent of textual content
                tmpElement.redDot.anchoredPosition = new Vector2(sysController.StarSysData.GetPosition().x * 0.12f,
                    sysController.StarSysData.GetPosition().z * 0.12f);
                tmpElement.cancelShipManagerButton.gameObject.SetActive(false);

                // Bind button handlers and images (keep existing wiring; textual content will be initialized centrally)
                tmpElement.buildButton.onClick.RemoveAllListeners();
                tmpElement.buildButton.onClick.AddListener(() => sysController.BuildClick(sysController));
                tmpElement.shipButton.onClick.RemoveAllListeners();
                tmpElement.shipButton.onClick.AddListener(() => sysController.ShipClick(sysController));

                // Ensure SysButtonOnOff components exist and assign types, then wire click handlers
                if (tmpElement.factoryButtonOn != null)
                {
                    var comp = tmpElement.factoryButtonOn.GetComponent<SysButtonOnOff>() ?? tmpElement.factoryButtonOn.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.FactoryOnButton;
                    tmpElement.factoryButtonOn.onClick.RemoveAllListeners();
                    tmpElement.factoryButtonOn.onClick.AddListener(() => sysController.FactoryButtonOnClicked(sysController));
                }
                if (tmpElement.factoryButtonOff != null)
                {
                    var comp = tmpElement.factoryButtonOff.GetComponent<SysButtonOnOff>() ?? tmpElement.factoryButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.FactoryOffButton;
                    tmpElement.factoryButtonOff.onClick.RemoveAllListeners();
                    tmpElement.factoryButtonOff.onClick.AddListener(() => sysController.FactoryButtonOffClicked(sysController));
                }
                if (tmpElement.yardButtonOn != null)
                {
                    var comp = tmpElement.yardButtonOn.GetComponent<SysButtonOnOff>() ?? tmpElement.yardButtonOn.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.ShipyardOnButton;
                    tmpElement.yardButtonOn.onClick.RemoveAllListeners();
                    tmpElement.yardButtonOn.onClick.AddListener(() => sysController.YardButtonOnClicked(sysController));
                }
                if (tmpElement.yardButtonOff != null)
                {
                    var comp = tmpElement.yardButtonOff.GetComponent<SysButtonOnOff>() ?? tmpElement.yardButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.ShipyardOffbutton;
                    tmpElement.yardButtonOff.onClick.RemoveAllListeners();
                    tmpElement.yardButtonOff.onClick.AddListener(() => sysController.YardButtonOffClicked(sysController));
                }
                if (tmpElement.shieldButtonOn != null)
                {
                    var comp = tmpElement.shieldButtonOn.GetComponent<SysButtonOnOff>() ?? tmpElement.shieldButtonOn.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.ShieldGeneratorOnButton;
                    tmpElement.shieldButtonOn.onClick.RemoveAllListeners();
                    tmpElement.shieldButtonOn.onClick.AddListener(() => sysController.ShieldButtonOnClicked(sysController));
                }
                if (tmpElement.shieldButtonOff != null)
                {
                    var comp = tmpElement.shieldButtonOff.GetComponent<SysButtonOnOff>() ?? tmpElement.shieldButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.ShieldGeneratorOffbutton;
                    tmpElement.shieldButtonOff.onClick.RemoveAllListeners();
                    tmpElement.shieldButtonOff.onClick.AddListener(() => sysController.ShieldButtonOffClicked(sysController));
                }
                if (tmpElement.oBButtonOn != null)
                {
                    var comp = tmpElement.oBButtonOn.GetComponent<SysButtonOnOff>() ?? tmpElement.oBButtonOn.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.OrbitalBatteryOnButton;
                    tmpElement.oBButtonOn.onClick.RemoveAllListeners();
                    tmpElement.oBButtonOn.onClick.AddListener(() => sysController.OBButtonOnClicked(sysController));
                }
                if (tmpElement.oBButtonOff != null)
                {
                    var comp = tmpElement.oBButtonOff.GetComponent<SysButtonOnOff>() ?? tmpElement.oBButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.OrbitalBatteryOffButton;
                    tmpElement.oBButtonOff.onClick.RemoveAllListeners();
                    tmpElement.oBButtonOff.onClick.AddListener(() => sysController.OBButtonOffClicked(sysController));
                }
                if (tmpElement.researchButtonOn != null)
                {
                    var comp = tmpElement.researchButtonOn.GetComponent<SysButtonOnOff>() ?? tmpElement.researchButtonOn.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.ResearchCenterOnButton;
                    tmpElement.researchButtonOn.onClick.RemoveAllListeners();
                    tmpElement.researchButtonOn.onClick.AddListener(() => sysController.ResearchButtonOnClicked(sysController));
                }
                if (tmpElement.researchButtonOff != null)
                {
                    var comp = tmpElement.researchButtonOff.GetComponent<SysButtonOnOff>() ?? tmpElement.researchButtonOff.gameObject.AddComponent<SysButtonOnOff>();
                    comp.button = SystemOnOffButtons.ResearchCenterOffButton;
                    tmpElement.researchButtonOff.onClick.RemoveAllListeners();
                    tmpElement.researchButtonOff.onClick.AddListener(() => sysController.ResearchButtonOffClicked(sysController));
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
                tmpElement.powerUnitImage.sprite = ThemeManager.Instance.CurrentTheme.PowerPlantImage;
                tmpElement.factoryImage.sprite = ThemeManager.Instance.CurrentTheme.FactoryImage;
                tmpElement.shipyardImage.sprite = ThemeManager.Instance.CurrentTheme.ShipyardImage;
                tmpElement.shieldPlantImage.sprite = ThemeManager.Instance.CurrentTheme.ShieldImage;
                tmpElement.orbitalBatteriesImage.sprite = ThemeManager.Instance.CurrentTheme.OrbitalBatteriesImage;
                tmpElement.researchImage.sprite = ThemeManager.Instance.CurrentTheme.ResearchCenterImage;

                // CENTRALIZED UI UPDATE - authoritative: read StarSysData lists and update UI
                try
                {
                    tmpElement.InitializeFromStarSysData(sysController.StarSysData);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"SetupSystemUIData: InitializeFromStarSysData failed for {sysController.name}: {ex.Message}");
                }

                // Persist references used by menu controller
                powerOverload = tmpElement.powerOvarload != null ? tmpElement.powerOvarload.gameObject : tmpElement.PowerOverload;
                PowerOverloadImage = tmpElement.powerOverloadImage != null ? tmpElement.powerOverloadImage.gameObject : tmpElement.PowerOverload?.gameObject;
                coroutineRunner = tmpElement.coroutineRunner;

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

    public void SetActiveSetParentUIGO(StarSysController theSysCon)
    {
        if (theSysCon == null) return;
        theSysCon.StarSysUIGameObject.SetActive(true);
        theSysCon.StarSysUIGameObject.transform.SetParent(ASystemMenuView.transform, false);
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
    }
    public void CloseBuildingQueues()
    {
        GalaxyMenuUIController.Instance.CloseMenu(Menu.BuildMenu);
    }
    public void RemoveSystem(StarSysController sysController)
    {
        if (sysController == null) return;
        if (sysControllersContains(sysController))
        {
            listOfStarSysUiGos.Remove(sysController.StarSysUIGameObject);
        }
    }

    private bool sysControllersContains(StarSysController sysController)
    {
        // safe helper - originally GalaxyMenu had its own list; here keep list tracking by GameObject
        return listOfStarSysUiGos.Contains(sysController.StarSysUIGameObject);
    }

    public void UpdateFacilityUI(StarSysController sysController, int plusMinus, string loadName, string ratioName, StarSysFacilityType facilityType)
    {
        if (!GameController.Instance.AreWeLocalPlayer(sysController.StarSysData.CurrentOwnerCivEnum)) return;

        int newFacilityLoad = 0;
        List<GameObject> facilities = new List<GameObject>();
        switch (facilityType)
        {
            case StarSysFacilityType.Factory:
                newFacilityLoad = sysController.StarSysData.FactoryData.PowerLoad;
                facilities = sysController.StarSysData.Factories;
                break;
            case StarSysFacilityType.Shipyard:
                newFacilityLoad = sysController.StarSysData.ShipyardData.PowerLoad;
                facilities = sysController.StarSysData.Shipyards;
                break;
            case StarSysFacilityType.ShieldGenerator:
                newFacilityLoad = sysController.StarSysData.ShieldGeneratorData.PowerLoad;
                facilities = sysController.StarSysData.ShieldGenerators;
                break;
            case StarSysFacilityType.OrbitalBattery:
                newFacilityLoad = sysController.StarSysData.OrbitalBatteryData.PowerLoad;
                facilities = sysController.StarSysData.OrbitalBatteries;
                break;
            case StarSysFacilityType.ResearchCenter:
                newFacilityLoad = sysController.StarSysData.ResearchCenterData.PowerLoad;
                facilities = sysController.StarSysData.ResearchCenters;
                break;
            default:
                break;
        }

        int numOn = 0;
        TextMeshProUGUI[] OneTMP = sysController.StarSysUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < OneTMP.Length; i++)
        {
            OneTMP[i].enabled = true;
            if (loadName == OneTMP[i].name)
            {
                OneTMP[i].text = (newFacilityLoad * facilities.Count).ToString();
                //if (int.TryParse(OneTMP[i].text, out int load))
                //{
                //    load += plusMinus * newFacilityLoad;
                //    OneTMP[i].text = load.ToString();
                //}
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
                break;
            }
        }
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
            var uiElement = controller.StarSysUIGameObject?.GetComponent<StarSysUIElement>();
            if (uiElement == null)
            {
                Debug.LogWarning($"AddSysFacility: StarSysUIElement not found for system {controller.name}. Falling back to string-based updates.");
            }
            else
            {
                StarSysUIElement.FacilityUI facUI = null;
                try
                {
                    facUI = uiElement.GetFacility(facilityType);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"AddSysFacility: facility UI of type {facilityType} not found on StarSysUIElement for {controller.name}. Exception: {ex.Message}");
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

    private void UpdateSystemPowerLoad(StarSysController controller)
    {
        throw new NotImplementedException();
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

    internal void ClickShipDeployCursor(StarSysController starSysControllerWaitingToExchangeShips)
    {
        if (GameController.Instance.AreWeLocalPlayer(starSysControllerWaitingToExchangeShips.StarSysData.CurrentOwnerCivEnum))
        {
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatSystIsLookingForShipDeploy(starSysControllerWaitingToExchangeShips);
            galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipExchange);
            MousePointerChanger.Instance.SetShipExchangeCursor(starSysControllerWaitingToExchangeShips);
        }
    }
    private void ClickMergeFleetButton()
    {
        // Merge fleet logic to be implemented
    }
    private void ClickNewFleetButton(StarSysController sysController)
    {
        if (sysController.StarSysData.ShipsList.Count < 2) return;
        MousePointerChanger.Instance.ResetCursor();
        var fleetManager = FleetManager.Instance;
        FleetSO fleetSO = fleetManager.GetFleetSO_byInt((int)sysController.StarSysData.CurrentOwnerCivEnum);
        var position = sysController.StarSysData.GetPosition();

        CivData thisCivData = CivManager.Instance.GetCivDataByCivEnum(fleetSO.CivOwnerEnum); // new CivData();
        FleetData fleetData = new FleetData(fleetSO);
        fleetData.CurrentWarpFactor = 3f;
        fleetData.CivLongName = thisCivData.CivLongName; //.CivLongName;
        fleetData.CivShortName = thisCivData.CivShortName;
        var galaxyMenuUICon = GalaxyMenuUIController.Instance;
        galaxyMenuUICon.ResetClickMode();
        galaxyMenuUICon.StarSystLookingForShipDeploy = sysController;
        galaxyMenuUICon.FleetLookingForShipDeploy = null;
        ShipDeployMenuUIController.Instance.TopStarSyst = sysController;
        var emptyFleetCon = fleetManager.InsatiateEmptyFleetController();
        var newFleet = fleetManager.InstantiateFleet(emptyFleetCon, sysController, fleetData, position, true);
        galaxyMenuUICon.FleetConSelectedForShipDeploy = newFleet;
        galaxyMenuUICon.StarSystConSelectedForShipDeploy = null;
        tempFleetController = newFleet;
        Destroy(emptyFleetCon.gameObject);
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
        cancelShipManagerButtonGO?.SetActive(false);
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
