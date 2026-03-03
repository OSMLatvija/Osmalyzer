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
            List<SuggestedAction> actionsForThisNode = [ ];

            OsmCreateNodeAction createNodeAction = new OsmCreateNodeAction(item.Coord);
            long id = createNodeAction.Id;
            actionsForThisNode.Add(createNodeAction);

            foreach (ValidationRule rule in rules)
                ApplyRuleAsNewNodeTags(rule, item, id, actionsForThisNode);

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


        void ApplyRuleAsNewNodeTags(ValidationRule rule, T dataItem, long newNodeId, List<SuggestedAction> actions)
        {
            switch (rule)
            {
                case ValidateElementFixme:
                case ValidateElementHasKey:
                case ValidateElementHasAcceptableValue:
                case ValidateElementDoesntHaveTag:
                    break; // no specific value to suggest for new node

                case ValidateElementHasValue elementHasValue:
                {
                    // Skip rules with ElementSelector - the new node has no sub-elements
                    if (elementHasValue.ElementSelector != null)
                        break;

                    if (string.IsNullOrEmpty(elementHasValue.Value))
                        break; // unknown or expected-absent value - nothing to add

                    actions.Add(new OsmSetValueSuggestedAction(OsmElement.OsmElementType.Node, newNodeId, elementHasValue.Tag, elementHasValue.Value));
                    break;
                }

                case ValidateElementHasAnyValue elementHasAnyValue:
                {
                    if (elementHasAnyValue.Values.Length == 0)
                        break;

                    actions.Add(new OsmSetValueSuggestedAction(OsmElement.OsmElementType.Node, newNodeId, elementHasAnyValue.Tag, elementHasAnyValue.Values[0]));
                    break;
                }

                case ValidateElementValueMatchesDataItemValue<T> elementValueMatchesDataItemValue:
                {
                    // Skip rules with ElementSelector - the new node has no sub-elements
                    if (elementValueMatchesDataItemValue.ElementSelector != null)
                        break;

                    string? dataValue = elementValueMatchesDataItemValue.DataItemValueLookup(dataItem);

                    if (string.IsNullOrEmpty(dataValue))
                        break; // unknown or expected-absent value - nothing to add

                    actions.Add(new OsmSetValueSuggestedAction(OsmElement.OsmElementType.Node, newNodeId, elementValueMatchesDataItemValue.Tag, dataValue));
                    break;
                }

                case ValidateElementTagSuffixesMatchDataItemValues<T> elementTagSuffixesMatchDataItemValues:
                {
                    List<string>? dataValues = elementTagSuffixesMatchDataItemValues.DataItemValuesLookup(dataItem);

                    if (dataValues == null)
                        break;

                    string tagPrefix = elementTagSuffixesMatchDataItemValues.TagPrefix + ":";

                    foreach (string suffix in dataValues)
                        actions.Add(new OsmSetValueSuggestedAction(OsmElement.OsmElementType.Node, newNodeId, tagPrefix + suffix, elementTagSuffixesMatchDataItemValues.ExpectedValue));

                    break;
                }

                default:
                    throw new NotImplementedException();
            }
        }
    }    
    
    
    private enum ReportGroup
    {
        SuggestedAdditions = -4 // after correlator, validator and probably before analyzer extra issues
    }
}