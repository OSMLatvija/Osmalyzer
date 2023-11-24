using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Osmalyzer;

[PublicAPI]
public class OsmGroups
{
    public readonly List<OsmGroup> groups;


    public OsmGroups(List<OsmGroup> groups)
    {
        this.groups = groups;
    }


    public void SortGroupsByElementCountAsc()
    {
        groups.Sort((g1, g2) => g1.Elements.Count.CompareTo(g2.Elements.Count));
    }

    public void SortGroupsByElementCountDesc()
    {
        groups.Sort((g1, g2) => g2.Elements.Count.CompareTo(g1.Elements.Count));
    }

    public OsmMultiValueGroups CombineBySimilarValues(Func<string, string, bool> valueMatcher, bool sortValues)
    {
        List<OsmMultiValueGroup> multiGroups = new List<OsmMultiValueGroup>();

        bool[] processed = new bool[groups.Count];

        for (int g1 = 0; g1 < groups.Count; g1++)
        {
            if (!processed[g1]) // otherwise, already added to another earlier group
            {
                OsmGroup group1 = groups[g1];

                OsmMultiValueGroup multiGroup = new OsmMultiValueGroup();
                    
                // Add elements from group 1 - these always go in 
                    
                multiGroup.Values.Add((group1.Value, group1.Elements.Count));
                foreach (OsmElement element in group1.Elements)
                    multiGroup.Elements.Add(new OsmMultiValueElement(group1.Value, element));

                // Find any similar groups
                    
                for (int g2 = g1 + 1; g2 < groups.Count; g2++)
                {
                    if (!processed[g2]) // otherwise, already added to another earlier group
                    {
                        OsmGroup group2 = groups[g2];

                        if (valueMatcher(group1.Value, group2.Value))
                        {
                            // Add elements from group 2 since they matched
                    
                            multiGroup.Values.Add((group2.Value, group2.Elements.Count));
                            foreach (OsmElement element in group2.Elements)
                                multiGroup.Elements.Add(new OsmMultiValueElement(group2.Value, element));

                            processed[g2] = true;
                        }
                    }
                }

                processed[g1] = true; // although this is pointless since we never "go back"

                if (sortValues)
                    multiGroup.SortValues(); 
                    
                multiGroups.Add(multiGroup);
            }
        }

        return new OsmMultiValueGroups(multiGroups);
    }
}