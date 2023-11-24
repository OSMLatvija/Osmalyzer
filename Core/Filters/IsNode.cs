namespace Osmalyzer;

public class IsNode : OsmFilter
{
    public override bool ForNodesOnly => true;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => false;


    internal override bool Matches(OsmElement element)
    {
        return element is OsmNode;
    }
}