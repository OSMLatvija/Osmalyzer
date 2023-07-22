using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class GlikaOaksAnalyzer : Analyzer
    {
        public override string Name => "Glika Oaks";

        public override string Description => "This report checks that all Glika Ozoli oak trees are mapped.";

        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(GikaOzoliAnalysisData) };


        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            OsmMasterData osmMasterData = osmData.MasterData;

            OsmDataExtract osmTrees = osmMasterData.Filter(
                new IsNode(),
                new HasValue("natural", "tree")
            );

            // Get Oak data

            List<GlikaOak> oaks = datas.OfType<GikaOzoliAnalysisData>().First().Oaks;

            // Parse

            report.AddGroup(ReportGroup.Issues, "Issues", null, "All trees appear to be mapped.");

            report.AddGroup(ReportGroup.Stats, "Matched oaks", "These trees were matched to the oak coordinates. Note that there are many individual trees mapped, so this doesn't mean it's the correct tree.");

            foreach (GlikaOak oak in oaks)
            {
                const double seekDistance = 30;

                OsmElement? closestOsmTree = osmTrees.GetClosestElementTo(oak.Coord, seekDistance, out double? closestDistance);

                if (closestOsmTree == null)
                {
                    report.AddEntry(
                        ReportGroup.Issues,
                        new IssueReportEntry(
                            "No OSM tree found in " + seekDistance + " m range of Glika oak `" + oak.Name + "` at " + oak.Coord.OsmUrl,
                            new SortEntryAsc(SortOrder.NoTree),
                            oak.Coord
                        )
                    );
                }
                else
                {
                    if (closestDistance! > 10)
                    {
                        report.AddEntry(
                            ReportGroup.Issues,
                            new IssueReportEntry(
                                "OSM tree " + closestOsmTree.OsmViewUrl + " found close to Glika oak `" + oak.Name + "`, but it's far away (" + closestDistance!.Value.ToString("F0") + " m), expected at " + oak.Coord.OsmUrl,
                                new SortEntryAsc(SortOrder.TreeFar),
                                oak.Coord
                            )
                        );
                    }
                    
                    // Add found oaks to stats

                    report.AddEntry(
                        ReportGroup.Stats,
                        new MapPointReportEntry(
                            closestOsmTree.GetAverageCoord(),
                            "`" + oak.Name + "` " + closestOsmTree.OsmViewUrl
                        )
                    );
                }
            }
        }


        private enum ReportGroup
        {
            Issues,
            Stats
        }

        private enum SortOrder // values used for sorting
        {
            NoTree = 0,
            TreeFar = 1
        }
    }
}