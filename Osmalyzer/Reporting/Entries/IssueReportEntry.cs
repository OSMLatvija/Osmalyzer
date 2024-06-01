namespace Osmalyzer;

public class IssueReportEntry : SortableReportEntry
{
    public IssueReportEntry(string text, ReportEntryContext? context = null)
        : base(text, context, null)
    {
    }
            
    public IssueReportEntry(string text, OsmCoord coord, MapPointStyle style, OsmElement? mapOsmElement = null)
        : base(text, null, null)
    {
        SubEntry = 
            mapOsmElement != null ?
                new MapPointReportEntry(coord, text, mapOsmElement, style) :
                new MapPointReportEntry(coord, text, style);
    }
            
    public IssueReportEntry(string text, EntrySortingRule sortingRule, OsmCoord coord, MapPointStyle style, OsmElement? mapOsmElement = null)
        : base(text, null, sortingRule)
    {
        SubEntry = 
            mapOsmElement != null ?
                new MapPointReportEntry(coord, text, mapOsmElement, style) :
                new MapPointReportEntry(coord, text, style);
    }
            
    public IssueReportEntry(string text, EntrySortingRule sortingRule)
        : base(text, null, sortingRule)
    {
    }
            
    public IssueReportEntry(string text, OsmCoord coord, MapPointStyle style, ReportEntryContext? context, OsmElement? mapOsmElement = null)
        : base(text, context, null)
    {
        SubEntry = 
            mapOsmElement != null ?
                new MapPointReportEntry(coord, text, mapOsmElement, style, context) :
                new MapPointReportEntry(coord, text, style, context);
    }
}