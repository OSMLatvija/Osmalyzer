namespace Osmalyzer;

public class IssueReportEntry : SortableReportEntry
{
    public IssueReportEntry(string text, ReportEntryContext? context = null)
        : base(text, context, null)
    {
    }
            
    public IssueReportEntry(string text, OsmCoord coord, MapPointStyle style)
        : base(text, null, null)
    {
        SubEntry = new MapPointReportEntry(coord, text, style);
    }
            
    public IssueReportEntry(string text, EntrySortingRule sortingRule, OsmCoord coord, MapPointStyle style)
        : base(text, null, sortingRule)
    {
        SubEntry = new MapPointReportEntry(coord, text, style);
    }
            
    public IssueReportEntry(string text, EntrySortingRule sortingRule)
        : base(text, null, sortingRule)
    {
    }
            
    public IssueReportEntry(string text, OsmCoord coord, MapPointStyle style, ReportEntryContext? context)
        : base(text, context, null)
    {
        SubEntry = new MapPointReportEntry(coord, text, style, context);
    }
}