using System.Collections.Generic;
using System.Linq;
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

        
        public (double lat, double lon) GetAverageNodeCoord()
        {
            double lat = nodes.Select(n => n.Lat).Average();
            double lon = nodes.Select(n => n.Lon).Average();
            return (lat, lon);
        }
    }
}