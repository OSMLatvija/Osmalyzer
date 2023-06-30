using System.Collections.Generic;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer
{
    public class OsmNode : OsmElement
    {
        public override OsmElementType ElementType => OsmElementType.Node;

        public override string OsmViewUrl => "https://www.openstreetmap.org/node/" + Id;

        [PublicAPI]
        public readonly double lat;
        [PublicAPI]
        public readonly double lon;
        // TODO: to coord struct and add all the useful methods and such
        
        [PublicAPI]
        public IReadOnlyList<OsmNode>? Ways => ways?.AsReadOnly();


        internal List<OsmNode>? ways;


        internal OsmNode(OsmGeo rawElement)
            : base(rawElement)
        {
            Node rawNode = (Node)rawElement;
            
            lat = rawNode.Latitude!.Value;
            lon = rawNode.Longitude!.Value;
        }
        
        
        public override (double lat, double lon) GetAverageCoord()
        {
            return (lat, lon);
        }
    }
}