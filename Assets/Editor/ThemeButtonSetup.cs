using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using BOTF3D.UI;

namespace BOTF3D.Editor
{
    /// <summary>
    /// Adds ThemedUIElement to every Button and Toggle in the open scene.
    /// Open GalaxyScene (or CombatScene), run the menu item, then save.
    /// </summary>
    public static class ThemeButtonSetup
    {
        [MenuItem("BOTF/Theme/Apply Theme Buttons — Current Scene")]
        public static void ApplyThemeToCurrentScene()
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var toggles = Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int added = 0, skipped = 0;

            foreach (var button in buttons)
            {
                if (!IsGameSceneObject(button.gameObject)) continue;

                if (button.GetComponent<ThemedUIElement>() != null)
                {
                    skipped++;
                    continue;
                }

                var themed = Undo.AddComponent<ThemedUIElement>(button.gameObject);
                themed.ThemeTarget = ThemeTarget.Button;
                themed.ButtonSpriteSlot = 0;
                themed.ApplyOnStart = true;
                EditorUtility.SetDirty(button.gameObject);
                added++;
            }

            foreach (var toggle in toggles)
            {
                if (!IsGameSceneObject(toggle.gameObject)) continue;

                if (toggle.GetComponent<ThemedUIElement>() != null)
                {
                    skipped++;
                    continue;
                }

                var themed = Undo.AddComponent<ThemedUIElement>(toggle.gameObject);
                themed.ThemeTarget = ThemeTarget.BackgroundColor;
                themed.ColorType = ThemeColorType.Background;
                themed.ApplyOnStart = true;
                EditorUtility.SetDirty(toggle.gameObject);
                added++;
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[ThemeButtonSetup] Added ThemedUIElement to {added} elements; {skipped} already had it. Save the scene to persist.");
        }

        [MenuItem("BOTF/Theme/Remove Theme Buttons — Current Scene")]
        public static void RemoveThemeFromCurrentScene()
        {
            var elements = Object.FindObjectsByType<ThemedUIElement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int removed = 0;

            foreach (var element in elements)
            {
                if (!IsGameSceneObject(element.gameObject)) continue;
                Undo.DestroyObjectImmediate(element);
                removed++;
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[ThemeButtonSetup] Removed ThemedUIElement from {removed} elements. Save the scene to persist.");
        }

        // Only process objects that live in project game scenes, not Mirror/package scenes.
        private static bool IsGameSceneObject(GameObject go)
        {
            if (!go.scene.IsValid()) return false;
            var path = go.scene.path;
            return !path.Contains("/Mirror/") && !path.Contains("/Packages/") && !path.Contains("Examples");
        }
    }
}
