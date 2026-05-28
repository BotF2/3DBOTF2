using System;
using System.IO;
using UnityEditor;
using UnityEngine;

using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
public class FleetSOImporter : EditorWindow
{
#if UNITY_EDITOR
    [MenuItem("Tools/Import FleetSO CSV")]
    public static void ShowWindow()
    {
        GetWindow<FleetSOImporter>("FleetSO CSV Importer");
    }

    private string filePath = "Assets/Editor/Data/FleetSO.csv";

    void OnGUI()
    {
        GUILayout.Label("FleetSO CSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("CSV File Path", filePath);

        if (GUILayout.Button("Import FleetSO CSV"))
        {
            //Output the Game data path to the console
            Debug.Log("dataPath : " + Application.dataPath);
            ImportFleetCSV(filePath);
        }
    }

    private static void ImportFleetCSV(string filePath)
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

            if (fields.Length > 4) // Ensure there are enough fields
            {
                string imageString = fields[1];
                foreach (string file in Directory.GetFiles($"3DBOTF2/Resources/Insignias/", "*.png"))
                {
                    if (file == "3DBOTF2/Resources/Insignias/" + imageString + ".png")
                    {
                        imageString = "Insignias/" + imageString;
                    }
                    else if (file == "3DBOTF2/Resources/Insignias/" + imageString + "S" + ".png")
                    {
                        imageString = "Insignias/" + imageString + "S";
                    }
                }
                FleetSO fleetSO = CreateInstance<FleetSO>();
                //index, insignia, fleetName, civOwnerEnum, defaultWarp
                fleetSO.CivIndex = int.Parse(fields[0]);
                fleetSO.Insignia = Resources.Load<Sprite>(imageString);
                fleetSO.CivOwnerEnum = GetMyCivEnum(fields[2]);
                fleetSO.CurrentWarpFactor = float.Parse(fields[3]);
                fleetSO.Description = fields[4];
                string assetPath = $"3DBOTF2/SO/FleetSO/FleetSO_{fleetSO.CivIndex}_{fleetSO.CivOwnerEnum}.asset";
                AssetDatabase.CreateAsset(fleetSO, assetPath);
                AssetDatabase.SaveAssets();
            }
        }

        Debug.Log("FleetSOImporter Import Complete");
    }
    public static CivEnum GetMyCivEnum(string title)
    {
        CivEnum st;
        Enum.TryParse(title, out st);
        return st;
    }
#endif
}
