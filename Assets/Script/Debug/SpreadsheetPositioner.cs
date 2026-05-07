using System.IO;
using System.Text;
using UnityEngine;

public class SpreadsheetPositioner : MonoBehaviour
{
    [Header("Input Settings")]
    public string inputFileName = "UntitledSpreadsheet.csv";
    public string outputFileName = "Updated_Positions.csv";

    [Header("Adjustment Settings")]
    public float xOffsetMultiplier = 0.05f;
    public float zSpreadMultiplier = 1.1f;
    public float staggerAmount = 0.2f;

    [ContextMenu("Generate New CSV")]
    public void ExportNewCoordinates()
    {
        string inputPath = Path.Combine(Application.dataPath, inputFileName);
        string outputPath = Path.Combine(Application.dataPath, outputFileName);

        if (!File.Exists(inputPath))
        {
            Debug.LogError("Source CSV not found at: " + inputPath);
            return;
        }

        string[] lines = File.ReadAllLines(inputPath);
        StringBuilder csvContent = new StringBuilder();

        // Add Header to the new file
        csvContent.AppendLine("New_X,New_Y,New_Z,Original_X,Original_Y,Original_Z");

        // Loop through data (starting at index 1 to skip original headers)
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length < 3) continue;

            if (float.TryParse(values[0], out float oldX) &&
                float.TryParse(values[1], out float oldY) &&
                float.TryParse(values[2], out float oldZ))
            {
                // Apply the anti-overlap logic
                float stagger = (i % 2 == 0) ? staggerAmount : -staggerAmount;
                float newX = oldX + (oldZ * xOffsetMultiplier) + stagger;
                float newY = oldY;
                float newZ = oldZ * zSpreadMultiplier;

                // Create a CSV row: New values first, then original values for reference
                csvContent.AppendLine($"{newX:F3},{newY:F3},{newZ:F3},{oldX},{oldY},{oldZ}");
            }
        }

        // Save the file to the Assets folder
        File.WriteAllText(outputPath, csvContent.ToString());

        Debug.Log($"<b>Success!</b> New spreadsheet created at: {outputPath}");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh(); // Makes the file appear in Unity immediately
#endif
    }
}
