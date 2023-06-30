namespace Osmalyzer
{
    /// <summary>
    /// Shown on a map
    /// </summary>
    public class MapPointReportEntry : ReportEntry
    {
        public OsmCoord Coord { get; }


        public MapPointReportEntry(OsmCoord coord, string text, ReportEntryContext? context = null)
            : base(text, context)
        {
            Coord = coord;
        }
    }
}