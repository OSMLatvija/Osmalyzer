namespace Osmalyzer;

public class IsNodeOrRelation : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => false;


    internal override bool Matches(OsmElement element)
    {
        return element is OsmNode or OsmRelation;
    }
}