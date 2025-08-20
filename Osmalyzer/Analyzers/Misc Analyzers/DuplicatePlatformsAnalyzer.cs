namespace Osmalyzer;

[UsedImplicitly]
public class DuplicatePlatformsAnalyzer : Analyzer
{
    public override string Name => "Duplicate Platforms";

    public override string Description => "This report finds public transport platforms that are mapped in duplicate, such as node + way.";

    public override AnalyzerGroup Group => AnalyzerGroup.PublicTransport;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract platforms = osmMasterData.Filter(
            new IsNodeOrWay(),
            new HasValue("public_transport", "platform")
        );
        
        // todo: what to do with highway=bus_stop ?

        OsmDataExtract platformNodes = platforms.Filter(
            new IsNode()
        );

        OsmDataExtract platformWays = platforms.Filter(
            new IsWay()
        );

        const float duplicateMatchDistance = 20;
        
        // Parse

        List<DuplicateNodeWithWaysGroup> duplicateNodeWithWaysGroups = new List<DuplicateNodeWithWaysGroup>();
            
        foreach (OsmNode platformNode in platformNodes.Nodes)
        {
            List<OsmWay> closestWays = platformWays.GetClosestWaysTo(platformNode.AverageCoord, duplicateMatchDistance);
            
            // todo: de-duplicate within groups - closest node to way
            
            if (closestWays.Count > 0)
                duplicateNodeWithWaysGroups.Add(new DuplicateNodeWithWaysGroup(platformNode, closestWays));
        }
        
        // Report
        
        report.AddGroup(ReportGroup.NodesWithDuplicateWays, "Nodes with duplicate ways");

        report.AddEntry(
            ReportGroup.NodesWithDuplicateWays,
            new DescriptionReportEntry(
                $"These public stop nodes with `public_transport=platform` have platform way(s) next to them within {duplicateMatchDistance} meters that should likely be `highway=platform` (if they represent the same stop)."
            )
        );

        if (duplicateNodeWithWaysGroups.Count > 0)
        {
            foreach (DuplicateNodeWithWaysGroup duplicateGroup in duplicateNodeWithWaysGroups)
            {
                report.AddEntry(
                    ReportGroup.NodesWithDuplicateWays,
                    new IssueReportEntry(
                        "Node " + duplicateGroup.Node.OsmViewUrl +
                        " has " + duplicateGroup.Ways.Count + " duplicate ways nearby: " + 
                        string.Join("; ", duplicateGroup.Ways.Select(w => w.OsmViewUrl)),
                        duplicateGroup.Node.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
        }
        else
        {
            report.AddEntry(
                ReportGroup.NodesWithDuplicateWays,
                new GenericReportEntry(
                    "No duplicates found."
                )
            );
        }
        
        // Stats
        
        report.AddGroup(ReportGroup.Stats, "Stats");

        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                "There are " + platforms.Count + " platforms mapped." 
            )
        );
        
        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                "There are " + platformNodes.Count + " platforms mapped as nodes." 
            )
        );

        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                "There are " + platformWays.Count + " platforms mapped as ways." 
            )
        );
    }


    private record DuplicateNodeWithWaysGroup(OsmNode Node, List<OsmWay> Ways);
    
    
    private enum ReportGroup
    {
        NodesWithDuplicateWays,
        Stats
    }
}