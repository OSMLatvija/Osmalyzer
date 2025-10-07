using System;

namespace Osmalyzer;

public class SplitValuesCheck : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => false;
    public override bool TaggedOnly => true;


    private readonly string _tag;
    private readonly Func<string, bool> _check;


    public SplitValuesCheck(string tag, Func<string, bool> check)
    {
        _tag = tag;
        _check = check;
    }


    internal override bool Matches(OsmElement element)
    {
        if (!element.HasAnyTags)
            return false;

        string? rawValue = element.GetValue(_tag);

        if (rawValue == null)
            return false;

        List<string> splitValues = TagUtils.SplitValue(rawValue);

        if (splitValues.Count == 0)
            return false;

        foreach (string splitValue in splitValues)
            if (!_check(splitValue))
                return false;

        return true;
    }
}