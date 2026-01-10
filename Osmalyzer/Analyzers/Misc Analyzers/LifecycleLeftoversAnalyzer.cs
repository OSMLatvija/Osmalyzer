namespace Osmalyzer;

[UsedImplicitly]
public class LifecycleLeftoversAnalyzer : Analyzer
{
    public override string Name => "Life-cycle Leftovers";

    public override string Description => "Reports ways (highways, railways) that likely still have leftover life-cycle tags (proposed/construction/planned/etc.) after presumed completion";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;

        OsmData waysWithHighway = OsmData.Filter(
            new IsWay(),
            new HasAnyKey("highway", "railway")
        );

        // Prepare report

        report.AddGroup(ReportGroup.Main, "Ways with likely leftover life-cycle tags");

        // Lifecycle tags/prefixes we consider suspicious depending on highway value
        string[] lifecyclePrefixes =
        [
            "proposed", // highway, railway
            "construction", // highway, railway
            "planned", // highway
            "abandoned", // railway
            "disused", // railway
            "razed" // railway
        ];
        
        int checkedCount = 0;
        int reportedCount = 0;

        foreach (OsmElement element in waysWithHighway.Elements)
        {
            checkedCount++;

            string? highwayValue = element.GetValue("highway");
            string? railwayValue = element.GetValue("railway");

            if (highwayValue != null && railwayValue != null)
            {
                // We don't know how to deal with this
                continue;
            }
            
            string mainTag = highwayValue != null ? "highway" : "railway";
            string mainValue = highwayValue ?? railwayValue!;

            List<string> leftoverTags = [ ];

            foreach (string prefix in lifecyclePrefixes)
            {
                if (prefix == mainValue)
                    continue;

                foreach (string tag in (string[])[ prefix, prefix + ":" + mainValue ])
                {
                    if (element.HasKey(tag)) // e.g. proposed=* or proposed:highway=*
                    {
                        string? value = element.GetValue(tag);
                        
                        if (value == null)
                            continue;

                        if (tag == "construction" && value == "minor")
                            continue; // special valid case of construction=minor
                        
                        if (tag is "disused" or "abandoned" && value == "yes")
                            if (!lifecyclePrefixes.Contains(mainValue))
                                continue; // common valid case of disused=yes or abandoned=yes on nonlifecycle highway=*

                        // For completed ways (no lifecycle on way) or mismatched lifecycle mixes, report the lifecycle tag
                        leftoverTags.Add(tag + "=" + value);
                    }
                }
            }

            if (leftoverTags.Count == 0)
                continue;

            string label =
                "`" + mainTag + "=" + mainValue + "`" +
                (element.HasKey("name") ? " \"`" + element.GetValue("name") + "`\"" : "");
            
            string keysStr = string.Join(", ", leftoverTags.Select(t => "`" + t + "`"));

            report.AddEntry(
                ReportGroup.Main,
                new IssueReportEntry(
                    label + " has suspect lifecycle tags " + keysStr + " -- " + element.OsmViewUrl,
                    element.AverageCoord,
                    MapPointStyle.Problem
                )
            );
            
            reportedCount++;
        }

        // Summary
        
        report.AddGroup(ReportGroup.Stats, "Stats");
        
        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                "Checked " + checkedCount + " ways; found " + reportedCount + " with possible leftover lifecycle tags"
            )
        );
    }

    
    private enum ReportGroup
    {
        Main,
        Stats
    }
}
