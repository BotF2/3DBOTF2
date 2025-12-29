using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Assets.Core;

public class FactorySOImporter : EditorWindow
{

#if UNITY_EDITOR

    [MenuItem("Tools/Import FactorySO TSV")]
    public static void ShowWindow()
    {
        GetWindow<FactorySOImporter>("FactorySO TSV Importer");
    }

    private string filePath = $"Assets/Resources/Data/StarSysFactory.TSV";

    void OnGUI()
    {
        GUILayout.Label("FactorySO TSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("TSV File Path", filePath);

        if (GUILayout.Button("Import FactorySO TSV"))
        {
            //Output the Game data path to the console
            Debug.Log("dataPath : " + Application.dataPath);
            ImportStarSysTSV(filePath);
        }
    }
    private static void ImportStarSysTSV(string filePath)
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
                foreach (string file in Directory.GetFiles($"Assets/Resources/Facilities/", "*.png"))
                {
                    if (file == "Assets/Resources/Facilities/" + imageString + ".png")
                    {
                        imageString = "Facilities/" + imageString;
                    }
                }


                if (fields.Length >= 7) // Ensure there are enough fields
                {
                    FactorySO factorySO = CreateInstance<FactorySO>();
                    ////StarSysInt	,	factorySO Enum	,	factorySO Short TextComponent	,	factorySO Long TextComponent	,	Home System	,	Triat One	,	Trait Two	,	factorySO Image	,	Insginia	,	Population	,	Credits	,	StartingTechLevel Points
                    factorySO.CivInt = int.Parse(fields[0]);
                    factorySO.TechLevel = (TechLevel)int.Parse(fields[1]);
                    factorySO.FacilitiesEnumType = (StarSysFacilityType)int.Parse(fields[2]);
                    factorySO.Name = (fields[3]);
                    factorySO.StartStarDate = int.Parse(fields[5]);
                    factorySO.BuildDuration = int.Parse(fields[6]);
                    factorySO.PowerLoad = int.Parse(fields[7]);
                    factorySO.FactorySprite = Resources.Load<Sprite>(imageString);
                    factorySO.Description = (fields[8]);
                    string assetPath = $"Assets/SO/StarSysFactorySO/FactorySO_{factorySO.CivInt}_{factorySO.Name}.asset";
                    AssetDatabase.CreateAsset(factorySO, assetPath);
                    AssetDatabase.SaveAssets();
                }
            }
            Debug.Log("FactorySOImporter Import Complete");
        }
    }

#endif
}


