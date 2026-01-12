namespace Osmalyzer;

public abstract class SortableReportEntry : ReportEntry
{
    public EntrySortingRule? SortingRule { get; }
    
    public int AdditionIndex { get; set; }

        
    protected SortableReportEntry(string text, ReportEntryContext? context, EntrySortingRule? sortingRule)
        : base(text, context)
    {
        SortingRule = sortingRule;
    }
}