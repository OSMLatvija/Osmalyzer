using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

public class HasAnyKey : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => true;


    private readonly List<string> _tags;


    public HasAnyKey(List<string> tags)
    {
        _tags = tags;
    }


    internal override bool Matches(OsmElement element)
    {
        return _tags.Any(element.HasKey);
    }
}