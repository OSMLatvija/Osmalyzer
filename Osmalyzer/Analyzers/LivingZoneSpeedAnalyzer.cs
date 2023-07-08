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

        public override string Description => "The report checks that living zone have the corretc maxspeed set.";


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };

        
        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            OsmMasterData osmMasterData = osmData.MasterData;

            OsmDataExtract livingStreets = osmMasterData.Filter(
                new IsWay(), 
                new HasValue("highway", "living_street")
            );
            
            OsmDataExtract limitedLivingStreets = livingStreets.Filter(
                new HasKey("maxspeed")
            );
            
            // Bad maxspeed values

            report.AddGroup(
                ReportGroup.InvalidSpeed, 
                "These roads have bad max speed limit values for living streets",
                "All living streets should have the max speed limit of 20 km/h. Only living zone roads should be tagged as living streets, so any other value is a mistake. Either the road is not classified correctly (it's not living zone) or the max speed value is incorrect for some reason. Note that courtyard roads also have a speed limit of 20 km/h and these are often mistagged as living streets. Only courtyard roads that are in a signed living zone are living streets, otherwise they are just service roads."
            );
            
            report.AddEntry(
                ReportGroup.InvalidSpeed,
                new PlaceholderReportEntry("There are no roads with invalid max speed limits.")
            );
            
            foreach (OsmElement livingStreet in limitedLivingStreets.Elements)
            {
                // todo: group connected streets and report as one entry
                
                string? maxspeedStr = livingStreet.GetValue("maxspeed");

                if (maxspeedStr != null)
                {
                    if (int.TryParse(maxspeedStr, out int maxspeed))
                    {
                        if (maxspeed != 20)
                        {
                            OsmCoord coord = livingStreet.GetAverageCoord();

                            report.AddEntry(
                                ReportGroup.InvalidSpeed,
                                new IssueReportEntry(
                                    "This road (segment) " + (livingStreet.HasKey("name") ? "\"" + livingStreet.GetValue("name") + "\" " : "") +
                                    "has an incorrect maxspeed value \"" + maxspeedStr + "\": " + livingStreet.OsmViewUrl,
                                    coord
                                )
                            );
                        }
                    }
                    else
                    {
                        OsmCoord coord = livingStreet.GetAverageCoord();

                        report.AddEntry(
                            ReportGroup.InvalidSpeed,
                            new IssueReportEntry(
                                "This road (segment) " + (livingStreet.HasKey("name") ? "\"" + livingStreet.GetValue("name") + "\" " : "") +
                                "has an invalid maxspeed value \"" + maxspeedStr + "\": " + livingStreet.OsmViewUrl,
                                coord
                            )
                        );
                    }
                }
            }
            
            // Stats
            
            report.AddGroup(ReportGroup.Stats, "Maxspeed stats on living streets");

            if (livingStreets.Count > 0) // should never happen otherwise
            {
                float unlimitedPortion = (float)limitedLivingStreets.Count / livingStreets.Count;

                report.AddEntry(
                    ReportGroup.Stats,
                    new GenericReportEntry(
                        "There are a total of " + livingStreets.Count + " living street (segments), " +
                        "of which " + limitedLivingStreets.Count + " or " + (unlimitedPortion * 100f).ToString("F1") + " % have maxspeed set."
                    )
                );
            }
            
            OverpassQuery overpassQuery = new OverpassQuery();

            overpassQuery.AddRule(new HasValueOverpassRule("highway", "living_street"));
            overpassQuery.AddRule(new DoesNotHaveKeyOverpassRule("maxspeed"));
            
            report.AddEntry(
                ReportGroup.Stats,
                new GenericReportEntry(
                    "Overpass query for living streets with no maxspeed value: " + overpassQuery.GetQueryLink()
                )
            );

            // todo report individual living streets with no maxspeed at all?
            // there are currently waaaay too many (8000+ at the time of writing) to report - only if we summarize them and link to overpass or something
            // or may be cluster from neighbourhoods or something, so they can be converted in one go?
        }
        
        private enum ReportGroup
        {
            InvalidSpeed,
            Stats
        }
    }
}