using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BOTF3D.Core;

namespace BOTF3D.Editor
{
    /// <summary>
    /// Populates TechManager.allTechDefs in PersistentScene from Assets/SO/TechDefSO/ so those
    /// 84 assets are serialized into the scene and included in every standalone build (no
    /// Resources folder required) - same convention as StarSysSOListPopulator.
    ///
    /// Run any time TechDefSOImporter authors new/changed TechDefSO assets:
    ///   BOTF > Fix > Populate TechDefSO List in PersistentScene
    /// </summary>
    public static class TechDefSOListPopulator
    {
        private const string PERSISTENT_SCENE_PATH = "Assets/Scenes/PersistentScene.unity";
        private const string SO_SEARCH_PATH = "Assets/SO/TechDefSO";

        [MenuItem("BOTF/Fix/Populate TechDefSO List in PersistentScene")]
        public static void PopulateTechDefList()
        {
            string[] guids = AssetDatabase.FindAssets("t:TechDefSO", new[] { SO_SEARCH_PATH });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Assets Found",
                    $"No TechDefSO assets were found in {SO_SEARCH_PATH}.\n" +
                    "Run Tools > Import TechDefSO CSVs first.", "OK");
                return;
            }

            List<TechDefSO> allDefs = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<TechDefSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(def => def != null)
                .OrderBy(def => def.Field)
                .ThenBy(def => def.Tier)
                .ThenBy(def => def.Id)
                .ToList();

            Debug.Log($"TechDefSOListPopulator: Found {allDefs.Count} TechDefSO assets in {SO_SEARCH_PATH}");

            Scene persistentScene = EditorSceneManager.GetSceneByPath(PERSISTENT_SCENE_PATH);
            bool wasAlreadyOpen = persistentScene.isLoaded;

            if (!wasAlreadyOpen)
            {
                bool proceed = EditorUtility.DisplayDialog("Open PersistentScene?",
                    "PersistentScene is not currently open.\n\n" +
                    "This tool needs to open it to update the TechManager component.\n\n" +
                    "Your current scene will be preserved (opened additively).",
                    "Open Additively", "Cancel");

                if (!proceed) return;

                persistentScene = EditorSceneManager.OpenScene(PERSISTENT_SCENE_PATH, OpenSceneMode.Additive);
            }

            TechManager techManager = null;
            foreach (GameObject root in persistentScene.GetRootGameObjects())
            {
                techManager = root.GetComponentInChildren<TechManager>(true);
                if (techManager != null) break;
            }

            if (techManager == null)
            {
                EditorUtility.DisplayDialog("TechManager Not Found",
                    "Could not find a TechManager component in PersistentScene.\n" +
                    "Make sure PersistentScene has a GameObject with TechManager attached.",
                    "OK");

                if (!wasAlreadyOpen)
                    EditorSceneManager.CloseScene(persistentScene, true);
                return;
            }

            SerializedObject so = new SerializedObject(techManager);
            SerializedProperty listProp = so.FindProperty("allTechDefs");

            if (listProp == null)
            {
                EditorUtility.DisplayDialog("Field Not Found",
                    "Could not find 'allTechDefs' field on TechManager.\n" +
                    "Check that the field name matches the private [SerializeField] list.",
                    "OK");

                if (!wasAlreadyOpen)
                    EditorSceneManager.CloseScene(persistentScene, true);
                return;
            }

            listProp.ClearArray();
            listProp.arraySize = allDefs.Count;
            for (int i = 0; i < allDefs.Count; i++)
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = allDefs[i];

            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(persistentScene);
            EditorSceneManager.SaveScene(persistentScene);

            Debug.Log($"✅ TechDefSOListPopulator: Assigned {allDefs.Count} TechDefSO assets to " +
                      $"TechManager in PersistentScene and saved.");

            EditorUtility.DisplayDialog("Done",
                $"Successfully assigned {allDefs.Count} TechDefSO assets to TechManager in PersistentScene.\n\n" +
                "PersistentScene has been saved. Rebuild the game to include these assets in the build.",
                "OK");
        }
    }
}
