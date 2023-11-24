namespace Osmalyzer;

public class DoesntHaveValue : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => true;


    private readonly string _key;
    private readonly string _value;
    private readonly bool _caseSensitive;


    public DoesntHaveValue(string key, string value, bool caseSensitive = true)
    {
        _key = key;
        _value = value;
        _caseSensitive = caseSensitive;
    }


    internal override bool Matches(OsmElement element)
    {
        return !element.HasValue(_key, _value, _caseSensitive);
    }
}