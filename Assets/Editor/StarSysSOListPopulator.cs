using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BOTF3D.Galaxy;

namespace BOTF3D.Editor
{
    /// <summary>
    /// One-time utility: populates StarSysManager.starSysSOList in GalaxyScene from
    /// Assets/SO/StarSysSO/ so those assets are serialized into the scene and included
    /// in every standalone build (no Resources folder required).
    ///
    /// Run once via:  BOTF > Fix > Populate StarSysSO List in GalaxyScene
    /// Re-run if you add new StarSysSO assets.
    /// </summary>
    public static class StarSysSOListPopulator
    {
        private const string GALAXY_SCENE_PATH = "Assets/Scenes/GalaxyScene.unity";
        private const string SO_SEARCH_PATH    = "Assets/SO/StarSysSO";

        [MenuItem("BOTF/Fix/Populate StarSysSO List in GalaxyScene")]
        public static void PopulateStarSysSOList()
        {
            // 1. Load all StarSysSO assets
            string[] guids = AssetDatabase.FindAssets("t:StarSysSO", new[] { SO_SEARCH_PATH });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Assets Found",
                    $"No StarSysSO assets were found in {SO_SEARCH_PATH}.\n" +
                    "Check that the path is correct.", "OK");
                return;
            }

            List<StarSysSO> allSOs = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<StarSysSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(so => so != null)
                .OrderBy(so => so.StarSysInt)
                .ToList();

            Debug.Log($"StarSysSOListPopulator: Found {allSOs.Count} StarSysSO assets in {SO_SEARCH_PATH}");

            // 2. Open GalaxyScene (or switch to it if already open)
            Scene galaxyScene = EditorSceneManager.GetSceneByPath(GALAXY_SCENE_PATH);
            bool wasAlreadyOpen = galaxyScene.isLoaded;

            if (!wasAlreadyOpen)
            {
                bool proceed = EditorUtility.DisplayDialog("Open GalaxyScene?",
                    "GalaxyScene is not currently open.\n\n" +
                    "This tool needs to open it to update the StarSysManager component.\n\n" +
                    "Your current scene will be preserved (opened additively).",
                    "Open Additively", "Cancel");

                if (!proceed) return;

                galaxyScene = EditorSceneManager.OpenScene(GALAXY_SCENE_PATH, OpenSceneMode.Additive);
            }

            // 3. Find StarSysManager in GalaxyScene
            StarSysManager starSysManager = null;
            foreach (GameObject root in galaxyScene.GetRootGameObjects())
            {
                starSysManager = root.GetComponentInChildren<StarSysManager>(true);
                if (starSysManager != null) break;
            }

            if (starSysManager == null)
            {
                EditorUtility.DisplayDialog("StarSysManager Not Found",
                    "Could not find a StarSysManager component in GalaxyScene.\n" +
                    "Make sure GalaxyScene has a GameObject with StarSysManager attached.",
                    "OK");

                if (!wasAlreadyOpen)
                    EditorSceneManager.CloseScene(galaxyScene, true);
                return;
            }

            // 4. Assign the SO list via SerializedObject so Undo/dirty tracking works
            SerializedObject so = new SerializedObject(starSysManager);
            SerializedProperty listProp = so.FindProperty("starSysSOList");

            if (listProp == null)
            {
                EditorUtility.DisplayDialog("Field Not Found",
                    "Could not find 'starSysSOList' field on StarSysManager.\n" +
                    "Check that the field name matches the private [SerializeField] list.",
                    "OK");

                if (!wasAlreadyOpen)
                    EditorSceneManager.CloseScene(galaxyScene, true);
                return;
            }

            listProp.ClearArray();
            listProp.arraySize = allSOs.Count;

            for (int i = 0; i < allSOs.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = allSOs[i];
            }

            so.ApplyModifiedProperties();

            // 5. Save GalaxyScene
            EditorSceneManager.MarkSceneDirty(galaxyScene);
            EditorSceneManager.SaveScene(galaxyScene);

            Debug.Log($"✅ StarSysSOListPopulator: Assigned {allSOs.Count} StarSysSO assets to " +
                      $"StarSysManager in GalaxyScene and saved.");

            EditorUtility.DisplayDialog("Done",
                $"Successfully assigned {allSOs.Count} StarSysSO assets to StarSysManager in GalaxyScene.\n\n" +
                "GalaxyScene has been saved. Rebuild the game to include these assets in the build.",
                "OK");
        }
    }
}
