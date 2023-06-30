namespace Osmalyzer
{
    /// <summary> Shown on a map, if possible </summary>
    public class MapPointReportEntry : ReportEntry
    {
        public OsmCoord Coord { get; }


        public MapPointReportEntry(OsmCoord coord, string text)
            : base(text)
        {
            Coord = coord;
        }
    }
}