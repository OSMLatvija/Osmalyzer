using System.Collections.Generic;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [PublicAPI]
    public class OsmGroup
    {
        public string Value { get; }

        public List<OsmElement> Elements { get; } = new List<OsmElement>();


        public OsmGroup(string value)
        {
            Value = value;
        }

        
        [Pure]
        public OsmCoord GetAverageElementCoord()
        {
            return OsmGeoTools.GetAverageCoord(Elements);
        }
    }
}