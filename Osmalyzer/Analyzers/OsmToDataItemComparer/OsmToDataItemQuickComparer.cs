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

            if (reportUnmatchedItem && !reportMatchedItem) throw new InvalidOperationException("Can't not match if not matching"); 
            if (reportMatchedItemFar && (!reportMatchedItem || !reportUnmatchedItem)) throw new InvalidOperationException("Can't match far if not both matching and unmatching");
            
            double matchDistance = reportMatchedItem ? entries.OfType<MatchedItemQuickComparerReportEntry>().First().Distance : 0;
            double unmatchDistance = reportUnmatchedItem ? entries.OfType<UnmatchedItemQuickComparerReportEntry>().First().Distance : matchDistance;

            report.AddGroup(ReportGroup.UnmatchedOsm, "Issues", null, "All elements appear to be mapped.");

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
                            ReportGroup.UnmatchedOsm,
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
                        matchedElements.Add(matchedOsmElement, dataItem);

                        double matchedOsmElementDistance = OsmGeoTools.DistanceBetween(matchedOsmElement.coord, dataItem.Coord);

                        if (matchedOsmElementDistance > matchDistance)
                        {
                            if (reportMatchedItemFar)
                            {
                                report.AddEntry(
                                    ReportGroup.UnmatchedOsm,
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
        }
        
        
        private enum ReportGroup
        {
            UnmatchedOsm,
            MatchedOsm
        }        
        
        private enum SortOrder // values used for sorting
        {
            NoElement = 0,
            ElementFar = 1
        }
    }
}