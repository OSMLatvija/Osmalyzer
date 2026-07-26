namespace Osmalyzer;

[UsedImplicitly]
public class CrossingIslandAnalyzer : Analyzer
{
    public override string Name => "Crossing islands";

    public override string Description => "This report checks crossing islands for various issues.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData osmMasterData = osmData.MasterData;

        OsmData ways = osmMasterData.Filter(
            new IsWay(),
            new OrMatch(
                new HasValue("highway", "path"),
                new HasValue("highway", "footway")
            ),
            new HasValue("footway", "crossing") // todo: later - what if it doesn't, but still have a crossing node?
        );

        // Prepare groups

        report.AddGroup(ReportGroup.MismatchedBad, "Untagged connecting island way");

        report.AddEntry(
            ReportGroup.MismatchedBad,
            new DescriptionReportEntry(
                "When crossings with traffic islands are mapped as separate ways for each road segment, " +
                "the connecting island way between them should be tagged with footway=traffic_island" +
                " (if it is indeed in the road median or along traffic islands)."
            )
        );
        
        // todo: later - crossing:island = yes between very close ways - likely meant no
        // todo: later - crossing way with multiple nodes - should split
        
        // Parse
        
        // TODO
    }
   
        
    private enum ReportGroup
    {
        MismatchedBad
    }
}