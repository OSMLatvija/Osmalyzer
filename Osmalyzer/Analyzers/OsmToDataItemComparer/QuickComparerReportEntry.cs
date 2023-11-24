using System;

namespace Osmalyzer
{
    public abstract class QuickComparerReportEntry
    {
    }

    public class MatchedItemQuickComparerReportEntry : QuickComparerReportEntry
    {
    }

    public class MatchedItemButFarQuickComparerReportEntry : QuickComparerReportEntry
    {
    }

    public class UnmatchedItemQuickComparerReportEntry : QuickComparerReportEntry
    {        
    }

    public class UnmatchedOsmQuickComparerReportEntry : QuickComparerReportEntry
    {
    }
}