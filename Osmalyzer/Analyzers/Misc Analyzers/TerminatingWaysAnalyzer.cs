using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class TerminatingWaysAnalyzer : Analyzer
{
    public override string Name => "Terminating Ways";

    public override string Description => "This report shows locations where ways terminate at the edge of areas and do not route through it.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(LatviaOsmAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract areas = osmMasterData.Filter(
            new IsClosedWay(),
            new OrMatch(
                new HasValue("amenity", "parking"),
                new HasValue("place", "square"),
                new AndMatch(
                    new HasValue("highway", "pedestrian"),
                    new HasValue("area", "yes")
                )
            )
        );
        
        // TODO: MULTIPOLYGONS
        

        // Prepare groups

        report.AddGroup(ReportGroup.Terminating, "Terminating ways");

        report.AddEntry(
            ReportGroup.Terminating,
            new DescriptionReportEntry(
                "These way-area intersection locations likely should interconnect and route within the area. There are many false positives due to the many combinations how these get drawn both correctly and incorrectly."
            )
        );
        
        // Parse

        List<BadTermination> badTerminations = new List<BadTermination>();
            
        foreach (OsmWay area in areas.Ways)
        {
            List<TerminationPoint>? points = null;

            for (int i = 0; i < area.Nodes.Count; i++)
            {
                OsmNode edgeNode = area.Nodes[i];

                if (i == area.Nodes.Count - 1 && edgeNode == area.Nodes[0])
                    continue; // skip the last same node as first
                
                if (edgeNode.Ways != null)
                {
                    List<OsmWay> waysTerminatingAtNode = new List<OsmWay>();
                    List<OsmWay> waysPassingThroughNode = new List<OsmWay>();

                    foreach (OsmWay way in edgeNode.Ways.Where(IsWayRoutable))
                    {
                        if (WayTerminatesAtEdge(way, edgeNode, area))
                            waysTerminatingAtNode.Add(way);

                        else if (WayPassesThroughEdge(way, edgeNode, area))
                            waysPassingThroughNode.Add(way);
                    }

                    if (waysTerminatingAtNode.Count == 1 && waysPassingThroughNode.Count == 0)
                    {
                        if (points == null)
                            points = new List<TerminationPoint>();

                        points.Add(new TerminationPoint(waysTerminatingAtNode[0], edgeNode));
                    }
                }
            }

            if (points != null)
            {
                badTerminations.Add(new BadTermination(area, points));
            }
        }
        
        [Pure]
        static bool IsWayRoutable(OsmWay way)
        {
            string? highwayValue = way.GetValue("highway");
            
            if (highwayValue != null)
                if (OsmKnowledge.IsRoutableHighwayValue(highwayValue))
                    return true;
                
            return false;
        }
        
        [Pure]
        static bool WayTerminatesAtEdge(OsmWay way, OsmNode edgeNode, OsmWay area)
        {
            if (way.Nodes.Count < 2)
                return false; // degenerate case
            
            if (way.Nodes[0] == edgeNode &&
                way.Nodes.Skip(1).All(n => !area.Nodes.Contains(n)))
                return true;
            
            if (way.Nodes[^1] == edgeNode &&
                way.Nodes.Take(way.Nodes.Count - 1).All(n => !area.Nodes.Contains(n)))
                return true;
            
            return false;
        }
        
        [Pure]
        static bool WayPassesThroughEdge(OsmWay way, OsmNode edgeNode, OsmWay area)
        {
            if (way.Nodes.Count < 2)
                return false; // degenerate case
            
            if (way.Nodes.Any(n => area.Nodes.Contains(n)))
                return true;
            
            return false;
        }
        

        if (badTerminations.Count > 0)
        {
            foreach (BadTermination badConnection in badTerminations)
            {
                report.AddEntry(
                    ReportGroup.Terminating,
                    new IssueReportEntry(
                        OsmKnowledge.GetFeatureLabel(badConnection.Area, "Area", true) + "  " + badConnection.Area.OsmViewUrl + 
                        " has " + badConnection.Points.Count + " unrouted terminating ways: " + 
                        string.Join("; ", badConnection.Points.Select(p => p.Way.OsmViewUrl + " at " + p.Node.OsmViewUrl)),
                        badConnection.Area.GetAverageCoord(),
                        MapPointStyle.Problem
                    )
                );
            }
        }
        else
        {
            report.AddEntry(
                ReportGroup.Terminating,
                new GenericReportEntry(
                    "No unroutable ways terminating at areas found."
                )
            );
        }
    }


    private record BadTermination(OsmWay Area, List<TerminationPoint> Points);
    
    private record TerminationPoint(OsmWay Way, OsmNode Node);
    
    
    private enum ReportGroup
    {
        Terminating
    }
}