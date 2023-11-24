using System;

namespace Osmalyzer
{
    public abstract class CorrelatorParamater
    {
    }

    public class MatchDistanceParamater : CorrelatorParamater
    {
        public int Distance { get; }

        
        public MatchDistanceParamater(int distance)
        {
            Distance = distance;
        }
    }

    public class MatchFarDistanceParamater : CorrelatorParamater
    {        
        public int FarDistance { get; }

        
        public MatchFarDistanceParamater(int farDistance)
        {
            FarDistance = farDistance;
        }
    }

    public class MatchCallbackParameter<T> : CorrelatorParamater where T : ICorrelatorItem
    {
        public Func<T, OsmElement, bool> MatchCallback { get; }
        

        public MatchCallbackParameter(Func<T, OsmElement, bool> matchCallback)
        {
            MatchCallback = matchCallback;
        }
    }

    public class LoneElementAllowanceCallbackParameter : CorrelatorParamater
    {
        public Func<OsmElement, bool> AllowanceCallback { get; }
        

        public LoneElementAllowanceCallbackParameter(Func<OsmElement, bool> allowanceCallback)
        {
            AllowanceCallback = allowanceCallback;
        }
    }
}