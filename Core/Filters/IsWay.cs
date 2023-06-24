namespace Osmalyzer
{
    public class IsWay : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => true;
        public override bool ForRelationsOnly => false;


        internal override bool Matches(OsmElement element)
        {
            return element is OsmWay;
        }
    }
}