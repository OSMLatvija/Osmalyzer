using System.Collections.Generic;

namespace Osmalyzer;

public class CorrelatorReport<T> where T : ICorrelatorItem
{
    public Dictionary<OsmElement, T> MatchedElements { get; }

        
    public CorrelatorReport(Dictionary<OsmElement, T> matchedElements)
    {
        MatchedElements = matchedElements;
    }
}