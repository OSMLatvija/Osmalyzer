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

        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(GlikaOzoliAnalysisData) };


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

            List<GlikaOak> oaks = datas.OfType<GlikaOzoliAnalysisData>().First().Oaks;

            // Parse

            report.AddGroup(ReportGroup.Issues, "Issues", null, "All trees appear to be mapped.");

            report.AddGroup(ReportGroup.Stats, "Matched oaks");

            foreach (GlikaOak oak in oaks)
            {
                const double seekDistance = 75;

                List<OsmNode> closestOsmTrees = osmTrees.GetClosestNodesTo(oak.Coord, seekDistance);

                if (closestOsmTrees.Count == 0)
                {
                    report.AddEntry(
                        ReportGroup.Issues,
                        new IssueReportEntry(
                            "No matching OSM tree found in " + seekDistance + " m range of " +
                            "Glika oak `" + oak.Name + "` " + (oak.Id != null ? "#" + oak.Id + " " : "") + "(" + oak.StartDate + ") at " + oak.Coord.OsmUrl,
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
                                    matchedOsmTree.OsmViewUrl + " found close to " +
                                    "Glika oak `" + oak.Name + "` " + (oak.Id != null ? "#" + oak.Id + " " : "") + "(" + oak.StartDate + "), " +
                                    "but it's far away (" + matchedOsmTreeDistance.ToString("F0") + " m), expected at " + oak.Coord.OsmUrl,
                                    new SortEntryAsc(SortOrder.TreeFar),
                                    oak.Coord
                                )
                            );
                        }

                        report.AddEntry(
                            ReportGroup.Stats,
                            new MapPointReportEntry(
                                matchedOsmTree.coord,
                                "`" + oak.Name + "` (" + oak.StartDate + ") matched " + 
                                matchedOsmTree.OsmViewUrl + " " + 
                                (matchedOsmTree.HasKey("name") ? "`" + matchedOsmTree.GetValue("name") + "` " : "") +
                                " at " + matchedOsmTreeDistance.ToString("F0") + " m"
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
                                    closestUnmatchedTree.OsmViewUrl + " found close to " +
                                    "Glika oak `" + oak.Name + "` " + (oak.Id != null ? "#" + oak.Id + " " : "") + "(" + oak.StartDate + ") at " + unmatchedOsmTreeDistance.ToString("F0") + " m" +
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