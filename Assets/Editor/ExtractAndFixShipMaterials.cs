using UnityEditor;
using UnityEngine;
using System.IO;

using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
public class ExtractAndFixShipMaterials : EditorWindow
{
    [MenuItem("Tools/Extract and Fix All Ship Materials")]
    static void ExtractAndFix()
    {
        // Find all FBX files in FBX folder
        string[] fbxGUIDs = AssetDatabase.FindAssets("t:Model", new[] { "Assets/FBX" });
        
        int extractedCount = 0;
        
        foreach (string guid in fbxGUIDs)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            
            if (importer == null) continue;
            
            Debug.Log($"Processing FBX: {fbxPath}");
            
            // Extract materials
            if (importer.materialImportMode == ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                // Create output folder based on FBX name
                string fbxName = Path.GetFileNameWithoutExtension(fbxPath);
                string materialFolder = $"Assets/Materials/Ships/{fbxName}";
                
                // Create folder if it doesn't exist
                if (!AssetDatabase.IsValidFolder(materialFolder))
                {
                    string parentFolder = "Assets/Materials/Ships";
                    if (!AssetDatabase.IsValidFolder(parentFolder))
                    {
                        AssetDatabase.CreateFolder("Assets/Materials", "Ships");
                    }
                    AssetDatabase.CreateFolder(parentFolder, fbxName);
                }
                
                // Extract materials
                try
                {
                    var materials = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                    foreach (var asset in materials)
                    {
                        if (asset is Material mat)
                        {
                            string matPath = $"{materialFolder}/{mat.name}.mat";
                            
                            // Create a copy of the material
                            Material newMat = new Material(mat);
                            newMat.renderQueue = 3001; // Fix render queue
                            
                            AssetDatabase.CreateAsset(newMat, matPath);
                            
                            Debug.Log($"  ✅ Extracted and fixed material: {matPath}");
                            extractedCount++;
                        }
                    }
                    
                    // Remap FBX internal materials to the extracted .mat files
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                    importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);
                    importer.SaveAndReimport();

                    Debug.Log($"  ✅ FBX remapped to extracted materials");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"  ❌ Failed to extract materials: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"  ℹ️ FBX already using ImportStandard — skipping");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"=== Extraction Complete ===");
        Debug.Log($"  Extracted: {extractedCount} materials");
        Debug.Log($"  All materials set to render queue 3001");
        
        EditorUtility.DisplayDialog("Complete", 
            $"Extracted {extractedCount} materials.\nAll set to render queue 3001.", 
            "OK");
    }
}