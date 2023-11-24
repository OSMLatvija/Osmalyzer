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

            // Get Riga taps

            RigaDrinkingWaterAnalysisData drinkingWaterData = datas.OfType<RigaDrinkingWaterAnalysisData>().First();

            List<DrinkingWater> rigaTapsAll = drinkingWaterData.DrinkingWaters;
            
            List<DrinkingWater> rigaTapsStatic = rigaTapsAll.Where(t => t.Type == DrinkingWater.InstallationType.Static).ToList();
            // We don't care about their mobile ones since we wouldn't map them on OSM (although with this report we could keep track)
            
            // Prepare data comparer/correlator

            OsmToDataItemQuickComparer<DrinkingWater> dataComparer = new OsmToDataItemQuickComparer<DrinkingWater>(
                osmTaps,
                rigaTapsStatic,
                new UnmatchedOsmElementAllowedByItselfCallbackQuickCompareParameter(IsUnmatchedOsmElementAllowed));
            // Note that we don't have any condition to mismatch taps, we only expect riga taps in riga

            bool IsUnmatchedOsmElementAllowed(OsmElement element)
            {
                // Riga tap list deletes their list during winter (rather than somehow tagging it)
                // Since OSM does not delete elements, but rather marks them as seasonal, we can keep assume those are correct
                return element.GetValue("seasonal") == "yes";
            }

            // Parse and report primary matching and location correlation

            QuickCompareReport<DrinkingWater> compareReport = dataComparer.Parse(
                report,
                new MatchedItemQuickComparerReportEntry(15),
                new UnmatchedItemQuickComparerReportEntry(75),
                new UnmatchedOsmQuickComparerReportEntry(),
                new MatchedItemButFarQuickComparerReportEntry()
            );

            // Prepare additional report groups

            report.AddGroup(
                ReportGroup.ExtraIssues, 
                "Other problems with drinking water taps",
                "These taps listed in Riga tap list have issues with OSM counterparts.",
                "No issues found with matching OSM taps."
            );

            foreach (KeyValuePair<OsmElement, DrinkingWater> match in compareReport.MatchedElements)
            {
                OsmElement osmTap = match.Key;
                DrinkingWater rigaTap = match.Value;
                
                // Check operator
            
                string? operatorValue = osmTap.GetValue("operator");
            
                const string expectedOperator = "Rīgas ūdens";
            
                if (operatorValue == null)
                {
                    report.AddEntry(
                        ReportGroup.ExtraIssues,
                        new IssueReportEntry(
                            "OSM tap doesn't have expected `oparator=" + expectedOperator + "` set for tap `" + rigaTap.Name + "` - " + osmTap.OsmViewUrl,
                            new SortEntryAsc(SortOrder.Tagging),
                            osmTap.GetAverageCoord()
                        )
                    );
                }
                else
                {
                    if (operatorValue != expectedOperator)
                    {
                        report.AddEntry(
                            ReportGroup.ExtraIssues,
                            new IssueReportEntry(
                                "OSM tap doesn't have expected `oparator=" + expectedOperator + "` set for tap `" + rigaTap.Name + "`, instead `" + operatorValue + "` - " + osmTap.OsmViewUrl,
                                new SortEntryAsc(SortOrder.Tagging),
                                osmTap.GetAverageCoord()
                            )
                        );
                    }
                }
                
                // Check type
               
                const string expectedManmade = "water_tap";
            
                string? manmadeValue = osmTap.GetValue("man_made");
            
                if (manmadeValue == null)
                {
                    report.AddEntry(
                        ReportGroup.ExtraIssues,
                        new IssueReportEntry(
                            "OSM tap doesn't have expected `man_made="+expectedManmade+"` set for tap `" + rigaTap.Name + "` - " + osmTap.OsmViewUrl,
                            new SortEntryAsc(SortOrder.Tagging),
                            osmTap.GetAverageCoord()
                        )
                    );
                }
                else
                {
                    if (manmadeValue != expectedManmade)
                    {
                        report.AddEntry(
                            ReportGroup.ExtraIssues,
                            new IssueReportEntry(
                                "OSM tap doesn't have expected `man_made="+expectedManmade+"` set for tap `" + rigaTap.Name + "`, instead `" + manmadeValue + "` - " + osmTap.OsmViewUrl,
                                new SortEntryAsc(SortOrder.Tagging),
                                osmTap.GetAverageCoord()
                            )
                        );
                    }
                }
                
                // Check drinkable
                
                string? drinkableValue = osmTap.GetValue("drinking_water");
            
                if (drinkableValue == null)
                {
                    report.AddEntry(
                        ReportGroup.ExtraIssues,
                        new IssueReportEntry(
                            "OSM tap doesn't have expected `drinking_water=yes` set for tap `" + rigaTap.Name + "` - " + osmTap.OsmViewUrl,
                            new SortEntryAsc(SortOrder.Tagging),
                            osmTap.GetAverageCoord()
                        )
                    );
                }
                else
                {
                    if (drinkableValue != "yes")
                    {
                        report.AddEntry(
                            ReportGroup.ExtraIssues,
                            new IssueReportEntry(
                                "OSM tap doesn't have expected `drinking_water=yes` set for tap `" + rigaTap.Name + "`, instead `" + drinkableValue + "` - " + osmTap.OsmViewUrl,
                                new SortEntryAsc(SortOrder.Tagging),
                                osmTap.GetAverageCoord()
                            )
                        );
                    }
                }
                
                // Check fixme
                
                string? fixmeValue = osmTap.GetValue("fixme");
            
                if (fixmeValue != null)
                {
                    report.AddEntry(
                        ReportGroup.ExtraIssues,
                        new IssueReportEntry(
                            "OSM tap has a `fixme=" + fixmeValue + "` set for tap `" + rigaTap.Name + "` - " + osmTap.OsmViewUrl,
                            new SortEntryAsc(SortOrder.Tagging),
                            osmTap.GetAverageCoord()
                        )
                    );
                }
            }
        }


        private enum ReportGroup
        {
            ExtraIssues
        }

        private enum SortOrder // values used for sorting
        {
            Tagging = 0
        }
    }
}