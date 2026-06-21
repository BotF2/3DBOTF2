using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.Editor
{
    /// <summary>
    /// Finds every Image inside a ScrollRect hierarchy that has Sprites-Default
    /// assigned as its material and clears it so Unity uses the correct UI material
    /// (which supports the stencil buffer required by Mask components).
    /// Run from BOTF/Fix/Clear Sprites-Default Material on UI Images.
    /// </summary>
    public static class FixSpritesDefaultMaterial
    {
        [MenuItem("BOTF/Fix/Clear Sprites-Default Material on UI Images")]
        public static void Fix()
        {
            var images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include);
            int cleared = 0;

            foreach (var img in images)
            {
                if (img.material == null) continue;
                if (img.material.name != "Sprites-Default") continue;

                Undo.RecordObject(img, "Clear Sprites-Default material");
                img.material = null;
                EditorUtility.SetDirty(img);
                cleared++;
                Debug.Log($"[FixMaterial] Cleared Sprites-Default on '{img.gameObject.name}' " +
                          $"(scene: {img.gameObject.scene.name})", img.gameObject);
            }

            if (cleared > 0)
            {
                EditorSceneManager.MarkAllScenesDirty();
                Debug.Log($"[FixMaterial] Cleared {cleared} Image(s). Save the scene to persist.");
            }

            EditorUtility.DisplayDialog("Fix Complete",
                cleared > 0
                    ? $"Cleared Sprites-Default from {cleared} Image(s).\nSave the scene to persist the fix."
                    : "No Images with Sprites-Default material found.",
                "OK");
        }
    }
}
