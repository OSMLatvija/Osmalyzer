using System.Diagnostics;

namespace Osmalyzer;

[UsedImplicitly]
public class VdbAnalyzer : Analyzer
{
    public override string Name => "VDB Place Names Statistics";

    public override string Description => "This report shows statistics for the VDB (Vietvārdu datubāze) place names data.";

    public override AnalyzerGroup Group => AnalyzerGroup.Miscellaneous;


    public override List<Type> GetRequiredDataTypes() => [ typeof(VdbAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();

        List<VdbEntry> entries = vdbData.Entries;

        // Report overall statistics

        report.AddGroup(
            ReportGroup.Overview,
            "Raw value summary",
            $"This list the most common values for all the fields in the data file."
        );

        // Report statistics for each raw field

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
                ReportGroup.Overview,
                $"Field \"{fieldName}\""
            );

            if (fieldName is "OBJECTID" or "OBJEKTAID")
            {
                // Skip reporting for unique ID fields
                report.AddEntry(
                    fieldIndex,
                    new GenericReportEntry(
                        $"Field \"{fieldName}\" contains {entries.Count} unique values."
                    )
                );
            }
            else
            {
                // Sort by count descending
                List<KeyValuePair<string, int>> sortedCounts = valueCounts
                                                               .OrderByDescending(kvp => kvp.Value)
                                                               .ToList();

                const int listLimit = 30;

                // Report top values
                foreach (KeyValuePair<string, int> kvp in sortedCounts.Take(listLimit))
                {
                    string displayValue = kvp.Key == "" ? "<empty>" : "`" + kvp.Key.Replace(Environment.NewLine, "↲") + "`";
                    float portion = (float)kvp.Value / entries.Count;

                    report.AddEntry(
                        fieldIndex,
                        new GenericReportEntry(
                            $"{displayValue} × {kvp.Value} ({portion * 100f:F3} %)"
                        )
                    );
                }

                // Report if there are more values
                if (sortedCounts.Count > listLimit)
                {
                    int remaining = sortedCounts.Count - listLimit;
                    int remainingCount = sortedCounts.Skip(listLimit).Sum(kvp => kvp.Value);
                    float remainingPortion = (float)remainingCount / entries.Count;

                    report.AddEntry(
                        fieldIndex,
                        new GenericReportEntry(
                            $"... and {remaining} more unique values ({remainingCount} entries, {remainingPortion * 100f:F1} %)"
                        )
                    );
                }
            }
        }
    }


    private enum ReportGroup
    {
        Overview
    }
}
