#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helper to automatically set MinTechPointsRequired on ShipSOs based on naming convention
/// Naming pattern: CIV_SHIPTYPE_TIER(CLONE) e.g., FED_DESTROYER_II(CLONE)
/// 
/// NOTE: Uses reflection to avoid assembly reference issues
/// </summary>
public class ShipTechSetupHelper : EditorWindow
{
    [MenuItem("BOTF/Ship Tech Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<ShipTechSetupHelper>("Ship Tech Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ship Tech Points Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool automatically sets MinTechPointsRequired on all ShipSOs based on their naming convention.\n\n" +
            "Expected format: CIV_SHIPTYPE_TIER(CLONE)\n" +
            "Examples:\n" +
            "  FED_DESTROYER_I(CLONE) → 25 points (Early)\n" +
            "  FED_CRUISER_II(CLONE) → 150 points (Developed)\n" +
            "  FED_HVYCRUISER_IV(CLONE) → 850 points (Supreme)",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Setup All Ships", GUILayout.Height(40)))
        {
            SetupAllShips();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("List All Ships (Debug)", GUILayout.Height(30)))
        {
            ListAllShips();
        }
    }

    private void SetupAllShips()
    {
        string[] guids = AssetDatabase.FindAssets("t:ShipSO");
        int updated = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ship = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (ship == null) continue;

            // Use reflection to access properties
            var shipNameProp = ship.GetType().GetField("ShipName");
            var minPointsProp = ship.GetType().GetField("MinTechPointsRequired");

            if (shipNameProp == null || minPointsProp == null)
            {
                skipped++;
                continue;
            }

            string shipName = shipNameProp.GetValue(ship) as string;
            if (string.IsNullOrEmpty(shipName)) continue;

            int recommendedPoints = GetRecommendedPointsFromName(shipName);
            int currentPoints = (int)minPointsProp.GetValue(ship);

            if (recommendedPoints >= 0 && currentPoints != recommendedPoints)
            {
                minPointsProp.SetValue(ship, recommendedPoints);
                EditorUtility.SetDirty(ship);
                updated++;
                Debug.Log($"✅ Updated {shipName}: {recommendedPoints} tech points required");
            }
            else if (recommendedPoints < 0)
            {
                skipped++;
                Debug.LogWarning($"⚠️ Skipped {shipName}: Could not determine tech points from name");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"=== Ship Tech Setup Complete ===");
        Debug.Log($"  Updated: {updated} ships");
        Debug.Log($"  Skipped: {skipped} ships");

        EditorUtility.DisplayDialog("Setup Complete",
            $"Updated {updated} ships\nSkipped {skipped} ships\n\nCheck Console for details.",
            "OK");
    }

    private int GetRecommendedPointsFromName(string shipName)
    {
        if (string.IsNullOrEmpty(shipName)) return -1;

        string upperName = shipName.ToUpper();

        // Determine tier from name (_I, _II, _III, _IV)
        int tier = 1;
        if (upperName.Contains("_IV")) tier = 4;
        else if (upperName.Contains("_III")) tier = 3;
        else if (upperName.Contains("_II")) tier = 2;
        else if (upperName.Contains("_I")) tier = 1;

        // Map tier to recommended points
        switch (tier)
        {
            case 1: // EARLY (_I)
                if (upperName.Contains("SCOUT")) return 0;
                if (upperName.Contains("DESTROYER")) return 25;
                if (upperName.Contains("TRANSPORT")) return 50;
                return 0;

            case 2: // DEVELOPED (_II)
                if (upperName.Contains("CRUISER") && !upperName.Contains("LT") && !upperName.Contains("HVY"))
                    return 150; // Cruiser_II
                if (upperName.Contains("SCOUT") || upperName.Contains("DESTROYER") || upperName.Contains("TRANSPORT"))
                    return 100; // Basic _II variants
                return 100;

            case 3: // ADVANCED (_III)
                if (upperName.Contains("CRUISER") && !upperName.Contains("LT") && !upperName.Contains("HVY"))
                    return 400; // Cruiser_III
                if (upperName.Contains("SCOUT") || upperName.Contains("DESTROYER") || upperName.Contains("TRANSPORT"))
                    return 300; // Basic _III variants
                return 300;

            case 4: // SUPREME (_IV)
                if (upperName.Contains("HVYCRUISER") || upperName.Contains("HVY"))
                    return 850; // HvyCruiser_IV
                if (upperName.Contains("LTCRUISER") || (upperName.Contains("LT") && upperName.Contains("CRUISER")))
                    return 700; // LtCruiser_IV
                if (upperName.Contains("SCOUT") || upperName.Contains("DESTROYER") || upperName.Contains("TRANSPORT"))
                    return 600; // Basic _IV variants
                return 600;

            default:
                return -1;
        }
    }

    private void ListAllShips()
    {
        string[] guids = AssetDatabase.FindAssets("t:ShipSO");

        Debug.Log("=== All Ships in Project ===");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ship = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (ship != null)
            {
                var shipNameProp = ship.GetType().GetField("ShipName");
                var minPointsProp = ship.GetType().GetField("MinTechPointsRequired");
                var civEnumProp = ship.GetType().GetField("CivEnum");

                if (shipNameProp == null || minPointsProp == null) continue;

                string shipName = shipNameProp.GetValue(ship) as string;
                int currentPoints = (int)minPointsProp.GetValue(ship);
                int recommended = GetRecommendedPointsFromName(shipName);
                string status = currentPoints == recommended ? "✅" : "⚠️";
                string civEnum = civEnumProp != null ? civEnumProp.GetValue(ship).ToString() : "Unknown";

                Debug.Log($"{status} {shipName} ({civEnum}): " +
                          $"Current={currentPoints}, " +
                          $"Recommended={recommended}");
            }
        }
    }
}
#endif
