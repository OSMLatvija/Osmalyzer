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
            
            bool reportMatchedItem = entries.OfType<MatchedItemQuickComparerReportEntry>().Any();
            bool reportMatchedItemFar = entries.OfType<MatchedItemButFarQuickComparerReportEntry>().Any();
            bool reportUnmatchedItem = entries.OfType<UnmatchedItemQuickComparerReportEntry>().Any();
            bool reportUnmatchedOsm = entries.OfType<UnmatchedOsmQuickComparerReportEntry>().Any();

            if (reportUnmatchedItem && !reportMatchedItem) throw new InvalidOperationException("Can't not match if not matching"); 
            if (reportMatchedItemFar && (!reportMatchedItem || !reportUnmatchedItem)) throw new InvalidOperationException("Can't match far if not both matching and unmatching");
            if (reportUnmatchedOsm && (!reportMatchedItem && !reportUnmatchedItem)) throw new InvalidOperationException("Can't (un)match osm if items are not matching or unmatching");
            
            double matchDistance = reportMatchedItem ? entries.OfType<MatchedItemQuickComparerReportEntry>().First().Distance : 0;
            double unmatchDistance = reportUnmatchedItem ? entries.OfType<UnmatchedItemQuickComparerReportEntry>().First().Distance : matchDistance;

            report.AddGroup(ReportGroup.Unmatched, "Issues", null, "All elements appear to be mapped.");

            if (reportMatchedItem)
                report.AddGroup(ReportGroup.MatchedOsm, "Matched elements");

            Dictionary<OsmElement, T> matchedElements = new Dictionary<OsmElement, T>();

            // Go
            
            foreach (T dataItem in _dataItems)
            {
                List<OsmNode> closestOsmElements = _osmElements.GetClosestNodesTo(dataItem.Coord, unmatchDistance);

                if (closestOsmElements.Count == 0)
                {
                    if (reportUnmatchedItem)
                    {
                        report.AddEntry(
                            ReportGroup.Unmatched,
                            new IssueReportEntry(
                                "No OSM element found in " + unmatchDistance + " m range of " +
                                dataItem.ReportString() + " at " + dataItem.Coord.OsmUrl,
                                new SortEntryAsc(SortOrder.NoItem),
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
                        matchedElements.Add(matchedOsmElement, dataItem);

                        double matchedOsmElementDistance = OsmGeoTools.DistanceBetween(matchedOsmElement.coord, dataItem.Coord);

                        if (matchedOsmElementDistance > matchDistance)
                        {
                            if (reportMatchedItemFar)
                            {
                                report.AddEntry(
                                    ReportGroup.Unmatched,
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

                        if (reportMatchedItem)
                        {
                            report.AddEntry(
                                ReportGroup.MatchedOsm,
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
                }
            }
            
            if (reportUnmatchedOsm)
            {
                foreach (OsmElement osmElement in _osmElements.Elements)
                {
                    if (matchedElements.TryGetValue(osmElement, out T? _))
                    {
                        // We don't have any logic for this yet
                    }
                    else
                    {
                        if (reportUnmatchedOsm)
                        {
                            report.AddEntry(
                                ReportGroup.Unmatched,
                                new IssueReportEntry(
                                    "No item found in " + unmatchDistance + " m range of OSM element " +
                                    (osmElement.HasKey("name") ? "`" + osmElement.GetValue("name") + "` " : "") +
                                    osmElement.OsmViewUrl,
                                    new SortEntryAsc(SortOrder.NoOsmElement),
                                    osmElement.GetAverageCoord()
                                )
                            );
                            
                            // TODO: report closest (unmatched) data item (these could be really far, so limit distance)
                        }
                    }
                }
            }
        }
        
        
        private enum ReportGroup
        {
            Unmatched,
            MatchedOsm
        }        
        
        private enum SortOrder // values used for sorting
        {
            NoItem = 0,
            NoOsmElement = 0,
            ElementFar = 1
        }
    }
}