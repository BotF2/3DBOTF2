using BOTF3D.Core;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ShipyardSOImporter : EditorWindow
{
#if UNITY_EDITOR

    [MenuItem("Tools/Import ShipyardSO TSV")]
    public static void ShowWindow()
    {
        GetWindow<ShipyardSOImporter>("ShipyardSO TSV Importer");
    }

    private string filePath = $"3D_BOTF2/Assets/Editor/Data/StarSysShipyard.tsv";

    void OnGUI()
    {
        GUILayout.Label("ShipyardSO TSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("TSV File Path", filePath);

        if (GUILayout.Button("Import ShipyardSO TSV"))
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
                foreach (string file in Directory.GetFiles($"BOTF3D/Resources/Shipyards/", "*.png"))
                {
                    if (file == "BOTF3D/Resources/Shipyards/" + imageString + ".png")
                    {
                        imageString = "Shipyards/" + imageString;
                    }
                }


                if (fields.Length >= 7) // Ensure there are enough fields
                {
                    ShipyardSO ShipyardSO = CreateInstance<ShipyardSO>();
                    ////StarSysInt	,	ShipyardSO Enum	,	ShipyardSO Short TextComponent	,	ShipyardSO Long TextComponent	,	Home System	,	Triat One	,	Trait Two	,	ShipyardSO Image	,	Insginia	,	Population	,	Credits	,	StartingTechLevel Points
                    ShipyardSO.CivInt = int.Parse(fields[0]);
                    ShipyardSO.TechLevel = (TechLevel)int.Parse(fields[1]);
                    ShipyardSO.FacilitiesEnumType = (StarSysFacilityType)int.Parse(fields[2]);
                    ShipyardSO.Name = (fields[3]);
                    ShipyardSO.StartStarDate = int.Parse(fields[5]);
                    ShipyardSO.BuildDuration = int.Parse(fields[6]);
                    ShipyardSO.PowerLoad = int.Parse(fields[7]);
                    ShipyardSO.ShipyardSprite = Resources.Load<Sprite>(imageString);
                    ShipyardSO.Description = (fields[8]);
                    string assetPath = $"BOTF3D/SO/StarSysShipyardSO/ShipyardSO_{ShipyardSO.CivInt}_{ShipyardSO.Name}.asset";
                    AssetDatabase.CreateAsset(ShipyardSO, assetPath);
                    AssetDatabase.SaveAssets();
                }
            }
            Debug.Log("ShipyardSOImporter Import Complete");
        }
    }

#endif
}
