using System;
using System.IO;
using UnityEditor;
using UnityEngine;

using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
public class CivSOImporter : EditorWindow
{
#if UNITY_EDITOR


    [MenuItem("Tools/Import CivSO TSV")]
    public static void ShowWindow()
    {
        GetWindow<CivSOImporter>("CivSO TSV Importer");
    }

    private string filePath = "Assets/Editor/Data/Civilizations.tsv";

    void OnGUI()
    {
        GUILayout.Label("CivSO TSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("TSV File Path", filePath);

        if (GUILayout.Button("Import CIV TSV"))
        {
            //Output the Game data path to the console
            Debug.Log("dataPath : " + Application.dataPath);
            ImportCIVTSV(filePath);
        }
    }

    private static void ImportCIVTSV(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            string[] fields = line.Split('\t');

            if (fields.Length > 8)
            {
                CivSO civSO = CreateInstance<CivSO>();
                civSO.CivInt = int.Parse(fields[0]);
                civSO.CivEnum = GetMyCivEnum(fields[1]);
                civSO.CivShortName = fields[2];
                civSO.CivLongName = fields[3];
                civSO.CivHomeSystem = fields[4];
                civSO.WarLikeEnum = GetWarLikeEnum(fields[5]);
                civSO.XenophbiaEnum = GetXenophobiaEnum(fields[6]);
                civSO.RuthlessEnum = GetRuthlessEnum(fields[7]);
                civSO.GreedyEnum = GetGreedyEnum(fields[8]);
                Sprite race = Resources.Load<Sprite>("Races/" + fields[9].ToLower());
                if (race == null) { race = Resources.Load<Sprite>("Races/" + fields[9].ToLower() + "s"); }
                civSO.CivImage = race;
                var name = Resources.Load<Sprite>("Insignias/" + fields[2].ToUpper());
                if (name == null) { name = Resources.Load<Sprite>("Insignias/" + fields[2].ToUpper() + "S"); }
                civSO.Insignia = name;
                civSO.CivTechLevel = TechLevel.EARLY;// StartingTechLevel enum
                civSO.HasWarp = bool.Parse(fields[11]);
                civSO.Playable = bool.Parse(fields[12]);
                civSO.Decription = fields[13];
                string assetPath = $"BOTF3D/SO/CivilizationSO/CivSO_{civSO.CivInt}_{civSO.CivShortName}.asset";
                AssetDatabase.CreateAsset(civSO, assetPath);
                AssetDatabase.SaveAssets();
            }
        }

        Debug.Log("CivSOImporter Import Complete");
    }

    public static CivEnum GetMyCivEnum(string title)
    {
        CivEnum st;
        Enum.TryParse(title, out st);
        return st;
    }
    public static WarLikeEnum GetWarLikeEnum(string title)
    {
        WarLikeEnum st;
        Enum.TryParse(title, out st);
        return st;
    }
    public static XenophobiaEnum GetXenophobiaEnum(string title)
    {
        XenophobiaEnum st;
        Enum.TryParse(title, out st);
        return st;
    }
    public static RuthlessEnum GetRuthlessEnum(string title)
    {
        RuthlessEnum st;
        Enum.TryParse(title, out st);
        return st;
    }
    public static GreedyEnum GetGreedyEnum(string title)
    {
        GreedyEnum st;
        Enum.TryParse(title, out st);
        return st;
    }

#endif
}

