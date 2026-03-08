using BOTF3D.Core;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SoundSOImporter : EditorWindow
{
#if UNITY_EDITOR
    [MenuItem("Tools/Import SoundSO CSV")]
    public static void ShowWindow()
    {
        GetWindow<SoundSOImporter>("SoundSO CSV Importer");
    }

    private string filePath = $"BOTF3D/Resources/Data/SoundSO.csv";

    void OnGUI()
    {
        GUILayout.Label("SoundSO CSV Importer", EditorStyles.boldLabel);
        filePath = EditorGUILayout.TextField("CSV File Path", filePath);

        if (GUILayout.Button("Import SoundSO CSV"))
        {
            //Output the Game data path to the console
            Debug.Log("dataPath : " + Application.dataPath);
            ImportSoundCSV(filePath);
        }
    }

    private static void ImportSoundCSV(string filePath)
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
                SoundSO soundSO = CreateInstance<SoundSO>();
                //Comment: clip (AudioClip), volume (float), pitchRange (Vector2), spatial (bool) priority (int)
                //soundSO.clip = ;
                soundSO.volume = 0.25f;
                soundSO.pitchRange = new Vector2(5, 5);
                soundSO.spatial = false;
                soundSO.priority = 5;
                string assetPath = $"BOTF3D/SO/SoundSO/SoundSO_{soundSO.clip.name}.asset";
                //AssetDatabase.CreateAsset(soundSO, assetPath);
                AssetDatabase.SaveAssets();
            }
        }

        Debug.Log("SoundSOImporter Import Complete");
    }
#endif
}
