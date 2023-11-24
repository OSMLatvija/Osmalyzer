namespace Osmalyzer;

/// <summary>
/// Defines the sort order of <see cref="SortableReportEntry"/>s within their <see cref="ReportGroup"/>.
/// This means entries can be passsed to <see cref="Report"/> without needing to observe any particular order,
/// but they will still be sorted according to the rule when collected for display.
/// </summary>
public abstract class EntrySortingRule
{
}