using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class TerminatingWaysAnalyzer : Analyzer
{
    public override string Name => "Terminating Ways";

    public override string Description => "This report shows locations where ways terminate at the edge of areas and do not route through it.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract areas = osmMasterData.Filter(
            new IsWay(),
            new HasValue("amenity", "parking")
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
            bool foundCrossing = false; // todo: but that won't find these if the ways don't connect/touch...
            
            foreach (OsmNode edgeNode in area.Nodes)
            {
                if (edgeNode.Ways != null)
                {
                    // Try to find one and only one way which terminates at this edge node
                    OsmWay? singleTerminatingWay = null;
                    bool multipleTerminatingWays = false;

                    foreach (OsmWay way in edgeNode.Ways)
                    {
                        if (IsWayRoutable(way))
                        {
                            if (edgeNode == way.Nodes[0] || edgeNode == way.Nodes[^1])
                            {
                                if (!multipleTerminatingWays) // else, already found multiple terminating, but can still look for crossings
                                {
                                    if (singleTerminatingWay != null)
                                    {
                                        // We already had found another terminating way, so this is a second one, thus they route to each other, we can stop now
                                        multipleTerminatingWays = true;
                                        singleTerminatingWay = null; // we no longer have a SINGLE way
                                    }
                                    else
                                    {
                                        singleTerminatingWay = way;
                                    }
                                }
                            }
                            else
                            {
                                foundCrossing = true;
                            }
                        }
                    }

                    if (singleTerminatingWay != null) // implies not multipleTerminatingWays
                    {
                        if (points == null)
                            points = new List<TerminationPoint>();
                                
                        points.Add(new TerminationPoint(singleTerminatingWay, edgeNode));
                    }
                }
            }

            if (points != null && (points.Count > 1 || foundCrossing))
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
        

        if (badTerminations.Count > 0)
        {
            foreach (BadTermination badConnection in badTerminations)
            {
                report.AddEntry(
                    ReportGroup.Terminating,
                    new IssueReportEntry(
                        "Area has " + badConnection.Points.Count + " unrouted terminating ways: " + badConnection.Area.OsmViewUrl + " - " + string.Join("; ", badConnection.Points.Select(p => p.Way.OsmViewUrl + " at " + p.Node.OsmViewUrl)),
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