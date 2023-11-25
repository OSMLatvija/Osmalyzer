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
            
        bool shouldReportMatchedItem = entries.OfType<MatchedItemBatch>().Any();
        bool shouldReportMatchedItemFar = entries.OfType<MatchedFarItemBatch>().Any();
        bool shouldReportUnmatchedItem = entries.OfType<UnmatchedItemBatch>().Any();
        bool shouldReportUnmatchedOsm = entries.OfType<UnmatchedOsmBatch>().Any();

        // Gather (optional) parameters (or set defaults)
            
        double matchDistance = _paramaters.OfType<MatchDistanceParamater>().FirstOrDefault()?.Distance ?? 15;
        double unmatchDistance = _paramaters.OfType<MatchFarDistanceParamater>().FirstOrDefault()?.FarDistance ?? 75;
        Func<T, OsmElement, bool>? matchCallback = _paramaters.OfType<MatchCallbackParameter<T>>().FirstOrDefault()?.MatchCallback ?? null;
        Func<OsmElement, bool>? loneElementAllowanceCallback = _paramaters.OfType<LoneElementAllowanceCallbackParameter>().FirstOrDefault()?.AllowanceCallback ?? null;
        string dataItemLabelSingular = _paramaters.OfType<DataItemLabelsParamater>().FirstOrDefault()?.LabelSingular ?? "item";
        string dataItemLabelPlural = _paramaters.OfType<DataItemLabelsParamater>().FirstOrDefault()?.LabelPlural ?? "items";

        List<OsmElementPreviewValue> osmElementPreviewParams = _paramaters.OfType<OsmElementPreviewValue>().ToList();

        // Match from data item perspective (i.e. find best OSM element to fit each)

        List<T> itemsToBeMatched = _dataItems.ToList();

        Dictionary<OsmElement, (T, double)> matchedElements = new Dictionary<OsmElement, (T, double)>();
        
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
                        
                        if (matchedElements.TryGetValue(closeElement, out (T, double) previous))
                        {
                            if (distance < previous.Item2)
                            {
                                // We are closer than previous match, so replace us as the best match and requeue the other item
                                
                                matchedElements.Remove(closeElement);
                                matchedElements.Add(closeElement, (dataItem, distance));
                                itemsToBeMatched.Add(previous.Item1); // requeue other
                                matched = true;
                                break;
                            }
                        }
                        else
                        {
                            // This element wasn't yet matched, so claim it as our best match 
                            
                            matchedElements.Add(closeElement, (dataItem, distance));
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
            if (matchedElements.ContainsKey(osmElement))
                continue;

            bool allowedByItself =
                loneElementAllowanceCallback != null &&
                loneElementAllowanceCallback(osmElement);

            if (!allowedByItself)
            {
                unmatchableElements.Add(osmElement);
                // TODO: find closest (unmatched) data item (these could be really far, so limit distance)
            }
            else
            {
                matchedLoneElements.Add(osmElement);
            }
        }
        
        // Prepare report group(s)

        report.AddGroup(
            ReportGroup.CorrelationResults,
            "Matching " + dataItemLabelPlural,
            "This lists the results of matching " + dataItemLabelPlural + " and OSM elements to each other."
        );
        
        // todo: generic summary, #s, legend
        
        // Report results

        if (shouldReportUnmatchedItem)
        {
            foreach (T dataItem in unmatchableItems)
            {
                report.AddEntry(
                    ReportGroup.CorrelationResults,
                    new MapPointReportEntry(
                        dataItem.Coord,
                        "No matchable OSM element found in " + unmatchDistance + " m range of " +
                        dataItem.ReportString() + " at " + dataItem.Coord.OsmUrl,
                        MapPointStyle.Problem
                    )
                );
            }
        }

        if (shouldReportUnmatchedOsm)
        {
            foreach (OsmElement osmElement in unmatchableElements)
            {
                report.AddEntry(
                    ReportGroup.CorrelationResults,
                    new MapPointReportEntry(
                        osmElement.GetAverageCoord(),
                        "No " + dataItemLabelSingular + " expected in " + unmatchDistance + " m range of OSM element " +
                        OsmElementReportText(osmElement),
                        MapPointStyle.Unwanted
                    )
                );
            }
        }
        
        if (shouldReportMatchedItem || shouldReportMatchedItemFar)
        {
            foreach ((OsmElement? osmElement, (T? dataItem, double distance)) in matchedElements)
            {
                if (shouldReportMatchedItem)
                {
                    report.AddEntry(
                        ReportGroup.CorrelationResults,
                        new MapPointReportEntry(
                            osmElement.GetAverageCoord(),
                            dataItem.ReportString() + " matched OSM element " +
                            OsmElementReportText(osmElement) +
                            " at " + distance.ToString("F0") + " m",
                            MapPointStyle.Okay
                        )
                    );
                }

                if (shouldReportMatchedItemFar)
                {
                    if (distance > matchDistance)
                    {
                        report.AddEntry(
                            ReportGroup.CorrelationResults,
                            new MapPointReportEntry(
                                osmElement.GetAverageCoord(),
                                "Matching OSM element " +
                                OsmElementReportText(osmElement) + " found around " +
                                dataItem.ReportString() + ", " +
                                "but it's far away (" + distance.ToString("F0") + " m), expected at " + dataItem.Coord.OsmUrl,
                                MapPointStyle.Dubious
                            )
                        );                        
                        
                        report.AddEntry(
                            ReportGroup.CorrelationResults,
                            new MapPointReportEntry(
                                dataItem.Coord,
                                "Expected location for " + dataItem.ReportString() + " at " + dataItem.Coord.OsmUrl,
                                MapPointStyle.Expected
                            )
                        );
                    }
                }
            }
        }
        
        if (shouldReportMatchedItem)
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
                        MapPointStyle.Okay
                    )
                );
            }
        }

        // Store the well-formatted correlated match list
        
        List<Correlation> correlations = new List<Correlation>();

        foreach ((OsmElement osmElement, (T dataItem, double distance)) in matchedElements)
            correlations.Add(new MatchedCorrelation<T>(osmElement, dataItem, distance));

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


    private enum ReportGroup
    {
        CorrelationResults = -10 // probably before analyzer extra issues
    }
}