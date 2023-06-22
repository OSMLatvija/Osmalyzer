using System.Collections.Generic;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer
{
    public class OsmWay : OsmElement
    {
        [PublicAPI]
        public IReadOnlyList<OsmNode> Nodes => nodes.AsReadOnly();
        

        internal readonly List<OsmNode> nodes = new List<OsmNode>();


        internal OsmWay(OsmGeo RawElement)
            : base(RawElement)
        {
        }
    }
}