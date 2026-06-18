using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using Mirror;
using BOTF3D.UI;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 120);
        private static readonly float TestTimeout = SessionState.GetFloat("PlayModeTest.TestTimeout", 40.0f);

        private static List<string> _capturedLogs = new List<string>();
        private const int MaxCapturedLogs = 100;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            switch (state)
            {
                case "Idle": break;
                case "WaitingForCompile":
                    Debug.Log("[PlayModeTest] Bootstrap compiled. Scheduling Play Mode entry.");
                    EditorApplication.delayCall += () =>
                    {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                        EditorApplication.isPlaying = true;
                    };
                    break;
                case "EnteringPlayMode":
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;
                case "InPlayMode":
                    if (EditorApplication.isPlaying) EditorApplication.update += WaitFramesThenRun;
                    break;
                case "Done":
                    Debug.Log(SentinelLog);
                    EditorApplication.delayCall += SelfDestruct;
                    break;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += WaitFramesThenRun;
            }
        }

        private static int _frameCount = 0;
        private static bool _setupDone = false;
        private static bool _testDone = false;
        private static double _testStartTime = 0;

        private static void WaitFramesThenRun()
        {
            _frameCount++;
            if (_frameCount < WaitFrames) return;
            if (_testDone) return;

            if (!_setupDone)
            {
                _setupDone = true;
                Application.logMessageReceived += OnLogMessage;
                _testStartTime = EditorApplication.timeSinceStartup;
                try { Setup(); }
                catch (System.Exception e) { FinishTest(true, "Setup exception: " + e.Message); }
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - _testStartTime);
            bool timedOut = elapsed >= TestTimeout;
            try
            {
                bool complete = Tick(elapsed);
                if (complete || timedOut) FinishTest(timedOut && !complete, timedOut ? "Timeout" : null);
            }
            catch (System.Exception e) { FinishTest(true, "Tick exception: " + e.Message); }
        }

        private static void FinishTest(bool isError, string errorMessage)
        {
            _testDone = true;
            EditorApplication.update -= WaitFramesThenRun;
            Application.logMessageReceived -= OnLogMessage;
            string resultJson = GetResult(isError, errorMessage);
            SessionState.SetString(ResultKey, resultJson);
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count >= MaxCapturedLogs) return;
            _capturedLogs.Add("[" + type + "] " + message);
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.AssetPathExists(scriptPath)) AssetDatabase.DeleteAsset(scriptPath);
            SessionState.EraseString(StateKey);
            SessionState.EraseString(ScriptPathKey);
        }

        [System.Serializable]
        private class TestResult { public bool success; public string error; public string[] logs; }

        private static void Setup()
        {
            Debug.Log("[Test] Setup: Scenes should be already loaded by provisioner.");
        }

        private static bool _clicked = false;
        private static bool Tick(float elapsed)
        {
            if (elapsed < 5.0f) return false;

            if (!_clicked)
            {
                GameObject buttonGO = GameObject.Find("Button SinglePlayer");
                if (buttonGO == null) 
                {
                    Debug.Log("[Test] Searching for button by name failed, searching all buttons...");
                    Button[] allButtons = GameObject.FindObjectsByType<Button>(FindObjectsSortMode.None);
                    foreach(var b in allButtons)
                    {
                        if (b.name.Contains("SinglePlayer"))
                        {
                            buttonGO = b.gameObject;
                            break;
                        }
                    }
                }

                if (buttonGO == null) 
                {
                    Debug.LogWarning("[Test] Could not find SinglePlayer button, maybe it's too early?");
                    return false;
                }

                Button button = buttonGO.GetComponent<Button>();
                if (button == null) throw new System.Exception("Button component not found");

                Debug.Log("[Test] Button found: " + buttonGO.name);
                Debug.Log("[Test] Clicking SinglePlayer button...");
                button.onClick.Invoke();
                _clicked = true;
                return false;
            }

            if (NetworkServer.active)
            {
                Debug.Log("[Test] SinglePlayer started successfully (NetworkServer.active = true)");
                return true;
            }

            return false;
        }

        private static string GetResult(bool isError, string errorMessage)
        {
            bool success = !isError && _clicked && NetworkServer.active;
            var result = new TestResult { success = success, error = errorMessage, logs = _capturedLogs.ToArray() };
            return JsonUtility.ToJson(result);
        }
    }
}
