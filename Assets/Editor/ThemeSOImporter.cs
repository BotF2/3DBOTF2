using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ThemeSOImporter : EditorWindow
{
#if UNITY_EDITOR


    [MenuItem("Tools/Import ThemeSO CSV")]
    public static void ShowWindow()
    {
        GetWindow<ThemeSOImporter>("ThemeSO CSV Importer");
    }

    private string filePath = "Assets/Editor/Data/Theme.csv"; // just a place holder csv file, to be replaced with the actual file

    void OnGUI()
    {
        GUILayout.Label("ThemeSO CSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("CSV File Path", filePath);

        if (GUILayout.Button("Import Theme CSV"))
        {
            //Output the Game data path to the console
            Debug.Log("dataPath : " + Application.dataPath);
            ImportThemeCSV(filePath);
        }
    }

    private static void ImportThemeCSV(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            string[] fields = line.Split(',');

            if (fields.Length > 8)
            {
                ThemeSO themeSO = CreateInstance<ThemeSO>();
                //themeSO.CivInt = int.Parse(fields[0]);
                //themeSO.CivEnum = GetMyCivEnum(fields[1]);
                //themeSO.CivShortName = fields[2];
                //themeSO.CivLongName = fields[3];
                //themeSO.CivHomeSystemName = fields[4];
                //themeSO.WarLikeEnum = GetWarLikeEnum(fields[5]);
                //themeSO.XenophbiaEnum = GetXenophobiaEnum(fields[6]);
                //themeSO.RuthlessEnum = GetRuthlessEnum(fields[7]);
                //themeSO.GreedyEnum = GetGreedyEnum(fields[8]);
                //Sprite race = Resources.Load<Sprite>("Races/" + fields[9].ToLower());
                //if (race == null) { race = Resources.Load<Sprite>("Races/" + fields[9].ToLower() + "s"); }
                //themeSO.CivImage = race;
                //var name = Resources.Load<Sprite>("Insignias/" + fields[2].ToUpper());
                //if (name == null) { name = Resources.Load<Sprite>("Insignias/" + fields[2].ToUpper() + "S"); }
                //themeSO.Insignia = name;
                //themeSO.CivTechLevel = StartingTechLevel.EARLY;// StartingTechLevel enum
                //themeSO.HasWarp = bool.Parse(fields[11]);
                //themeSO.Playable = bool.Parse(fields[12]);
                //themeSO.Decription = "ToDo, connect to libaray of themeSO descriptions";
                //string assetPath = $"Assets/SO/CivilizationSO/ThemeSO_{themeSO.CivInt}_{themeSO.CivShortName}.asset";
                // AssetDatabase.CreateAsset(themeSO, assetPath);
                AssetDatabase.SaveAssets();
            }
        }

        Debug.Log("ThemeSOImporter Import Complete");
    }

    public static ThemeEnum GetThemeEnum(string title)
    {
        ThemeEnum st;
        Enum.TryParse(title, out st);
        return st;
    }
    //public static WarLikeEnum GetWarLikeEnum(string title)
    //{
    //    WarLikeEnum st;
    //    Enum.TryParse(title, out st);
    //    return st;
    //}
    //public static XenophobiaEnum GetXenophobiaEnum(string title)
    //{
    //    XenophobiaEnum st;
    //    Enum.TryParse(title, out st);
    //    return st;
    //}
    //public static RuthlessEnum GetRuthlessEnum(string title)
    //{
    //    RuthlessEnum st;
    //    Enum.TryParse(title, out st);
    //    return st;
    //}
    //public static GreedyEnum GetGreedyEnum(string title)
    //{
    //    GreedyEnum st;
    //    Enum.TryParse(title, out st);
    //    return st;
    //}

#endif
}


