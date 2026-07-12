// Ignore Spelling: Sys

using BOTF3D.Core;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = UnityEngine.UI.Toggle;


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
    public void Initialize() { }
    public void UpdateState() { }
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
    private Dictionary<StarSysFacilityType, FacilityUI> facilityUIDictionary;
    // fields below for dictionary reference
    public RectTransform shipContent;

    [Header("Ship List Expansion")]
    public GameObject ShipScrollView;
    public UnityEngine.UI.Button ExpandShipsButton;
    [Tooltip("Width of the Ship Scroll View in collapsed state (2 columns × 140 px + spacing).")]
    public float CollapsedShipScrollViewWidth = 290f;

    [Header("Build Queue Display")]
    [Tooltip("Assign to YardScroll ViewQueue/Viewport/Content in the prefab.")]
    public Transform yardQueueContent;
    [Tooltip("Assign to FactoryScrollViewQueue/Viewport/Content in the prefab.")]
    public Transform factoryQueueContent;

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
    public Button cargoButton;

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
    public TextMeshProUGUI groundForceName;
    public TextMeshProUGUI numGroundForce;
    public TextMeshProUGUI populationText;

    [Header("Images")]
    public Image powerUnitImage;
    public Image factoryImage;
    public Image shipyardImage;
    public Image shieldPlanetImage;
    public Image orbitalBatteriesImage;
    public Image researchImage;
    public Image groundForceImage;

    [Header("Orbital Battery Grid")]
    [Tooltip("OrbitalBatteryGrid transform (GridLayoutGroup, 32x32 cells, Fixed Column Count 11) that OB_Element icons are instantiated under.")]
    public RectTransform orbitalBatteryContent;
    [Tooltip("OB_Element prefab (Assets/PreFabs/Combat/OB_Element.prefab) instantiated once per orbital battery, up to orbitalBatteryGridLimit.")]
    public GameObject orbitalBatteryIconPrefab;
    [Tooltip("Max number of orbital battery icons shown, matching the grid's Fixed Column Count.")]
    public int orbitalBatteryGridLimit = 11;

    [Header("Ground Force Grid")]
    [Tooltip("GroundForceGrid transform (GridLayoutGroup, 32x32 cells) that GroundForceElement icons are instantiated under.")]
    public RectTransform groundForceContent;
    [Tooltip("GroundForceElement prefab (Assets/PreFabs/Combat/GroundForceElement.prefab) instantiated once per ground force unit, up to groundForceGridLimit.")]
    public GameObject groundForceIconPrefab;
    [Tooltip("Max number of ground force icons shown, matching the grid's Fixed Column Count.")]
    public int groundForceGridLimit = 11;

    [Header("Compact Row")]
    [Tooltip("SysCompactHeader component on the CompactHeader child — always visible in the list.")]
    public SysCompactHeader compactHeader;
    [Tooltip("Root of all original prefab content — toggled by the expand/collapse button.")]
    public GameObject expandedContent;

    [Header("AI Management Toggles")]
    public Toggle toggleOff;
    public Toggle toggleEconomy;
    public Toggle toggleWar;
    public Toggle toggleDefence;

    public void WireAIModeToggles(StarSysController sysCon)
    {
        if (sysCon?.StarSysData == null) return;
        var data = sysCon.StarSysData;
        SetToggleWithoutNotify(toggleOff,     data.AIBuildMode == AIBuildMode.Off);
        SetToggleWithoutNotify(toggleEconomy, data.AIBuildMode == AIBuildMode.Economy);
        SetToggleWithoutNotify(toggleWar,     data.AIBuildMode == AIBuildMode.War);
        SetToggleWithoutNotify(toggleDefence, data.AIBuildMode == AIBuildMode.Defence);
        WireToggle(toggleOff,     () => data.AIBuildMode = AIBuildMode.Off);
        WireToggle(toggleEconomy, () => data.AIBuildMode = AIBuildMode.Economy);
        WireToggle(toggleWar,     () => data.AIBuildMode = AIBuildMode.War);
        WireToggle(toggleDefence, () => data.AIBuildMode = AIBuildMode.Defence);
    }

    private void SetToggleWithoutNotify(Toggle t, bool isOn)
    {
        if (t != null) t.SetIsOnWithoutNotify(isOn);
    }

    private void WireToggle(Toggle t, Action onSelected)
    {
        if (t == null) return;
        t.onValueChanged.RemoveAllListeners();
        t.onValueChanged.AddListener(isOn => { if (isOn) onSelected(); });
    }

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
        {
            Debug.LogWarning("InitializeFromStarSysData: data is NULL!");
            return;
        }

        Debug.Log($"🔧 InitializeFromStarSysData called for system: {data.SysName}");
        Debug.Log($"  Facilities list size: {facilities?.Count ?? 0}");

        if (sysName != null)
            sysName.text = data.SysName ?? string.Empty;

        // ✅ Use pre-calculated values from StarSysData (calculated by UpdateSystemPowerBalance)
        int powerPlantCount = data.PowerPlants?.Count ?? 0;

        if (numPUnits != null)
            numPUnits.text = powerPlantCount.ToString();
        if (numTotalEOut != null)
            numTotalEOut.text = data.TotalSysPowerOutput.ToString(); // ✅ Use stored value, not recalculation
        if (numPLoad != null)
            numPLoad.text = data.TotalSysPowerLoad.ToString();

        // update each facility UI entry from the data structure
        foreach (var f in facilities)
        {
            if (f == null)
            {
                Debug.LogWarning($"  Facility entry is NULL in facilities list!");
                continue;
            }

            Debug.Log($"  Processing facility: {f.type}");

            switch (f.type)
            {
                case StarSysFacilityType.PowerPlanet:
                    {
                        var pd = data.PowerPlantData;
                        if (f.icon != null) f.icon.sprite = pd?.PowerPlantSprite;
                        if (f.nameText != null)
                        {
                            string name = pd?.Name ?? string.Empty;
                            f.nameText.text = name;
                            Debug.Log($"    ✅ Set PowerPlant name to: '{name}'");
                        }
                        else
                        {
                            Debug.LogWarning($"    ❌ PowerPlant nameText is NULL!");
                        }
                        if (numTotalEOut != null) numTotalEOut.text = data.TotalSysPowerOutput.ToString();
                        if (f.ratioText != null) f.ratioText.text = powerPlantCount.ToString();
                        if (f.loadText != null) f.loadText.text = (pd?.BasePowerOutput ?? 0).ToString();
                        break;
                    }
                case StarSysFacilityType.Factory:
                    {
                        var fd = data.FactoryData;
                        var facilityList = data.Factories;
                        int total = facilityList?.Count ?? 0;
                        int numOn = CountFacilitiesOn(facilityList);
                        int loadPerFacility = fd?.PowerLoad ?? 0;

                        if (f.icon != null) f.icon.sprite = fd?.FactorySprite;
                        if (f.nameText != null)
                        {
                            string name = fd?.Name ?? string.Empty;
                            f.nameText.text = name;
                            Debug.Log($"    ✅ Set Factory name to: '{name}' (from FactoryData)");
                        }
                        else
                        {
                            Debug.LogWarning($"    ❌ Factory nameText is NULL!");
                        }
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
                        if (f.nameText != null)
                        {
                            string name = sd?.Name ?? string.Empty;
                            f.nameText.text = name;
                            Debug.Log($"    ✅ Set Shipyard name to: '{name}'");
                        }
                        else
                        {
                            Debug.LogWarning($"    ❌ Shipyard nameText is NULL!");
                        }
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
                        if (f.nameText != null)
                        {
                            string name = sh?.Name ?? string.Empty;
                            f.nameText.text = name;
                            Debug.Log($"    ✅ Set Shield name to: '{name}'");
                        }
                        else
                        {
                            Debug.LogWarning($"    ❌ Shield nameText is NULL!");
                        }
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
                        if (f.nameText != null)
                        {
                            string name = ob?.Name ?? string.Empty;
                            f.nameText.text = name;
                            Debug.Log($"    ✅ Set OrbitalBattery name to: '{name}'");
                        }
                        else
                        {
                            Debug.LogWarning($"    ❌ OrbitalBattery nameText is NULL!");
                        }
                        if (f.ratioText != null) f.ratioText.text = $"{numOn}/{total}";
                        if (f.loadText != null) f.loadText.text = (loadPerFacility * numOn).ToString();

                        SyncOrbitalBatteryGrid(obs, ob?.OrbitalBatterySprite);
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
                        if (f.nameText != null)
                        {
                            string name = rc?.Name ?? string.Empty;
                            f.nameText.text = name;
                            Debug.Log($"    ✅ Set ResearchCenter name to: '{name}'");
                        }
                        else
                        {
                            Debug.LogWarning($"    ❌ ResearchCenter nameText is NULL!");
                        }
                        if (f.ratioText != null) f.ratioText.text = $"{numOn}/{total}";
                        if (f.loadText != null) f.loadText.text = (loadPerFacility * numOn).ToString();
                        break;
                    }
                default:
                    Debug.LogWarning($"  Unknown facility type: {f.type}");
                    break;
            }
        }

        // Ground forces have no power/on-off entry in the facilities list above (no power load,
        // no on/off buttons), so they're synced directly here instead. GroundForceData still has no
        // CSV/SO importer for a designer-authored name/sprite, so those stay blank/prefab-authored
        // until that pipeline exists — only the count/grid and population are live.
        var gfd = data.GroundForceData;
        var groundForces = data.GroundForces;
        int totalGroundForces = groundForces?.Count ?? 0;
        if (groundForceImage != null && gfd?.GroundForceSprite != null) groundForceImage.sprite = gfd.GroundForceSprite;
        // groundForceName is the static "Ground Forces" header baked into the prefab - leave it alone.
        // GroundForceData.Name has no designer-authored pipeline yet (see comment above) and is only
        // ever populated with the "null" placeholder from StarSysManager.AddGroundForceUnit, so
        // displaying it here just replaces the header with the literal text "null".
        if (numGroundForce != null) numGroundForce.text = totalGroundForces.ToString();
        // Population shown here is civilians only (total population minus fielded ground forces).
        // data.Population already excludes GroundForces under the carve-out model in
        // StarSysManager/PopulationManager (ground forces are drawn out of, not added on top of, the
        // population pool), so it doesn't need GroundForces subtracted again here.
        if (populationText != null) populationText.text = data.Population.ToString();
        SyncGroundForceGrid(groundForces);

        // show/hide power overload indicator
        if (PowerOverload != null)
        {
            bool overloaded = data.TotalSysPowerLoad > data.TotalSysPowerOutput;
            PowerOverload.SetActive(overloaded);
        }

        Debug.Log($"🔧 InitializeFromStarSysData complete for {data.SysName}");
    }

    /// <summary>
    /// Keeps one OB_Element icon per orbital battery in orbitalBatteryContent, in sync with
    /// data.OrbitalBatteries, up to orbitalBatteryGridLimit (matches the grid's Fixed Column Count).
    /// Powered-off batteries stay visible but grayed out (see OrbitalBatteryIconUI.SetOnOff) — they
    /// still occupy a grid slot and count toward the limit, same as the old on/off behavior.
    /// Also refreshes the header orbitalBatteriesImage using the sprite already resolved for this
    /// system's owning civ (OrbitalBatteryData.OrbitalBatterySprite), not the globally-selected theme —
    /// otherwise this header would show the local player's theme even for systems owned by another civ.
    /// Called from InitializeFromStarSysData and from StarSysMenuUIController.UpdateFacilityUI (on/off toggles).
    /// </summary>
    public void SyncOrbitalBatteryGrid(List<GameObject> batteries, Sprite headerSprite)
    {
        if (orbitalBatteriesImage != null && headerSprite != null)
            orbitalBatteriesImage.sprite = headerSprite;

        if (orbitalBatteryContent == null || orbitalBatteryIconPrefab == null || batteries == null)
            return;

        var existingIcons = orbitalBatteryContent.GetComponentsInChildren<OrbitalBatteryIconUI>(true);
        var usedIcons = new HashSet<OrbitalBatteryIconUI>();

        int shown = 0;
        foreach (var facilityGO in batteries)
        {
            if (facilityGO == null) continue;
            if (shown >= orbitalBatteryGridLimit) break;

            OrbitalBatteryIconUI iconUI = null;
            foreach (var existing in existingIcons)
            {
                if (existing.SourceFacilityGO == facilityGO)
                {
                    iconUI = existing;
                    break;
                }
            }

            if (iconUI == null)
            {
                var iconGO = Instantiate(orbitalBatteryIconPrefab, orbitalBatteryContent);
                iconUI = iconGO.GetComponent<OrbitalBatteryIconUI>();
                if (iconUI == null)
                {
                    Debug.LogWarning("orbitalBatteryIconPrefab is missing an OrbitalBatteryIconUI component!");
                    continue;
                }
                iconUI.SourceFacilityGO = facilityGO;
            }

            iconUI.gameObject.SetActive(true);
            bool isOn = facilityGO.GetComponent<TextMeshProUGUI>()?.text == "1";
            iconUI.SetOnOff(isOn);
            usedIcons.Add(iconUI);
            shown++;
        }

        // Hide any leftover icons beyond the grid limit or for batteries no longer in the list
        foreach (var existing in existingIcons)
        {
            if (!usedIcons.Contains(existing))
                existing.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Keeps one GroundForceElement icon per ground force unit in groundForceContent, in sync with
    /// data.GroundForces, up to groundForceGridLimit. Ground forces have no power load and no on/off
    /// state, so every icon shown is simply active — there is no gray-out step like orbital batteries.
    /// Called from InitializeFromStarSysData.
    /// </summary>
    public void SyncGroundForceGrid(List<GameObject> groundForces)
    {
        if (groundForceContent == null || groundForceIconPrefab == null || groundForces == null)
            return;

        var existingIcons = groundForceContent.GetComponentsInChildren<GroundForceIconUI>(true);
        var usedIcons = new HashSet<GroundForceIconUI>();

        int shown = 0;
        foreach (var facilityGO in groundForces)
        {
            if (facilityGO == null) continue;
            if (shown >= groundForceGridLimit) break;

            GroundForceIconUI iconUI = null;
            foreach (var existing in existingIcons)
            {
                if (existing.SourceFacilityGO == facilityGO)
                {
                    iconUI = existing;
                    break;
                }
            }

            if (iconUI == null)
            {
                var iconGO = Instantiate(groundForceIconPrefab, groundForceContent);
                iconUI = iconGO.GetComponent<GroundForceIconUI>();
                if (iconUI == null)
                {
                    Debug.LogWarning("groundForceIconPrefab is missing a GroundForceIconUI component!");
                    continue;
                }
                iconUI.SourceFacilityGO = facilityGO;
            }

            iconUI.gameObject.SetActive(true);
            usedIcons.Add(iconUI);
            shown++;
        }

        // Hide any leftover icons beyond the grid limit or for units no longer in the list
        foreach (var existing in existingIcons)
        {
            if (!usedIcons.Contains(existing))
                existing.gameObject.SetActive(false);
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

    private void OnDestroy()
    {
        //Debug.LogError($"❌❌❌ StarSysUI_Fields '{gameObject.name}' IS BEING DESTROYED!");
        //Debug.LogError($"  System name: {sysName?.text ?? "UNKNOWN"}");
        //Debug.LogError($"  Stack trace:\n{System.Environment.StackTrace}");
    }
}
