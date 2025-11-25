using Assets.Core;
using System.Collections;
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

    [Header("Power overload visuals")]
    [SerializeField] private GameObject powerOverload;

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
        // Initially hide fleet menu views
        if (SystemsMenuView != null)
            SystemsMenuView.SetActive(false);
        if (ASystemMenuView != null)
            ASystemMenuView.SetActive(false);
        //galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
        //parentCanvas.worldCamera = galaxyEventCamera;
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
                RectTransform[] transforArrayInStarSysUI = sysController.StarSysUIGameObject.GetComponentsInChildren<RectTransform>();
                for (int i = 0; i < transforArrayInStarSysUI.Length; i++)
                {
                    if (transforArrayInStarSysUI[i].name == "RedDot")
                    {
                        float x = sysController.StarSysData.GetPosition().x * 0.12f;
                        float y = 0f;
                        float z = sysController.StarSysData.GetPosition().z * 0.12f;
                        transforArrayInStarSysUI[i].Translate(new Vector3(x, z, y), Space.Self);
                    }
                    if (transforArrayInStarSysUI[i].name == "ShipContent")
                    {
                        aSystemShipListContainer = transforArrayInStarSysUI[i].gameObject;
                    }
                }

                // Text bindings
                TextMeshProUGUI[] OneTMP = sysController.StarSysUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
                for (int i = 0; i < OneTMP.Length; i++)
                {
                    OneTMP[i].enabled = true;
                    var name = OneTMP[i].name;

                    switch (name)
                    {
                        case "SysName":
                            OneTMP[i].text = sysController.StarSysData.SysName;
                            break;
                        case "HeaderPowerUnitText":
                            OneTMP[i].text = sysController.StarSysData.PowerPlantData.Name;
                            break;
                        case "NumPUnits":
                            OneTMP[i].text = (sysController.StarSysData.PowerPlants.Count).ToString();
                            break;
                        case "NumTotalEOut":
                            sysController.StarSysData.TotalSysPowerOutput = sysController.StarSysData.PowerPlants.Count * sysController.StarSysData.PowerPlantData.PowerOutput;
                            OneTMP[i].text = (sysController.StarSysData.TotalSysPowerOutput).ToString();
                            break;
                        case "NumP Load":
                            OneTMP[i].text = (sysController.StarSysData.TotalSysPowerLoad).ToString();
                            break;
                        case "NameFactory":
                            OneTMP[i].text = sysController.StarSysData.FactoryData.Name;
                            break;
                        case "NumFactoryRatio":
                            int count = 0;
                            foreach (var item in sysController.StarSysData.Factories)
                            {
                                TextMeshProUGUI TheText = item.GetComponent<TextMeshProUGUI>();
                                if (TheText.text == "1")
                                    count++;
                            }
                            OneTMP[i].text = count.ToString() + "/" + (sysController.StarSysData.Factories.Count).ToString();
                            break;
                        case "FactoryLoad":
                            OneTMP[i].text = (sysController.StarSysData.FactoryData.PowerLoad * sysController.StarSysData.Factories.Count).ToString();
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
                            OneTMP[i].text = (sysController.StarSysData.ShipyardData.PowerLoad * sysController.StarSysData.Shipyards.Count).ToString();
                            break;
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
                            break;
                        case "PowerOverload":
                            OneTMP[i].gameObject.SetActive(false);
                            powerOverload = OneTMP[i].gameObject;
                            break;
                        default:
                            break;
                    }
                }

                Image[] listOfImages = sysController.StarSysUIGameObject.GetComponentsInChildren<Image>();
                for (int i = 0; i < listOfImages.Length; i++)
                {
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

                // Buttons wiring
                Button[] listButtons = sysController.StarSysUIGameObject.GetComponentsInChildren<Button>();
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
                        case "FactoryButtonOff":
                        case "YardButtonOn":
                        case "YardButtonOff":
                        case "ShieldButtonOn":
                        case "ShieldButtonOff":
                        case "OBButtonOn":
                        case "OBButtonOff":
                        case "ResearchButtonOn":
                        case "ResearchButtonOff":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.FacilityOnClick(sysController, listButton.name));
                            break;
                        case "ShipDeployButton":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => SelectedShipDeployCursor(sysController));
                            break;
                        case "CancelShipDeployButton":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => ClickCancelShipDeployButton());
                            break;
                        case "NewFleetButton":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => ClickNewFleetButton(sysController));
                            break;
                        case "CancelNewFleetButton":
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => ClickCancelFleetButton());
                            break;
                        default:
                            break;
                    }
                }

                // Attach system ships UI if any
                for (int i = 0; i < sysController.StarSysData.ShipsList.Count; i++)
                {
                    if (sysController.StarSysData.ShipsList[i].ShipListUIGameObject != null)
                    {
                        var transforms = sysController.StarSysUIGameObject.transform.GetComponentsInChildren<Transform>();
                        for (int j = 0; j < transforms.Length; j++)
                        {
                            if (transforms[j].gameObject.name == "aSystemShipContent")
                            {
                                aSystemShipListContainer = transforms[j].gameObject;
                                break;
                            }
                        }
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

    public void UpdateFacilityUI(StarSysController sysController, int plusMinus, string loadName, string ratioName, StarSysFacilities facilityType)
    {
        if (!GameController.Instance.AreWeLocalPlayer(sysController.StarSysData.CurrentOwnerCivEnum)) return;

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
        TextMeshProUGUI[] OneTMP = sysController.StarSysUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < OneTMP.Length; i++)
        {
            OneTMP[i].enabled = true;
            if (loadName == OneTMP[i].name)
            {
                if (int.TryParse(OneTMP[i].text, out int load))
                {
                    load += plusMinus * newFacilityLoad;
                    OneTMP[i].text = load.ToString();
                }
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

    public void UpdateSystemPowerLoad(StarSysController sysCon)
    {
        if (sysCon == null) return;
        int load = 0;
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
        TextMeshProUGUI[] OneTMP = sysCon.StarSysUIGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < OneTMP.Length; i++)
        {
            OneTMP[i].enabled = true;
            if ("NumP Load" == OneTMP[i].name)
                OneTMP[i].text = load.ToString();
        }
    }

    public void FlashPowerOverload()
    {
        if (powerOverload == null) return;
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < 3; i++)
        {
            powerOverload.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            powerOverload.SetActive(false);
            yield return new WaitForSeconds(0.5f);
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

    internal void SelectedShipDeployCursor(StarSysController starSysControllerWaitingToExchangeShips)
    {
        if (GameController.Instance.AreWeLocalPlayer(starSysControllerWaitingToExchangeShips.StarSysData.CurrentOwnerCivEnum))
        {
            var galaxyUI = GalaxyMenuUIController.Instance;
            galaxyUI.WhatSystIsLookingForShipDeploy(starSysControllerWaitingToExchangeShips);
            galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipExchange);
            MousePointerChanger.Instance.SetShipExchangeCursor(starSysControllerWaitingToExchangeShips);
        }
    }
    private void ClickCancelShipDeployButton()
    {
        var galaxyUI = GalaxyMenuUIController.Instance;
        galaxyUI.ClickCancelShipDeployButton();
        galaxyUI.ResetClickMode();
        MousePointerChanger.Instance.ResetCursor();
    }
    private void ClickNewFleetButton(StarSysController sysController)
    {
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
        galaxyMenuUICon.StarSystConSelectedForShipDeploy = null;
        galaxyMenuUICon.StarSystLookingForShipDeploy = sysController;
        galaxyMenuUICon.FleetLookingForShipDeploy = null;
        ShipDeployMenuUIController.Instance.TopStarSyst = sysController;
        fleetManager.InstantiateFleet(sysController, fleetData, position, true);
        //Call this in InstantiateFleet: galaxyMenuUICon.ShowShipDeployForSystemNewFleet(sysController, newFleetCon);

    }
    private void ClickCancelFleetButton()
    {
        MousePointerChanger.Instance.ResetCursor();
        // to do
    }
}
