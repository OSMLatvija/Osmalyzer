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
    public string WikidataUrl => @"https://www.wikidata.org/entity/" + QID;
    

    [PublicAPI]
    public string? this[long propertyID] => _statements.FirstOrDefault(s => s.PropertyID == propertyID)?.Value ?? null;


    private readonly Dictionary<string, string> _labels;
    private readonly List<WikidataStatement> _statements;


    internal WikidataItem(long id, Dictionary<string, string> labels, List<WikidataStatement> statements)
    {
        ID = id;
        _labels = labels;
        _statements = statements;
    }


    [PublicAPI]
    public string? GetLabel(string languageId)
    {
        _labels.TryGetValue(languageId, out string? label);
        return label;
    }

    [PublicAPI]
    public string? GetDefault() => GetLabel("mul"); // mul = multilingual


    [PublicAPI]
    public string? GetStatementValue(WikiDataProperty property) => GetStatementValue((long)property);

    [PublicAPI]
    public string? GetStatementValue(long propertyID)
    {
        WikidataStatement? statement = _statements.FirstOrDefault(s => s.PropertyID == propertyID);
        return statement?.Value ?? null;
    }
}