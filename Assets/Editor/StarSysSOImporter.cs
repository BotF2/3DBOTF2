using BOTF3D.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class StarSysSOImporter : EditorWindow
{
#if UNITY_EDITOR

    [MenuItem("Tools/Import StarSysSO CSV")]
    public static void ShowWindow()
    {
        GetWindow<StarSysSOImporter>("StarSysSO CSV Importer");
    }

    private string filePath = "Assets/Editor/Data/StarSystems.csv";

    void OnGUI()
    {
        GUILayout.Label("StarSysSO CSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("CSV File Path", filePath);

        if (GUILayout.Button("Import StarSysSO CSV"))
        {
            //Output the Game data path to the console
            Debug.Log("dataPath : " + Application.dataPath);
            ImportStarSysCSV(filePath);
        }
    }
    private static void ImportStarSysCSV(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        for (int i = 0; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');
            string imageString = fields[6];
            string imagesPower = fields[15];
            string imagesFactory = fields[16];
            string imagesShipyard = fields[17];
            string imagesShield = fields[18];
            string imagesOB = fields[19];
            string imagesRC = fields[20];

            string starsPath = Path.Combine(Application.dataPath, "Art/Stars");
            string facilitiesPath = Path.Combine(Application.dataPath, "Art/Facilities");
            // Build asset paths
            string starSpritePath = FindAssetPath("Assets/Art/Stars", imageString);
            // ✅ DIAGNOSTIC: Log sprite path resolution
            if (string.IsNullOrEmpty(starSpritePath))
            {
                Debug.LogError($"❌ Row {i}: Failed to find star sprite '{imageString}' for system '{fields[4]}'");
            }
            else
            {
                Debug.Log($"  ✅ Row {i}: Found star sprite at '{starSpritePath}' for system '{fields[4]}'");
            }
            string powerSpritePath = FindAssetPath("Assets/Art/Facilities", imagesPower);
            string factorySpritePath = FindAssetPath("Assets/Art/Facilities", imagesFactory);
            string shipyardSpritePath = FindAssetPath("Assets/Art/Facilities", imagesShipyard);
            string shieldSpritePath = FindAssetPath("Assets/Art/Facilities", imagesShield);
            string obSpritePath = FindAssetPath("Assets/Art/Facilities", imagesOB);
            string rcSpritePath = FindAssetPath("Assets/Art/Facilities", imagesRC);

            if (lines.Length >= 8) // Ensure there are enough fields
            {
                StarSysSO StarSysSO = CreateInstance<StarSysSO>();
                //StarSysInt	,	StarSysSO Enum	,	StarSysSO Short TextComponent	,	StarSysSO Long TextComponent	,	Home System	,	Triat One	,	Trait Two	,	StarSysSO Image	,	Insginia	,	Population	,	Credits	,	StartingTechLevel Points
                StarSysSO.StarSysInt = int.Parse(fields[0]);
                StarSysSO.Position = new Vector3((int.Parse(fields[1])) / 10, (int.Parse(fields[2])) / 10, (int.Parse(fields[3])) / 10);
                StarSysSO.SysName = fields[4];
                StarSysSO.FirstOwner = GetMyCivEnum(fields[5]);
                StarSysSO.CurrentOwner = GetMyCivEnum(fields[5]);
                StarSysSO.StarType = GetMyStarTypeEnum(fields[6]);
                StarSysSO.StarSprit = LoadSprite(starSpritePath);
                if (StarSysSO.StarSprit == null)
                {
                    Debug.LogError($"❌ Row {i}: FAILED to load sprite from '{starSpritePath}' for system '{StarSysSO.SysName}'");
                    Debug.LogError($"   This system will appear without a visible star in-game!");
                }
                else
                {
                    Debug.Log($"  ✅ Row {i}: Successfully loaded sprite '{StarSysSO.StarSprit.name}' for system '{StarSysSO.SysName}'");
                }
                StarSysSO.Dilitium = int.Parse(fields[7]);
                StarSysSO.PowerStations = int.Parse(fields[8]);
                StarSysSO.Factories = int.Parse(fields[9]);
                StarSysSO.Shipyards = int.Parse(fields[10]);
                StarSysSO.ResearchCenters = int.Parse(fields[11]);
                StarSysSO.ShieldGenerators = int.Parse(fields[12]);
                StarSysSO.OrbitalBatteries = int.Parse(fields[13]);
                StarSysSO.Description = fields[14];
                StarSysSO.powerPlantSprite = LoadSprite(powerSpritePath);
                StarSysSO.factorySprite = LoadSprite(factorySpritePath);
                StarSysSO.shipyardSprite = LoadSprite(shipyardSpritePath);
                StarSysSO.shieldSprite = LoadSprite(shieldSpritePath);
                StarSysSO.orbitalSprite = LoadSprite(obSpritePath);
                StarSysSO.researchCenterSprite = LoadSprite(rcSpritePath);
                StarSysSO.IsHomeworld = bool.Parse(fields[21]);
                StarSysSO.IsHabitable = bool.Parse(fields[22]);
                StarSysSO.IsTerraformable = bool.Parse(fields[23]);
                string assetPath = $"Assets/SO/StarSysSO/StarSysSO_{StarSysSO.StarSysInt}_{StarSysSO.SysName}.asset";
                AssetDatabase.CreateAsset(StarSysSO, assetPath);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("CivSOImporter Import Complete");
        }
    }
    static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogError($"❌ Sprite not found at: {path}");
        }
        return sprite;
    }
    static string FindAssetPath(string folder, string fileName)
    {
        string[] guids = AssetDatabase.FindAssets(fileName, new[] { folder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);

            if (string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase))
                return path;
        }

        Debug.LogError($"❌ Could not find asset: {fileName} in {folder}");
        return null;
    }
    public static CivEnum GetMyCivEnum(string title)
    {
        CivEnum st;
        Enum.TryParse(title, out st);
        return st;
    }
    public static GalaxyObjectType GetMyStarTypeEnum(string title)
    {
        GalaxyObjectType st;
        Enum.TryParse(title, out st);
        return st;
    }

#endif
}
