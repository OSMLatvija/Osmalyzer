namespace Osmalyzer
{
    public class IssueReportEntry : ReportEntry
    {
        public IssueReportEntry(string text, ReportEntryContext? context = null)
            : base(text, context)
        {
        }
            
        public IssueReportEntry(string text, OsmCoord coord)
            : base(text)
        {
            SubEntry = new MapPointReportEntry(coord, text);
        }
    }
}