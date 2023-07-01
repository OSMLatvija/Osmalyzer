namespace Osmalyzer
{
    public class GenericReportEntry : SortableReportEntry
    {
        public GenericReportEntry(string text)
            : base(text, null, null)
        {
        }
        
        public GenericReportEntry(string text, EntrySortingRule sortingRule)
            : base(text, null, sortingRule)
        {
        }
    }
}