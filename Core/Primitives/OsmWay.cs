﻿using System.Collections.Generic;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer
{
    public class OsmWay : OsmElement
    {
        public override OsmElementType ElementType => OsmElementType.Way;

        public override string OsmViewUrl => "https://www.openstreetmap.org/way/" + Id;

        [PublicAPI]
        public IReadOnlyList<OsmNode> Nodes => nodes.AsReadOnly();


        internal readonly List<OsmNode> nodes = new List<OsmNode>();
        
        internal readonly long[] nodeIds;


        internal OsmWay(OsmGeo rawElement)
            : base(rawElement)
        {
            nodeIds = ((Way)rawElement).Nodes;
        }

        
        public override OsmCoord GetAverageCoord()
        {
            return OsmGeoTools.GetAverageCoord(nodes);
        }
    }
}