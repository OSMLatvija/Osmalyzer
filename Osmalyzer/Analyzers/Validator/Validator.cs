using System;
using System.Linq;

namespace Osmalyzer;

public class Validator<T> where T : IDataItem
{
    private readonly CorrelatorReport _correlatorReport;


    public Validator(CorrelatorReport correlatorReport)
    {
        _correlatorReport = correlatorReport;
    }


    public void Validate(Report report, params ValidationRule[] rules)
    {
        report.AddGroup(
            ReportGroup.ValidationResults, 
            "Other issues",
            "These matched/found OSM elements and/or data items have additional individual (known) issues.",
            "No (known) issues found with matched/found OSM elements and/or data items."
        );
        
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

                case LoneCorrelation loneCorrelation:
                    osmElement = loneCorrelation.OsmElement;
                    dataItem = default;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(match));
            }


            // We may not have a data item matches, so only print label if there is one
            string itemLabel = dataItem != null ? " for " + dataItem.ReportString() : "";
            

            foreach (ValidationRule rule in rules)
            {
                switch (rule)
                {
                    case ValidateElementFixme:
                        CheckElementFixme();
                        break;
                    
                    case ValidateElementHasValue elementHasValue:
                        CheckElementHasValue(elementHasValue);
                        break;
                    
                    case ValidateElementHasAcceptableValue elementHasAcceptableValue:
                        CheckElementHasAcceptableValue(elementHasAcceptableValue);
                        break;

                    default:
                        throw new NotImplementedException();
                }
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
                            new SortEntryAsc(SortOrder.Tagging),
                            osmElement.GetAverageCoord(),
                            MapPointStyle.Problem
                        )
                    );
                }
            }

            void CheckElementHasValue(ValidateElementHasValue rule)
            {
                string? value = osmElement.GetValue(rule.Tag);

                if (value == null)
                {
                    report.AddEntry(
                        ReportGroup.ValidationResults,
                        new IssueReportEntry(
                            "OSM element doesn't have expected " + GetTagValueDisplayString(rule.Tag, rule.Values) + " set" + itemLabel + " - " + osmElement.OsmViewUrl,
                            new SortEntryAsc(SortOrder.Tagging),
                            osmElement.GetAverageCoord(),
                            MapPointStyle.Problem
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
                                new SortEntryAsc(SortOrder.Tagging),
                                osmElement.GetAverageCoord(),
                                MapPointStyle.Problem
                            )
                        );
                    }
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
                                new SortEntryAsc(SortOrder.Tagging),
                                osmElement.GetAverageCoord(),
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
            }
        }
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


    private enum ReportGroup
    {
        ValidationResults = -5 // after validation but probably before analyzer extra issues
    }
    
    private enum SortOrder // values used for sorting
    {
        Tagging = 0
    }
}