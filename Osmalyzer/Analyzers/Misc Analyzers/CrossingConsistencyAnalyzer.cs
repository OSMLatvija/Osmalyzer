namespace Osmalyzer;

[UsedImplicitly]
public class CrossingConsistencyAnalyzer : Analyzer
{
    public override string Name => "Crossing Consistency";

    public override string Description => "This report checks that crossings have consistent tags between their node and way.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;

        OsmData ways = OsmData.Filter(
            new IsWay(),
            new OrMatch(
                new HasValue("highway", "path"),
                new HasValue("highway", "footway")
            ),
            new HasValue("footway", "crossing")
        );
        
        OsmData points = OsmData.Filter(
            new IsNode(),
            new HasValue("highway", "crossing")
        );

        // Prepare groups

        report.AddGroup(ReportGroup.MismatchedBad, "Inconsistent crossings");

        report.AddEntry(
            ReportGroup.MismatchedBad,
            new DescriptionReportEntry(
                "These crossing way-node pairs have tag values that do not match each other. " +
                "These are most likely errors, because almost all values should match since they represent the same crossing."
            )
        );
        
        report.AddGroup(ReportGroup.MismatchedCommon, "Inconsistent crossings (known)");

        report.AddEntry(
            ReportGroup.MismatchedCommon,
            new DescriptionReportEntry(
                "Same as above section, but these are a common known variation that is left over from earlier tagging before crossing tag standartization. " +
                "Because there are so many of the same type, these are in a separate list. " +
                "This includes: way being `crossing=marked` but node being `crossing=traffic_signals` representing zebra surface + signals that should instead both be `crossing=traffic_signals` + `crossing:markings=zebra`."
            )
        );
        
        // Parse
        
        List<Crossing> crossings = GatherCrossings(ways, points);
        
        
        List<ProblematicCrossing> problematicCrossings = new List<ProblematicCrossing>();
        

        string[] crossingTags =
        {
            "crossing",
            "crossing:markings",
            "crossing:island",
            "tactile_paving",
            "lit",
            "button_operated",
            "traffic_signals:sound",
            "traffic_signals:vibration",
            "button_operated",
            "traffic_calming"
        };
        
        foreach (Crossing crossing in crossings)
        {
            List<CrossingIssue> issues = new List<CrossingIssue>();

            foreach (string tag in crossingTags)
            {
                string? wayValue = crossing.Way.GetValue(tag);
                string? nodeValue = crossing.Node.GetValue(tag);

                if (wayValue != null && nodeValue != null && !TagUtils.ValuesMatch(wayValue, nodeValue))
                    if (!SpecialAllowedCase())
                        issues.Add(new MismatchValueIssue(tag, wayValue, nodeValue));
                
                continue;

                
                bool SpecialAllowedCase()
                {
                    // Tactile paving isn't continuous along the way, but there is (some) tactile paving at the kerbs/edges
                    if (tag == "tactile_paving")
                        if (wayValue == "no")
                            if (nodeValue is "yes" or "incorrect")
                                return true;

                    return false;
                }
            }
            
            if (issues.Count > 0)
                problematicCrossings.Add(new ProblematicCrossing(crossing, CalculateSeverity(issues), issues));
            
            static Severity CalculateSeverity(List<CrossingIssue> issues)
            {
                if (issues.Count > 1)
                    return Severity.Bad;
                
                // Special case: crossing - marked vs traffic_signals.
                if (issues.Any(i => i is MismatchValueIssue { Key: "crossing", WayValue: "marked", NodeValue: "traffic_signals" }))
                    return Severity.Common;

                return Severity.Bad;
            }
        }
        

        if (problematicCrossings.Count > 0)
        {
            foreach (ProblematicCrossing problematicCrossing in problematicCrossings)
            {
                report.AddEntry(
                    problematicCrossing.Severity == Severity.Bad ? ReportGroup.MismatchedBad : ReportGroup.MismatchedCommon,
                    new IssueReportEntry(
                        "Crossing way-node pair " + problematicCrossing.Crossing.Way.OsmViewUrl + " - " + problematicCrossing.Crossing.Node.OsmViewUrl + 
                        " has " + ProblemDescription(problematicCrossing) + ".",
                        problematicCrossing.Crossing.Node.coord,
                        MapPointStyle.Problem
                    )
                );

                [Pure]
                static string ProblemDescription(ProblematicCrossing problem)
                {
                    if (problem.Issues.Count == 1)
                        return IssueDescription(problem.Issues[0]);

                    return "these issues: " + string.Join("; ", problem.Issues.Select(IssueDescription));

                    [Pure]
                    static string IssueDescription(CrossingIssue issue)
                    {
                        switch (issue)
                        {
                            case MismatchValueIssue mvi:
                                return "mismatched values for `" + mvi.Key + "` - `" + mvi.WayValue + "` vs `" + mvi.NodeValue + "`";
                            
                            default:
                                throw new ArgumentOutOfRangeException(nameof(issue));
                        }
                    }
                }
            }
        }
    }


    private static List<Crossing> GatherCrossings(OsmData ways, OsmData points)
    {
        List<Crossing> crossings = new List<Crossing>();
        
        Dictionary<long, OsmNode> sortedPoints = points.Nodes.ToDictionary(n => n.Id);
        
        foreach (OsmWay way in ways.Ways)
        {
            List<OsmNode>? matchedNodes = null;
            
            foreach (OsmNode wayNode in way.Nodes)
            {
                if (sortedPoints.ContainsKey(wayNode.Id))
                {
                    if (matchedNodes == null)
                        matchedNodes = new List<OsmNode>();
                    
                    matchedNodes.Add(wayNode);
                }
            }

            if (matchedNodes != null)
                if (matchedNodes.Count == 1)
                    crossings.Add(new Crossing(way, matchedNodes[0]));
        }

        return crossings;
    }


    private record Crossing(OsmWay Way, OsmNode Node);
    

    private record MismatchValueIssue(string Key, string WayValue, string NodeValue) : CrossingIssue;
    
    private abstract record CrossingIssue;
    
    
    private record ProblematicCrossing(Crossing Crossing, Severity Severity, List<CrossingIssue> Issues);


    private enum Severity
    {
        Bad,
        Common
    }
        
    private enum ReportGroup
    {
        MismatchedBad,
        MismatchedCommon
    }
}