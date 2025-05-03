namespace Osmalyzer;

[UsedImplicitly]
public class HighwaySeasonalSpeedsAnalyzer : Analyzer
{
    public override string Name => "Highway Seasonal Speeds";

    public override string Description => "This report finds different values for highway seasonal speeds, that is, highways that have higher summer speed limits.";

    public override AnalyzerGroup Group => AnalyzerGroups.Road;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];

        
    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
            
        OsmDataExtract speedLimitedRoads = osmMasterData.Filter(
            new IsWay(),
            new HasAnyValue("highway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "service"),
            new HasKey("maxspeed"),
            new HasKey("maxspeed:conditional")
        );
            
        // Start report file
            
        report.AddGroup(ReportGroup.Main, "Ways with maxspeed and maxspeed:conditional: " + speedLimitedRoads.Elements.Count);

        // Process
            
        List<(int regular, int conditional)> limits = new List<(int regular, int conditional)>(); 
                
        foreach (OsmElement way in speedLimitedRoads.Elements)
        {
            string maxspeedStr = way.GetValue("maxspeed")!;

            if (int.TryParse(maxspeedStr, out int maxspeed))
            {
                string maxspeedConditionalStr = way.GetValue("maxspeed:conditional")!;

                Match match = Regex.Match(maxspeedConditionalStr, @"([0-9]+)\s*@\s*\(May 1\s*-\s*Oct 1\)");

                if (match.Success)
                {
                    int maxspeedConditional = int.Parse(match.Groups[1].ToString());
                        
                    if (!limits.Any(l => l.regular == maxspeed && l.conditional == maxspeedConditional))
                        limits.Add((maxspeed, maxspeedConditional));

                    if (maxspeed == maxspeedConditional)
                    {
                        OsmCoord coord = way.GetAverageCoord();

                        report.AddEntry(
                            ReportGroup.Main,
                            new IssueReportEntry(
                                "Same limits for " + maxspeed + ": " + maxspeedConditionalStr + " on " + way.OsmViewUrl,
                                coord,
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
                else
                {
                    if (!Regex.IsMatch(maxspeedConditionalStr, @"\d+ @ \((\w\w-\w\w )?\d\d:\d\d-\d\d:\d\d\)")) // "30 @ (Mo-Fr 07:00-19:00)" / "90 @ (22:00-07:00)"
                    {
                        OsmCoord coord = way.GetAverageCoord();

                        report.AddEntry(
                            ReportGroup.Main,
                            new GenericReportEntry(
                                "Max speed does not appear to be seasonal: " + maxspeedConditionalStr + " on " + way.OsmViewUrl,
                                coord,
                                MapPointStyle.Dubious
                            )
                        );
                    }
                }
            }
            else
            {
                OsmCoord coord = way.GetAverageCoord();

                report.AddEntry(
                    ReportGroup.Main,
                    new GenericReportEntry(
                        "Maxspeed not recognized: " + maxspeedStr + " on " + way.OsmViewUrl,
                        coord,
                        MapPointStyle.Dubious
                    )
                );
            }
        }

        limits.Sort();
            
        report.AddGroup(ReportGroup.Combos, "Combos found");

        foreach ((int regular, int conditional) in limits)
        {
            report.AddEntry(
                ReportGroup.Combos,
                new GenericReportEntry(
                    "Conditional limit " + conditional + " for regular limit " + regular
                )
            );
        }
    }
        
    private enum ReportGroup
    {
        Main,
        Combos
    }
}