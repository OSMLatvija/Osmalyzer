using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[PublicAPI]
public class OsmMultiValueGroup
{
    /// <summary> All the unique values </summary>
    public List<(string value, int count)> Values { get; private set; } = new List<(string value, int count)>();

    public List<OsmMultiValueElement> Elements { get; } = new List<OsmMultiValueElement>();

    /// <summary> Element counts for each unique value </summary>
    public List<int> ElementCounts { get; } = new List<int>();


    [Pure]
    public List<(string v, int c)> GetUniqueValuesForKey(string key, bool sort = false)
    {
        Dictionary<string, int> used = new Dictionary<string, int>();

        foreach (OsmMultiValueElement element in Elements)
        {
            if (element.Element.HasAnyTags)
            {
                foreach (string elementKey in element.Element.AllKeys!)
                {
                    if (elementKey == key)
                    {
                        string value = element.Element.GetValue(key)!;

                        if (!used.ContainsKey(value))
                            used.Add(value, 1);
                        else
                            used[value]++;
                    }
                }
            }
        }

        if (sort)
            return used.Select(u => (u.Key, u.Value)).OrderByDescending(l => l.Item2).ToList();
        else
            return used.Select(u => (u.Key, u.Value)).ToList();
    }

    public void SortValues()
    {
        Values = Values.OrderByDescending(l => l.Item2).ToList();
    }

    [Pure]
    public List<OsmElement> GetElementsWithValue(string value)
    {
        return Elements.Where(e => e.Value == value).Select(e => e.Element).ToList();
    }
}