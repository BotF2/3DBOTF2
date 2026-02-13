// Ignore Spelling: Sys

using Assets.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Represents the collection of class fields for elements in a star system UI panel, providing references to buttons, text fields,
/// images, and sliders used to display and interact with star system data in the game interface.
/// It is attached to the StarSysUI_Prefab instantiated GameObject.
/// <remarks>This class exposes public fields for Unity UI components, allowing other scripts to access and
/// manipulate the star system's user interface. All fields should be assigned via the Unity Editor. This type is
/// intended for use as a component on a Unity GameObject representing a star system UI panel.</remarks>
/// </summary>
public class StarSysUI_Fields : MonoBehaviour
{
    [Serializable]
    public class FacilityUI
    {
        public StarSysFacilityType type;

        public TextMeshProUGUI nameText;
        public TextMeshProUGUI loadText;
        public TextMeshProUGUI ratioText;

        public Button onButton;
        public Button offButton;

        public Image icon;
    }
    [Header("System")]
    public TextMeshProUGUI sysName;
    public RectTransform redDot;

    [Header("Power")]
    public TextMeshProUGUI numPUnits;
    public TextMeshProUGUI numTotalEOut;
    public TextMeshProUGUI numPLoad;

    [Header("Facilities")]
    public List<FacilityUI> facilities = new();

    [Header("Alerts")]
    public GameObject PowerOverload;
    //public CoroutineRunner CoroutineRunner;

    private Dictionary<StarSysFacilityType, FacilityUI> facilityUIDictionary;
    // fields below for dictionary reference
    public RectTransform shipContent;

    [Header("Buttons")]
    public Button buildButton;
    public Button shipButton;
    public Button factoryButtonOn;
    public Button factoryButtonOff;
    public Button yardButtonOn;
    public Button yardButtonOff;
    public Button shieldButtonOn;
    public Button shieldButtonOff;
    public Button oBButtonOn;
    public Button oBButtonOff;
    public Button researchButtonOn;
    public Button researchButtonOff;
    public Button newFleetButton;
    public Button mergeFleetButton;
    public Button shipDeployButton;
    public Button cancelShipManagerButton;

    [Header("Text")]
    public TextMeshProUGUI headerPowerUnitText;
    public TextMeshProUGUI nameFactory;
    public TextMeshProUGUI numFactoryRatio;
    public TextMeshProUGUI factoryLoad;
    public TextMeshProUGUI shipyardName;
    public TextMeshProUGUI numYardsOnRatio;
    public TextMeshProUGUI yardLoad;
    public TextMeshProUGUI shieldName;
    public TextMeshProUGUI numShieldsRatio;
    public TextMeshProUGUI shieldLoad;
    public TextMeshProUGUI oBName;
    public TextMeshProUGUI numOBRatio;
    public TextMeshProUGUI oBLoad;
    public TextMeshProUGUI researchName;
    public TextMeshProUGUI numResearchRatio;
    public TextMeshProUGUI researchLoad;
    public TextMeshProUGUI powerOverload;

    [Header("Images")]
    public Image powerUnitImage;
    public Image factoryImage;
    public Image shipyardImage;
    public Image shieldPlantImage;
    public Image orbitalBatteriesImage;
    public Image researchImage;
    //public Image powerOverLoad;

    //internal CoroutineRunner coroutineRunner;
    private void Awake()
    {
        facilityUIDictionary = new();
        foreach (var f in facilities)
            facilityUIDictionary[f.type] = f;
        //if (powerOverload != null)
        //powerOverload.SetActive(false);
        //coroutineRunner = CoroutineRunner.Instance;
    }

    public FacilityUI GetFacility(StarSysFacilityType type)
        => facilityUIDictionary[type]; // get facilty UI by type as a StarSysFacilityType enum called in StarSysMenuUIController

