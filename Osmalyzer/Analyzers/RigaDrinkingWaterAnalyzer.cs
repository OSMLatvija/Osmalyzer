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

        public override string Description => "This report checks that drinking water taps for Riga are mapped";


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

            List<DrinkingWater> rigaTapsAll = drinkingWaterData.DrinkingWaters;
            
            List<DrinkingWater> rigaTapsStatic = rigaTapsAll.Where(t => t.Type == DrinkingWater.InstallationType.Static).ToList();
            // We don't care about their mobile ones since we wouldn't map them on OSM (although with this report we could keep track)

            // Parse

            List<(DrinkingWater riga, OsmElement osm)> matchedTaps = new List<(DrinkingWater, OsmElement)>();
            
            int matchedFarCount = 0;

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
                    if (closestDistance! > 15)
                    {
                        matchedFarCount++;
                        
                        report.AddEntry(
                            ReportGroup.Issues,
                            new IssueReportEntry(
                                "OSM tap found close to Rīga tap `" + rigaTap.Name + "` but it's far away (" + closestDistance!.Value.ToString("F0") + " m), expected at " + rigaTap.Coord.OsmUrl,
                                new SortEntryAsc(SortOrder.MainIssue),
                                rigaTap.Coord
                            )
                        );
                    }
                    
                    // Check tags

                    string? operatorValue = closestOsmTap.GetValue("operator");

                    if (operatorValue == null)
                    {
                        report.AddEntry(
                            ReportGroup.Issues,
                            new IssueReportEntry(
                                "OSM tap doesn't have expected `oparator=Rīgas Ūdens` set - " + rigaTap.Coord.OsmUrl,
                                new SortEntryAsc(SortOrder.SideIssue),
                                closestOsmTap.GetAverageCoord()
                            )
                        );
                    }
                    else
                    {
                        const string expectedOperator = "Rīgas ūdens";
                        
                        if (operatorValue != expectedOperator)
                        {
                            report.AddEntry(
                                ReportGroup.Issues,
                                new IssueReportEntry(
                                    "OSM tap doesn't have expected `oparator=" + expectedOperator + "` set, insetad `" + operatorValue + "` - " + rigaTap.Coord.OsmUrl,
                                    new SortEntryDesc(SortOrder.SideIssue),
                                    closestOsmTap.GetAverageCoord()
                                )
                            );
                        }
                    }

                    // Add found tap to stats
                        
                    matchedTaps.Add((rigaTap, closestOsmTap));
                    
                    report.AddEntry(
                        ReportGroup.Stats,
                        new MapPointReportEntry(
                            closestOsmTap.GetAverageCoord(), 
                            rigaTap.Name + " - " + closestOsmTap.OsmViewUrl
                        )
                    );
                }
            }
            
            // todo: match osm within riga bounds to these? there are way more though. 
            
            // Stats

            report.AddEntry(
                ReportGroup.Stats,
                new DescriptionReportEntry(
                    "Matched " + matchedTaps.Count + "/" + rigaTapsStatic.Count + " Riga taps to OSM taps (" + matchedFarCount + " far away) -- " +
                    string.Join(", ", matchedTaps.Select(t => "`" + t.riga.Name + "`")) +
                    "."
                )
            );
        }


        private enum ReportGroup
        {
            Issues,
            Stats
        }

        private enum SortOrder // values used for sorting
        {
            MainIssue = 0,
            SideIssue = 1
        }
    }
}