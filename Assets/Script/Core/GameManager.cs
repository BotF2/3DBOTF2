// Ignore Spelling: Sys

using BOTF3D.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace BOTF3D.Core
{
    #region Enums
    public enum CivEnum
    {
        FED,
        ROM,
        KLING,
        CARD,
        DOM,
        BORG,
        TERRAN,
        #region minors
        ACAMARIANS,
        AKAALI,
        AKRITIRIANS,
        ALDEANS,
        ALGOLIANS,
        ALSAURIANS,
        ANDORIANS,
        ANGOSIANS,
        ANKARI,
        ANTEDEANS,
        ANTICANS,
        ARBAZAN,
        ARDANANS,
        ARGRATHI,
        ARKARIANS,
        ATREANS,
        AXANAR,
        BAJORANS,
        BAKU,
        BANDI,
        BANEANS,
        BARZANS,
        BENZITES,
        BETAZOIDS,
        BILANAIANS,
        BOLIANS,
        BOMAR,
        BOSLICS,
        BOTHA,
        BREELLIANS,
        BREEN,
        BREKKIANS,
        BYNARS,
        CAIRN,
        CALDONIANS,
        CAPELLANS,
        CHALNOTH,
        CORIDAN,
        CORVALLENS,
        CYTHERIANS,
        DELTANS,
        DENOBULANS,
        DEVORE,
        DOPTERIANS,
        DOSI,
        DRAI,
        DREMANS,
        EDO,
        ELAURIANS,
        ELAYSIANS,
        ENTHARANS,
        EVORA,
        EXCALBIANS,
        FERENGI,
        FLAXIANS,
        GORN,
        GRAZERITES,
        HAAKONIANS,
        HALKANS,
        HAZARI,
        HEKARANS,
        HIROGEN,
        HORTA,
        IYAARANS,
        JNAII,
        KAELON,
        KAREMMA,
        KAZON,
        KELLERUN,
        KESPRYTT,
        KLAESTRON,
        KRADIN,
        KREETASSANS,
        KRIOSIANS,
        KTARIANS,
        LEDOSIANS,
        LISSEPIANS,
        LOKIRRIM,
        LURIANS,
        MALCORIANS,
        MALON,
        MAQUIS,
        MARKALIANS,
        MERIDIANS,
        MINTAKANS,
        MIRADORN,
        MIZARIANS,
        MOKRA,
        MONEANS,
        NAUSICAANS,
        NECHANI,
        NEZU,
        NORCADIANS,
        NUMIRI,
        NUUBARI,
        NYRIANS,
        OCAMPA,
        ORIONS,
        ORNARANS,
        PAKLED,
        PARADANS,
        QUARREN,
        RAKHARI,
        RAKOSANS,
        RAMATIANS,
        RIGELIANS,
        RISIANS,
        RUTIANS,
        SELAY,
        SHELIAK,
        SIKARIANS,
        SKRREEA,
        SONA,
        SULIBAN,
        TAKARANS,
        TAKARIANS,
        TAKTAK,
        TALARIANS,
        TALAXIANS,
        TALOSIANS,
        TAMARIANS,
        TANUGANS,
        TELLARITES,
        TEPLANS,
        THOLIANS,
        TILONIANS,
        TLANI,
        TRABE,
        TRILL,
        TROGORANS,
        TZENKETHI,
        ULLIANS,
        VAADWAUR,
        VENTAXIANS,
        VHNORI,
        VIDIIANS,
        VISSIANS,
        VORGONS,
        VORI,
        VULCANS,
        WADI,
        XANTHANS,
        XEPOLITES,
        XINDI,
        XYRILLIANS,
        YADERANS,
        YRIDIANS,
        ZAHL,
        ZAKDORN,
        ZALKONIANS,
        ZIBALIANS,
        #endregion
        ZZUNINHABITED1,
        #region More Uninhabited
        ZZUNINHABITED2,
        ZZUNINHABITED3,
        ZZUNINHABITED4,
        ZZUNINHABITED5,
        ZZUNINHABITED6,
        ZZUNINHABITED7,
        ZZUNINHABITED8,
        ZZUNINHABITED9,
        ZZUNINHABITED10,
        ZZUNINHABITED11,
        ZZUNINHABITED12,
        ZZUNINHABITED13,
        ZZUNINHABITED14,
        ZZUNINHABITED15,
        ZZUNINHABITED16,
        ZZUNINHABITED17,
        ZZUNINHABITED18,
        ZZUNINHABITED19,
        ZZUNINHABITED20,
        ZZUNINHABITED21,
        ZZUNINHABITED22,
        ZZUNINHABITED23,
        ZZUNINHABITED24,
        ZZUNINHABITED25,
        ZZUNINHABITED26,
        ZZUNINHABITED27,
        ZZUNINHABITED28,
        ZZUNINHABITED29,
        ZZUNINHABITED30,
        ZZUNINHABITED31,
        ZZUNINHABITED32,
        ZZUNINHABITED33,
        ZZUNINHABITED34,
        ZZUNINHABITED35,
        ZZUNINHABITED36,
        ZZUNINHABITED37,
        ZZUNINHABITED38,
        ZZUNINHABITED39,
        ZZUNINHABITED40,
        ZZUNINHABITED41,
        ZZUNINHABITED42,
        ZZUNINHABITED43,
        ZZUNINHABITED44,
        ZZUNINHABITED45,
        ZZUNINHABITED46,
        ZZUNINHABITED47,
        ZZUNINHABITED48,
        ZZUNINHABITED49,
        ZZUNINHABITED50,
        ZZUNINHABITED51,
        ZZUNINHABITED52,
        ZZUNINHABITED53,
        None,
        #endregion
    }
    public enum GalaxyMapType
    {
        CANON,
        RANDOM,
        RING,
        WHATEVER
    }
    public enum GalaxySize
    {
        SMALL,
        MEDIUM,
        LARGE,
        PONDEROUS
    }
    public enum TechLevel
    {
        EARLY = 100,
        DEVELOPED = 300,
        ADVANCED = 600,
        SUPREME = 900
    }
    public enum GameMode
    {
        SINGLEPLAYER,
        MULTIPLAYER
    }
    public enum SystemData
    {
        Sys_Int,
        X_Vector3,
        Y_Vector3,
        Z_Vector3,
        Name,
        Civ_Owner,
        Sys_Type,
        Star_Type,
        Planet_1,
        Moons_1,
        Planet_2,
        Moons_2,
        Planet_3,
        Moons_3,
        Planet_4,
        Moons_4,
        Planet_5,
        Moons_5,
        Planet_6,
        Moons_6,
        Planet_7,
        Moons_7,
        Planet_8,
        Moons_8
    }

    public enum ShipType
    {
        Scout,
        Destroyer,
        Cruiser,
        LtCruiser,
        HvyCruiser,
        Transport,
        OneMore
    }

    public enum GalaxyObjectType
    {
        /// <summary>
        /// These galactic objects can be a 'system' 
        /// ( BlueStar through Nebulas and down to the UniComplex) 
        /// system GalaxyObjectTypes are inhabited with a planet or are planet like in the case of the UniComplex. 
        /// Also, system can be uninhabited star systems or nebulas with habitable planet so available for colonization by a transport ship in a visiting fleet
        /// The station will be treated as its own class with fields/properties, but like a fleet that does not move 
        /// The galactic objects black-holes and wormholes will have their own class with fields and/properties
        /// The Target Destination is user defined location in deep space.
        /// </summary>
        None = 0,
        BlueStar,
        WhiteStar,
        YellowStar,
        OrangeStar,
        RedStar,
        Nebula,
        OmarianNebula,
        OrionNebula,
        UniComplex,
        Station,
        TargetDestination,
        UnknownFleet,
        Fleet,
        BlackHole,
        WormHole,
        SomeBadAssGalacticObject,
    }
    public enum SystemOnOffButtons
    {
        FactoryOnButton,
        FactoryOffButton,
        PowerPlantOnButton,
        PowerPlantOffButton,
        ShipyardOnButton,
        ShipyardOffbutton,
        ShieldGeneratorOnButton,
        ShieldGeneratorOffbutton,
        OrbitalBatteryOnButton,
        OrbitalBatteryOffButton,
        ResearchCenterOnButton,
        ResearchCenterOffButton,

    }

    public enum PlanetType
    {
        H_uninhabitable,
        J_gasGiant,
        M_habitable,
        L_marginalForLife,
        K_marsLike,
        Moon

    }
    public enum CombatOrders
    {
        Engage, // default order
        Rush,
        Retreat,
        Formation, // aka protect transports
        TargetTransports,
        None,
    }
    public enum StarSysFacilityType
    {
        PowerPlanet,
        Factory,
        Shipyard,
        ShieldGenerator,
        OrbitalBattery,
        ResearchCenter
    }
    public enum CombatType
    {
        None,
        DeepSpaceCombat,
        StarSystemCombat,
        StarSystemInvasion
    }
    #endregion

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; } // a static singleton, no other script can instantiate a GameManager, must us the singleton

        [SerializeField]
        private MainMenuUIController mainMenuUIController;
        public GameController GameController;

        [Header("Galaxy Reference")]
        // Remove [SerializeField] - will be found at runtime
        private GameObject galaxyImageGO;

        // Cached bounds to avoid recalculating every frame
        private float galaxyWidth = -1f;
        private float galaxyHeight = -1f;


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
            InitializeGameManagerWithMainMenuUIController();
        }
        public void SetGameMode(GameMode mode)
        {
            GameController.Instance.GameData.GameMode = mode;
        }
        private void InitializeGameManagerWithMainMenuUIController()
        {
            // Don't use GameObject.Find - use the singleton Instance instead
            if (MainMenuUIController.Instance != null)
            {
                mainMenuUIController = MainMenuUIController.Instance;
                Debug.Log("GameManager: Found MainMenuUIController via Instance");
            }
            else
            {
                Debug.LogWarning("GameManager: MainMenuUIController.Instance is null - will retry when MainMenu scene loads");
            }
        }
        private void Start()
        {
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"GameManager: Scene loaded: {scene.name}");

            // When MainMenu scene loads, find the controller
            if (scene.name.Contains("MainMenu") && mainMenuUIController == null)
            {
                InitializeGameManagerWithMainMenuUIController();
            }

            // When GalaxyScene loads, find the galaxy image
            if (scene.name == "GalaxyScene")
            {
                FindGalaxyImageReference();
                CalculateGalaxyBounds();
            }
        }

        private void FindGalaxyImageReference()
        {
            // Find by name (adjust name to match your hierarchy)
            galaxyImageGO = GameObject.Find("GalaxyImage") ?? GameObject.Find("Galaxy");

            if (galaxyImageGO != null)
            {
                Debug.Log($"GameManager: Found galaxyImageGO: {galaxyImageGO.name}");
            }
            else
            {
                // Try finding by tag if you have one
                galaxyImageGO = GameObject.FindGameObjectWithTag("GalaxyImage");

                if (galaxyImageGO != null)
                {
                    Debug.Log($"GameManager: Found galaxyImageGO by tag: {galaxyImageGO.name}");
                }
                else
                {
                    Debug.LogWarning("GameManager: Could not find GalaxyImage GameObject in scene!");
                }
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public float GalaxyWidth
        {
            get
            {
                if (galaxyWidth < 0f)
                    CalculateGalaxyBounds();
                return galaxyWidth;
            }
        }

        public float GalaxyHeight
        {
            get
            {
                if (galaxyHeight < 0f)
                    CalculateGalaxyBounds();
                return galaxyHeight;
            }
        }
        private void CalculateGalaxyBounds()
        {
            if (galaxyImageGO == null)
            {
                Debug.LogWarning("GameManager: galaxyImageGO is null, using default galaxy bounds.");
                galaxyWidth = 200f;
                galaxyHeight = 200f;
                return;
            }

            var spriteRenderer = galaxyImageGO.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // SpriteRenderer.bounds gives world-space size after scale
                galaxyWidth = spriteRenderer.bounds.size.x;
                galaxyHeight = spriteRenderer.bounds.size.z; // or .y depending on your axis
                Debug.Log($"GameManager: Galaxy bounds calculated: width={galaxyWidth}, height={galaxyHeight}");
            }
            else
            {
                // Fallback: if Image component (UI) instead of SpriteRenderer
                var image = galaxyImageGO.GetComponent<Image>();
                var rectTransform = galaxyImageGO.GetComponent<RectTransform>();
                if (image != null && rectTransform != null)
                {
                    galaxyWidth = rectTransform.rect.width * rectTransform.lossyScale.x;
                    galaxyHeight = rectTransform.rect.height * rectTransform.lossyScale.y;
                    Debug.Log($"GameManager: Galaxy UI bounds calculated: width={galaxyWidth}, height={galaxyHeight}");
                }
                else
                {
                    Debug.LogWarning("GameManager: No SpriteRenderer or Image found, using default bounds.");
                    galaxyWidth = 200f;
                    galaxyHeight = 200f;
                }
            }
        }

        // Optional: force recalculation if galaxy scale changes at runtime
        public void RecalculateGalaxyBounds()
        {
            galaxyWidth = -1f;
            galaxyHeight = -1f;
            CalculateGalaxyBounds();
        }
    }
}