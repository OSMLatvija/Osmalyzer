namespace Osmalyzer;

[UsedImplicitly]
public class SharpAngleRoadAnalyzer : Analyzer
{
    public override string Name => "Sharp-angled Roads";

    public override string Description => "This report finds road junctions where two roads meet at an extremely sharp angles, " +
                                          "which likely indicates either a drawing error or a missing turn restriction.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;


    /// <summary> Angle threshold in degrees -- connections at or below this are considered sharp </summary>
    private const double maxSharpAngle = 30;

    /// <summary>
    /// Vehicle-routable highway values.
    /// Excludes footway/path/cycleway/steps/bridleway -- sharp angles are irrelevant for pedestrian/bicycle routing.
    /// </summary>
    private static readonly string[] _routableHighwayValues =
    [
        "motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential",
        "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link",
        "living_street", "service", "track"
    ];


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData osmMasterData = osmData.MasterData;

        OsmData roads = osmMasterData.Filter(
            new IsWay(),
            new HasAnyValue("highway", _routableHighwayValues),
            new DoesntHaveValue("area", "yes"), // exclude highway areas
            new DoesntHaveAnyValue("access", "no", "private", "destination") // inaccessible to regular vehicles, sharp angle is irrelevant
        );

        // Build node-to-ways index for shared junction nodes

        Dictionary<long, List<OsmWay>> nodeToWays = new Dictionary<long, List<OsmWay>>();

        foreach (OsmWay road in roads.Ways)
        {
            if (road.Nodes.Count < 2)
                continue;

            // Only consider end nodes of each way -- mid-way nodes are just shape points where the road continues
            // But also consider any node shared by another way
            foreach (OsmNode node in road.Nodes)
            {
                long nodeId = node.Id;

                if (!nodeToWays.TryGetValue(nodeId, out List<OsmWay>? wayList))
                {
                    wayList = [ ];
                    nodeToWays[nodeId] = wayList;
                }

                if (!wayList.Contains(road))
                    wayList.Add(road);
            }
        }

        // Load all turn restriction relations for quick lookup

        OsmData restrictionRelations = osmMasterData.Filter(
            new IsRelation(),
            new HasAnyValue("type", "restriction")
        );

        Dictionary<long, List<OsmRelation>> viaNodeRestrictions = BuildViaNodeRestrictionIndex(restrictionRelations);

        // Prepare groups

        report.AddGroup(
            ReportGroup.Sharp,
            "Very sharp angles",
            "Road connections at sharp acute angles (≤" + maxSharpAngle + "°) that should be reviewed."
        );

        report.AddGroup(
            ReportGroup.WithRestrictions,
            "Sharp angles with turn restrictions",
            "Road connections at sharp angles that already have a turn restriction, so routing should be fine. Listed for reference."
        );

        report.AddGroup(
            ReportGroup.Stats,
            "Statistics"
        );

        // Analyze junctions

        // Track already-reported pairs so we don't duplicate (way A + way B at node N = way B + way A at node N)
        HashSet<(long, long, long)> reportedPairs = [ ];

        int sharpCount = 0;
        int restrictedCount = 0;
        int skippedOnewayCount = 0;

