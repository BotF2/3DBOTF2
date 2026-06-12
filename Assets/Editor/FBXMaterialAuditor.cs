using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scans all FBX files under Assets/FBX/ and reports:
///   - All materials assigned per mesh renderer
///   - Duplicate materials (same shader + identical float/color properties)
///   - Null / empty material slots
///   - Consolidation recommendations
///
/// Run: Tools > FBX Material Auditor
/// </summary>
public class FBXMaterialAuditor : EditorWindow
{
    [MenuItem("Tools/FBX Material Auditor")]
    public static void ShowWindow() => GetWindow<FBXMaterialAuditor>("FBX Material Auditor");

    private Vector2 _scroll;
    private string _report = "Click 'Run Audit' to scan Assets/FBX/";
    private string _fbxFolder = "Assets/FBX";

    private void OnGUI()
    {
        EditorGUILayout.LabelField("FBX Material Auditor", EditorStyles.boldLabel);
        _fbxFolder = EditorGUILayout.TextField("FBX Folder", _fbxFolder);

        if (GUILayout.Button("Run Audit", GUILayout.Height(32)))
        {
            _report = RunAudit(_fbxFolder);
        }

        if (GUILayout.Button("Save Report to Desktop"))
        {
            string path = Path.Combine(System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Desktop), "FBX_Material_Audit.txt");
            File.WriteAllText(path, _report);
            Debug.Log($"Report saved to: {path}");
        }

        EditorGUILayout.Space();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private static string RunAudit(string fbxFolder)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== FBX MATERIAL AUDIT ===");
        sb.AppendLine($"Folder: {fbxFolder}");
        sb.AppendLine();

