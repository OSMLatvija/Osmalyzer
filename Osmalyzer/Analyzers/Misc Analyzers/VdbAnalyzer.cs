using System.Diagnostics;

namespace Osmalyzer;

[UsedImplicitly]
public class VdbAnalyzer : Analyzer
{
    // todo: similar names with similar coords, like Rejeņi
    
    public override string Name => "VDB Place Names Statistics";

    public override string Description => "This report shows statistics for the VDB (Vietvārdu datubāze) place names data.";

    public override AnalyzerGroup Group => AnalyzerGroup.Miscellaneous;


    public override List<Type> GetRequiredDataTypes() => [ typeof(VdbAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        // Report overall statistics
        
        report.AddGroup(
            ReportGroup.Stats,
            "Overall statistics",
            "This gives an overview of the parsed well-known VDB data. " +
            "Also some statistics for fields that need to be parsed to make structured sense."
        );
        
        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                $"Total entries: {vdbData.Entries.Count}"
            )
        );        
        
        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                $"Admin (hamlet, village, parish, city, municipality) division entries: " +
                $"{vdbData.Municipalities.Count + vdbData.Cities.Count + vdbData.Parishes.Count + vdbData.Villages.Count + vdbData.Hamlets.Count}, of which active: " +
                $"municipalities x {vdbData.Municipalities.Count(e => e.IsActive)}" +
                $", cities x {vdbData.Cities.Count(e => e.IsActive)}" +
                $", parishes x {vdbData.Parishes.Count(e => e.IsActive)}" +
                $", villages x {vdbData.Villages.Count(e => e.IsActive)}" +
                $", hamlets x {vdbData.Hamlets.Count(e => e.IsActive)}"
            )
        );

        Dictionary<string, int> pronunciationQualifierCounts = new Dictionary<string, int>();
        Dictionary<string, int> commentQualifierCounts = new Dictionary<string, int>();
        
        foreach (VdbEntry entry in vdbData.Entries)
        {
            foreach (VdbAltName altName in entry.AltNames)
            {
                foreach (VdbAltNameQualifier qualifier in altName.Qualifiers)
                {
                    if (qualifier.Type == VdbAltNameQualifierType.Pronunciation)
                    {
                        pronunciationQualifierCounts.TryAdd(qualifier.Content, 0);
                        pronunciationQualifierCounts[qualifier.Content]++;
                    }
                    else // Comment
                    {
                        commentQualifierCounts.TryAdd(qualifier.Content, 0);
                        commentQualifierCounts[qualifier.Content]++;
                    }
                }
            }
        }
        
        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                "Alternate names pronunciation qualifiers by count: " +
                string.Join(", ", pronunciationQualifierCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => $"`{kvp.Key.Replace("`","'")}` × {kvp.Value}"))
            )
        );
        
        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                "Alternate names comment qualifiers by count: " +
                string.Join(", ", commentQualifierCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => $"`{kvp.Key.Replace("`","'")}` × {kvp.Value}"))
            )
        );
        
#if DEBUG
        using FileStream fileStream = File.Create(Name + " - Active admin entries.txt");
        using StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);
        foreach (VdbEntry entry in vdbData.Municipalities.Where(e => e.IsActive)) writer.WriteLine(entry.ReportString());
        foreach (VdbEntry entry in vdbData.Cities.Where(e => e.IsActive)) writer.WriteLine(entry.ReportString());
        foreach (VdbEntry entry in vdbData.Parishes.Where(e => e.IsActive)) writer.WriteLine(entry.ReportString());
        foreach (VdbEntry entry in vdbData.Villages.Where(e => e.IsActive)) writer.WriteLine(entry.ReportString());
        foreach (VdbEntry entry in vdbData.Hamlets.Where(e => e.IsActive)) writer.WriteLine(entry.ReportString());
