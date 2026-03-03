namespace Osmalyzer;

public class Spawner<T> where T : IDataItem
{
    private readonly CorrelatorReport _correlatorReport;
    
    private readonly string? _customTitle;


    public Spawner(CorrelatorReport correlatorReport, string? customTitle = null)
    {
        _customTitle = customTitle;
        _correlatorReport = correlatorReport;
    }

    
    public Spawn Spawn(Report report, List<ValidationRule> rules)
    {
        report.AddGroup(
            ReportGroup.SuggestedAdditions,
            _customTitle ?? "Suggested additions",
                "These data items are not currently matched to OSM elements and can be added with these (suggested) tags.",
            "No unmatched data items are available for addition."
        );
        
        List<T> unmatchedItems = _correlatorReport.Correlations
                                                  .OfType<UnmatchedItemCorrelation<T>>()
                                                  .Select(c => c.DataItem)
                                                  .ToList();

        List<SuggestedAction> suggestedAdditions = [ ];
        
        foreach (T item in unmatchedItems)
        {
            // TODO: OsmNode newOsmNode = additionsData.CreateNewNode(item.Coord);

            List<SuggestedAction> actionsForThisNode = [ ];

            // TODO: actionsForThisNode.Add(new OsmCreateElementAction(newOsmNode));
            
            // TODO: BASED ON RULES
            

            suggestedAdditions.AddRange(actionsForThisNode);

            report.AddEntry(
                ReportGroup.SuggestedAdditions,
                new IssueReportEntry(
                    "Data item " + item.ReportString() + " can be added at " +
                    item.Coord.OsmUrl +
                    " as" + Environment.NewLine + SuggestedActionApplicator.GetTagsForSuggestedActionsAsCodeString(actionsForThisNode),
                    item.Coord,
                    MapPointStyle.Suggestion
                )
            );
        }
        
        return new Spawn(suggestedAdditions);
    }    
    
    
    private enum ReportGroup
    {
        SuggestedAdditions = -4 // after correlator, validator and probably before analyzer extra issues
    }
}