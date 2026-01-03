using JetBrains.Annotations;

namespace WikidataSharp;

public class WikidataStatement
{
    private const string entityQUri = "http://www.wikidata.org/entity/Q";
    private const string entityPUri = "http://www.wikidata.org/entity/P";

    
    [PublicAPI]
    public long PropertyID { get; }
    
    [PublicAPI]
    public string Value { get; }

    [PublicAPI]
    public string DataType { get; }

    [PublicAPI]
    public WikidataRank Rank { get; }

    [PublicAPI]
    public string? Language { get; }

    [PublicAPI]
    public IReadOnlyDictionary<long, string> Qualifiers => _qualifiers;

    [PublicAPI]
    public long? AsQID => DataType == "uri" && Value.StartsWith(entityQUri) ? long.Parse(Value[entityQUri.Length..]) : null;
    
    [PublicAPI]
    public long? AsPID => DataType == "uri" && Value.StartsWith(entityPUri) ? long.Parse(Value[entityPUri.Length..]) : null;
    

    private readonly Dictionary<long, string> _qualifiers;


    public WikidataStatement(long propertyID, string value, string dataType, WikidataRank rank, string? language, Dictionary<long, string> qualifiers)
    {
        PropertyID = propertyID;
        Value = value;
        DataType = dataType;
        Rank = rank;
        Language = language;
        _qualifiers = qualifiers;
    }


    [PublicAPI]
    public string? GetQualifier(long qualifierPropertyID)
    {
        _qualifiers.TryGetValue(qualifierPropertyID, out string? value);
        return value;
    }

    [PublicAPI]
    public bool HasEndTime() => _qualifiers.ContainsKey(582); // P582 = end time // todo: deharcode


    public override string ToString()
    {
        return $"P{PropertyID} [{DataType}] = {Value} ({Rank}" + (Language != null ? ", " + Language : "") + $")";
    }
}

public enum WikidataRank
{
    Deprecated = 0,
    Normal = 1,
    Preferred = 2
}