#endif

        // Report problematic data (duplicates)
        
        report.AddGroup(
            ReportGroup.ProblematicData,
            "Problematic Data",
            "This section reports problematic data issues found in the VDB data. " +
            "This find duplicate or near-identical entries, which are most likely an export error in the source data.",
            "No issues were found."
        );
        
        foreach (VdbEntryIssue issue in vdbData.Issues)
        {
            switch (issue)
            {
                case VdbResolvedDuplicate resolvedDuplicate:
                    report.AddEntry(
                        ReportGroup.ProblematicData,
                        new GenericReportEntry(
                            "Resolved a known near-identical entries by keeping entry " +
                            resolvedDuplicate.MainEntry.ReportString() +
                            $" and removing {resolvedDuplicate.RemovedEntries.Count} duplicate(s): " +
                            string.Join(", ", resolvedDuplicate.RemovedEntries.Select(e => e.ReportString()))
                        )
                    );
                    break;
                
                case VdbUnresolvedDuplicate unresolvedDuplicate:
                    report.AddEntry(
                        ReportGroup.ProblematicData,
                        new GenericReportEntry(
                            "Unresolved near-identical entries found: " +
                            string.Join(", ", unresolvedDuplicate.AllEntries.Select(e => e.ReportString()))
                        )
                    );
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(issue));
            }
        }
        
        // Report statistics for each raw field

        report.AddGroup(
            ReportGroup.RawValues,
            "Raw value summary",
            $"This lists the raw values from teh data, " +
            $"mostly sorted by the most common values for all the fields, so it can be better understood what they mean. " +
            $"These are not parsed or filtered in any way and may or may not apply or mean anything for different feature types."
        );

        for (int fieldIndex = 0; fieldIndex < VdbAnalysisData.FieldNames.Length; fieldIndex++)
        {
            string fieldName = VdbAnalysisData.FieldNames[fieldIndex];
            List<string> fieldValues = vdbData.RawEntries.Select(e => e.GetValue(fieldIndex) ?? "").ToList();

            // Count unique values
            Dictionary<string, int> valueCounts = new Dictionary<string, int>();

            foreach (string value in fieldValues)
            {
#if DEBUG
                if (value.Contains(Environment.NewLine))
                    Debug.WriteLine("Multiline value found in field \"" + fieldName + "\": `" + value + "`");
                
                if (value == "<Null>")
                    Debug.WriteLine("Literal \"<Null>\" value found in field \"" + fieldName + "\".");
#endif
                
                valueCounts.TryAdd(value, 0);
                valueCounts[value]++;
            }

            // Create group for this field
            report.AddGroup(
                fieldIndex,
                ReportGroup.RawValues,
                $"Field \"{fieldName}\""
            );

            if (fieldName is "GEOPLATUMS" or "GEOGARUMS" or "GEO_GAR" or "GEO_PLAT")
            {
                report.AddEntry(
                    fieldIndex,
                    new GenericReportEntry(
                        $"Field \"{fieldName}\" is coordinate value."
                    )
                );
            }
            else if (fieldName is "OBJECTID" or "OBJEKTAID")
            {
                // Skip reporting for unique ID fields
                report.AddEntry(
                    fieldIndex,
                    new GenericReportEntry(
                        $"Field \"{fieldName}\" contains {vdbData.Entries.Count} unique values."
                    )
                );
            }
            else if (fieldName is "DATUMSIZM" or "OBJEKTAID")
            {
                report.AddEntry(
                    fieldIndex,
                    new GenericReportEntry(
                        $"Field \"{fieldName}\" is a date."
                    )
                );
            }
            else
            {
                // Sort by count descending
                List<KeyValuePair<string, int>> sortedCounts = valueCounts
                                                               .OrderByDescending(kvp => kvp.Value)
                                                               .ToList();

                const int listLimit = 50;

                // Report top values
                foreach (KeyValuePair<string, int> kvp in sortedCounts.Take(listLimit))
                {
                    string displayValue = kvp.Key == "" ? "<empty>" : "`" + kvp.Key.Replace(Environment.NewLine, "↲") + "`";
                    float portion = (float)kvp.Value / vdbData.Entries.Count;

                    report.AddEntry(
                        fieldIndex,
                        new GenericReportEntry(
                            $"{displayValue} × {kvp.Value} ({portion * 100f:F3} %)",
                            new SortEntryDesc(kvp.Value)
                        )
                    );
                }

                // Report if there are more values
                if (sortedCounts.Count > listLimit)
                {
                    bool listAll = fieldName == "VEIDS"; // we want to know all types

                    if (listAll)
                    {
                        // List all remaining values, but concatenate into a single report entry for brevity
                        
                        List<string> remainingEntries = [ ];
                        
                        foreach (KeyValuePair<string, int> kvp in sortedCounts.Skip(listLimit))
                        {
                            string displayValue = kvp.Key == "" ? "<empty>" : "`" + kvp.Key.Replace(Environment.NewLine, "↲") + "`";

                            remainingEntries.Add(
                                $"{displayValue} × {kvp.Value}"
                            );
                        }
                        
                        report.AddEntry(
                            fieldIndex,
                            new GenericReportEntry(
                                "... remaining values: " + string.Join("; ", remainingEntries)
                            )
                        );
                        
#if DEBUG
                        // Also dump a file with all values in a list so we can quickly see them
                        File.WriteAllLines(Name + $" {fieldName} all values.txt", sortedCounts.Select(kvp => $"{kvp.Key}"), Encoding.UTF8);
#endif
                    }
                    else
                    {
                        // Just a summary
                        
                        int remaining = sortedCounts.Count - listLimit;
                        int remainingCount = sortedCounts.Skip(listLimit).Sum(kvp => kvp.Value);
                        float remainingPortion = (float)remainingCount / vdbData.Entries.Count;

                        report.AddEntry(
                            fieldIndex,
                            new GenericReportEntry(
                                $"... and {remaining} more unique values ({remainingCount} entries, {remainingPortion * 100f:F1} %)",
                                new SortEntryDesc(0)
                            )
                        );
                    }
                }
                
                if (fieldName is "PAGASTS")
                {
                    List<KeyValuePair<string, int>> nonParishCounts = sortedCounts
                                                                      .Where(e => !e.Key.EndsWith(" pagasts"))
                                                                      .ToList();

                    report.AddEntry(
                        fieldIndex,
                        new GenericReportEntry(
                            $"Non-parish entries: {nonParishCounts.Count} unique values: " +
                            string.Join("; ", nonParishCounts.Select(kvp => $"`{kvp.Key}` × {kvp.Value}")),
                            new SortEntryDesc(-1)
                        )
                    );
                }
                else if (fieldName is "NOVADS")
                {
                    List<KeyValuePair<string, int>> nonMunicipalityCounts = sortedCounts
                                                                            .Where(e => !e.Key.EndsWith(" novads"))
                                                                            .ToList();

                    report.AddEntry(
                        fieldIndex,
                        new GenericReportEntry(
                            $"Non-municipality entries: {nonMunicipalityCounts.Count} unique values: " +
                            string.Join("; ", nonMunicipalityCounts.Select(kvp => $"`{kvp.Key}` × {kvp.Value}")),
                            new SortEntryDesc(-1)
                        )
                    );
                }
                else if (fieldName is "PAMATNOSAUKUMS")
                {
                    // List some fun stats - a sampling of shortest names, longest names

                    IEnumerable<string> distinctValues = fieldValues.Distinct().Select(v => v.Replace("`", "\'")).ToList();

                    List<string> shortestNames = distinctValues
                                                 .OrderBy(v => v.Length)
                                                 .Take(250)
                                                 .ToList();
                    
                    report.AddEntry(
                        fieldIndex,
                        new GenericReportEntry(
                            $"A sampling of shortest names: " + string.Join("; ", shortestNames.Select(n => $"`{n}`")),
                            new SortEntryDesc(-1)
                        )
                    );

                    List<string> longestNames = distinctValues
                                                .OrderByDescending(v => v.Length)
                                                .Take(20)
                                                .ToList();
                    
                    report.AddEntry(
                        fieldIndex,
                        new GenericReportEntry(
                            $"A sampling of longest names: " + string.Join("; ", longestNames.Select(n => $"`{n}`")),
                            new SortEntryDesc(-2)
                        )
                    );
                }
            }
        }
    }


    private enum ReportGroup
    {
        // Values to avoid conflict with field index group IDs
        Stats = -100,
        ProblematicData = -99,
        RawValues = -98
    }
}
