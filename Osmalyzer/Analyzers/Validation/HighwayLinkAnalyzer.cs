namespace Osmalyzer;

[UsedImplicitly]
[DisabledAnalyzer("WIP")]
public class HighwayLinkAnalyzer : Analyzer
{
    public override string Name => "Highway Links";

    public override string Description => "This report checks common issues with highway links.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData osmMasterData = osmData.MasterData;

        string[] mainValues = [ "motorway", "trunk", "primary", "secondary", "tertiary" ];
        string[] linksValues = [ "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link" ];
        
        OsmData ways = osmMasterData.Filter(
            new IsWay(),
            new HasAnyValue(
                "highway",
                mainValues.Concat(linksValues)
            )
        );
        
        OsmData mains = ways.Filter(
            new HasAnyValue(
                "highway",
                mainValues
            )
        );
        
        OsmData links = ways.Filter(
            new HasAnyValue(
                "highway",
                linksValues
            )
        );

        // Prepare groups

        report.AddGroup(ReportGroup.LinkOnlyConnections, "Link-only connections");

        report.AddEntry(
            ReportGroup.LinkOnlyConnections,
            new DescriptionReportEntry(
                "These connections between roads are only with a link and no main highway classification. " +
                "This is most likely an error."
            )
        );
        
        // Parse
        
        // todo: actually design
        // todo: loops, multiple connections, nearby connections, etc.
    }
        
    private enum ReportGroup
    {
        LinkOnlyConnections
    }
}