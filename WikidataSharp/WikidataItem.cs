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
    public string? this[long propertyID] => GetBestStatementValue(propertyID);


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
    public string? GetStatementValue(WikiDataProperty property) => GetBestStatementValue((long)property);

    [PublicAPI]
    public string? GetStatementValue(long propertyID) => GetBestStatementValue(propertyID);

    /// <summary>
    /// Gets the best statement value for a property, prioritizing by rank and filtering out statements with end time
    /// </summary>
    private string? GetBestStatementValue(long propertyID)
    {
        List<WikidataStatement> candidates = _statements.Where(s => s.PropertyID == propertyID).ToList();
        
        if (candidates.Count == 0)
            return null;

        // First, try to find a preferred rank statement without end time
        WikidataStatement? preferred = candidates
            .Where(s => s.Rank == WikidataRank.Preferred && !s.HasEndTime())
            .FirstOrDefault();
        
        if (preferred != null)
            return preferred.Value;

        // If no preferred without end time, try normal rank without end time
        WikidataStatement? normal = candidates
            .Where(s => s.Rank == WikidataRank.Normal && !s.HasEndTime())
            .FirstOrDefault();
        
        if (normal != null)
            return normal.Value;

        // Fall back to any preferred rank (even with end time)
        WikidataStatement? anyPreferred = candidates
            .Where(s => s.Rank == WikidataRank.Preferred)
            .FirstOrDefault();
        
        if (anyPreferred != null)
            return anyPreferred.Value;

        // Fall back to any normal rank
        WikidataStatement? anyNormal = candidates
            .Where(s => s.Rank == WikidataRank.Normal)
            .FirstOrDefault();
        
        if (anyNormal != null)
            return anyNormal.Value;

        // Last resort: return any statement
        return candidates.FirstOrDefault()?.Value;
    }
}