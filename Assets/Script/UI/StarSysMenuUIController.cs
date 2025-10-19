using Assets.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;

public class StarSysMenuUIController : MonoBehaviour
{
    public static StarSysMenuUIController Instance;
    [Header("References (assign in Inspector)")]
    [SerializeField] private GameObject systemsMenuView;
    [SerializeField] private GameObject sysListContainer;
    [SerializeField] private GameObject sysShipListContainer;
    [SerializeField] private GameObject aSystemMenuView;
    [SerializeField] private GameObject aSystemShipContainer;
   // [SerializeField] private GameObject sysBackground;
    [SerializeField] private FleetMenuUIController fleetMenuUIController; // used for parenting right-side ship UI

    [Header("Runtime lists")]
    [SerializeField] private List<GameObject> listOfStarSysUiGos = new List<GameObject>();
    [SerializeField] private List<GameObject> listOfSysShipUiGos = new List<GameObject>();

    [Header("Power overload visuals")]
    [SerializeField] private GameObject powerOverload;

    public GameObject SystemsMenuView => systemsMenuView;
    public GameObject ASystemMenuView => aSystemMenuView;
    public GameObject SysListContainer => sysListContainer;
    public bool IsVisibleA_SystemMenuView => aSystemMenuView.activeSelf;
    public bool IsVisibleSystemMenuView => systemsMenuView.activeSelf;

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
        if (systemsMenuView != null)
            systemsMenuView.SetActive(false);
        if (aSystemMenuView != null)
            aSystemMenuView.SetActive(false);
    }
    public void ShowSystemMenuView()
    {
        systemsMenuView.SetActive(true);
    }
    public void ShowA_SystemMenuView()
    {
        aSystemMenuView.SetActive(true);
    }
    public void HideSystemMenuView()
    {
        systemsMenuView.SetActive(false);
    }
    public void HideA_SystemMenuView()
    {
        aSystemMenuView.SetActive(false);
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
                sysController.StarSysUIGameObject.SetActive(true);
                sysController.StarSysUIGameObject.transform.SetParent(sysListContainer.transform, false);
                listOfStarSysUiGos.Add(sysController.StarSysUIGameObject);

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
                    if (transforArrayInStarSysUI[i].name == "aSystemShipContent")
                    {
                        aSystemShipContainer = transforArrayInStarSysUI[i].gameObject;
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
                        case "SelectOtherSysOrFleetForShipsButton": // ToDo; Implement functionality
                            listButton.onClick.RemoveAllListeners();
                            listButton.onClick.AddListener(() => sysController.SelectedOtherForShips(sysController));
                            break;
                        case "CancelSysOrFleetForShipsButton":
                            listButton.onClick.RemoveAllListeners();// ToDo; Implement functionality
                            listButton.onClick.AddListener(() => sysController.ClickCancelForShipsButton());
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
                                aSystemShipContainer = transforms[j].gameObject;
                                break;
                            }
                        }
                        sysController.StarSysData.ShipsList[i].ShipListUIGameObject.transform.SetParent(aSystemShipContainer.transform, false);
                    }
                }
            }

            if (sysController.StarSysUIGameObject != null)
            {
                sysController.StarSysUIGameObject.SetActive(true);
                sysController.StarSysUIGameObject.transform.SetParent(sysListContainer.transform, false);
            }
        }
    }
    public void SetUpASystemUIData(StarSysController theSysCon)
    {
        if (theSysCon == null) return;
        theSysCon.StarSysUIGameObject.SetActive(true);
        theSysCon.StarSysUIGameObject.transform.SetParent(aSystemMenuView.transform, false);
    }
    public void MoveTheSysUIGO(GameObject sysConGO)
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

    public void MoveBackAnySysUIGO()
    {
        for (int i = 0; i < aSystemMenuView.transform.childCount; i++)
        {
            if (aSystemMenuView.transform.GetChild(i).gameObject != null)
                aSystemMenuView.transform.GetChild(i).gameObject.transform.SetParent(sysListContainer.transform, false);
        }
    }

    public void CloseSystemShipsUI(StarSysController theSysCon)
    {
        if (theSysCon == null) return;
        theSysCon.StarSysRightSideShipsUIGameObject.SetActive(false);
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

    internal void LoadRightSideShipManagerSystemUIPrefab(GameObject gameObject)
    {

        var systemCon = gameObject.GetComponent<StarSysController>();
        if (systemCon == null) return;
        if (GameController.Instance.AreWeLocalPlayer(systemCon.StarSysData.CurrentOwnerCivEnum))
        {
            if (fleetMenuUIController != null && fleetMenuUIController.AFleetMenuView != null)
                systemCon.StarSysRightSideShipsUIGameObject.transform.SetParent(fleetMenuUIController.AFleetMenuView.transform, false);
            else
                systemCon.StarSysRightSideShipsUIGameObject.transform.SetParent(aSystemMenuView.transform, false);

            systemCon.StarSysRightSideShipsUIGameObject.SetActive(true);
            systemCon.StarSysRightSideShipsUIGameObject.transform.SetParent(aSystemMenuView.transform, false);
        }
        systemCon.StarSysRightSideShipsUIGameObject.transform.Translate(new Vector3(0f, 0f, 0f), Space.Self);
    }

}
