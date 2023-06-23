using System.Collections.Generic;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [PublicAPI]
    public class OsmMultiValueGroups
    {
        public readonly List<OsmMultiValueGroup> groups;


        public OsmMultiValueGroups(List<OsmMultiValueGroup> groups)
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
    }
}