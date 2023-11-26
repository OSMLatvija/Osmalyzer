using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

/// <summary>
/// Match OSM elements to custom data items, such as coming from some source.
/// Reusable generic logic for locating and matching items on the map and finding common problems. 
/// </summary>
public class Correlator<T> where T : ICorrelatorItem
{
    private readonly OsmDataExtract _osmElements;
        
    private readonly List<T> _dataItems;
        
    private readonly CorrelatorParamater[] _paramaters;


    public Correlator(
        OsmDataExtract osmElements, 
        List<T> dataItems, 
        params CorrelatorParamater[] paramaters)
    {
        if (osmElements == null) throw new ArgumentNullException(nameof(osmElements));
        if (dataItems == null) throw new ArgumentNullException(nameof(dataItems));
            
        _osmElements = osmElements;
        _dataItems = dataItems;
        _paramaters = paramaters;
    }


    public CorrelatorReport Parse(Report report, params CorrelatorBatch[] entries)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (entries == null) throw new ArgumentNullException(nameof(entries));
            
        // See what sort of filters we have and which matching logic we will need to do (and report)
            
        bool shouldReportMatchedItem = entries.OfType<MatchedPairBatch>().Any();
        bool shouldReportMatchedItemFar = entries.OfType<MatchedFarPairBatch>().Any();
        bool shouldReportUnmatchedItem = entries.OfType<UnmatchedItemBatch>().Any();
        MatchedLoneOsmBatch? matchedLoneOsmBatch = entries.OfType<MatchedLoneOsmBatch>().FirstOrDefault();
        bool shouldReportMatchedLoneOsm = matchedLoneOsmBatch != null;
        bool reportMatchedLoneOsmAsProblem =shouldReportMatchedLoneOsm && matchedLoneOsmBatch!.AsProblem;
        bool shouldReportUnmatchedOsm = entries.OfType<UnmatchedOsmBatch>().Any();

        // Gather (optional) parameters (or set defaults)
            
        double matchDistance = _paramaters.OfType<MatchDistanceParamater>().FirstOrDefault()?.Distance ?? 15;
        double unmatchDistance = _paramaters.OfType<MatchFarDistanceParamater>().FirstOrDefault()?.FarDistance ?? 75;
        Func<T, OsmElement, bool>? matchCallback = _paramaters.OfType<MatchCallbackParameter<T>>().FirstOrDefault()?.MatchCallback ?? null;
        Func<OsmElement, bool>? loneElementAllowanceCallback = _paramaters.OfType<LoneElementAllowanceCallbackParameter>().FirstOrDefault()?.AllowanceCallback ?? null;
        string dataItemLabelSingular = _paramaters.OfType<DataItemLabelsParamater>().FirstOrDefault()?.LabelSingular ?? "item";
        string dataItemLabelPlural = _paramaters.OfType<DataItemLabelsParamater>().FirstOrDefault()?.LabelPlural ?? "items";
        double matchOriginMinReportDistance = _paramaters.OfType<MinOriginReportDistanceParamater>().FirstOrDefault()?.MinDistance ?? 20;

        List<OsmElementPreviewValue> osmElementPreviewParams = _paramaters.OfType<OsmElementPreviewValue>().ToList();

        // Match from data item perspective (i.e. find best OSM element to fit each)

        List<T> itemsToBeMatched = _dataItems.ToList();

        Dictionary<OsmElement, Match> allMatchedElements = new Dictionary<OsmElement, Match>();
        
        List<T> unmatchableItems = new List<T>();

        do
        {
            // We run this in a loop, because finding the closest element to a data item
            // does not mean we couldn't find another data item for which this element is actually closer.
            // So finding I1 ---- E is great, but then we could find I1 ---- E -- I2, which is a better match
            
            // Process all yet-unmatched this loop (first loop would process all)
            List<T> currentlyMatching = itemsToBeMatched.ToList();
            
            // But keep track of items we have to re-match next loop
            itemsToBeMatched.Clear();
            
            foreach (T dataItem in currentlyMatching)
            {
                List<OsmElement> matchableOsmElements = _osmElements.GetClosestElementsTo(dataItem.Coord, unmatchDistance);

                if (matchCallback != null)
                    matchableOsmElements = matchableOsmElements.Where(e => matchCallback(dataItem, e)).ToList();

                if (matchableOsmElements.Count == 0)
                {
                    // Nothing in range, so purely unmatchable
                    unmatchableItems.Add(dataItem);
                }
                else
                {
                    bool matched = false;
                    
                    foreach (OsmElement closeElement in matchableOsmElements) // this is sorted closest first
                    {
                        double distance = OsmGeoTools.DistanceBetween(dataItem.Coord, closeElement.GetAverageCoord());

                        bool far = distance > matchDistance;
                        
                        if (allMatchedElements.TryGetValue(closeElement, out Match? previous))
                        {
                            if (distance < previous.Distance)
                            {
                                // We are closer than previous match, so replace us as the best match and requeue the other item
                                
                                allMatchedElements.Remove(closeElement);
                                allMatchedElements.Add(closeElement, new Match(dataItem, closeElement, distance, far));
                                itemsToBeMatched.Add(previous.Item); // requeue other
                                matched = true;
                                break;
                            }
                        }
                        else
                        {
                            // This element wasn't yet matched, so claim it as our best match 
                            
                            allMatchedElements.Add(closeElement, new Match(dataItem, closeElement, distance, far));
                            matched = true;
                            break;
                        }
                    }

                    if (!matched) // all options were worse
                    {
                        // Couldn't use any of the elements
                        unmatchableItems.Add(dataItem);
                    }
                }
            }

        } while (itemsToBeMatched.Count > 0); // until we either match every one or decide they're unmatchable
        
