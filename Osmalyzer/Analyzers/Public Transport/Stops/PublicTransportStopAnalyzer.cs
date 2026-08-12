namespace Osmalyzer;

public abstract class PublicTransportStopAnalyzer<T> : PublicTransportStopAnalyzerBase
    where T : GTFSAnalysisData, new()
{
    public override string Description => 
        "This checks public transport stops for " + Name + " operator.";

    public override AnalyzerGroup Group => AnalyzerGroup.PublicTransportStops;
    
    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(T) ];


    /// <summary> Very short label for report texts </summary>
    protected abstract string Label { get; } // todo: not used, wire
    
    protected abstract bool CheckStopBoardingTypes { get; }
    
    protected abstract string OsmTaggingValueForOperator { get; }
    protected abstract string? OsmTaggingValueForOperatorWikidata { get; }
    protected abstract OsmCoord? StopMatchingCenter { get; }
    protected abstract double? StopMatchingDistance { get; }
    protected abstract string? GtfsDisambiguatorId { get; }
    protected abstract bool RegionalService { get; }


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
                        new HasValue("highway", "bus_stop"),
                        new HasValue("railway", "tram_stop"),
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
        
        // Correlate OSM stops to data stops
        
        report.AddGroup(
            GroupDesignation.StopMatching,
            "Stop matching"
        );

        // Convert GTFS stops to data items for correlation
        List<GTFSStopDataItem> stopDataItems = gtfsNetwork.Stops.Stops
                                                          .Select(stop => new GTFSStopDataItem(stop))
                                                          .ToList();

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
            if (osmStopName != null && IsStopNameMatchGoodEnough(gtfsStop.Stop.Name, osmStopName))
                return MatchStrength.Good;

            if (RegionalService)
            {
                string? osmRegionalStopName = osmElement.GetValue("reg_name");
                if (osmRegionalStopName != null && IsStopNameMatchGoodEnough(gtfsStop.Stop.Name, osmRegionalStopName))
                    return MatchStrength.Good;
            }

            string? osmAltName = osmElement.GetValue("alt_name");
            if (osmAltName != null && IsStopNameMatchGoodEnough(gtfsStop.Stop.Name, osmAltName))
                return MatchStrength.Regular; // signs and data may occasionally differ
            // TODO: REPORT THESE - THEY MIGHT GET FIXED OVER TIME

            return MatchStrength.Unmatched;
        }

        // Run the correlation
        CorrelatorReport correlatorReport = correlator.Parse(
            report,
            GroupDesignation.StopMatching,
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
        
        // Validate stops and propose changes
        
        if (CheckStopBoardingTypes)
        {
            Validator<GTFSStopDataItem> stopValidator = new Validator<GTFSStopDataItem>(
                correlatorReport,
                "Stop tagging issues"
            );

            Validation validation = stopValidator.Validate(
                report,
                false, // don't validate unmatched elements (we only care about matched stops)
                false, // don't validate lone OSM elements
                
                // todo: name? regional name?
                
                //new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>("ref", gtfsStopItem => gtfsStopItem.Stop.Id),

                new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>(
                    e => !ShouldDisambiguateGtfsStopId(e), // for single or no id - can use directly (without disambiguating)
                    "gtfs:stop_id",
                    gtfsStopItem => gtfsStopItem.Stop.Id
                ),
                new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>(
                    e => GtfsDisambiguatorId != null && ShouldDisambiguateGtfsStopId(e), // for multiple ids - need to disambiguate
                    "gtfs:stop_id:" + GtfsDisambiguatorId,
                    gtfsStopItem => gtfsStopItem.Stop.Id
                ),

                new ValidateElementHasValue("operator", OsmTaggingValueForOperator),
                new ValidateElementHasValue("operator:wikidata", OsmTaggingValueForOperatorWikidata),
                new ValidateElementValueMatchesDataItemValue<GTFSStopDataItem>("request_stop", GetExpectedRequestStopValue)
            );
            
            string? GetExpectedRequestStopValue(GTFSStopDataItem gtfsStopItem)
            {
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
    }
   
    
    [Pure]
    private static bool IsStopNameMatchGoodEnough(string ptStopName, string osmStopName)
    {
        // Quick check first, may be we don't need to do anything
        if (ptStopName == osmStopName)
            return true;

        if (CleanedStopNameCache.TryGetValue(ptStopName, out string? cleanedPtStopName))
        {
            // We already cleaned this name, so use it
            ptStopName = cleanedPtStopName;
        }
        else
        {
            // Clean the name and store it for later
            cleanedPtStopName = CleanName(ptStopName);
            CleanedStopNameCache[ptStopName] = cleanedPtStopName;
            ptStopName = cleanedPtStopName;
        }

        if (CleanedStopNameCache.TryGetValue(osmStopName, out string? cleanedOsmStopName))
        {
            // We already cleaned this name, so use it
            osmStopName = cleanedOsmStopName;
        }
        else
        {
            // Clean the name and store it for later
            cleanedOsmStopName = CleanName(osmStopName);
            CleanedStopNameCache[osmStopName] = cleanedOsmStopName;
            osmStopName = cleanedOsmStopName;
        }
        

        [Pure]
        static string CleanName(string name)
        {
            // Stops never really differ by capitalization, so just lower them and avoid weird capitalization in addition to everything else
            // Rezekne "18.Novembra iela" vs OSM "18. novembra iela"
            name = name.ToLower();

            // Remove errornous spaces
            // e.g. ATD "DS  Salūts"
            name = Regex.Replace(name, @"\s{2,}", @" ");
            
            // Trim parenthesis from OSM
            // Jurmalas OSM stops have a lot of parenthesis, like JS "Majoru stacija" vs OSM "Majoru stacija (Majori)"
            name = Regex.Replace(name, @" \([^\(\)]+\)$", @"");
            
            // Trim brackets from GTFS
            // A couple Jurmalas GTFS stops have brackets, like JS "Promenādes iela [Promenādes iela]" and "Promenādes iela [Rīgas iela]" vs OSM "Promenādes iela" and "Promenādes iela"
            name = Regex.Replace(name, @" \[[^\[\]]+\]$", @"");

            // Sometimes proper quotes are inconsistent between the two
            // OSM "Arēna "Rīga"" vs RS "Arēna Rīga" or OSM ""Bērnu pasaule"" vs RS "Bērnu pasaule"
            // or opposite OSM "Dzintars" vs RS ""Dzintars""
            name = name.Replace("\"", "");
            
            // Both OSM and RS stops are inconsistent about spacing around characters
            // "2.trolejbusu parks" or "Jaunciema 2.šķērslīnija" also all the abbreviated "P.Lejiņa iela"
            // "Upesgrīvas iela/ Spice"
            // OSM "TEC-2 pārvalde" vs RS "TEC- 2 pārvalde" or OSM "Preču-2" vs RS "Preču - 2"
            name = Regex.Replace(name, @"(?<! )([\./-])", @" $1");
            name = Regex.Replace(name, @"([\./-])(?! )", @"$1 ");

            return name;
        }
        

        // Are cleaned names equal?
        if (ptStopName == osmStopName)
            return true;
        
        
        // Special checks
        
        // RS likes to abbreviate names for stops while OSM spells them out
        // OSM "Eduarda Smiļģa iela" vs RS "E.Smiļģa iela"
        // Because there are so many like this, I will consider them correct for now, even if they aren't technically accurate 
        if (ptStopName.Contains('.') && !osmStopName.Contains('.'))
        {
            string[] ptSplit = ptStopName.Split('.');
            if (ptSplit.Length == 2)
            {
                string ptPrefix = ptSplit[0].TrimEnd(); // "E"
                string ptSuffiix = ptSplit[1].TrimStart(); // "Smiļģa iela"
    
                if (osmStopName.StartsWith(ptPrefix) && osmStopName.EndsWith(ptSuffiix)) // not a perfect check, but good enough
                    return true;
            }
        }
            
        // RS also has some double names for some reason when OSM and real-life has just one "part"
        // RS "Botāniskais dārzs/Rīgas Stradiņa universitāte" vs OSM "Botāniskais dārzs
        if (ptStopName.Contains('/'))
        {
            string[] ptSplit = ptStopName.Split('/');
            if (ptSplit.Length == 2)
            {
                string ptFirst = ptSplit[0].TrimEnd(); // "Botāniskais dārzs"
                string ptSecond = ptSplit[1].TrimStart(); // "Rīgas Stradiņa universitāte"
    
                if (osmStopName == ptFirst || osmStopName == ptSecond)
                    return true;
            }
        }
            
        // Couldn't match anything
        return false;
    }
    

    private enum GroupDesignation
    {
        StopMatching,
        ProposedChanges
    }
}