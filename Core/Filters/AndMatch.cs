using System.Linq;

namespace Osmalyzer;

public class AndMatch : OsmFilter
{
    public override bool ForNodesOnly => _filters.Any(f => f.ForNodesOnly);
    public override bool ForWaysOnly => _filters.Any(f => f.ForWaysOnly);
    public override bool ForRelationsOnly => _filters.Any(f => f.ForRelationsOnly);
    public override bool TaggedOnly => _filters.Any(f => f.TaggedOnly);
        
        
    private readonly OsmFilter[] _filters;

        
    public AndMatch(params OsmFilter[] filters)
    {
        _filters = filters;
    }

    internal override bool Matches(OsmElement element)
    {
        return _filters.All(f => f.Matches(element));
    }
}