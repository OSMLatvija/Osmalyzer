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

        public override string Description => "This report checks that drinking water taps for Riga are mapped and their tagging is correct. They are all expected to be free-standing drinkable water taps (brīvkrāni) operated by Rīgas ūdens.";


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(RigaDrinkingWaterAnalysisData) };
        

        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
           
            OsmMasterData osmMasterData = osmData.MasterData;

            OsmDataExtract osmTaps = osmMasterData.Filter(
                new IsNode(),
                new HasValue("amenity", "drinking_water"),
                new InsidePolygon(BoundaryHelper.GetRigaPolygon(osmMasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
            );
            
            // Prepare report groups

            report.AddGroup(
                ReportGroup.RigaIssues, 
                "Problems with drinking water taps",
                "These taps listed in Riga tap list have issues with OSM counterparts.",
                "No issues found with matching OSM taps."
            );
            
            report.AddGroup(
                ReportGroup.OsmIssues, 
                "Unrecognized taps", 
                "These taps are not listed in Riga tap list. They may not be operated by Rīgas ūdens or considered public taps (brīvkrāni) or added to the official list (yet). These are most likely not incorrect.", 
                "No additional taps found."
            );
            
            report.AddGroup(ReportGroup.Stats, "Matched taps");

            // Get Riga taps

            RigaDrinkingWaterAnalysisData drinkingWaterData = datas.OfType<RigaDrinkingWaterAnalysisData>().First();

            List<DrinkingWater> rigaTapsAll = drinkingWaterData.DrinkingWaters;
            
            List<DrinkingWater> rigaTapsStatic = rigaTapsAll.Where(t => t.Type == DrinkingWater.InstallationType.Static).ToList();
            // We don't care about their mobile ones since we wouldn't map them on OSM (although with this report we could keep track)

            // Match Riga taps to OSM taps

            List<(DrinkingWater riga, OsmElement osm)> matchedTaps = new List<(DrinkingWater, OsmElement)>();
            
            int matchedFarCount = 0;

            foreach (DrinkingWater rigaTap in rigaTapsStatic)
            {
                const double seekDistance = 75;

                OsmElement? closestOsmTap = osmTaps.GetClosestElementTo(rigaTap.Coord, seekDistance, out double? closestDistance);

                if (closestOsmTap == null)
                {
                    report.AddEntry(
                        ReportGroup.RigaIssues,
                        new IssueReportEntry(
                            "No OSM tap found in " + seekDistance + " m range of Rīga tap `" + rigaTap.Name + "` at " + rigaTap.Coord.OsmUrl,
                            new SortEntryAsc(SortOrder.NoTap),
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
                            ReportGroup.RigaIssues,
                            new IssueReportEntry(
                                "OSM tap " + closestOsmTap.OsmViewUrl + " found close to Rīga tap `" + rigaTap.Name + "` but it's far away (" + closestDistance!.Value.ToString("F0") + " m), expected at " + rigaTap.Coord.OsmUrl,
                                new SortEntryAsc(SortOrder.TapFar),
                                rigaTap.Coord
                            )
                        );
                    }
                    
                    // Check operator

                    string? operatorValue = closestOsmTap.GetValue("operator");

                    const string expectedOperator = "Rīgas ūdens";

                    if (operatorValue == null)
                    {
                        report.AddEntry(
                            ReportGroup.RigaIssues,
                            new IssueReportEntry(
                                "OSM tap doesn't have expected `oparator=" + expectedOperator + "` set for tap `" + rigaTap.Name + "` - " + closestOsmTap.OsmViewUrl,
                                new SortEntryAsc(SortOrder.Tagging),
                                closestOsmTap.GetAverageCoord()
                            )
                        );
                    }
                    else
                    {
                        if (operatorValue != expectedOperator)
                        {
                            report.AddEntry(
                                ReportGroup.RigaIssues,
                                new IssueReportEntry(
                                    "OSM tap doesn't have expected `oparator=" + expectedOperator + "` set for tap `" + rigaTap.Name + "`, instead `" + operatorValue + "` - " + closestOsmTap.OsmViewUrl,
                                    new SortEntryAsc(SortOrder.Tagging),
                                    closestOsmTap.GetAverageCoord()
                                )
                            );
                        }
                    }
                    
                    // Check type
                    
                    string? manmadeValue = closestOsmTap.GetValue("man_made");

                    if (manmadeValue == null)
                    {
                        report.AddEntry(
                            ReportGroup.RigaIssues,
                            new IssueReportEntry(
                                "OSM tap doesn't have expected `man_made=water_tap` set for tap `" + rigaTap.Name + "` - " + closestOsmTap.OsmViewUrl,
                                new SortEntryAsc(SortOrder.Tagging),
                                closestOsmTap.GetAverageCoord()
                            )
                        );
                    }
                    else
                    {
                        if (manmadeValue != expectedOperator)
                        {
                            report.AddEntry(
                                ReportGroup.RigaIssues,
                                new IssueReportEntry(
                                    "OSM tap doesn't have expected `man_made=water_tap` set for tap `" + rigaTap.Name + "`, instead `" + manmadeValue + "` - " + closestOsmTap.OsmViewUrl,
                                    new SortEntryAsc(SortOrder.Tagging),
                                    closestOsmTap.GetAverageCoord()
                                )
                            );
                        }
                    }
                    
                    // Check drinkable
                    
                    string? drinkableValue = closestOsmTap.GetValue("drinking_water");

                    if (drinkableValue == null)
                    {
                        report.AddEntry(
                            ReportGroup.RigaIssues,
                            new IssueReportEntry(
                                "OSM tap doesn't have expected `drinking_water=yes` set for tap `" + rigaTap.Name + "` - " + closestOsmTap.OsmViewUrl,
                                new SortEntryAsc(SortOrder.Tagging),
                                closestOsmTap.GetAverageCoord()
                            )
                        );
                    }
                    else
                    {
                        if (drinkableValue != expectedOperator)
                        {
                            report.AddEntry(
                                ReportGroup.RigaIssues,
                                new IssueReportEntry(
                                    "OSM tap doesn't have expected `drinking_water=yes` set for tap `" + rigaTap.Name + "`, instead `" + drinkableValue + "` - " + closestOsmTap.OsmViewUrl,
                                    new SortEntryAsc(SortOrder.Tagging),
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
            
            // Match OSM taps to Riga taps

            foreach (OsmElement osmTap in osmTaps.Elements)
            {
                if (matchedTaps.Any(mt => mt.osm == osmTap))
                    continue;
                
                report.AddEntry(
                    ReportGroup.OsmIssues,
                    new GenericReportEntry(
                        "OSM tap found, but not matched to any Riga tap - " + osmTap.OsmViewUrl,
                        osmTap.GetAverageCoord()
                    )
                );
                
                // todo: operator
            }
            
            
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
            RigaIssues,
            Stats,
            OsmIssues
        }

        private enum SortOrder // values used for sorting
        {
            NoTap = 0,
            TapFar = 1,
            Tagging = 2
        }
    }
}