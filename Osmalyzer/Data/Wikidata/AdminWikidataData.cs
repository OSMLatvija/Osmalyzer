using WikidataSharp;

namespace Osmalyzer;

public abstract class AdminWikidataData : AnalysisData, IUndatedAnalysisData
{
    /// <summary>
    /// Gets the name to use for matching from a <see cref="WikidataItem"/>
    /// </summary>
    /// <remarks>
    /// Prefers official name property, then general name property, then Latvian label, then multilingual label
    /// </remarks>
    public static string? GetBestName(WikidataItem item, string language)
    {
        // todo: cache?
        
        return item.GetStatementValue(WikiDataProperty.OfficialName, language) ?? // prefer specific official name property
               item.GetStatementValue(WikiDataProperty.Name, language) ?? // accept specific general name property
               item.GetLabel(language) ?? // if preferred properties are missing, use Latvian label
               item.GetLabel("mul"); // fallback to multilingual label
    }


    /// <summary>
    /// Assigns Wikidata items to data items by matching with a custom matcher function
    /// </summary>
    protected void AssignWikidataItems<T>(
        List<T> dataItems, 
        List<WikidataItem> wikidataItems,
        Func<T, WikidataItem, bool> matcher)
        where T : class, IHasWikidataItem
    {
        foreach (T dataItem in dataItems)
        {
            List<WikidataItem> matches = wikidataItems.Where(wd => matcher(dataItem, wd)).ToList();
            
            // todo: resolve same name stuff

            if (matches.Count > 1) throw new NotImplementedException();
            
            if (matches.Count == 1)
                dataItem.WikidataItem = matches[0];
        }
    }
}