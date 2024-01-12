using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

public class HasAnyKey : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => true;


    private readonly string[] _tags;


    public HasAnyKey(IEnumerable<string> tags) : this(tags.ToArray()) { }
        
    public HasAnyKey(params string[] tags)
    {
        _tags = tags;
    }


    internal override bool Matches(OsmElement element)
    {
        if (_tags.Length == 0)
            if (element.HasAnyTags)
                return true;
        
        return _tags.Any(element.HasKey);
    }
}