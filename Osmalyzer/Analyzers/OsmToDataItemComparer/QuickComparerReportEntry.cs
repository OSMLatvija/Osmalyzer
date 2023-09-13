namespace Osmalyzer
{
    public abstract class QuickComparerReportEntry
    {
    }

    public class MatchedQuickComparerReportEntry : QuickComparerReportEntry
    {
        public double Distance { get; set; }

        
        public MatchedQuickComparerReportEntry(double distance)
        {
            Distance = distance;
        }
    }

    public class MatchedButFarQuickComparerReportEntry : QuickComparerReportEntry
    {
    }

    public class UnmatchedQuickComparerReportEntry : QuickComparerReportEntry
    {        
        public double Distance { get; set; }

        
        public UnmatchedQuickComparerReportEntry(double distance)
        {
            Distance = distance;
        }
    }
}