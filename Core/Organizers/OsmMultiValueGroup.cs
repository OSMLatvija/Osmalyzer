using System.Collections.Generic;
using JetBrains.Annotations;

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
            {
                if (element.Element.HasAnyTags)
                {
                    foreach (string elementKey in element.Element.AllKeys!)
                    {
                        if (elementKey == key)
                        {
                            string value = element.Element.GetValue(key)!;
                            if (!used.Contains(value))
                                used.Add(value);
                        }
                    }
                }
            }

            if (sort)
                used.Sort();
            
            return used;
        }
    }
}