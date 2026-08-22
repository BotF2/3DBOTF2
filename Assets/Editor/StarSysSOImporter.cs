using System;
using System.IO;
using UnityEditor;
using UnityEngine;

using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
public class StarSysSOImporter : EditorWindow
{
#if UNITY_EDITOR

    [MenuItem("Tools/Import StarSysSO CSV")]
    public static void ShowWindow()
    {
        GetWindow<StarSysSOImporter>("StarSysSO CSV Importer");
    }

    private string filePath = "Assets/Editor/Data/StarSystems.csv";
    private bool skipHeader = false;

    void OnGUI()
    {
        GUILayout.Label("StarSysSO CSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("CSV File Path", filePath);
        skipHeader = EditorGUILayout.Toggle("Skip First Row (Header)", skipHeader);

        if (GUILayout.Button("Import StarSysSO CSV"))
        {
            try
            {
                Debug.Log("=== Starting StarSysSO Import ===");
                Debug.Log("dataPath : " + Application.dataPath);
                ImportStarSysCSV(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Import failed: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Import Error", $"Failed to import CSV:\n{ex.Message}", "OK");
            }
        }

        if (GUILayout.Button("Test Parse First Row"))
        {
            TestParseFirstRow(filePath);
        }
    }

    private void TestParseFirstRow(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        if (lines.Length == 0)
        {
            Debug.LogError("CSV is empty");
            return;
        }

        Debug.Log("=== Testing First Row Parse ===");
        string[] fields = lines[0].Split(',');
        Debug.Log($"Field count: {fields.Length}");

        for (int i = 0; i < fields.Length; i++)
        {
            Debug.Log($"Field[{i}] = '{fields[i]}'");
        }

        // Test enum parsing
        Debug.Log($"\nTesting CivEnum parse of '{fields[5]}':");
        CivEnum civResult = GetMyCivEnum(fields[5]);
        Debug.Log($"  Result: {civResult}");

        Debug.Log($"\nTesting GalaxyObjectType parse of '{fields[6]}':");
        GalaxyObjectType starResult = GetMyStarTypeEnum(fields[6]);
        Debug.Log($"  Result: {starResult}");

        // Test sprite paths
        Debug.Log($"\nTesting sprite path for '{fields[6]}':");
        string starPath = FindAssetPath("Assets/Art/Stars", fields[6]);
        Debug.Log($"  Found: {starPath ?? "NULL"}");
    }

    private void ImportStarSysCSV(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            EditorUtility.DisplayDialog("File Not Found", $"Could not find file:\n{filePath}", "OK");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length == 0)
        {
            Debug.LogError("CSV file is empty");
            return;
        }

        int startIndex = skipHeader ? 1 : 0;
        int successCount = 0;
        int errorCount = 0;

        // Ensure output directory exists
        string outputDir = "Assets/SO/StarSysSO";
        if (!AssetDatabase.IsValidFolder(outputDir))
        {
            string parentDir = "Assets/SO";
            if (!AssetDatabase.IsValidFolder(parentDir))
            {
                AssetDatabase.CreateFolder("Assets", "SO");
            }
            AssetDatabase.CreateFolder(parentDir, "StarSysSO");
            AssetDatabase.Refresh();
        }

        for (int i = startIndex; i < lines.Length; i++)
        {
            try
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    Debug.LogWarning($"⚠️ Row {i}: Skipping empty line");
                    continue;
                }

                string[] fields = lines[i].Split(',');

                // Validate field count
                if (fields.Length < 24)
                {
                    Debug.LogError($"❌ Row {i}: Insufficient fields ({fields.Length}/24). Line: {lines[i]}");
                    errorCount++;
                    continue;
                }

                Debug.Log($"Processing Row {i}: {fields[4]}");

                // CSV Structure:
                // 0:StarSysInt, 1:X, 2:Y, 3:Z, 4:SysName, 5:Owner, 6:StarImage, 
                // 7:Dilithium, 8:PowerStations, 9:Factories, 10:Shipyards, 11:ResearchCenters,
                // 12:ShieldGenerators, 13:OrbitalBatteries, 14:Description,
                // 15:PowerSprite, 16:FactorySprite, 17:ShipyardSprite, 18:ShieldSprite, 
                // 19:OrbitalSprite, 20:ResearchSprite, 21:IsHomeworld, 22:IsHabitable, 23:IsTerraformable

                StarSysSO newStar = CreateInstance<StarSysSO>();

                // Parse integer fields
                if (!int.TryParse(fields[0], out int starSysInt))
                {
                    Debug.LogError($"❌ Row {i}: Failed to parse StarSysInt '{fields[0]}'");
                    errorCount++;
                    continue;
                }
                newStar.StarSysInt = starSysInt;

                // ✅ Parse position - NO DIVISION, coordinates are already in correct scale
                if (!float.TryParse(fields[1], out float x) ||
                    !float.TryParse(fields[2], out float y) ||
                    !float.TryParse(fields[3], out float z))
                {
                    Debug.LogError($"❌ Row {i}: Failed to parse position");
                    errorCount++;
                    continue;
                }

                // ✅ Store as LOCAL position (relative to GalaxyCenter)
                newStar.Position = new Vector3(x, y, z);

                newStar.SysName = fields[4];
                newStar.FirstOwner = GetMyCivEnum(fields[5]);
                newStar.CurrentOwner = newStar.FirstOwner;

                // Field 6 is the STAR IMAGE NAME, not the enum
                // We need to determine the star type from the image name
                // Field 6 is the STAR IMAGE NAME, not the enum
                // We need to determine the star type from the image name
                string starSpriteName = fields[6];

                // Determine StarType and load appropriate sprite
                switch (fields[6])
                {

                    case "Yellow":
                        newStar.StarType = GalaxyObjectType.YellowStar;
                        starSpriteName = "Yellow";
                        newStar.StarSprit = LoadSprite(FindAssetPath("Assets/Art/Stars", starSpriteName));
                        break;

                    case "Red":
                        newStar.StarType = GalaxyObjectType.RedStar;
                        starSpriteName = "Red";
                        newStar.StarSprit = LoadSprite(FindAssetPath("Assets/Art/Stars", starSpriteName));
                        break;

                    case "Blue":
                        newStar.StarType = GalaxyObjectType.BlueStar;
                        starSpriteName = "Blue";
                        newStar.StarSprit = LoadSprite(FindAssetPath("Assets/Art/Stars", starSpriteName));
                        break;

                    case "Orange":
                        newStar.StarType = GalaxyObjectType.OrangeStar;
                        starSpriteName = "Orange";
                        newStar.StarSprit = LoadSprite(FindAssetPath("Assets/Art/Stars", starSpriteName));
                        break;

                    case "White":
                        newStar.StarType = GalaxyObjectType.WhiteStar;
                        starSpriteName = "White";
                        newStar.StarSprit = LoadSprite(FindAssetPath("Assets/Art/Stars", starSpriteName));
                        break;
                    case "Nebula":
                        newStar.StarType = GalaxyObjectType.Nebula;
                        // Get random nebula sprite (Nebula0 through Nebula26)
                        var rnd = new System.Random();
                        int nebulaIndex = rnd.Next(0, 27); // 0 to 26 inclusive
                        starSpriteName = "Nebula" + nebulaIndex.ToString();
                        // Load the random nebula sprite
                        string nebulaPath = FindAssetPath("Assets/Art/Stars", starSpriteName);
                        newStar.StarSprit = LoadSprite(nebulaPath);
                        Debug.Log($"  🌌 Nebula: Using {starSpriteName} from {nebulaPath}");
                        break;
                    case "UniComplex":
                        newStar.StarType = GalaxyObjectType.UniComplex;
                        starSpriteName = "UniComplex";
                        newStar.StarSprit = LoadSprite(FindAssetPath("Assets/Art/Stars", starSpriteName));
                        break;
                    case "OmarianNebula":
                        newStar.StarType = GalaxyObjectType.OmarianNebula;
                        starSpriteName = "OmarianNebula";
                        newStar.StarSprit = LoadSprite(FindAssetPath("Assets/Art/Stars", starSpriteName));
                        break;
                    default:
                        Debug.LogWarning($"⚠️ Row {i}: Unrecognized star image '{fields[6]}', defaulting to None");
                        newStar.StarType = GalaxyObjectType.None;
                        break;
                }

                if (newStar.StarSprit == null)
                {
                    Debug.LogWarning($"⚠️ Row {i}: Could not load star sprite '{starSpriteName}' for {newStar.SysName}");
                }

                // Parse facility counts
                newStar.Dilithium = ParseIntSafe(fields[7], i, "Dilithium");
                newStar.PowerStations = ParseIntSafe(fields[8], i, "PowerStations");
                newStar.Factories = ParseIntSafe(fields[9], i, "Factories");
                newStar.Shipyards = ParseIntSafe(fields[10], i, "Shipyards");
                newStar.ResearchCenters = ParseIntSafe(fields[11], i, "ResearchCenters");
                newStar.ShieldGenerators = ParseIntSafe(fields[12], i, "ShieldGenerators");
                newStar.OrbitalBatteries = ParseIntSafe(fields[13], i, "OrbitalBatteries");

                newStar.Description = fields[14];

                // Load facility sprites
                newStar.powerPlantSprite = LoadSprite(FindAssetPath("Assets/Art/Facilities", fields[15]));
                newStar.factorySprite = LoadSprite(FindAssetPath("Assets/Art/Facilities", fields[16]));
                newStar.shipyardSprite = LoadSprite(FindAssetPath("Assets/Art/Facilities", fields[17]));
                newStar.shieldSprite = LoadSprite(FindAssetPath("Assets/Art/Facilities", fields[18]));
                newStar.orbitalSprite = LoadSprite(FindAssetPath("Assets/Art/Facilities", fields[19]));
                newStar.researchCenterSprite = LoadSprite(FindAssetPath("Assets/Art/Facilities", fields[20]));

                // Parse booleans
                newStar.IsHomeworld = ParseBoolSafe(fields[21]);
                newStar.IsHabitable = ParseBoolSafe(fields[22]);
                newStar.IsTerraformable = ParseBoolSafe(fields[23]);

                // Create asset
                string assetPath = $"{outputDir}/StarSysSO_{newStar.StarSysInt:000}_{SanitizeFileName(newStar.SysName)}.asset";

                AssetDatabase.CreateAsset(newStar, assetPath);
                successCount++;
                Debug.Log($"  ✅ Created: {assetPath} at position ({x:F2}, {y:F2}, {z:F2})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Row {i}: Exception - {ex.Message}\n{ex.StackTrace}");
                errorCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ StarSysSO Import Complete: {successCount} succeeded, {errorCount} failed");
        EditorUtility.DisplayDialog("Import Complete",
            $"Imported {successCount} star systems\n{errorCount} errors encountered\n\nCheck console for details.",
            "OK");
    }

    static int ParseIntSafe(string value, int row, string fieldName)
    {
        if (int.TryParse(value, out int result))
            return result;

        Debug.LogWarning($"⚠️ Row {row}: Failed to parse {fieldName} '{value}', using 0");
        return 0;
    }

    static bool ParseBoolSafe(string value)
    {
        if (bool.TryParse(value, out bool result))
            return result;

        // Try common variations
        string upper = value.ToUpper();
        if (upper == "TRUE" || upper == "1" || upper == "YES")
            return true;

        return false;
    }

    static string SanitizeFileName(string name)
    {
        // Remove invalid filename characters
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Replace(" ", "_");
    }

    static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null && !string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"⚠️ Sprite not found at: {path}");
        }
        return sprite;
    }

    static string FindAssetPath(string folder, string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        // First try exact match with common extensions
        string[] extensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };
        foreach (string ext in extensions)
        {
            string directPath = $"{folder}/{fileName}{ext}";
            if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), directPath)))
            {
                return directPath;
            }
        }

        // Fallback to AssetDatabase search
        string[] guids = AssetDatabase.FindAssets(fileName, new[] { folder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);

            if (string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase))
                return path;
        }

        // Try searching without folder restriction
        guids = AssetDatabase.FindAssets(fileName);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
        }

        return null;
    }

    public static CivEnum GetMyCivEnum(string title)
    {
        if (Enum.TryParse(title, true, out CivEnum result))
            return result;

        Debug.LogWarning($"⚠️ Could not parse CivEnum '{title}', using default");
        return default(CivEnum);
    }

    public static GalaxyObjectType GetMyStarTypeEnum(string title)
    {
        if (Enum.TryParse(title, true, out GalaxyObjectType result1))
            return result1;
        else if (Enum.TryParse(title + "Star", true, out GalaxyObjectType result2))
            return result2;
        else if (Enum.TryParse(title, true, out GalaxyObjectType result3))
            return result3;
        //else if (Enum.TryParse(title.Replace(" ", "") + "Star", true, out GalaxyObjectType result4))
        //    return result4;
        Debug.LogWarning($"⚠️ Could not parse GalaxyObjectType '{title}', using default");
        return default(GalaxyObjectType);
    }

#endif
}