        // Match from OSM element perspective

        List<OsmElement> unmatchableElements = new List<OsmElement>();

        List<OsmElement> matchedLoneElements = new List<OsmElement>();

        foreach (OsmElement osmElement in _osmElements.Elements)
        {
            if (allMatchedElements.ContainsKey(osmElement))
                continue;

            bool allowedByItself =
                loneElementAllowanceCallback != null &&
                loneElementAllowanceCallback(osmElement);

            if (allowedByItself)
            {
                matchedLoneElements.Add(osmElement);
            }
            else
            {
                unmatchableElements.Add(osmElement);
                // TODO: find closest (unmatched) data item (these could be really far, so limit distance)
            }
        }
        
        // Prepare report group(s)

        report.AddGroup(
            ReportGroup.CorrelationResults,
            "Matching " + dataItemLabelPlural,
            "This lists the results of matching " + dataItemLabelPlural + " and OSM elements to each other."
        );
        
        // Report results

        report.AddEntry(
            ReportGroup.CorrelationResults,
            new GenericReportEntry(
                "There are " + _dataItems.Count + " data items that could potentially match to " + _osmElements.Count + " elements."
            )
        );

        if (shouldReportUnmatchedItem)
        {
            if (unmatchableItems.Count > 0)
            {
                foreach (T dataItem in unmatchableItems)
                {
                    report.AddEntry(
                        ReportGroup.CorrelationResults,
                        new MapPointReportEntry(
                            dataItem.Coord,
                            "No OSM element found in " + unmatchDistance + " m range of " +
                            dataItem.ReportString() + " at " + dataItem.Coord.OsmUrl,
                            MapPointStyle.CorrelatorItemUnmatched
                        )
                    );
                }

                report.AddEntry(
                    ReportGroup.CorrelationResults,
                    new GenericReportEntry(
                        "No OSM element found for " + unmatchableItems.Count + " data item" + (unmatchableItems.Count > 1 ? "s" : "") + ". "
                    )
                );
            }
        }

        if (shouldReportUnmatchedOsm)
        {
            if (unmatchableElements.Count > 0)
            {
                foreach (OsmElement osmElement in unmatchableElements)
                {
                    report.AddEntry(
                        ReportGroup.CorrelationResults,
                        new MapPointReportEntry(
                            osmElement.GetAverageCoord(),
                            "No " + dataItemLabelSingular + " expected in " + unmatchDistance + " m range of OSM element " +
                            OsmElementReportText(osmElement),
                            MapPointStyle.CorrelatorOsmUnmatched
                        )
                    );
                }

                report.AddEntry(
                    ReportGroup.CorrelationResults,
                    new GenericReportEntry(
                        "No data item found for " + unmatchableElements.Count + " OSM element" + (unmatchableElements.Count > 1 ? "s" : "") + ". "
                    )
                );
            }
        }
        
        if (shouldReportMatchedItem)
        {
            List<Match> closeMatchedElements = allMatchedElements
                                               .Where(p => !p.Value.Far)
                                               .Select(p => p.Value)
                                               .ToList();

            if (closeMatchedElements.Count > 0)
            {
                foreach (Match match in closeMatchedElements)
                {
                    report.AddEntry(
                        ReportGroup.CorrelationResults,
                        new MapPointReportEntry(
                            match.Element.GetAverageCoord(),
                            match.Item.ReportString() + " matched OSM element " +
                            OsmElementReportText(match.Element) +
                            " at " + match.Distance.ToString("F0") + " m",
                            MapPointStyle.CorrelatorPairMatched
                        )
                    );

                    if (match.Distance > matchOriginMinReportDistance)
                    {
                        report.AddEntry(
                            ReportGroup.CorrelationResults,
                            new MapPointReportEntry(
                                match.Item.Coord,
                                "Expected location for " + match.Item.ReportString() + " at " + match.Item.Coord.OsmUrl,
                                MapPointStyle.CorrelatorPairMatchedOffsetOrigin
                            )
                        );
                    }
                }

                report.AddEntry(
                    ReportGroup.CorrelationResults,
                    new GenericReportEntry(
                        "Matched " + closeMatchedElements.Count + " data item" + (closeMatchedElements.Count > 1 ? "s" : "") + " to OSM element" + (closeMatchedElements.Count > 1 ? "s" : "") + ". "
                    )
                );
            }
        }
        
