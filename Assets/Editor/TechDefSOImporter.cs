using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

using BOTF3D.Core;

/// <summary>
/// TechTree_Phase2_Design.md §8 phase II.1: authors all 84 TechDefSO assets (35 shared +
/// 49 faction-unique) from the two CSVs that are the source of truth (§7) - re-run this any
/// time TechTree_CommonBranches.csv or TechTree_FactionUnique.csv changes; it updates existing
/// assets in place (matched by Id) rather than creating duplicates, so it's safe to re-run.
/// Follows the same importer convention as StarSysSOImporter/ShipSOImporter etc.
/// </summary>
public class TechDefSOImporter : EditorWindow
{
#if UNITY_EDITOR

    [MenuItem("Tools/Import TechDefSO CSVs")]
    public static void ShowWindow()
    {
        GetWindow<TechDefSOImporter>("TechDefSO CSV Importer");
    }

    private string sharedCsvPath = "Assets/Docs/TechTree_CommonBranches.csv";
    private string uniqueCsvPath = "Assets/Docs/TechTree_FactionUnique.csv";
    private const string OutputDir = "Assets/SO/TechDefSO";

    void OnGUI()
    {
        GUILayout.Label("TechDefSO CSV Importer", EditorStyles.boldLabel);
        GUILayout.Label("Authors/updates all 84 TechDefSO assets from the two design CSVs.", EditorStyles.wordWrappedLabel);
        sharedCsvPath = EditorGUILayout.TextField("Shared branches CSV", sharedCsvPath);
        uniqueCsvPath = EditorGUILayout.TextField("Faction-unique CSV", uniqueCsvPath);

        if (GUILayout.Button("Import Both CSVs"))
        {
            try
            {
                Debug.Log("=== Starting TechDefSO Import ===");
                EnsureOutputFolder();
                int sharedOk = ImportSharedBranches(sharedCsvPath);
                int uniqueOk = ImportFactionUnique(uniqueCsvPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"✅ TechDefSO Import Complete: {sharedOk} shared + {uniqueOk} unique = {sharedOk + uniqueOk} techs.");
                EditorUtility.DisplayDialog("Import Complete",
                    $"Shared branches: {sharedOk}\nFaction-unique: {uniqueOk}\nTotal: {sharedOk + uniqueOk} (expected 84)\n\nCheck console for details.",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Import failed: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Import Error", $"Failed to import CSVs:\n{ex.Message}", "OK");
            }
        }
    }

    private static void EnsureOutputFolder()
    {
        if (AssetDatabase.IsValidFolder(OutputDir)) return;
        if (!AssetDatabase.IsValidFolder("Assets/SO"))
            AssetDatabase.CreateFolder("Assets", "SO");
        AssetDatabase.CreateFolder("Assets/SO", "TechDefSO");
        AssetDatabase.Refresh();
    }

    // ── Shared branches (TechTree_CommonBranches.csv: Branch,Tier,TechPoints,TimeLine,TechName,EffectSummary,EffectHook) ──

    private int ImportSharedBranches(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"❌ Shared branches CSV not found: {path}");
            return 0;
        }

        string[] lines = File.ReadAllLines(path);
        int successCount = 0;

        for (int i = 1; i < lines.Length; i++) // row 0 is the header
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            List<string> f = ParseCsvLine(lines[i]);
            if (f.Count < 7)
            {
                Debug.LogError($"❌ [Shared] Row {i}: expected 7 fields, got {f.Count}. Line: {lines[i]}");
                continue;
            }

