using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class BarrierConnectionAnalyzer : Analyzer
{
    public override string Name => "Barrier Connections";

    public override string Description => "This report checks how barriers and other ways are connected and if there is a potential (routing) problem.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(LatviaOsmAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract barriers = osmMasterData.Filter(
            new IsWay(),
            new HasKey("barrier")
        );

        // Prepare groups

        report.AddGroup(ReportGroup.Misconnected, "Way-barrier intersections");

        report.AddEntry(
            ReportGroup.Misconnected,
            new DescriptionReportEntry(
                "These may need to not connect or have a proper connecting node like a gate, or some other adjustement. Some may be correct, although such cases are rare in real-world."
            )
        );
        
        // Parse

        foreach (OsmElement barrier in barriers.Elements)
        {
            if (barrier is OsmWay barrierWay)
            {
                string barrierValue = barrier.GetValue("barrier")!;
                  
                // If the way is a gate, then we can assume the connection is passable 
                switch (barrierValue)
                {
                    case "gate":
                    case "wicket_gate":
                    case "lift_gate":
                    case "swing_gate":
                    case "sliding_gate":
                    case "kissing_gate":
                    case "entrance":
                    case "cattle_grid":
                    case "chain":
                    case "sally_port":
                        continue;
                }

                foreach (OsmNode barrierNode in barrierWay.Nodes)
                {
                    if (barrierNode.HasKey("barrier")) // gate or something
                        continue;
                    
                    if (barrierNode.Ways != null)
                    {
                        foreach (OsmWay barrierNodeWay in barrierNode.Ways)
                        {
                            if (barrierNodeWay != barrier)
                            {
                                if (barrierNodeWay.HasKey("highway"))
                                {
                                    OsmWay highway = barrierNodeWay;
                                    
                                    // Ignore explicit areas as they might connect to tons of things and this is fine, they aren't routable that way 
                                    if (highway.GetValue("area") == "yes")
                                        continue;
                                    
                                    string highwayValue = highway.GetValue("highway")!;
                                    
                                    // Platforms can be implicit areas so they are fine for the same reason as above
                                    if (highwayValue == "platform" && highway.Closed)
                                        continue;

                                    report.AddEntry(
                                        ReportGroup.Misconnected,
                                        new IssueReportEntry(
                                            "Barrier connected to highway at " + barrierNode.OsmViewUrl + " - " +
                                            OsmKnowledge.GetFeatureLabel(barrier, "barrier", false) + " " + barrier.OsmViewUrl + "; " +
                                            OsmKnowledge.GetFeatureLabel(highway, "highway", false) + " " + highway.OsmViewUrl,
                                            barrierNode.coord,
                                            MapPointStyle.Dubious
                                        )
                                    );
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    
    private enum ReportGroup
    {
        Misconnected
    }
}