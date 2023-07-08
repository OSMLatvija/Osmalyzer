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

        public GenericReportEntry(string text, OsmCoord coord)
            : base(text, null, null)
        {
            SubEntry = new MapPointReportEntry(coord, text);
        }
        
        public GenericReportEntry(string text, EntrySortingRule sortingRule, OsmCoord coord)
            : base(text, null, sortingRule)
        {
            SubEntry = new MapPointReportEntry(coord, text);
        }
    }
}