        foreach ((long nodeId, List<OsmWay> waysAtNode) in nodeToWays)
        {
            if (waysAtNode.Count < 2)
                continue;

            OsmNode? junctionNode = FindNodeById(waysAtNode, nodeId);
            if (junctionNode == null)
                continue;

            for (int i = 0; i < waysAtNode.Count; i++)
            {
                for (int j = i + 1; j < waysAtNode.Count; j++)
                {
                    OsmWay wayA = waysAtNode[i];
                    OsmWay wayB = waysAtNode[j];

                    // Get adjacent node in each way relative to the junction
                    OsmCoord? adjA = GetAdjacentCoord(wayA, junctionNode);
                    OsmCoord? adjB = GetAdjacentCoord(wayB, junctionNode);

                    if (adjA == null || adjB == null)
                        continue;

                    double angle = OsmGeoTools.AngleBetweenSegments(adjA.Value, junctionNode.coord, adjB.Value);

                    if (angle > maxSharpAngle)
                        continue;

                    // Deduplicate
                    long pairKey1 = Math.Min(wayA.Id, wayB.Id);
                    long pairKey2 = Math.Max(wayA.Id, wayB.Id);
                    if (!reportedPairs.Add((pairKey1, pairKey2, nodeId)))
                        continue;

                    // Check if oneway traffic flow means no actual turn is needed

                    if (IsOnewayFlowCompatible(wayA, wayB, junctionNode))
                    {
                        skippedOnewayCount++;
                        continue;
                    }

                    // Check for existing turn restriction at this junction

                    bool hasRestriction = HasTurnRestrictionAtJunction(viaNodeRestrictions, junctionNode, wayA, wayB);

                    // Report

                    string angleStr = angle.ToString("F1") + "°";

                    string text =
                        angleStr + " angle at " + junctionNode.OsmViewUrl + " between " +
                        OsmKnowledge.GetFeatureLabel(wayA, "highway", false) + " " + 
                        (wayA.GetValue("name") != null ? "`" + wayA.GetValue("name") + "` " : "") +
                        wayA.OsmViewUrl +
                        " and " +
                        OsmKnowledge.GetFeatureLabel(wayB, "highway", false) + " " + 
                        (wayB.GetValue("name") != null ? "`" + wayB.GetValue("name") + "` " : "") +
                        wayB.OsmViewUrl;

                    // Sort by best road class first, then by worst -- so trunk+service sorts above trunk+residential,
                    // and both sort above primary+service; zero-padded so string comparison works correctly
                    int rankA = GetHighwayRank(wayA);
                    int rankB = GetHighwayRank(wayB);
                    string sortKey = Math.Min(rankA, rankB).ToString("D2") + "_" + Math.Max(rankA, rankB).ToString("D2");
                    SortEntryAsc sortRule = new SortEntryAsc(sortKey);

                    if (hasRestriction)
                    {
                        text += " (has turn restriction)";

                        report.AddEntry(
                            ReportGroup.WithRestrictions,
                            new GenericReportEntry(text, sortRule)
                        );

                        restrictedCount++;
                    }
                    else
                    {
                        report.AddEntry(
                            ReportGroup.Sharp,
                            new IssueReportEntry(
                                text,
                                sortRule,
                                junctionNode.coord,
                                MapPointStyle.Problem
                            )
                        );

                        sharpCount++;
                    }
                }
            }
        }

        // If no entries, add placeholder

        if (sharpCount == 0)
            report.AddEntry(
                ReportGroup.Sharp,
                new GenericReportEntry("No sharp angle connections found.")
            );

        if (restrictedCount == 0)
            report.AddEntry(
                ReportGroup.WithRestrictions,
                new GenericReportEntry("No sharp angle connections with turn restrictions found.")
            );

