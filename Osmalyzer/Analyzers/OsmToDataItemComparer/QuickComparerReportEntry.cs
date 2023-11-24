namespace Osmalyzer
{
    public abstract class QuickComparerReportEntry
    {
    }

    public class MatchedItemQuickComparerReportEntry : QuickComparerReportEntry
    {
        public double Distance { get; set; }

        
        public MatchedItemQuickComparerReportEntry(double distance)
        {
            Distance = distance;
        }
    }

    public class MatchedItemButFarQuickComparerReportEntry : QuickComparerReportEntry
    {
    }

    public class UnmatchedItemQuickComparerReportEntry : QuickComparerReportEntry
    {        
        public double Distance { get; set; }

        
        public UnmatchedItemQuickComparerReportEntry(double distance)
        {
            Distance = distance;
        }
    }
}