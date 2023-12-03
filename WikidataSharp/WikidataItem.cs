using JetBrains.Annotations;

namespace WikidataSharp;

public class WikidataItem
{
    [PublicAPI]
    public long ID { get; }

    [PublicAPI]
    public IEnumerable<WikidataStatement> Statements => _statements;


    public string WikidataUrl => @"https://www.wikidata.org/entity/Q" + ID;
    

    [PublicAPI]
    public string? this[long propertyID] => _statements.FirstOrDefault(s => s.PropertyID == propertyID)?.Value ?? null;

    
    private readonly List<WikidataStatement> _statements;


    internal WikidataItem(long id, List<WikidataStatement> statements)
    {
        ID = id;
        _statements = statements;
    }
}