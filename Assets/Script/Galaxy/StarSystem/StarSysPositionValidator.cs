#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BOTF3D.Galaxy;

/// <summary>
/// Editor-only batch check for StarSysSO.Position values: flags any system positioned
/// outside the galaxy image and any pair of systems positioned too close together.
/// Also recomputes the true galaxy image bounds from the live GalaxyScene and compares
/// them against the hardcoded GalaxyPositionBounds constants, so drift (e.g. after the
/// galaxy art or its scene transforms change) gets caught instead of silently going stale.
/// </summary>
public static class StarSysPositionValidator
{
    private const string GalaxyScenePath = "Assets/Scenes/GalaxyScene.unity";
    private const float BoundsDriftTolerance = 0.5f;

    [MenuItem("Tools/Validate Star System Positions")]
    public static void ValidatePositions()
    {
        CheckBoundsAgainstLiveScene();

        var guids = AssetDatabase.FindAssets("t:StarSysSO");
        var systems = new List<StarSysSO>();
        foreach (var guid in guids)
        {
            var so = AssetDatabase.LoadAssetAtPath<StarSysSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (so != null) systems.Add(so);
        }

        int outOfBounds = 0;
        foreach (var so in systems)
        {
            if (!GalaxyPositionBounds.IsWithinBounds(so.Position))
            {
                outOfBounds++;
                Debug.LogWarning($"[StarSysPositionValidator] {so.SysName} is OUTSIDE the galaxy image bounds: {so.Position}", so);
            }
        }

        int tooClose = 0;
        for (int i = 0; i < systems.Count; i++)
        {
            float nearest = float.MaxValue;
            StarSysSO nearestSo = null;
            for (int j = 0; j < systems.Count; j++)
            {
                if (i == j) continue;
                float d = GalaxyPositionBounds.WeightedDistance(systems[i].Position, systems[j].Position);
                if (d < nearest)
                {
                    nearest = d;
                    nearestSo = systems[j];
                }
            }

            if (nearest < GalaxyPositionBounds.MinRecommendedNeighborDistance)
            {
                tooClose++;
                Debug.LogWarning(
                    $"[StarSysPositionValidator] {systems[i].SysName} is only {nearest:F2} units from {nearestSo.SysName} " +
                    $"(min recommended {GalaxyPositionBounds.MinRecommendedNeighborDistance}).", systems[i]);
            }
        }

        Debug.Log($"[StarSysPositionValidator] Checked {systems.Count} systems. Out of bounds: {outOfBounds}. Too close to a neighbor: {tooClose}.");
    }

    private static void CheckBoundsAgainstLiveScene()
    {
        if (!TryComputeLiveBoundsInGalaxyCenterSpace(out Vector3 min, out Vector3 max))
        {
            Debug.LogWarning("[StarSysPositionValidator] Could not recompute galaxy bounds from the live scene - " +
                "skipping the drift check and validating against the existing GalaxyPositionBounds constants only.");
            return;
        }

        bool drifted =
            Mathf.Abs(min.x - GalaxyPositionBounds.XMin) > BoundsDriftTolerance ||
            Mathf.Abs(max.x - GalaxyPositionBounds.XMax) > BoundsDriftTolerance ||
            Mathf.Abs(min.z - GalaxyPositionBounds.ZMin) > BoundsDriftTolerance ||
            Mathf.Abs(max.z - GalaxyPositionBounds.ZMax) > BoundsDriftTolerance;

        if (drifted)
        {
            Debug.LogWarning(
                "[StarSysPositionValidator] GalaxyPositionBounds constants look stale. " +
                $"Live scene bounds are x:[{min.x:F2},{max.x:F2}] z:[{min.z:F2},{max.z:F2}], " +
                $"but GalaxyPositionBounds has x:[{GalaxyPositionBounds.XMin},{GalaxyPositionBounds.XMax}] " +
                $"z:[{GalaxyPositionBounds.ZMin},{GalaxyPositionBounds.ZMax}]. " +
                "Update the constants in GalaxyPositionBounds.cs to match.");
        }
        else
        {
            Debug.Log("[StarSysPositionValidator] GalaxyPositionBounds constants match the live scene.");
        }
    }

    private static bool TryComputeLiveBoundsInGalaxyCenterSpace(out Vector3 min, out Vector3 max)
    {
        min = max = default;

        Scene scene = default;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.path == GalaxyScenePath) { scene = s; break; }
        }

        bool weOpenedScene = false;
        if (!scene.IsValid())
        {
            scene = EditorSceneManager.OpenScene(GalaxyScenePath, OpenSceneMode.Additive);
            weOpenedScene = true;
        }

        try
        {
            GameObject galaxyCenterGo = null;
            GameObject galaxyImageGo = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                galaxyCenterGo ??= FindByName(root.transform, "GalaxyCenter");
                galaxyImageGo ??= FindByName(root.transform, "GalaxyImage");
            }

            if (galaxyCenterGo == null || galaxyImageGo == null)
                return false;

            var spriteRenderer = galaxyImageGo.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                return false;

            var worldBounds = spriteRenderer.bounds;
            var galaxyCenterTransform = galaxyCenterGo.transform;

            Vector3 localMin = Vector3.positiveInfinity;
            Vector3 localMax = Vector3.negativeInfinity;
            for (int cx = 0; cx <= 1; cx++)
            for (int cy = 0; cy <= 1; cy++)
            for (int cz = 0; cz <= 1; cz++)
            {
                var worldCorner = new Vector3(
                    cx == 0 ? worldBounds.min.x : worldBounds.max.x,
                    cy == 0 ? worldBounds.min.y : worldBounds.max.y,
                    cz == 0 ? worldBounds.min.z : worldBounds.max.z);
                var localCorner = galaxyCenterTransform.InverseTransformPoint(worldCorner);
                localMin = Vector3.Min(localMin, localCorner);
                localMax = Vector3.Max(localMax, localCorner);
            }

            min = localMin;
            max = localMax;
            return true;
        }
        finally
        {
            if (weOpenedScene)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameObject FindByName(Transform root, string name)
    {
        if (root.name == name) return root.gameObject;
        foreach (Transform child in root)
        {
            var found = FindByName(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
