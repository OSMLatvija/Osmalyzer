namespace Osmalyzer;

public class Validator<T> where T : IDataItem
{
    private readonly CorrelatorReport _correlatorReport;
    
    private readonly string? _customTitle;


    public Validator(CorrelatorReport correlatorReport, string? customTitle = null)
    {
        _customTitle = customTitle;
        _correlatorReport = correlatorReport;
    }


    public List<SuggestedAction> Validate(Report report, bool validateUnmatchedElements, params ValidationRule[] rules)
    {
        report.AddGroup(
            ReportGroup.ValidationResults, 
            _customTitle ?? "Other issues",
            "These OSM elements and/or data items have additional individual (known) issues.",
            "No (known) issues found with matched/found OSM elements and/or data items."
        );

        List<SuggestedAction> suggestedChanges = [ ];
        
        foreach (Correlation match in _correlatorReport.Correlations)
        {
            OsmElement osmElement;
            T? dataItem;

            switch (match)
            {
                case MatchedCorrelation<T> matchedCorrelation:
                    osmElement = matchedCorrelation.OsmElement;
                    dataItem = matchedCorrelation.DataItem;
                    break;

                case LoneOsmCorrelation loneCorrelation:
                    osmElement = loneCorrelation.OsmElement;
                    dataItem = default;
                    break;
                
                case UnmatchedOsmCorrelation unmatchedCorrelation:
                    if (!validateUnmatchedElements)
                        continue;
                    
                    osmElement = unmatchedCorrelation.OsmElement;
                    dataItem = default;
                    break;
                
                case UnmatchedItemCorrelation<T>:
                    // No OSM element to validate, just a data item
                    continue;

                default:
                    throw new ArgumentOutOfRangeException(nameof(match));
            }


            // We may not have a data item matches, so only print label if there is one
            string itemLabel = dataItem != null ? " for " + dataItem.ReportString() : "";
            
            
            List<SuggestedAction>? suggestedChangesForElement = null;

            foreach (ValidationRule rule in rules)
            {
                switch (rule)
                {
                    case ValidateElementFixme:
                        CheckElementFixme();
                        break;

                    case ValidateElementHasValue elementHasValue:
                    {
                        List<SuggestedAction>? suggestedChangesForRule = CheckElementHasValue(elementHasValue);
                        if (suggestedChangesForRule != null)
                        {
                            suggestedChangesForElement ??= [ ];
                            suggestedChangesForElement.AddRange(suggestedChangesForRule);
                        }
                        break;
                    }

                    case ValidateElementHasAnyValue elementHasAnyValue:
                    {
                        List<SuggestedAction>? suggestedChangesForRule = CheckElementHasAnyValue(elementHasAnyValue);
                        if (suggestedChangesForRule != null)
                        {
                            suggestedChangesForElement ??= [ ];
                            suggestedChangesForElement.AddRange(suggestedChangesForRule);
                        }
                        break;
                    }

                    case ValidateElementHasKey elementHasKey:
                        CheckElementHasKey(elementHasKey);
                        break;
                    
                    case ValidateElementDoesntHaveTag elementDoesntHaveValue:
                        CheckElementDoesntHaveValue(elementDoesntHaveValue);
                        break;
                    
                    case ValidateElementHasAcceptableValue elementHasAcceptableValue:
                        CheckElementHasAcceptableValue(elementHasAcceptableValue);
                        break;

                    case ValidateElementValueMatchesDataItemValue<T> elementValueMatchesDataItemValue:
                    {
                        List<SuggestedAction>? suggestedChangesForRule = CheckElementValueMatchesDataItemValue(elementValueMatchesDataItemValue);
                        if (suggestedChangesForRule != null)
                        {
                            suggestedChangesForElement ??= [ ];
                            suggestedChangesForElement.AddRange(suggestedChangesForRule);
                        }
                        break;
                    }

                    default:
                        throw new NotImplementedException();
                }
            }

            if (suggestedChangesForElement?.Count > 0)
            {
                // Remove conflicting suggested changes
                
                for (int i = 0; i < suggestedChangesForElement.Count; i++)
                {
                    // Set and remove for same key?
                    
                    if (suggestedChangesForElement[i] is OsmRemoveKeySuggestedAction removeKey)
                    {
                        for (int k = 0; k < suggestedChangesForElement.Count; k++)
                        {
                            if (k == i)
                                continue;

                            if (suggestedChangesForElement[k] is OsmSetValueSuggestedAction setValue)
                            {
                                if (setValue.Key == removeKey.Key)
                                {
                                    suggestedChangesForElement.RemoveAt(i);
                                    i--;
                                    break;
                                }
                            }
                        }
                    }
                }
                
                suggestedChanges.AddRange(suggestedChangesForElement);
            }

            continue;


            void CheckElementFixme()
            {
                string? fixmeValue = osmElement.GetValue("fixme");
            
                if (fixmeValue != null)
                {
                    report.AddEntry(
                        ReportGroup.ValidationResults,
                        new IssueReportEntry(
                            "OSM element has a `fixme=" + fixmeValue + "` set" + itemLabel + " - " + osmElement.OsmViewUrl,
                            new SortEntryAsc(GetSortKey(osmElement)),
                            osmElement.AverageCoord,
                            MapPointStyle.Problem,
                            osmElement
                        )
                    );
                }
            }

            List<SuggestedAction>? CheckElementHasValue(ValidateElementHasValue rule)
            {
                List<SuggestedAction>? suggestedChangesForRule = null;
                
                string? elementValue = osmElement.GetValue(rule.Tag);
                
                if (rule.Value == null) // we don't know what is expected
                    return null;

                // Is the expected value in a different tag that is known to be incorrect there?
                List<string>? foundInIncorrectTags = CheckIncorrectTagsForValue(rule.IncorrectTags, osmElement, rule.Value);

                if (foundInIncorrectTags != null)
                {
                    report.AddEntry(
                        ReportGroup.ValidationResults,
                        new IssueReportEntry(
                            "OSM element has expected value `" + rule.Value + "` set" + itemLabel +
                            ", but not in the expected tag `" + rule.Tag + "`" +
                            " (found in tag(s): " + string.Join(", ", foundInIncorrectTags.Select(t => "`" + t + "`")) + ")" +
                            " - " + osmElement.OsmViewUrl,
                            new SortEntryAsc(GetSortKey(osmElement)),
                            osmElement.AverageCoord,
                            MapPointStyle.Problem,
                            osmElement
                        )
                    );
                    
                    suggestedChangesForRule ??= [ ];
                    foreach (string incorrectTag in foundInIncorrectTags)
                        suggestedChangesForRule.Add(new OsmRemoveKeySuggestedAction(osmElement, incorrectTag));
                }

                if (rule.Value != "") // we know what it should be 
                {
                    if (rule.Value != elementValue) // but it's not that
                    {
                        report.AddEntry(
                            ReportGroup.ValidationResults,
                            new IssueReportEntry(
                                "OSM element doesn't have expected " + GetTagValueDisplayString(rule.Tag, rule.Value) + " set" + itemLabel + " - " + osmElement.OsmViewUrl,
                                new SortEntryAsc(GetSortKey(osmElement)),
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );

                        suggestedChangesForRule ??= [ ];
                        suggestedChangesForRule.Add(new OsmSetValueSuggestedAction(osmElement, rule.Tag, rule.Value));
                    }
                }
                else // we don't know what it should be
                {
                    report.AddEntry(
                        ReportGroup.ValidationResults,
                        new IssueReportEntry(
                            "OSM element has unexpected " + GetTagValueDisplayString(rule.Tag, elementValue) + " set" + itemLabel + ", expecting none - " + osmElement.OsmViewUrl,
                            new SortEntryAsc(GetSortKey(osmElement)),
                            osmElement.AverageCoord,
                            MapPointStyle.Problem,
                            osmElement
                        )
                    );
                    
                    suggestedChangesForRule ??= [ ];
                    suggestedChangesForRule.Add(new OsmSetValueSuggestedAction(osmElement, rule.Tag, rule.Value) );
                }

                return suggestedChangesForRule;
            }

            List<SuggestedAction>? CheckElementHasAnyValue(ValidateElementHasAnyValue rule)
            {
                List<SuggestedAction>? suggestedChangesForRule = null;
                
                string? value = osmElement.GetValue(rule.Tag);

                if (value == null)
                {
                    report.AddEntry(
                        ReportGroup.ValidationResults,
                        new IssueReportEntry(
                            "OSM element doesn't have expected " + GetTagValueDisplayString(rule.Tag, rule.Values) + " set" + itemLabel + " - " + osmElement.OsmViewUrl,
                            new SortEntryAsc(GetSortKey(osmElement)),
                            osmElement.AverageCoord,
                            MapPointStyle.Problem,
                            osmElement
                        )
                    );
                }
                else
                {
                    if (!rule.Values.Contains(value))
                    {
                        report.AddEntry(
                            ReportGroup.ValidationResults,
                            new IssueReportEntry(
                                "OSM element doesn't have expected " + GetTagValueDisplayString(rule.Tag, rule.Values) + " set" + itemLabel + ", instead `" + value + "` - " + osmElement.OsmViewUrl,
                                new SortEntryAsc(GetSortKey(osmElement)),
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );
                    }
                }

                return suggestedChangesForRule;
            }

            void CheckElementHasKey(ValidateElementHasKey rule)
            {
                string? value = osmElement.GetValue(rule.Tag);

                if (value == null)
                {
                    report.AddEntry(
                        ReportGroup.ValidationResults,
                        new IssueReportEntry(
                            "OSM element doesn't have expected `" + rule.Tag + "` set" + itemLabel + " - " + osmElement.OsmViewUrl,
                            new SortEntryAsc(GetSortKey(osmElement)),
                            osmElement.AverageCoord,
                            MapPointStyle.Problem,
                            osmElement
                        )
                    );
                }
            }

            void CheckElementDoesntHaveValue(ValidateElementDoesntHaveTag rule)
            {
                string? value = osmElement.GetValue(rule.Tag);

                if (value != null)
                {
                    report.AddEntry(
                        ReportGroup.ValidationResults,
                        new IssueReportEntry(
                            "OSM element isn't expected to have `" + rule.Tag + "` set" + itemLabel + ", instead `" + value + "` - " + osmElement.OsmViewUrl,
                            new SortEntryAsc(GetSortKey(osmElement)),
                            osmElement.AverageCoord,
                            MapPointStyle.Problem,
                            osmElement
                        )
                    );
                }
            }

            void CheckElementHasAcceptableValue(ValidateElementHasAcceptableValue rule)
            {
                string? value = osmElement.GetValue(rule.Tag);

                if (value != null)
                {
                    if (!rule.Check(value))
                    {
                        report.AddEntry(
                            ReportGroup.ValidationResults,
                            new IssueReportEntry(
                                "OSM element doesn't have a " + rule.ValueLabel + " set" + itemLabel + ", instead `" + value + "` - " + osmElement.OsmViewUrl,
                                new SortEntryAsc(GetSortKey(osmElement)),
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );
                    }
                }
            }

            List<SuggestedAction>? CheckElementValueMatchesDataItemValue(ValidateElementValueMatchesDataItemValue<T> rule)
            {
                // No item, no "problem"
                if (dataItem == null)
                    return null;
                
                List<SuggestedAction>? suggestedChangesForRule = null;
                
                string? elementValue = osmElement.GetValue(rule.Tag);
                string? dataValue = rule.DataItemValueLookup(dataItem);

                if (dataValue == null)
                    return null; // we don't know what it is supposed to be 

                if (dataValue != "") // we know what it should be
                {
                    // Is the expected value in a different tag that is known to be incorrect there?
                    List<string>? foundInIncorrectTags = CheckIncorrectTagsForValue(rule.IncorrectTags, osmElement, dataValue);

                    if (foundInIncorrectTags != null)
                    {
                        report.AddEntry(
                            ReportGroup.ValidationResults,
                            new IssueReportEntry(
                                "OSM element has expected value `" + dataValue + "` set" + itemLabel +
                                ", but not in the expected tag `" + rule.Tag + "`" +
                                " (found in tag(s): " + string.Join(", ", foundInIncorrectTags.Select(t => "`" + t + "`")) + ")" +
                                " - " + osmElement.OsmViewUrl,
                                new SortEntryAsc(GetSortKey(osmElement)),
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );

                        suggestedChangesForRule ??= [ ];
                        foreach (string incorrectTag in foundInIncorrectTags)
                            suggestedChangesForRule.Add(new OsmRemoveKeySuggestedAction(osmElement, incorrectTag));
                    }

                    if (elementValue == null)
                    {
                        report.AddEntry(
                            ReportGroup.ValidationResults,
                            new IssueReportEntry(
                                "OSM element doesn't have expected " + GetTagValueDisplayString(rule.Tag, dataValue) + " set" + itemLabel + " - " + osmElement.OsmViewUrl,
                                new SortEntryAsc(GetSortKey(osmElement)),
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );

                        suggestedChangesForRule ??= [ ];
                        suggestedChangesForRule.Add(new OsmSetValueSuggestedAction(osmElement, rule.Tag, dataValue));
                    }
                    else
                    {
                        if (elementValue != dataValue)
                        {
                            report.AddEntry(
                                ReportGroup.ValidationResults,
                                new IssueReportEntry(
                                    "OSM element doesn't have expected " + GetTagValueDisplayString(rule.Tag, dataValue) + " set" + itemLabel + ", instead `" + elementValue + "` - " + osmElement.OsmViewUrl,
                                    new SortEntryAsc(GetSortKey(osmElement)),
                                    osmElement.AverageCoord,
                                    MapPointStyle.Problem,
                                    osmElement
                                )
                            );
                        }
                    }
                }
                else // we don't know what it should be
                {
                    if (elementValue != null)
                    {
                        report.AddEntry(
                            ReportGroup.ValidationResults,
                            new IssueReportEntry(
                                "OSM element has unexpected " + GetTagValueDisplayString(rule.Tag, elementValue) + " set" + itemLabel + ", expecting none - " + osmElement.OsmViewUrl,
                                new SortEntryAsc(GetSortKey(osmElement)),
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );
                    
                        suggestedChangesForRule ??= [ ];
                        suggestedChangesForRule.Add(new OsmRemoveKeySuggestedAction(osmElement, rule.Tag));
                    }
                }

                return null;
            }
        }
        
        return suggestedChanges;
    }

    private static List<string>? CheckIncorrectTagsForValue(string[]? incorrectTags, OsmElement osmElement, string? dataValue)
    {
        if (incorrectTags == null)
            return null;
        
        List<string>? foundInIncorrectTags = null;

        foreach (string incorrectTag in incorrectTags)
        {
            string? incorrectTagValue = osmElement.GetValue(incorrectTag);
                        
            if (incorrectTagValue != null)
            {
                if (incorrectTagValue == dataValue)
                {
                    foundInIncorrectTags ??= [ ];
                    foundInIncorrectTags.Add(incorrectTag);
                }
            }
        }
            
        return foundInIncorrectTags;

    }


    [Pure]
    private static string GetTagValueDisplayString(string tag, string value)
    {
        if (tag == "wikidata" || tag.EndsWith(":wikidata") && Regex.IsMatch(value, @"^Q\d+$"))
            return "`" +  tag + "=" + value + "` (https://www.wikidata.org/entity/" + value + ")"; // link to wikidata
        
        return "`" +  tag + "=" + value + "`";
    }
    
    [Pure]
    private static string GetTagValueDisplayString(string tag, string[] values)
    {
        if (values.Length == 1)
            return "`" +  tag + "=" + values[0] + "`";

        string s = "`" + tag + "`=";
        
        for (int i = 0; i < values.Length; i++)
        {
            if (i == values.Length - 1)
                s += " or ";
            else if (i > 0)
                s += ", ";

            s += "`" + values[i] + "`";
        }

        return s;
    }

    [Pure]
    private static string GetSortKey(OsmElement osmElement)
    {
        return 
            osmElement.GetValue("name") ?? 
            osmElement.Id.ToString();
        
        // todo: smarter?
        // todo: based on analyzer's custom rule?
    }


    private enum ReportGroup
    {
        ValidationResults = -5 // after correlator and probably before analyzer extra issues
    }
}