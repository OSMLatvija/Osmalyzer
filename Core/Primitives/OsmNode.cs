using System.Collections.Generic;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer
{
    public class OsmNode : OsmElement
    {
        public override OsmElementType ElementType => OsmElementType.Node;

        [PublicAPI]
        public double Lat => ((Node)RawElement).Latitude!.Value;
        [PublicAPI]
        public double Lon => ((Node)RawElement).Longitude!.Value;
        // TODO: to coord struct and add allt he useful methods and such
        
        [PublicAPI]
        public IReadOnlyList<OsmNode>? Ways => ways?.AsReadOnly();


        internal List<OsmNode>? ways;


        internal OsmNode(OsmGeo RawElement)
            : base(RawElement)
        {
        }
    }
}