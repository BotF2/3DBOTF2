namespace BOTF3D.Core
{
    public enum ReportCategory { Combat, Diplomacy, Intel }

    public class ReportEntry
    {
        public ReportCategory Category  { get; }
        public int            Stardate  { get; }
        public string         Summary   { get; }
        public string         Detail    { get; }

        public ReportEntry(ReportCategory category, int stardate, string summary, string detail = "")
        {
            Category = category;
            Stardate = stardate;
            Summary  = summary;
            Detail   = detail ?? "";
        }
    }
}
