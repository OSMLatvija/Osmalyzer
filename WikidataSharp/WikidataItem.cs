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
    public string? this[long propertyID] => GetStatementValue(propertyID);
    
    [PublicAPI]
    public string? this[WikiDataProperty property] => GetStatementValue(property);


    private readonly Dictionary<string, string> _labels;
    private readonly List<WikidataStatement> _statements;


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
    public string? GetStatementValue(WikiDataProperty property, string? language = null) => GetStatementValue((long)property, language);

    [PublicAPI]
    [Pure]
    public string? GetStatementValue(long propertyID, string? language = null)
    {
        return _statements
               .Where(s => s.PropertyID == propertyID && !s.HasEndTime() && (s.Language == null || s.Language == language))
               .OrderByDescending(s => s.Rank)
               .FirstOrDefault()?.Value;
    }
    
    [PublicAPI]
    [Pure]
    public long? GetStatementValueAsQID(WikiDataProperty property) => GetStatementValueAsQID((long)property);

    [PublicAPI]
    [Pure]
    public long? GetStatementValueAsQID(long propertyID) => GetStatement(propertyID)?.AsQID;

    [PublicAPI]
    [Pure]
    public WikidataStatement? GetStatement(long propertyID)
    {
        return _statements
            .Where(s => s.PropertyID == propertyID && !s.HasEndTime())
            .OrderByDescending(s => s.Rank)
            .FirstOrDefault();
    }

    [PublicAPI]
    [Pure]
    public bool HasStatementValueAsQID(WikiDataProperty instanceOf, long smallVillageInLatviaQID) => HasStatementValueAsQID((long)instanceOf, smallVillageInLatviaQID);
    
    [PublicAPI]
    [Pure]
    public bool HasStatementValueAsQID(long propertyID, long qid)
    {
        return _statements.Any(s => s.PropertyID == propertyID && s.AsQID == qid && !s.HasEndTime());
    }

    // todo: other typed versions

    [PublicAPI]
    [Pure]
    public bool HasStatement(long propertyID)
    {
        return _statements.Any(s => s.PropertyID == propertyID && !s.HasEndTime());
    }
}