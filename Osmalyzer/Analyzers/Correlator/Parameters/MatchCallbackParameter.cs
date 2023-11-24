using System;

namespace Osmalyzer
{
    public class MatchCallbackParameter<T> : CorrelatorParamater where T : ICorrelatorItem
    {
        public Func<T, OsmElement, bool> MatchCallback { get; }
        

        public MatchCallbackParameter(Func<T, OsmElement, bool> matchCallback)
        {
            MatchCallback = matchCallback;
        }
    }
}