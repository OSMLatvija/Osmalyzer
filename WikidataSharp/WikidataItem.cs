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
    public string? this[long propertyID] => _statements
                                            .Where(s => s.PropertyID == propertyID && !s.HasEndTime())
                                            .OrderByDescending(s => s.Rank)
                                            .FirstOrDefault()?.Value;


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
    public string? GetStatementValue(WikiDataProperty property, string? language = null) => GetStatementValue((long)property, language);

    [PublicAPI]
    public string? GetStatementValue(long propertyID, string? language = null)
    {
        return _statements
               .Where(s => s.PropertyID == propertyID && !s.HasEndTime() && (s.Language == null || s.Language == language))
               .OrderByDescending(s => s.Rank)
               .FirstOrDefault()?.Value;
    }
}