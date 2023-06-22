using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    public class TrolleybusWireAnalyzer : Analyzer
    {
        public override string Name => "Trolleybus Wires";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };

        
        public override void Run(IEnumerable<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            List<OsmBlob> blobs = OsmBlob.CreateMultiple(
                osmData.DataFileName,
                new List<OsmFilter[]>()
                {
                    new OsmFilter[]
                    {
                        new IsRelation(),
                        new HasValue("type", "route"),
                        new HasValue("route", "trolleybus")
                    },
                    new OsmFilter[]  
                    {
                        new IsWay(),
                        new HasAnyValue("highway", new List<string>() { "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "service" })
                    }
                }
            );

            OsmBlob routes = blobs[0];
            OsmBlob roads = blobs[1];

            // Process

            foreach (OsmElement route in routes.Elements)
            {
                OsmRelation relation = (OsmRelation)route;

                string routeName = relation.GetValue("name")!;

                bool foundIssue = false;
                
                foreach (OsmRelationMember member in relation.Members)
                {
                    OsmElement? roadSegment = roads.Elements.FirstOrDefault(r => r.Id == member.Id);

                    if (roadSegment != null)
                    {
                        string? trolley_wire = roadSegment.GetValue("trolley_wire");
                        string? trolley_wire_forward = roadSegment.GetValue("trolley_wire:forward");
                        string? trolley_wire_backward = roadSegment.GetValue("trolley_wire:backward");

                        if (trolley_wire != null && (trolley_wire_forward != null || trolley_wire_backward != null))
                        {
                            CheckFirstMentionOfRouteIssue();
                            report.WriteRawLine("Conflicting `trolley_wire:xxx` subvalue(s) with main `trolley_wire` value on https://www.openstreetmap.org/way/" + roadSegment.Id);
                        }
                        else if (trolley_wire != null)
                        {
                            if (trolley_wire != "yes" && trolley_wire != "no")
                            {
                                CheckFirstMentionOfRouteIssue();
                                report.WriteRawLine("`trolley_wire` unknown value \"" + trolley_wire + "\" on https://www.openstreetmap.org/way/" + roadSegment.Id);
                            }
                        }
                        else if (trolley_wire_forward != null || trolley_wire_backward != null)
                        {
                            if (trolley_wire_forward != null && trolley_wire_forward != "yes" && trolley_wire_forward != "no")
                            {
                                CheckFirstMentionOfRouteIssue();
                                report.WriteRawLine("`trolley_wire:forward` unknown value \"" + trolley_wire_forward + "\" on https://www.openstreetmap.org/way/" + roadSegment.Id);
                            }

                            if (trolley_wire_backward != null && trolley_wire_backward != "yes" && trolley_wire_backward != "no")
                            {
                                CheckFirstMentionOfRouteIssue();
                                report.WriteRawLine("`trolley_wire:backward` unknown value \"" + trolley_wire_backward + "\" on https://www.openstreetmap.org/way/" + roadSegment.Id);
                            }
                        }
                        else
                        {
                            CheckFirstMentionOfRouteIssue();
                            report.WriteRawLine("`trolley_wire` missing on https://www.openstreetmap.org/way/" + roadSegment.Id);
                        }


                        void CheckFirstMentionOfRouteIssue()
                        {
                            if (!foundIssue)
                            {
                                foundIssue = true;
                                report.WriteRawLine("Route " + route.Id + " \"" + routeName + "\"");
                            }
                        }
                    }
                }
            }
            
            // TODO: trolley_wire=no, but no route - pointless? not that it hurts anything
        }
    }
}