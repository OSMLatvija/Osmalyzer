using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer
{
    public class OsmWay : OsmElement
    {
        public override OsmElementType ElementType => OsmElementType.Way;

        [PublicAPI]
        public IReadOnlyList<OsmNode> Nodes => nodes.AsReadOnly();


        internal readonly List<OsmNode> nodes = new List<OsmNode>();
        
        internal readonly long[] nodeIds;


        internal OsmWay(OsmGeo rawElement)
            : base(rawElement)
        {
            nodeIds = ((Way)rawElement).Nodes;
        }

        
        public (double lat, double lon) GetAverageNodeCoord()
        {
            double lat = nodes.Select(n => n.lat).Average();
            double lon = nodes.Select(n => n.lon).Average();
            return (lat, lon);
        }
    }
}