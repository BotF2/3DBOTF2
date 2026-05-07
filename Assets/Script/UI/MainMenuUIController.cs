// Ignore Spelling: Kling BOTF
using BOTF3D.Audio;
using BOTF3D.Core;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    public class MainMenuUIController : MonoBehaviour
    {
        /// <summary Multiplayer issues>
        /// ??? Unity ToggleGroup by default only allows one toggle to be active so:
        /// Will each remote player make a unique selection in their own Toggle group or
        /// is it better to just have buttons, or toggles not in a group for remotes to select?
        /// Need to sort out and define local player for the host and from each remote player PC in multiplayer lobby
        /// We can try using (Mirror; with GameObject LocalPlayerCivEnum = NetworkClient.LocalPlayerCivEnum.gameObject;)
        /// </summary>
        /// </summary>
        public static MainMenuUIController Instance { get; private set; }

        public MainMenuData MainMenuData = new MainMenuData();
        [SerializeField]
        private GameObject mainMenuCanvas;
        [SerializeField]
        private Camera uiCamera;

        private Camera galaxyCamera;
        private GameObject galaxyCenter;
        public GameObject GalaxyMenuGO { get; private set; }

        [SerializeField]
        private GameObject TipCanvas;
        [SerializeField]
        private GameObject mainMenuButton;
        //ToDo for multiplayer lobby
        //public CivEnum SelectedRemote0CivEnum;
        //public CivEnum SelectedRemote1CivEnum;
        //public CivEnum SelectedRemote2CivEnum;
        //public CivEnum SelectedRemote3CivEnum;
        //public CivEnum SelectedRemote4CivEnum;
        //public CivEnum SelectedRemote5CivEnum;
        public bool IsSinglePlayer;
        [SerializeField]
        private GameObject panelLobby;
        [SerializeField]
        private GameObject panelMuliplayer;
        [SerializeField]
        private GameObject panelCivSelection;
        [SerializeField]
        private GameObject panelGamePara;
        [SerializeField]
        private GameObject singlePlayToggleGroup;
        [SerializeField]
        private GameObject mulitplayerToggleGroup;
        [SerializeField]
        private GameObject mapToggleGroup;
        [SerializeField]
        private GameObject galaxySizeToggleGroup;
        [SerializeField]
        private GameObject techLevelToggleGroup;
        [SerializeField]
        private TMP_Text playerFed, playerRom, playerKling, playerCard, playerDom, playerBorg, playerTerran;
        private string player = "You", computer = "Computer", notInGame = "Absent";
        [SerializeField]
        private LocalizeStringEvent[] playerLocalizers;
        //private LocalizeStringEvent playerFedLocalizer, playerRomLocalizer, playerKlingLocalizer,
        //                   playerCardLocalizer, playerDomLocalizer, playerBorgLocalizer, playerTerranLocalizer;
        private Toggle activeLocalPlayerToggle;
        private CivEnum localPlayerCiv = CivEnum.FED;
        private Toggle[] civToggles;
        //private List<CivEnum> majorCivsInGameList = new List<CivEnum>
        //{
        //    CivEnum.FED, CivEnum.ROM, CivEnum.KLING, CivEnum.CARD, CivEnum.DOM, CivEnum.BORG, CivEnum.TERRAN
        //};
        [SerializeField] private GameObject fedImages;
        [SerializeField] private GameObject romImages;
        [SerializeField] private GameObject klingImages;
        [SerializeField] private GameObject cardImages;
        [SerializeField] private GameObject domImages;
        [SerializeField] private GameObject borgImages;
        [SerializeField] private GameObject terranImages;
        //ToDo for multiplayer lobby
        //private Toggle _activeRemote0;
        //private Toggle _activeRemote1;
        //private Toggle _activeRemote2;
        //private Toggle _activeRemote3;
        //private Toggle _activeRemote4;
        //private Toggle _activeRemote5;
        //private Toggle _activeRemote6;
        public Toggle FedLocalPlayerToggle, RomLocalPlayerToggle, KlingLocalPlayerToggle, CardLocalPlayerToggle,
            DomLocalPlayerToggle, BorgLocalPlayerToggle, TerranLocalPlayerToggle;

        public ToggleGroup SinglePlayerCivilizationGroup;
        //public ToggleGroup MultiplayerCivilizationGroup;// Can and should this be a group in the multiplayer setting, maybe.
        public Toggle FedOnOff, RomOnOff, KlingOnOff, CardOnOff, DomOnOff, BorgOnOff, TerranOnOff;
        public List<Toggle> OnOffToggles;
        private Toggle activeMapToggle;
        public ToggleGroup MapToggleGroup;
        public Toggle CanonToggle, RandomToggle, RingToggle;
        public List<Toggle> MapToggles;
        private Toggle activeGalaxySizeToggle;
        public ToggleGroup GalaxySizeToggleGroup;
        public Toggle SmallGalaxyToggle, MediumGalaxyToggle, LargeGalaxyToggle, PonderousGalaxyToggle;
        public List<Toggle> GalaxySizeToggles;
        private Toggle activeTechLevelToggle;
        public ToggleGroup TechLevelToggleGroup;
        public Toggle EarlyToggle, DevelopedToggle, AdvancedToggle, SupremeToggle;
        public List<Toggle> TechLevelToggles;
        [SerializeField]
        private GameObject settingsMenuView;
        [SerializeField]
        private GameObject closeSettingsButton;

        [Header("Localization")]
        [SerializeField] private Button buttonEnglish;
        [SerializeField] private Button buttonFrench;
        [SerializeField] private Button buttonGerman;
        [SerializeField] private Button buttonItalian;
        [SerializeField] private Button buttonSpanish;
        [SerializeField] private Button buttonPolish;
        [SerializeField] private Button buttonPortuguese;

        private void Awake()
        {
            Debug.Log("=== MainMenuUIController.Awake() START ===");

            if (Instance != null)
            {
                Debug.LogWarning("MainMenuUIController: Duplicate instance detected, destroying");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureEventSystem();
            Debug.Log("✅ MainMenuUIController: Instance set and marked DontDestroyOnLoad");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterMainMenu(this);
            }
            InitializeCameras();

            // ✅ CRITICAL: Set up ToggleGroup FIRST
            SinglePlayerCivilizationGroup.enabled = true;
            SinglePlayerCivilizationGroup = singlePlayToggleGroup.GetComponent<ToggleGroup>();

            // ✅ IMPORTANT: Set allowSwitchOff to false (ensures one is always selected)
            SinglePlayerCivilizationGroup.allowSwitchOff = false;

            // ✅ Register all toggles
            SinglePlayerCivilizationGroup.RegisterToggle(FedLocalPlayerToggle);
            SinglePlayerCivilizationGroup.RegisterToggle(RomLocalPlayerToggle);
            SinglePlayerCivilizationGroup.RegisterToggle(KlingLocalPlayerToggle);
            SinglePlayerCivilizationGroup.RegisterToggle(CardLocalPlayerToggle);
            SinglePlayerCivilizationGroup.RegisterToggle(DomLocalPlayerToggle);
            SinglePlayerCivilizationGroup.RegisterToggle(BorgLocalPlayerToggle);
            SinglePlayerCivilizationGroup.RegisterToggle(TerranLocalPlayerToggle);

            // Other toggle groups...
            MapToggleGroup.enabled = true;
            MapToggleGroup = mapToggleGroup.GetComponent<ToggleGroup>();
            MapToggleGroup.RegisterToggle(CanonToggle);
            MapToggleGroup.RegisterToggle(RandomToggle);
            MapToggleGroup.RegisterToggle(RingToggle);

            // On/Off toggles (these are NOT in a toggle group - they can all be on/off independently)
            FedOnOff.isOn = true;
            RomOnOff.isOn = true;
            KlingOnOff.isOn = true;
            CardOnOff.isOn = true;
            DomOnOff.isOn = true;
            BorgOnOff.isOn = true;
            TerranOnOff.isOn = false;

            GalaxySizeToggleGroup = galaxySizeToggleGroup.GetComponent<ToggleGroup>();
            GalaxySizeToggleGroup.RegisterToggle(SmallGalaxyToggle);
            GalaxySizeToggleGroup.RegisterToggle(MediumGalaxyToggle);
            GalaxySizeToggleGroup.RegisterToggle(LargeGalaxyToggle);
            GalaxySizeToggleGroup.RegisterToggle(PonderousGalaxyToggle);

            TechLevelToggleGroup.enabled = true;
            TechLevelToggleGroup = techLevelToggleGroup.GetComponent<ToggleGroup>();
            TechLevelToggleGroup.RegisterToggle(EarlyToggle);
            TechLevelToggleGroup.RegisterToggle(DevelopedToggle);
            TechLevelToggleGroup.RegisterToggle(AdvancedToggle);
            TechLevelToggleGroup.RegisterToggle(SupremeToggle);
            // ✅ Initialize localizer array (order matches CivEnum: FED=0, ROM=1, etc.)
            playerLocalizers = new LocalizeStringEvent[]
            {
                playerFed.GetComponent<LocalizeStringEvent>(),
                playerRom.GetComponent<LocalizeStringEvent>(),
                playerKling.GetComponent<LocalizeStringEvent>(),
                playerCard.GetComponent<LocalizeStringEvent>(),
                playerDom.GetComponent<LocalizeStringEvent>(),
                playerBorg.GetComponent<LocalizeStringEvent>(),
                playerTerran.GetComponent<LocalizeStringEvent>()
            };
            // ✅ Initialize toggle array
            civToggles = new Toggle[]
            {
                FedOnOff, RomOnOff, KlingOnOff, CardOnOff, DomOnOff, BorgOnOff, TerranOnOff
            };
            // Get LocalizeStringEvent components from each player text
            //playerFedLocalizer = playerFed.GetComponent<LocalizeStringEvent>();
            //playerRomLocalizer = playerRom.GetComponent<LocalizeStringEvent>();
            //playerKlingLocalizer = playerKling.GetComponent<LocalizeStringEvent>();
            //playerCardLocalizer = playerCard.GetComponent<LocalizeStringEvent>();
            //playerDomLocalizer = playerDom.GetComponent<LocalizeStringEvent>();
            //playerBorgLocalizer = playerBorg.GetComponent<LocalizeStringEvent>();
            //playerTerranLocalizer = playerTerran.GetComponent<LocalizeStringEvent>();
            // Pending Multiplayer lobby if needed
            //MultiplayerCivilizationGroup.enabled = true;
            //MultiplayerCivilizationGroup = mulitplayerToggleGroup.GetComponent<ToggleGroup>();
            //MultiplayerCivilizationGroup.RegisterToggle(FedLocalPalyerToggle);
            //MultiplayerCivilizationGroup.RegisterToggle(KlingLocalPlayerToggle);
            //MultiplayerCivilizationGroup.RegisterToggle(RomLocalPlayerToggle);
            //MultiplayerCivilizationGroup.RegisterToggle(CardLocalPlayerToggle);
            //MultiplayerCivilizationGroup.RegisterToggle(DomLocalPlayerToggle);
            //MultiplayerCivilizationGroup.RegisterToggle(BorgLocalPlayerToggle);
            //MultiplayerCivilizationGroup.RegisterToggle(TerranLocalPlayerToggle);

            // ✅ Wire language buttons
            SetupLanguageButtons();

            // ✅ Wire toggle events to update images AND background visibility
            FedLocalPlayerToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ShowCivImages(CivEnum.FED);
                    UpdateToggleBackgrounds(FedLocalPlayerToggle);
                }
            });
            RomLocalPlayerToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ShowCivImages(CivEnum.ROM);
                    UpdateToggleBackgrounds(RomLocalPlayerToggle);
                }
            });
            KlingLocalPlayerToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ShowCivImages(CivEnum.KLING);
                    UpdateToggleBackgrounds(KlingLocalPlayerToggle);
                }
            });
            CardLocalPlayerToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ShowCivImages(CivEnum.CARD);
                    UpdateToggleBackgrounds(CardLocalPlayerToggle);
                }
            });
            DomLocalPlayerToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ShowCivImages(CivEnum.DOM);
                    UpdateToggleBackgrounds(DomLocalPlayerToggle);
                }
            });
            BorgLocalPlayerToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ShowCivImages(CivEnum.BORG);
                    UpdateToggleBackgrounds(BorgLocalPlayerToggle);
                }
            });
            TerranLocalPlayerToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ShowCivImages(CivEnum.TERRAN);
                    UpdateToggleBackgrounds(TerranLocalPlayerToggle);
                }
            });
            Debug.Log("=== MainMenuUIController.Awake() COMPLETE ===");
        }

        private void Start()
        {
            SetupMainMenuUI();

            // ✅ Initialize all player text to "Computer" before any selection
            InitializePlayerTextDefaults();
        }
        /// <summary>
        /// Initialize all player text fields with default "Computer" localization
        /// Call this in Start() or when panel first loads
        /// </summary>
        private void InitializePlayerTextDefaults()
        {
            Debug.Log("InitializePlayerTextDefaults: Setting all players to 'Computer' by default");

            for (int i = 0; i < playerLocalizers.Length; i++)
            {
                if (playerLocalizers[i] != null)
                {
                    // Set default to "Computer"
                    SetLocalizedPlayerText(playerLocalizers[i], "Computer");
                }
            }
        }
        private IEnumerator InitializeAfterLocalization()
        {
            Debug.Log("MainMenuUIController: Waiting for localization...");

            // ✅ Wait for localization to be ready
            yield return LocalizationSettings.InitializationOperation;

            Debug.Log("✅ Localization ready - setting up MainMenu UI");

            // ✅ Now setup your UI (existing Start() code goes here)
            SetupMainMenuUI();

            // ✅ Force refresh all localized strings
            if (LocaleManager.Instance != null)
            {
                LocaleManager.Instance.RefreshAllLocalizedStrings();
            }
        }
        /// <summary>
        /// Sets a localized string key for a player text field
        /// </summary>
        /// <summary>
        /// Sets a localized string key for a player text field
        /// </summary>
        /// <summary>
        /// Sets a localized string key for a player text field
        /// </summary>
        private void SetLocalizedPlayerText(LocalizeStringEvent localizer, string key)
        {
            if (localizer == null)
            {
                Debug.LogError($"SetLocalizedPlayerText: localizer is null for key '{key}'");
                return;
            }

            Debug.Log($"SetLocalizedPlayerText: Setting localizer to key='{key}'");
            localizer.StringReference.SetReference("StringTableCollection", key);
            localizer.RefreshString();
        }
        private void SetupMainMenuUI()
        {
            VerifyButtonsAreInteractable();

            // ✅ CRITICAL: Initialize all toggle checkmarks before setting any toggle states
            InitializeAllToggleCheckmarks();

            // ✅ Play main menu music
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic("MusicFiveYearMission", crossfade: false);
                Debug.Log("🎵 Playing MainMenu music: MusicFiveYearMission");
            }
            else
            {
                Debug.LogWarning("⚠️ AudioManager not found - music won't play");
            }

            FedLocalPlayerToggle.isOn = true;
            FedLocalPlayerToggle.Select();
            FedLocalPlayerToggle.OnSelect(null);

            // ✅ Show Fed images since it's default
            TurnOffAllImages();
            fedImages.SetActive(true);
            FedLocalPlayerToggle.isOn = true;
            FedLocalPlayerToggle.Select();
            FedLocalPlayerToggle.OnSelect(null);

            // ✅ Show Fed images since it's default
            TurnOffAllImages();
            fedImages.SetActive(true);

            KlingLocalPlayerToggle.isOn = false;
            RomLocalPlayerToggle.isOn = false;
            CardLocalPlayerToggle.isOn = false;
            DomLocalPlayerToggle.isOn = false;
            BorgLocalPlayerToggle.isOn = false;
            TerranLocalPlayerToggle.isOn = false;

            // Build OnOffToggles list
            OnOffToggles.Add(FedOnOff);
            OnOffToggles.Add(RomOnOff);
            OnOffToggles.Add(KlingOnOff);
            OnOffToggles.Add(CardOnOff);
            OnOffToggles.Add(DomOnOff);
            OnOffToggles.Add(BorgOnOff);
            OnOffToggles.Add(TerranOnOff);

            // Initialize civ list
            MainMenuData.InGamePlayableCivList.Add(CivEnum.FED);
            MainMenuData.InGamePlayableCivList.Add(CivEnum.ROM);
            MainMenuData.InGamePlayableCivList.Add(CivEnum.KLING);
            MainMenuData.InGamePlayableCivList.Add(CivEnum.CARD);
            MainMenuData.InGamePlayableCivList.Add(CivEnum.DOM);
            MainMenuData.InGamePlayableCivList.Add(CivEnum.BORG);
            MainMenuData.InGamePlayableCivList.Add(CivEnum.TERRAN);

            // Map toggles
            CanonToggle.isOn = true;
            RandomToggle.isOn = false;
            RingToggle.isOn = false;

            // Galaxy size toggles
            SmallGalaxyToggle.isOn = true;
            MediumGalaxyToggle.isOn = false;
            LargeGalaxyToggle.isOn = false;
            PonderousGalaxyToggle.isOn = false;

            // Tech level toggles
            EarlyToggle.isOn = true;
            DevelopedToggle.isOn = false;
            AdvancedToggle.isOn = false;
            SupremeToggle.isOn = false;
            Debug.Log("=== MainMenuUIController.Start() COMPLETE ===");
        }

        /// <summary>
        /// Ensures an EventSystem exists for UI input AND persists across scenes
        /// </summary>
        private void EnsureEventSystem()
        {
            // Check if EventSystem already exists
            EventSystem currentES = EventSystem.current;

            if (currentES == null)
            {
                Debug.LogWarning("⚠️ No EventSystem found - creating one");

                GameObject eventSystemGO = new GameObject("EventSystem");
                currentES = eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();

                // ✅ CRITICAL FIX: Make EventSystem persist across scenes
                DontDestroyOnLoad(eventSystemGO);

                Debug.Log("✅ Created persistent EventSystem with StandaloneInputModule");
            }
            else
            {
                Debug.Log($"✅ EventSystem found: {currentES.name}");

                // ✅ CRITICAL FIX: Ensure StandaloneInputModule exists
                var inputModule = currentES.GetComponent<StandaloneInputModule>();
                if (inputModule == null)
                {
                    currentES.gameObject.AddComponent<StandaloneInputModule>();
                    Debug.Log("✅ Added missing StandaloneInputModule to existing EventSystem");
                }

                // ✅ CRITICAL FIX: Make existing EventSystem persistent
                if (currentES.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    DontDestroyOnLoad(currentES.gameObject);
                    Debug.Log($"✅ Made EventSystem persistent (was in {currentES.gameObject.scene.name})");
                }
            }
        }
        /// <summary>
        /// Debug helper to verify button states
        /// </summary>
        private void VerifyButtonsAreInteractable()
        {
            // ✅ Search from mainMenuCanvas root instead of this GameObject
            Button[] buttons = null;

            if (mainMenuCanvas != null)
            {
                buttons = mainMenuCanvas.GetComponentsInChildren<Button>(true);
                Debug.Log($"Found {buttons.Length} buttons in MainMenuCanvas");
            }
            else
            {
                // Fallback: search from this GameObject
                buttons = GetComponentsInChildren<Button>(true);
                Debug.Log($"Found {buttons.Length} buttons as children of MainMenuUIController");
            }

            if (buttons.Length == 0)
            {
                Debug.LogError("❌ NO BUTTONS FOUND! Check MainMenu hierarchy structure!");
                return;
            }

            foreach (var button in buttons)
            {
                if (button.interactable)
                {
                    Debug.Log($"  ✅ Button '{button.name}' is interactable");
                }
                else
                {
                    Debug.LogWarning($"  ❌ Button '{button.name}' is NOT interactable!");
                }
            }
        }
        private void InitializeCameras()
        {
            Debug.Log("InitializeCameras: Menu camera setup");

            if (uiCamera != null)
            {
                uiCamera.enabled = true;
                Debug.Log("  - UI Camera enabled");
            }

            // GalaxyCenter doesn't exist yet - will be found when GalaxyScene loads
            SetupUICamera();
        }

        private void SetupUICamera()
        {
            if (mainMenuCanvas != null)
            {
                Canvas canvas = mainMenuCanvas.GetComponent<Canvas>();
                if (canvas != null && uiCamera != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = uiCamera;
                    canvas.planeDistance = 10f;
                    Debug.Log("  - MainMenu canvas set to Screen Space - Camera mode");
                }
            }
        }

        // Call this when transitioning to gameplay (from Panel-GameParametersWindow)
        public void LoadGalaxyScene()
        {
            TimeManager.Instance.timeRunning = true;
            TimeManager.Instance.StartTime();
            UpdateMapSelection();
            UpdateGalaxySizeSelection();
            UpdateTechLevelSelection();
            UpdateNotInGame();
            CivManager.Instance.UpdatePlayableCivGameList(MainMenuData.InGamePlayableCivList, (int)MainMenuData.SelectedGalaxySize, this.MainMenuData.SelectedGalaxyType);

            Debug.Log("LoadGalaxyScene: Starting clean scene transition");

            // Store game settings before transition
            GameController.Instance.GameData.GameMode = IsSinglePlayer ? GameMode.SINGLEPLAYER : GameMode.MULTIPLAYER;
            //GameController.Instance.GameData.MajorCivsInGameList = majorCivsInGameList;

            // Use coroutine for clean transition
            StartCoroutine(LoadGalaxySceneCoroutine());
        }

        private System.Collections.IEnumerator LoadGalaxySceneCoroutine()
        {
            Debug.Log("LoadGalaxySceneCoroutine: Step 1 - Waiting for UI events to finish");
            // ✅ Fade out menu music
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic(fade: true);
                Debug.Log("🎵 Fading out MainMenu music");
            }
            // Wait for UI to finish (per copilot-instructions.md: wait two frames)
            yield return null;
            yield return null;

            Debug.Log("LoadGalaxySceneCoroutine: Step 2 - Checking for existing GalaxyScene");

            // CRITICAL: Check if GalaxyScene is already loaded
            Scene existingGalaxyScene = SceneManager.GetSceneByName("GalaxyScene");
            if (existingGalaxyScene.IsValid() && existingGalaxyScene.isLoaded)
            {
                Debug.LogWarning("  GalaxyScene already loaded! Unloading old instance first...");

                // Clear fog revelers BEFORE unloading
                if (FischlWorks_FogWar.csFogWar.Instance != null)
                {
                    FischlWorks_FogWar.csFogWar.Instance.ClearAllRevealers();
                }

                yield return SceneManager.UnloadSceneAsync(existingGalaxyScene);
                Debug.Log("  Old GalaxyScene unloaded");

                // Wait a frame after unload
                yield return null;
            }

            Debug.Log("LoadGalaxySceneCoroutine: Step 3 - Loading GalaxyScene");

            // Load galaxy scene additively
            var asyncLoad = SceneManager.LoadSceneAsync("GalaxyScene", LoadSceneMode.Additive);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            Debug.Log("LoadGalaxySceneCoroutine: Step 4 - GalaxyScene loaded, finding and activating galaxy objects");

            // CRITICAL: Clear any leftover fog revelers before activating new scene
            if (FischlWorks_FogWar.csFogWar.Instance != null)
            {
                FischlWorks_FogWar.csFogWar.Instance.ClearAllRevealers();
                Debug.Log("  Cleared fog revealers");
            }

            // Find and activate galaxy objects
            FindAndActivateGalaxySceneReferences();

            // Initialize game systems
            CivManager.Instance.OnNewGameButtonClicked(
                (int)MainMenuData.SelectedGalaxySize,
                (int)MainMenuData.SelectedTechLevel,
                (int)MainMenuData.SelectedGalaxyType,
                (int)GameManager.Instance.GameController.GameData.LocalPlayerCivEnum,
                IsSinglePlayer);

            // Wait for initialization (per copilot-instructions.md: wait two frames)
            yield return null;
            yield return null;

            Debug.Log("LoadGalaxySceneCoroutine: Step 5 - Hiding UI and unloading MainMenuScene");
            // ✅ CRITICAL FIX: Update EventSystem BEFORE disabling MainMenu camera
            UpdateEventSystemForGalaxy();
            // Disable UI camera
            if (uiCamera != null)
            {
                uiCamera.enabled = false;
                Debug.Log("  MainMenu UI camera disabled");
            }

            // Hide main menu canvas
            if (mainMenuCanvas != null)
            {
                var canvasComponent = mainMenuCanvas.GetComponent<Canvas>();
                if (canvasComponent != null)
                {
                    canvasComponent.enabled = false;
                    Debug.Log("  MainMenu canvas disabled");
                }
            }

            // Unload MainMenu scene
            Scene mainMenuScene = SceneManager.GetSceneByName("MainMenuScene");
            if (!mainMenuScene.IsValid())
            {
                mainMenuScene = SceneManager.GetSceneByName("MainMenuScene");
            }

            if (mainMenuScene.IsValid() && mainMenuScene.isLoaded)
            {
                Debug.Log($"Unloading scene: {mainMenuScene.name}");
                yield return SceneManager.UnloadSceneAsync(mainMenuScene);
                Debug.Log("MainMenu scene unloaded successfully");
            }

            Debug.Log("LoadGalaxySceneCoroutine: Complete");
        }
        /// <summary>
        /// Updates EventSystem to work with Galaxy UI cameras
        /// </summary>
        private void UpdateEventSystemForGalaxy()
        {
            Debug.Log("UpdateEventSystemForGalaxy: Configuring EventSystem for galaxy scene");

            // Find the galaxy camera
            Camera galaxyCamera = galaxyCenter?.GetComponentInChildren<Camera>();
            if (galaxyCamera == null)
            {
                galaxyCamera = Camera.main;
            }

            if (galaxyCamera == null)
            {
                Debug.LogError("UpdateEventSystemForGalaxy: No galaxy camera found!");
                return;
            }

            Debug.Log($"  Found galaxy camera: {galaxyCamera.name}");
            // Update all galaxy canvases to use the galaxy camera
            Canvas[] galaxyCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            int updatedCount = 0;

            foreach (Canvas canvas in galaxyCanvases)
            {
                // Only update canvases in GalaxyScene (not MainMenu or Persistent)
                if (canvas.gameObject.scene.name == "GalaxyScene")
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceCamera ||
                        canvas.renderMode == RenderMode.WorldSpace)
                    {
                        canvas.worldCamera = galaxyCamera;
                        updatedCount++;
                        Debug.Log($"  ✅ Updated canvas '{canvas.name}' to use galaxy camera");
                    }
                }
            }

            Debug.Log($"UpdateEventSystemForGalaxy: Updated {updatedCount} canvases");
        }
        private void FindAndActivateGalaxySceneReferences()
        {
            Debug.Log("FindAndActivateGalaxySceneReferences: Searching in loaded scenes...");
            // ✅ Play galaxy exploration music
            if (AudioManager.Instance != null)
            {
                //AudioManager.Instance.PlayMusic("Galaxy Music here", crossfade: true);
                //Debug.Log("🎵 Playing Galaxy music");
            }
            // List all loaded scenes and their root objects
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                Debug.Log($"  Loaded scene {i}: {scene.name} (loaded: {scene.isLoaded})");

                if (scene.isLoaded)
                {
                    var rootObjects = scene.GetRootGameObjects();
                    Debug.Log($"    Root objects in {scene.name}: {rootObjects.Length}");
                    foreach (var root in rootObjects)
                    {
                        Debug.Log($"      - {root.name}");
                    }
                }
            }

            // Find GalaxyCenter
            if (galaxyCenter == null)
            {
                galaxyCenter = FindGameObjectRecursive("GalaxyCenter");
            }

            if (galaxyCenter != null)
            {
                galaxyCenter.SetActive(true);
                Debug.Log($"  GalaxyCenter activated: {galaxyCenter.name}");

                // Enable camera first
                galaxyCamera = galaxyCenter.GetComponentInChildren<Camera>(includeInactive: true);
                if (galaxyCamera != null)
                {
                    galaxyCamera.gameObject.SetActive(true);
                    galaxyCamera.enabled = true;
                    Debug.Log($"  Galaxy Camera enabled: {galaxyCamera.name}");

                    var cameraController = galaxyCamera.GetComponent<GalaxyCameraDragMoveZoom>();
                    if (cameraController != null)
                    {
                        cameraController.EnableCameraControl();
                        Debug.Log("  Galaxy Camera controller enabled");
                    }
                    else
                    {
                        Debug.LogError("  GalaxyCameraDragMoveZoom component NOT FOUND on camera!");
                    }
                }
                else
                {
                    Debug.LogError("  Galaxy Camera not found in GalaxyCenter!");
                }

                // CRITICAL: Update FleetManager's references BEFORE creating fleets
                if (FleetManager.Instance != null)
                {
                    FleetManager.Instance.FindGalaxyReferences();
                    Debug.Log("  FleetManager galaxy references updated");
                }

                // CRITICAL: Update StarSysManager's references too
                if (StarSysManager.Instance != null)
                {
                    StarSysManager.Instance.FindGalaxyReferences();
                    Debug.Log("  StarSysManager galaxy references updated");
                }
            }
            else
            {
                Debug.LogError("  GalaxyCenter NOT FOUND in any loaded scene!");
            }

            // Find and activate CanvasGalaxy
            if (GalaxyMenuGO == null)
            {
                GalaxyMenuGO = FindGameObjectRecursive("CanvasGalaxy");
            }

            if (GalaxyMenuGO != null)
            {
                GalaxyMenuGO.SetActive(true);
                Debug.Log($"  CanvasGalaxy activated");

                // CRITICAL: Update all UI controllers AFTER CanvasGalaxy is activated
                if (FleetManager.Instance != null)
                {
                    FleetManager.Instance.FindGalaxyReferences();
                    Debug.Log("  FleetManager references updated");
                }

                if (StarSysManager.Instance != null)
                {
                    StarSysManager.Instance.FindGalaxyReferences();
                    Debug.Log("  StarSysManager references updated");
                }

                if (FleetMenuUIController.Instance != null)
                {
                    FleetMenuUIController.Instance.FindFleetUIContainers();
                    Debug.Log("  FleetMenuUIController references updated");
                }

                if (StarSysMenuUIController.Instance != null)
                {
                    StarSysMenuUIController.Instance.FindSysUIContainers();
                    Debug.Log("  StarSysMenuUIController references updated");
                }

                if (GalaxyMenuUIController.Instance != null)
                {
                    GalaxyMenuUIController.Instance.InitializeGalaxyCamera();
                    Debug.Log("  GalaxyMenuUIController camera initialized");
                }
            }
            else
            {
                Debug.LogWarning("  CanvasGalaxy not found");
            }

            // Activate other objects
            ActivateGalaxyGameObjects();
        }

        // Improved recursive search
        private GameObject FindGameObjectRecursive(string name)
        {
            // Search all loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    foreach (GameObject rootObj in scene.GetRootGameObjects())
                    {
                        GameObject found = FindInHierarchy(rootObj.transform, name);
                        if (found != null)
                        {
                            Debug.Log($"  Found '{name}' in scene '{scene.name}' under '{rootObj.name}'");
                            return found;
                        }
                    }
                }
            }

            Debug.LogError($"  ? '{name}' not found in any scene hierarchy");
            return null;
        }

        // Recursive helper to search entire hierarchy
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

        private void ActivateGalaxyGameObjects()
        {
            // Activate GalaxyImage (might be child of GalaxyCenter)
            var galaxyImage = FindGameObjectRecursive("GalaxyImage");
            if (galaxyImage != null)
            {
                galaxyImage.SetActive(true);
                Debug.Log($"  ? GalaxyImage activated");
            }

            // Activate BlackHoleSagA
            var blackHole = FindGameObjectRecursive("BlackHoleSagA");
            if (blackHole != null)
            {
                blackHole.SetActive(true);
                Debug.Log($"  ? BlackHoleSagA activated");
            }

            // Activate FogPlaneParent
            var fogPlane = FindGameObjectRecursive("FogPlaneParent");
            if (fogPlane != null)
            {
                fogPlane.SetActive(true);
                Debug.Log($"  ? FogPlaneParent activated");
            }

            // Activate PlayerDefinedTargetManager
            var targetManager = FindGameObjectRecursive("PlayerDefinedTargetManager");
            if (targetManager != null)
            {
                targetManager.SetActive(true);
                Debug.Log($"  ? PlayerDefinedTargetManager activated");
            }

            Debug.Log("ActivateGalaxyGameObjects: Complete");
        }

        public void ReturnToLobbyMenu()
        {
            ResetPlayers(-1); // resets all to "Computer"
            panelLobby.SetActive(true);
            panelMuliplayer.SetActive(false);
            panelCivSelection.SetActive(false);
            panelGamePara.SetActive(false);
        }

        //// No longer needed - kept for reference
        //public void TransitionToGameplay()
        //{
        //    // This method is now handled by LoadGalaxySceneCoroutine
        //    Debug.Log("TransitionToGameplay: Scene transition handled by LoadGalaxySceneCoroutine");
        //}

        private void UpdatePlayers()
        {
            activeLocalPlayerToggle = SinglePlayerCivilizationGroup.ActiveToggles().ToArray().FirstOrDefault();
            if (activeLocalPlayerToggle != null)
                ActivePlayerToggle();
            #region Multiplayer toggle group - 
            // ToDo do we need a multiplayer toggle group?
            //foreach (var toggle in MultiplayerCivilizationGroup.ActiveToggles().ToArray())
            //{
            //    // ToDo: !!! need to get local player for SetLocalCivilization(int of civ) and
            //    // CivManager.current.LocalPlayerCivEnum = CivManager.current.GetCivDataByCivEnum(CivEnum...);
            //    // can try using Mirror; with GameObject LocalPlayerCivEnum = NetworkClient.LocalPlayerCivEnum.gameObject;
            //    if (toggle.name == "TOGGLE_FED")
            //    {
            //        FedLocalPalyerToggle = _activeRemote0;
            //    }
            //    else if (toggle.name == "TOGGLE_ROM")
            //    {
            //        RomLocalPlayerToggle = _activeRemote1;
            //    }
            //    else if (toggle.name == "TOGGLE_KLING")
            //    {
            //        KlingLocalPlayerToggle = _activeRemote2;
            //    }

            //    else if (toggle.name == "TOGGLE_CARD")
            //    {
            //        CardLocalPlayerToggle = _activeRemote3;
            //    }
            //    else if (toggle.name == "TOGGLE_DOM")
            //    {
            //        DomLocalPlayerToggle = _activeRemote4;
            //    }
            //    else if (toggle.name == "TOGGLE_BORG")
            //    {
            //        BorgLocalPlayerToggle = _activeRemote5;
            //    }
            //    else if (toggle.name == "TOGGLE_TERRAN")
            //    {
            //        TerranLocalPlayerToggle = _activeRemote6;
            //    }
            //}
            #endregion Multiplayer toggle group
        }
        public void UpdateMapSelection()
        {
            activeMapToggle = MapToggleGroup.ActiveToggles().ToArray().FirstOrDefault();
            if (activeMapToggle != null)
            {
                ActiveMapToggle();
            }
        }
        public void UpdateGalaxySizeSelection()
        {
            activeGalaxySizeToggle = GalaxySizeToggleGroup.ActiveToggles().ToArray().FirstOrDefault();
            if (activeMapToggle != null)
            {
                ActiveGalaxySizeToggle();
            }
        }
        public void UpdateTechLevelSelection()
        {
            activeTechLevelToggle = TechLevelToggleGroup.ActiveToggles().ToArray().FirstOrDefault();
            if (activeTechLevelToggle != null)
            {
                ActiveTechLevelToggle();
            }
        }
        private void UpdateNotInGame()
        {
            for (int i = 0; i < civToggles.Length; i++)
            {
                if (playerLocalizers[i] == null) continue;

                string currentKey = playerLocalizers[i].StringReference.TableEntryReference.Key;

                if (!civToggles[i].isOn && currentKey != "You")
                {
                    // Civ is OFF and not the player → set to "Absent"
                    SetLocalizedPlayerText(playerLocalizers[i], "Absent");
                }
                else if (civToggles[i].isOn && currentKey == "Absent")
                {
                    // Civ turned back ON from "Absent" → set to "Computer"
                    SetLocalizedPlayerText(playerLocalizers[i], "Computer");
                }
                // ✅ If currentKey == "You", leave it alone (don't change it)
            }
        }
        private void ActivePlayerToggle()
        {
            // ✅ Turn off all images first
            TurnOffAllImages();

            switch (activeLocalPlayerToggle.name.ToUpper())
            {
                case "TOGGLELOCAL_FED":
                    fedImages.SetActive(true);  // ✅ Show Fed images
                    FedOnOff.isOn = true;
                    FedOnOff.OnSelect(null);
                    FedLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active FedLocalPlayerToggle.");
                    SetLocalCivilization(0);
                    ResetPlayers(0);
                    break;

                case "TOGGLELOCAL_ROM":
                    romImages.SetActive(true);  // ✅ Show Rom images
                    RomOnOff.isOn = true;
                    RomOnOff.OnSelect(null);
                    RomLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active RomLocalPlayerToggle.");
                    SetLocalCivilization(1);
                    ResetPlayers(1);
                    break;

                case "TOGGLELOCAL_KLING":
                    klingImages.SetActive(true);  // ✅ Show Kling images
                    KlingOnOff.isOn = true;
                    KlingOnOff.OnSelect(null);
                    KlingLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active KlingLocalPlayerToggle.");
                    SetLocalCivilization(2);
                    ResetPlayers(2);
                    break;

                case "TOGGLELOCAL_CARD":
                    cardImages.SetActive(true);  // ✅ Show Card images
                    CardOnOff.isOn = true;
                    CardOnOff.OnSelect(null);
                    CardLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active CardLocalPlayerToggle.");
                    SetLocalCivilization(3);
                    ResetPlayers(3);
                    break;

                case "TOGGLELOCAL_DOM":
                    domImages.SetActive(true);  // ✅ Show Dom images
                    DomOnOff.isOn = true;
                    DomOnOff.OnSelect(null);
                    DomLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active DomLocalPlayerToggle.");
                    SetLocalCivilization(4);
                    ResetPlayers(4);
                    break;

                case "TOGGLELOCAL_BORG":
                    borgImages.SetActive(true);  // ✅ Show Borg images
                    BorgOnOff.isOn = true;
                    BorgOnOff.OnSelect(null);
                    BorgLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active BorgLocalPlayerToggle.");
                    SetLocalCivilization(5);
                    ResetPlayers(5);
                    break;

                case "TOGGLELOCAL_TERRAN":
                    terranImages.SetActive(true);  // ✅ Show Terran images
                    TerranOnOff.isOn = true;
                    TerranOnOff.OnDeselect(null);
                    TerranLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active TerranLocalPlayerToggle.");
                    SetLocalCivilization(6);
                    ResetPlayers(6);
                    break;

                default:
                    Debug.LogWarning($"ActivePlayerToggle: Unknown toggle name '{activeLocalPlayerToggle.name}'");
                    break;
            }
        }
        private void TurnOffAllImages()
        {
            fedImages.SetActive(false);
            romImages.SetActive(false);
            klingImages.SetActive(false);
            cardImages.SetActive(false);
            domImages.SetActive(false);
            borgImages.SetActive(false);
            terranImages.SetActive(false);
        }

        /// <summary>
        /// Initialize all toggle checkmarks to ensure they have proper Image components and materials.
        /// This fixes issues where checkmarks don't show up or materials aren't assigned at runtime.
        /// </summary>
        private void InitializeAllToggleCheckmarks()
        {
            Debug.Log("=== InitializeAllToggleCheckmarks: START ===");

            EnsureToggleCheckmarkConfigured(FedLocalPlayerToggle, "ToggleLocal_Fed");
            EnsureToggleCheckmarkConfigured(RomLocalPlayerToggle, "ToggleLocal_Rom");
            EnsureToggleCheckmarkConfigured(KlingLocalPlayerToggle, "ToggleLocal_Kling");
            EnsureToggleCheckmarkConfigured(CardLocalPlayerToggle, "ToggleLocal_Card");
            EnsureToggleCheckmarkConfigured(DomLocalPlayerToggle, "ToggleLocal_Dom");
            EnsureToggleCheckmarkConfigured(BorgLocalPlayerToggle, "ToggleLocal_Borg");
            EnsureToggleCheckmarkConfigured(TerranLocalPlayerToggle, "ToggleLocal_Terran");

            Debug.Log("=== InitializeAllToggleCheckmarks: COMPLETE ===");
        }

        /// <summary>
        /// Ensure a toggle's Background Image is configured to show/hide based on toggle state.
        /// The Background Image component will be controlled by Unity's Toggle.graphic property.
        /// </summary>
        private void EnsureToggleCheckmarkConfigured(Toggle toggle, string toggleName)
        {
            if (toggle == null)
            {
                Debug.LogError($"EnsureToggleCheckmarkConfigured: '{toggleName}' toggle is NULL!");
                return;
            }

            // ✅ Find Background child GameObject
            Transform background = toggle.transform.Find("Background");
            if (background == null)
            {
                Debug.LogError($"EnsureToggleCheckmarkConfigured: '{toggleName}' has no Background child!");
                return;
            }

            // ✅ Get or add Image component to Background
            Image backgroundImage = background.GetComponent<Image>();
            if (backgroundImage == null)
            {
                Debug.LogWarning($"EnsureToggleCheckmarkConfigured: '{toggleName}/Background' missing Image component - adding it");
                backgroundImage = background.gameObject.AddComponent<Image>();
            }

            // ✅ CRITICAL: Assign Background Image as the Toggle's graphic
            // Unity's Toggle will automatically enable/disable this Image based on isOn state
            toggle.graphic = backgroundImage;
            Debug.Log($"  ✅ Assigned Background Image to Toggle.graphic for '{toggleName}'");

            // ✅ Ensure Background GameObject is ACTIVE (Toggle will control Image visibility, not GameObject)
            background.gameObject.SetActive(true);

            // ✅ Set initial state: only show background if toggle is ON
            backgroundImage.enabled = toggle.isOn;

            Debug.Log($"✅ '{toggleName}' configured: Image={backgroundImage != null}, " +
                      $"Sprite={(backgroundImage?.sprite != null ? backgroundImage.sprite.name : "None")}, " +
                      $"IsOn={toggle.isOn}, ImageEnabled={backgroundImage.enabled}");
        }

        public void ActiveMapToggle()
        {
            switch (activeMapToggle.name.ToUpper())
            {
                case "TOGGLE_CANON":
                    CanonToggle.isOn = true;
                    CanonToggle.OnSelect(null);
                    CanonToggle = activeMapToggle;
                    //CanonToggle.GetComponent<Image>().color = activeColor;
                    SetMapGalaxyType((int)GalaxyMapType.CANON);
                    break;
                case "TOGGLE_RANDOM":
                    RandomToggle.isOn = true;
                    RandomToggle.OnSelect(null);
                    RandomToggle = activeMapToggle;
                    //RandomToggle.GetComponent<Image>().color = activeColor;
                    SetMapGalaxyType((int)GalaxyMapType.RANDOM);
                    break;
                case "TOGGLE_RING":
                    RingToggle.isOn = true;
                    RingToggle.OnSelect(null);
                    RingToggle = activeMapToggle;
                    // RingToggle.GetComponent<Image>().color = activeColor;
                    SetMapGalaxyType((int)(GalaxyMapType.RING));
                    break;
                default:
                    break;
            }
        }
        public void ActiveGalaxySizeToggle()
        {
            switch (activeGalaxySizeToggle.name.ToUpper())
            {
                case "TOGGLE_SMALL":
                    SmallGalaxyToggle.isOn = true;
                    SmallGalaxyToggle.OnSelect(null);
                    SmallGalaxyToggle = activeGalaxySizeToggle;
                    SetGalaxySize((int)GalaxySize.SMALL);
                    break;
                case "TOGGLE_MEDIUM":
                    MediumGalaxyToggle.isOn = true;
                    MediumGalaxyToggle.OnSelect(null);
                    MediumGalaxyToggle = activeGalaxySizeToggle;
                    SetGalaxySize((int)GalaxySize.MEDIUM);
                    break;
                case "TOGGLE_LARGE":
                    LargeGalaxyToggle.isOn = true;
                    LargeGalaxyToggle.OnSelect(null);
                    LargeGalaxyToggle = activeGalaxySizeToggle;
                    SetGalaxySize((int)GalaxySize.MEDIUM);
                    break;
                case "TOGGLE_PONDEROUS":
                    PonderousGalaxyToggle.isOn = true;
                    PonderousGalaxyToggle.OnSelect(null);
                    PonderousGalaxyToggle = activeGalaxySizeToggle;
                    SetGalaxySize((int)GalaxySize.EXTREME);
                    break;
                default:
                    break;
            }
        }
        public void ActiveTechLevelToggle()
        {
            switch (activeTechLevelToggle.name.ToUpper())
            {
                case "TOGGLE_EARLY":
                    EarlyToggle.isOn = true;
                    EarlyToggle.OnSelect(null);
                    EarlyToggle = activeTechLevelToggle;
                    SetTechLevel((int)TechLevel.EARLY);
                    GameController.Instance.GameData.StartingTechLevel = TechLevel.EARLY;
                    break;
                case "TOGGLE_DEVELOPED":
                    DevelopedToggle.isOn = true;
                    DevelopedToggle.OnSelect(null);
                    DevelopedToggle = activeTechLevelToggle;
                    SetTechLevel((int)TechLevel.DEVELOPED);
                    GameController.Instance.GameData.StartingTechLevel = TechLevel.DEVELOPED;
                    break;
                case "TOGGLE_ADVANCED":
                    AdvancedToggle.isOn = true;
                    AdvancedToggle.OnSelect(null);
                    AdvancedToggle = activeTechLevelToggle;
                    SetTechLevel((int)TechLevel.ADVANCED);
                    GameController.Instance.GameData.StartingTechLevel = TechLevel.ADVANCED;
                    break;
                case "TOGGLE_SUPREME":
                    SupremeToggle.isOn = true;
                    SupremeToggle.OnSelect(null);
                    SupremeToggle = activeTechLevelToggle;
                    SetTechLevel((int)TechLevel.SUPREME);
                    GameController.Instance.GameData.StartingTechLevel = TechLevel.SUPREME;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Resets all players and sets ONLY the specified civ to "You"
        /// </summary>
        /// <param name="civInt">Index of civilization to mark as "You" (0-6), or -1 to reset all to Computer</param>
        private void ResetPlayers(int civInt)
        {
            Debug.Log($"=== ResetPlayers({civInt}) START ===");

            // Reset all "You" to "Computer" (keeps "Absent" unchanged)
            for (int i = 0; i < playerLocalizers.Length; i++)
            {
                if (playerLocalizers[i] == null)
                {
                    Debug.LogError($"  playerLocalizers[{i}] is NULL!");
                    continue;
                }

                string currentKey = playerLocalizers[i].StringReference.TableEntryReference.Key;
                Debug.Log($"  Index {i}: current key = '{currentKey}'");

                if (currentKey == "You")
                {
                    Debug.Log($"  Index {i}: Resetting from 'You' to 'Computer'");
                    SetLocalizedPlayerText(playerLocalizers[i], "Computer");
                }
            }

            // Set selected civ to "You"
            if (civInt >= 0 && civInt < playerLocalizers.Length)
            {
                Debug.Log($"  Setting index {civInt} to 'You'");
                SetLocalizedPlayerText(playerLocalizers[civInt], "You");
            }
            else
            {
                Debug.LogWarning($"  Invalid civInt: {civInt}");
            }

            Debug.Log($"=== ResetPlayers({civInt}) COMPLETE ===");
        }
        //private void ResetPlayers()

        private void SetCivMajorCivsInGame(List<CivEnum> majorCivsInGameList)
        {
            if (!IsSinglePlayer && NetworkServer.active)
                PlayerManager.Instance.SetMajorCivsInGameForMultiPlayer(majorCivsInGameList, localPlayerCiv);
            else if (IsSinglePlayer) { }
            // PlayerManager.Instance.SetMajorCivsInGameForSinglePlayer(majorCivsInGameList, localPlayerCiv);
        }
        public void SetMultiPlayer()
        {
            IsSinglePlayer = false;
            panelLobby.SetActive(false);
            panelMuliplayer.SetActive(true);
            panelCivSelection.SetActive(false);
            singlePlayToggleGroup.SetActive(false);
            GameController.Instance.GameData.GameMode = GameMode.MULTIPLAYER;
            UpdateNotInGame(); // Update player statuses based on toggles before starting the game
            //GameController.Instance.GameData.MajorCivsInGameList = majorCivsInGameList;
        }

        public void SetSinglePlayer() // button in Canvas MainMenu / Panel-Lobby when first loaded 
        {
            Debug.Log("=== SetSinglePlayer: Starting ===");

            IsSinglePlayer = true;
            panelLobby.SetActive(false);
            panelMuliplayer.SetActive(false);
            panelCivSelection.SetActive(true);
            singlePlayToggleGroup.SetActive(true);

            // ✅ CRITICAL FIX: Check if GameController exists
            if (GameController.Instance == null)
            {
                Debug.LogError("SetSinglePlayer: GameController.Instance is NULL!");
                Debug.LogError("  Is PersistentScene loaded? Is GameController GameObject active?");
                return;
            }

            // ✅ Check if GameData exists
            if (GameController.Instance.GameData == null)
            {
                Debug.LogError("SetSinglePlayer: GameController.Instance.GameData is NULL!");
                Debug.LogError("  GameController needs to initialize GameData in Awake() or Start()");
                return;
            }

            // ✅ Now safe to set
            GameController.Instance.GameData.GameMode = GameMode.SINGLEPLAYER;
            UpdateNotInGame(); // Update player statuses based on toggles before starting the game
            //GameController.Instance.GameData.MajorCivsInGameList = majorCivsInGameList;

            Debug.Log($"  ✅ Set GameMode to SINGLEPLAYER");

            // ✅ Check if CombatUIManager exists
            if (CombatUIManager.Instance == null)
            {
                Debug.Log("SetSinglePlayer: CombatUIManager.Instance is NULL - combat UI won't be initialized yet");
            }
            else
            {
                CombatUIManager.Instance.CivEnumLocalPlayer = localPlayerCiv;
                Debug.Log($"  ✅ Set CombatUIManager local player: {localPlayerCiv}");
            }

            // ✅ Check if NetworkManager exists
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("SetSinglePlayer: NetworkManager.singleton is NULL!");
                return;
            }

            // ✅ Only start if not already running
            if (!NetworkServer.active && !NetworkClient.isConnected)
            {
                NetworkManager.singleton.StartHost();
                Debug.Log("  ✅ Started host (network manager)");
            }
            else
            {
                Debug.Log("SetSinglePlayer: NetworkManager already running - skipping StartHost()");
            }

            Debug.Log("=== SetSinglePlayer: Complete ===");
        }

        public void FedOnOffToggleReset()
        {
            if (FedLocalPlayerToggle.isOn == true)
                FedOnOff.isOn = true;
        }
        public void RomOnOffToggleReset()
        {
            if (RomLocalPlayerToggle.isOn == true)
                RomOnOff.isOn = true;
        }
        public void KlingOnOffToggleReset()
        {
            if (KlingLocalPlayerToggle.isOn == true)
                KlingOnOff.isOn = true;
        }
        public void CardOnOffToggleReset()
        {
            if (CardLocalPlayerToggle.isOn == true)
                CardOnOff.isOn = true;
        }
        public void DomOnOffToggleReset()
        {
            if (DomLocalPlayerToggle.isOn == true)
                DomOnOff.isOn = true;
        }
        public void BorgOnOffToggleReset()
        {
            if (BorgLocalPlayerToggle.isOn == true)
                BorgOnOff.isOn = true;
        }
        public void TerranOnOffToggleReset()
        {
            if (TerranLocalPlayerToggle.isOn == true)
                TerranOnOff.isOn = true;
        }

        public void FedPlayToggleReset()
        {
            if (FedOnOff.isOn == false && FedLocalPlayerToggle.isOn == true)
                RomLocalPlayerToggle.isOn = true;
        }
        public void RomPlayToggleReset()
        {
            if (RomOnOff.isOn == false && RomLocalPlayerToggle.isOn == true)
                KlingLocalPlayerToggle.isOn = true;
        }
        public void KlingPlayToggleReset()
        {
            if (KlingOnOff.isOn == false && KlingLocalPlayerToggle.isOn == true)
                CardLocalPlayerToggle.isOn = true;
        }
        public void CardPlayToggleReset()
        {
            if (CardOnOff.isOn == false && CardLocalPlayerToggle.isOn == true)
                DomLocalPlayerToggle.isOn = true;
        }
        public void DomPlayToggleReset()
        {
            if (DomOnOff.isOn == false && DomLocalPlayerToggle.isOn == true)
                BorgLocalPlayerToggle.isOn = true;
        }
        public void BorgPlayerToggleReset()
        {
            if (BorgOnOff.isOn == false && BorgLocalPlayerToggle.isOn == true)
                TerranLocalPlayerToggle.isOn = true;
        }
        public void TerranPlayerToggleReset()
        {
            if (TerranOnOff.isOn == false && TerranLocalPlayerToggle.isOn == true)
                FedLocalPlayerToggle.isOn = true;
        }

        public void PreviousButton()
        {
            panelMuliplayer.SetActive(false);
            panelCivSelection.SetActive(false);
            panelGamePara.SetActive(false);
            panelLobby.SetActive(true);
        }
        public void SaveButton()
        {
            Debug.Log($"SaveButton: localPlayerCiv = {localPlayerCiv} (index {(int)localPlayerCiv})");

            UpdatePlayers();      // This calls ActivePlayerToggle() → ResetPlayer(civInt)
            UpdateNotInGame();    // This handles "Absent" states

            // ❌ REMOVE THIS LINE - Don't call ResetPlayer again!
            // ResetPlayer((int)localPlayerCiv);  

            panelLobby.SetActive(false);
            panelMuliplayer.SetActive(false);
            panelCivSelection.SetActive(false);
            panelGamePara.SetActive(true);

            Debug.Log($"SaveButton: Complete. Check Panel-GameParametersMenu player list.");
        }
        public void OpenSettingButton()
        {
            settingsMenuView.SetActive(true);
            closeSettingsButton.SetActive(true);
        }
        public void CloseSettingsMenu()
        {
            settingsMenuView.SetActive(false);
            closeSettingsButton.SetActive(false);
        }
        public void ReturnButton()
        {
            // Restore the local player selection
            ResetPlayers((int)localPlayerCiv);
            panelLobby.SetActive(false);
            panelMuliplayer.SetActive(false);
            panelCivSelection.SetActive(true);
            panelGamePara.SetActive(false);
        }

        private void SetGalaxySize(int index)
        {
            this.MainMenuData.SelectedGalaxySize = (GalaxySize)index;
        }

        private void SetMapGalaxyType(int index)
        {
            this.MainMenuData.SelectedGalaxyType = (GalaxyMapType)index;
        }

        private void SetTechLevel(int index)
        {
            this.MainMenuData.SelectedTechLevel = (TechLevel)index;
        }

        private void SetLocalCivilization(int index)
        {
            // ✅ NULL-SAFE: Check GameManager exists
            if (GameManager.Instance == null)
            {
                Debug.LogError($"SetLocalCivilization({index}): GameManager.Instance is NULL!");
                return;
            }

            // ✅ NULL-SAFE: Check GameController exists
            if (GameManager.Instance.GameController == null)
            {
                Debug.LogError($"SetLocalCivilization({index}): GameController is NULL!");
                return;
            }

            // ✅ NULL-SAFE: Check GameData exists
            if (GameManager.Instance.GameController.GameData == null)
            {
                Debug.LogError($"SetLocalCivilization({index}): GameData is NULL!");
                return;
            }

            // ✅ Now safe to set
            GameManager.Instance.GameController.GameData.LocalPlayerCivEnum = (CivEnum)((int)index);
            localPlayerCiv = (CivEnum)((int)index);

            // ✅ NULL-SAFE: Check ThemeManager
            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.ApplyTheme((ThemeEnum)((int)index));
                Debug.Log($"SetLocalCivilization: Set to {localPlayerCiv}, applied theme");
            }
            else
            {
                Debug.LogWarning("SetLocalCivilization: ThemeManager.Instance is NULL");
            }
        }

        private void SetupLanguageButtons()
        {
            Debug.Log("SetupLanguageButtons: Wiring language buttons...");

            // ✅ Wire English button
            if (buttonEnglish != null)
            {
                buttonEnglish.onClick.RemoveAllListeners();
                buttonEnglish.onClick.AddListener(() => ChangeToEnglish());
                Debug.Log("✅ English button wired");
            }
            else
            {
                Debug.LogWarning("⚠️ buttonEnglish is NULL - not wired");
            }

            // ✅ Wire French button
            if (buttonFrench != null)
            {
                buttonFrench.onClick.RemoveAllListeners();
                buttonFrench.onClick.AddListener(() => ChangeToFrench());
                Debug.Log("✅ French button wired");
            }
            else
            {
                Debug.LogWarning("⚠️ buttonFrench is NULL - not wired");
            }

            // ✅ Wire German button
            if (buttonGerman != null)
            {
                buttonGerman.onClick.RemoveAllListeners();
                buttonGerman.onClick.AddListener(() => ChangeToGerman());
                Debug.Log("✅ German button wired");
            }
            else
            {
                Debug.LogWarning("⚠️ buttonGerman is NULL - not wired");
            }
            // ✅ Wire Spanish button
            if (buttonSpanish != null)
            {
                buttonSpanish.onClick.RemoveAllListeners();
                buttonSpanish.onClick.AddListener(() => ChangeToSpanish());
                Debug.Log("✅ Spanish button wired");
            }
            else
            {
                Debug.LogWarning("⚠️ buttonSpanish is NULL - not wired");
            }
            // ✅ Wire Italian button
            if (buttonItalian != null)
            {
                buttonItalian.onClick.RemoveAllListeners();
                buttonItalian.onClick.AddListener(() => ChangeToItalian());
                Debug.Log("✅ Italian button wired");
            }
            else
            {
                Debug.LogWarning("⚠️ buttonItalian is NULL - not wired");
            }
            // ✅ Wire Polish button
            if (buttonPolish != null)
            {
                buttonPolish.onClick.RemoveAllListeners();
                buttonPolish.onClick.AddListener(() => ChangeToPolish());
                Debug.Log("✅ Polish button wired");
            }
            else
            {
                Debug.LogWarning("⚠️ buttonPolish is NULL - not wired");
            }

            // ✅ Wire Portuguese button
            if (buttonPortuguese != null)
            {
                buttonPortuguese.onClick.RemoveAllListeners();
                buttonPortuguese.onClick.AddListener(() => ChangeToPortuguese());
                Debug.Log("✅ Portuguese button wired");
            }
            else
            {
                Debug.LogWarning("⚠️ buttonPortuguese is NULL - not wired");
            }

        }

        private void ChangeToEnglish()
        {
            // ✅ Always use Instance directly (don't cache it)
            if (LocaleManager.Instance == null)
            {
                Debug.LogError("ChangeToEnglish: LocaleManager.Instance is NULL! Cannot change language.");
                return;
            }

            var locale = GetLocaleByCode("en");
            if (locale != null)
            {
                LocaleManager.Instance.ChangeLanguage(locale);
                Debug.Log("✅ Language changed to English");
            }
            else
            {
                Debug.LogError("❌ English locale not found!");
            }
        }

        private void ChangeToFrench()
        {
            // ✅ Always use Instance directly (don't cache it)
            if (LocaleManager.Instance == null)
            {
                Debug.LogError("ChangeToFrench: LocaleManager.Instance is NULL! Cannot change language.");
                return;
            }

            var locale = GetLocaleByCode("fr");
            if (locale != null)
            {
                LocaleManager.Instance.ChangeLanguage(locale);
                Debug.Log("✅ Language changed to French");
            }
            else
            {
                Debug.LogError("❌ French locale not found!");
            }
        }

        private void ChangeToGerman()
        {
            // ✅ Always use Instance directly (don't cache it)
            if (LocaleManager.Instance == null)
            {
                Debug.LogError("ChangeToGerman: LocaleManager.Instance is NULL! Cannot change language.");
                return;
            }

            var locale = GetLocaleByCode("de");
            if (locale != null)
            {
                LocaleManager.Instance.ChangeLanguage(locale);
                Debug.Log("✅ Language changed to German");
            }
            else
            {
                Debug.LogError("❌ German locale not found!");
            }
        }
        private void ChangeToSpanish()
        {
            // ✅ Always use Instance directly (don't cache it)
            if (LocaleManager.Instance == null)
            {
                Debug.LogError("ChangeToSpanish: LocaleManager.Instance is NULL! Cannot change language.");
                return;
            }

            var locale = GetLocaleByCode("es");
            if (locale != null)
            {
                LocaleManager.Instance.ChangeLanguage(locale);
                Debug.Log("✅ Language changed to Spanish");
            }
            else
            {
                Debug.LogError("❌ Spanish locale not found!");
            }
        }
        private void ChangeToPolish()
        {
            // ✅ Always use Instance directly (don't cache it)
            if (LocaleManager.Instance == null)
            {
                Debug.LogError("ChangeToPolish: LocaleManager.Instance is NULL! Cannot change language.");
                return;
            }

            var locale = GetLocaleByCode("pl");
            if (locale != null)
            {
                LocaleManager.Instance.ChangeLanguage(locale);
                Debug.Log("✅ Language changed to Polish");
            }
            else
            {
                Debug.LogError("❌ Polish locale not found!");
            }
        }
        private void ChangeToItalian()
        {
            // ✅ Always use Instance directly (don't cache it)
            if (LocaleManager.Instance == null)
            {
                Debug.LogError("ChangeToItalian: LocaleManager.Instance is NULL! Cannot change language.");
                return;
            }

            var locale = GetLocaleByCode("it");
            if (locale != null)
            {
                LocaleManager.Instance.ChangeLanguage(locale);
                Debug.Log("✅ Language changed to Italian");
            }
            else
            {
                Debug.LogError("❌ Italian locale not found!");
            }
        }
        private void ChangeToPortuguese()
        {
            // ✅ Always use Instance directly (don't cache it)
            if (LocaleManager.Instance == null)
            {
                Debug.LogError("ChangeToPortuguese: LocaleManager.Instance is NULL! Cannot change language.");
                return;
            }

            var locale = GetLocaleByCode("pt-BR"); // ✅ Changed from "pt" to "pt-BR"
            if (locale != null)
            {
                LocaleManager.Instance.ChangeLanguage(locale);
                Debug.Log("✅ Language changed to Portuguese (Brazilian)");
            }
            else
            {
                Debug.LogError("❌ Portuguese locale not found!");
            }
        }
        /// <summary>
        /// Gets a Locale by language code (en, fr, de, etc.)
        /// </summary>
        private Locale GetLocaleByCode(string code)
        {
            var availableLocales = LocalizationSettings.AvailableLocales.Locales;

            foreach (var locale in availableLocales)
            {
                if (locale.Identifier.Code == code)
                {
                    return locale;
                }
            }

            Debug.LogWarning($"Locale with code '{code}' not found! Available locales: {string.Join(", ", availableLocales.Select(l => l.Identifier.Code))}");
            return null;
        }

        /// <summary>
        /// Shows images for the specified civilization, hiding all others.
        /// </summary>
        private void ShowCivImages(CivEnum civEnum)
        {
            TurnOffAllImages();

            switch (civEnum)
            {
                case CivEnum.FED:
                    fedImages.SetActive(true);
                    break;
                case CivEnum.ROM:
                    romImages.SetActive(true);
                    break;
                case CivEnum.KLING:
                    klingImages.SetActive(true);
                    break;
                case CivEnum.CARD:
                    cardImages.SetActive(true);
                    break;
                case CivEnum.DOM:
                    domImages.SetActive(true);
                    break;
                case CivEnum.BORG:
                    borgImages.SetActive(true);
                    break;
                case CivEnum.TERRAN:
                    terranImages.SetActive(true);
                    break;
            }

            Debug.Log($"ShowCivImages: Displaying {civEnum} images");
        }

        /// <summary>
        /// Updates all civilization toggle backgrounds - shows ONLY the selected toggle's background
        /// </summary>
        private void UpdateToggleBackgrounds(Toggle selectedToggle)
        {
            Debug.Log($"UpdateToggleBackgrounds: Selected toggle = {selectedToggle.name}");

            // ✅ Array of all civilization toggles
            Toggle[] allCivToggles = new Toggle[]
            {
                FedLocalPlayerToggle,
                RomLocalPlayerToggle,
                KlingLocalPlayerToggle,
                CardLocalPlayerToggle,
                DomLocalPlayerToggle,
                BorgLocalPlayerToggle,
                TerranLocalPlayerToggle
            };

            // ✅ Iterate through all toggles and update their background visibility
            foreach (var toggle in allCivToggles)
            {
                if (toggle == null) continue;

                // Find Background child
                Transform background = toggle.transform.Find("Background");
                if (background == null)
                {
                    Debug.LogWarning($"  Toggle '{toggle.name}' has no Background child!");
                    continue;
                }

                // Get Image component
                Image backgroundImage = background.GetComponent<Image>();
                if (backgroundImage == null)
                {
                    Debug.LogWarning($"  Toggle '{toggle.name}/Background' has no Image component!");
                    continue;
                }

                // ✅ Show background ONLY if this is the selected toggle
                bool shouldShow = (toggle == selectedToggle);
                backgroundImage.enabled = shouldShow;

                Debug.Log($"  {toggle.name}: Background.Image.enabled = {shouldShow}");
            }
        }
    }
}


