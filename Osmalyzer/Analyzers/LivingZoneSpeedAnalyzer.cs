using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class LivingZoneSpeedAnalyzer : Analyzer
    {
        public override string Name => "Living Zone Speeds";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };

        
        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            OsmMasterData osmMasterData = osmData.MasterData;

            OsmDataExtract livingStreets = osmMasterData.Filter(
                new IsWay(), 
                new HasValue("highway", "living_street"), 
                new HasKey("maxspeed")
            );

            // Process

            report.AddGroup(ReportGroup.InvalidSpeed, "These roads have bad max speed limit values for living streets:");
            
            report.AddEntry(
                ReportGroup.InvalidSpeed,
                new Report.DescriptionReportEntry("All living streets should have the max speed limit of 20 km/h. Only living zone roads should be tagged as living streets, so any other value is a mistake. Either the road is not classified correctly (it's not living zone) or the max speed value is incorrect for some reason. Note that courtyard roads also have a speed limit of 20 km/h and these are often mistagged as living streets. Only courtyard roads that are in a signed living zone are living streets, otherwise they are just service roads.")
            );
            
            report.AddEntry(
                ReportGroup.InvalidSpeed,
                new Report.PlaceholderReportEntry("There are no roads with invalid max speed limits.")
            );
            
            foreach (OsmElement livingStreet in livingStreets.Elements)
            {
                // todo: group connected streets and report as one entry
                
                string? maxspeedStr = livingStreet.GetValue("maxspeed");

                if (maxspeedStr != null)
                {
                    if (int.TryParse(maxspeedStr, out int maxspeed))
                    {
                        if (maxspeed != 20)
                        {
                            report.AddEntry(
                                ReportGroup.InvalidSpeed,
                                new Report.MainReportEntry(
                                    "This road (segment) " + (livingStreet.HasKey("name") ? "\"" + livingStreet.GetValue("name") + "\" " : "") +
                                    "has an incorrect maxspeed value \"" + maxspeedStr + "\": https://www.openstreetmap.org/way/" + livingStreet.Id)
                            );
                        }
                    }
                    else
                    {
                        report.AddEntry(
                            ReportGroup.InvalidSpeed,
                            new Report.MainReportEntry(
                                "This road (segment) " + (livingStreet.HasKey("name") ? "\"" + livingStreet.GetValue("name") + "\" " : "") +
                                "has an invalid maxspeed value \"" + maxspeedStr + "\": https://www.openstreetmap.org/way/" + livingStreet.Id
                            )
                        );
                    }
                }
            }
            
            // todo living streets with no maxspeed at all - there are currently waaaay too many to report - only if we summarize them and link to overpass or something
        }
        
        private enum ReportGroup
        {
            InvalidSpeed
        }
    }
}