        if (shouldReportMatchedItemFar)
        {
            List<Match> farMatchedElements = allMatchedElements
                                             .Where(p => p.Value.Far)
                                             .Select(p => p.Value)
                                             .ToList();

            if (farMatchedElements.Count > 0)
            {
                foreach (Match match in farMatchedElements)
                {
                    report.AddEntry(
                        ReportGroup.CorrelationResults,
                        new MapPointReportEntry(
                            match.Element.GetAverageCoord(),
                            "Matching OSM element " +
                            OsmElementReportText(match.Element) + " found around " +
                            match.Item.ReportString() + ", " +
                            "but it's far away (" + match.Distance.ToString("F0") + " m), expected at " + match.Item.Coord.OsmUrl,
                            MapPointStyle.CorrelatorPairMatchedFar
                        )
                    );

                    report.AddEntry(
                        ReportGroup.CorrelationResults,
                        new MapPointReportEntry(
                            match.Item.Coord,
                            "Expected location for " + match.Item.ReportString() + " at " + match.Item.Coord.OsmUrl,
                            MapPointStyle.CorrelatorPairMatchedFarOrigin
                        )
                    );
                }

                report.AddEntry(
                    ReportGroup.CorrelationResults,
                    new GenericReportEntry(
                        "Matched over longer distance " + farMatchedElements.Count + " data item" + (farMatchedElements.Count > 1 ? "s" : "") + " to OSM element" + (farMatchedElements.Count > 1 ? "s" : "") + ". "
                    )
                );
            }
        }
        
        if (shouldReportMatchedLoneOsm)
        {
            if (matchedLoneElements.Count > 0)
            {
                foreach (OsmElement osmElement in matchedLoneElements)
                {
                    report.AddEntry(
                        ReportGroup.CorrelationResults,
                        new MapPointReportEntry(
                            osmElement.GetAverageCoord(),
                            "Matched OSM element " +
                            OsmElementReportText(osmElement) +
                            " by itself",
                            reportMatchedLoneOsmAsProblem ? MapPointStyle.CorrelatorLoneOsmUnmatched : MapPointStyle.CorrelatorLoneOsmMatched
                        )
                    );
                }

                report.AddEntry(
                    ReportGroup.CorrelationResults,
                    new GenericReportEntry(
                        "Matched " + matchedLoneElements.Count + " lone OSM element" + (matchedLoneElements.Count > 1 ? "s" : "") + " by themselves."
                    )
                );
            }
        }
        
        // todo: legend

        // Store the well-formatted correlated match list
        
        List<Correlation> correlations = new List<Correlation>();

        foreach (Match match in allMatchedElements.Values)
            correlations.Add(new MatchedCorrelation<T>(match.Element, match.Item, match.Distance, match.Far));

        foreach (OsmElement osmElement in matchedLoneElements)
            correlations.Add(new LoneCorrelation(osmElement));
            
        // Return a report about what we parsed and found

        return new CorrelatorReport(correlations);

        
        [Pure]
        string OsmElementReportText(OsmElement element)
        {
            string s = "";
                
            // Add custom labels from values as requested
            
            foreach (OsmElementPreviewValue previewValue in osmElementPreviewParams)
            {
                if (element.HasKey(previewValue.Tag))
                {
                    if (previewValue.Labels.Length == 0)
                    {
                        // Just show the value
                        
                        if (s != "") s += " ";

                        // Just list the value
                        if (previewValue.ShowTag)
                            s += "`" + previewValue.Tag + "=" + element.GetValue(previewValue.Tag) + "`";
                        else
                            s += "`" + element.GetValue(previewValue.Tag) + "`";
                    }
                    else
                    {
                        // Only show the value for specific recognized labels
                        
                        OsmElementPreviewValue.PreviewLabel? label = previewValue.Labels.FirstOrDefault(l => l.Value == element.GetValue(previewValue.Tag));

                        if (label != null)
                        {
                            if (s != "") s += " ";
                            
                            if (previewValue.ShowTag)
                                 s += "`" + previewValue.Tag + "` ";
                            
                            s += label.Label;
                        }
                    }
                }
            }
            
            // Default URL stuff with ID and such
            
            if (s != "") s += " ";
            s += element.OsmViewUrl;
                
            return s;
        }
    }


    private class Match
    {
        public T Item { get; }

        public OsmElement Element { get; }

        public double Distance { get; }

        public bool Far { get; }

        
        public Match(T item, OsmElement element, double distance, bool far)
        {
            Item = item;
            Element = element;
            Distance = distance;
            Far = far;
        }
    }

    private enum ReportGroup
    {
        CorrelationResults = -10 // probably before analyzer extra issues
    }
}