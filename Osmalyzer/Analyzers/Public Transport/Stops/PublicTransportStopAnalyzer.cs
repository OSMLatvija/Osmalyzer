namespace Osmalyzer;

public abstract class PublicTransportStopAnalyzer<T> : Analyzer
    where T : GTFSAnalysisData, new()
{
    public sealed override string Name => Label + " Stops";
    
    public sealed override string Description => 
        "This checks public transport stops for " + Name + " operator.";

    public sealed override AnalyzerGroup Group => AnalyzerGroup.PublicTransportStops;
    
    public sealed override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(T) ];


    /// <summary> Very short label for report texts </summary>
    protected abstract string Label { get; } // todo: not used, wire

    protected abstract string OsmTaggingValueForOperator { get; }
    protected abstract string? OsmTaggingValueForOperatorWikidata { get; }
    protected abstract OsmCoord? StopMatchingCenter { get; }
    protected abstract double? StopMatchingDistance { get; }
    protected abstract string? GtfsDisambiguatorId { get; }
    protected abstract bool RegionalService { get; }
    protected abstract bool ValidateOsmPickupDropoffValues { get; }


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load stop data

        GTFSAnalysisData gtfsData = datas.OfType<T>().First();

        GTFSNetwork gtfsNetwork = gtfsData.Network;
            
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData masterData = osmData.MasterData;

        List<OsmData> routeData = masterData.Filter(
            [
                [
                    new IsNode(),
                    new OrMatch(
                        new HasValue("highway", "bus_stop"), // (can share with tram too) 
                        new HasValue("railway", "tram_stop"), // not railway=platform which can be separate node/way
                        // not public_transport=platform which can be by itself
                        new HasValue("disused:railway", "tram_stop") // still valid, if not in active use - Rigas Satiksme data seems to have these too
                    )
                ],
                [
                    new IsRelation(),
                    new HasValue("type", "route"),
                    new OrMatch(
                        new HasAnyValue("route", "tram", "bus", "trolleybus"),
                        new HasAnyValue("disused:route", "tram", "bus", "trolleybus")
                    )
                ]
            ]
        );
            
        OsmData osmStops = routeData[0];
        OsmData osmRoutes = routeData[1];

        // Convert GTFS stops to data items for correlation
        
        List<GTFSStopDataItem> stopDataItems = gtfsNetwork.Stops.Stops
                                                          .Where(s => s.LocationType is GTFSStopLocationType.Stop or GTFSStopLocationType.Unknown)
                                                          .Select(stop => new GTFSStopDataItem(stop))
                                                          .ToList();
        
        
        // Correlate OSM stops to data stops

        Correlator<GTFSStopDataItem> correlator = new Correlator<GTFSStopDataItem>(
            osmStops,
            stopDataItems,
            new MatchDistanceParamater(50),
            new MatchFarDistanceParamater(150),
            new MatchCallbackParameter<GTFSStopDataItem>(IsGoodStopMatch),
            new LoneElementAllowanceParameter(DoesStopLookLikeOperatorsStop),
            new DataItemLabelsParamater("stop", "stops"),
            new OsmElementPreviewValue("name", false),
            new OsmElementPreviewValue("ref", false)
        );

        // Create correlator with appropriate parameters
        MatchStrength IsGoodStopMatch(GTFSStopDataItem gtfsStop, OsmElement osmElement)
        {
            // Explicit `gtfs:stop_id`
            if (GtfsDisambiguatorId != null)
            {
                string? gtfsStopDabIdTag = osmElement.GetValue("gtfs:stop_id:" + GtfsDisambiguatorId);
                if (gtfsStopDabIdTag != null && gtfsStopDabIdTag == gtfsStop.Stop.Id)
                    return MatchStrength.Strong;
            }

            string? gtfsStopIdTag = osmElement.GetValue("gtfs:stop_id");
            if (gtfsStopIdTag != null && gtfsStopIdTag == gtfsStop.Stop.Id)
                return MatchStrength.Strong;
            
            string? refTag = osmElement.GetValue("ref"); // weaker than `gtfs:stop_id` but still a strong match
            if (refTag != null && refTag == gtfsStop.Stop.Id)
                return MatchStrength.Strong;
            
            string? unsignedRefTag = osmElement.GetValue("unsigned_ref"); 
            if (unsignedRefTag != null && unsignedRefTag == gtfsStop.Stop.Id)
                return MatchStrength.Strong;
            
            string? osmStopName = osmElement.GetValue("name");

            // Use existing name matching logic
            if (osmStopName != null && PublicTransportHelpers.IsStopNameMatchGoodEnough(gtfsStop.Stop.Name, osmStopName))
                return MatchStrength.Good;

            if (RegionalService)
            {
                string? osmRegionalStopName = osmElement.GetValue("reg_name");
                if (osmRegionalStopName != null && PublicTransportHelpers.IsStopNameMatchGoodEnough(gtfsStop.Stop.Name, osmRegionalStopName))
                    return MatchStrength.Good;
            }

            string? osmAltName = osmElement.GetValue("alt_name");
            if (osmAltName != null && PublicTransportHelpers.IsStopNameMatchGoodEnough(gtfsStop.Stop.Name, osmAltName))
                return MatchStrength.Regular; // signs and data may occasionally differ
            // TODO: REPORT THESE - THEY MIGHT GET FIXED OVER TIME

            return MatchStrength.Unmatched;
        }

        // Run the correlation
        CorrelatorReport correlatorReport = correlator.Parse(
            report,
            "This matches `highway=bus_stop` and `railway=tram_stop` elements, but not `public_transport=platform` itself. " +
            // todo: explain more about proper tagging
            "This reports unmatched and lone stops, but these are not necessarily errors and could mean different operator stops, disused stops, planned stops, etc. " +
            (StopMatchingCenter != null && StopMatchingDistance != null ? "All stops are matched within " + StopMatchingDistance + " m of assumed operator's area of influence, but this of course does not actually mean that they are the operator's stops." : "") +
            "The operator may also change stops and routes temporarily, which affects this matching, so verify any inconsistencies against the operator's info. " +
            "OSM stop tagging is also particularly complicated, so everything has to be double-check against intended and actual data.",
            new MatchedPairBatch(),
            new MatchedFarPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedLoneOsmBatch(true)
        );
        
        bool DoesStopLookLikeOperatorsStop(OsmElement osmStop)
        {
            // Has operator's tags?

            string? operatorTag = osmStop.GetValue("operator");
            if (operatorTag != null && operatorTag.ToLower() == OsmTaggingValueForOperator.ToLower())
                return true;

            string? operatorWikidataTag = osmStop.GetValue("operator:wikidata");
            if (OsmTaggingValueForOperatorWikidata != null)
            {
                if (operatorWikidataTag != null && operatorWikidataTag.ToLower() == OsmTaggingValueForOperatorWikidata.ToLower())
                    return true;
            }

            // Has operator's route?

            if (osmStop.Relations != null)
            {
                foreach (OsmRelationMember member in osmStop.Relations)
                {
                    // todo: not using actual routes we find

                    if (member.Type == OsmElement.OsmElementType.Relation)
                    {
                        if (member.Element!.HasValue("type", "route") &&
                            member.Element!.HasValue("route", "tram", "bus", "trolleybus"))
                        {
                            string? routeOperatorTag = member.Element.GetValue("operator");
                            if (routeOperatorTag != null && routeOperatorTag.ToLower() == OsmTaggingValueForOperator.ToLower())
                                return true;

                            string? routeOperatorWikidataTag = member.Element.GetValue("operator:wikidata");
                            if (routeOperatorWikidataTag != null && routeOperatorWikidataTag.ToLower() == OsmTaggingValueForOperatorWikidata.ToLower())
                                return true;
                        }
                    }
                }
            }
            
            // Is explicitly regional?
            
            // `network=LV:regional` ?
            string? networkTag = osmStop.GetValue("network");
            if (networkTag != null && networkTag.ToLower().Contains("regional"))
                return false;

            // `service=regional` ?
            string? serviceTag = osmStop.GetValue("service");
            if (serviceTag != null && serviceTag.ToLower() == "regional")
                return false;
            
            // Within distance of expected?
            
            if (StopMatchingCenter != null && StopMatchingDistance != null)
            {
                double distance = OsmGeoTools.DistanceBetween(StopMatchingCenter!.Value, osmStop.AverageCoord);
                if (distance <= StopMatchingDistance)
                {
                    if (operatorTag == null && operatorWikidataTag == null) // no other operator specified
                        return true; // we assume stops within our influence might be ours since they aren't tagged as not ours 
                }
            }

            return false;
        }
        
        // Additional osm-data stop stuff

        foreach (MatchedCorrelation<GTFSStopDataItem> match in correlatorReport.Correlations.OfType<MatchedCorrelation<GTFSStopDataItem>>())
        {
            if (match.OsmElement.Relations != null)
            {
                foreach (OsmRelationMember relationMember in match.OsmElement.Relations)
                {
                    // new HasValue("type", "route"),
                    // new OrMatch(
                    //     new HasAnyValue("route", "tram", "bus", "trolleybus"),
                    //     new HasAnyValue("disused:route", "tram", "bus", "trolleybus")
                    // )

                    OsmRelation relation = relationMember.Owner;
                    if (relation.HasValue("type", "route"))
                    {
                        if (relation.HasValue(["route", "disused:route"], "bus")) match.DataItem.ImpliedBus = true;
                        if (relation.HasValue(["route", "disused:route"], "tram")) match.DataItem.ImpliedTram = true;
                        if (relation.HasValue(["route", "disused:route"], "trolleybus")) match.DataItem.ImpliedTrolleybus = true;
                        // We are assuming that disused is temporary, such as due to roadworks or seasonal or something
                    }
                }
            }
        }
        
        // Validate stops and propose changes
        
        Validator<GTFSStopDataItem> stopValidator = new Validator<GTFSStopDataItem>(
            correlatorReport,
            "Stop tagging issues"
        );

        Validation validation = stopValidator.Validate(
            report,
            false, // don't validate unmatched elements (we only care about matched stops)
            false, // don't validate lone OSM elements
                
            // todo: name? regional name?
                
            new ValidateElementHasValue("operator", OsmTaggingValueForOperator),
            new ValidateElementHasValue("operator:wikidata", OsmTaggingValueForOperatorWikidata),

            new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>(
                (e,_) => !ShouldDisambiguateGtfsStopId(e), // for single or no id - can use directly (without disambiguating)
                "gtfs:stop_id",
                gtfsStopItem => gtfsStopItem.Stop.Id
            ),
            new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>(
                (e,_) => GtfsDisambiguatorId != null && ShouldDisambiguateGtfsStopId(e), // for multiple ids - need to disambiguate
                "gtfs:stop_id:" + GtfsDisambiguatorId,
                gtfsStopItem => gtfsStopItem.Stop.Id
            ),
            //new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>("ref", gtfsStopItem => gtfsStopItem.Stop.Id),
                
            new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>("request_stop", GetExpectedRequestStopValue),
                
            new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>((e,d) => ShouldCheckField(e, "bus", d.Bus), "bus", i => i.Bus ? "yes" : ""),
            new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>((e,d) => ShouldCheckField(e, "tram", d.Tram), "tram", i => i.Tram ? "yes" : ""),
            new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>((e,d) => ShouldCheckField(e, "trolleybus", d.Trolleybus), "trolleybus", i => i.Trolleybus ? "yes" : "")
        );

        bool ShouldCheckField(OsmElement element, string tag, bool expected)
        {
            if (!expected)
                if (element.GetValue(tag) == "no")
                    return false; // explicitly tagged as `no`, so we can assume it's already correct, although not expected to be set by default 
                
            return true;
        }

        string? GetExpectedRequestStopValue(GTFSStopDataItem gtfsStopItem)
        {
            if (!ValidateOsmPickupDropoffValues)
                return null; // Don't validate if not enabled
            
            // Determine expected request_stop value based on GTFS pickup type
            if (gtfsStopItem.Stop.CommonPickupType == GTFSPickupType.MustCoordinateWithDriverToArrangePickup)
                return "yes";
            else if (gtfsStopItem.Stop.CommonPickupType == GTFSPickupType.RegularlyScheduledPickup)
                return "no"; // Regular stops shouldn't have the tag
            else
                return null; // Unknown/mixed/other values - don't know what to tag
        }
            
        bool ShouldDisambiguateGtfsStopId(OsmElement stop)
        {
            foreach (string key in stop.AllKeys!) // implied not null if we are matches as stop
                if (key.StartsWith("gtfs:stop_id:"))
                    return true; // Any existing disambiguation means we should disambiguate

            return false;
        }

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmStops, validation.Changes, this);
        SuggestedActionApplicator.ExplainForReport(validation.Changes, report, GroupDesignation.ProposedChanges);
#endif
    }
   
    
    private enum GroupDesignation
    {
        ProposedChanges
    }
}