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
        double coordMismatchDistance,
        out List<WikidataMatchIssue> issues)
        where T : class, IDataItem, IHasWikidataItem
    {
        issues = [ ];
        
        int count = 0;
        
        foreach (T dataItem in dataItems)
        {
            List<WikidataItem> matches = wikidataItems.Where(wd => matcher(dataItem, wd)).ToList();
           
            if (matches.Count == 0)
                continue;
            
            if (matches.Count > 1)
            {
                issues.Add(new MultipleWikidataMatchesWikidataMatchIssue<T>(dataItem, matches));
                
                continue;
            }

            WikidataCoord? coord = matches[0].GetBestStatementValueAsCoordinate(WikiDataProperty.CoordinateLocation);

            if (coord != null)
            {
                OsmCoord osmCoord = new OsmCoord(coord.Value.Latitude, coord.Value.Longitude);

                double distance = OsmGeoTools.DistanceBetweenCheap(dataItem.Coord, osmCoord);
                
                if (distance > coordMismatchDistance)
                {
                    issues.Add(new CoordinateMismatchWikidataMatchIssue<T>(dataItem, matches[0], distance));
                    continue;
                }
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
            .Where(item => !item.HasActiveStatement(WikiDataProperty.DissolvedAbolishedOrDemolishedDate))
            .ToList();
    }


    public abstract record WikidataMatchIssue;

    public record MultipleWikidataMatchesWikidataMatchIssue<T>(T DataItem, List<WikidataItem> WikidataItems) : WikidataMatchIssue;
    
    public record CoordinateMismatchWikidataMatchIssue<T>(T DataItem, WikidataItem WikidataItem, double DistanceMeters) : WikidataMatchIssue;
}