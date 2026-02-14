using BOTF3D.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ShieldGeneratorSOImporter : EditorWindow
{
#if UNITY_EDITOR

    [MenuItem("Tools/Import ShieldGeneratorSO TSV")]
    public static void ShowWindow()
    {
        GetWindow<ShieldGeneratorSOImporter>("ShieldGeneratorSO TSV Importer");
    }

    private string filePath = $"BOTF3D/Resources/Data/StarSysShieldGenerator.tsv";

    void OnGUI()
    {
        GUILayout.Label("ShieldGeneratorSO TSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("TSV File Path", filePath);

        if (GUILayout.Button("Import ShieldGeneratorSO TSV"))
        {
            //Output the Game data path to the console
            Debug.Log("dataPath : " + Application.dataPath);
            ImportShieldTSV(filePath);
        }
    }
    private static void ImportShieldTSV(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            string[] fields = line.Split("\t");

            if (fields.Length > 7) // Ensure there are enough fields
            {
                string imageString = fields[4];
                foreach (string file in Directory.GetFiles($"BOTF3D/Resources/Facilities/", "*.png"))
                {
                    if (file == "BOTF3D/Resources/Facilities/" + imageString + ".png")
                    {
                        imageString = "Facilities/" + imageString;
                    }
                }


                if (fields.Length >= 7) // Ensure there are enough fields
                {
                    ShieldGeneratorSO ShieldGeneratorSO = CreateInstance<ShieldGeneratorSO>();
                    ////StarSysInt	,	ShieldGeneratorSO Enum	,	ShieldGeneratorSO Short TextComponent	,	ShieldGeneratorSO Long TextComponent	,	Home System	,	Triat One	,	Trait Two	,	ShieldGeneratorSO Image	,	Insginia	,	Population	,	Credits	,	StartingTechLevel Points
                    ShieldGeneratorSO.CivInt = int.Parse(fields[0]);
                    ShieldGeneratorSO.TechLevel = (TechLevel)int.Parse(fields[1]);
                    ShieldGeneratorSO.FacilitiesEnumType = (StarSysFacilityType)int.Parse(fields[2]);
                    ShieldGeneratorSO.Name = (fields[3]);
                    ShieldGeneratorSO.StartStarDate = int.Parse(fields[5]);
                    ShieldGeneratorSO.BuildDuration = int.Parse(fields[6]);
                    ShieldGeneratorSO.PowerLoad = int.Parse(fields[7]);
                    ShieldGeneratorSO.ShieldGeneratorSprite = Resources.Load<Sprite>(imageString);
                    ShieldGeneratorSO.Description = (fields[8]);
                    string assetPath = $"BOTF3D/SO/StarSysShieldGeneratorSO/ShieldGeneratorSO_{ShieldGeneratorSO.CivInt}_{ShieldGeneratorSO.Name}.asset";
                    AssetDatabase.CreateAsset(ShieldGeneratorSO, assetPath);
                    AssetDatabase.SaveAssets();
                }
            }
            Debug.Log("ShieldGeneratorSOImporter Import Complete");
        }
    }

#endif
}
