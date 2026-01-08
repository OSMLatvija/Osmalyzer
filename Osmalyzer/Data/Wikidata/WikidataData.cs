using WikidataSharp;

namespace Osmalyzer;

public abstract class WikidataData : AnalysisData, IUndatedAnalysisData
{
    /// <summary>
    /// Assigns Wikidata items to data items by matching with a custom matcher function
    /// </summary>
    protected void AssignWikidataItems<T>(
        List<T> dataItems, 
        List<WikidataItem> wikidataItems,
        Func<T, WikidataItem, bool> matcher,
        out List<(T, List<WikidataItem>)> multiMatches)
        where T : class, IHasWikidataItem
    {
        multiMatches = [ ];
        
        int count = 0;
        
        foreach (T dataItem in dataItems)
        {
            List<WikidataItem> matches = wikidataItems.Where(wd => matcher(dataItem, wd)).ToList();
           
            if (matches.Count == 0)
                continue;
            
            if (matches.Count > 1)
            {
                multiMatches.Add((dataItem, matches));
                
                continue;
            }

            dataItem.WikidataItem = matches[0];
            count++;
        }
        
        if (count == 0) throw new Exception("No Wikidata items were matched, which is unexpected and likely means data or logic is broken.");
            
    }

    [Pure]
    protected static List<WikidataItem> FilterOutDissolved(List<WikidataItem> items)
    {
        return items
            .Where(item => !item.HasStatement(WikiDataProperty.DissolvedAbolishedOrDemolishedDate))
            .ToList();
    }
}