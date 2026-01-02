using WikidataSharp;

namespace Osmalyzer;

public abstract class AdminWikidataData : AnalysisData, IUndatedAnalysisData
{
    /// <summary>
    /// Assigns Wikidata items to data items by matching names
    /// </summary>
    protected void AssignWikidataItems<T>(
        List<T> dataItems, 
        List<WikidataItem> wikidataItems,
        Func<T, string> dataItemNameLookup, 
        Action<T, WikidataItem> dataItemAssigner)
    {
        foreach (T dataItem in dataItems)
        {
            string name = dataItemNameLookup(dataItem);

            List<WikidataItem> matches = wikidataItems.Where(i => (
                                                       i.GetStatementValue(WikiDataProperty.OfficialName) ?? // prefer specific official name property
                                                       i.GetStatementValue(WikiDataProperty.Name) ?? // accept specific general name property
                                                       i.GetLabel("lv") ?? // if preferred properties are missing, use Latvian label
                                                       i.GetLabel("mul") // fallback to multilingual label
                                                   ) == name
            ).ToList();
            
            // todo: resolve same name stuff

            if (matches.Count > 1) throw new NotImplementedException();
            
            if (matches.Count == 1)
                dataItemAssigner(dataItem, matches[0]);
        }
    }
}