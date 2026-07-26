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

        OsmData footwayWays = osmMasterData.Filter(
            new IsWay(),
            new OrMatch(
                new HasValue("highway", "path"), // todo: or not?
                new HasValue("highway", "footway")
            )
        );

        OsmData cyclewayWays = osmMasterData.Filter(
            new IsWay(),
            new OrMatch(
                new HasValue("highway", "cycleway")
            )
        );

        // Prepare groups

        report.AddGroup(ReportGroup.IslandsBetweenCrossings, "Island ways between crossing ways");

        report.AddEntry(
            ReportGroup.IslandsBetweenCrossings,
            new DescriptionReportEntry(
                "When crossings with traffic islands are mapped as separate ways for each road segment, " +
                "the island way between them should be tagged with `*=traffic_island`. " +
                "Due to many weird layouts, this will have false positives. " +
                "It is also impossible to detect what is a traffic island and what is just a short footway between independent crossings."
            )
        );

        // Prepare to collect issues
        
        List<ProblematicWay> untaggedIslandsBetweenCrossings = [ ];
        List<ProblematicWay> islandsWithNonCrossingNeighbours = [ ];

        // Process

        ProcessWays(footwayWays.Ways, "footway");
        ProcessWays(cyclewayWays.Ways, "cycleway");
        
        void ProcessWays(IEnumerable<OsmWay> ways, string primaryTag)
        {
            // Build helper structures

            ConnectingWayLookup connectingWayLookup = new ConnectingWayLookup(ways);

            // Process

            foreach (OsmWay way in ways)
            {
                // Make sure we are not a crossing
                if (LooksLikeCrossing(way))
                    continue;

                bool looksLikeTrafficIsland = LooksLikeTrafficIsland(way);

                OsmNode firstNode = way.Nodes[0];
                OsmNode lastNode = way.Nodes[^1];

                List<OsmWay> startNeighbours = connectingWayLookup.GetConnectingWays(firstNode, way).ToList();
                List<OsmWay> endNeighbours = connectingWayLookup.GetConnectingWays(lastNode, way).ToList();

                // If we are between other ways
                if (startNeighbours.Count == 0 || endNeighbours.Count == 0)
                    continue; // todo: report unconnected islands

                // Skip multiple connections, this is too complex and needs better micromapping before we can process it
                if (startNeighbours.Count > 1 || endNeighbours.Count > 1)
                    continue; // todo: report these as warnings for fixing may be?

                OsmWay startNeighbour = startNeighbours[0];
                OsmWay endNeighbour = endNeighbours[0];

                double distance = OsmGeoTools.DistanceBetween(firstNode.coord, lastNode.coord);

                if (looksLikeTrafficIsland)
                {
                    // If we are a traffic island, make sure we are between crossings
                    if (!LooksLikeCrossing(startNeighbour) || !LooksLikeCrossing(endNeighbour))
                        islandsWithNonCrossingNeighbours.Add(new ProblematicWay(way, startNeighbour, endNeighbour, primaryTag));

                    // todo: report when very long?
                }
                else
                {
                    // We cannot distinguish split crossings from just ways between independent crossings,
                    // so apply a threshold to avoid reporting ways that are more than likely not part of the same crossing "group" 
                    if (distance > 15) // meters
                        continue;

                    // If we are not a traffic island, make sure we are not between (two) crossings
                    if (LooksLikeCrossing(startNeighbour) && LooksLikeCrossing(endNeighbour))
                        untaggedIslandsBetweenCrossings.Add(new ProblematicWay(way, startNeighbour, endNeighbour, primaryTag));
                }
            }
        }

        // Report found issues
        
        if (untaggedIslandsBetweenCrossings.Count > 0)
        {
            foreach (ProblematicWay problem in untaggedIslandsBetweenCrossings)
            {
                report.AddEntry(
                    ReportGroup.IslandsBetweenCrossings,
                    new IssueReportEntry(
                        "Way " + problem.Way.OsmViewUrl +
                        " connects between crossing ways " +
                        problem.StartNeighbour.OsmViewUrl + " and " + problem.EndNeighbour.OsmViewUrl +
                        " but is not tagged as `" + problem.PrimaryTag + "=traffic_island`.",
                        problem.Way.AverageCoord,
                        MapPointStyle.Problem,
                        problem.Way
                    )
                );
            }
            
            foreach (ProblematicWay problem in islandsWithNonCrossingNeighbours)
            {
                report.AddEntry(
                    ReportGroup.IslandsBetweenCrossings,
                    new IssueReportEntry(
                        "Way " + problem.Way.OsmViewUrl +
                        " is tagged as `" + problem.PrimaryTag + "=traffic_island` but connects between non-crossing ways " +
                        problem.StartNeighbour.OsmViewUrl + " and " + problem.EndNeighbour.OsmViewUrl + ".",
                        problem.Way.AverageCoord,
                        MapPointStyle.Problem,
                        problem.Way
                    )
                );
            }
        }
    }


    [Pure]
    private static bool LooksLikeTrafficIsland(OsmWay way)
    {
        return 
            way.GetValue("footway") == "traffic_island" || 
            way.GetValue("cycleway") == "traffic_island";
    }

    [Pure]
    private static bool LooksLikeCrossing(OsmWay way)
    {
        return
            way.GetValue("footway") == "crossing" ||
            way.GetValue("cycleway") == "crossing";
    }


    private class ConnectingWayLookup
    {
        private readonly Dictionary<long, List<OsmWay>> _waysAtNode;
        
        
        public ConnectingWayLookup(IEnumerable<OsmWay> ways)
        {
            _waysAtNode = [ ];

            foreach (OsmWay way in ways)
            {
                OsmNode firstNode = way.Nodes[0];
                OsmNode lastNode = way.Nodes[^1];

                if (!_waysAtNode.ContainsKey(firstNode.Id))
                    _waysAtNode[firstNode.Id] = [ ];
                
                _waysAtNode[firstNode.Id].Add(way);

                
                if (!_waysAtNode.ContainsKey(lastNode.Id))
                    _waysAtNode[lastNode.Id] = [ ];
                
                _waysAtNode[lastNode.Id].Add(way);
            }
        }

        
        [Pure]
        public IEnumerable<OsmWay> GetConnectingWays(OsmNode node, OsmWay except)
        {
            if (!_waysAtNode.TryGetValue(node.Id, out List<OsmWay>? waysAtNode))
                return [ ];

            return waysAtNode.Except(except);
        }
    }
    
        
    private record ProblematicWay(OsmWay Way, OsmWay StartNeighbour, OsmWay EndNeighbour, string PrimaryTag);

    private enum ReportGroup
    {
        IslandsBetweenCrossings
    }
}