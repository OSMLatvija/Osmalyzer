using System;

namespace Osmalyzer
{
    public abstract class QuickCompareParamater
    {
    }

    public class MatchDistanceQuickCompareParamater : QuickCompareParamater
    {
        public int Distance { get; }

        
        public MatchDistanceQuickCompareParamater(int distance)
        {
            Distance = distance;
        }
    }

    public class MatchFarDistanceQuickCompareParamater : QuickCompareParamater
    {        
        public int FarDistance { get; }

        
        public MatchFarDistanceQuickCompareParamater(int farDistance)
        {
            FarDistance = farDistance;
        }
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