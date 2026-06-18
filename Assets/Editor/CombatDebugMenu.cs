using UnityEngine;
using UnityEditor;
using BOTF3D.Combat.Testing;

namespace BOTF3D.Editor
{
    /// <summary>
    /// Unity Editor menu items for combat debug tools.
    /// Provides quick access to testing features.
    /// </summary>
    public static class CombatDebugMenu
    {
        [MenuItem("BOTF/Combat Testing/Open Recordings Folder", priority = 1)]
        public static void OpenRecordingsFolder()
        {
            CombatTestingHelper.OpenRecordingsFolder();
        }

        [MenuItem("BOTF/Combat Testing/List Recordings", priority = 2)]
        public static void ListRecordings()
        {
            CombatTestingHelper.ListRecordings();
        }

        [MenuItem("BOTF/Combat Testing/Show Quick Guide", priority = 3)]
        public static void ShowQuickGuide()
        {
            CombatTestingHelper.PrintQuickGuide();
        }

        [MenuItem("BOTF/Combat Testing/---", priority = 10)]
        public static void Separator1() { }

        [MenuItem("BOTF/Combat Testing/Open Test Runner", priority = 11)]
        public static void OpenTestRunner()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }

        [MenuItem("BOTF/Combat Testing/Open README", priority = 12)]
        public static void OpenReadme()
        {
            string path = "Assets/Script/Combat/Testing/README_COMBAT_TESTING.md";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
            else
            {
                Debug.LogWarning($"README not found at: {path}");
            }
        }

        [MenuItem("BOTF/Combat Testing/---", priority = 20)]
        public static void Separator2() { }

        [MenuItem("BOTF/Combat Testing/Toggle Debug UI (F1)", priority = 21)]
        public static void ToggleDebugUI()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Not in Play Mode",
                    "Debug UI can only be toggled during Play Mode.\n\nPress Play and try again, or use the F1 key in-game.",
                    "OK");
                return;
            }

            Debug.Log("💡 Press F1 in the Game view to toggle debug UI");
        }

        [MenuItem("BOTF/Combat Testing/Toggle Debug UI (F1)", validate = true)]
        public static bool ToggleDebugUIValidate()
        {
            return Application.isPlaying;
        }

        [MenuItem("BOTF/Combat Testing/---", priority = 30)]
        public static void Separator3() { }

        [MenuItem("BOTF/Combat Testing/Run All Tests", priority = 31)]
        public static void RunAllTests()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            Debug.Log("💡 Test Runner opened. Click 'Run All' to execute tests.");
        }

        [MenuItem("BOTF/Combat Testing/Clear All Recordings", priority = 32)]
        public static void ClearRecordings()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "CombatRecordings");

            if (!System.IO.Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("No Recordings",
                    "No recordings folder found. Nothing to clear.",
                    "OK");
                return;
            }

            var files = System.IO.Directory.GetFiles(path, "*.json");

            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("No Recordings",
                    "Recordings folder is already empty.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog("Clear Recordings",
                $"Delete {files.Length} combat recording(s)?\n\nThis cannot be undone.",
                "Delete", "Cancel");

            if (confirmed)
            {
                foreach (var file in files)
                {
                    System.IO.File.Delete(file);
                }
                Debug.Log($"✅ Deleted {files.Length} combat recording(s)");
            }
        }

        [MenuItem("BOTF/Combat Testing/---", priority = 40)]
        public static void Separator4() { }

        [MenuItem("BOTF/Combat Testing/About", priority = 41)]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog("Combat Debug & Testing Tools",
                "Version: 1.0\n" +
                "Author: Claude Code\n" +
                "Date: 2026-06-01\n\n" +
                "Features:\n" +
                "• Automatic combat recording to JSON\n" +
                "• In-game debug overlay (F1)\n" +
                "• Combat scenario editor\n" +
                "• Automated unit tests\n" +
                "• Quick testing workflow\n\n" +
                "For documentation, see:\n" +
                "BOTF > Combat Testing > Open README",
                "OK");
        }
    }
}
