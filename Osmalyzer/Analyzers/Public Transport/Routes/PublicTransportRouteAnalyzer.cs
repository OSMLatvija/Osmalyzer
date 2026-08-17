using System.Drawing;

namespace Osmalyzer;

public abstract class PublicTransportRouteAnalyzer<T> : Analyzer
    where T : GTFSAnalysisData, new()
{
    public sealed override string Name => Label + " Routes";
    
    public sealed override string Description => 
        "This checks public transport routes for " + Name + " operator. " +
        "Each operator's GTFS route is matched against OSM routes based on the expected stops. " +
        "Note that this might result in poor and incorrect matches if OSM doesn't have a matching route mapped " +
        "or some other route matches it instead (because of similar stops). " +
        "Each route is shown in its own section so it can be compared with the matched OSM route, if any. " +
        "If stops don't fully match between OSM and GTFS, then points will also be shown for missing or extra stops. " +
        "Note that GTFS stores routes differently than OSM and not necessarily all should be mapped. " +
        "Technically, GTFS doesn't have route \"variants\" - the ones below are collected from repeating unique stop sequences. " +
        "Most routes have their \"primary\" variants corresponding to many GTFS trips, such as typical daytime operation. " +
        "These should likely all be mapped on OSM. " +
        "Then some routes have additional variants, such as shortened evening route or returning to depot. " +
        "Whether to map these depends on many factors and differs greatly by operator. " +
        "GTFS route geometry is visualized in magenta and OSM ways used by routes in blue on top. " +
        "Note that GTFS geometry/shapes after very often imprecise or even wrong at high zoom levels.";

    public sealed override AnalyzerGroup Group => AnalyzerGroup.PublicTransportRoutes;
    
    public sealed override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(T) ];


    /// <summary> Very short label for report texts </summary>
    protected abstract string Label { get; }

    protected abstract string? GtfsDisambiguatorId { get; }
    protected abstract bool RegionalService { get; }

    /// <summary> Agency as defined by `agency_name` in GTFS </summary>
    protected abstract string? Agency { get; }
    /// <summary> Agency brief readable page name/label </summary>
    protected abstract string? AgencyLabel { get; }
    

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load stop data

        GTFSAnalysisData gtfsData = datas.OfType<T>().First();

        GTFSNetwork gtfsNetwork = gtfsData.Network;
            
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData masterData = osmData.MasterData;

        OsmData osmRoutes = masterData.Filter(
            new IsRelation(),
            new HasValue("type", "route"),
            new OrMatch(
                new HasAnyValue("route", "tram", "bus", "trolleybus"),
                new HasAnyValue("disused:route", "tram", "bus", "trolleybus")
            )
        );
        
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
        
        // Add "supergroups" for routes since there are so many

        foreach (GTFSRoute gtfsRoute in gtfsNetwork.Routes.Routes)
        {
            string header = gtfsRoute.TypeLabel + " #" + gtfsRoute.Number + " \"" + gtfsRoute.Name + "\"";

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
        
        // Determine which routes have at least one variant meeting the threshold
        // For routes with no variants meeting the threshold, mark the most-used variant to show anyway
        
        HashSet<RouteVariant> forcedToShowVariants = [ ];
        
        foreach (GTFSRoute gtfsRoute in gtfsNetwork.Routes.Routes)
        {
            List<RouteVariant> variantsForRoute = routeVariants.Where(rv => rv.Route == gtfsRoute).ToList();
            
            bool hasVariantMeetingThreshold = variantsForRoute.Any(rv => rv.TripCount >= minTripCountToInclude);
            
            if (!hasVariantMeetingThreshold && variantsForRoute.Count > 0)
            {
                // No variants meet the threshold, so force the most-used one to be shown
                RouteVariant mostUsedVariant = variantsForRoute.MaxBy(rv => rv.TripCount)!;
                forcedToShowVariants.Add(mostUsedVariant);
            }
        }
        
        // Show results for each route variant

        foreach (RouteVariant variant in routeVariants)
        {
            (RoutePair? routePair, List<StopMatch> stopMatches) = GetStopMatches(routePairs, variant);

            // Skip route if we don't have a match and it's not frequent enough
            
            if (routePair == null && // no OSM route matched, so we might not want to even show if it's infrequent
                variant.TripCount < minTripCountToInclude && // todo: optional, e.g. JAP lists them but RS doesn't
                !forcedToShowVariants.Contains(variant)) // unless this is the most-used variant for a route with no frequent variants
            {
                // Not enough to report as a "full" route, presumably first/final depo routes 
                skippedVariants.Add(variant);
                continue;
            }
            
            // Display this route
            
            string header = variant.Route.TypeLabel + " #" + variant.Route.Number + " from " + variant.FirstStop.Name + " to " + variant.LastStop.Name;

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
                                fullStopMatch.OsmStop.AverageCoord,
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
                                fullStopMatch.OsmStop.AverageCoord,
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
                                osmOnlyStopMatch.OsmStop.AverageCoord,
                                "This stop is not supposed to be in OSM route: " +
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
                                (routePair != null ? "This stop is missing from OSM route: " : "Route stop: ") +
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
            
            // GTFS shape
            
            if (variant.Shape != null)
            {
                for (int i = 0; i < variant.Shape.Points.Count - 1; i++)
                {
                    GTFSShapePoint current = variant.Shape.Points[i];
                    GTFSShapePoint next = variant.Shape.Points[i + 1];
                    
                    report.AddEntry(
                        variant,
                        new MapLineReportEntry(
                            [ current.Coord, next.Coord ],
                            ToHex(Color.MediumVioletRed)
                        )
                    );
                }

                static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            }

            if (routePair != null)
            {
                // OSM shape

                if (variant.Shape != null)
                {
                    foreach (OsmWay way in routePair.OsmRoute.Members
                                                        .Where(m => m.Type == OsmElement.OsmElementType.Way && m.Role == "")
                                                        .Select(m => m.Element!)
                                                        .Cast<OsmWay>())
                    {
                        OsmCoord[] coords = way.Nodes.Select(n => n.coord).ToArray();

                        report.AddEntry(
                            variant,
                            new MapLineReportEntry(
                                coords,
                                ToHex(Color.DeepSkyBlue)
                            )
                        );
                    }

                    static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
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
            // todo: stop transport mode match
            
        }

        // List skipped variants
        
        if (skippedVariants.Count > 0)
        {
            report.AddGroup(GroupDesignation.SkippedVariants, "Skipped route variants");

            report.AddEntry(GroupDesignation.SkippedVariants, new GenericReportEntry("These routes aren't matched to any OSM route have less than " + minTripCountToInclude + " trips with their unique sequence/pattern of stops, so they are skipped from full analysis."));

            foreach (RouteVariant variant in skippedVariants)
            {
                report.AddEntry(GroupDesignation.SkippedVariants, new GenericReportEntry("The route " + variant.Route.TypeLabel + " #" + variant.Route.Number + ": " + variant.FirstStop.Name + " => " + variant.LastStop.Name + " has " + variant.TripCount + " trips with the sequence of " + variant.StopCount + " stops: " + string.Join(", ", variant.Stops.Select(s => s.Name))));
            }
        }
    }

    

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
                                        osmOnlyStopMatch.OsmStop.AverageCoord,
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

        public GTFSShape? Shape => _trips[0].Shape; // any will do (presumably all the same and if not... oh well)

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


        public override string ToString()
        {
            return Route.Name;
        }
    }

    [Pure]
    private static List<RoutePair> MatchOsmRoutesToRouteVariants(OsmData osmRoutes, List<RouteVariant> routeVariants)
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
                    if (OsmGeoTools.DistanceBetween(osmRoute.AverageCoord, variant.AverageCoord) > 50000) // 50 km (although for ATD this may still not be enough)
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
                    if (PublicTransportHelpers.IsStopNameMatchGoodEnough(routeStop.Name, osmRouteStopNames[i]!))
                    {
                        double distance = OsmGeoTools.DistanceBetween(
                            routeStop.Coord,
                            osmRouteStops[i].AverageCoord
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
   

    [Pure]
    private static string RouteStopMapPointLabel(GTFSStop routeStop)
    {
        return "`" + routeStop.Name + "` [`" + routeStop.Id + "`]";
    }
                
    [Pure]
    private string OsmStopMapPointLabel(OsmElement osmStop)
    {
        string? osmStopName = osmStop.GetValue("name");
        string? osmStopRegionalName = osmStop.GetValue("reg_name");
        string? osmStopRef = osmStop.GetValue("ref");
        string? osmGtfsId = osmStop.GetValue("gtfs:stop_id");
        string? osmGtfsDabId = osmStop.GetValue("gtfs:stop_id:" + GtfsDisambiguatorId);

        string? refs = null;
        
        if (osmStopRef != null)
            refs = "`" + osmStopRef + "`";
        
        if (osmGtfsId != null && osmGtfsId != osmStopRef)
            refs += (refs != null ? ", " : "") + "`" + osmGtfsId + "`";
        
        if (osmGtfsDabId != null && osmGtfsDabId != osmStopRef && osmGtfsDabId != osmGtfsId)
            refs += (refs != null ? ", " : "") + "`" + osmGtfsDabId + "`";

        return
            (osmStopName != null ? " `" + osmStopName + "`" : "Unnamed") +
            (osmStopRegionalName != null ? " (regional `" + osmStopRegionalName + "`)" : "") +
            (refs != null ? " [" + refs + "]" : "");
    }


    private enum GroupDesignation
    {
        SkippedVariants
    }
}