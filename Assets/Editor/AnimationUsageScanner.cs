#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to scan the project for animation assets (.anim, animator controllers, overrides, fbx clips)
/// and report which assets are referenced by scenes/prefabs (via AssetDatabase.GetDependencies) and which
/// are not.
/// Usage: Window -> Animation Usage Scanner -> Run Scan (or Tools/Animation Usage Scanner/Run Scan)
/// The report is written to: Assets/Editor/AnimationUsageReport.txt
/// </summary>
public static class AnimationUsageScanner
{
    private static readonly string reportPath = "BOTF3D/Editor/AnimationUsageReport.txt";

    [MenuItem("Tools/Animation Usage Scanner/Run Scan")]
    public static void RunScanFromMenu()
    {
        RunScan();
        EditorUtility.DisplayDialog("Animation Usage Scanner", "Scan complete. Report written to:\n" + reportPath, "OK");
    }

    public static void RunScan()
    {
        var start = DateTime.Now;

        // Collect animation-related assets
        var animClipGuids = AssetDatabase.FindAssets("t:AnimationClip");
        var animatorControllerGuids = AssetDatabase.FindAssets("t:AnimatorController");
        var animatorOverrideGuids = AssetDatabase.FindAssets("t:AnimatorOverrideController");

        var animPaths = new HashSet<string>();

        foreach (var g in animClipGuids)
            animPaths.Add(AssetDatabase.GUIDToAssetPath(g));
        foreach (var g in animatorControllerGuids)
            animPaths.Add(AssetDatabase.GUIDToAssetPath(g));
        foreach (var g in animatorOverrideGuids)
            animPaths.Add(AssetDatabase.GUIDToAssetPath(g));

        // Also include .controller assets found under AssetDatabase.FindAssets without type filter (just in case)

        // Find all prefabs and scenes to check dependencies
        var prefabGuids = AssetDatabase.FindAssets("t:ShipFBX_ModelAsGOPrefab");
        var sceneGuids = AssetDatabase.FindAssets("t:Scene");
        var otherGameObjects = AssetDatabase.FindAssets("t:GameObject"); // catches some older prefab types

        var used = new HashSet<string>(); // asset paths marked as used via dependencies

        Action<string> collectDeps = (assetPath) =>
        {
            try
            {
                var deps = AssetDatabase.GetDependencies(assetPath, true);
                foreach (var d in deps)
                {
                    if (animPaths.Contains(d)) used.Add(d);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to get dependencies for {assetPath}: {e.Message}");
            }
        };

        int processed = 0;
        foreach (var g in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            collectDeps(path);
            processed++;
        }
        foreach (var g in sceneGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            collectDeps(path);
            processed++;
        }
        foreach (var g in otherGameObjects)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            collectDeps(path);
            processed++;
        }

        // Also check animator override controllers directly and their referenced clips
        foreach (var g in animatorOverrideGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            collectDeps(path);
        }

        // Additionally search all asset dependencies of scenes/prefabs for any fbx that contains clips
        // and mark them if any dependency name contains the word 'anim' -- conservative approach

        // Scan code for Resources.Load("...") string literals that may reference animation names
        var codeReferencedStrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dataPath = Application.dataPath;
        var csFiles = Directory.GetFiles(dataPath, "*.cs", SearchOption.AllDirectories);
        var resourcesLoadPattern = new Regex("Resources\\.Load(?:<[^>]+>)?\\s*\\(\\s*\"([^\"]+)\"", RegexOptions.Compiled);

        foreach (var file in csFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                foreach (Match m in resourcesLoadPattern.Matches(content))
                {
                    if (m.Groups.Count > 1)
                    {
                        var s = m.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(s)) codeReferencedStrings.Add(s);
                    }
                }
            }
            catch (Exception) { }
        }

        // Try to map code referenced strings to animation asset paths by filename
        var codeReferencedAssets = new HashSet<string>();
        foreach (var s in codeReferencedStrings)
        {
            // search by file name or by path ending
            foreach (var asset in animPaths)
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(asset);
                if (fileNameWithoutExt.Equals(s, StringComparison.OrdinalIgnoreCase) || asset.EndsWith(s + ".anim", StringComparison.OrdinalIgnoreCase) || asset.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    codeReferencedAssets.Add(asset);
                }
            }
        }

        // Merge code references into used
        foreach (var p in codeReferencedAssets) used.Add(p);

        // Any animation asset that contains "Warp" or "Torpedo" etc might be used dynamically; include heuristic
        var heuristics = new[] { "warp", "warpSignature", "torpedo", "ping", "explosion" };
        foreach (var asset in animPaths)
        {
            var name = Path.GetFileNameWithoutExtension(asset).ToLowerInvariant();
            foreach (var h in heuristics)
            {
                if (name.Contains(h))
                {
                    // don't mark automatically used, but list as heuristic candidate in report
                }
            }
        }

        var unused = animPaths.Except(used).OrderBy(x => x).ToList();
        var usedList = used.OrderBy(x => x).ToList();

        // Build report
        using (var sw = new StreamWriter(reportPath, false))
        {
            sw.WriteLine("Animation Usage Scan Report");
            sw.WriteLine("Generated: " + DateTime.Now);
            sw.WriteLine("Scan duration: " + (DateTime.Now - start).TotalSeconds + "s");
            sw.WriteLine();
            sw.WriteLine($"Total animation-related assets found: {animPaths.Count}");
            sw.WriteLine($"Total prefabs/scenes checked: {processed}");
            sw.WriteLine($"Animation assets referenced by prefabs/scenes/code: {usedList.Count}");
            sw.WriteLine($"Animation assets unreferenced (candidates for cleanup): {unused.Count}");
            sw.WriteLine();

            sw.WriteLine("-- Used animation assets (referenced by scenes/prefabs or code):");
            foreach (var p in usedList)
            {
                sw.WriteLine(p);
            }
            sw.WriteLine();

            sw.WriteLine("-- Unused animation assets (no dependency found):");
            foreach (var p in unused)
            {
                sw.WriteLine(p);
            }
            sw.WriteLine();

            sw.WriteLine("-- Strings found in code via Resources.Load(...) (may indicate runtime loads):");
            foreach (var s in codeReferencedStrings.OrderBy(x => x)) sw.WriteLine(s);
            sw.WriteLine();

            sw.WriteLine("-- BOTF3D matched to code strings (by filename/path):");
            foreach (var p in codeReferencedAssets) sw.WriteLine(p);
            sw.WriteLine();

            sw.WriteLine("-- Notes:");
            sw.WriteLine("This report relies on AssetDatabase.GetDependencies for prefabs and scenes and a simple regex for Resources.Load string literals.");
            sw.WriteLine("It may miss runtime-loaded assets referenced by custom addressables/assetbundles or generated at runtime.");
            sw.WriteLine("Before deleting assets listed as 'unused', verify they are not referenced by scenes not in the project or loaded by string from external sources.");
        }

        Debug.Log($"Animation usage scan completed. Report written to: {reportPath}");
    }
}
#endif
