using BOTF3D.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BOTF3D.Editor
{
    /// <summary>
    /// Unity Editor window for creating and testing combat scenarios.
    /// Allows quick setup of combat situations without full game context.
    /// Menu: BOTF > Combat Scenario Editor
    /// </summary>
    public class CombatScenarioEditor : EditorWindow
    {
        // Scenario settings
        private string scenarioName = "Test Scenario";
        private CivEnum sideOneCiv = CivEnum.FED;
        private TechLevel sideOneTechLevel = TechLevel.EARLY;
        private CombatOrders sideOneOrder = CombatOrders.Engage;
        private CivEnum sideTwoCiv = CivEnum.KLING;
        private TechLevel sideTwoTechLevel = TechLevel.EARLY;
        private CombatOrders sideTwoOrder = CombatOrders.Engage;

        // Ship composition, these are just sliders for quick setup - the actual ship creation logic will be in CombatScenarioRunner
        private int s1Scouts = 1;
        private int s1Destroyers = 1;
        private int s1Cruisers = 1;
        private int s1LtCruisers = 0;
        private int s1HvyCruisers = 0;
        private int s1Transports = 0;

        private int s2Scouts = 1;
        private int s2Destroyers = 1;
        private int s2Cruisers = 1;
        private int s2LtCruisers = 0;
        private int s2HvyCruisers = 0;
        private int s2Transports = 0;

        // Saved scenarios
        private List<BOTF3D.Combat.Testing.CombatScenario> savedScenarios = new List<BOTF3D.Combat.Testing.CombatScenario>();
        private Vector2 scenarioScrollPos;
        private Vector2 mainScrollPos;

        [MenuItem("BOTF/Combat Scenario Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<CombatScenarioEditor>("Combat Scenario Editor");
            window.minSize = new Vector2(500, 700);
        }

        private void OnEnable()
        {
            LoadScenarios();
        }

        private void OnGUI()
        {
            mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawScenarioSettings();
            EditorGUILayout.Space(10);

            DrawShipComposition();
            EditorGUILayout.Space(10);

            DrawActionButtons();
            EditorGUILayout.Space(10);

            DrawSavedScenarios();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("Combat Scenario Editor", headerStyle);
            EditorGUILayout.LabelField("Quickly set up and test combat situations", EditorStyles.centeredGreyMiniLabel);

            // Show manager status if in Play Mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                DrawManagerStatus();
            }
        }

        private void DrawManagerStatus()
        {
            // NOTE: ShipManager and CombatManager only exist in CombatScene, which loads when combat starts
            // We only validate TimeManager and SceneController here
            bool timeManager = BOTF3D.Core.TimeManager.Instance != null;
            bool sceneController = BOTF3D.Core.SceneController.Instance != null;

            bool allReady = timeManager && sceneController;

            GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = allReady ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f) }
            };

            string statusText = allReady
                ? "✅ Core Managers Ready (Combat managers load automatically)"
                : "⚠️ Core Managers Missing - Load PersistentScene + MainMenuScene, then start game";

            EditorGUILayout.LabelField(statusText, statusStyle);

            if (!allReady)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Show Details", GUILayout.Width(100)))
                {
                    ValidateGameManagers(); // Shows detailed dialog
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawScenarioSettings()
        {
            EditorGUILayout.LabelField("Scenario Settings", EditorStyles.boldLabel);

            scenarioName = EditorGUILayout.TextField("Scenario Name", scenarioName);

            EditorGUILayout.Space(5);

            // Side One
            EditorGUILayout.LabelField("Side One", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            sideOneCiv = (CivEnum)EditorGUILayout.EnumPopup("Civilization", sideOneCiv);
            sideOneTechLevel = (TechLevel)EditorGUILayout.EnumPopup("Tech Level", sideOneTechLevel);
            sideOneOrder = (CombatOrders)EditorGUILayout.EnumPopup("Combat Order", sideOneOrder);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);

            // Side Two
            EditorGUILayout.LabelField("Side Two", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            sideTwoCiv = (CivEnum)EditorGUILayout.EnumPopup("Civilization", sideTwoCiv);
            sideTwoTechLevel = (TechLevel)EditorGUILayout.EnumPopup("Tech Level", sideTwoTechLevel);
            sideTwoOrder = (CombatOrders)EditorGUILayout.EnumPopup("Combat Order", sideTwoOrder);
            EditorGUI.indentLevel--;
        }

        private void DrawShipComposition()
        {
            EditorGUILayout.LabelField("Ship Composition", EditorStyles.boldLabel);

            // Side One Ships
            EditorGUILayout.LabelField("Side One Fleet", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            s1Scouts     = DrawShipSlider("Scouts",         s1Scouts,     ShipType.Scout,      sideOneTechLevel);
            s1Destroyers = DrawShipSlider("Destroyers",     s1Destroyers, ShipType.Destroyer,  sideOneTechLevel);
            s1Cruisers   = DrawShipSlider("Cruisers",       s1Cruisers,   ShipType.Cruiser,    sideOneTechLevel);
            s1LtCruisers = DrawShipSlider("Light Cruisers", s1LtCruisers, ShipType.LtCruiser,  sideOneTechLevel);
            s1HvyCruisers= DrawShipSlider("Heavy Cruisers", s1HvyCruisers,ShipType.HvyCruiser, sideOneTechLevel);
            s1Transports = DrawShipSlider("Transports",     s1Transports, ShipType.Transport,  sideOneTechLevel);
            EditorGUI.indentLevel--;

            int s1Total = s1Scouts + s1Destroyers + s1Cruisers + s1LtCruisers + s1HvyCruisers + s1Transports;
            EditorGUILayout.LabelField($"Total: {s1Total} ships", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // Side Two Ships
            EditorGUILayout.LabelField("Side Two Fleet", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            s2Scouts     = DrawShipSlider("Scouts",         s2Scouts,     ShipType.Scout,      sideTwoTechLevel);
            s2Destroyers = DrawShipSlider("Destroyers",     s2Destroyers, ShipType.Destroyer,  sideTwoTechLevel);
            s2Cruisers   = DrawShipSlider("Cruisers",       s2Cruisers,   ShipType.Cruiser,    sideTwoTechLevel);
            s2LtCruisers = DrawShipSlider("Light Cruisers", s2LtCruisers, ShipType.LtCruiser,  sideTwoTechLevel);
            s2HvyCruisers= DrawShipSlider("Heavy Cruisers", s2HvyCruisers,ShipType.HvyCruiser, sideTwoTechLevel);
            s2Transports = DrawShipSlider("Transports",     s2Transports, ShipType.Transport,  sideTwoTechLevel);
            EditorGUI.indentLevel--;

            int s2Total = s2Scouts + s2Destroyers + s2Cruisers + s2LtCruisers + s2HvyCruisers + s2Transports;
            EditorGUILayout.LabelField($"Total: {s2Total} ships", EditorStyles.miniLabel);
        }

        /// <summary>
        /// Draws a ship count slider, greyed out and zeroed when the ship type is not available at the given tech level.
        /// </summary>
        private int DrawShipSlider(string label, int value, ShipType shipType, TechLevel techLevel)
        {
            bool available = BOTF3D.Combat.Testing.CombatScenarioRunner.IsShipTypeAvailableAtTechLevel(shipType, techLevel);
            if (!available) value = 0;
            EditorGUI.BeginDisabledGroup(!available);
            string displayLabel = available ? label : $"{label} (N/A at {techLevel})";
            value = EditorGUILayout.IntSlider(displayLabel, value, 0, 10);
            EditorGUI.EndDisabledGroup();
            return value;
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Start Combat", GUILayout.Height(40)))
            {
                StartCombatScenario();
            }

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Save Scenario", GUILayout.Height(40)))
            {
                SaveCurrentScenario();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Load Quick Test Scenario"))
            {
                LoadQuickTestScenario();
            }
        }

        private void DrawSavedScenarios()
        {
            EditorGUILayout.LabelField("Saved Scenarios", EditorStyles.boldLabel);

            if (savedScenarios.Count == 0)
            {
                EditorGUILayout.HelpBox("No saved scenarios. Create one and click 'Save Scenario'.", MessageType.Info);
                return;
            }

            scenarioScrollPos = EditorGUILayout.BeginScrollView(scenarioScrollPos, GUILayout.Height(200));

            for (int i = 0; i < savedScenarios.Count; i++)
            {
                var scenario = savedScenarios[i];

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(scenario.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{scenario.sideOneCiv} Tech{GetTechLevelRoman(scenario.sideOneTechLevel)} ({scenario.sideOneOrder}) vs {scenario.sideTwoCiv} Tech{GetTechLevelRoman(scenario.sideTwoTechLevel)} ({scenario.sideTwoOrder})", EditorStyles.miniLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Load", GUILayout.Width(60)))
                {
                    LoadScenario(scenario);
                }
                if (GUILayout.Button("Start", GUILayout.Width(60)))
                {
                    LoadScenario(scenario);
                    StartCombatScenario();
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    DeleteScenario(i);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.EndScrollView();
        }

        private void StartCombatScenario()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Cannot Start Combat",
                    "You must be in Play Mode to start a combat scenario.\n\nPress Play and try again.",
                    "OK");
                return;
            }

            // Validate that required managers exist
            if (!ValidateGameManagers())
            {
                return;
            }

            Debug.Log($"🎬 Starting combat scenario: {scenarioName}");

            // Find or create CombatScenarioRunner
            BOTF3D.Combat.Testing.CombatScenarioRunner runner = GameObject.FindAnyObjectByType<BOTF3D.Combat.Testing.CombatScenarioRunner>();

            if (runner == null)
            {
                GameObject runnerObj = new GameObject("CombatScenarioRunner");
                runner = runnerObj.AddComponent<BOTF3D.Combat.Testing.CombatScenarioRunner>();
                Debug.Log("✅ Created CombatScenarioRunner");
            }

            // Create scenario from current editor settings
            var scenario = new BOTF3D.Combat.Testing.CombatScenario
            {
                name = scenarioName,
                sideOneCiv = sideOneCiv,
                sideOneTechLevel = sideOneTechLevel,
                sideOneOrder = sideOneOrder,
                sideTwoCiv = sideTwoCiv,
                sideTwoTechLevel = sideTwoTechLevel,
                sideTwoOrder = sideTwoOrder,
                s1Scouts = s1Scouts,
                s1Destroyers = s1Destroyers,
                s1Cruisers = s1Cruisers,
                s1HvyCruisers = s1HvyCruisers,
                s1LtCruisers = s1LtCruisers,
                s1Transports = s1Transports,
                s2Scouts = s2Scouts,
                s2Destroyers = s2Destroyers,
                s2Cruisers = s2Cruisers,
                s2HvyCruisers = s2HvyCruisers,
                s2LtCruisers = s2LtCruisers,
                s2Transports = s2Transports
            };

            // Run the scenario
            runner.RunScenario(scenario);

            EditorUtility.DisplayDialog("Combat Scenario Started",
                $"Scenario '{scenarioName}' is now running!\n\n" +
                $"Side 1: {sideOneCiv} (Tech {sideOneTechLevel}) - {sideOneOrder}\n" +
                $"  Ships: {s1Scouts + s1Destroyers + s1Cruisers + s1LtCruisers + s1HvyCruisers + s1Transports}\n\n" +
                $"Side 2: {sideTwoCiv} (Tech {sideTwoTechLevel}) - {sideTwoOrder}\n" +
                $"  Ships: {s2Scouts + s2Destroyers + s2Cruisers + s2LtCruisers + s2HvyCruisers + s2Transports}\n\n" +
                "Combat will start in a few seconds. Press F1 for debug overlay.",
                "OK");
        }

        private void SaveCurrentScenario()
        {
            var scenario = new BOTF3D.Combat.Testing.CombatScenario
            {
                name = scenarioName,
                sideOneCiv = sideOneCiv,
                sideOneTechLevel = sideOneTechLevel,
                sideOneOrder = sideOneOrder,
                sideTwoCiv = sideTwoCiv,
                sideTwoTechLevel = sideTwoTechLevel,
                sideTwoOrder = sideTwoOrder,
                s1Scouts = s1Scouts,
                s1Destroyers = s1Destroyers,
                s1Cruisers = s1Cruisers,
                s1LtCruisers = s1LtCruisers,
                s1HvyCruisers = s1HvyCruisers,
                s1Transports = s1Transports,
                s2Scouts = s2Scouts,
                s2Destroyers = s2Destroyers,
                s2Cruisers = s2Cruisers,
                s2LtCruisers = s2LtCruisers,
                s2HvyCruisers = s2HvyCruisers,
                s2Transports = s2Transports
            };

            savedScenarios.Add(scenario);
            SaveScenarios();

            Debug.Log($"✅ Saved scenario: {scenarioName}");
        }

        private void LoadScenario(BOTF3D.Combat.Testing.CombatScenario scenario)
        {
            scenarioName = scenario.name;
            sideOneCiv = scenario.sideOneCiv;
            sideOneTechLevel = scenario.sideOneTechLevel;
            sideOneOrder = scenario.sideOneOrder;
            sideTwoCiv = scenario.sideTwoCiv;
            sideTwoTechLevel = scenario.sideTwoTechLevel;
            sideTwoOrder = scenario.sideTwoOrder;
            s1Scouts = scenario.s1Scouts;
            s1Destroyers = scenario.s1Destroyers;
            s1Cruisers = scenario.s1Cruisers;
            s1LtCruisers = scenario.s1LtCruisers;
            s1HvyCruisers = scenario.s1HvyCruisers;
            s1Transports = scenario.s1Transports;
            s2Scouts = scenario.s2Scouts;
            s2Destroyers = scenario.s2Destroyers;
            s2Cruisers = scenario.s2Cruisers;
            s2LtCruisers = scenario.s2LtCruisers;
            s2HvyCruisers = scenario.s2HvyCruisers;
            s2Transports = scenario.s2Transports;

            Debug.Log($"✅ Loaded scenario: {scenario.name}");
        }

        private void DeleteScenario(int index)
        {
            if (EditorUtility.DisplayDialog("Delete Scenario",
                $"Delete '{savedScenarios[index].name}'?",
                "Delete", "Cancel"))
            {
                savedScenarios.RemoveAt(index);
                SaveScenarios();
            }
        }

        private void LoadQuickTestScenario()
        {
            scenarioName = "Quick Test";
            sideOneCiv = CivEnum.FED;
            sideOneTechLevel = TechLevel.EARLY;
            sideOneOrder = CombatOrders.Engage;
            sideTwoCiv = CivEnum.KLING;
            sideTwoTechLevel = TechLevel.EARLY;
            sideTwoOrder = CombatOrders.Engage;
            s1Scouts = 1;
            s1Destroyers = 1;
            s1Cruisers = 0;
            s1LtCruisers = 0;
            s1HvyCruisers = 0;
            s1Transports = 0;
            s2Scouts = 1;
            s2Destroyers = 1;
            s2Cruisers = 0;
            s2LtCruisers = 0;
            s2HvyCruisers = 0;
            s2Transports = 0;

            Debug.Log("✅ Loaded quick test scenario");
        }

        // ===== PERSISTENCE =====

        private void SaveScenarios()
        {
            string json = JsonUtility.ToJson(new BOTF3D.Combat.Testing.ScenarioList { scenarios = savedScenarios }, true);
            EditorPrefs.SetString("CombatScenarios", json);
        }

        private void LoadScenarios()
        {
            string json = EditorPrefs.GetString("CombatScenarios", "");
            if (!string.IsNullOrEmpty(json))
            {
                var list = JsonUtility.FromJson<BOTF3D.Combat.Testing.ScenarioList>(json);
                savedScenarios = list.scenarios ?? new List<BOTF3D.Combat.Testing.CombatScenario>();
            }
        }

        /// <summary>
        /// Validate that core game managers are initialized
        /// NOTE: ShipManager and CombatManager are NOT validated here because they only exist
        /// in CombatScene, which loads when RequestCombat() is called. We only check the
        /// core managers that should exist in the galaxy scene.
        /// </summary>
        private bool ValidateGameManagers()
        {
            System.Text.StringBuilder missingManagers = new System.Text.StringBuilder();

            // Only validate core managers that exist in PersistentScene/MainMenuScene
            if (BOTF3D.Core.TimeManager.Instance == null)
                missingManagers.AppendLine("• TimeManager (PersistentScene)");

            if (BOTF3D.Core.SceneController.Instance == null)
                missingManagers.AppendLine("• SceneController (PersistentScene)");

            if (missingManagers.Length > 0)
            {
                EditorUtility.DisplayDialog("Core Managers Not Initialized",
                    "Cannot start combat - core managers are missing:\n\n" +
                    missingManagers.ToString() +
                    "\nTo use the Combat Scenario Editor:\n\n" +
                    "1. Load PersistentScene in Hierarchy\n" +
                    "2. Open MainMenuScene additively (right-click → Open Scene Additive)\n" +
                    "3. Press Play\n" +
                    "4. Click through menus to reach Galaxy view (Create Map)\n" +
                    "5. Then use Combat Scenario Editor\n\n" +
                    "NOTE: ShipManager and CombatManager will be created automatically\n" +
                    "when CombatScene loads (they don't exist yet - this is expected).",
                    "OK");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Convert TechLevel enum to Roman numerals for display
        /// </summary>
        private string GetTechLevelRoman(TechLevel level)
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
    }
}
