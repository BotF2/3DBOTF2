using BOTF3D.Core;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Combat.Testing
{
    /// <summary>
    /// Runtime Combat Scenario Editor for distribution builds.
    /// Mirrors the Assets/Editor/CombatScenarioEditor window using Unity IMGUI (OnGUI)
    /// so it works in standalone builds without any prefab setup.
    ///
    /// Setup: Add this component to any persistent GameObject (e.g. in PersistentScene).
    /// Toggle: Press F10 in-game. Scenarios persist across sessions via PlayerPrefs.
    ///
    /// Requirements before pressing Start Combat:
    ///   - PersistentScene loaded (TimeManager + SceneController must exist)
    ///   - Game must be past the main menu (Galaxy view or later)
    /// </summary>
    public class RuntimeScenarioEditorUI : MonoBehaviour
    {
        private const string PREFS_KEY = "CombatScenarios_Runtime";
        private const KeyCode TOGGLE_KEY = KeyCode.F10;

        [Tooltip("Show the editor immediately on startup — useful for dedicated testing builds.")]
        public bool ShowOnStart = false;

        // ── Window state ────────────────────────────────────────────────────────────
        private bool _visible;
        private Rect _windowRect = new Rect(20, 20, 560, 680);
        private Vector2 _scrollMain;
        private Vector2 _scrollSaved;

        // ── Per-side config ─────────────────────────────────────────────────────────
        private SideState _s1 = new SideState { scouts = 1, destroyers = 1 };
        private SideState _s2 = new SideState { scouts = 1, destroyers = 1 };
        private string _scenarioName = "Test Scenario";

        // ── Saved scenarios ─────────────────────────────────────────────────────────
        private List<CombatScenario> _saved = new List<CombatScenario>();

        // ── Cached enum arrays (built once) ─────────────────────────────────────────
        private static readonly CivEnum[] _civValues;
        private static readonly string[] _civNames;
        private static readonly TechLevel[] _techValues;
        private static readonly string[] _techLabels = { "I Early", "II Dev", "III Adv", "IV Sup" };
        private static readonly CombatOrders[] _orderValues;
        private static readonly string[] _orderNames;

        public object Instance { get; private set; }

        static RuntimeScenarioEditorUI()
        {
            _civValues = (CivEnum[])System.Enum.GetValues(typeof(CivEnum));
            _civNames = new string[_civValues.Length];
            for (int i = 0; i < _civValues.Length; i++) _civNames[i] = _civValues[i].ToString();

            _techValues = (TechLevel[])System.Enum.GetValues(typeof(TechLevel));

            _orderValues = (CombatOrders[])System.Enum.GetValues(typeof(CombatOrders));
            _orderNames = new string[_orderValues.Length];
            for (int i = 0; i < _orderValues.Length; i++) _orderNames[i] = _orderValues[i].ToString();
        }

        // ── Unity lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            LoadScenarios();

            Debug.Log("GalaxySceneInitializer: Awake - starting initialization sequence...");

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate GalaxySceneInitializer - destroying duplicate");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            // FindUIElement(currentCombatUICanvas, "PanelCombat_Menu");
            Debug.Log("✅ GalaxySceneInitializer initialized (persistent)");

        }

        private void Start()
        {
            _visible = ShowOnStart;

            // Default side 2 to KLING if it exists in the enum
            for (int i = 0; i < _civValues.Length; i++)
            {
                if (_civValues[i] == CivEnum.KLING) { _s2.civIdx = i; break; }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(TOGGLE_KEY)) _visible = !_visible;
        }

        // ── IMGUI rendering ─────────────────────────────────────────────────────────
        private void OnGUI()
        {
            if (!_visible) return;
            _windowRect = GUI.Window(9999, _windowRect, DrawWindow, "  Combat Scenario Editor      [F10 to close]");
            IMGUIBlocker.Register(_windowRect);
        }

        private void DrawWindow(int id)
        {
            _scrollMain = GUILayout.BeginScrollView(_scrollMain);

            DrawStatusBar();
            GUILayout.Space(6);
            DrawSidePanel("── SIDE ONE ──────────────────────────────────", _s1);
            GUILayout.Space(6);
            DrawSidePanel("── SIDE TWO ──────────────────────────────────", _s2);
            GUILayout.Space(8);
            DrawActionsRow();
            GUILayout.Space(6);
            DrawSavedScenariosList();

            GUILayout.EndScrollView();

            // Allow dragging by the title bar only (top 20 px)
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20));
        }

        private bool IsGalaxyPopulated()
        {
            return BOTF3D.Galaxy.StarSysManager.Instance != null &&
                   BOTF3D.Galaxy.StarSysManager.Instance.StarSysControllerList != null &&
                   BOTF3D.Galaxy.StarSysManager.Instance.StarSysControllerList.Count > 0;
        }

        private void DrawStatusBar()
        {
            bool coreReady = TimeManager.Instance != null && SceneController.Instance != null;
            bool galaxyReady = IsGalaxyPopulated();
            bool allReady = coreReady && galaxyReady;

            Color prev = GUI.color;

            if (!coreReady)
            {
                GUI.color = new Color(1f, 0.5f, 0.5f);
                GUILayout.Label("✗  Core managers missing — load PersistentScene, start the game, reach Galaxy view");
            }
            else if (!galaxyReady)
            {
                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUILayout.Label("⚠  Galaxy not yet generated — complete Create Map in the main menu first");
            }
            else
            {
                GUI.color = new Color(0.4f, 1f, 0.4f);
                GUILayout.Label("✔  Ready — configure below and press START COMBAT");
            }

            GUI.color = prev;
        }

        private void DrawSidePanel(string header, SideState side)
        {
            GUILayout.Label(header, GUI.skin.box);

            // Civilization (cycle with < > because there are 40+ entries)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Civilization:", GUILayout.Width(105));
            if (GUILayout.Button("◄", GUILayout.Width(26)))
                side.civIdx = (side.civIdx - 1 + _civValues.Length) % _civValues.Length;
            GUILayout.Label(_civNames[side.civIdx], GUI.skin.box, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("►", GUILayout.Width(26)))
                side.civIdx = (side.civIdx + 1) % _civValues.Length;
            GUILayout.EndHorizontal();

            // Tech Level (toolbar — 4 short entries fit well)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tech Level:", GUILayout.Width(105));
            side.techIdx = GUILayout.Toolbar(side.techIdx, _techLabels);
            GUILayout.EndHorizontal();

            // Combat Order (cycle — 7 entries would overflow a toolbar)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Order:", GUILayout.Width(105));
            if (GUILayout.Button("◄", GUILayout.Width(26)))
                side.orderIdx = (side.orderIdx - 1 + _orderValues.Length) % _orderValues.Length;
            GUILayout.Label(_orderNames[side.orderIdx], GUI.skin.box, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("►", GUILayout.Width(26)))
                side.orderIdx = (side.orderIdx + 1) % _orderValues.Length;
            GUILayout.EndHorizontal();

            // Fleet sliders
            TechLevel tech = _techValues[side.techIdx];
            GUILayout.Label("Fleet:");
            side.scouts = DrawShipSlider("Scouts", side.scouts, ShipType.Scout, tech);
            side.destroyers = DrawShipSlider("Destroyers", side.destroyers, ShipType.Destroyer, tech);
            side.cruisers = DrawShipSlider("Cruisers", side.cruisers, ShipType.Cruiser, tech);
            side.ltCruisers = DrawShipSlider("Lt Cruisers", side.ltCruisers, ShipType.LtCruiser, tech);
            side.hvyCruisers = DrawShipSlider("Hvy Cruisers", side.hvyCruisers, ShipType.HvyCruiser, tech);
            side.transports = DrawShipSlider("Transports", side.transports, ShipType.Transport, tech);

            int total = side.scouts + side.destroyers + side.cruisers + side.ltCruisers + side.hvyCruisers + side.transports;
            GUILayout.Label($"  Total: {total} ships");
        }

        private int DrawShipSlider(string label, int value, ShipType shipType, TechLevel tech)
        {
            bool available = CombatScenarioRunner.IsShipTypeAvailableAtTechLevel(shipType, tech);
            if (!available) value = 0;

            GUILayout.BeginHorizontal();
            GUI.enabled = available;
            GUILayout.Label(available ? $"{label}:" : $"{label} (N/A):", GUILayout.Width(120));
            value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, 0, 10, GUILayout.ExpandWidth(true)));
            GUILayout.Label(value.ToString("0"), GUILayout.Width(22));
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            return value;
        }

        private void DrawActionsRow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Scenario Name:", GUILayout.Width(110));
            _scenarioName = GUILayout.TextField(_scenarioName);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            Color prevBg = GUI.backgroundColor;

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("START COMBAT", GUILayout.Height(38)))
                StartCombat();

            GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            if (GUILayout.Button("SAVE SCENARIO", GUILayout.Height(38)))
                SaveCurrentScenario();

            GUI.backgroundColor = prevBg;
            GUILayout.EndHorizontal();

            GUILayout.Space(3);
            if (GUILayout.Button("Load Quick Test  (FED vs KLING, Early, Engage vs Engage)"))
                LoadQuickTest();
        }

        private void DrawSavedScenariosList()
        {
            GUILayout.Label($"── SAVED SCENARIOS ({_saved.Count}) ──────────────────────", GUI.skin.box);

            if (_saved.Count == 0)
            {
                GUILayout.Label("  No saved scenarios. Configure a scenario above and press SAVE.");
                return;
            }

            _scrollSaved = GUILayout.BeginScrollView(_scrollSaved, GUILayout.Height(170));

            for (int i = _saved.Count - 1; i >= 0; i--)
            {
                var s = _saved[i];
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{s.name}");
                GUILayout.Label($"  S1: {s.sideOneCiv} T{TechRoman(s.sideOneTechLevel)} ({s.sideOneOrder})   S2: {s.sideTwoCiv} T{TechRoman(s.sideTwoTechLevel)} ({s.sideTwoOrder})");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Load", GUILayout.Width(70)))
                    LoadScenarioIntoEditor(s);
                if (GUILayout.Button("Start", GUILayout.Width(70)))
                {
                    LoadScenarioIntoEditor(s);
                    StartCombat();
                }
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("Delete", GUILayout.Width(70)))
                {
                    _saved.RemoveAt(i);
                    PersistScenarios();
                }
                GUI.backgroundColor = prevBg;
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(2);
            }

            GUILayout.EndScrollView();
        }

        // ── Actions ─────────────────────────────────────────────────────────────────
        private void StartCombat()
        {
            if (TimeManager.Instance == null || SceneController.Instance == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat,
                    "RuntimeScenarioEditorUI: Cannot start — TimeManager or SceneController is null. " +
                    "Ensure PersistentScene is loaded and the game has been started.");
                return;
            }

            if (!IsGalaxyPopulated())
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat,
                    "RuntimeScenarioEditorUI: Cannot start — galaxy map is not yet generated. " +
                    "Complete the Create Map flow in the main menu before using the Combat Scenario Editor.");
                return;
            }

            CombatScenarioRunner runner = FindAnyObjectByType<CombatScenarioRunner>();
            if (runner == null)
            {
                var go = new GameObject("CombatScenarioRunner");
                runner = go.AddComponent<CombatScenarioRunner>();
            }

            runner.RunScenario(BuildScenario());
            _visible = false; // Hide the editor while combat runs
        }

        private void SaveCurrentScenario()
        {
            _saved.Add(BuildScenario());
            PersistScenarios();
        }

        private void LoadScenarioIntoEditor(CombatScenario s)
        {
            _scenarioName = s.name;
            FindEnumIndex(_civValues, s.sideOneCiv, out _s1.civIdx);
            FindEnumIndex(_civValues, s.sideTwoCiv, out _s2.civIdx);
            FindEnumIndex(_techValues, s.sideOneTechLevel, out _s1.techIdx);
            FindEnumIndex(_techValues, s.sideTwoTechLevel, out _s2.techIdx);
            FindEnumIndex(_orderValues, s.sideOneOrder, out _s1.orderIdx);
            FindEnumIndex(_orderValues, s.sideTwoOrder, out _s2.orderIdx);

            _s1.scouts = s.s1Scouts; _s1.destroyers = s.s1Destroyers; _s1.cruisers = s.s1Cruisers;
            _s1.ltCruisers = s.s1LtCruisers; _s1.hvyCruisers = s.s1HvyCruisers; _s1.transports = s.s1Transports;
            _s2.scouts = s.s2Scouts; _s2.destroyers = s.s2Destroyers; _s2.cruisers = s.s2Cruisers;
            _s2.ltCruisers = s.s2LtCruisers; _s2.hvyCruisers = s.s2HvyCruisers; _s2.transports = s.s2Transports;
        }

        private void LoadQuickTest()
        {
            _scenarioName = "Quick Test";
            FindEnumIndex(_civValues, CivEnum.FED, out _s1.civIdx);
            FindEnumIndex(_civValues, CivEnum.KLING, out _s2.civIdx);
            FindEnumIndex(_techValues, TechLevel.EARLY, out _s1.techIdx);
            FindEnumIndex(_techValues, TechLevel.EARLY, out _s2.techIdx);
            FindEnumIndex(_orderValues, CombatOrders.Engage, out _s1.orderIdx);
            FindEnumIndex(_orderValues, CombatOrders.Engage, out _s2.orderIdx);
            _s1.scouts = 1; _s1.destroyers = 1; _s1.cruisers = 0; _s1.ltCruisers = 0; _s1.hvyCruisers = 0; _s1.transports = 0;
            _s2.scouts = 1; _s2.destroyers = 1; _s2.cruisers = 0; _s2.ltCruisers = 0; _s2.hvyCruisers = 0; _s2.transports = 0;
        }

        // ── Persistence ─────────────────────────────────────────────────────────────
        private void PersistScenarios()
        {
            string json = JsonUtility.ToJson(new ScenarioList { scenarios = _saved }, true);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadScenarios()
        {
            string json = PlayerPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                var list = JsonUtility.FromJson<ScenarioList>(json);
                _saved = list?.scenarios ?? new List<CombatScenario>();
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────
        private CombatScenario BuildScenario() => new CombatScenario
        {
            name = _scenarioName,
            sideOneCiv = _civValues[_s1.civIdx],
            sideOneTechLevel = _techValues[_s1.techIdx],
            sideOneOrder = _orderValues[_s1.orderIdx],
            sideTwoCiv = _civValues[_s2.civIdx],
            sideTwoTechLevel = _techValues[_s2.techIdx],
            sideTwoOrder = _orderValues[_s2.orderIdx],
            s1Scouts = _s1.scouts,
            s1Destroyers = _s1.destroyers,
            s1Cruisers = _s1.cruisers,
            s1LtCruisers = _s1.ltCruisers,
            s1HvyCruisers = _s1.hvyCruisers,
            s1Transports = _s1.transports,
            s2Scouts = _s2.scouts,
            s2Destroyers = _s2.destroyers,
            s2Cruisers = _s2.cruisers,
            s2LtCruisers = _s2.ltCruisers,
            s2HvyCruisers = _s2.hvyCruisers,
            s2Transports = _s2.transports,
        };

        private static void FindEnumIndex<T>(T[] values, T target, out int index) where T : System.Enum
        {
            index = 0;
            for (int i = 0; i < values.Length; i++)
                if (values[i].Equals(target)) { index = i; return; }
        }

        private static string TechRoman(TechLevel level)
        {
            switch (level)
            {
                case TechLevel.EARLY: return "I";
                case TechLevel.DEVELOPED: return "II";
                case TechLevel.ADVANCED: return "III";
                case TechLevel.SUPREME: return "IV";
                default: return "?";
            }
        }

        // ── Inner types ─────────────────────────────────────────────────────────────
        private class SideState
        {
            public int civIdx;
            public int techIdx;
            public int orderIdx;
            public int scouts, destroyers, cruisers, ltCruisers, hvyCruisers, transports;
        }
    }
}
