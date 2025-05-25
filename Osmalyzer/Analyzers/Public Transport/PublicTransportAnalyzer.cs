namespace Osmalyzer;

public abstract class PublicTransportAnalyzer<T> : PublicTransportAnalyzerBase
    where T : GTFSAnalysisData, new()
{
    public override string Description => 
        "This checks public transport routes for " + Name + " operator. " +
        "Each operator's GTFS route is matched against OSM routes based on the expected stops. " +
        "Note that this might result in poor and incorrect matches if OSM doesn't have a matching route mapped " +
        "or some other route matches it instead (because of similar stops). " +
        "Each route is shown in its own section so it can be compared with the matched OSM route, if any. " +
        "Note that GTFS stores routes differently than OSM and not neccessarilly all should be mapped. " +
        "Technically, GTFS doesn't have route \"variants\" - the ones below are collected from repeating unique stop sequences.";

    public override AnalyzerGroup Group => AnalyzerGroup.PublicTransport;
    
    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(T) ];


    /// <summary> Very short label for report texts </summary>
    protected abstract string Label { get; }


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load stop data

        GTFSAnalysisData gtfsData = datas.OfType<T>().First();

        GTFSNetwork gtfsNetwork = gtfsData.Network;
            
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        List<OsmDataExtract> osmDataExtracts = osmMasterData.Filter(
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
            
        OsmDataExtract osmStops = osmDataExtracts[0];
        OsmDataExtract osmRoutes = osmDataExtracts[1];

        // Parse routes into variants

        // GTFS is grouped by route and service, but these don't actually represent a "route" the way a user might expect.
        // Websites (like RS) group by stop pattern/variant, which are matching trips in GTFS.
        // So we want to group by such "variants" to match how users and OSM would expect them.

        List<RouteVariant> routeVariants = [ ];

        List<RouteVariant> skippedVariants = [ ];

        const int minTripCountToInclude = 5;

        foreach (GTFSRoute gtfsRoute in gtfsNetwork.Routes.Routes)
            foreach (RouteVariant variant in ExtractVariantsFromRoute(gtfsRoute).OrderByDescending(mt => mt.TripCount))
                routeVariants.Add(variant);
        
        // Clean up GTFS data

        CleanUpGtfsData(gtfsNetwork);
        
        // Add "supergroups" for routes since there are so many

        foreach (GTFSRoute gtfsRoute in gtfsNetwork.Routes.Routes)
        {
            string header = gtfsRoute.CleanType + " #" + gtfsRoute.Number + " \"" + gtfsRoute.Name + "\"";

            report.AddGroup(
                gtfsRoute, // supergroup "ID" as parent for variants
                header,
                null,
                null,
                false
            );
        }
        
        // Match OSM routes to data routes

        List<RoutePair> routePairs = MatchOsmRoutesToRouteVariants(osmRoutes, routeVariants);
        
        // Show results for each route variant

        foreach (RouteVariant variant in routeVariants)
        {
            (RoutePair? routePair, List<StopMatch> stopMatches) = GetStopMatches(routePairs, variant);

            // Skip route if we don't have a match and it's not frequent enough
            
            if (routePair == null && // no OSM route matched, so we might not want to even show if it's infrequent
                variant.TripCount < minTripCountToInclude) // todo: optional, e.g. JAP lists them but RS doesn't
            {
                // Not enough to report as a "full" route, presumably first/final depo routes 
                skippedVariants.Add(variant);
                continue;
            }
            
            // Display this route
            
            string header = variant.Route.CleanType + " #" + variant.Route.Number + " from " + variant.FirstStop.Name + " to " + variant.LastStop.Name;

            if (routePair != null)
            {
                string? osmRouteName = routePair.OsmRoute.GetValue("name");

                if (osmRouteName != null)
                    header += " — \"" + osmRouteName + "\"";
            }

            report.AddGroup(
                variant, // our group "ID"
                variant.Route, // parent group "ID"
                header,
                null,
                null,
                false,
                false // don't cluster stops, we want "discrete" preview
            );

            // Route and match generic info

            int routeVariantCount = routeVariants.Count(rv => rv.Route == variant.Route);

            report.AddEntry(
                variant,
                new GenericReportEntry(
                    "This route variant/pattern has " + variant.StopCount + " stops: " + string.Join(", ", variant.Stops.Select(s => "`" + s.Name + "`")) + ". " +
                    "This variant/pattern appears " + variant.TripCount + " times as a trip in GTFS data (out of " + variant.Route.Trips.Count() + " total trips for " + routeVariantCount + " variants)."
                )
            );
            
            if (routePair != null)
            {
                string? osmRouteName = routePair.OsmRoute.GetValue("name");
                
                report.AddEntry(
                    variant,
                    new GenericReportEntry(
                        "This route matches OSM route " + 
                        (osmRouteName != null ? "`" + osmRouteName + "`" : "") + 
                        " with a " + (routePair.Score * 100).ToString("F0") + "% match (matched " + routePair.Stops.Count + "/" + variant.StopCount + " stops) - " + routePair.OsmRoute.OsmViewUrl
                    )
                );
                
                report.AddEntry(
                    variant,
                    new GenericReportEntry(
                        "OSM route has these stops: " + 
                        string.Join(", ", ExtractOsmRouteStops(routePair.OsmRoute).Select(OsmStopMapPointLabel))
                    )
                );
            }
            else
            {
                report.AddEntry(
                    variant,
                    new IssueReportEntry("No matching OSM route found that uses similar stops as the route in the data.")
                );
            }

            // Stop map and matching

            foreach (StopMatch stopMatch in stopMatches)
            {
                switch (stopMatch)
                {
                    case GoodRouteAndOsmStopMatch fullStopMatch:
                        report.AddEntry(
                            variant,
                            new MapPointReportEntry(
                                fullStopMatch.OsmStop.GetAverageCoord(),
                                "Route stop and OSM stop match well: " +
                                " Route stop " + 
                                RouteStopMapPointLabel(fullStopMatch.RouteStop) +
                                " matching OSM stop " +
                                OsmStopMapPointLabel(fullStopMatch.OsmStop) + " - " + fullStopMatch.OsmStop.OsmViewUrl,
                                fullStopMatch.OsmStop,
                                MapPointStyle.BusStopMatchedWell
                            )
                        );
                        break;

                    case PoorRouteAndOsmStopMatch fullStopMatch:
                        report.AddEntry(
                            variant,
                            new MapPointReportEntry(
                                fullStopMatch.OsmStop.GetAverageCoord(),
                                "Route stop and OSM stop match, but poorly: " +
                                " Route stop " + 
                                RouteStopMapPointLabel(fullStopMatch.RouteStop) +
                                " matching OSM stop " +
                                OsmStopMapPointLabel(fullStopMatch.OsmStop) + " - " + fullStopMatch.OsmStop.OsmViewUrl,
                                fullStopMatch.OsmStop,
                                MapPointStyle.BusStopMatchedPoorly
                            )
                        );
                        break;

                    case OsmOnlyStopMatch osmOnlyStopMatch:
                        report.AddEntry(
                            variant,
                            new MapPointReportEntry(
                                osmOnlyStopMatch.OsmStop.GetAverageCoord(),
                                "OSM stop not in route: " +
                                OsmStopMapPointLabel(osmOnlyStopMatch.OsmStop) + " - " + osmOnlyStopMatch.OsmStop.OsmViewUrl,
                                osmOnlyStopMatch.OsmStop,
                                MapPointStyle.BusStopOsmUnmatched
                            )
                        );
                        break;

                    case RouteOnlyStopMatch routeOnlyStopMatch:
                        report.AddEntry(
                            variant,
                            new MapPointReportEntry(
                                routeOnlyStopMatch.RouteStop.Coord,
                                (routePair != null ? "Route stop not in OSM: " : "Route stop: ") +
                                RouteStopMapPointLabel(routeOnlyStopMatch.RouteStop),
                                MapPointStyle.BusStopOriginalUnmatched
                            )
                        );
                        // todo: likely matches we can use
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(stopMatch));
                }
            }
            
            // Stop order
            
            // TODO: ...
            
            // OSM route way geometry validation 
            
            // TODO: gaps/unconnected
            // TODO: stop reaching
            // TODO: stop order matches way order -- might be too complicated
            
            // Generic matches
            
            // todo: names
            // todo: to / via / from
            // todo: stop tranport mode match
                

            [Pure]
            static string RouteStopMapPointLabel(GTFSStop routeStop)
            {
                return "`" + routeStop.Name + "` [`" + routeStop.Id + "`]";
            }
                
            [Pure]
            static string OsmStopMapPointLabel(OsmElement osmStop)
            {
                string? osmStopName = osmStop.GetValue("name");
                string? osmStopRef = osmStop.GetValue("ref");

                return
                    (osmStopName != null ? " `" + osmStopName + "`" : "Unnamed") +
                    (osmStopRef != null ? " [`" + osmStopRef + "`]" : "");
            }
        }

        // List skipped variants
        
        if (skippedVariants.Count > 0)
        {
            report.AddGroup(GroupDesignation.SkippedVariants, "Skipped route variants");

            report.AddEntry(GroupDesignation.SkippedVariants, new GenericReportEntry("These routes aren't matched to any OSM route have less than " + minTripCountToInclude + " trips with their unique sequence/pattern of stops, so they are skipped from full analysis."));

            foreach (RouteVariant variant in skippedVariants)
            {
                report.AddEntry(GroupDesignation.SkippedVariants, new GenericReportEntry("The route " + variant.Route.CleanType + " #" + variant.Route.Number + ": " + variant.FirstStop.Name + " => " + variant.LastStop.Name + " has " + variant.TripCount + " trips with the sequence of " + variant.StopCount + " stops: " + string.Join(", ", variant.Stops.Select(s => s.Name))));
            }
        }
    }

    
    /// <summary>
    /// Ask inheritor to apply their specific known fixes to GTFS that are unique to the operator.
    /// Basically, GTFS data is messy and each provider has their own quirks.
    /// This gets called before data is used for matching.
    /// </summary>
    protected abstract void CleanUpGtfsData(GTFSNetwork gtfsNetwork);


    [Pure]
    private static (RoutePair? routePair, List<StopMatch> stopMatches) GetStopMatches(List<RoutePair> routePairs, RouteVariant variant)
    {
        RoutePair? routePair = routePairs.Find(rp => rp.RouteVariant == variant);

        if (routePair == null) // osm route not matched, so all stops are data route only
            return (null, variant.Stops.Select(s => new RouteOnlyStopMatch(s)).ToList<StopMatch>());

        List<StopMatch> stopMatches = [ ];

        List<OsmElement> osmStops = CollectOsmStops();

        // Match directly to quickly-identified name matching pairs
        
        foreach (StopPair stopPair in routePair.Stops)
            stopMatches.Add(new GoodRouteAndOsmStopMatch(stopPair.OsmStop, stopPair.RouteStop));
            
        foreach (GTFSStop routeStop in variant.Stops)
            if (routePair.Stops.All(s => s.RouteStop != routeStop))
                stopMatches.Add(new RouteOnlyStopMatch(routeStop));

        foreach (OsmElement osmStop in osmStops)
            if (routePair.Stops.All(s => s.OsmStop != osmStop))
                stopMatches.Add(new OsmOnlyStopMatch(osmStop));
        
        // Try other matching logic

        bool changedSomething;
        
        do
        {
            changedSomething = false;
            
            // Try to assume OSM stop as probably correct if it fits with the route
            // This happens when the name mismatches (or is missing or something) for some reason
            
            foreach (OsmOnlyStopMatch osmOnlyStopMatch in stopMatches.OfType<OsmOnlyStopMatch>())
            {
                // What stop came before that matched?
                
                // todo: after?
                // todo: multiple gap?
                
                // Scenario:
                //                 ..->   [ONLY OSM]
                // -->  [COMMON]               ?
                //                 ''->   [ONLY GTFS]
                // 
                // We want to see if [ONLY OSM] pairs up with an [ONLY GTFS].
                // So we take [ONLY OSM], find previous OSM stop, and see if it's a [COMMON] one.
                // If so, we take gtfs stop from [COMMON] and find next gtfs stop and see if it is an [ONLY GTFS].
                // In other words - we have a "pair" of stops by themselves but both are supposed to follow previous entry,
                // so we can assume they are probably supposed to be the same stop.
                // todo: This can also apply "backwards" or even for multiple stops.
                
                OsmElement? previousOsmStop = GetPreviousOsmStop(osmOnlyStopMatch.OsmStop);
                if (previousOsmStop != null)
                {
                    RouteAndOsmStopMatch? previousCommonMatch = stopMatches.OfType<RouteAndOsmStopMatch>().FirstOrDefault(m => m.OsmStop == previousOsmStop);
                    if (previousCommonMatch != null)
                    {
                        GTFSStop? pairingRouteStop = GetNextRouteStopMatch(previousCommonMatch.RouteStop);
                        if (pairingRouteStop != null)
                        {
                            RouteOnlyStopMatch? routeOnlyMatch = stopMatches.OfType<RouteOnlyStopMatch>().FirstOrDefault(m => m.RouteStop == pairingRouteStop);
                            if (routeOnlyMatch != null)
                            {
                                if (OsmGeoTools.DistanceBetween(
                                        osmOnlyStopMatch.OsmStop.GetAverageCoord(),
                                        routeOnlyMatch.RouteStop.Coord
                                    ) < 70) // too far, probably actually different, so skip
                                {
                                    stopMatches.Remove(osmOnlyStopMatch);
                                    stopMatches.Remove(routeOnlyMatch);

                                    PoorRouteAndOsmStopMatch replacementMatch = new PoorRouteAndOsmStopMatch(
                                        osmOnlyStopMatch.OsmStop,
                                        routeOnlyMatch.RouteStop
                                    );

                                    stopMatches.Add(replacementMatch);

                                    //Console.WriteLine("Replaced individual matches with pair match: " + routeOnlyMatch.RouteStop.Name + " <=> " + osmOnlyStopMatch.OsmStop.GetValue("name"));

                                    changedSomething = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                
                // todo: when too far - bad match
                // todo: can above be wrong in other ways even when name matches?
            }
            
        } while (changedSomething);

        return (routePair, stopMatches);


        [Pure]
        List<OsmElement> CollectOsmStops()
        {
            List<OsmElement> osmStops = [ ];
            
            foreach (OsmRelationMember routeMember in routePair.OsmRoute.Members)
                if (routeMember.Element != null)
                    if (routeMember.Role is "platform" or "platform_entry_only" or "platform_exit_only")
                        osmStops.Add(routeMember.Element);
            
            return osmStops;
        }

        [Pure]
        OsmElement? GetPreviousOsmStop(OsmElement osmStop)
        {
            int givenIndex = osmStops.FindIndex(s => s == osmStop);
            
            if (givenIndex == -1)
                return null; // not found
            
            if (givenIndex == 0)
                return null; // first stop, no previous
            
            return osmStops[givenIndex - 1];
        }
        
        [Pure]
        GTFSStop? GetNextRouteStopMatch(GTFSStop routeStop)
        {
            int givenIndex = routePair.RouteVariant.Stops.ToList().FindIndex(s => s == routeStop);
            
            if (givenIndex == -1)
                return null; // not found
            
            if (givenIndex == routePair.RouteVariant.Stops.Count - 1)
                return null; // last stop, no next
            
            return routePair.RouteVariant.Stops[givenIndex + 1];
        }
    }

    private abstract record StopMatch;

    private abstract record RouteAndOsmStopMatch(OsmElement OsmStop, GTFSStop RouteStop) : StopMatch;
    
    private record GoodRouteAndOsmStopMatch(OsmElement OsmStop, GTFSStop RouteStop) : RouteAndOsmStopMatch(OsmStop, RouteStop);
    
    private record PoorRouteAndOsmStopMatch(OsmElement OsmStop, GTFSStop RouteStop) : RouteAndOsmStopMatch(OsmStop, RouteStop);
    
    private record RouteOnlyStopMatch(GTFSStop RouteStop) : StopMatch;
    
    private record OsmOnlyStopMatch(OsmElement OsmStop) : StopMatch;


    [Pure]
    private static IEnumerable<RouteVariant> ExtractVariantsFromRoute(GTFSRoute route)
    {
        List<RouteVariant> variants = [ ];

        foreach (GTFSTrip trip in route.Trips)
        {
            if (trip.Stops.Count() < 2)
                continue; // skip degenerate trips (probably data error)
            
            RouteVariant? existing = variants.Find(mt => mt.Stops.SequenceEqual(trip.Stops));

            if (existing != null)
                existing.AddTrip(trip);
            else
                variants.Add(new RouteVariant(route, trip, trip.Stops));
        }
        
        return variants;
    }

    private class RouteVariant
    {
        public GTFSRoute Route { get; }
        
        public IList<GTFSTrip> Trips => _trips;

        public IList<GTFSStop> Stops => _stops;

        public GTFSStop FirstStop => _stops[0];
        
        public GTFSStop LastStop => _stops[^1];
        
        public int StopCount => _stops.Count;
        
        public int TripCount => _trips.Count;

        public OsmCoord AverageCoord { get; }


        private readonly List<GTFSTrip> _trips;
        
        private readonly List<GTFSStop> _stops;


        public RouteVariant(GTFSRoute route, GTFSTrip trip, IEnumerable<GTFSStop> stops)
        {
            Route = route;
            _trips = [ trip ];
            _stops = stops.ToList();

            AverageCoord = OsmGeoTools.GetAverageCoord(_stops.Select(s => s.Coord));
        }

        
        public void AddTrip(GTFSTrip trip)
        {
            _trips.Add(trip);
        }
    }

    [Pure]
    private static List<RoutePair> MatchOsmRoutesToRouteVariants(OsmDataExtract osmRoutes, List<RouteVariant> routeVariants)
    {
        List<RouteVariant> remainingVariants = routeVariants.ToList();

        List<RoutePair> foundRoutePairs = [ ];

        while (remainingVariants.Count > 0)
        {
            for (int i = 0; i < remainingVariants.Count; i++)
            {
                RouteVariant variant = remainingVariants[i];

                OsmRelation? bestMatch = null;
                float bestScore = 0f;
                List<StopPair> bestMatchStopPairs = [ ];

                foreach (OsmRelation osmRoute in osmRoutes.Relations)
                {
                    if (OsmGeoTools.DistanceBetween(osmRoute.GetAverageCoord(), variant.AverageCoord) > 50000) // 50 km (although for ATD this may still not be enough)
                        continue; // too far away, skip

                    (float score, List<StopPair> stopPairs) = GetOsmRouteAndRouteMatchScore(osmRoute, variant);

                    if (score > 0.4f)
                    {
                        if (bestMatch == null || score > bestScore)
                        {
                            // Check if we have already previously matched this OSM route to another variant
                            // and don't try to match if unless we are actually better than the previous match
                            RoutePair? previousOsmRouteMatch = foundRoutePairs.Find(rp => rp.OsmRoute == osmRoute);
                            if (previousOsmRouteMatch != null)
                                if (previousOsmRouteMatch.Score > score)
                                    continue; // this osm route was previously better matched to another variant, so we can never "take it over"
                            
                            bestMatch = osmRoute;
                            bestScore = score;
                            bestMatchStopPairs = stopPairs;
                        }
                    }
                }

                // Whether we matched or not, we are done for now
                remainingVariants.RemoveAt(i);
                i--;

                if (bestMatch != null)
                {
                    RoutePair? previousOsmRouteMatch = foundRoutePairs.Find(rp => rp.OsmRoute == bestMatch);

                    if (previousOsmRouteMatch != null && previousOsmRouteMatch.Score < bestScore)
                    {
                        // The OSM route we chose is a better match for us than it was to the variant that matched it before,
                        // so we want to "take over" the match and ask the previous variant to find a new match
                                    
                        // Remove the old match - we will add our own instead
                        foundRoutePairs.Remove(previousOsmRouteMatch);
                        
                        // Readd the previous variant to the pending list so it's rematched
                        remainingVariants.Add(previousOsmRouteMatch.RouteVariant);
                    }
                    
                    // Add our match as a pair
                    foundRoutePairs.Add(new RoutePair(bestMatch, variant, bestMatchStopPairs, bestScore));
                    // For now, this is the best match for this OSM route, but another variant may determined to be better later (in which case we'll try for another route) 
                }
            }
        }

        return foundRoutePairs;
    }

    [Pure]
    private static (float score, List<StopPair> stopPairs) GetOsmRouteAndRouteMatchScore(OsmRelation osmRoute, RouteVariant variant)
    {
        List<OsmElement> osmRouteStops = ExtractOsmRouteStops(osmRoute);
        List<string?> osmRouteStopNames = ExtractOsmRouteStopNames(osmRouteStops);

        // Note that this is deliberately naive matching, only names and not actually matching OSM stop positions
        // We just want to "find the route on OSM"

        // This is how many stops we have
        int matchCount = Math.Max(variant.StopCount, osmRouteStops.Count);

        float matchedStopScore = 0;

        List<StopPair> stopPairs = [ ];
            
        for (int i = 0; i < variant.Stops.Count; i++)
        {
            int matchIndex = FindBestStopMatch(variant.Stops[i]);

            if (matchIndex != -1)
            {
                // Stop score is based on how proportionally close it is to its "true position" in the data route
                // (otherwise we would match reversed or routes with extra/missing stops the same)
                float stopScore = 1f - Math.Abs(i - matchIndex) / (float)variant.StopCount;

                if (stopScore < 0f)
                    stopScore = 0f;

                matchedStopScore += stopScore / matchCount;
                    
                stopPairs.Add(new StopPair(variant.Stops[i], osmRouteStops[matchIndex]));
            }
        }

        return (matchedStopScore, stopPairs);

            
        [Pure]
        int FindBestStopMatch(GTFSStop routeStop)
        {
            int? bestMatchIndex = null;
            double bestDistance = 0f;

            for (int i = 0; i < osmRouteStopNames.Count; i++)
            {
                if (osmRouteStopNames[i] != null)
                {
                    if (IsStopNameMatchGoodEnough(routeStop.Name, osmRouteStopNames[i]!))
                    {
                        double distance = OsmGeoTools.DistanceBetween(
                            routeStop.Coord,
                            osmRouteStops[i].GetAverageCoord()
                        );

                        if (bestMatchIndex == null || distance < bestDistance)
                        {
                            bestMatchIndex = i;
                            bestDistance = distance;
                        }
                    }
                }
            }

            return bestMatchIndex ?? -1;
        }
    }

    [Pure]
    private static List<OsmElement> ExtractOsmRouteStops(OsmRelation osmRoute)
    {
        List<OsmElement> stops = [ ];

        foreach (OsmRelationMember member in osmRoute.Members)
        {
            if (member.Element != null)
            {
                if (member.Role is "platform" or "platform_entry_only" or "platform_exit_only")
                {
                    stops.Add(member.Element);
                }
            }
        }
        
        return stops;
    }

    [Pure]
    private static List<string?> ExtractOsmRouteStopNames(List<OsmElement> osmStops)
    {
        List<string?> stopNames = [ ];

        foreach (OsmElement osmStop in osmStops)
        {
            string? name = osmStop.GetValue("name");
            
            if (name != null)
                stopNames.Add(name);
            else
                stopNames.Add(null);
        }
        
        return stopNames;
    }

    private record RoutePair(OsmRelation OsmRoute, RouteVariant RouteVariant, List<StopPair> Stops, float Score);
    
    private record StopPair(GTFSStop RouteStop, OsmElement OsmStop);
    
    
    //
    // [Pure]
    // private static StopNameMatching DoStopsMatch(OsmNode osmStop, GTFSStop ptStop)
    // {
    //     string? stopName = osmStop.GetValue("name");
    //
    //     if (stopName == null)
    //         return StopNameMatching.Mismatch;
    //
    //     bool tramStop =
    //         osmStop.HasValue("railway", "tram_stop") ||
    //         osmStop.HasValue("disused:railway", "tram_stop") ||
    //         osmStop.HasValue("tram", "yes");
    //
    //     bool trolleybusStop =
    //         osmStop.HasValue("highway", "bus_stop") ||
    //         osmStop.HasValue("trolleybus", "yes");
    //
    //     bool busStop =
    //         osmStop.HasValue("highway", "bus_stop") &&
    //         !osmStop.HasValue("trolleybus", "yes");
    //
    //     bool typeMatch = 
    //         tramStop && ptStop.Tram ||
    //         trolleybusStop && ptStop.Trolleybus ||
    //         busStop && ptStop.Bus;
    //         
    //     if (IsStopNameMatchGoodEnough(ptStop.Name, stopName))
    //         return typeMatch ? StopNameMatching.Match : StopNameMatching.WeakMatch;
    //         
    //     // Stops in real-life can have a different name to GTFS
    //     // For example OSM "Ulbrokas ciems" is signed so in real-life vs RS "Ulbroka"
    //     // After verifying these, one can keep `name=Ulbrokas ciems` but `alt_name=Ulbroka`, so we can match these
    //         
    //     string? stopAltName = osmStop.GetValue("alt_name");
    //
    //     if (stopAltName != null)
    //         if (IsStopNameMatchGoodEnough(ptStop.Name, stopAltName))
    //             return typeMatch ? StopNameMatching.Match : StopNameMatching.WeakMatch;
    //         
    //     return StopNameMatching.Mismatch;
    // }
    //

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
        SkippedVariants
    }
}