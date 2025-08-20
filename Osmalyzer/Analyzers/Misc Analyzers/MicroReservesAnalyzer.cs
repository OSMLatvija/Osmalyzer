namespace Osmalyzer;

[UsedImplicitly]
public class MicroReservesAnalyzer : Analyzer
{
    public override string Name => "Micro Reserves";

    public override string Description => "This report checks that excepted microreserves are mapped";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;

    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(MicroReserveAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load reserve data

        MicroReserveAnalysisData reserveData = datas.OfType<MicroReserveAnalysisData>().First();

        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmReserves = osmMasterData.Filter(
            new OrMatch(
                new AndMatch(
                    new IsWay(),
                    new HasValue("leisure", "nature_reserve")
                ),
                new AndMatch(
                    new IsWayOrRelation(),
                    new HasValue("boundary", "protected_area")
                )
            )
        );

        // TODO: https://likumi.lv/ta/id/20083-noteikumi-par-dabas-liegumiem - full reserves
            
        // Parse

        // TODO: CORRELATOR
        
        report.AddGroup(ReportGroup.Issues, "Unmatched Micro Reserves", null, "All defined reserves have a matching OSM element.");
            
        report.AddGroup(ReportGroup.Matched, "Matched Micro Reserves");

        int matchedCount = 0;

        List<(OsmElement osm, List<Microreserve> reserves)> matches = new List<(OsmElement, List<Microreserve>)>(); 
            
        foreach (Microreserve reserve in reserveData.Reserves)
        {
            const int searchDistance = 300;
                
            OsmElement? osmReserve = osmReserves.GetClosestElementTo(reserve.Coord, searchDistance, out double? closestDistance);

            if (osmReserve != null)
            {
                matchedCount++;

                if (closestDistance > 50)
                {
                    // todo: we have like 3000 unmatched, so this wouldn't help
                }
                    
                report.AddEntry(
                    ReportGroup.Matched, 
                    new MapPointReportEntry(
                        reserve.Coord, 
                        "Match!",
                        MapPointStyle.Okay
                    )
                );

                (OsmElement _, List<Microreserve> previousMatchedReserves) = matches.FirstOrDefault(m => m.osm == osmReserve);
                if (previousMatchedReserves != null)
                    previousMatchedReserves.Add(reserve);
                else
                    matches.Add((osmReserve, new List<Microreserve>() { reserve }));
            }
            else
            {
                report.AddEntry(
                    ReportGroup.Issues,
                    new IssueReportEntry(
                        "Couldn't find an OSM element for micro-reserve " + reserve + " within " + searchDistance + " m.",
                        reserve.Coord,
                        MapPointStyle.Problem
                    )
                );
            }
        }

        int multimatches = 0;
            
        foreach ((OsmElement osmReserve, List<Microreserve> matchedReserves) in matches)
        {
            if (matchedReserves.Count > 1)
            {
                multimatches++;

                report.AddEntry(
                    ReportGroup.Issues,
                    new IssueReportEntry(
                        "OSM reserve " + osmReserve.OsmViewUrl + " " +
                        "matched to multiple reserves - " + string.Join("; ", matchedReserves.Select(r => r.ToString())) + ".",
                        osmReserve.AverageCoord,
                        MapPointStyle.Dubious
                    )
                );
            }
        }

        report.AddEntry(
            ReportGroup.Issues,
            new DescriptionReportEntry(
                "Matched " + matchedCount + "/" + reserveData.Reserves.Count + " reserves to " + matches.Count + "/" + osmReserves.Count + " OSM elements with " + multimatches + " multi-matches."
            )
        );
    }
        
    private enum ReportGroup
    {
        Issues,
        Matched
    }
}