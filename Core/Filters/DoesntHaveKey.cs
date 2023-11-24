namespace Osmalyzer;

public class DoesntHaveKey : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => false;


    private readonly string _key;


    public DoesntHaveKey(string key)
    {
        _key = key;
    }


    internal override bool Matches(OsmElement element)
    {
        return !element.HasKey(_key);
    }
}