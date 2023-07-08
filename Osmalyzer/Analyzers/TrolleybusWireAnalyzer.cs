using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class TrolleybusWireAnalyzer : Analyzer
    {
        public override string Name => "Trolleybus Wires";

        public override string Description => "This report checks that trolleybus wires tags are set for all ways that have trolleybus routes.";


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };

        
        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            OsmMasterData osmMasterData = osmData.MasterData;

            List<OsmDataExtract> osmDataExtracts = osmMasterData.Filter(
                new List<OsmFilter[]>()
                {
                    new OsmFilter[]
                    {
                        new IsRelation(),
                        new HasValue("type", "route"),
                        new HasValue("route", "trolleybus")
                    }
                }
            );

            OsmDataExtract routes = osmDataExtracts[0];

            // Process

            report.AddGroup(ReportGroup.Issues, "Ways with trolley_wire issues");

            report.AddEntry(
                ReportGroup.Issues,
                new PlaceholderReportEntry(
                    "No issues with `trolley_wire`s found"
                )
            );
            
            foreach (OsmElement route in routes.Elements)
            {
                OsmRelation relation = (OsmRelation)route;

                string routeName = relation.GetValue("name")!;

                bool foundIssue = false;
                
                foreach (OsmRelationMember member in relation.Members)
                {
                    if (member.Element == null)
                        continue;
                    
                    if (member.Element is not OsmWay roadSegment)
                        continue;
                    
                    if (member.Role == "platform") // todo: or should it only accept empty?
                        continue;

                    string? trolley_wire = roadSegment.GetValue("trolley_wire");
                    string? trolley_wire_forward = roadSegment.GetValue("trolley_wire:forward");
                    string? trolley_wire_backward = roadSegment.GetValue("trolley_wire:backward");

                    if (trolley_wire != null && (trolley_wire_forward != null || trolley_wire_backward != null))
                    {
                        CheckFirstMentionOfRouteIssue();
                        report.AddEntry(
                            ReportGroup.Issues,
                            new IssueReportEntry(
                                "Conflicting `trolley_wire:xxx` subvalue(s) with main `trolley_wire` value on " + roadSegment.OsmViewUrl,
                                roadSegment.GetAverageCoord()
                            )
                        );
                    }
                    else if (trolley_wire != null)
                    {
                        if (trolley_wire != "yes" && trolley_wire != "no")
                        {
                            CheckFirstMentionOfRouteIssue();
                            report.AddEntry(
                                ReportGroup.Issues,
                                new IssueReportEntry(
                                    "`trolley_wire` unknown value \"" + trolley_wire + "\" on " + roadSegment.OsmViewUrl,
                                    roadSegment.GetAverageCoord()
                                )
                            );
                        }
                    }
                    else if (trolley_wire_forward != null || trolley_wire_backward != null)
                    {
                        if (trolley_wire_forward != null && trolley_wire_forward != "yes" && trolley_wire_forward != "no")
                        {
                            CheckFirstMentionOfRouteIssue();
                            report.AddEntry(
                                ReportGroup.Issues,
                                new IssueReportEntry(
                                    "`trolley_wire:forward` unknown value \"" + trolley_wire_forward + "\" on " + roadSegment.OsmViewUrl,
                                    roadSegment.GetAverageCoord()
                                )
                            );
                        }

                        if (trolley_wire_backward != null && trolley_wire_backward != "yes" && trolley_wire_backward != "no")
                        {
                            CheckFirstMentionOfRouteIssue();
                            report.AddEntry(
                                ReportGroup.Issues,
                                new IssueReportEntry(
                                    "`trolley_wire:backward` unknown value \"" + trolley_wire_backward + "\" on " + roadSegment.OsmViewUrl,
                                    roadSegment.GetAverageCoord()
                                )
                            );                                    
                        }
                    }
                    else
                    {
                        CheckFirstMentionOfRouteIssue();
                        report.AddEntry(
                            ReportGroup.Issues,
                            new IssueReportEntry(
                                "`trolley_wire` missing on " + roadSegment.OsmViewUrl,
                                roadSegment.GetAverageCoord()
                            )
                        );
                    }


                    void CheckFirstMentionOfRouteIssue()
                    {
                        if (!foundIssue)
                        {
                            foundIssue = true;
                            
                            report.AddEntry(
                                ReportGroup.Issues,
                                new IssueReportEntry(
                                    "Route " + route.Id + " \"" + routeName + "\""
                                )
                            );
                        }
                    }
                }
            }
            
            // TODO: trolley_wire=no, but no route - pointless? not that it hurts anything
        }
        
        private enum ReportGroup
        {
            Issues
        }
    }
}