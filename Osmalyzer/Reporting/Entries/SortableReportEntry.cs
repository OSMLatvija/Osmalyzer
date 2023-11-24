namespace Osmalyzer;

public abstract class SortableReportEntry : ReportEntry
{
    public EntrySortingRule? SortingRule { get; }

        
    protected SortableReportEntry(string text, ReportEntryContext? context, EntrySortingRule? sortingRule)
        : base(text, context)
    {
        SortingRule = sortingRule;
    }
}