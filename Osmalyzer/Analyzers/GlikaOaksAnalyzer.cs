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
                const double seekDistance = 75;

                List<OsmNode> closestOsmTrees = osmTrees.GetClosestNodesTo(oak.Coord, seekDistance);

                if (closestOsmTrees.Count == 0)
                {
                    report.AddEntry(
                        ReportGroup.Issues,
                        new IssueReportEntry(
                            "No matching OSM tree found in " + seekDistance + " m range of Glika oak `" + oak.Name + "` at " + oak.Coord.OsmUrl,
                            new SortEntryAsc(SortOrder.NoTree),
                            oak.Coord
                        )
                    );
                }
                else
                {
                    OsmNode? matchedOsmTree = closestOsmTrees.FirstOrDefault(t => DoesOsmTreeMatchOak(t, oak));

                    if (matchedOsmTree != null)
                    {
                        double matchedOsmTreeDistance = OsmGeoTools.DistanceBetween(matchedOsmTree.coord, oak.Coord);

                        if (matchedOsmTreeDistance > 15)
                        {
                            report.AddEntry(
                                ReportGroup.Issues,
                                new IssueReportEntry(
                                    "Matching OSM tree " +
                                    (matchedOsmTree.HasKey("name") ? "`" + matchedOsmTree.GetValue("name") + "` " : "") +
                                    matchedOsmTree.OsmViewUrl + " found close to Glika oak `" + oak.Name + "`, but it's far away (" + matchedOsmTreeDistance.ToString("F0") + " m), expected at " + oak.Coord.OsmUrl,
                                    new SortEntryAsc(SortOrder.TreeFar),
                                    oak.Coord
                                )
                            );
                        }

                        report.AddEntry(
                            ReportGroup.Stats,
                            new MapPointReportEntry(
                                matchedOsmTree.coord,
                                "`" + oak.Name + "` " + matchedOsmTree.OsmViewUrl + " (" + matchedOsmTreeDistance.ToString("F0") + " m)"
                            )
                        );
                        
                        // todo: denomination
                        // todo: species
                        // todo: start_date
                    }
                    else
                    {
                        OsmNode closestUnmatchedTree = closestOsmTrees.OrderBy(t => OsmGeoTools.DistanceBetween(t.coord, oak.Coord)).First();

                        double unmatchedOsmTreeDistance = OsmGeoTools.DistanceBetween(closestUnmatchedTree.coord, oak.Coord);

                        const double acceptDistance = 30;

                        if (unmatchedOsmTreeDistance < acceptDistance)
                        {
                            report.AddEntry(
                                ReportGroup.Issues,
                                new IssueReportEntry(
                                    "Unmatched OSM tree " +
                                    (closestUnmatchedTree.HasKey("name") ? "`" + closestUnmatchedTree.GetValue("name") + "` " : "") +
                                    closestUnmatchedTree.OsmViewUrl + " found close to Glika oak `" + oak.Name + "` at " + unmatchedOsmTreeDistance.ToString("F0") + " m" +
                                    (closestOsmTrees.Count > 1 ? " (there are " + closestOsmTrees.Count + " trees nearby)" : "") +
                                    ", expected at " + oak.Coord.OsmUrl,
                                    new SortEntryAsc(SortOrder.TreeFar),
                                    oak.Coord
                                )
                            );
                        }
                    }
                }
            }
        }

        [Pure]
        private static bool DoesOsmTreeMatchOak(OsmNode osmTree, GlikaOak oak)
        {
            return osmTree.GetValue("name")?.ToLower().Contains("glika ozols") ?? false;
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