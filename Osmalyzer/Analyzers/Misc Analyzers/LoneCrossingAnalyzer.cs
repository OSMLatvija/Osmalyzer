namespace Osmalyzer;

[UsedImplicitly]
public class LoneCrossingAnalyzer : Analyzer
{
    public override string Name => "Lone Crossings";

    public override string Description => "This report finds crossings on roads/footways that don't have an accompanying expected footway/road/cycleway way. " +
                                          "That is, it is unclear what the crossing represents. " +
                                          "These are not errors if the mapping detail is simply missing/lacking. Ideally, ways should be drawn.";

    public override AnalyzerGroup Group => AnalyzerGroup.Roads;

    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract osmCrossingNodes = osmMasterData.Filter(
            new IsNode(),
            new HasAnyValue("highway", "crossing"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );
            
        // Parse

        report.AddGroup(ReportGroup.StrayCrossings, 
                        "Stray Crossings",
                        "These crossings have neither a road nor a footway.",
                        "All crossings have at least an expected way.");
        
        report.AddGroup(ReportGroup.RoadOnlyCrossings, 
                        "Road-only Crossings",
                        "These crossings have a road but no footway.",
                        "No crossings are road-only.");
        
        report.AddGroup(ReportGroup.FootwayOnlyCrossings,
                        "Footway-only Crossings",
                        "These crossings have a footway but no road.",
                        "No crossings are footway-only.");

        List<StrayCrossingNode> strayCrossingNodes = [ ];
        List<RoadOnlyCrossingNode> roadOnlyCrossingNodes = [ ];
        List<FootwayOnlyCrossingNode> footwayOnlyCrossingNodes = [ ];

        foreach (OsmNode node in osmCrossingNodes.Nodes)
        {
            bool hasRoad = false;
            bool hasFootway = false;
            bool hasCycleway = false;

            if (node.Ways != null)
            {
                foreach (OsmWay parentWay in node.Ways)
                {
                    if (parentWay.HasValue("highway", "motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link", "living_street", "pedestrian", "service", "track"))
                        hasRoad = true;
                    
                    if (parentWay.HasValue("highway", "footway", "path", "pedestrian"))
                        hasFootway = true;
                    
                    if (parentWay.HasValue("highway", "cycleway"))
                        hasCycleway = true;
                }
            }
            
            bool hasPerson = hasFootway || hasCycleway;

            if (hasRoad && !hasPerson)
            {
                roadOnlyCrossingNodes.Add(new RoadOnlyCrossingNode(node));
            }
            else if (!hasRoad && hasPerson)
            {
                if (!hasPerson || !hasCycleway) // footway crossing cycleway is a valid crossing 
                    footwayOnlyCrossingNodes.Add(new FootwayOnlyCrossingNode(node));
            }
            else if (!hasRoad && !hasPerson)
            {
                strayCrossingNodes.Add(new StrayCrossingNode(node));
            }
        }

        foreach (StrayCrossingNode strayCrossingNode in strayCrossingNodes)
        {
            report.AddEntry(
                ReportGroup.StrayCrossings,
                new IssueReportEntry(
                    "This crossing is neither on a road nor a footway - " + strayCrossingNode.Node.OsmViewUrl,
                    strayCrossingNode.Node.AverageCoord,
                    MapPointStyle.Problem
                )
            );
        }
        
        foreach (RoadOnlyCrossingNode roadOnlyCrossingNode in roadOnlyCrossingNodes)
        {
            report.AddEntry(
                ReportGroup.RoadOnlyCrossings,
                new IssueReportEntry(
                    "This crossing is on a road but not on a footway - " + roadOnlyCrossingNode.Node.OsmViewUrl,
                    roadOnlyCrossingNode.Node.AverageCoord,
                    MapPointStyle.Problem
                )
            );
        }
        
        foreach (FootwayOnlyCrossingNode footwayOnlyCrossingNode in footwayOnlyCrossingNodes)
        {
            report.AddEntry(
                ReportGroup.FootwayOnlyCrossings,
                new IssueReportEntry(
                    "This crossing is on a footway but not on a road - " + footwayOnlyCrossingNode.Node.OsmViewUrl,
                    footwayOnlyCrossingNode.Node.AverageCoord,
                    MapPointStyle.Problem
                )
            );
        }
    }
    
    
    private record StrayCrossingNode(OsmNode Node);
    
    private record RoadOnlyCrossingNode(OsmNode Node);
    
    private record FootwayOnlyCrossingNode(OsmNode Node);
        
        
    private enum ReportGroup
    {
        StrayCrossings,
        RoadOnlyCrossings,
        FootwayOnlyCrossings
    }
}