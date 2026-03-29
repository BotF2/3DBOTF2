using BOTF3D.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ShipSOImporter : EditorWindow
{
    [MenuItem("Tools/Import ShipSO CSV")]
    public static void ShowWindow()
    {
        GetWindow<ShipSOImporter>("ShipSO CSV Importer");
    }

    private string filePath = "Assets/Editor/Data/ShipSO.csv";

    void OnGUI()
    {
        GUILayout.Label("ShipSO CSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("CSV File Path", filePath);

        if (GUILayout.Button("Import CSV"))
        {
            Debug.Log("dataPath : " + Application.dataPath);
            ImportCSV(filePath);
        }
    }

    private static void ImportCSV(string filePath)
    {
        Debug.Log("=== ImportCSV START ===");

        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        Debug.Log($"  Read {lines.Length} lines from CSV");

        int importedCount = 0;
        int skippedCount = 0;

        foreach (string line in lines)
        {
            if (line.StartsWith("ShipName") || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] fields = line.Split(',');
            Debug.Log($"  Processing line: {fields[0]}");
            string assetPath = "";

            if (fields.Length > 8) // Ensure there are enough fields
            {
                ShipSO shipSO = CreateInstance<ShipSO>();
                shipSO.ShipName = fields[0].Trim();

                string spritePath = $"Assets/Art/Ships/{shipSO.ShipName}.png";
                shipSO.shipSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                if (shipSO.shipSprite == null)
                {
                    spritePath = $"Assets/Art/Ships/{shipSO.ShipName}(CLONE).png";
                    shipSO.shipSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                }

                if (shipSO.shipSprite == null)
                {
                    // Fallback to default
                    spritePath = "Assets/Art/Ships/DEFAULT.png";
                    shipSO.shipSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    Debug.LogWarning($"  ⚠️ Sprite not found for '{shipSO.ShipName}' - using DEFAULT");
                }
                else
                {
                    Debug.Log($"  ✅ Loaded sprite: {spritePath}");
                }

                string[] data = shipSO.ShipName.Split("_");
                shipSO.CivEnum = GetMyCivEnum(data[0]);
                shipSO.TechLevel = GetMyTechLevel(data[2], out TechLevel st);
                shipSO.ShipType = GetMyShipClass(data[1]);

                // Remove (CLONE) from name if present
                // ✅ Add detailed FBX loading logs
                string fbxBaseName = shipSO.ShipName.Replace("(CLONE)", "").Replace("(Clone)", "").Trim();
                Debug.Log($"    Looking for FBX: {fbxBaseName}");

                string fbxPath = $"Assets/FBX/{fbxBaseName}.fbx";
                Debug.Log($"    Trying path: {fbxPath}");

                shipSO.ShipFBX_ModelAsGOPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

                if (shipSO.ShipFBX_ModelAsGOPrefab == null)
                {
                    Debug.LogWarning($" Use default FBX   ❌ NOT FOUND at: {fbxPath}");
                    fbxPath = $"Assets/FBX/FED_DESTROYER.fbx";
                    shipSO.ShipFBX_ModelAsGOPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                }

                if (shipSO.ShipFBX_ModelAsGOPrefab != null)
                {
                    Debug.Log($"    ✅ LOADED FBX: {AssetDatabase.GetAssetPath(shipSO.ShipFBX_ModelAsGOPrefab)}");
                }
                else
                {
                    Debug.LogError($"    ❌ FBX NOT FOUND for {fbxBaseName}");
                }

                shipSO.ShieldMaxHealth = int.Parse(fields[2]);
                shipSO.HullMaxHealth = int.Parse(fields[4]);
                shipSO.TorpedoDamage = int.Parse(fields[6]);
                shipSO.BeamDamage = int.Parse(fields[8]);

                int civFactor = 0;
                switch (shipSO.CivEnum)
                {
                    case CivEnum.FED:
                    case CivEnum.ROM:
                    case CivEnum.KLING:
                    case CivEnum.TERRAN:
                        break;
                    case CivEnum.CARD:
                        civFactor = -1;
                        break;
                    case CivEnum.DOM:
                        civFactor = +2;
                        break;
                    case CivEnum.BORG:
                        civFactor = +4;
                        break;
                    default:
                        break;
                }

                switch (shipSO.ShipType)
                {
                    case ShipType.Scout:
                        shipSO.BuildDuration = 6 + civFactor;
                        break;
                    case ShipType.Destroyer:
                        shipSO.BuildDuration = 8 + civFactor;
                        break;
                    case ShipType.Cruiser:
                        shipSO.BuildDuration = 10 + civFactor;
                        break;
                    case ShipType.LtCruiser:
                        shipSO.BuildDuration = 10 + civFactor;
                        break;
                    case ShipType.HvyCruiser:
                        shipSO.BuildDuration = 14 + civFactor;
                        break;
                    case ShipType.Transport:
                        shipSO.BuildDuration = 10 + civFactor;
                        break;
                    default:
                        break;
                }

                shipSO.maxWarpFactor = float.Parse(fields[11]);
                assetPath = $"Assets/SO/ShipSO/{shipSO.ShipName}.asset";

                // ✅ CRITICAL: Create directory if it doesn't exist
                string directoryPath = Path.GetDirectoryName(assetPath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    AssetDatabase.Refresh(); // Refresh so Unity sees the new folder
                    Debug.Log($"  ✅ Created directory: {directoryPath}");
                }

                // ✅ Check if asset already exists (prevent duplicates)
                if (AssetDatabase.LoadAssetAtPath<ShipSO>(assetPath) != null)
                {
                    Debug.LogWarning($"  ⚠️ Asset already exists, skipping: {assetPath}");
                    skippedCount++;
                    continue;
                }

                AssetDatabase.CreateAsset(shipSO, assetPath);
                AssetDatabase.SaveAssets();
                importedCount++;
                Debug.Log($"  ✅ Created: {assetPath}");
            }
        }

        Debug.Log($"ShipSO Import Complete - Imported: {importedCount}, Skipped: {skippedCount}");
        AssetDatabase.Refresh(); // Final refresh to show all new assets
    }

    public static CivEnum GetMyCivEnum(string title)
    {
        CivEnum st;
        Enum.TryParse(title, out st);
        return st;
    }
    public static TechLevel GetMyTechLevel(string title, out TechLevel st)
    {
        title = title.Replace("(CLONE)", "");

        switch (title)
        {
            case "I":
                st = TechLevel.EARLY;
                break;
            case "II":
                st = TechLevel.DEVELOPED;
                break;
            case "III":
                st = TechLevel.ADVANCED;
                break;
            case "IV":
                st = TechLevel.SUPREME;
                break;
            default:
                st = TechLevel.EARLY;
                break;
        }
        return st;
    }
    public static ShipType GetMyShipClass(string title)
    {
        switch (title)
        {
            case "TRANSPORT":
                return ShipType.Transport;
            case "SCOUT":
                return ShipType.Scout;
            case "DESTROYER":
                return ShipType.Destroyer;
            case "CRUISER":
                return ShipType.Cruiser;
            case "LTCRUISER":
                return ShipType.LtCruiser;
            case "HVYCRUISER":
                return ShipType.Transport;
            case "ONEMORE":
                return ShipType.OneMore;
            default:
                return ShipType.Scout;
        }
    }
    //#endif
}
