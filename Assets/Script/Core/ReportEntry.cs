using UnityEngine;

namespace BOTF3D.Core
{
    public enum ReportCategory { Combat, Diplomacy, Intel }

    public enum ReportSeverity { Info, Warning, Critical }

    /// <summary>
    /// Star Trek-style galaxy quadrant, split at the origin of galaxy-space (x, z).
    /// </summary>
    public enum GalaxyQuadrant { Alpha, Beta, Gamma, Delta }

    public class ReportEntry
    {
        public ReportCategory  Category  { get; }
        public int             Stardate  { get; }
        public string          Summary   { get; }
        public string          Detail    { get; }
        public string          Location  { get; }
        public GalaxyQuadrant? Quadrant  { get; }
        public ReportSeverity  Severity  { get; }

        public ReportEntry(ReportCategory category, int stardate, string summary, string detail = "",
            string location = "", GalaxyQuadrant? quadrant = null, ReportSeverity severity = ReportSeverity.Info)
        {
            Category = category;
            Stardate = stardate;
            Summary  = summary;
            Detail   = detail ?? "";
            Location = location ?? "";
            Quadrant = quadrant;
            Severity = severity;
        }

        /// <summary>
        /// negative x, negative z = Alpha; positive x, negative z = Beta;
        /// positive x, positive z = Delta; negative x, positive z = Gamma.
        /// </summary>
        public static GalaxyQuadrant QuadrantFromPosition(Vector3 position)
        {
            if (position.x < 0f)
                return position.z < 0f ? GalaxyQuadrant.Alpha : GalaxyQuadrant.Gamma;
            return position.z < 0f ? GalaxyQuadrant.Beta : GalaxyQuadrant.Delta;
        }
    }
}
