using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class SpeedLimitAnalyzer : Analyzer
    {
        public override string Name => "Speed Limits";

        public override string Description => "This report checks that various speed limits are correct. This doesn't check living zones, see separate report.";

        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };
        

        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
           
            OsmMasterData osmMasterData = osmData.MasterData;

            OsmDataExtract osmRoads = osmMasterData.Filter(
                new IsWay(),
                new HasAnyValue("maxspeed", "80", "90"),
                new HasAnyValue("highway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "trunk_link", "primary_link", "secondary_link"),
                new HasKey("surface")
            );
            
            // Parse

            report.AddGroup(ReportGroup.Unpaved90, 
                            "Unpaved roads with 90", 
                            "These roads are unpaved and have `maxspeed=90`. Unpaved roads in Latvia are limited to 80 by default. It's most likely a mistake, usually because `surface` or `maxspeed` were set at different times and/or changed over time.",
                            "No unpaved roads have a speed limit of 90.");
            
            report.AddGroup(ReportGroup.Paved80, 
                            "Paved roads with 80", 
                            "These roads are paved but have `maxspeed=80`. It's possibly a mistake if the surface or speed limits have changed. It is however perfectly possibly for a paved road to have a signed speed limit of 80. Roads with `maxspeed:type=sign` are ignored - it's probably a good idea to tag false positives if signage can be confirmed.", 
                            "No unpaved roads have a speed limit of 80.");

            OsmDataExtract pavedRoads80 = osmRoads.Filter(
                new HasValue("maxspeed", "80"),
                new HasAnyValue("surface", "asphalt", "paved", "concrete", "chipseal")
            );

            // Stuff that's not expected to be high speed "paving_stones", "sett", "wood", "cobblestone", "concrete:plates", "metal", "grass_paver",

            OsmDataExtract unpavedRoads90 = osmRoads.Filter(
                new HasValue("maxspeed", "90"),
                new HasAnyValue("surface", "unpaved", "ground", "gravel", "dirt", "grass", "compacted", "sand", "fine_gravel", "earth", "pebblestone")
            );

            // TODO: group somehow - by name, ref? ideally, connected
            
            foreach (OsmWay road in unpavedRoads90.Ways) // we only have ways
            {
                report.AddEntry(
                    ReportGroup.Unpaved90,
                    new IssueReportEntry(
                        "This road segment has `maxspeed=90`, but `surface=" + road.GetValue("surface")! + "` " + road.OsmViewUrl,
                        road.GetAverageCoord()
                    )
                );
            }
            
            foreach (OsmWay road in pavedRoads80.Ways) // we only have ways
            {
                string? maxspeedType = road.GetValue("maxspeed:type");
                
                if (maxspeedType == "sign")
                    continue;

                report.AddEntry(
                    ReportGroup.Paved80,
                    new GenericReportEntry(
                        "This road segment has `maxspeed=80`, but `surface=" + road.GetValue("surface")! + "` " + road.OsmViewUrl,
                        road.GetAverageCoord()
                    )
                );
            }
        }
        
        
        private enum ReportGroup
        {
            Unpaved90,
            Paved80
        }
    }
}