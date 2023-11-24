using System;

namespace Osmalyzer
{
    public abstract class QuickCompareParamater
    {
    }

    public class MatchCallbackQuickCompareParameter<T> : QuickCompareParamater where T : IQuickComparerDataItem
    {
        public Func<T, OsmElement, bool> MatchCallback { get; }
        

        public MatchCallbackQuickCompareParameter(Func<T, OsmElement, bool> matchCallback)
        {
            MatchCallback = matchCallback;
        }
    }

    public class UnmatchedOsmElementAllowedByItselfCallbackQuickCompareParameter : QuickCompareParamater
    {
        public Func<OsmElement, bool> AllowanceCallback { get; }
        

        public UnmatchedOsmElementAllowedByItselfCallbackQuickCompareParameter(Func<OsmElement, bool> allowanceCallback)
        {
            AllowanceCallback = allowanceCallback;
        }
    }
}