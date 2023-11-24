using System.Collections.Generic;

namespace Osmalyzer
{
    public class QuickCompareReport<T> where T : IQuickComparerDataItem
    {
        public Dictionary<OsmElement, T> MatchedElements { get; }

        
        public QuickCompareReport(Dictionary<OsmElement, T> matchedElements)
        {
            MatchedElements = matchedElements;
        }
    }
}