namespace Osmalyzer
{
    public class IssueReportEntry : SortableReportEntry
    {
        public IssueReportEntry(string text, ReportEntryContext? context = null)
            : base(text, context, null)
        {
        }
            
        public IssueReportEntry(string text, OsmCoord coord)
            : base(text, null, null)
        {
            SubEntry = new MapPointReportEntry(coord, text);
        }
            
        public IssueReportEntry(string text, EntrySortingRule sortingRule, OsmCoord coord)
            : base(text, null, sortingRule)
        {
            SubEntry = new MapPointReportEntry(coord, text);
        }
            
        public IssueReportEntry(string text, EntrySortingRule sortingRule)
            : base(text, null, sortingRule)
        {
        }
            
        public IssueReportEntry(string text, OsmCoord coord, ReportEntryContext? context)
            : base(text, context, null)
        {
            SubEntry = new MapPointReportEntry(coord, text, context);
        }
    }
}