using JetBrains.Annotations;

namespace WikidataSharp;

public class WikidataItem
{
    [PublicAPI]
    public long ID { get; }

    [PublicAPI]
    public IReadOnlyDictionary<string, string> Labels => _labels;

    [PublicAPI]
    public IReadOnlyList<WikidataStatement> Statements => _statements;


    [PublicAPI]
    public string QID => "Q" + ID;
    
    [PublicAPI]
    public string WikidataUrl => @"http://www.wikidata.org/entity/" + QID;
    

    [PublicAPI]
    public string? this[long propertyID] => GetBestStatementStringValue(propertyID);
    
    [PublicAPI]
    public string? this[WikiDataProperty property] => GetBestStatementStringValue(property);


    private readonly Dictionary<string, string> _labels;
    private readonly List<WikidataStatement> _statements;

    private Dictionary<long, WikidataStatement?>? _bestStatements;

    private string? _bestName;
    private string? _bestNameLanguage;
    

    internal WikidataItem(long id, Dictionary<string, string> labels, List<WikidataStatement> statements)
    {
        ID = id;
        _labels = labels;
        _statements = statements;
    }


    [PublicAPI]
    [Pure]
    public string? GetLabel(string languageId)
    {
        _labels.TryGetValue(languageId, out string? label);
        return label;
    }

    [PublicAPI]
    [Pure]
    public string? GetDefault() => GetLabel("mul"); // mul = multilingual


    [PublicAPI]
    [Pure]
    public string? GetBestStatementStringValue(WikiDataProperty property, string? language = null) => GetBestStatementStringValue((long)property, language);

    [PublicAPI]
    [Pure]
    public string? GetBestStatementStringValue(long propertyID, string? language = null)
    {
        return _statements
               .Where(s => s.PropertyID == propertyID && s.Type == WikidataValueType.Literal && !s.HasEndTime() && (s.Language == null || s.Language == language))
               .OrderByDescending(s => s.Rank)
               .FirstOrDefault()?.AsString;
    }
    
    [PublicAPI]
    [Pure]
    public long? GetBestStatementValueAsQID(WikiDataProperty property) => GetBestStatementValueAsQID((long)property);

    [PublicAPI]
    [Pure]
    public long? GetBestStatementValueAsQID(long propertyID) => GetBestStatement(propertyID)?.AsQID;

    /// <summary>
    ///
    /// "Best" means active statement of the highest rank.
    /// </summary>
    [PublicAPI]
    [Pure]
    public WikidataStatement? GetBestStatement(long propertyID)
    {
        if (_bestStatements != null)
            if (_bestStatements.TryGetValue(propertyID, out WikidataStatement? cachedStatement))
                return cachedStatement;

        WikidataStatement? bestStatement = _statements
                                            .Where(s => s.PropertyID == propertyID && !s.HasEndTime())
                                            .OrderByDescending(s => s.Rank)
                                            .FirstOrDefault();

        _bestStatements ??= [ ];
        _bestStatements[propertyID] = bestStatement;
        return bestStatement;
    }

    [PublicAPI]
    [Pure]
    public decimal? GetBestStatementValueAsDecimal(WikiDataProperty property) => GetBestStatementValueAsDecimal((long)property);

    [PublicAPI]
    [Pure]
    public decimal? GetBestStatementValueAsDecimal(long propertyID) => GetBestStatement(propertyID)?.AsDecimal;

    [PublicAPI]
    [Pure]
    public DateTime? GetBestStatementValueAsDateTime(WikiDataProperty property) => GetBestStatementValueAsDateTime((long)property);

    [PublicAPI]
    [Pure]
    public DateTime? GetBestStatementValueAsDateTime(long propertyID) => GetBestStatement(propertyID)?.AsDateTime;

    [PublicAPI]
    [Pure]
    public WikidataCoord? GetBestStatementValueAsCoordinate(WikiDataProperty property) => GetBestStatementValueAsCoordinate((long)property);

    [PublicAPI]
    [Pure]
    public WikidataCoord? GetBestStatementValueAsCoordinate(long propertyID) => GetBestStatement(propertyID)?.AsCoordinate;


    [PublicAPI]
    [Pure]
    public bool HasActiveStatementValueAsQID(WikiDataProperty instanceOf, long smallVillageInLatviaQID) => HasActiveStatementValueAsQID((long)instanceOf, smallVillageInLatviaQID);
    
    [PublicAPI]
    [Pure]
    public bool HasActiveStatementValueAsQID(long propertyID, long qid)
    {
        return _statements.Any(s => s.PropertyID == propertyID && s.AsQID == qid && !s.HasEndTime());
    }

    [PublicAPI]
    [Pure]
    public bool HasActiveStatement(WikiDataProperty property) => HasActiveStatement((long)property);

    [PublicAPI]
    [Pure]
    public bool HasActiveStatement(long propertyID)
    {
        return _statements.Any(s => s.PropertyID == propertyID && !s.HasEndTime());
    }
    
    /// <summary>
    /// Gets the name to use for matching from a <see cref="WikidataItem"/>
    /// </summary>
    /// <remarks>
    /// Prefers official name property, then general name property, then Latvian label, then multilingual label
    /// </remarks>
    public string? GetBestName(string language)
    {
        if (_bestNameLanguage != null)
            return _bestName; // could be null

        string? value = GetBestStatementStringValue(WikiDataProperty.OfficialName, language) ?? // prefer specific official name property
                        GetBestStatementStringValue(WikiDataProperty.Name, language) ?? // accept specific general name property
                        GetLabel(language) ?? // if preferred properties are missing, use Latvian label
                        GetLabel("mul"); // fallback to multilingual label

        _bestNameLanguage = language;
        _bestName = value;
        return value;
    }
}