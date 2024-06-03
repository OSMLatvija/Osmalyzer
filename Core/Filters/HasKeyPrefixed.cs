namespace Osmalyzer;

public class HasKeyPrefixed : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => true;


    private readonly string _keyPrefix;


    public HasKeyPrefixed(string keyPrefix)
    {
        _keyPrefix = keyPrefix;
    }


    internal override bool Matches(OsmElement element)
    {
        return element.HasKeyPrefixed(_keyPrefix);
    }
}