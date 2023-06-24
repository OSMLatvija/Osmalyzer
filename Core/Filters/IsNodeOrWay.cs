﻿namespace Osmalyzer
{
    public class IsNodeOrWay : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => false;
        public override bool ForRelationsOnly => false;


        internal override bool Matches(OsmElement element)
        {
            return element is OsmNode or OsmWay;
        }
    }
}