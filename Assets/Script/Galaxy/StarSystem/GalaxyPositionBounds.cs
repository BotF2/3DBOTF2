using UnityEngine;

namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Shared, runtime-safe envelope that StarSysSO.Position values must stay within to
    /// remain over the GalaxyImage sprite, plus the weighted "too close" threshold used
    /// when spacing out star systems for the shallow top-down galaxy camera.
    ///
    /// These constants describe the GalaxyScene as it exists today: GalaxyCenter's
    /// transform scale (10,10,10) combined with GalaxyImageHolder/GalaxyImage's compound
    /// rotation+scale and the Galaxy.png sprite's pixel dimensions/PPU. If the galaxy
    /// image, its import settings, or the GalaxyCenter/GalaxyImage transform scales are
    /// ever changed, re-run "Tools/Validate Star System Positions" in the Editor - it
    /// recomputes the true bounds from the live scene and will flag if these constants
    /// have drifted out of sync.
    /// </summary>
    public static class GalaxyPositionBounds
    {
        public const float XMin = -64f;
        public const float XMax = 64f;
        public const float ZMin = -88f;
        public const float ZMax = 88f;

        // Weighted 2D distance metric: the shallow-angle top-down camera makes X
        // separation read as "closer together" on screen than the same Z separation,
        // so X differences are weighted more heavily than Z when judging overlap.
        public const float XWeight = 1.3f;
        public const float ZWeight = 1f;

        // Below this weighted distance, two systems are considered too close together.
        public const float MinRecommendedNeighborDistance = 3f;

        public static bool IsWithinBounds(Vector3 position)
        {
            return position.x >= XMin && position.x <= XMax
                && position.z >= ZMin && position.z <= ZMax;
        }

        public static float WeightedDistance(Vector3 a, Vector3 b)
        {
            float dx = (a.x - b.x) * XWeight;
            float dz = (a.z - b.z) * ZWeight;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
