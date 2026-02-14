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

    private string filePath = $"BOTF3D/Resources/Data/StarSystems.csv";

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

        foreach (string line in lines)
        {
            string[] fields = line.Split(',');

            if (fields.Length > 7) // Ensure there are enough fields
            {
                string imageString = fields[6];
                foreach (string file in Directory.GetFiles($"BOTF3D/Resources/Stars/", "*.png"))
                {
                    if (file == "BOTF3D/Resources/Stars/" + imageString + ".png")
                    {
                        imageString = "Stars/" + imageString;
                    }
                }
                string imagesPower = fields[17];
                foreach (string file in Directory.GetFiles($"BOTF3D/Resources/Facilities/", "*.png"))
                {
                    if (file == "BOTF3D/Resources/Facilities/" + imagesPower + ".png")
                    {
                        imagesPower = "Facilities/" + imagesPower;
                    }
                }
                string imagesFactory = fields[18];
                foreach (string file in Directory.GetFiles($"BOTF3D/Resources/Facilities/", "*.png"))
                {
                    if (file == "BOTF3D/Resources/Facilities/" + imagesFactory + ".png")
                    {
                        imagesFactory = "Facilities/" + imagesFactory;
                    }
                }
                string imagesShipyard = fields[19];
                foreach (string file in Directory.GetFiles($"BOTF3D/Resources/Facilities/", "*.png"))
                {
                    if (file == "BOTF3D/Resources/Facilities/" + imagesShipyard + ".png")
                    {
                        imagesShipyard = "Facilities/" + imagesShipyard;
                    }
                }
                string imagesShield = fields[20];
                foreach (string file in Directory.GetFiles($"BOTF3D/Resources/Facilities/", "*.png"))
                {
                    if (file == "BOTF3D/Resources/Facilities/" + imagesShield + ".png")
                    {
                        imagesShield = "Facilities/" + imagesShield;
                    }
                }
                string imagesOB = fields[21];
                foreach (string file in Directory.GetFiles($"BOTF3D/Resources/Facilities/", "*.png"))
                {
                    if (file == "BOTF3D/Resources/Facilities/" + imagesOB + ".png")
                    {
                        imagesOB = "Facilities/" + imagesOB;
                    }
                }
                string imagesRC = fields[22];
                foreach (string file in Directory.GetFiles($"BOTF3D/Resources/Facilities/", "*.png"))
                {
                    if (file == "BOTF3D/Resources/Facilities/" + imagesRC + ".png")
                    {
                        imagesRC = "Facilities/" + imagesRC;
                    }
                }

                if (fields.Length >= 8) // Ensure there are enough fields
                {
                    StarSysSO StarSysSO = CreateInstance<StarSysSO>();
                    ////StarSysInt	,	StarSysSO Enum	,	StarSysSO Short TextComponent	,	StarSysSO Long TextComponent	,	Home System	,	Triat One	,	Trait Two	,	StarSysSO Image	,	Insginia	,	Population	,	Credits	,	StartingTechLevel Points
                    StarSysSO.StarSysInt = int.Parse(fields[0]);
                    StarSysSO.Position = new Vector3((int.Parse(fields[1])) / 10, (int.Parse(fields[2])) / 10, (int.Parse(fields[3])) / 10);
                    StarSysSO.SysName = fields[4];
                    StarSysSO.FirstOwner = GetMyCivEnum(fields[5]);
                    StarSysSO.CurrentOwner = GetMyCivEnum(fields[5]);
                    StarSysSO.StarType = GetMyStarTypeEnum(fields[6]);
                    StarSysSO.StarSprit = Resources.Load<Sprite>(imageString);
                    StarSysSO.Population = int.Parse(fields[7]);
                    StarSysSO.PopulationLimit = int.Parse(fields[8]);
                    StarSysSO.Farms = int.Parse(fields[9]);
                    StarSysSO.PowerStations = int.Parse(fields[10]);
                    StarSysSO.Factories = int.Parse(fields[11]);
                    StarSysSO.Shipyards = int.Parse(fields[12]);
                    StarSysSO.ResearchCenters = int.Parse(fields[13]);
                    StarSysSO.ShieldGenerators = int.Parse(fields[14]);
                    StarSysSO.OrbitalBatteries = int.Parse(fields[15]);
                    StarSysSO.Description = "descrition here...";
                    StarSysSO.powerPlantSprite = Resources.Load<Sprite>(imagesPower);
                    StarSysSO.factorySprite = Resources.Load<Sprite>(imagesFactory);
                    StarSysSO.shipyardSprite = Resources.Load<Sprite>(imagesShipyard);
                    StarSysSO.shieldSprite = Resources.Load<Sprite>(imagesShield);
                    StarSysSO.orbitalSprite = Resources.Load<Sprite>(imagesOB);
                    StarSysSO.researchCenterSprite = Resources.Load<Sprite>(imagesRC);
                    string assetPath = $"BOTF3D/SO/StarSysSO/StarSysSO_{StarSysSO.StarSysInt}_{StarSysSO.SysName}.asset";
                    AssetDatabase.CreateAsset(StarSysSO, assetPath);
                    AssetDatabase.SaveAssets();
                }
            }
            Debug.Log("CivSOImporter Import Complete");
        }
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
