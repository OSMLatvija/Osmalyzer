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

        // Get all footways/paths
        OsmData allFootwayWays = osmMasterData.Filter(
            new IsWay(),
            new OrMatch(
                new HasValue("highway", "path"),
                new HasValue("highway", "footway")
            )
        );
        
        // Out of those, get all crossing ways
        OsmData crossingWays = allFootwayWays.Filter(
            new HasValue("footway", "crossing")
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
        
        // Build a lookup of ways by terminal nodes for quick access
        Dictionary<OsmNode, List<OsmWay>> waysAtNode = new Dictionary<OsmNode, List<OsmWay>>();
        foreach (OsmWay way in allFootwayWays.Ways)
        {
            OsmNode? firstNode = way.Nodes.FirstOrDefault();
            OsmNode? lastNode = way.Nodes.LastOrDefault();
            
            if (firstNode != null)
            {
                if (!waysAtNode.ContainsKey(firstNode))
                    waysAtNode[firstNode] = new List<OsmWay>();
                waysAtNode[firstNode].Add(way);
            }
            
            if (lastNode != null && lastNode != firstNode)
            {
                if (!waysAtNode.ContainsKey(lastNode))
                    waysAtNode[lastNode] = new List<OsmWay>();
                waysAtNode[lastNode].Add(way);
            }
        }

        HashSet<long> crossingWayIds = new HashSet<long>(crossingWays.Ways.Select(w => w.Id));
        List<ProblematicIslandWay> problematicWays = new List<ProblematicIslandWay>();

        // For each non-crossing way, check if it connects crossing ways on both ends
        foreach (OsmWay way in allFootwayWays.Ways)
        {
            // Skip if this is already marked as crossing or traffic island
            if (crossingWayIds.Contains(way.Id))
                continue;
            
            if (way.GetValue("footway") == "traffic_island")
                continue;

            OsmNode firstNode = way.Nodes[0];
            OsmNode lastNode = way.Nodes[^1];

            // Check if both ends connect to crossing ways
            bool firstConnectsToSomething = false;
            bool lastConnectsToSomething = false;

            if (waysAtNode.TryGetValue(firstNode, out List<OsmWay>? waysAtFirst))
            {
                firstConnectsToSomething = waysAtFirst.Any(w => w.Id != way.Id && crossingWayIds.Contains(w.Id));
            }

            if (waysAtNode.TryGetValue(lastNode, out List<OsmWay>? waysAtLast))
            {
                lastConnectsToSomething = waysAtLast.Any(w => w.Id != way.Id && crossingWayIds.Contains(w.Id));
            }

            if (firstConnectsToSomething && lastConnectsToSomething)
            {
                // This way connects crossing ways on both ends - it's a candidate for traffic island tagging
                OsmNode reportCoord = firstNode; // Use first node as the report coordinate
                problematicWays.Add(new ProblematicIslandWay(way, reportCoord));
            }
        }

        // Report findings
        if (problematicWays.Count > 0)
        {
            foreach (ProblematicIslandWay problem in problematicWays)
            {
                report.AddEntry(
                    ReportGroup.MismatchedBad,
                    new IssueReportEntry(
                        "Way " + problem.Way.OsmViewUrl + " connects crossing ways on both ends and should be tagged as `footway=traffic_island`.",
                        problem.Coordinate.coord,
                        MapPointStyle.Problem
                    )
                );
            }
        }
    }
   
        
    private record ProblematicIslandWay(OsmWay Way, OsmNode Coordinate);

    private enum ReportGroup
    {
        MismatchedBad
    }
}