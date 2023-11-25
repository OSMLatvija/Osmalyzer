using System;

namespace Osmalyzer;

public class CustomMatch : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => false;

        
    private readonly Func<OsmElement, bool> _check;


    public CustomMatch(Func<OsmElement, bool> check)
    {
        _check = check;
    }


    internal override bool Matches(OsmElement element)
    {
        return _check(element);
    }
}