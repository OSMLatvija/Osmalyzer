namespace Osmalyzer;

public class IsWayOrRelation : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => false;


    internal override bool Matches(OsmElement element)
    {
        return element is OsmWay or OsmRelation;
    }
}