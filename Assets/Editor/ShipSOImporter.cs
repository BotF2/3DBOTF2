using Assets.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;


public class ShipSOImporter : EditorWindow
{
//#if UNITY_EDITOR

    [MenuItem("Tools/Import ShipSO CSV")]
    public static void ShowWindow()
    {
        GetWindow<ShipSOImporter>("ShipSO CSV Importer");
    }

    private string filePath = $"Assets/Resources/Data/ShipSO.csv";

    void OnGUI()
    {
        GUILayout.Label("ShipSO CSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("CSV File Path", filePath);

        if (GUILayout.Button("Import CSV"))
        {
            //Output the Game data path to the console
            Debug.Log("dataPath : " + Application.dataPath);
            ImportCSV(filePath);
        }
    }

    private static void ImportCSV(string filePath)
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
            string assetPath = " ";

            if (fields.Length > 8) // Ensure there are enough fields
            {
                ShipSO shipSO = CreateInstance<ShipSO>();
                shipSO.ShipName = fields[0];
                shipSO.shipSprite = Resources.Load<Sprite>("Ships/" + shipSO.ShipName);
                if (shipSO.shipSprite != null) { }
                else shipSO.shipSprite = Resources.Load<Sprite>("Ships/DEFAULT");
                string[] data = shipSO.ShipName.Split("_");
                shipSO.CivEnum = GetMyCivEnum(data[0]);
                shipSO.TechLevel = GetMyTechLevel(data[2], out TechLevel st);
                shipSO.ShipType = GetMyShipClass(data[1]);
                shipSO.ShieldMaxHealth = int.Parse(fields[2]);
                shipSO.HullMaxHealth = int.Parse(fields[4]);
                shipSO.TorpedoDamage = int.Parse(fields[6]);
                shipSO.BeamDamage = int.Parse(fields[8]);
                // build duration does not use the csv value, currently calcuated below, consider adding in tech level factor
                //int baseValue = 1000;
                //shipSO.BuildDuration = int.Parse(fields[10]);
                string fBXName= shipSO.ShipName.Replace("(CLONE)","");// remove (CLONE)
                var checkForFBX = Resources.Load<GameObject>("FBX/" + fBXName);
                if (checkForFBX != null)
                    shipSO.Prefab = Resources.Load<GameObject>("FBX/" + fBXName); // load the prefab from Resources/Ships
                //shipSO.ShipDescription = fields[9]; // later we can add a description field to the csv
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
                //shipSO.BuildDuration = int.Parse(fields[10]);
                shipSO.maxWarpFactor = float.Parse(fields[11]);
                if (shipSO.TechLevel == TechLevel.EARLY)
                    assetPath = $"Assets/SO/ShipSO_Level_0/ShipSO_{shipSO.ShipName}.asset";
                else if (shipSO.TechLevel == TechLevel.DEVELOPED)
                    assetPath = $"Assets/SO/ShipSO_Level_1/ShipSO_{shipSO.ShipName}.asset";
                else if (shipSO.TechLevel == TechLevel.ADVANCED)
                    assetPath = $"Assets/SO/ShipSO_Level_2/ShipSO_{shipSO.ShipName}.asset";
                else if (shipSO.TechLevel == TechLevel.SUPREME)
                    assetPath = $"Assets/SO/ShipSO_Level_3/ShipSO_{shipSO.ShipName}.asset";
                AssetDatabase.CreateAsset(shipSO, assetPath);
                AssetDatabase.SaveAssets();
            }
        }

        Debug.Log("ShipSO Import Complete");
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
