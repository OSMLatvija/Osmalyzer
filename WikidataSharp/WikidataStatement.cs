using System.Globalization;
using JetBrains.Annotations;

namespace WikidataSharp;

public class WikidataStatement
{
    private const string entityQUri = "http://www.wikidata.org/entity/Q";
    private const string entityPUri = "http://www.wikidata.org/entity/P";

    
    [PublicAPI]
    public long PropertyID { get; }
    
    [PublicAPI]
    public string RawValue { get; }

    [PublicAPI]
    public WikidataValueType Type { get; }

    [PublicAPI]
    public WikidataDataType? DataType { get; }

    [PublicAPI]
    public WikidataUriType? UriType { get; }

    [PublicAPI]
    public WikidataRank Rank { get; }

    [PublicAPI]
    public string? Language { get; }

    [PublicAPI]
    public IReadOnlyDictionary<long, string> Qualifiers => _qualifiers;
    
    [PublicAPI]
    public string? AsString => Type == WikidataValueType.Literal ? RawValue : null;

    [PublicAPI]
    public long? AsQID => Type == WikidataValueType.Uri && UriType == WikidataUriType.Entity ? long.Parse(RawValue[entityQUri.Length..]) : null;
    
    [PublicAPI]
    public long? AsPID => Type == WikidataValueType.Uri && UriType == WikidataUriType.Property ? long.Parse(RawValue[entityPUri.Length..]) : null;

    [PublicAPI]
    public decimal? AsDecimal => DataType == WikidataDataType.Decimal ? decimal.Parse(RawValue, CultureInfo.InvariantCulture) : null;

    [PublicAPI]
    public DateTime? AsDateTime => DataType == WikidataDataType.DateTime ? DateTime.Parse(RawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal) : null;
    

    private readonly Dictionary<long, string> _qualifiers;


    public WikidataStatement(long propertyID, string value, WikidataValueType type, string? dataTypeRaw, WikidataRank rank, string? language, Dictionary<long, string> qualifiers)
    {
        PropertyID = propertyID;
        RawValue = value;
        Type = type;
        Rank = rank;
        Language = language;
        _qualifiers = qualifiers;
        
        // Determine DataType from the raw datatype string
        if (dataTypeRaw != null)
        {
            if (dataTypeRaw == "http://www.w3.org/2001/XMLSchema#decimal")
                DataType = WikidataDataType.Decimal;
            else if (dataTypeRaw == "http://www.w3.org/2001/XMLSchema#dateTime")
                DataType = WikidataDataType.DateTime;
        }
        
        // Determine UriType from the URI value
        if (type == WikidataValueType.Uri)
        {
            if (value.StartsWith(entityQUri))
                UriType = WikidataUriType.Entity;
            else if (value.StartsWith(entityPUri))
                UriType = WikidataUriType.Property;
        }
    }


    [PublicAPI]
    public string? GetQualifier(long qualifierPropertyID)
    {
        _qualifiers.TryGetValue(qualifierPropertyID, out string? value);
        return value;
    }

    [PublicAPI]
    public bool HasEndTime() => _qualifiers.ContainsKey(582); // P582 = end time // todo: dehardcode


    public override string ToString()
    {
        return $"P{PropertyID} [{Type}" + (DataType.HasValue ? $":{DataType.Value}" : "") + $"] = {RawValue} ({Rank}" + (Language != null ? ", " + Language : "") + $")";
    }
}


/// <summary>
/// The "type" field from SPARQL JSON results
/// </summary>
public enum WikidataValueType
{
    Literal,
    Uri
}


/// <summary>
/// The "datatype" sub-field for <see cref="WikidataValueType.Literal"/>s
/// </summary>
public enum WikidataDataType
{
    Decimal,
    DateTime
}


public enum WikidataRank
{
    Deprecated = 0,
    Normal = 1,
    Preferred = 2
}


/// <summary>
/// The specific type of Wikidata URI
/// </summary>
public enum WikidataUriType
{
    Entity, // Q items
    Property // P items
}