    /// <summary>
    /// Populate UI fields from StarSysData. Safe, null-checked and idempotent.
    /// Sets system name, power numbers and each facility's icon/name/load/ratio if available.
    /// </summary>
    /// <param name="data">Star system data used to initialize UI</param>
    public void InitializeFromStarSysData(StarSysData data)
    {
        if (data == null)
            return;

        if (sysName != null)
            sysName.text = data.SysName ?? string.Empty;

        int powerPlantCount = data.PowerPlants?.Count ?? 0;
        int totalOutputPerPlant = data.PowerPlantData?.PowerOutput ?? 0;
        int totalOutput = totalOutputPerPlant * powerPlantCount;

        if (numPUnits != null)
            numPUnits.text = powerPlantCount.ToString();
        if (numTotalEOut != null)
            numTotalEOut.text = totalOutput.ToString();
        if (numPLoad != null)
            numPLoad.text = data.TotalSysPowerLoad.ToString();

        // update each facility UI entry from the data structure
        foreach (var f in facilities)
        {
            if (f == null)
                continue;

            switch (f.type)
            {
                case StarSysFacilityType.PowerPlanet:
                    {
                        var pd = data.PowerPlantData;
                        if (f.icon != null) f.icon.sprite = pd?.PowerPlantSprite;
                        if (f.nameText != null) f.nameText.text = pd?.Name ?? string.Empty;
                        if (f.ratioText != null) f.ratioText.text = powerPlantCount.ToString();
                        if (f.loadText != null) f.loadText.text = (pd?.PowerOutput ?? 0).ToString();
                        break;
                    }
                case StarSysFacilityType.Factory:
                    {
                        var fd = data.FactoryData;
                        var facilities = data.Factories;
                        int total = facilities?.Count ?? 0;
                        int numOn = CountFacilitiesOn(facilities);
                        int loadPerFacility = fd?.PowerLoad ?? 0;

                        if (f.icon != null) f.icon.sprite = fd?.FactorySprite;
                        if (f.nameText != null) f.nameText.text = fd?.Name ?? string.Empty;
                        if (f.ratioText != null) f.ratioText.text = $"{numOn}/{total}";
                        if (f.loadText != null) f.loadText.text = (loadPerFacility * numOn).ToString();
                        break;
                    }
                case StarSysFacilityType.Shipyard:
                    {
                        var sd = data.ShipyardData;
                        var yards = data.Shipyards;
                        int total = yards?.Count ?? 0;
                        int numOn = CountFacilitiesOn(yards);
                        int loadPerFacility = sd?.PowerLoad ?? 0;

                        if (f.icon != null) f.icon.sprite = sd?.ShipyardSprite;
                        if (f.nameText != null) f.nameText.text = sd?.Name ?? string.Empty;
                        if (f.ratioText != null) f.ratioText.text = $"{numOn}/{total}";
                        if (f.loadText != null) f.loadText.text = (loadPerFacility * numOn).ToString();
                        break;
                    }
                case StarSysFacilityType.ShieldGenerator:
                    {
                        var sh = data.ShieldGeneratorData;
                        var shields = data.ShieldGenerators;
                        int total = shields?.Count ?? 0;
                        int numOn = CountFacilitiesOn(shields);
                        int loadPerFacility = sh?.PowerLoad ?? 0;

                        if (f.icon != null) f.icon.sprite = sh?.ShieldGeneratorSprite;
                        if (f.nameText != null) f.nameText.text = sh?.Name ?? string.Empty;
                        if (f.ratioText != null) f.ratioText.text = $"{numOn}/{total}";
                        if (f.loadText != null) f.loadText.text = (loadPerFacility * numOn).ToString();
                        break;
                    }
                case StarSysFacilityType.OrbitalBattery:
                    {
                        var ob = data.OrbitalBatteryData;
                        var obs = data.OrbitalBatteries;
                        int total = obs?.Count ?? 0;
                        int numOn = CountFacilitiesOn(obs);
                        int loadPerFacility = ob?.PowerLoad ?? 0;

                        if (f.icon != null) f.icon.sprite = ob?.OrbitalBatterySprite;
                        if (f.nameText != null) f.nameText.text = ob?.Name ?? string.Empty;
                        if (f.ratioText != null) f.ratioText.text = $"{numOn}/{total}";
                        if (f.loadText != null) f.loadText.text = (loadPerFacility * numOn).ToString();
                        break;
                    }
                case StarSysFacilityType.ResearchCenter:
                    {
                        var rc = data.ResearchCenterData;
                        var rcs = data.ResearchCenters;
                        int total = rcs?.Count ?? 0;
                        int numOn = CountFacilitiesOn(rcs);
                        int loadPerFacility = rc?.PowerLoad ?? 0;

                        if (f.icon != null) f.icon.sprite = rc?.ResearchCenterSprite;
                        if (f.nameText != null) f.nameText.text = rc?.Name ?? string.Empty;
                        if (f.ratioText != null) f.ratioText.text = $"{numOn}/{total}";
                        if (f.loadText != null) f.loadText.text = (loadPerFacility * numOn).ToString();
                        break;
                    }
                default:
                    break;
            }
        }

        // show/hide power overload indicator
        if (PowerOverload != null)
        {
            bool overloaded = data.TotalSysPowerLoad > totalOutput;
            PowerOverload.SetActive(overloaded);
        }
    }

    /// <summary>
    /// Counts how many facilities in the list are turned on (TextMeshProUGUI.text == "1")
    /// </summary>
    private int CountFacilitiesOn(List<GameObject> facilityList)
    {
        if (facilityList == null) return 0;

        int count = 0;
        foreach (var facilityGO in facilityList)
        {
            if (facilityGO == null) continue;

            var tmp = facilityGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null && tmp.text == "1")
                count++;
        }
        return count;
    }
}