            try
            {
                TechFieldEnum field = ParseBranch(f[0]);
                int tier = int.Parse(f[1]);
                int techPoints = int.Parse(f[2]);
                int timeLine = int.Parse(f[3]);
                string techName = f[4].Trim();
                string effectSummary = f[5].Trim();
                string hookToken = f[6].Trim();

                if (!Enum.TryParse(hookToken, out TechEffectHook hook))
                {
                    Debug.LogError($"❌ [Shared] Row {i} '{techName}': EffectHook '{hookToken}' has no matching TechEffectHook member - add it there first.");
                    continue;
                }

                string id = $"{field}_T{tier}_{SlugToPascal(techName)}";

                var def = GetOrCreate(id);
                def.Id = id;
                def.DisplayName = techName;
                def.Description = effectSummary;
                def.Field = field;
                def.Tier = tier;
                def.TechPointsThreshold = techPoints;
                def.ResearchCost = timeLine;
                def.UnlockMode = TechUnlockMode.Researched;
                def.IsShared = true;
                def.RestrictedToCiv = default;
                def.EffectHook = hook;

                SaveDef(def, id);
                successCount++;
                Debug.Log($"  ✅ [Shared] {id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [Shared] Row {i}: {ex.Message}");
            }
        }

        return successCount;
    }

    private static TechFieldEnum ParseBranch(string branch)
    {
        switch (branch.Trim())
        {
            case "Propulsion": return TechFieldEnum.Propulsion;
            case "Tactical": return TechFieldEnum.Tactical;
            case "Ordnance": return TechFieldEnum.Ordnance;
            case "Sensors & Science": return TechFieldEnum.Science;
            case "Intelligence": return TechFieldEnum.Intelligence;
            default: throw new Exception($"Unrecognized Branch '{branch}'");
        }
    }

    // ── Faction-unique (TechTree_FactionUnique.csv: Civ,Slot,TechPoints,TimeLine,UnlockMode,TechName,EffectSummary,BackendTieIn,CanonNotes) ──

    private int ImportFactionUnique(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"❌ Faction-unique CSV not found: {path}");
            return 0;
        }

        string[] lines = File.ReadAllLines(path);
        int successCount = 0;

        for (int i = 1; i < lines.Length; i++) // row 0 is the header
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            List<string> f = ParseCsvLine(lines[i]);
            if (f.Count < 8)
            {
                Debug.LogError($"❌ [Unique] Row {i}: expected at least 8 fields, got {f.Count}. Line: {lines[i]}");
                continue;
            }

            try
            {
                CivEnum civ = ParseCiv(f[0]);
                string slot = f[1].Trim(); // "Innate" or "T1".."T7"
                int tier = slot == "Innate" ? 0 : int.Parse(slot.Substring(1));
                int techPoints = int.Parse(f[2]);
                int timeLine = int.Parse(f[3]);
                TechUnlockMode unlockMode = (TechUnlockMode)Enum.Parse(typeof(TechUnlockMode), f[4].Trim());
                string techName = f[5].Trim();
                string effectSummary = f[6].Trim();

                string hookName = SlugToPascal(techName);
                if (!Enum.TryParse(hookName, out TechEffectHook hook))
                {
                    Debug.LogError($"❌ [Unique] Row {i} '{techName}' (derived hook '{hookName}'): no matching TechEffectHook member - add it there first.");
                    continue;
                }

                string id = $"{civ}_{slot}_{SlugToPascal(techName)}";

                var def = GetOrCreate(id);
                def.Id = id;
                def.DisplayName = techName;
                def.Description = effectSummary;
                def.Field = TechFieldEnum.FactionUnique;
                def.Tier = tier;
                def.TechPointsThreshold = techPoints;
                def.ResearchCost = timeLine;
                def.UnlockMode = unlockMode;
                def.IsShared = false;
                def.RestrictedToCiv = civ;
                def.EffectHook = hook;

                SaveDef(def, id);
                successCount++;
                Debug.Log($"  ✅ [Unique] {id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [Unique] Row {i}: {ex.Message}");
            }
        }

        return successCount;
    }

    private static CivEnum ParseCiv(string civName)
    {
        switch (civName.Trim())
        {
            case "Federation": return CivEnum.FED;
            case "Klingon": return CivEnum.KLING;
            case "Romulan": return CivEnum.ROM;
            case "Borg": return CivEnum.BORG;
            case "Cardassian": return CivEnum.CARD;
            case "Terran Empire": return CivEnum.TERRAN;
            case "Dominion": return CivEnum.DOM;
            default: throw new Exception($"Unrecognized Civ '{civName}'");
        }
    }

    // ── Shared helpers ──────────────────────────────────────────────────────────────────────

    private static TechDefSO GetOrCreate(string id)
    {
        string assetPath = $"{OutputDir}/{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TechDefSO>(assetPath);
        return existing != null ? existing : ScriptableObject.CreateInstance<TechDefSO>();
    }

    private static void SaveDef(TechDefSO def, string id)
    {
        string assetPath = $"{OutputDir}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<TechDefSO>(assetPath) == null)
            AssetDatabase.CreateAsset(def, assetPath);
        else
            EditorUtility.SetDirty(def);
    }

    /// <summary>Minimal RFC4180-style single-line CSV parser: handles quoted fields, embedded
    /// commas inside quotes, and doubled "" as an escaped quote. Both design CSVs use exactly
    /// this shape (no field spans multiple lines).</summary>
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields;
    }

    /// <summary>Converts a tech name like "Fear-Driven Command Protocols II" or "Near-Perfect
    /// Cloak (capstone)" into the matching TechEffectHook member name ("FearDrivenCommandProtocolsII",
    /// "NearPerfectCloak") - drops any trailing "(...)" annotation, strips apostrophes without
    /// splitting the word, then PascalCases on space/hyphen boundaries.</summary>
    private static string SlugToPascal(string name)
    {
        name = Regex.Replace(name, @"\(.*?\)", "");
        name = name.Replace("'", "").Replace("’", "");

        var sb = new StringBuilder();
        foreach (var word in name.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string clean = Regex.Replace(word, @"[^A-Za-z0-9]", "");
            if (clean.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(clean[0]));
            if (clean.Length > 1) sb.Append(clean.Substring(1));
        }
        return sb.ToString();
    }

#endif
}
