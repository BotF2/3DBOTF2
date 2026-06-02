using UnityEngine;
using UnityEditor;
using BOTF3D.Combat;
using BOTF3D.Core;
using BOTF3D.Civilization;
using System.Collections.Generic;
using System.Linq;

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
        private CombatOrders sideOneOrder = CombatOrders.Engage;
        private CombatOrders sideTwoOrder = CombatOrders.Rush;
        private CivEnum sideOneCiv = CivEnum.FED;
        private CivEnum sideTwoCiv = CivEnum.KLING;

        // Ship composition
        private int s1Scouts = 2;
        private int s1Destroyers = 2;
        private int s1Cruisers = 1;
        private int s1Battleships = 0;
        private int s1Transports = 1;

        private int s2Scouts = 2;
        private int s2Destroyers = 2;
        private int s2Cruisers = 1;
        private int s2Battleships = 0;
        private int s2Transports = 0;

        // Saved scenarios
        private List<CombatScenario> savedScenarios = new List<CombatScenario>();
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
            sideOneOrder = (CombatOrders)EditorGUILayout.EnumPopup("Order", sideOneOrder);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);

            // Side Two
            EditorGUILayout.LabelField("Side Two", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            sideTwoCiv = (CivEnum)EditorGUILayout.EnumPopup("Civilization", sideTwoCiv);
            sideTwoOrder = (CombatOrders)EditorGUILayout.EnumPopup("Order", sideTwoOrder);
            EditorGUI.indentLevel--;
        }

        private void DrawShipComposition()
        {
            EditorGUILayout.LabelField("Ship Composition", EditorStyles.boldLabel);

            // Side One Ships
            EditorGUILayout.LabelField("Side One Fleet", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            s1Scouts = EditorGUILayout.IntSlider("Scouts", s1Scouts, 0, 10);
            s1Destroyers = EditorGUILayout.IntSlider("Destroyers", s1Destroyers, 0, 10);
            s1Cruisers = EditorGUILayout.IntSlider("Cruisers", s1Cruisers, 0, 10);
            s1Battleships = EditorGUILayout.IntSlider("Battleships", s1Battleships, 0, 5);
            s1Transports = EditorGUILayout.IntSlider("Transports", s1Transports, 0, 10);
            EditorGUI.indentLevel--;

            int s1Total = s1Scouts + s1Destroyers + s1Cruisers + s1Battleships + s1Transports;
            EditorGUILayout.LabelField($"Total: {s1Total} ships", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // Side Two Ships
            EditorGUILayout.LabelField("Side Two Fleet", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            s2Scouts = EditorGUILayout.IntSlider("Scouts", s2Scouts, 0, 10);
            s2Destroyers = EditorGUILayout.IntSlider("Destroyers", s2Destroyers, 0, 10);
            s2Cruisers = EditorGUILayout.IntSlider("Cruisers", s2Cruisers, 0, 10);
            s2Battleships = EditorGUILayout.IntSlider("Battleships", s2Battleships, 0, 5);
            s2Transports = EditorGUILayout.IntSlider("Transports", s2Transports, 0, 10);
            EditorGUI.indentLevel--;

            int s2Total = s2Scouts + s2Destroyers + s2Cruisers + s2Battleships + s2Transports;
            EditorGUILayout.LabelField($"Total: {s2Total} ships", EditorStyles.miniLabel);
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
                EditorGUILayout.LabelField($"{scenario.sideOneCiv} ({scenario.sideOneOrder}) vs {scenario.sideTwoCiv} ({scenario.sideTwoOrder})", EditorStyles.miniLabel);

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

            Debug.Log($"🎬 Starting combat scenario: {scenarioName}");

            // TODO: This requires integration with your existing combat system
            // You'll need to create a CombatScenarioRunner component that can:
            // 1. Create a CombatData object with the specified parameters
            // 2. Instantiate ships based on the composition
            // 3. Load the combat scene
            // 4. Initialize the combat controller

            EditorUtility.DisplayDialog("Combat Scenario",
                $"Scenario '{scenarioName}' setup complete!\n\n" +
                $"Side 1: {sideOneCiv} ({sideOneOrder})\n" +
                $"Side 2: {sideTwoCiv} ({sideTwoOrder})\n\n" +
                "Note: Full combat integration requires CombatScenarioRunner component.",
                "OK");
        }

        private void SaveCurrentScenario()
        {
            var scenario = new CombatScenario
            {
                name = scenarioName,
                sideOneCiv = sideOneCiv,
                sideTwoCiv = sideTwoCiv,
                sideOneOrder = sideOneOrder,
                sideTwoOrder = sideTwoOrder,
                s1Scouts = s1Scouts,
                s1Destroyers = s1Destroyers,
                s1Cruisers = s1Cruisers,
                s1Battleships = s1Battleships,
                s1Transports = s1Transports,
                s2Scouts = s2Scouts,
                s2Destroyers = s2Destroyers,
                s2Cruisers = s2Cruisers,
                s2Battleships = s2Battleships,
                s2Transports = s2Transports
            };

            savedScenarios.Add(scenario);
            SaveScenarios();

            Debug.Log($"✅ Saved scenario: {scenarioName}");
        }

        private void LoadScenario(CombatScenario scenario)
        {
            scenarioName = scenario.name;
            sideOneCiv = scenario.sideOneCiv;
            sideTwoCiv = scenario.sideTwoCiv;
            sideOneOrder = scenario.sideOneOrder;
            sideTwoOrder = scenario.sideTwoOrder;
            s1Scouts = scenario.s1Scouts;
            s1Destroyers = scenario.s1Destroyers;
            s1Cruisers = scenario.s1Cruisers;
            s1Battleships = scenario.s1Battleships;
            s1Transports = scenario.s1Transports;
            s2Scouts = scenario.s2Scouts;
            s2Destroyers = scenario.s2Destroyers;
            s2Cruisers = scenario.s2Cruisers;
            s2Battleships = scenario.s2Battleships;
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
            sideTwoCiv = CivEnum.KLING;
            sideOneOrder = CombatOrders.Engage;
            sideTwoOrder = CombatOrders.Rush;
            s1Scouts = 1;
            s1Destroyers = 1;
            s1Cruisers = 1;
            s1Battleships = 0;
            s1Transports = 1;
            s2Scouts = 2;
            s2Destroyers = 1;
            s2Cruisers = 0;
            s2Battleships = 1;
            s2Transports = 0;

            Debug.Log("✅ Loaded quick test scenario");
        }

        // ===== PERSISTENCE =====

        private void SaveScenarios()
        {
            string json = JsonUtility.ToJson(new ScenarioList { scenarios = savedScenarios }, true);
            EditorPrefs.SetString("CombatScenarios", json);
        }

        private void LoadScenarios()
        {
            string json = EditorPrefs.GetString("CombatScenarios", "");
            if (!string.IsNullOrEmpty(json))
            {
                var list = JsonUtility.FromJson<ScenarioList>(json);
                savedScenarios = list.scenarios ?? new List<CombatScenario>();
            }
        }

        [System.Serializable]
        private class ScenarioList
        {
            public List<CombatScenario> scenarios;
        }
    }

    [System.Serializable]
    public class CombatScenario
    {
        public string name;
        public CivEnum sideOneCiv;
        public CivEnum sideTwoCiv;
        public CombatOrders sideOneOrder;
        public CombatOrders sideTwoOrder;
        public int s1Scouts;
        public int s1Destroyers;
        public int s1Cruisers;
        public int s1Battleships;
        public int s1Transports;
        public int s2Scouts;
        public int s2Destroyers;
        public int s2Cruisers;
        public int s2Battleships;
        public int s2Transports;
    }
}
