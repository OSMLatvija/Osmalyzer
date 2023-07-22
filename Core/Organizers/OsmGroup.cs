using System.Collections.Generic;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [PublicAPI]
    public class OsmGroup
    {
        public string Value { get; }

        public List<OsmElement> Elements { get; } = new List<OsmElement>();


        public int Count => Elements.Count;


        public OsmGroup(string value)
        {
            Value = value;
        }

        
        [Pure]
        public OsmCoord GetAverageElementCoord()
        {
            return OsmGeoTools.GetAverageCoord(Elements);
        }

        [Pure]
        public List<string> CollectValues(string key)
        {
            List<string> values = new List<string>();

            foreach (OsmElement element in Elements)
            {
                string? value = element.GetValue(key);
                
                if (value != null)
                    if (!values.Contains(value))
                        values.Add(value);
            }
            
            return values;
        }
    }
}