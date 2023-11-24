using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer
{
    public class OsmToDataItemQuickComparer<T> where T : IQuickComparerDataItem
    {
        private readonly OsmDataExtract _osmElements;
        
        private readonly List<T> _dataItems;
        
        private readonly Func<T, OsmElement, bool> _matchCallback;


        public OsmToDataItemQuickComparer(OsmDataExtract osmElements, List<T> dataItems, Func<T, OsmElement, bool> matchCallback)
        {
            if (osmElements == null) throw new ArgumentNullException(nameof(osmElements));
            if (dataItems == null) throw new ArgumentNullException(nameof(dataItems));
            if (matchCallback == null) throw new ArgumentNullException(nameof(matchCallback));
            
            _osmElements = osmElements;
            _dataItems = dataItems;
            _matchCallback = matchCallback;
        }


        public void Parse(Report report, params QuickComparerReportEntry[] entries)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            
            // See what sort of filters we have and which matching logic we will need to do (and report)
            
            bool reportMatched = entries.OfType<MatchedQuickComparerReportEntry>().Any();
            bool reportMatchedFar = entries.OfType<MatchedButFarQuickComparerReportEntry>().Any();
            bool reportUnmatched = entries.OfType<UnmatchedQuickComparerReportEntry>().Any();

            if (reportUnmatched && !reportMatched) throw new InvalidOperationException("Can't not match if not matching"); 
            if (reportMatchedFar && (!reportMatched || !reportUnmatched)) throw new InvalidOperationException("Can't match far if not both matching and unmatching"); 
            
            double matchDistance = reportMatched ? entries.OfType<MatchedQuickComparerReportEntry>().First().Distance : 0;
            double unmatchDistance = reportUnmatched ? entries.OfType<UnmatchedQuickComparerReportEntry>().First().Distance : matchDistance;

            report.AddGroup(ReportGroup.Issues, "Issues", null, "All elements appear to be mapped.");

            if (reportMatched)
                report.AddGroup(ReportGroup.Matched, "Matched elements");

            // Go
            
            foreach (T dataItem in _dataItems)
            {
                List<OsmNode> closestOsmElements = _osmElements.GetClosestNodesTo(dataItem.Coord, unmatchDistance);

                if (closestOsmElements.Count == 0)
                {
                    if (reportUnmatched)
                    {
                        report.AddEntry(
                            ReportGroup.Issues,
                            new IssueReportEntry(
                                "No OSM element found in " + unmatchDistance + " m range of " +
                                dataItem.ReportString() + " at " + dataItem.Coord.OsmUrl,
                                new SortEntryAsc(SortOrder.NoElement),
                                dataItem.Coord
                            )
                        );
                    }
                }
                else
                {
                    OsmNode? matchedOsmElement = closestOsmElements.FirstOrDefault(t => _matchCallback(dataItem, t));

                    if (matchedOsmElement != null)
                    {
                        double matchedOsmElementDistance = OsmGeoTools.DistanceBetween(matchedOsmElement.coord, dataItem.Coord);

                        if (matchedOsmElementDistance > matchDistance)
                        {
                            if (reportMatchedFar)
                            {
                                report.AddEntry(
                                    ReportGroup.Issues,
                                    new IssueReportEntry(
                                        "Matching OSM element " +
                                        (matchedOsmElement.HasKey("name") ? "`" + matchedOsmElement.GetValue("name") + "` " : "") +
                                        matchedOsmElement.OsmViewUrl + " found close to " +
                                        dataItem.ReportString() + ", " +
                                        "but it's far away (" + matchedOsmElementDistance.ToString("F0") + " m), expected at " + dataItem.Coord.OsmUrl,
                                        new SortEntryAsc(SortOrder.ElementFar),
                                        dataItem.Coord
                                    )
                                );
                            }
                        }

                        if (reportMatched)
                        {
                            report.AddEntry(
                                ReportGroup.Matched,
                                new MapPointReportEntry(
                                    matchedOsmElement.coord,
                                    dataItem.ReportString() + " matched " +
                                    matchedOsmElement.OsmViewUrl + " " +
                                    (matchedOsmElement.HasKey("name") ? "`" + matchedOsmElement.GetValue("name") + "` " : "") +
                                    " at " + matchedOsmElementDistance.ToString("F0") + " m"
                                )
                            );
                        }
                    }
                    // else
                    // {
                    //     OsmNode closestUnmatchedOsmElement = closestOsmElements.OrderBy(t => OsmGeoTools.DistanceBetween(t.coord, dataItem.Coord)).First();
                    //
                    //     double unmatchedOsmElementDistance = OsmGeoTools.DistanceBetween(closestUnmatchedOsmElement.coord, dataItem.Coord);
                    //
                    //     if (unmatchedOsmElementDistance < matchDistance)
                    //     {
                    //         report.AddEntry(
                    //             ReportGroup.Issues,
                    //             new IssueReportEntry(
                    //                 "Unmatched OSM element " +
                    //                 (closestUnmatchedOsmElement.HasKey("name") ? "`" + closestUnmatchedOsmElement.GetValue("name") + "` " : "") +
                    //                 closestUnmatchedOsmElement.OsmViewUrl + " found close to " +
                    //                 dataItem.ReportString() + " at " + unmatchedOsmElementDistance.ToString("F0") + " m" +
                    //                 (closestOsmElements.Count > 1 ? " (there are " + closestOsmElements.Count + " elements nearby)" : "") +
                    //                 ", expected at " + dataItem.Coord.OsmUrl,
                    //                 new SortEntryAsc(SortOrder.ElementFar),
                    //                 dataItem.Coord
                    //             )
                    //         );
                    //     }
                    // }
                }
            }
        }
        
        
        private enum ReportGroup
        {
            Issues,
            Matched
        }        
        
        private enum SortOrder // values used for sorting
        {
            NoElement = 0,
            ElementFar = 1
        }
    }
}