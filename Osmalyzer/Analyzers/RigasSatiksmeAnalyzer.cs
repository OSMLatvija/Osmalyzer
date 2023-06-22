using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class RigasSatiksmeAnalyzer : Analyzer
    {
        public override string Name => "Rigas Satiksme";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(RigasSatiksmeAnalysisData) };
        

        public override void Run(IEnumerable<AnalysisData> datas, Report report)
        {
            // Load RS stop data

            RigasSatiksmeAnalysisData rsData = datas.OfType<RigasSatiksmeAnalysisData>().First();

            RigasSatiksmeNetwork rsNetwork = new RigasSatiksmeNetwork(rsData.ExtractionFolder);
            
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            List<OsmBlob> blobs = OsmBlob.CreateMultiple(
                osmData.DataFileName,
                new List<OsmFilter[]>()
                {
                    new OsmFilter[]
                    {
                        new IsNode(),
                        new OrMatch(
                            new HasValue("highway", "bus_stop"),
                            new HasValue("railway", "tram_stop"),
                            new HasValue("disused:railway", "tram_stop") // still valid, if not in active use - RS data seems to have these too
                        )
                    },
                    new OsmFilter[]
                    {
                        new IsRelation(),
                        new HasValue("type", "route"),
                        new OrMatch(
                            new HasAnyValue("route", new List<string> { "tram", "bus", "trolleybus" }),
                            new HasAnyValue("disused:route", new List<string> { "tram", "bus", "trolleybus" })
                        )
                    }
                }
            );
            
            OsmBlob osmStops = blobs[0];
            OsmBlob osmRoutes = blobs[1];
            
            // Params

            const double maxSearchDistance = 150.0; // we search this far for potential stops
            const double acceptDistance = 50.0; // but only this far counts as a good match
            
            // Prepare report stuff

            report.AddGroup(ReportGroup.NoStopMatchButHaveClose, "These RS stops don't have a matching OSM stop in range (" + maxSearchDistance + " m), but have a stop nearby (<" + acceptDistance + " m):");
            report.AddGroup(ReportGroup.MatchedOsmStopIsTooFar, "These RS stops have a matching OSM stop in range (" + maxSearchDistance + " m), but it is far away (>" + acceptDistance + " m)");
            report.AddGroup(ReportGroup.NoStopMatchInRange, "These RS stops don't have any already-unmatched OSM stops in range (" + maxSearchDistance + " m)");
            report.AddGroup(ReportGroup.NoStopMatchAndAllFar, "These RS stops have no matching OSM stop in range (" + maxSearchDistance + " m), and even all unmatched stops are far away (>" + acceptDistance + " m)");
            report.AddGroup(ReportGroup.NoOsmRouteMatch, "These RS routes were not matched to any OSM route:");
            report.AddGroup(ReportGroup.StopRematchFromRoutes, "These RS-OSM stop pairs didn't match RS & OSM routes:");
            report.AddGroup(ReportGroup.RsRouteMissingOsmStop, "These RS stops are not in the OSM route:");
            report.AddGroup(ReportGroup.OsmRouteExtraStop, "These OSM stops are not in the RS route:");

            // Parse stops

            List<OsmNode> matchedOsmStops = new List<OsmNode>();
            // so that we don't match the same stop multiple times

            Dictionary<RigasSatiksmeStop, OsmNode> fullyMatchedStops = new Dictionary<RigasSatiksmeStop, OsmNode>();

            foreach (RigasSatiksmeStop rsStop in rsNetwork.Stops.Stops)
            {
                // Find all potential OSM stops in range
                List<OsmNode> closestStops = osmStops.GetClosestNodesTo(rsStop.Lat, rsStop.Lon, maxSearchDistance);

                // Except the ones we already matched (if another RS stop wants them - it will have to find a further one or fail)
                closestStops = closestStops.Except(matchedOsmStops).ToList();

                // See if any OSM stops definitely match the RS stops (may even be multiple for some locations)
                List<(StopNameMatching match, OsmNode node)> matchedStops = closestStops.Select(s => (DoStopsMatch(s, rsStop), s)).ToList();
                // this will contain all stops but with match info - match, mismatch, weak match, whatever

                // Pick closest best-matched closest - prefer match, but settle for any follow-up, like weak match
                (StopNameMatching match, OsmNode matchedStop) = matchedStops
                                                                .OrderByDescending(ms => ms.match)
                                                                .ThenBy(ms => OsmGeoTools.DistanceBetween(ms.node.Lat, ms.node.Lon, rsStop.Lat, rsStop.Lon))
                                                                .FirstOrDefault();

                if (matchedStop != null &&
                    match != StopNameMatching.Mismatch) // anything else is fine
                {
                    double stopDistance = OsmGeoTools.DistanceBetween(matchedStop.Lat, matchedStop.Lon, rsStop.Lat, rsStop.Lon);
                    bool stopInAcceptRange = stopDistance <= acceptDistance;

                    if (stopInAcceptRange)
                    {
                        // Everything seems to be in order
                        fullyMatchedStops.Add(rsStop, matchedStop);
                    }
                    else
                    {
                        string osmStopName = matchedStop.GetValue("name")!; // already matched, can't not have name

                        report.AddEntry(ReportGroup.MatchedOsmStopIsTooFar, "RS stop \"" + rsStop.Name + "\"" + " matches OSM stop \"" + osmStopName + "\" but is far away " + stopDistance.ToString("F0") + " m - https://www.openstreetmap.org/node/" + matchedStop.Id + " , expecting around https://www.openstreetmap.org/#map=19/" + rsStop.Lat.ToString("F5") + "/" + rsStop.Lon.ToString("F5"));
                    }

                    if (!matchedOsmStops.Contains(matchedStop))
                        matchedOsmStops.Add(matchedStop);
                }
                else
                {
                    OsmNode? closestStop = closestStops.FirstOrDefault();

                    if (closestStop != null) // only unmatched stop(s) within distance
                    {
                        string? osmStopName = closestStop.GetValue("name");

                        double stopDistance = OsmGeoTools.DistanceBetween(closestStop.Lat, closestStop.Lon, rsStop.Lat, rsStop.Lon);
                        bool stopInAcceptRange = stopDistance <= acceptDistance;
                        
                        if (stopInAcceptRange && osmStopName != null) // already knwo it's not a match
                        {
                            report.AddEntry(ReportGroup.NoStopMatchButHaveClose, "RS stop \"" + rsStop.Name + "\" has no matching OSM stop nearby but is closest to OSM stop \"" + osmStopName + "\" - https://www.openstreetmap.org/node/" + closestStop.Id);
                        }
                        else if (stopInAcceptRange && osmStopName == null)
                        {
                            report.AddEntry(ReportGroup.NoStopMatchButHaveClose, "RS stop \"" + rsStop.Name + "\" has no matching OSM stop nearby but is closest to unnamed OSM stop - https://www.openstreetmap.org/node/" + closestStop.Id);
                        }
                        else if (!stopInAcceptRange)
                        {
                            report.AddEntry(ReportGroup.NoStopMatchAndAllFar, "RS stop \"" + rsStop.Name + "\" has no matching OSM stop in range and all other stops are far away (closest " + (osmStopName != null ? "\"" + osmStopName + "\"" : "unnamed") + " at " + stopDistance.ToString("F0") + " m) -- https://www.openstreetmap.org/#map=19/" + rsStop.Lat.ToString("F5") + "/" + rsStop.Lon.ToString("F5"));
                        }
                    }
                    else // no stop at all within distance
                    {
                        OsmNode farawayStop = osmStops.GetClosestNodeTo(rsStop.Lat, rsStop.Lon)!;
                        double farawayDistance = OsmGeoTools.DistanceBetween(farawayStop.Lat, farawayStop.Lon, rsStop.Lat, rsStop.Lon);

                        report.AddEntry(ReportGroup.NoStopMatchInRange, "No OSM stops at all in range of RS stop \"" + rsStop.Name + "\" (closest " + farawayDistance.ToString("F0") + " m) - https://www.openstreetmap.org/#map=19/" + rsStop.Lat.ToString("F5") + "/" + rsStop.Lon.ToString("F5") + "");
                    }
                }
            }

            // todo: other way - OSM stop but no RS stop
            
            // Parse routes

            foreach (RigasSatiksmeRoute rsRoute in rsNetwork.Routes.Routes)
            {
                List<OsmRelation> matchingOsmRoutes = osmRoutes.Elements.Cast<OsmRelation>().Where(e => MatchesRoute(e, rsRoute)).ToList();

#if !REMOTE_EXECUTION
                Debug.WriteLine(rsRoute.Name + " - x" + matchingOsmRoutes.Count + ": " + string.Join(", ", matchingOsmRoutes.Select(s => s.Id)));
#endif

                if (matchingOsmRoutes.Count == 0)
                {
                    List<RigasSatiksmeStop> endStops = new List<RigasSatiksmeStop>();
                    
                    foreach (RigasSatiksmeService service in rsRoute.Services)
                    {
                        foreach (RigasSatiksmeTrip trip in service.Trips)
                        {
                            RigasSatiksmeStop firstStop = trip.Points.First().Stop;
                            if (!endStops.Contains(firstStop))
                                endStops.Add(firstStop);
                            
                            RigasSatiksmeStop lastStop = trip.Points.First().Stop;
                            if (!endStops.Contains(lastStop))
                                endStops.Add(lastStop);
                        }
                    }

                    report.AddEntry(ReportGroup.NoOsmRouteMatch, "RS " + rsRoute.CleanType + " route #" + rsRoute.Number + " \"" + rsRoute.Name + "\" did not match any OSM route. RS end stops are " + string.Join(", ", endStops.Select(s => "\"" + s.Name + "\" (" + (fullyMatchedStops.ContainsKey(s) ? "https://www.openstreetmap.org/node/" + fullyMatchedStops[s].Id : "no matched OSM stop") + ")")) + ".");
                }
                else
                {
                    // TODO: same number of services?

                    foreach (OsmRelation osmRoute in matchingOsmRoutes)
                    {
                        List<OsmElement> routeStops = osmRoute.Members.Select(m => osmStops.Elements.FirstOrDefault(e => e.Id == m.Id)).Where(e => e != null).ToList()!;

                        (RigasSatiksmeTrip trip, float match) = FindBestTripMatch(rsRoute, routeStops);

#if !REMOTE_EXECUTION
                        Debug.WriteLine("* \"" + osmRoute.GetValue("name") + "\" best match at " + (match * 100f).ToString("F1") + "% for " + trip);
#endif

                        if (match >= 0)
                        {
                            List<RigasSatiksmeStop> missingRsStops = new List<RigasSatiksmeStop>();
                            List<OsmElement> missingOsmStops = new List<OsmElement>();
                            
                            foreach (RigasSatiksmeStop rsStop in trip.Stops)
                            {
                                if (fullyMatchedStops.TryGetValue(rsStop, out OsmNode? expectedOsmStop))
                                {
                                    if (!routeStops.Contains(expectedOsmStop))
                                    {
                                        missingRsStops.Add(rsStop);
                                        report.AddEntry(
                                            ReportGroup.RsRouteMissingOsmStop, 
                                            "RS " + rsRoute.CleanType + " route's #" + rsRoute.Number + " \"" + rsRoute.Name + "\" " +
                                            //"trip's #" + trip.Id + " " + -- meaningless ID since many repeat it and this is first found
                                            "stop \"" + rsStop.Name + "\" matched to " +
                                            "OSM stop " + expectedOsmStop.GetValue("name") + "\" https://www.openstreetmap.org/node/" + expectedOsmStop.Id + " " +
                                            "was not in the matched " +
                                            "OSM route \"" + osmRoute.GetValue("name") + "\" https://www.openstreetmap.org/relation/" + osmRoute.Id + ".", 
                                            rsStop
                                        );
                                    }
                                }
                                else
                                {
                                    // We already reported in general that this RS stop doesn't have OSM stop match - don't spam for every route - they will all not have it
                                    // todo: summarize how many rs trips are missing stuff because of it?
                                }
                            }

                            foreach (OsmElement routeStop in routeStops)
                            {
                                if (fullyMatchedStops.All(ms => ms.Value != routeStop))
                                {
                                    missingOsmStops.Add(routeStop);
                                    report.AddEntry(
                                        ReportGroup.OsmRouteExtraStop, 
                                        "OSM route \"" + osmRoute.GetValue("name") + "\" https://www.openstreetmap.org/relation/" + osmRoute.Id + " " +
                                        "has " + (routeStop.HasKey("name") ? "a stop \"" + routeStop.GetValue("name") + "\"" : "an unnamed stop") + " https://www.openstreetmap.org/node/" + routeStop.Id + 
                                        ", which is not in the matched " +
                                        "RS " + rsRoute.CleanType + " route's #" + rsRoute.Number + " \"" + rsRoute.Name + "\" " +
                                        //"trip #" + trip.Id + -- meaningless ID since many repeat it and this is first found
                                        ".", 
                                        routeStop
                                    );
                                }
                            }

                            foreach (RigasSatiksmeStop rsStop in missingRsStops)
                            {
                                OsmElement? possibleRematch = missingOsmStops.FirstOrDefault(s => s.HasKey("name") && IsStopNameMatchGoodEnough(rsStop.Name, s.GetValue("name")!));

                                if (possibleRematch != null)
                                {
                                    // Cancel reporting each stop individually
                                    report.CancelEntry(ReportGroup.RsRouteMissingOsmStop, rsStop);
                                    report.CancelEntry(ReportGroup.OsmRouteExtraStop, possibleRematch);

                                    OsmNode originalMatch = fullyMatchedStops[rsStop];

                                    double distance = OsmGeoTools.DistanceBetween(originalMatch.Lat, originalMatch.Lon, rsStop.Lat, rsStop.Lon);

                                    report.AddEntry(
                                        ReportGroup.StopRematchFromRoutes,
                                        "RS " + rsRoute.CleanType + " route's #" + rsRoute.Number + " \"" + rsRoute.Name + "\" " +
                                        //"trip's #" + trip.Id + " " + -- meaningless ID since many repeat it and this is first found
                                        "stop \"" + rsStop.Name + "\" " +
                                        "and OSM route's \"" + osmRoute.GetValue("name") + "\" https://www.openstreetmap.org/relation/" + osmRoute.Id + " " +
                                        "stop \"" + possibleRematch.GetValue("name") + "\" https://www.openstreetmap.org/node/" + possibleRematch.Id + " " +
                                        "appear in both routes but did not cross-match originally " +
                                        "(RS stop matched \"" + originalMatch.GetValue("name") + "\" https://www.openstreetmap.org/node/" + originalMatch.Id + " " + distance.ToString("F0") + " m away)" +
                                        "."
                                    );
                                }
                            }
                        }
                        else
                        {
                            // TODO: if low match, report as really bad and don't bother with stops
                        }

                        // todo: report platform role not set for platforms
                        // todo: report platform node that we did not find/detect as a platform
                    }

                    // TODO: match services to routes - this may be nigh impossible unless I can distinguish expected regular and optional non-regular trips/services - it's a mess - OSM only has regular for most
                    
                    
                    (RigasSatiksmeTrip service, float bestMatch) FindBestTripMatch(RigasSatiksmeRoute rsRoute, List<OsmElement> osmStops)
                    {
                        // I have to do fuzyz matching, because I don't actually know which service and which trip OSM is representing
                        // That is, I don't know which RS trip is the regular normal trip and which are random depo and alternate trips
                        // OSM may even have alternate trips, so I need to match them in that case
                        // So this matches the route with the best stop match - more stop matches, better match
                        
                        RigasSatiksmeTrip? bestTrip = null;
                        float bestMatch = 0f;
                        
                        foreach (RigasSatiksmeService rsService in rsRoute.Services)
                        {
                            foreach (RigasSatiksmeTrip rsTrip in rsService.Trips)
                            {
                                int matchedStops = 0;

                                foreach (RigasSatiksmeStop rsStop in rsTrip.Stops)
                                {
                                    if (fullyMatchedStops.TryGetValue(rsStop, out OsmNode? expectedOsmStop))
                                    {
                                        if (osmStops.Contains(expectedOsmStop))
                                            matchedStops++;
                                    }
                                }

                                float match = Math.Max(0f, (float)matchedStops / Math.Max(rsTrip.Points.Count(), osmStops.Count));

                                if (bestTrip == null ||
                                    match > bestMatch)
                                {
                                    bestMatch = match;
                                    bestTrip = rsTrip;
                                }
                            }
                        }

                        return (bestTrip!, bestMatch); // will always match at least something, even if 0%
                    }
                }
            }
        }


        [Pure]
        private static bool MatchesRoute(OsmRelation osmRoute, RigasSatiksmeRoute route)
        {
            // OSM
            // from	Preču 2
            // name	Bus 13: Preču 2 => Kleisti => Babītes stacija
            // public_transport:version	2
            // ref	13
            // roundtrip	no
            // route	bus
            // to	Babītes stacija
            // type	route
            // via	Kleisti
            
            // RS
            // riga_bus_13,"13","Babītes stacija - Kleisti - Preču 2",,3,https://saraksti.rigassatiksme.lv/index.html#riga/bus/13,F4B427,FFFFFF,2001300

            if (route.Type != osmRoute.GetValue("route"))
                return false;
            
            if (route.Number != osmRoute.GetValue("ref"))
                return false;

            string? osmName = osmRoute.GetValue("name");

            if (osmName == null)
                return false;

            string[] split = route.Name.Split('-');

            int count = 0;
            
            foreach (string s in split)
            {
                string rsName = s.Trim();

                if (OsmNameHasRSNamePart(osmName, rsName))
                    count++;

                // TODO: This will probably need matching RS<->OSM name matching
            }

            if (count < 2)
                return false;
            
            // Some are Abrenes iela - Jaunciems - Suži
            // Some are Abrenes iela - Jaunmārupe

            return true; // Guess it's "good enough" without more complex checks
            
            
            [Pure]
            static bool OsmNameHasRSNamePart(string osmName, string rsName)
            {
                // Straight match
                if (osmName.Contains(rsName))
                    return true;
                
                // TEC 2 vs TEC-2
                if (osmName.Replace('-', ' ').Contains(rsName))
                    return true;

                return false;
            }
        }

        [Pure]
        private static StopNameMatching DoStopsMatch(OsmNode osmStop, RigasSatiksmeStop rsStop)
        {
            string? stopName = osmStop.GetValue("name");

            if (stopName == null)
                return StopNameMatching.Mismatch;

            bool tramStop =
                osmStop.HasValue("railway", "tram_stop") ||
                osmStop.HasValue("disused:railway", "tram_stop") ||
                osmStop.HasValue("tram", "yes");

            bool trolleybusStop =
                osmStop.HasValue("highway", "bus_stop") ||
                osmStop.HasValue("trolleybus", "yes");

            bool busStop =
                osmStop.HasValue("highway", "bus_stop") &&
                !osmStop.HasValue("trolleybus", "yes");

            bool typeMatch = 
                tramStop && rsStop.Tram ||
                trolleybusStop && rsStop.Trolleybus ||
                busStop && rsStop.Bus;
            
            if (IsStopNameMatchGoodEnough(rsStop.Name, stopName))
                return typeMatch ? StopNameMatching.Match : StopNameMatching.WeakMatch;
            
            // Stops in real-life can have a different name to RS
            // For example OSM "Ulbrokas ciems" is signed so in real-life vs RS "Ulbroka"
            // After verifying these, one can keep `name=Ulbrokas ciems` but `alt_name=Ulbroka`, so we can match these
            
            string? stopAltName = osmStop.GetValue("alt_name");

            if (stopAltName != null)
                if (IsStopNameMatchGoodEnough(rsStop.Name, stopAltName))
                    return typeMatch ? StopNameMatching.Match : StopNameMatching.WeakMatch;
            
            return StopNameMatching.Mismatch;
        }

        [Pure]
        private static bool IsStopNameMatchGoodEnough(string rsStopName, string osmStopName)
        {
            // Quick check first, may be we don't need to do anything
            if (rsStopName == osmStopName)
                return true;
            
            // Both OSM and RS stops are inconsistent about spacing around characters
            // "2.trolejbusu parks" or "Jaunciema 2.šķērslīnija" (also all the abbreviated "P.Lejiņa iela" although this won't match)
            // "Upesgrīvas iela/ Spice"
            // OSM "TEC-2 pārvalde" vs RS "TEC- 2 pārvalde" or OSM "Preču-2" vs RS "Preču - 2"
            if (Regex.Replace(Regex.Replace(osmStopName, @"([\./-])(?! )", @"$1 "), @"(?<! )([\./-])", @" $1") == 
                Regex.Replace( Regex.Replace(rsStopName, @"([\./-])(?! )", @"$1 "), @"(?<! )([\./-])", @" $1"))
                return true;
            
            // Sometimes proper quotes are inconsistent between thw two
            // OSM "Arēna "Rīga"" vs RS "Arēna Rīga" or OSM ""Bērnu pasaule"" vs RS "Bērnu pasaule"
            // or opposite OSM "Dzintars" vs RS ""Dzintars""
            if (osmStopName.Replace("\"", "") == rsStopName.Replace("\"", ""))
                return true;
            
            // RS likes to abbbreviate names for stops while OSM spells them out
            // OSM "Eduarda Smiļģa iela" vs RS "E.Smiļģa iela"
            // Because there are so many like this, I will consider them correct for now, even if they aren't technically accurate 
            if (rsStopName.Contains('.') && !osmStopName.Contains('.'))
            {
                string[] rsSplit = rsStopName.Split('.');
                if (rsSplit.Length == 2)
                {
                    string rsPrefix = rsSplit[0].TrimEnd(); // "E"
                    string rsSuffiix = rsSplit[1].TrimStart(); // "Smiļģa iela"

                    if (osmStopName.StartsWith(rsPrefix) && osmStopName.EndsWith(rsSuffiix)) // not a perfect check, but good enough
                        return true;
                }
            }
            
            // RS also has some double names for some reason when OSM and real-life has just one "part"
            // RS "Botāniskais dārzs/Rīgas Stradiņa universitāte" vs OSM "Botāniskais dārzs
            if (rsStopName.Contains('/'))
            {
                string[] rsSplit = rsStopName.Split('/');
                if (rsSplit.Length == 2)
                {
                    string rsFirst = rsSplit[0].TrimEnd(); // "Botāniskais dārzs"
                    string rsSecond = rsSplit[1].TrimStart(); // "Rīgas Stradiņa universitāte"

                    if (osmStopName == rsFirst || osmStopName == rsSecond)
                        return true;
                }
            }
            
            // Couldn't match anything
            return false;
        }

        private enum StopNameMatching // order used by sorting
        {
            Mismatch = 0,
            WeakMatch = 1,
            Match = 2
        }

        private enum ReportGroup
        {
            NoStopMatchButHaveClose,
            MatchedOsmStopIsTooFar,
            NoStopMatchInRange,
            NoStopMatchAndAllFar,
            NoOsmRouteMatch,
            OsmRouteExtraStop,
            RsRouteMissingOsmStop,
            StopRematchFromRoutes
        }
    }
}