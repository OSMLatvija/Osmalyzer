using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class BridgeAndWaterConnectionAnalyzer : Analyzer
{
    public override string Name => "Bridge-Water Connections";

    public override string Description => "This report shows locations where a bridge and a waterway connect unexpectedly.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract bridges = osmMasterData.Filter(
            new IsWay(),
            new HasKey("bridge")
        );

        // Prepare groups

        report.AddGroup(ReportGroup.Misconnected, "Misconnected features");

        report.AddEntry(
            ReportGroup.Misconnected,
            new DescriptionReportEntry(
                "These likely should not connect."
            )
        );
        
        // Parse

        List<BadConnection> badConnections = new List<BadConnection>(); 

        foreach (OsmElement bridge in bridges.Elements)
        {
            if (bridge is OsmWay bridgeWay)
            {
                foreach (OsmNode bridgeNode in bridgeWay.Nodes)
                {
                    if (bridgeNode.Ways != null)
                    {
                        foreach (OsmWay bridgeNodeWay in bridgeNode.Ways)
                        {
                            if (bridgeNodeWay != bridge)
                            {
                                if (bridgeNodeWay.HasKey("waterway"))
                                {
                                    if (bridgeNodeWay.HasValue("waterway", "dam")) // highways can cross/touch dams
                                        continue;
                                    
                                    // todo: but if the way is a relation?

                                    BadConnection? existing = badConnections.FirstOrDefault(bc => bc.Bridge == bridgeWay && bc.Waterway == bridgeNodeWay);

                                    if (existing != null)
                                        existing.Nodes.Add(bridgeNode);
                                    else
                                        badConnections.Add(new BadConnection(bridgeWay, bridgeNodeWay, new List<OsmNode>() { bridgeNode }));
                                }
                            }
                        }
                    }
                }
            }
        }

        if (badConnections.Count > 0)
        {
            foreach (BadConnection badConnection in badConnections)
            {
                report.AddEntry(
                    ReportGroup.Misconnected,
                    new IssueReportEntry(
                        "Bridge connects to waterway at " + badConnection.Nodes.Count + " points : " + badConnection.Bridge.OsmViewUrl + " - " + badConnection.Waterway.OsmViewUrl + " - " + string.Join(", ", badConnection.Nodes.Select(n => n.OsmViewUrl)),
                        OsmGeoTools.GetAverageCoord(badConnection.Nodes),
                        MapPointStyle.Problem
                    )
                );
            }
        }
        else
        {
            report.AddEntry(
                ReportGroup.Misconnected,
                new GenericReportEntry(
                    "No bridge-waterway connections found"
                )
            );
        }
    }


    private record BadConnection(OsmWay Bridge, OsmWay Waterway, List<OsmNode> Nodes);
    
    
    private enum ReportGroup
    {
        Misconnected
    }
}