using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    public class OsmToDataItemQuickComparer<T> where T : IQuickComparerDataItem
    {
        private readonly OsmDataExtract _osmElements;
        
        private readonly List<T> _dataItems;
        
        private readonly Func<T, OsmElement, bool> _matchCallback;
        

        public OsmToDataItemQuickComparer(
            OsmDataExtract osmElements, 
            List<T> dataItems, 
            Func<T, OsmElement, bool> matchCallback)
        {
            if (osmElements == null) throw new ArgumentNullException(nameof(osmElements));
            if (dataItems == null) throw new ArgumentNullException(nameof(dataItems));
            if (matchCallback == null) throw new ArgumentNullException(nameof(matchCallback));
            
            _osmElements = osmElements;
            _dataItems = dataItems;
            _matchCallback = matchCallback;
        }


        public QuickCompareReport<T> Parse(Report report, params QuickComparerReportEntry[] entries)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            
            // See what sort of filters we have and which matching logic we will need to do (and report)
            
            bool reportMatchedItem = entries.OfType<MatchedItemQuickComparerReportEntry>().Any();
            bool reportMatchedItemFar = entries.OfType<MatchedItemButFarQuickComparerReportEntry>().Any();
            bool reportUnmatchedItem = entries.OfType<UnmatchedItemQuickComparerReportEntry>().Any();
            bool reportUnmatchedOsm = entries.OfType<UnmatchedOsmQuickComparerReportEntry>().Any();

            double matchDistance = reportMatchedItem ? entries.OfType<MatchedItemQuickComparerReportEntry>().First().Distance : 0;
            double unmatchDistance = reportUnmatchedItem ? entries.OfType<UnmatchedItemQuickComparerReportEntry>().First().Distance : matchDistance;
            Func<OsmElement, bool>? unmatchedOsmElementAllowedByItselfCallback = reportUnmatchedOsm ? entries.OfType<UnmatchedOsmQuickComparerReportEntry>().First().AllowedByItselfCallback : null;

            // Prepare report groups

            if (reportUnmatchedItem || reportUnmatchedOsm || reportMatchedItemFar)
            {
                report.AddGroup(
                    ReportGroup.Unmatched,
                    "Unmatched items",
                    "This lists the items and elements that could not be matched to each other.",
                    "All elements appear to be mapped."
                );
            }

            if (reportMatchedItem)
            {
                report.AddGroup(
                    ReportGroup.MatchedOsm, 
                    "Matched items",
                    "This displays a map of all the items that were matched to each other."
                );
            }

            // Go

            Dictionary<OsmElement, T> matchedElements = new Dictionary<OsmElement, T>();
            
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
                                        OsmElementReportText(matchedOsmElement) + " found close to " +
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
                                    OsmElementReportText(matchedOsmElement) +
                                    " at " + matchedOsmElementDistance.ToString("F0") + " m"
                                )
                            );
                        }
                    }
                }
            }
            
            foreach (OsmElement osmElement in _osmElements.Elements)
            {
                if (matchedElements.ContainsKey(osmElement))
                    continue;

                bool allowedByItself =
                    unmatchedOsmElementAllowedByItselfCallback != null &&
                    unmatchedOsmElementAllowedByItselfCallback(osmElement);
                
                if (!allowedByItself)
                {
                    if (reportUnmatchedOsm)
                    {
                        report.AddEntry(
                            ReportGroup.Unmatched,
                            new IssueReportEntry(
                                "No item found in " + unmatchDistance + " m range of OSM element " +
                                OsmElementReportText(osmElement),
                                new SortEntryAsc(SortOrder.NoOsmElement),
                                osmElement.GetAverageCoord()
                            )
                        );
                        
                        // TODO: report closest (unmatched) data item (these could be really far, so limit distance)
                    }
                }
                else
                {
                    if (reportMatchedItem)
                    {
                        report.AddEntry(
                            ReportGroup.MatchedOsm,
                            new MapPointReportEntry(
                                osmElement.GetAverageCoord(),
                                "Matched OSM element by itself " +
                                OsmElementReportText(osmElement)
                            )
                        );
                    }
                }
            }
            
            // Return a report about what we parsed and found

            return new QuickCompareReport<T>(matchedElements);
        }

        
        [Pure]
        private static string OsmElementReportText(OsmElement element)
        {
            return 
                (element.HasKey("name") ? "`" + element.GetValue("name") + "` " : "") + 
                element.OsmViewUrl;
        }


        private enum ReportGroup
        {
            Unmatched = -10, // probably before analyzer extra issues
            MatchedOsm = 100 // probably after analyzer issues
        }        
        
        private enum SortOrder // values used for sorting
        {
            NoItem = 0,
            NoOsmElement = 0,
            ElementFar = 1
        }
    }
}