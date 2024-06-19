using System;

namespace Osmalyzer;

public class HasValue : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => true;


    private readonly string _key;
    private readonly string _value;
    private readonly bool _caseSensitive;


    public HasValue(string key, string value, bool caseSensitive = true)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));
        
        _key = key;
        _value = value;
        _caseSensitive = caseSensitive;
    }


    internal override bool Matches(OsmElement element)
    {
        return element.HasValue(_key, _value, _caseSensitive);
    }
}