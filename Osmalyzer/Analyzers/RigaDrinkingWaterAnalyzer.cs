using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class RigaDrinkingWaterAnalyzer : Analyzer
    {
        public override string Name => "Riga Drinking Water";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(RigaDrinkingWaterAnalysisData) };
        

        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
           
            OsmMasterData osmMasterData = osmData.MasterData;

            OsmDataExtract osmTaps = osmMasterData.Filter(
                new IsNode(),
                new HasValue("amenity", "drinking_water")
            );
            
            // Prepare report groups

            report.AddGroup(ReportGroup.Issues, "Problems with drinking water taps");
            
            report.AddGroup(ReportGroup.Stats, "Matched taps");

            // Get Riga taps

            RigaDrinkingWaterAnalysisData drinkingWaterData = datas.OfType<RigaDrinkingWaterAnalysisData>().First();

            List<DrinkingWater> rigaTaps = drinkingWaterData.DrinkingWaters;
            // Parse

            List<DrinkingWater> matchedTaps = new List<DrinkingWater>();
            
            int matchedFarCount = 0;

            List<DrinkingWater> rigaTapsStatic = rigaTaps.Where(t => t.Type == DrinkingWater.InstallationType.Static).ToList();

            foreach (DrinkingWater rigaTap in rigaTapsStatic)
            {
                const double seekDistance = 75;

                OsmElement? closestOsmTap = osmTaps.GetClosestElementTo(rigaTap.Coord, seekDistance, out double? closestDistance);

                if (closestOsmTap == null)
                {
                    report.AddEntry(
                        ReportGroup.Issues,
                        new IssueReportEntry(
                            "No OSM tap found in " + seekDistance + " m range of Rīga tap `" + rigaTap.Name + "` at " + rigaTap.Coord.OsmUrl,
                            rigaTap.Coord
                        )
                    );
                }
                else
                {
                    matchedTaps.Add(rigaTap);
                        
                    if (closestDistance! > 15)
                    {
                        matchedFarCount++;
                        
                        report.AddEntry(
                            ReportGroup.Issues,
                            new IssueReportEntry(
                                "OSM tap found close to Rīga tap `" + rigaTap.Name + "` but it's far away (" + closestDistance.Value.ToString("F0") + " m), expected at " + rigaTap.Coord.OsmUrl,
                                rigaTap.Coord
                            )
                        );
                    }
                    
                    
                    // todo: operator
                    // todo: source
                }
            }
            
            // todo: match osm within riga bounds to these? there are way more though. 
            
            // Stats

            report.AddEntry(
                ReportGroup.Stats,
                new DescriptionReportEntry(
                    "Matched " + matchedTaps.Count + "/" + rigaTapsStatic.Count + " Riga taps to OSM taps (" + matchedFarCount + " far away) -- " +
                    string.Join(", ", matchedTaps.Select(t => "`" + t.Name + "`")) +
                    "."
                )
            );
        }


        private enum ReportGroup
        {
            Issues,
            Stats
        }
    }
}