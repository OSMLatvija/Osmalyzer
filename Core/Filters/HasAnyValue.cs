using System.Linq;

namespace Osmalyzer;

public class HasAnyValue : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => true;


    private readonly string _key;
    private readonly string[] _values;
    private readonly bool _caseSensitive;


    public HasAnyValue(string key, params string[] values)
    {
        _key = key;
        _values = values;
        _caseSensitive = false;
    }

    public HasAnyValue(string key, IEnumerable<string> values, bool caseSensitive = true)
    {
        _key = key;
        _values = values.ToArray();
        _caseSensitive = caseSensitive;
    }


    internal override bool Matches(OsmElement element)
    {
        return 
            element.HasAnyTags &&
            _values.Any(v => element.HasValue(_key, v, _caseSensitive));
    }
}