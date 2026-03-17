using BOTF3D.Core;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PowerPlantSOImporter : EditorWindow
{
#if UNITY_EDITOR

    [MenuItem("Tools/Import PowerPlantSO TSV")]
    public static void ShowWindow()
    {
        GetWindow<PowerPlantSOImporter>("PowerPlantSO TSV Importer");
    }

    private string filePath = "Assets/Editor/Data/StarSysPowerPlant.tsv";

    void OnGUI()
    {
        GUILayout.Label("PowerPlantSO TSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("TSV File Path", filePath);

        if (GUILayout.Button("Import PowerPlantSO TSV"))
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
        //MessageBox.Show("xy is imported separeted with TABs", "INFO", MessageBoxButton.OK);
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
                    PowerPlantSO powerPlantSO = CreateInstance<PowerPlantSO>();
                    ////StarSysInt	,	powerPlantSO Enum	,	powerPlantSO Short TextComponent	,	powerPlantSO Long TextComponent	,	Home System	,	Triat One	,	Trait Two	,	powerPlantSO Image	,	Insginia	,	Population	,	Credits	,	StartingTechLevel Points
                    powerPlantSO.CivInt = int.Parse(fields[0]);
                    powerPlantSO.TechLevel = (TechLevel)int.Parse(fields[1]);
                    powerPlantSO.FacilitiesEnumType = (StarSysFacilityType)int.Parse(fields[2]);
                    powerPlantSO.Name = (fields[3]);
                    powerPlantSO.StartStarDate = int.Parse(fields[5]);
                    powerPlantSO.BuildDuration = int.Parse(fields[6]);
                    powerPlantSO.PowerOutput = int.Parse(fields[7]);
                    powerPlantSO.PowerPlantSprite = Resources.Load<Sprite>(imageString);
                    powerPlantSO.Description = (fields[8]);
                    string assetPath = $"BOTF3D/SO/StarSysPowerPlantSO/PowerPlantSO_{powerPlantSO.CivInt}_{powerPlantSO.Name}.asset";
                    AssetDatabase.CreateAsset(powerPlantSO, assetPath);
                    AssetDatabase.SaveAssets();
                }
            }
            Debug.Log("PowerPlantSOImporter Import Complete");
        }
    }

#endif
}
