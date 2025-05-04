using System.Diagnostics;

namespace Osmalyzer;

public abstract class PublicTransportAnalyzer<T> : Analyzer
    where T : GTFSAnalysisData, new()
{
    public override string Description => "This checks the public transport route issues for " + Name;

    public override AnalyzerGroup Group => AnalyzerGroups.PublicTransport;
    
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
        // todo: skip only if not found in OSM

        foreach (GTFSRoute gtfsRoute in gtfsNetwork.Routes.Routes)
        {
            foreach (RouteVariant variant in ExtractVariantsFromRoute(gtfsRoute).OrderByDescending(mt => mt.TripCount))
            {
                if (variant.TripCount < minTripCountToInclude) // todo: optional, e.g. JAP lists them but RS doesn't
                {
                    // Not enough to report as a "full" route, presumably something like first/final depot routes 
                    skippedVariants.Add(variant);
                    continue;
                }

                routeVariants.Add(variant);
            }
        }
        
        // Clean up GTFS data

        gtfsNetwork.CleanStopNames(CleanRouteStopName);
        
        // Match OSM routes to data routes

        List<RoutePair> routePairs = MatchOsmRoutesToRouteVariants(osmRoutes, routeVariants);
        
        // Show results for each route variant

        foreach (RouteVariant variant in routeVariants)
        {
            if (variant.TripCount < minTripCountToInclude) // todo: optional, e.g. JAP lists them but RS doesn't
            {
                // Not enough to report as a "full" route, presumably first/final depo routes 
                skippedVariants.Add(variant);
                continue;
            }
            
            string header = variant.Route.CleanType + " #" + variant.Route.Number + ": " + variant.FirstStop.Name + " => " + variant.LastStop.Name;
            // e.g. "Bus #2: Mangaļsala => Abrenes iela"

            report.AddGroup(
                variant,
                header,
                null,
                null,
                false,
                false // don't cluster stops, we want "discrete" preview
            );

            (RoutePair? routePair, List<StopMatch> stopMatches) = GetStopMatches(routePairs, variant);

            // Route and match generic info

            report.AddEntry(
                variant,
                new GenericReportEntry("This route has " + variant.StopCount + " stops: " + string.Join(", ", variant.Stops.Select(s => "`" + s.Name + "`")))
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
                    case FullStopMatch fullStopMatch:
                        report.AddEntry(
                            variant,
                            new MapPointReportEntry(
                                fullStopMatch.OsmStop.GetAverageCoord(),
                                "Route stop and OSM stop match: " +
                                " Route stop " + 
                                RouteStopMapPointLabel(fullStopMatch.RouteStop) +
                                " matching OSM stop " +
                                OsmStopMapPointLabel(fullStopMatch.OsmStop) + " - " + fullStopMatch.OsmStop.OsmViewUrl,
                                fullStopMatch.OsmStop,
                                MapPointStyle.BusStopMatchedWell
                            )
                        );
                        // todo: original location
                        // todo: distant matches
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

            report.AddEntry(GroupDesignation.SkippedVariants, new GenericReportEntry("These routes have less than " + minTripCountToInclude + " trips with their unique sequence/pattern of stops, so they are skipped from full analysis."));

            foreach (RouteVariant variant in skippedVariants)
            {
                report.AddEntry(GroupDesignation.SkippedVariants, new GenericReportEntry("The route " + variant.Route.CleanType + " #" + variant.Route.Number + ": " + variant.FirstStop.Name + " => " + variant.LastStop.Name + " has " + variant.TripCount + " trips with the sequence of " + variant.StopCount + " stops: " + string.Join(", ", variant.Stops.Select(s => s.Name))));
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
            
        foreach (StopPair stopPair in routePair.Stops)
            stopMatches.Add(new FullStopMatch(stopPair.OsmStop, stopPair.RouteStop));
            
        foreach (GTFSStop routeStop in variant.Stops)
            if (routePair.Stops.All(s => s.RouteStop != routeStop))
                stopMatches.Add(new RouteOnlyStopMatch(routeStop));

        foreach (OsmRelationMember routeMember in routePair.OsmRoute.Members)
            if (routeMember.Element != null)
                if (routeMember.Role is "platform" or "platform_entry_only" or "platform_exit_only")
                    if (routePair.Stops.All(s => s.OsmStop != routeMember.Element))
                        stopMatches.Add(new OsmOnlyStopMatch(routeMember.Element));

        return (routePair, stopMatches);
    }

    private abstract record StopMatch;

    private record FullStopMatch(OsmElement OsmStop, GTFSStop RouteStop) : StopMatch;
    
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

                    if (score > 0.1f)
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

    // private (GTFSTrip service, float bestMatch) FindBestTripMatch(GTFSRoute route, List<OsmNode> stops)
    // {
    //     // I have to do fuzyz matching, because I don't actually know which service and which trip OSM is representing
    //     // That is, I don't know which GTFS trip is the regular normal trip and which are random depo and alternate trips
    //     // OSM may even have alternate trips, so I need to match them in that case
    //     // So this matches the route with the best stop match - more stop matches, better match
    //                     
    //     GTFSTrip? bestTrip = null;
    //     float bestMatch = 0f;
    //                     
    //     foreach (GTFSService ptService in route.Services)
    //     {
    //         foreach (GTFSTrip ptTrip in ptService.Trips)
    //         {
    //             int matchedStops = 0;
    //
    //             foreach (GTFSStop ptStop in ptTrip.Stops)
    //             {
    //                 if (fullyMatchedStops.TryGetValue(ptStop, out OsmNode? expectedOsmStop))
    //                 {
    //                     if (stops.Contains(expectedOsmStop))
    //                         matchedStops++;
    //                 }
    //             }
    //
    //             float match = Math.Max(0f, (float)matchedStops / Math.Max(ptTrip.Points.Count(), stops.Count));
    //
    //             if (bestTrip == null ||
    //                 match > bestMatch)
    //             {
    //                 bestMatch = match;
    //                 bestTrip = ptTrip;
    //             }
    //         }
    //     }
    //
    //     return (bestTrip!, bestMatch); // will always match at least something, even if 0%
    // }
    //
    //
    // [Pure]
    // private static bool MatchesRoute(OsmRelation osmRoute, GTFSRoute route)
    // {
    //     // OSM
    //     // from	Preču 2
    //     // name	Bus 13: Preču 2 => Kleisti => Babītes stacija
    //     // public_transport:version	2
    //     // ref	13
    //     // roundtrip	no
    //     // route	bus
    //     // to	Babītes stacija
    //     // type	route
    //     // via	Kleisti
    //         
    //     // RS
    //     // riga_bus_13,"13","Babītes stacija - Kleisti - Preču 2",,3,https://saraksti.rigassatiksme.lv/index.html#riga/bus/13,F4B427,FFFFFF,2001300
    //
    //     if (route.Type != osmRoute.GetValue("route"))
    //         return false;
    //         
    //     if (route.Number != osmRoute.GetValue("ref"))
    //         return false;
    //
    //     string? osmName = osmRoute.GetValue("name");
    //
    //     if (osmName == null)
    //         return false;
    //
    //     string[] split = route.Name.Split('-');
    //
    //     int count = 0;
    //         
    //     foreach (string s in split)
    //     {
    //         string ptName = s.Trim();
    //
    //         if (OsmNameHasPTNamePart(osmName, ptName))
    //             count++;
    //
    //         // TODO: This will probably need matching GTFS<->OSM name matching
    //     }
    //
    //     if (count < 2)
    //         return false;
    //         
    //     // Some are Abrenes iela - Jaunciems - Suži
    //     // Some are Abrenes iela - Jaunmārupe
    //
    //     return true; // Guess it's "good enough" without more complex checks
    //         
    //         
    //     [Pure]
    //     static bool OsmNameHasPTNamePart(string osmName, string ptName)
    //     {
    //         // Straight match
    //         if (osmName.Contains(ptName))
    //             return true;
    //             
    //         // TEC 2 vs TEC-2
    //         if (osmName.Replace('-', ' ').Contains(ptName))
    //             return true;
    //
    //         return false;
    //     }
    // }
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
    private static string CleanRouteStopName(string ptStopName)
    {
        // Rezeknes almost all stops have "uc" and "nc" like suffixes like "Brīvības iela nc" and "Brīvības iela uc" - probably route direction "no centra"/"uz centru"?
        ptStopName = Regex.Replace(ptStopName, @" (uc|nc|mv)$", @"");

        // todo: move more here from IsStopNameMatchGoodEnough
        
        return ptStopName;
    }

    [Pure]
    private static bool IsStopNameMatchGoodEnough(string ptStopName, string osmStopName)
    {
        // Stops never differ by capitalization, so just lower them and avoid weird capitalization in addition to everything else
        // Rezekne "18.Novembra iela" vs OSM "18. novembra iela"
        ptStopName = ptStopName.ToLower();
        osmStopName = osmStopName.ToLower();
            
        // Quick check first, may be we don't need to do anything
        if (ptStopName == osmStopName)
            return true;
            
        // Trim parenthesis from OSM
        // Jurmalas OSM stops have a lot of parenthesis, like JS "Majoru stacija" vs OSM "Majoru stacija (Majori)"
        osmStopName = Regex.Replace(osmStopName, @" \([^\(\)]+\)$", @"");
        // todo: return if the match was poor quality this way and the name should be checked
        // todo: what if GTFS data DOES have the parenthesis?
    
        // Trim brackets from GTFS
        // A couple Jurmalas GTFS stops have brackets, like JS "Promenādes iela [Promenādes iela]" and "Promenādes iela [Rīgas iela]" vs OSM "Promenādes iela" and "Promenādes iela"
        ptStopName = Regex.Replace(ptStopName, @" \[[^\[\]]+\]$", @"");
            
        // Both OSM and RS stops are inconsistent about spacing around characters
        // "2.trolejbusu parks" or "Jaunciema 2.šķērslīnija" (also all the abbreviated "P.Lejiņa iela" although this won't match)
        // "Upesgrīvas iela/ Spice"
        // OSM "TEC-2 pārvalde" vs RS "TEC- 2 pārvalde" or OSM "Preču-2" vs RS "Preču - 2"
        if (Regex.Replace(Regex.Replace(osmStopName, @"([\./-])(?! )", @"$1 "), @"(?<! )([\./-])", @" $1") == 
            Regex.Replace( Regex.Replace(ptStopName, @"([\./-])(?! )", @"$1 "), @"(?<! )([\./-])", @" $1"))
            return true;
            
        // Sometimes proper quotes are inconsistent between the two
        // OSM "Arēna "Rīga"" vs RS "Arēna Rīga" or OSM ""Bērnu pasaule"" vs RS "Bērnu pasaule"
        // or opposite OSM "Dzintars" vs RS ""Dzintars""
        if (osmStopName.Replace("\"", "") == ptStopName.Replace("\"", ""))
            return true;
            
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
    
    //
    // private enum StopNameMatching // order used by sorting
    // {
    //     Mismatch = 0,
    //     WeakMatch = 1,
    //     Match = 2
    // }
    
    private enum GroupDesignation
    {
        SkippedVariants
    }
}