namespace Osmalyzer
{
    public class IsRelation : OsmFilter
    {
        public override bool ForNodesOnly => false;
        public override bool ForWaysOnly => false;
        public override bool ForRelationsOnly => true;


        internal override bool Matches(OsmElement element)
        {
            return element is OsmRelation;
        }
    }
}