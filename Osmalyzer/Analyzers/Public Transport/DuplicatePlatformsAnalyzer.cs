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

        OsmData osmMasterData = osmData.MasterData;

        OsmData platforms = osmMasterData.Filter(
            new IsNodeOrWay(),
            new HasValue("public_transport", "platform")
        );
        
        // todo: what to do with highway=bus_stop ?

        OsmData platformNodes = platforms.Filter(
            new IsNode()
        );

        OsmData platformWays = platforms.Filter(
            new IsWay()
        );

        const float duplicateMatchDistance = 20;
        
        // Parse

        List<DuplicateNodeWithWaysGroup> duplicateNodeWithWaysGroups = [ ];
        List<OsmNode> nodesWithoutIssues = [ ];
            
        foreach (OsmNode platformNode in platformNodes.Nodes)
        {
            List<OsmWay> closestWays = platformWays.GetClosestWaysTo(platformNode.AverageCoord, duplicateMatchDistance);
            
            // todo: de-duplicate within groups - closest node to way
            
            // Skip way platforms unambiguously linked to a node platform, which we don't need to check
            // (They may have wrong tags, but that's not our problem - they were grouped, so it's a problem within the group)
            closestWays = closestWays.Where(w => !IsPlatformAndNodeInDifferentTransitStopRelations(w, platformNode)).ToList();
            
            if (closestWays.Count > 0)
                duplicateNodeWithWaysGroups.Add(new DuplicateNodeWithWaysGroup(platformNode, closestWays));
            else
                nodesWithoutIssues.Add(platformNode);
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

        foreach (OsmNode node in nodesWithoutIssues)
        {
            report.AddEntry(
                ReportGroup.NodesWithDuplicateWays,
                new MapPointReportEntry( // no text
                    node.AverageCoord,
                    "Node " + node.OsmViewUrl + " has no detected duplicate ways nearby.",
                    MapPointStyle.Okay
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

    
    [Pure]
    private static bool IsPlatformAndNodeInDifferentTransitStopRelations(OsmWay way, OsmNode node)
    {
        List<OsmRelationMember>? nodeRelations = node.Relations?.Where(r => LooksLikeTransitStop(r.Owner)).ToList();
        
        if (nodeRelations == null)
            return false; // no relations, so no possible links
        
        if (nodeRelations.Count > 1)
            return false; // node is in multiple relations, so we cannot determine unambiguously
        // todo: this is a convoluted case, does this even exist in practice?
        
        List<OsmRelationMember>? wayRelations = way.Relations?.Where(r => LooksLikeTransitStop(r.Owner)).ToList();
        
        if (wayRelations == null)
            return false; // no relations, so no possible links

        // Check each way relation and see if the node is also in it
        foreach (OsmRelationMember relationMember in wayRelations)
            if (relationMember.Owner.Members.Any(m => m.Element! == node))
                return true; // node and way in the same relation
        
        return false;
    }

    private static bool LooksLikeTransitStop(OsmRelation relation)
    {
        // public_transport=stop_area + type=public_transport
        return relation.HasValue("public_transport", "stop_area") &&
               relation.HasValue("type", "public_transport");
    }


    private record DuplicateNodeWithWaysGroup(OsmNode Node, List<OsmWay> Ways);
    
    
    private enum ReportGroup
    {
        NodesWithDuplicateWays,
        Stats
    }
}