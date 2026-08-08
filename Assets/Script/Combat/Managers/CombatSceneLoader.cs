using UnityEngine;
using UnityEngine.SceneManagement;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Handles finding and loading CombatScene references.
    /// Locates canvases and other scene objects needed for combat.
    /// </summary>
    public class CombatSceneLoader
    {
        public GameObject CombatUICanvas { get; private set; }
        public GameObject Combat3DCanvas { get; private set; }
        public GameObject GameOverCanvas { get; private set; }

        /// <summary>
        /// Find all combat scene references (canvases)
        /// </summary>
        public bool FindCombatSceneReferences()
        {
            Debug.Log("=== Finding CombatScene References ===");

            Scene combatScene = SceneManager.GetSceneByName("CombatScene");
            if (!combatScene.isLoaded)
            {
                Debug.LogError("❌ CombatScene is not loaded!");
                return false;
            }

            GameObject[] rootObjects = combatScene.GetRootGameObjects();
            Debug.Log($"🔍 Found {rootObjects.Length} root objects in CombatScene");

            // Search for canvases
            foreach (GameObject root in rootObjects)
            {
                CheckAndAssignCanvases(root);
                SearchChildrenForCanvases(root.transform);
            }

            // Validate
            bool success = true;

            if (CombatUICanvas == null)
            {
                Debug.LogError("❌ CombatUICanvas not found!");
                success = false;
            }
            else
            {
                Debug.Log($"✅ CombatUICanvas found: {CombatUICanvas.name}");
            }

            if (Combat3DCanvas == null)
            {
                Debug.LogError("❌ Combat3DCanvas not found!");
                success = false;
            }
            else
            {
                Debug.Log($"✅ Combat3DCanvas found: {Combat3DCanvas.name}");
            }

            if (GameOverCanvas == null)
            {
                // Scene object is named "CombatOverCanvas" (see CheckAndAssignCanvases) - kept as
                // "GameOverCanvas" here since that's the public property name CombatManager/
                // CombatUIManager already consume; only the scene-name lookup needed to change.
                Debug.LogError("❌ CombatOverCanvas not found!");
                success = false;
            }
            else
            {
                Debug.Log($"✅ CombatOverCanvas found: {GameOverCanvas.name}");
            }

            return success;
        }

        /// <summary>
        /// Check and assign canvas references
        /// </summary>
        private void CheckAndAssignCanvases(GameObject obj)
        {
            if (obj.name == "CombatUICanvas" && CombatUICanvas == null)
            {
                CombatUICanvas = obj;
            }
            else if (obj.name == "Combat3DCanvas" && Combat3DCanvas == null)
            {
                Combat3DCanvas = obj;
            }
            else if (obj.name == "CombatOverCanvas" && GameOverCanvas == null)
            {
                GameOverCanvas = obj;
            }
        }

        /// <summary>
        /// Recursively search children for canvases
        /// </summary>
        private void SearchChildrenForCanvases(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject child = parent.GetChild(i).gameObject;
                CheckAndAssignCanvases(child);
                SearchChildrenForCanvases(child.transform);
            }
        }

        /// <summary>
        /// Clear all canvas references
        /// </summary>
        public void ClearReferences()
        {
            CombatUICanvas = null;
            Combat3DCanvas = null;
            GameOverCanvas = null;
        }

        /// <summary>
        /// Get full hierarchy path of a GameObject for debugging
        /// </summary>
        public static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
