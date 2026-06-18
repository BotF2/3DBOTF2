using UnityEditor;
using UnityEngine;

namespace BOTF3D.Editor
{
    /// <summary>
    /// Simple utility to check for compilation errors.
    /// Run via: BOTF > Check Compilation
    /// </summary>
    public static class CompilationErrorChecker
    {
        [MenuItem("BOTF/Check Compilation")]
        public static void CheckCompilation()
        {
            // Force a recompile
            AssetDatabase.Refresh();

            Debug.Log("🔍 Checking for compilation errors...");
            Debug.Log("If Unity is currently showing errors, they will appear in the console.");
            Debug.Log("If no errors appear after this message, compilation is successful!");

            // Check if scripts are compiling
            if (EditorApplication.isCompiling)
            {
                Debug.Log("⏳ Unity is currently compiling scripts...");
            }
            else
            {
                Debug.Log("✅ No active compilation. Check console for any red error messages.");
            }
        }
    }
}
