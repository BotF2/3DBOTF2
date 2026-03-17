// Ignore Spelling: Kling
using BOTF3D.Audio;
using BOTF3D.Core;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
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

        // Remove [SerializeField] from these - they'll be found at runtime
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
        private readonly string player = "You", computer = "Computer", notInGame = "Absent";
        private Toggle activeLocalPlayerToggle;
        private CivEnum localPlayerCiv = CivEnum.FED;
        private List<CivEnum> majorCivsInGameList = new List<CivEnum>
        {
            CivEnum.FED, CivEnum.ROM, CivEnum.KLING, CivEnum.CARD, CivEnum.DOM, CivEnum.BORG, CivEnum.TERRAN
        };
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
        [SerializeField] private LocaleManager localeManager = LocaleManager.Instance;


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
            Instance = this;

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

            // ✅ Wire toggle events to update images directly
            FedLocalPlayerToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowCivImages(CivEnum.FED); });
            RomLocalPlayerToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowCivImages(CivEnum.ROM); });
            KlingLocalPlayerToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowCivImages(CivEnum.KLING); });
            CardLocalPlayerToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowCivImages(CivEnum.CARD); });
            DomLocalPlayerToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowCivImages(CivEnum.DOM); });
            BorgLocalPlayerToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowCivImages(CivEnum.BORG); });
            TerranLocalPlayerToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowCivImages(CivEnum.TERRAN); });
        }

        private void Start()
        {
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
            TimeManager.Instance.StarTime();
            UpdateMapSelection();
            UpdateGalaxySizeSelection();
            UpdateTechLevelSelection();
            UpdateNotInGame();
            CivManager.Instance.UpdatePlayableCivGameList(MainMenuData.InGamePlayableCivList, (int)MainMenuData.SelectedGalaxySize, this.MainMenuData.SelectedGalaxyType);

            Debug.Log("LoadGalaxyScene: Starting clean scene transition");

            // Store game settings before transition
            GameController.Instance.GameData.GameMode = IsSinglePlayer ? GameMode.SINGLEPLAYER : GameMode.MULTIPLAYER;
            GameController.Instance.GameData.MajorCivsInGameList = majorCivsInGameList;

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

            // Disable UI camera
            if (uiCamera != null)
            {
                uiCamera.enabled = false;
            }

            // Hide main menu canvas
            if (mainMenuCanvas != null)
            {
                var canvasComponent = mainMenuCanvas.GetComponent<Canvas>();
                if (canvasComponent != null)
                {
                    canvasComponent.enabled = false;
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

        private void FindAndActivateGalaxySceneReferences()
        {
            Debug.Log("FindAndActivateGalaxySceneReferences: Searching in loaded scenes...");
            // ✅ Play galaxy exploration music
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic("GalaxyExplorationTheme", crossfade: true);
                Debug.Log("🎵 Playing Galaxy music");
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


        // This is now only called when returning from Galaxy to MainMenu (e.g., quit to menu)
        public void ReturnToMainMenu()
        {
            Debug.Log("ReturnToMainMenu: Reloading MainMenu scene");

            // Simply reload the MainMenu scene - it will unload Galaxy automatically
            SceneManager.LoadScene("MainMenuScene");

            // Reset instance if this controller is destroyed
            Instance = null;
        }

        // No longer needed - kept for reference
        public void TransitionToGameplay()
        {
            // This method is now handled by LoadGalaxySceneCoroutine
            Debug.Log("TransitionToGameplay: Scene transition handled by LoadGalaxySceneCoroutine");
        }

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
            for (int i = 0; i < OnOffToggles.Count; i++)
            {
                if (OnOffToggles[i].isOn == false)
                {
                    switch (i)
                    {
                        case 0:
                            playerFed.text = notInGame;
                            majorCivsInGameList.Remove(CivEnum.FED);
                            break;
                        case 1:
                            playerRom.text = notInGame;
                            majorCivsInGameList.Remove(CivEnum.ROM);
                            break;
                        case 2:
                            playerKling.text = notInGame;
                            majorCivsInGameList.Remove(CivEnum.KLING);
                            break;
                        case 3:
                            playerCard.text = notInGame;
                            majorCivsInGameList.Remove(CivEnum.CARD);
                            break;
                        case 4:
                            playerDom.text = notInGame;
                            majorCivsInGameList.Remove(CivEnum.DOM);
                            break;
                        case 5:
                            playerBorg.text = notInGame;
                            majorCivsInGameList.Remove(CivEnum.BORG);
                            break;
                        case 6:
                            playerTerran.text = notInGame;
                            majorCivsInGameList.Remove(CivEnum.TERRAN);
                            break;
                        default:
                            break;
                    }
                }
                if (OnOffToggles[i].isOn == true)
                {
                    switch (i)
                    {
                        case 0:
                            if (!majorCivsInGameList.Contains(CivEnum.FED))
                            {
                                majorCivsInGameList.Add(CivEnum.FED);
                                if (localPlayerCiv == CivEnum.FED)
                                    playerFed.text = player;
                                else
                                    playerFed.text = computer;
                            }
                            break;
                        case 1:
                            if (!majorCivsInGameList.Contains(CivEnum.ROM))
                            {
                                majorCivsInGameList.Add(CivEnum.ROM);
                                if (localPlayerCiv == CivEnum.ROM)
                                    playerRom.text = player;
                                else
                                    playerRom.text = computer;
                            }
                            break;
                        case 2:
                            if (!majorCivsInGameList.Contains(CivEnum.KLING))
                            {
                                majorCivsInGameList.Add(CivEnum.KLING);
                                if (localPlayerCiv == CivEnum.KLING)
                                    playerKling.text = player;
                                else
                                    playerKling.text = computer;
                            }
                            break;
                        case 3:
                            if (!majorCivsInGameList.Contains(CivEnum.CARD))
                            {
                                majorCivsInGameList.Add(CivEnum.CARD);
                                if (localPlayerCiv == CivEnum.CARD)
                                    playerCard.text = player;
                                else
                                    playerCard.text = computer;
                            }
                            break;
                        case 4:
                            if (!majorCivsInGameList.Contains(CivEnum.DOM))
                            {
                                majorCivsInGameList.Add(CivEnum.DOM);
                                if (localPlayerCiv == CivEnum.DOM)
                                    playerDom.text = player;
                                else
                                    playerDom.text = computer;
                            }
                            break;
                        case 5:
                            if (!majorCivsInGameList.Contains(CivEnum.BORG))
                            {
                                majorCivsInGameList.Add(CivEnum.BORG);
                                if (localPlayerCiv == CivEnum.BORG)
                                    playerBorg.text = player;
                                else
                                    playerBorg.text = computer;
                            }
                            break;
                        case 6:
                            if (!majorCivsInGameList.Contains(CivEnum.TERRAN))
                            {
                                majorCivsInGameList.Add(CivEnum.TERRAN);
                                if (localPlayerCiv == CivEnum.TERRAN)
                                    playerTerran.text = player;
                                else
                                    playerTerran.text = computer;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            SetCivMajorCivsInGame(majorCivsInGameList);
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
                    PlaceTheYouInPlayerList(0);
                    break;

                case "TOGGLELOCAL_ROM":
                    romImages.SetActive(true);  // ✅ Show Rom images
                    RomOnOff.isOn = true;
                    RomOnOff.OnSelect(null);
                    RomLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active RomLocalPlayerToggle.");
                    SetLocalCivilization(1);
                    PlaceTheYouInPlayerList(1);
                    break;

                case "TOGGLELOCAL_KLING":
                    klingImages.SetActive(true);  // ✅ Show Kling images
                    KlingOnOff.isOn = true;
                    KlingOnOff.OnSelect(null);
                    KlingLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active KlingLocalPlayerToggle.");
                    SetLocalCivilization(2);
                    PlaceTheYouInPlayerList(2);
                    break;

                case "TOGGLELOCAL_CARD":
                    cardImages.SetActive(true);  // ✅ Show Card images
                    CardOnOff.isOn = true;
                    CardOnOff.OnSelect(null);
                    CardLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active CardLocalPlayerToggle.");
                    SetLocalCivilization(3);
                    PlaceTheYouInPlayerList(3);
                    break;

                case "TOGGLELOCAL_DOM":
                    domImages.SetActive(true);  // ✅ Show Dom images
                    DomOnOff.isOn = true;
                    DomOnOff.OnSelect(null);
                    DomLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active DomLocalPlayerToggle.");
                    SetLocalCivilization(4);
                    PlaceTheYouInPlayerList(4);
                    break;

                case "TOGGLELOCAL_BORG":
                    borgImages.SetActive(true);  // ✅ Show Borg images
                    BorgOnOff.isOn = true;
                    BorgOnOff.OnSelect(null);
                    BorgLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active BorgLocalPlayerToggle.");
                    SetLocalCivilization(5);
                    PlaceTheYouInPlayerList(5);
                    break;

                case "TOGGLELOCAL_TERRAN":
                    terranImages.SetActive(true);  // ✅ Show Terran images
                    TerranOnOff.isOn = true;
                    TerranOnOff.OnDeselect(null);
                    TerranLocalPlayerToggle = activeLocalPlayerToggle;
                    Debug.Log("Active TerranLocalPlayerToggle.");
                    SetLocalCivilization(6);
                    PlaceTheYouInPlayerList(6);
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
                    SetGalaxySize((int)GalaxySize.PONDEROUS);
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
        private void PlaceTheYouInPlayerList(int civInt)
        {
            switch (civInt)
            {
                case 0:
                    playerFed.text = player;
                    break;
                case 1:
                    playerRom.text = player;
                    break;
                case 2:
                    playerKling.text = player;
                    break;
                case 3:
                    playerCard.text = player;
                    break;
                case 4:
                    playerDom.text = player;
                    break;
                case 5:
                    playerBorg.text = player;
                    break;
                case 6:
                    playerTerran.text = player;
                    break;
                default:
                    break;
            }
        }

        private void ResetPlayers()
        {
            if (playerFed.text == player)
                playerFed.text = computer;
            if (playerRom.text == player)
                playerRom.text = computer;
            if (playerKling.text == player)
                playerKling.text = computer;
            if (playerCard.text == player)
                playerCard.text = computer;
            if (playerDom.text == player)
                playerDom.text = computer;
            if (playerBorg.text == player)
                playerBorg.text = computer;
            if (playerTerran.text == player)
                playerTerran.text = computer;
        }
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
            GameController.Instance.GameData.MajorCivsInGameList = majorCivsInGameList;
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
            GameController.Instance.GameData.MajorCivsInGameList = majorCivsInGameList;

            Debug.Log($"  ✅ Set GameMode to SINGLEPLAYER");

            // ✅ Check if CombatUIController exists
            if (CombatUIController.Instance == null)
            {
                Debug.LogWarning("SetSinglePlayer: CombatUIController.Instance is NULL - combat UI won't be initialized yet");
            }
            else
            {
                CombatUIController.Instance.CivEnumLocalPlayer = localPlayerCiv;
                Debug.Log($"  ✅ Set CombatUIController local player: {localPlayerCiv}");
            }

            // ✅ Check if NetworkManager exists
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("SetSinglePlayer: NetworkManager.singleton is NULL!");
                return;
            }

            NetworkManager.singleton.StartHost();
            Debug.Log("  ✅ Started host (network manager)");

            Debug.Log("=== SetSinglePlayer: Complete ===");
        }

        private void FedOnOffToggleReset()
        {
            if (FedLocalPlayerToggle.isOn == true)
                FedOnOff.isOn = true;
        }
        private void RomOnOffToggleReset()
        {
            if (RomLocalPlayerToggle.isOn == true)
                RomOnOff.isOn = true;
        }
        private void KlinOnOffToggleReset()
        {
            if (KlingLocalPlayerToggle.isOn == true)
                KlingOnOff.isOn = true;
        }
        private void CardOnOffToggleReset()
        {
            if (CardLocalPlayerToggle.isOn == true)
                CardOnOff.isOn = true;
        }
        private void DomOnOffToggleReset()
        {
            if (DomLocalPlayerToggle.isOn == true)
                DomOnOff.isOn = true;
        }
        private void BorgOnOffToggleReset()
        {
            if (BorgLocalPlayerToggle.isOn == true)
                BorgOnOff.isOn = true;
        }
        private void TerranOnOffToggleReset()
        {
            if (TerranLocalPlayerToggle.isOn == true)
                TerranOnOff.isOn = true;
        }

        private void FedPlayToggleReset()
        {
            if (FedOnOff.isOn == false && FedLocalPlayerToggle.isOn == true)
                RomLocalPlayerToggle.isOn = true;
        }
        private void RomPlayToggleReset()
        {
            if (RomOnOff.isOn == false && RomLocalPlayerToggle.isOn == true)
                KlingLocalPlayerToggle.isOn = true;
        }
        private void KlingPlayToggleReset()
        {
            if (KlingOnOff.isOn == false && KlingLocalPlayerToggle.isOn == true)
                CardLocalPlayerToggle.isOn = true;
        }
        private void CardPlayToggleReset()
        {
            if (CardOnOff.isOn == false && CardLocalPlayerToggle.isOn == true)
                DomLocalPlayerToggle.isOn = true;
        }
        private void DomPlayToggleReset()
        {
            if (DomOnOff.isOn == false && DomLocalPlayerToggle.isOn == true)
                BorgLocalPlayerToggle.isOn = true;
        }
        private void BorgPlayerToggleReset()
        {
            if (BorgOnOff.isOn == false && BorgLocalPlayerToggle.isOn == true)
                TerranLocalPlayerToggle.isOn = true;
        }
        private void TerranPlayerToggleReset()
        {
            if (TerranOnOff.isOn == false && TerranLocalPlayerToggle.isOn == true)
                FedLocalPlayerToggle.isOn = true;
        }

        private void CancelButton()
        {
            panelMuliplayer.SetActive(false);
            panelCivSelection.SetActive(false);
            panelGamePara.SetActive(false);
            panelLobby.SetActive(true);
        }
        private void SaveButton()
        {
            //singlePlayToggleGroup.SetActive(true);
            UpdatePlayers();
            UpdateNotInGame();
            panelLobby.SetActive(false);
            panelMuliplayer.SetActive(false);
            panelCivSelection.SetActive(false);
            panelGamePara.SetActive(true);

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
        private void ReturnButton()
        {
            ResetPlayers();
            panelLobby.SetActive(false);
            panelMuliplayer.SetActive(false);
            panelCivSelection.SetActive(true);
            panelGamePara.SetActive(false);
        }
        private void SetCivSelectionMenu(CivEnum civEnum)
        {

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
            GameManager.Instance.GameController.GameData.LocalPlayerCivEnum = (CivEnum)((int)index);
            localPlayerCiv = (CivEnum)((int)index);
            ThemeManager.Instance.ApplyTheme((ThemeEnum)((int)index));
        }

        private void SetupLanguageButtons()
        {
            if (localeManager == null)
            {
                localeManager = FindObjectOfType<LocaleManager>();
                if (localeManager == null)
                {
                    Debug.LogWarning("MainMenuUIController: LocaleManager not found!");
                    return;
                }
            }

            // ✅ Wire English button
            if (buttonEnglish != null)
            {
                buttonEnglish.onClick.RemoveAllListeners();
                buttonEnglish.onClick.AddListener(() => ChangeToEnglish());
                Debug.Log("✅ English button wired");
            }

            // ✅ Wire French button
            if (buttonFrench != null)
            {
                buttonFrench.onClick.RemoveAllListeners();
                buttonFrench.onClick.AddListener(() => ChangeToFrench());
                Debug.Log("✅ French button wired");
            }

            // ✅ Wire German button
            if (buttonGerman != null)
            {
                buttonGerman.onClick.RemoveAllListeners();
                buttonGerman.onClick.AddListener(() => ChangeToGerman());
                Debug.Log("✅ German button wired");
            }
        }

        private void ChangeToEnglish()
        {
            var locale = GetLocaleByCode("en");
            if (locale != null)
            {
                localeManager.ChangeLanguage(locale);
                Debug.Log("Language changed to English");
            }
        }

        private void ChangeToFrench()
        {
            var locale = GetLocaleByCode("fr");
            if (locale != null)
            {
                localeManager.ChangeLanguage(locale);
                Debug.Log("Language changed to French");
            }
        }

        private void ChangeToGerman()
        {
            var locale = GetLocaleByCode("de");
            if (locale != null)
            {
                localeManager.ChangeLanguage(locale);
                Debug.Log("Language changed to German");
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
    }
}