        // Stats

        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                "Unrestricted: " + sharpCount + "; " +
                "with restrictions: " + restrictedCount + "; " +
                "oneway flow: " + skippedOnewayCount
            )
        );
    }


    /// <summary>
    /// Gets the coordinate of the node adjacent to <paramref name="junctionNode"/> in <paramref name="way"/>.
    /// If the junction node appears at an end, the adjacent is the next/previous node.
    /// If it appears in the middle, we need to consider both directions -- we return the one that
    /// creates the smallest angle opportunity (both sides are separate potential turns, we check both elsewhere via pair iteration).
    /// For mid-way nodes, returns null since both segments of the same way continue through -- no turn happens here for this way.
    /// </summary>
    [Pure]
    private static OsmCoord? GetAdjacentCoord(OsmWay way, OsmNode junctionNode)
    {
        IReadOnlyList<OsmNode> nodes = way.Nodes;

        // Find all positions of junction node in this way
        List<int> positions = [ ];
        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i] == junctionNode)
                positions.Add(i);

        if (positions.Count == 0)
            return null;

        // For end nodes, return the adjacent node's coord
        foreach (int pos in positions)
        {
            bool isFirst = pos == 0;
            bool isLast = pos == nodes.Count - 1;

            // For closed ways, first == last, treat as "passes through"
            if (way.Closed && (isFirst || isLast))
                continue;

            if (isFirst)
                return nodes[1].coord;

            if (isLast)
                return nodes[^2].coord;
        }

        // Junction node is only in the middle of this way -- the way passes through this node,
        // it doesn't terminate here, so it's not a "turn" for this way
        return null;
    }


    /// <summary>
    /// Returns true if no valid travel direction exists through the sharp angle at the junction,
    /// meaning the junction can be ignored because oneway constraints make all turns physically impossible.
    /// A travel direction (arrive on X, depart on Y) is valid only if:
    /// X allows arriving (not oneway flowing away from junction) AND Y allows departing (not oneway flowing into junction).
    /// </summary>
    [Pure]
    private static bool IsOnewayFlowCompatible(OsmWay wayA, OsmWay wayB, OsmNode junctionNode)
    {
        // flow: +1 = away from junction (can depart, cannot arrive), -1 = into junction (can arrive, cannot depart), 0 = bidirectional
        int flowA = GetOnewayFlow(wayA, junctionNode);
        int flowB = GetOnewayFlow(wayB, junctionNode);

        bool canArriveOnA = flowA != +1;  // blocked from arriving if it flows away
        bool canDepartOnA = flowA != -1;  // blocked from departing if it flows into junction
        bool canArriveOnB = flowB != +1;
        bool canDepartOnB = flowB != -1;

        // Check each possible travel direction through the sharp angle
        bool aThenB = canArriveOnA && canDepartOnB;
        bool bThenA = canArriveOnB && canDepartOnA;

        // If no valid travel direction exists, the sharp angle is physically unreachable -- skip it
        return !aThenB && !bThenA;
    }


    /// <summary>
    /// Returns the oneway flow direction relative to the junction node.
    /// +1 = traffic flows away from junction node (junction is the start of travel).
    /// -1 = traffic flows into the junction node (junction is the end of travel).
    /// 0 = not a oneway road.
    /// </summary>
    [Pure]
    private static int GetOnewayFlow(OsmWay way, OsmNode junctionNode)
    {
        string? oneway = way.GetValue("oneway");

        // junction=roundabout and highway=motorway/motorway_link imply oneway in node order, same as oneway=yes
        if (oneway is null or "no")
        {
            if (!way.HasValue("junction", "roundabout") &&
                !way.HasValue("highway", "motorway", "motorway_link"))
                return 0;
        }

        bool isReversed = oneway == "-1";
        // oneway=yes means travel in the direction of the way's node order

        bool junctionIsFirst = way.Nodes[0] == junctionNode;
        bool junctionIsLast = way.Nodes[^1] == junctionNode;

        if (!junctionIsFirst && !junctionIsLast)
            return 0; // junction is in the middle, not relevant

        if (isReversed)
        {
            // Traffic flows opposite to node order
            // If junction is first node, traffic flows toward it (into junction)
            // If junction is last node, traffic flows away from it
            return junctionIsFirst ? -1 : +1;
        }

        // Normal direction: traffic flows in node order
        // If junction is first node, traffic flows away from it
        // If junction is last node, traffic flows into it
        return junctionIsFirst ? +1 : -1;
    }


    /// <summary>
    /// Builds an index of turn restriction relations keyed by via node ID
    /// </summary>
    [Pure]
    private static Dictionary<long, List<OsmRelation>> BuildViaNodeRestrictionIndex(OsmData restrictionRelations)
    {
        Dictionary<long, List<OsmRelation>> index = new Dictionary<long, List<OsmRelation>>();

        foreach (OsmRelation relation in restrictionRelations.Relations)
        {
            foreach (OsmRelationMember member in relation.Members)
            {
                if (member.Role == "via" && member.Element is OsmNode viaNode)
                {
                    if (!index.TryGetValue(viaNode.Id, out List<OsmRelation>? list))
                    {
                        list = [ ];
                        index[viaNode.Id] = list;
                    }

                    list.Add(relation);
                }
            }
        }

        return index;
    }


    /// <summary>
    /// Checks if the sharp-angle turn between <paramref name="wayA"/> and <paramref name="wayB"/>
    /// is effectively restricted at the junction. This considers the actual restriction type:
    /// <c>no_*</c> restrictions explicitly prohibit a from→to turn;
    /// <c>only_*</c> restrictions implicitly prohibit all turns from the <c>from</c> way except to the <c>to</c> way;
    /// <c>no_entry</c>/<c>no_exit</c> block passage entirely.
    /// A sharp-angle pair is considered "covered" if the turn is restricted in at least one direction.
    /// </summary>
    [Pure]
    private static bool HasTurnRestrictionAtJunction(
        Dictionary<long, List<OsmRelation>> viaNodeRestrictions,
        OsmNode junctionNode,
        OsmWay wayA,
        OsmWay wayB)
    {
        if (!viaNodeRestrictions.TryGetValue(junctionNode.Id, out List<OsmRelation>? restrictions))
            return false;

        foreach (OsmRelation restriction in restrictions)
        {
            string? restrictionValue = GetRestrictionValue(restriction);
            if (restrictionValue == null)
                continue;

            // Collect from/to members
            List<long> fromWayIds = new List<long>();
            List<long> toWayIds = new List<long>();

            foreach (OsmRelationMember member in restriction.Members)
            {
                if (member.Element is not OsmWay memberWay)
                    continue;

                if (member.Role == "from")
                    fromWayIds.Add(memberWay.Id);
                else if (member.Role == "to")
                    toWayIds.Add(memberWay.Id);
            }

            // no_entry / no_exit -- blocks passage entirely for one of our ways
            if (restrictionValue is "no_entry" or "no_exit")
            {
                bool involvesA = fromWayIds.Contains(wayA.Id) || toWayIds.Contains(wayA.Id);
                bool involvesB = fromWayIds.Contains(wayB.Id) || toWayIds.Contains(wayB.Id);

                if (involvesA || involvesB)
                    return true;
            }

            // no_* (no_left_turn, no_right_turn, no_straight_on, no_u_turn)
            // Explicitly bans the from→to turn

            if (restrictionValue.StartsWith("no_"))
            {
                // A→B banned?
                if (fromWayIds.Contains(wayA.Id) && toWayIds.Contains(wayB.Id))
                    return true;

                // B→A banned?
                if (fromWayIds.Contains(wayB.Id) && toWayIds.Contains(wayA.Id))
                    return true;
            }

            // only_* (only_left_turn, only_right_turn, only_straight_on, only_u_turn)
            // From the `from` way, only the `to` way is allowed -- everything else is implicitly banned

            if (restrictionValue.StartsWith("only_"))
            {
                // If `from` is one of our ways and `to` is NOT the other, the turn to the other is banned
                if (fromWayIds.Contains(wayA.Id) && !toWayIds.Contains(wayB.Id))
                    return true;

                if (fromWayIds.Contains(wayB.Id) && !toWayIds.Contains(wayA.Id))
                    return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Gets the main restriction value from a restriction relation (e.g. <c>no_left_turn</c>).
    /// Checks <c>restriction</c> tag and mode-specific <c>restriction:*</c> tags.
    /// </summary>
    [Pure]
    private static string? GetRestrictionValue(OsmRelation restriction)
    {
        if (restriction.AllTags == null)
            return null;

        foreach ((string key, string value) in restriction.AllTags)
        {
            // Main restriction tag or mode-specific (e.g. restriction:hgv)
            if (key == "restriction" || key.StartsWith("restriction:"))
            {
                // Skip conditional tags here -- they have complex time-based values
                if (key.Contains("conditional"))
                    continue;

                // Value could be "none" which means no restriction (used with conditional)
                if (value == "none")
                    continue;

                return value;
            }
        }

        return null;
    }


    /// <summary>
    /// Returns a sort rank for the given way based on its <c>highway</c> value.
    /// Lower number = more important road class, so entries are sorted with higher-importance roads first.
    /// Link roads rank just below their parent class. <see cref="_routableHighwayValues"/> that don't appear here get the fallback rank.
    /// </summary>
    [Pure]
    private static int GetHighwayRank(OsmWay way)
    {
        return way.GetValue("highway") switch
        {
            "motorway"       => 0,
            "motorway_link"  => 1,
            "trunk"          => 2,
            "trunk_link"     => 3,
            "primary"        => 4,
            "primary_link"   => 5,
            "secondary"      => 6,
            "secondary_link" => 7,
            "tertiary"       => 8,
            "tertiary_link"  => 9,
            "unclassified"   => 10,
            "residential"    => 11,
            "living_street"  => 12,
            "service"        => 13,
            "track"          => 14,
            _                => 15
        };
    }


    [Pure]
    private static OsmNode? FindNodeById(List<OsmWay> ways, long nodeId)
    {
        foreach (OsmWay way in ways)
            foreach (OsmNode node in way.Nodes)
                if (node.Id == nodeId)
                    return node;

        return null;
    }


    private enum ReportGroup
    {
        Sharp,
        WithRestrictions,
        Stats
    }
}

