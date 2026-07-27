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

        const double trafficWayDistanceThreshold = 15;
        
        report.AddGroup(ReportGroup.CorrectTrafficIslands, "Correct traffic islands");

        report.AddEntry(
            ReportGroup.CorrectTrafficIslands,
            new DescriptionReportEntry(
                "Traffic island ways that appear to be properly mapped."
            )
        );

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

        report.AddGroup(ReportGroup.CrossingIslandTagging, "Crossing island tagging on crossings");

        report.AddEntry(
            ReportGroup.CrossingIslandTagging,
            new DescriptionReportEntry(
                "When crossings with traffic islands are mapped as separate ways for each road segment, " +
                "the crossing ways and crossing nodes should be tagged with `crossing:island=yes`."
            )
        );

        // Prepare to collect issues
        
        List<ProblematicIslandWay> correctTrafficIslands = [ ];
        List<ProblematicIslandWay> untaggedIslandsBetweenCrossings = [ ];
        List<ProblematicIslandWay> islandsWithNonCrossingNeighbours = [ ];
        List<ProblematicCrossingNode> crossingNodesWithoutIslandTag = [ ];
        List<ProblematicCrossingWay> crossingWaysWithoutIslandTag = [ ];

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
                if (TaggedAsCrossing(way))
                    continue;

                bool taggedAsTrafficIsland = TaggedAsTrafficIsland(way);

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
                
                // todo: extent along all ways to detect islands, like a triangle traffic island but it has 3 ways mapped - all should be islands

                double distance = OsmGeoTools.DistanceBetween(firstNode.coord, lastNode.coord);
                bool withinDistance = distance <= trafficWayDistanceThreshold;
                bool bothNeighboursTaggedAsCrossings = TaggedAsCrossing(startNeighbour) && TaggedAsCrossing(endNeighbour);
                
                if (taggedAsTrafficIsland)
                {
                    // If we are a traffic island, make sure we are between crossings
                    if (!TaggedAsCrossing(startNeighbour) || !TaggedAsCrossing(endNeighbour))
                        islandsWithNonCrossingNeighbours.Add(new ProblematicIslandWay(way, startNeighbour, endNeighbour, primaryTag, distance));
                    else
                        correctTrafficIslands.Add(new ProblematicIslandWay(way, startNeighbour, endNeighbour, primaryTag, distance));

                    // todo: report when very long?
                }
                else
                {
                    // We cannot distinguish split crossings from just ways between independent crossings,
                    // so apply a threshold to avoid reporting ways that are more than likely not part of the same crossing "group" 
                    if (withinDistance) // meters
                    {
                        // If we are not a traffic island, make sure we are not between (two) crossings
                        if (bothNeighboursTaggedAsCrossings)
                            untaggedIslandsBetweenCrossings.Add(new ProblematicIslandWay(way, startNeighbour, endNeighbour, primaryTag, distance));
                    }
                }

                // todo: refuge island way itself tagged with crossing:island=yes (or other crossing tags)
                
                if (withinDistance && bothNeighboursTaggedAsCrossings) // even if not tagged (yet) as traffic island
                {
                    OsmNode? startCrossingNode = GetCrossingNode(startNeighbour);
                    OsmNode? endCrossingNode = GetCrossingNode(endNeighbour);

                    // todo: report if node is terminal
                        
                    // todo: report if no nodes are available

                    CheckTrafficIslandTaggingOnWay(startNeighbour);
                    CheckTrafficIslandTaggingOnWay(endNeighbour);
                    if (startCrossingNode != null) CheckTrafficIslandTaggingOnNode(startCrossingNode, startNeighbour);
                    if (endCrossingNode != null) CheckTrafficIslandTaggingOnNode(endCrossingNode, endNeighbour);
                        
                    void CheckTrafficIslandTaggingOnNode(OsmNode crossingNode, OsmWay nodeWay)
                    {
                        string? islandValue = crossingNode.GetValue("crossing:island");

                        if (islandValue != "no")
                        {
                            // Skip already-reported nodes, which can happen with multiple-island crossings
                            if (crossingNodesWithoutIslandTag.Any(n => n.Node == crossingNode))
                                return;
                                
                            crossingNodesWithoutIslandTag.Add(new ProblematicCrossingNode(crossingNode, nodeWay, islandValue));
                        }
                    }

                    void CheckTrafficIslandTaggingOnWay(OsmWay crossingWay)
                    {
                        string? islandValue = crossingWay.GetValue("crossing:island");

                        if (islandValue != "no")
                        {
                            // Skip already-reported ways, which can happen with multiple-island crossings
                            if (crossingWaysWithoutIslandTag.Any(w => w.Way == crossingWay))
                                return;

                            crossingWaysWithoutIslandTag.Add(new ProblematicCrossingWay(crossingWay, islandValue));
                        }
                    }
                    
                    // todo: merge into single entry for both node and way
                }
            }
        }
        
        // Report found issues
        
        if (correctTrafficIslands.Count > 0)
        {
            foreach (ProblematicIslandWay problem in correctTrafficIslands)
            {
                report.AddEntry(
                    ReportGroup.CorrectTrafficIslands,
                    new IssueReportEntry(
                        "Way " + problem.Way.OsmViewUrl +
                        " connects at " + problem.Distance.ToString("F0") + "m between crossing ways " +
                        problem.StartNeighbour.OsmViewUrl + " and " + problem.EndNeighbour.OsmViewUrl +
                        " and is tagged as expected with `" + problem.PrimaryTag + "=traffic_island`.",
                        problem.Way.AverageCoord,
                        MapPointStyle.Okay,
                        problem.Way
                    )
                );
            }
        }
        
        foreach (ProblematicIslandWay problem in untaggedIslandsBetweenCrossings)
        {
            report.AddEntry(
                ReportGroup.IslandsBetweenCrossings,
                new IssueReportEntry(
                    "Way " + problem.Way.OsmViewUrl +
                    " connects at " + problem.Distance.ToString("F0") + "m between crossing ways " +
                    problem.StartNeighbour.OsmViewUrl + " and " + problem.EndNeighbour.OsmViewUrl +
                    " but is not tagged as `" + problem.PrimaryTag + "=traffic_island`.",
                    problem.Way.AverageCoord,
                    MapPointStyle.Problem,
                    problem.Way
                )
            );
        }
        
        foreach (ProblematicIslandWay problem in islandsWithNonCrossingNeighbours)
        {
            report.AddEntry(
                ReportGroup.IslandsBetweenCrossings,
                new IssueReportEntry(
                    "Way " + problem.Way.OsmViewUrl +
                    " is tagged as `" + problem.PrimaryTag + "=traffic_island` " +
                    "but connects at " + problem.Distance.ToString("F0") + "m between non-crossing ways " +
                    problem.StartNeighbour.OsmViewUrl + " and " + problem.EndNeighbour.OsmViewUrl + ".",
                    problem.Way.AverageCoord,
                    MapPointStyle.Problem,
                    problem.Way
                )
            );
        }

        foreach (ProblematicCrossingNode problem in crossingNodesWithoutIslandTag)
        {
            report.AddEntry(
                ReportGroup.CrossingIslandTagging,
                new IssueReportEntry(
                    "Crossing node " + problem.Node.OsmViewUrl + " " +
                    "on a crossing way " + problem.NodeWay.OsmViewUrl +
                    " appears to be part of separately-mapped crossings with a traffic island " +
                    " but is not tagged as `crossing:island=no`, " +
                    "instead " + (problem.IslandValue != null ? "`" + problem.IslandValue + "`" : "no value") + ".",
                    problem.Node.coord,
                    problem.IslandValue != null ? MapPointStyle.Problem : MapPointStyle.Dubious, // value is technically not required
                    problem.Node
                )
            );
        }
        
        foreach (ProblematicCrossingWay problem in crossingWaysWithoutIslandTag)
        {
            report.AddEntry(
                ReportGroup.CrossingIslandTagging,
                new IssueReportEntry(
                    "Crossing way " + problem.Way.OsmViewUrl + " " +
                    "appears to be part of separately-mapped crossings with a traffic island " +
                    " but is not tagged as `crossing:island=no`, " +
                    "instead " + (problem.IslandValue != null ? "`" + problem.IslandValue + "`" : "no value") + ".",
                    problem.Way.AverageCoord,
                    problem.IslandValue != null ? MapPointStyle.Problem : MapPointStyle.Dubious, // value is technically not required
                    problem.Way
                )
            );
        }
    }


    [Pure]
    private static bool TaggedAsTrafficIsland(OsmWay way)
    {
        return 
            way.GetValue("footway") == "traffic_island" || 
            way.GetValue("cycleway") == "traffic_island";
    }

    [Pure]
    private static bool TaggedAsCrossing(OsmWay way)
    {
        return
            way.GetValue("footway") == "crossing" ||
            way.GetValue("cycleway") == "crossing";
    }

    [Pure]
    private static OsmNode? GetCrossingNode(OsmWay way)
    {
        foreach (OsmNode node in way.Nodes)
            if (node.GetValue("highway") == "crossing")
                return node;
        
        return null;
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
    
        
    private record ProblematicIslandWay(OsmWay Way, OsmWay StartNeighbour, OsmWay EndNeighbour, string PrimaryTag, double Distance);
    
    private record ProblematicCrossingNode(OsmNode Node, OsmWay NodeWay, string? IslandValue);
    
    private record ProblematicCrossingWay(OsmWay Way, string? IslandValue);

    
    private enum ReportGroup
    {
        CorrectTrafficIslands,
        IslandsBetweenCrossings,
        CrossingIslandTagging
    }
}