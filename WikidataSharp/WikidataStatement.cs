using JetBrains.Annotations;

namespace WikidataSharp;

public class WikidataStatement
{
    [PublicAPI]
    public long PropertyID { get; }
    
    [PublicAPI]
    public string Value { get; }

    [PublicAPI]
    public string DataType { get; }

    [PublicAPI]
    public WikidataRank Rank { get; }

    [PublicAPI]
    public IReadOnlyDictionary<long, string> Qualifiers => _qualifiers;


    private readonly Dictionary<long, string> _qualifiers;

    
    public WikidataStatement(long propertyID, string value, string dataType, WikidataRank rank, Dictionary<long, string> qualifiers)
    {
        PropertyID = propertyID;
        Value = value;
        DataType = dataType;
        Rank = rank;
        _qualifiers = qualifiers;
    }


    [PublicAPI]
    public string? GetQualifier(long qualifierPropertyID)
    {
        _qualifiers.TryGetValue(qualifierPropertyID, out string? value);
        return value;
    }

    [PublicAPI]
    public bool HasEndTime() => _qualifiers.ContainsKey(582); // P582 = end time


    public override string ToString()
    {
        return $"P{PropertyID} [{DataType}] = {Value} ({Rank})";
    }
}

public enum WikidataRank
{
    Deprecated = 0,
    Normal = 1,
    Preferred = 2
}
