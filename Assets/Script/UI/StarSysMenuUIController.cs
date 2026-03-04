// Ignore Spelling: Sys Anya

using BOTF3D.Core;
using BOTF3D.GamePlay;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// The UI controller owns hierarchy and presentation.
    /// </summary>
    public class StarSysMenuUIController : MonoBehaviour
    {
        public static StarSysMenuUIController Instance;
        private StarSysController lastSysCon;
        private StarSysController activeStarSysController;
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
        public GameObject PowerOverloadImage;
        public Slider ShipSliderBuildProgress;
        public Slider SliderBuildProgress;

        private void Awake()
        {
            // ✅ Simple scene-based singleton - no DontDestroyOnLoad!
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("✅ StarSysMenuUIController: Instance assigned");
            }
            else if (Instance != this)
            {
                Debug.LogWarning($"❌ Duplicate StarSysMenuUIController found! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // DON'T call FindSysUIContainers() here - GalaxyScene doesn't exist yet!
            // It will be called from MainMenuUIController after scene loads

            // Record the original parent of each StarSysUIGameObject
            // This might be empty initially - systems are created later
            if (StarSysManager.Instance != null)
            {
                for (int i = 0; i < StarSysManager.Instance.StarSysControllerList.Count; i++)
                {
                    var sysCon = StarSysManager.Instance.StarSysControllerList[i];
                    if (sysCon != null && sysCon.StarSysUIGameObject != null)
                    {
                        var child = sysCon.StarSysUIGameObject;
                        var childController = child.GetComponent<FleetAndSystemChildController>();
                        if (childController != null && childController.OriginalParentTransform == null)
                        {
                            if (child.transform.parent != null)
                            {
                                childController.OriginalParentTransform = child.transform.parent;
                            }
                            else if (SysListContainer != null)
                            {
                                childController.OriginalParentTransform = SysListContainer.transform;
                            }
                            else if (ASystemMenuView != null)
                            {
                                childController.OriginalParentTransform = ASystemMenuView.transform;
                            }
                        }
                    }
                }
            }

            // Initially hide views (they might not exist yet)
            if (SystemsMenuView != null)
                SystemsMenuView.SetActive(false);
            if (ASystemMenuView != null)
                ASystemMenuView.SetActive(false);
        }
        public void SetUIReferences(GameObject systemListContainer, GameObject canvasGalaxy)
        {
            this.SysListContainer = systemListContainer;
            // Store canvasGalaxy reference if needed by the class
        }
        /// <summary>
        /// Shows the scrollable list view of all local player's systems
        /// CALLED BY: GalaxyMenuUIController.SystemButtonPressed()
        /// </summary>
        public void ShowSystemMenuView()
        {
            Debug.Log("=== ShowSystemMenuView: Starting ===");

            if (SystemsMenuView == null || SysListContainer == null)
            {
                FindSysUIContainers();
            }

            if (SystemsMenuView == null)
            {
                Debug.LogError("ShowSystemMenuView: SystemsMenuView is NULL!");
                return;
            }

            // ✅ Move all local player's system UIs to the scrollable SysListContainer
            if (StarSysManager.Instance != null)
            {
                foreach (var sysCon in StarSysManager.Instance.StarSysControllerList)
                {
                    if (sysCon == null || sysCon.StarSysUIGameObject == null) continue;

                    // Only show local player's systems
                    if (!GameController.Instance.AreWeLocalPlayer(sysCon.StarSysData.CurrentOwnerCivEnum))
                        continue;

                    // ✅ Move to scrollable list container and ACTIVATE
                    sysCon.StarSysUIGameObject.transform.SetParent(SysListContainer.transform, false);
                    sysCon.StarSysUIGameObject.SetActive(true); // ✅ CRITICAL: Activate when displaying!
                }
            }

            SystemsMenuView.SetActive(true);
            Debug.Log("  SystemMenuView activated with scrollable list");

            SetupSystemUIData();

            Debug.Log("=== ShowSystemMenuView: Complete ===");
        }

        /// <summary>
        /// Hides the scrollable list view and moves UIs back to home storage
        /// CALLED BY: GalaxyMenuUIController.CloseMenu(Menu.SystemMenu)
        /// </summary>
        public void HideSystemMenuView()
        {
            if (SystemsMenuView == null)
            {
                Debug.LogWarning("HideSystemMenuView: SystemsMenuView is null, skipping");
                return;
            }

            // ✅ Move all system UIs back to home storage
            MoveSystemsToHomeStorage();

            SystemsMenuView.SetActive(false);
            Debug.Log("SystemMenuView hidden, UIs moved back to storage");
        }

        /// <summary>
        /// Shows the detailed view of a single system
        /// CALLED BY: GalaxyMenuUIController.OpenMenu(Menu.ASystemMenu) when clicking a system
        /// </summary>
        public void ShowA_SystemMenuView()
        {
            if (ASystemMenuView == null)
            {
                Debug.LogWarning("ShowA_SystemMenuView: ASystemMenuView is null, skipping");
                return;
            }

            ASystemMenuView.SetActive(true);
            Debug.Log("ASystemMenuView shown (single system detail)");
        }

        /// <summary>
        /// Hides the single system detail view and moves UI back to home storage
        /// CALLED BY: GalaxyMenuUIController.CloseMenu(Menu.ASystemMenu)
        /// </summary>
        public void HideA_SystemMenuView()
        {
            if (ASystemMenuView == null)
            {
                Debug.LogWarning("HideA_SystemMenuView: ASystemMenuView is null, skipping");
                return;
            }

            // ✅ Move system UI from detail view back to home storage
            MoveSystemsToHomeStorage();

            ASystemMenuView.SetActive(false);
            Debug.Log("ASystemMenuView hidden (single system detail)");
        }

        /// <summary>
        /// Wires buttons and updates UI data for all visible systems
        /// CALLED BY: ShowSystemMenuView() after systems are parented
        /// </summary>
        public void SetupSystemUIData()
        {
            Debug.Log("SetupSystemUIData: Wiring buttons and updating data");

            if (StarSysManager.Instance == null)
            {
                Debug.LogError("  StarSysManager.Instance is null!");
                return;
            }

            if (SysListContainer == null)
            {
                FindSysUIContainers();

                if (SysListContainer == null)
                {
                    Debug.LogError("  SysListContainer is null! Cannot display systems.");
                    return;
                }
            }

            var systems = StarSysManager.Instance.StarSysControllerList;
            if (systems == null || systems.Count == 0)
            {
                Debug.LogWarning("  No systems in StarSysManager!");
                return;
            }

            int setupCount = 0;

            foreach (var sysCon in systems)
            {
                if (sysCon == null || sysCon.StarSysUIGameObject == null) continue;

                // Only setup local player's systems
                if (!GameController.Instance.AreWeLocalPlayer(sysCon.StarSysData.CurrentOwnerCivEnum))
                    continue;

                var sysUIFieldElement = sysCon.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();
                if (sysUIFieldElement == null)
                {
                    Debug.LogWarning($"  System '{sysCon.name}' UI has no StarSysUI_Fields component - skipping");
                    continue;
                }

                // ✅ Set ShipListUIParent if not already set
                if (sysUIFieldElement.shipContent != null && sysCon.StarSysData.ShipListUIParent == null)
                {
                    sysCon.StarSysData.ShipListUIParent = sysUIFieldElement.shipContent.gameObject;
                }

                // ✅ FIRST TIME ONLY: Wire buttons and set original parent
                if (!listOfStarSysUiGos.Contains(sysCon.StarSysUIGameObject))
                {
                    // Set OriginalParentTransform to home storage
                    var childController = sysCon.StarSysUIGameObject.GetComponent<FleetAndSystemChildController>();
                    if (childController != null)
                    {
                        // ✅ Use StarSysManager's container as the original parent
                        if (StarSysManager.Instance.StarSysUI_ListContainer != null)
                        {
                            childController.OriginalParentTransform = StarSysManager.Instance.StarSysUI_ListContainer.transform;
                        }
                        else
                        {
                            childController.OriginalParentTransform = SysListContainer.transform; // Fallback
                        }
                    }

                    // Position red dot on mini-map
                    if (sysUIFieldElement.redDot != null)
                    {
                        sysUIFieldElement.redDot.anchoredPosition = new Vector2(
                            sysCon.StarSysData.GetPosition().x * 0.12f,
                            sysCon.StarSysData.GetPosition().z * 0.12f);
                    }

                    // Wire all buttons (BuildButton, ShipButton, etc.)
                    WireSystemUIButtons(sysCon, sysUIFieldElement);

                    // Add to tracking list
                    listOfStarSysUiGos.Add(sysCon.StarSysUIGameObject);
                }

                // ✅ EVERY TIME: Update facility data
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.Factory);
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.Shipyard);
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.ShieldGenerator);
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.OrbitalBattery);
                UpdateFacilityUI(sysCon, 0, StarSysFacilityType.ResearchCenter);

                // Update from StarSysData
                try
                {
                    sysUIFieldElement.InitializeFromStarSysData(sysCon.StarSysData);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"SetupSystemUIData: InitializeFromStarSysData failed for {sysCon.name}: {ex.Message}");
                }

                setupCount++;
            }

            Debug.Log($"SetupSystemUIData: Setup {setupCount} systems");
        }

        /// <summary>
        /// Helper: Wires all buttons on a system UI (called once per UI)
        /// </summary>
        private void WireSystemUIButtons(StarSysController sysCon, StarSysUI_Fields fields)
        {
            // Hide cancel button initially
            if (fields.cancelShipManagerButton != null)
            {
                fields.cancelShipManagerButton.gameObject.SetActive(false);
            }

            // Wire action buttons
            if (fields.buildButton != null)
            {
                fields.buildButton.onClick.RemoveAllListeners();
                fields.buildButton.onClick.AddListener(() => sysCon.BuildClick(sysCon));
            }

            if (fields.shipButton != null)
            {
                fields.shipButton.onClick.RemoveAllListeners();
                fields.shipButton.onClick.AddListener(() => sysCon.ShipClick(sysCon));
            }

            if (fields.shipDeployButton != null)
            {
                fields.shipDeployButton.onClick.RemoveAllListeners();
                fields.shipDeployButton.onClick.AddListener(() => StarSysClickShipDeployButton(sysCon));
            }

            if (fields.newFleetButton != null)
            {
                fields.newFleetButton.onClick.RemoveAllListeners();
                fields.newFleetButton.onClick.AddListener(() => ClickNewFleetButton(sysCon));
            }

            if (fields.mergeFleetButton != null)
            {
                fields.mergeFleetButton.onClick.RemoveAllListeners();
                fields.mergeFleetButton.onClick.AddListener(() => StarSysClickMergeShipsButton(sysCon));
            }

            // Wire facility On/Off buttons
            WireFacilityButton(fields.factoryButtonOn, SystemOnOffButtons.FactoryOnButton, () => sysCon.FactoryButtonOnClicked(sysCon));
            WireFacilityButton(fields.factoryButtonOff, SystemOnOffButtons.FactoryOffButton, () => sysCon.FactoryButtonOffClicked(sysCon));

            WireFacilityButton(fields.yardButtonOn, SystemOnOffButtons.ShipyardOnButton, () => sysCon.YardButtonOnClicked(sysCon));
            WireFacilityButton(fields.yardButtonOff, SystemOnOffButtons.ShipyardOffbutton, () => sysCon.YardButtonOffClicked(sysCon));

            WireFacilityButton(fields.shieldButtonOn, SystemOnOffButtons.ShieldGeneratorOnButton, () => sysCon.ShieldButtonOnClicked(sysCon));
            WireFacilityButton(fields.shieldButtonOff, SystemOnOffButtons.ShieldGeneratorOffbutton, () => sysCon.ShieldButtonOffClicked(sysCon));

            WireFacilityButton(fields.oBButtonOn, SystemOnOffButtons.OrbitalBatteryOnButton, () => sysCon.OBButtonOnClicked(sysCon));
            WireFacilityButton(fields.oBButtonOff, SystemOnOffButtons.OrbitalBatteryOffButton, () => sysCon.OBButtonOffClicked(sysCon));

            WireFacilityButton(fields.researchButtonOn, SystemOnOffButtons.ResearchCenterOnButton, () => sysCon.ResearchButtonOnClicked(sysCon));
            WireFacilityButton(fields.researchButtonOff, SystemOnOffButtons.ResearchCenterOffButton, () => sysCon.ResearchButtonOffClicked(sysCon));

            // Set theme images
            if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
            {
                if (fields.powerUnitImage != null)
                    fields.powerUnitImage.sprite = ThemeManager.Instance.CurrentTheme.PowerPlantImage;
                if (fields.factoryImage != null)
                    fields.factoryImage.sprite = ThemeManager.Instance.CurrentTheme.FactoryImage;
                if (fields.shipyardImage != null)
                    fields.shipyardImage.sprite = ThemeManager.Instance.CurrentTheme.ShipyardImage;
                if (fields.shieldPlantImage != null)
                    fields.shieldPlantImage.sprite = ThemeManager.Instance.CurrentTheme.ShieldImage;
                if (fields.orbitalBatteriesImage != null)
                    fields.orbitalBatteriesImage.sprite = ThemeManager.Instance.CurrentTheme.OrbitalBatteriesImage;
                if (fields.researchImage != null)
                    fields.researchImage.sprite = ThemeManager.Instance.CurrentTheme.ResearchCenterImage;
            }

            // Assign PowerOverloadImage from first system (singleton pattern)
            if (PowerOverloadImage == null && fields.PowerOverload != null)
            {
                PowerOverloadImage = fields.PowerOverload;
            }
        }

        /// <summary>
        /// Helper: Wires a single facility button with its component and listener
        /// </summary>
        private void WireFacilityButton(Button button, SystemOnOffButtons buttonType, UnityEngine.Events.UnityAction listener)
        {
            if (button == null) return;

            // Set SysButtonOnOff component type if exists
            var comp = button.GetComponent<SysButtonOnOff>();
            if (comp != null)
            {
                comp.button = buttonType;
            }

            // Wire click listener
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(listener);
        }

        /// <summary>
        /// Moves all system UIs back to home storage (StarSysUI_ListContainer)
        /// CALLED BY: HideSystemMenuView(), HideA_SystemMenuView(), MoveBackAnyStarSysUIGO()
        /// </summary>
        private void MoveSystemsToHomeStorage()
        {
            Debug.Log("MoveSystemsToHomeStorage: Starting");

            // ✅ Get home storage container from StarSysManager
            GameObject homeContainer = StarSysManager.Instance?.StarSysUI_ListContainer;

            if (homeContainer == null)
            {
                Debug.LogWarning("  ⚠️ StarSysUI_ListContainer not found! Systems will remain where they are.");
                return;
            }

            int movedCount = 0;

            // Move from ASystemMenuView (detail view)
            if (ASystemMenuView != null)
            {
                for (int i = ASystemMenuView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = ASystemMenuView.transform.GetChild(i);
                    if (child == null) continue;

                    var starSysUIFields = child.GetComponent<StarSysUI_Fields>();
                    if (starSysUIFields != null)
                    {
                        child.SetParent(homeContainer.transform, false);
                        child.gameObject.SetActive(false); // Deactivate when in storage
                        movedCount++;
                    }
                }
            }

            // Move from SysListContainer (scalable list view)
            if (SysListContainer != null && SysListContainer != homeContainer)
            {
                for (int i = SysListContainer.transform.childCount - 1; i >= 0; i--)
                {
                    var child = SysListContainer.transform.GetChild(i);
                    if (child == null) continue;

                    var starSysUIFields = child.GetComponent<StarSysUI_Fields>();
                    if (starSysUIFields != null)
                    {
                        child.SetParent(homeContainer.transform, false);
                        child.gameObject.SetActive(false);
                        movedCount++;
                    }
                }
            }

            // ✅ Check for stray system UIs in fleet views (error case)
            var aFleetView = FleetMenuUIController.Instance?.AFleetMenuView;
            if (aFleetView != null)
            {
                for (int i = aFleetView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = aFleetView.transform.GetChild(i);
                    if (child == null) continue;

                    var starSysUIFields = child.GetComponent<StarSysUI_Fields>();
                    if (starSysUIFields != null)
                    {
                        Debug.LogError($"  ❌ SYSTEM UI '{child.name}' found in AFleetMenuView! Moving to home storage.");
                        child.SetParent(homeContainer.transform, false);
                        child.gameObject.SetActive(false); // ✅ CRITICAL: Deactivate!
                        movedCount++;
                    }
                }
            }

            Debug.Log($"MoveSystemsToHomeStorage: Moved {movedCount} UIs to home storage");
        }

        public void SetActiveSetParentUIGO(StarSysController theSysCon)
        {
            // CRITICAL: Find containers if needed
            if (SysListContainer == null || ASystemMenuView == null)
            {
                FindSysUIContainers();
            }

            // ✅ NEW: Hide any open fleet UIs first (mutual exclusion)
            if (FleetMenuUIController.Instance != null)
            {
                Debug.Log("  Hiding any open fleet UIs before showing star system UI");
                FleetMenuUIController.Instance.MoveBackAnyaFleetUIGO();
            }

            SetupSystemUIData();

            if (theSysCon == null)
            {
                Debug.LogWarning("SetActiveSetParentUIGO: theSysCon is null");
                return;
            }

            // ✅ Check if StarSysUIGameObject is null
            if (theSysCon.StarSysUIGameObject == null)
            {
                Debug.LogError($"SetActiveSetParentUIGO: StarSysUIGameObject is null for system '{theSysCon.name}'!");
                return;
            }

            // Check ASystemMenuView exists
            if (ASystemMenuView == null)
            {
                Debug.LogError("SetActiveSetParentUIGO: ASystemMenuView is null!");
                return;
            }

            // ✅ Move to detail view and ACTIVATE
            try
            {
                theSysCon.StarSysUIGameObject.transform.SetParent(ASystemMenuView.transform, false);
                theSysCon.StarSysUIGameObject.SetActive(true); // ✅ Ensure it's active
                lastSysCon = theSysCon;

                Debug.Log($"SetActiveSetParentUIGO: Successfully displayed system '{theSysCon.name}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SetActiveSetParentUIGO: Exception displaying system '{theSysCon.name}': {ex.Message}");
                return;
            }

            // ✅ Update PowerOverloadImage to this system's power overload visual
            var sysUIFieldElement = theSysCon.StarSysUIGameObject?.GetComponent<StarSysUI_Fields>();
            if (sysUIFieldElement != null && sysUIFieldElement.PowerOverload != null)
            {
                PowerOverloadImage = sysUIFieldElement.PowerOverload;
            }
        }

        public void CloseBuildingQueues()
        {
            GalaxyMenuUIController.Instance.CloseMenu(Menu.BuildMenu);
            GalaxyMenuUIController.Instance.CloseMenu(Menu.ASystemMenu);
            if (lastSysCon != null)
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
            var fields = sysController.StarSysUIGameObject.GetComponent<StarSysUI_Fields>();

            if (fields == null)
            {
                Debug.LogWarning($"UpdateFacilityUI: StarSysUI_Fields not found on {sysController.name}");
                return;
            }

            int newFacilityLoad = 0;
            int numOn = 0;
            int numOff = 0;
            List<GameObject> facilities = new List<GameObject>();

            Button onButton = null;
            Button offButton = null;

            switch (facilityType)
            {
                case StarSysFacilityType.Factory:
                    newFacilityLoad = sysController.StarSysData.FactoryData.PowerLoad;
                    facilities = sysController.StarSysData.Factories;
                    onButton = fields.factoryButtonOn;
                    offButton = fields.factoryButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.Factory, facilities);
                    numOff = facilities.Count - numOn;
                    fields.numFactoryRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.factoryLoad.text = (newFacilityLoad * numOn).ToString();
                    break;
                case StarSysFacilityType.Shipyard:
                    newFacilityLoad = sysController.StarSysData.ShipyardData.PowerLoad;
                    facilities = sysController.StarSysData.Shipyards;
                    onButton = fields.yardButtonOn;
                    offButton = fields.yardButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.Shipyard, facilities);
                    numOff = facilities.Count - numOn;

                    Debug.Log($"📊 UpdateFacilityUI: {facilityType} - Total: {facilities.Count}, On: {numOn}, Off: {numOff}");

                    fields.numYardsOnRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.yardLoad.text = (newFacilityLoad * numOn).ToString();

                    Debug.Log($"✏️ UpdateFacilityUI: Set ratio text to '{fields.numYardsOnRatio.text}'");
                    break;
                case StarSysFacilityType.ShieldGenerator:
                    newFacilityLoad = sysController.StarSysData.ShieldGeneratorData.PowerLoad;
                    facilities = sysController.StarSysData.ShieldGenerators;
                    onButton = fields.shieldButtonOn;
                    offButton = fields.shieldButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.ShieldGenerator, facilities);
                    numOff = facilities.Count - numOn;
                    fields.numShieldsRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.shieldLoad.text = (newFacilityLoad * numOn).ToString();
                    break;
                case StarSysFacilityType.OrbitalBattery:
                    newFacilityLoad = sysController.StarSysData.OrbitalBatteryData.PowerLoad;
                    facilities = sysController.StarSysData.OrbitalBatteries;
                    onButton = fields.oBButtonOn;
                    offButton = fields.oBButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.OrbitalBattery, facilities);
                    numOff = facilities.Count - numOn;
                    fields.numOBRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.oBLoad.text = (newFacilityLoad * numOn).ToString();
                    break;
                case StarSysFacilityType.ResearchCenter:
                    newFacilityLoad = sysController.StarSysData.ResearchCenterData.PowerLoad;
                    facilities = sysController.StarSysData.ResearchCenters;
                    onButton = fields.researchButtonOn;
                    offButton = fields.researchButtonOff;
                    numOn = NumFacilitiesTurnedOn(StarSysFacilityType.ResearchCenter, facilities);
                    numOff = facilities.Count - numOn;
                    fields.numResearchRatio.text = numOn.ToString() + "/" + (facilities.Count).ToString();
                    fields.researchLoad.text = (newFacilityLoad * numOn).ToString();
                    break;
                default:
                    break;
            }

            // ✅ NEW: Hide/Show On button based on whether there are facilities to turn on
            if (onButton != null)
            {
                onButton.gameObject.SetActive(numOff > 0); // Show only if there are facilities that are OFF
            }

            // ✅ NEW: Hide/Show Off button based on whether there are facilities to turn off
            if (offButton != null)
            {
                offButton.gameObject.SetActive(numOn > 0); // Show only if there are facilities that are ON
            }
        }

        private int NumFacilitiesTurnedOn(StarSysFacilityType factory, List<GameObject> facilities) //, StarSysController sysController, ref int numOn, ref int newFacilityLoad, StarSysUI_Fields fields)
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
            //if (load > output)
            //    CoroutineRunner.FlashPowerOverload();
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

        internal void AddSysFacility(StarSysController controller, GameObject facilityGO, string loadName, string ratioName, StarSysFacilityType facilityType)
        {
            Debug.Log($"🏗️ AddSysFacility CALLED: {facilityType} for system {controller?.name}");

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
                        newFacilityLoad = starSysData.PowerPlantData?.PowerOutput ?? 0;
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
                if (facilityGO != null && !facilities.Contains(facilityGO))
                {
                    facilities.Add(facilityGO);
                    Debug.Log($"✅ AddSysFacility: Added {facilityType} to {controller.name}. New count: {facilities.Count}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ AddSysFacility: Facility already in list or null for {controller.name}");
                }

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

                        // ✅ MOVED: Update system power balance and facility UI
                        UpdateSystemPowerBalance(controller);
                        UpdateFacilityUI(controller, 0, facilityType);
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

                    // ✅ Update both power balance and facility UI in fallback path too
                    UpdateSystemPowerBalance(controller);
                    UpdateFacilityUI(controller, 0, facilityType);
                }
                else
                {
                    Debug.LogWarning($"AddSysFacility fallback: StarSysUIGameObject is null for {controller.name} and typed UI update failed.");
                }
            }

            // ✅ At the very end, force UI rebuild
            UpdateSystemPowerBalance(controller);
            UpdateFacilityUI(controller, 0, facilityType);

            // Force layout rebuild
            if (controller.StarSysUIGameObject != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
                    controller.StarSysUIGameObject.GetComponent<RectTransform>());
            }
        }

        private void OnDisable()
        {
            // When the UI menu closes (e.g., switching menus or hiding canvas)
            CleanupDestroyedOrInactiveUIs();
        }

        // Only destroy system UIs when:
        // 1. Scene unload - DON'T DO THIS! Let Unity handle it
        private void OnDestroy()
        {
            // ✅ Only clear Instance if we're the current instance
            if (Instance == this)
            {
                Debug.LogWarning("⚠️ StarSysMenuUIController SINGLETON is being destroyed!");
                Debug.LogWarning($"  GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
                Debug.LogWarning($"  Stack trace:\n{System.Environment.StackTrace}");

                Instance = null;
            }
            else
            {
                Debug.Log("StarSysMenuUIController: Duplicate instance destroyed (this is normal)");
            }

            // ❌ NEVER destroy system UIs here - they belong to the scene!
        }

        // 2. Save/load game (recreate galaxy)
        public void OnLoadGame()
        {
            Debug.LogWarning("⚠️ OnLoadGame: Clearing all system UIs for galaxy rebuild");
            ClearAllStarSysUiGos(); // This is OK - we're rebuilding the galaxy

            // Recreate UIs will happen in SetupSystemUIData()
        }
        private void CleanupDestroyedOrInactiveUIs()
        {
            // Inactive UIs are VALID - they're just in hidden containers
            listOfStarSysUiGos.RemoveAll(go => go == null);

            Debug.Log($"CleanupDestroyedOrInactiveUIs: Removed destroyed entries, {listOfStarSysUiGos.Count} valid UIs remain");
        }

        /// <summary>
        /// ONLY call this on scene unload or new game!
        /// </summary>
        private void ClearAllStarSysUiGos()
        {
            // ✅ LOG STACK TRACE TO FIND WHAT'S CALLING THIS
            Debug.LogError("❌❌❌ ClearAllStarSysUiGos CALLED! This should ONLY happen on scene unload/load game!");
            Debug.LogError($"Stack trace:\n{System.Environment.StackTrace}");

            foreach (var go in listOfStarSysUiGos)
            {
                if (go != null)
                    Destroy(go);
            }
            listOfStarSysUiGos.Clear();

            // ✅ CRITICAL: Also clear references in StarSysControllers
            if (StarSysManager.Instance != null)
            {
                foreach (var sysCon in StarSysManager.Instance.StarSysControllerList)
                {
                    if (sysCon != null)
                    {
                        sysCon.StarSysUIGameObject = null;
                    }
                }
            }
        }

        private void StarSysClickShipDeployButton(StarSysController sysController)
        {
            var galaxyUI = GalaxyMenuUIController.Instance;
            if (galaxyUI != null)
            {
                galaxyUI.WhatSystIsLookingForShipDeploy(sysController);
                galaxyUI.SetClickMode(GalaxyClickMode.SelectForShipDeploy);
                MousePointerChanger.Instance.SetShipExchangeCursor();
                ShipDeployMenuUIController.Instance.TopStarSyst = sysController;
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
            //fleetData.FleetInt = fleetManager.GetNewFleetInt(thisCivData.CivEnum);
            //fleetData.Name = $"{thisCivData.CivShortName} Fleet {fleetData.FleetInt}";
            fleetData.Insignia = thisCivData.InsigniaSprite;
            fleetData.ShipsList = new List<ShipController>();
            var galaxyMenuUICon = GalaxyMenuUIController.Instance;
            galaxyMenuUICon.ResetClickMode();

            var newFleet = fleetManager.InstantiateFleet(null, sysController, fleetData, position, true);
            tempFleetController = newFleet;
            galaxyMenuUICon.ShowShipDeployForSystemNewFleet(sysController, newFleet);

        }
        public void ClickCancelShipManageButton()
        {
            var sd = ShipDeployMenuUIController.Instance;
            var galaxyUI = GalaxyMenuUIController.Instance;

            // Check if we're in merge mode
            bool isMergeMode = (galaxyUI.FleetLookingForShipMerge != null || galaxyUI.StarSystLookingForShipMerge != null);

            if (sd != null && sd.ShipDeployPanel != null && sd.ShipDeployPanel.activeInHierarchy)
            {
                if (isMergeMode)
                {
                    // Use merge commit for merge operations
                    sd.CommitMergeAndClose(CancelShipManageAfterCommit);
                }
                else
                {
                    // Use deploy commit for normal deploy operations
                    sd.CommitShipDeployForNewFleetAndClose(CancelShipManageAfterCommit);
                }

                return;
            }

            // Normal path
            CancelShipManageAfterCommit();
        }

        // New: run the cleanup logic *after* a commit has completed.
        public void CancelShipManageAfterCommit()
        {
            if (tempFleetController == null) return;

            Debug.Log($"CancelShipManageAfterCommit (System): tempFleetController '{tempFleetController.name}' has {tempFleetController.FleetData.ShipsList.Count} ships");

            // Only destroy the fleet if it has NO ships
            if (tempFleetController.FleetData.ShipsList.Count == 0)
            {
                Debug.Log($"Destroying empty fleet '{tempFleetController.name}'");

                if (FleetManager.Instance.TempFogRevealerFleet != null)
                    FleetManager.Instance.RemoveFogWarRevealer(FleetManager.Instance.TempFogRevealerFleet);
                FleetManager.Instance.TempFogRevealerFleet = null;

                FleetManager.Instance.DestroyFleetController(tempFleetController);
                tempFleetController = null;
            }
            else
            {
                Debug.Log($"Keeping fleet '{tempFleetController.name}' with {tempFleetController.FleetData.ShipsList.Count} ships");
                // Fleet has ships, so finalize it and keep it
                tempFleetController = null; // Clear temp reference but don't destroy
            }

            var galaxyUI = GalaxyMenuUIController.Instance;
            MousePointerChanger.Instance.ResetCursor();
            if (cancelShipManagerButtonGO != null)
                cancelShipManagerButtonGO.SetActive(false);
            if (ShipDeployMenuUIController.Instance != null)
                ShipDeployMenuUIController.Instance.gameObject.SetActive(false);
            if (galaxyUI != null)
            {
                galaxyUI.ClickCancelShipDeployButton();
                galaxyUI.ResetClickMode();
                galaxyUI.CompleteShipExchange();
            }

            HideA_SystemMenuView();
        }

        /// <summary>
        /// Sets the build progress slider for facility construction.
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1.</param>
        public void SetBuildProgress(float progress)
        {
            if (SliderBuildProgress != null)
            {
                SliderBuildProgress.value = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Sets the build progress slider for ship construction.
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1.</param>
        public void SetShipBuildProgress(float progress)
        {
            if (ShipSliderBuildProgress != null)
            {
                ShipSliderBuildProgress.value = Mathf.Clamp01(progress);
            }
        }

        public void FindSysUIContainers()
        {
            if (SystemsMenuView != null && ASystemMenuView != null && SysListContainer != null)
            {
                Debug.Log("StarSysMenuUIController: All containers already assigned");
                return;
            }

            var canvasGalaxy = GameObject.Find("CanvasGalaxy");
            if (canvasGalaxy == null)
            {
                Debug.LogWarning("StarSysMenuUIController: CanvasGalaxy not found");
                return;
            }

            // ✅ Find SystemsMenuView (list view with scroll)
            if (SystemsMenuView == null)
            {
                SystemsMenuView = FindInHierarchy(canvasGalaxy.transform, "SystemsMenuView");
                Debug.Log($"StarSysMenuUIController: Found SystemsMenuView: {SystemsMenuView != null}");
            }

            // ✅ Find ASystemMenuView (single system detail view)
            if (ASystemMenuView == null)
            {
                ASystemMenuView = FindInHierarchy(canvasGalaxy.transform, "ASystemMenuView");
                Debug.Log($"StarSysMenuUIController: Found ASystemMenuView: {ASystemMenuView != null}");
            }

            // ✅ Find SysListContainer (INSIDE SystemsMenuView/Viewport)
            if (SysListContainer == null && SystemsMenuView != null)
            {
                // Try to find inside SystemsMenuView first
                SysListContainer = FindInHierarchy(SystemsMenuView.transform, "SysListContainer");

                if (SysListContainer != null)
                {
                    Debug.Log($"StarSysMenuUIController: ✅ Found SysListContainer inside SystemsMenuView");
                }
                else
                {
                    // Fallback: search entire CanvasGalaxy
                    SysListContainer = FindInHierarchy(canvasGalaxy.transform, "SysListContainer");
                    Debug.Log($"StarSysMenuUIController: Found SysListContainer (fallback): {SysListContainer != null}");
                }
            }
        }

        private GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name)
                return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject found = FindInHierarchy(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
        public void MoveBackAnyStarSysUIGO()
        {
            Debug.Log("=== MoveBackAnyStarSysUIGO: Starting ===");

            // ✅ Clean up destroyed references
            listOfStarSysUiGos.RemoveAll(go => go == null);
            Debug.Log($"  Cleaned tracking list, now has {listOfStarSysUiGos.Count} valid entries");

            // ✅ Get home storage container from StarSysManager
            GameObject homeContainer = StarSysManager.Instance?.StarSysUI_ListContainer;

            if (homeContainer == null)
            {
                Debug.LogWarning("  ⚠️ StarSysUI_ListContainer not found! UIs will remain visible.");
                return;
            }

            // Move from ASystemMenuView (detail view)
            if (ASystemMenuView != null)
            {
                Debug.Log($"  Checking ASystemMenuView ({ASystemMenuView.transform.childCount} children)");

                for (int i = ASystemMenuView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = ASystemMenuView.transform.GetChild(i);
                    if (child == null) continue;

                    // ✅ Check if this is a SYSTEM UI (not a fleet UI!)
                    var starSysUIFields = child.GetComponent<StarSysUI_Fields>();
                    var fleetUIFields = child.GetComponent<FleetUI_Fields>();

                    if (starSysUIFields != null && fleetUIFields == null)
                    {
                        // This is a star system UI - move to home and DEACTIVATE
                        Debug.Log($"    Moving SYSTEM UI '{child.name}' to home storage");
                        child.SetParent(homeContainer.transform, false);
                        child.gameObject.SetActive(false); // ✅ CRITICAL: Deactivate when in storage!
                    }
                    else if (fleetUIFields != null)
                    {
                        Debug.LogWarning($"    Fleet UI '{child.name}' found in ASystemMenuView - skipping (Fleet controller handles this)");
                    }
                }

                ASystemMenuView.SetActive(false); // ✅ Hide the view
            }

            // Move from SysListContainer (scrollable list view)
            if (SysListContainer != null && SysListContainer != homeContainer)
            {
                Debug.Log($"  Checking SysListContainer ({SysListContainer.transform.childCount} children)");

                for (int i = SysListContainer.transform.childCount - 1; i >= 0; i--)
                {
                    var child = SysListContainer.transform.GetChild(i);
                    if (child == null) continue;

                    var starSysUIFields = child.GetComponent<StarSysUI_Fields>();
                    if (starSysUIFields != null)
                    {
                        Debug.Log($"    Moving SYSTEM UI '{child.name}' to home storage");
                        child.SetParent(homeContainer.transform, false);
                        child.gameObject.SetActive(false); // ✅ CRITICAL: Deactivate!
                    }
                }
            }

            // ✅ CRITICAL: Also check home storage itself and deactivate any active children
            if (homeContainer != null)
            {
                Debug.Log($"  Checking home storage ({homeContainer.transform.childCount} children)");

                for (int i = 0; i < homeContainer.transform.childCount; i++)
                {
                    var child = homeContainer.transform.GetChild(i);
                    if (child != null && child.gameObject.activeSelf)
                    {
                        var starSysUIFields = child.GetComponent<StarSysUI_Fields>();
                        if (starSysUIFields != null)
                        {
                            Debug.Log($"    Deactivating SYSTEM UI '{child.name}' in home storage");
                            child.gameObject.SetActive(false); // ✅ DEACTIVATE!
                        }
                    }
                }
            }

            // ✅ Check for stray system UIs in fleet views (error case)
            var aFleetView = FleetMenuUIController.Instance?.AFleetMenuView;
            if (aFleetView != null)
            {
                for (int i = aFleetView.transform.childCount - 1; i >= 0; i--)
                {
                    var child = aFleetView.transform.GetChild(i);
                    if (child == null) continue;

                    var starSysUIFields = child.GetComponent<StarSysUI_Fields>();
                    if (starSysUIFields != null)
                    {
                        Debug.LogError($"  ❌ SYSTEM UI '{child.name}' found in AFleetMenuView! Moving to home storage.");
                        child.SetParent(homeContainer.transform, false);
                        child.gameObject.SetActive(false); // ✅ CRITICAL: Deactivate!
                    }
                }
            }

            activeStarSysController = null;
            Debug.Log("=== MoveBackAnyStarSysUIGO: Complete - all UIs moved and DEACTIVATED ===");
        }
    }
}
