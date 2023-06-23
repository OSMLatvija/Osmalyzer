using System.Collections.Generic;
using JetBrains.Annotations;
using OsmSharp.Tags;

namespace Osmalyzer
{
    [PublicAPI]
    public class OsmMultiValueGroup
    {
        /// <summary> All the unique values </summary>
        public List<string> Values { get; } = new List<string>();

        public List<OsmMultiValueElement> Elements { get; } = new List<OsmMultiValueElement>();

        /// <summary> Element counts for each unique value </summary>
        public List<int> ElementCounts { get; } = new List<int>();

        
        public List<string> GetUniqueKeyValues(string key, bool sort = false)
        {
            List<string> used = new List<string>();

            foreach (OsmMultiValueElement element in Elements)
                foreach (Tag tag in element.Element.RawElement.Tags)
                    if (tag.Key == key)
                        if (!used.Contains(tag.Value))
                            used.Add(tag.Value);

            if (sort)
                used.Sort();
            
            return used;
        }
    }
}