        // ── 1. Gather all FBX GUIDs ───────────────────────────────────────────
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { fbxFolder });
        sb.AppendLine($"Found {guids.Length} FBX model(s)\n");

        // ── 2. Per-model report ───────────────────────────────────────────────
        // Key = material GUID, Value = list of (fbx name, slot index, slot name)
        var matUsage = new Dictionary<string, List<string>>();
        var nullSlots = new List<(string fbx, int slot, string submesh)>();

        foreach (string guid in guids)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            string fbxName = Path.GetFileNameWithoutExtension(fbxPath);
            sb.AppendLine($"── {fbxName} ({fbxPath})");

            // Load all assets embedded in the FBX (meshes, materials, etc.)
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

            // Collect materials embedded in this FBX
            var embeddedMats = assets.OfType<Material>().ToList();
            if (embeddedMats.Count == 0)
            {
                sb.AppendLine("   ⚠️  No embedded materials found in FBX.");
            }
            else
            {
                foreach (var mat in embeddedMats)
                {
                    string matGuid = AssetDatabase.AssetPathToGUID(
                        AssetDatabase.GetAssetPath(mat));
                    if (string.IsNullOrEmpty(matGuid)) matGuid = mat.name; // fallback for embedded

                    if (!matUsage.ContainsKey(matGuid))
                        matUsage[matGuid] = new List<string>();
                    matUsage[matGuid].Add(fbxName);

                    sb.AppendLine($"   Material: '{mat.name}'  shader={mat.shader?.name ?? "NULL"}");
                    ReportMaterialProperties(mat, sb);
                }
            }

            // Load the root prefab and check all MeshRenderers for null slots
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab != null)
            {
                var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null)
                        {
                            nullSlots.Add((fbxName, i, r.name));
                            sb.AppendLine($"   ❌ NULL material slot [{i}] on mesh '{r.name}'");
                        }
                        else
                        {
                            sb.AppendLine($"   Slot [{i}] on '{r.name}' → '{mats[i].name}'");
                        }
                    }
                }
            }

            sb.AppendLine();
        }

        // ── 3. Cross-FBX duplicate material names ─────────────────────────────
        sb.AppendLine("=== DUPLICATE MATERIAL NAMES ACROSS FBX FILES ===");
        var allMaterialPaths = AssetDatabase.FindAssets("t:Material", new[] { fbxFolder });
        var matNameMap = new Dictionary<string, List<string>>(); // name → paths

        foreach (string g in allMaterialPaths)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            string n = Path.GetFileNameWithoutExtension(p);
            // Strip Unity's auto-suffix e.g. "Hull 1" → "Hull", "Hull (1)" → "Hull"
            string baseName = n.TrimEnd('1', '2', '3', '4', '5', ' ').Trim().TrimEnd('(', ')');
            if (!matNameMap.ContainsKey(baseName)) matNameMap[baseName] = new List<string>();
            matNameMap[baseName].Add(p);
        }

        bool foundDupes = false;
        foreach (var kvp in matNameMap.Where(k => k.Value.Count > 1))
        {
            foundDupes = true;
            sb.AppendLine($"  '{kvp.Key}' has {kvp.Value.Count} variants:");
            foreach (var p in kvp.Value) sb.AppendLine($"    {p}");
        }
        if (!foundDupes) sb.AppendLine("  No duplicate material names found.");
        sb.AppendLine();

        // ── 4. Null slot summary ──────────────────────────────────────────────
        sb.AppendLine("=== NULL MATERIAL SLOT SUMMARY ===");
        if (nullSlots.Count == 0)
        {
            sb.AppendLine("  ✅ No null material slots found.");
        }
        else
        {
            foreach (var (fbx, slot, mesh) in nullSlots)
                sb.AppendLine($"  {fbx} → mesh '{mesh}' → slot [{slot}] is NULL");
        }
        sb.AppendLine();

        // ── 5. Consolidation recommendations ─────────────────────────────────
        sb.AppendLine("=== CONSOLIDATION RECOMMENDATIONS ===");
        AppendConsolidationHints(sb, fbxFolder);

        return sb.ToString();
    }

    private static void ReportMaterialProperties(Material mat, StringBuilder sb)
    {
        if (mat == null) return;
        // Report key visual properties that indicate whether two mats are identical
        if (mat.HasProperty("_Color"))
            sb.AppendLine($"      _Color={mat.GetColor("_Color")}");
        if (mat.HasProperty("_BaseColor"))
            sb.AppendLine($"      _BaseColor={mat.GetColor("_BaseColor")}");
        if (mat.HasProperty("_MainTex"))
        {
            var tex = mat.GetTexture("_MainTex");
            sb.AppendLine($"      _MainTex={(tex != null ? tex.name : "none")}");
        }
        if (mat.HasProperty("_BaseMap"))
        {
            var tex = mat.GetTexture("_BaseMap");
            sb.AppendLine($"      _BaseMap={(tex != null ? tex.name : "none")}");
        }
        if (mat.HasProperty("_Metallic"))
            sb.AppendLine($"      _Metallic={mat.GetFloat("_Metallic"):F2}");
        if (mat.HasProperty("_Smoothness"))
            sb.AppendLine($"      _Smoothness={mat.GetFloat("_Smoothness"):F2}");
        if (mat.HasProperty("_EmissionColor"))
        {
            Color e = mat.GetColor("_EmissionColor");
            if (e != Color.black)
                sb.AppendLine($"      _EmissionColor={e}  ← has emission");
        }
    }

    private static void AppendConsolidationHints(StringBuilder sb, string fbxFolder)
    {
        // Find all external .mat files under the FBX folder and its Materials subfolder
        string matFolder = fbxFolder + "/Materials";
        string[] searchFolders = AssetDatabase.IsValidFolder(matFolder)
            ? new[] { fbxFolder, matFolder }
            : new[] { fbxFolder };

        string[] matGuids = AssetDatabase.FindAssets("t:Material", searchFolders);

        // Group by shader name — mats sharing a shader AND same textures are candidates
        var byShader = new Dictionary<string, List<(string path, Material mat)>>();
        foreach (string g in matGuids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) continue;
            string key = m.shader?.name ?? "Unknown";
            if (!byShader.ContainsKey(key)) byShader[key] = new List<(string, Material)>();
            byShader[key].Add((p, m));
        }

        foreach (var kvp in byShader)
        {
            if (kvp.Value.Count < 2) continue;

            // Find pairs where _MainTex/_BaseMap AND _Color/_BaseColor are identical
            for (int i = 0; i < kvp.Value.Count; i++)
            {
                for (int j = i + 1; j < kvp.Value.Count; j++)
                {
                    var (pa, ma) = kvp.Value[i];
                    var (pb, mb) = kvp.Value[j];
                    if (AreMaterialsEffectivelyIdentical(ma, mb))
                    {
                        sb.AppendLine($"  ✂️  CONSOLIDATE: '{Path.GetFileName(pa)}' and '{Path.GetFileName(pb)}'");
                        sb.AppendLine($"       Both use shader '{kvp.Key}' with identical properties.");
                        sb.AppendLine($"       Keep one, use 'Edit > Find References In Scene' to repoint the other.");
                    }
                }
            }
        }
    }

    private static bool AreMaterialsEffectivelyIdentical(Material a, Material b)
    {
        if (a.shader != b.shader) return false;

        // Compare colour
        Color ca = a.HasProperty("_Color") ? a.GetColor("_Color") :
                   a.HasProperty("_BaseColor") ? a.GetColor("_BaseColor") : Color.white;
        Color cb = b.HasProperty("_Color") ? b.GetColor("_Color") :
                   b.HasProperty("_BaseColor") ? b.GetColor("_BaseColor") : Color.white;
        if (ca != cb) return false;

        // Compare main texture reference
        Texture ta = a.HasProperty("_MainTex") ? a.GetTexture("_MainTex") :
                     a.HasProperty("_BaseMap") ? a.GetTexture("_BaseMap") : null;
        Texture tb = b.HasProperty("_MainTex") ? b.GetTexture("_MainTex") :
                     b.HasProperty("_BaseMap") ? b.GetTexture("_BaseMap") : null;
        if (ta != tb) return false;

        // Compare metallic / smoothness
        float metA = a.HasProperty("_Metallic") ? a.GetFloat("_Metallic") : 0f;
        float metB = b.HasProperty("_Metallic") ? b.GetFloat("_Metallic") : 0f;
        if (Mathf.Abs(metA - metB) > 0.01f) return false;

        float smA = a.HasProperty("_Smoothness") ? a.GetFloat("_Smoothness") : 0f;
        float smB = b.HasProperty("_Smoothness") ? b.GetFloat("_Smoothness") : 0f;
        if (Mathf.Abs(smA - smB) > 0.01f) return false;

        return true;
    }
}
