using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
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
            report.AddGroup(ReportGroup.NoRouteMatch, "These RS routes were not matched to any OSM route:");

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
                List<OsmNode> matchedStops = closestStops.Where(s => StopNamesMatch(s, rsStop) == StopNameMatching.Match).ToList();

                // Closest matched (they are pre-sorted) is best match
                OsmNode? matchedStop = matchedStops.FirstOrDefault();

                if (matchedStop != null)
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

                        report.WriteEntry(ReportGroup.MatchedOsmStopIsTooFar, "RS stop \"" + rsStop.Name + "\"" + " matches OSM stop \"" + osmStopName + "\" but is far away " + stopDistance.ToString("F0") + " m - https://www.openstreetmap.org/node/" + matchedStop.Id + " , expecting around https://www.openstreetmap.org/#map=19/" + rsStop.Lat.ToString("F5") + "/" + rsStop.Lon.ToString("F5"));
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
                            report.WriteEntry(ReportGroup.NoStopMatchButHaveClose, "RS stop \"" + rsStop.Name + "\" has no matching OSM stop nearby but is closest to OSM stop \"" + osmStopName + "\" - https://www.openstreetmap.org/node/" + closestStop.Id);
                        }
                        else if (stopInAcceptRange && osmStopName == null)
                        {
                            report.WriteEntry(ReportGroup.NoStopMatchButHaveClose, "RS stop \"" + rsStop.Name + "\" has no matching OSM stop nearby but is closest to unnamed OSM stop - https://www.openstreetmap.org/node/" + closestStop.Id);
                        }
                        else if (!stopInAcceptRange)
                        {
                            report.WriteEntry(ReportGroup.NoStopMatchAndAllFar, "RS stop \"" + rsStop.Name + "\" has no matching OSM stop in range and all other stops are far away (closest " + (osmStopName != null ? "\"" + osmStopName + "\"" : "unnamed") + " at " + stopDistance.ToString("F0") + " m) -- https://www.openstreetmap.org/#map=19/" + rsStop.Lat.ToString("F5") + "/" + rsStop.Lon.ToString("F5"));
                        }
                    }
                    else // no stop at all within distance
                    {
                        OsmNode farawayStop = osmStops.GetClosestNodeTo(rsStop.Lat, rsStop.Lon)!;
                        double farawayDistance = OsmGeoTools.DistanceBetween(farawayStop.Lat, farawayStop.Lon, rsStop.Lat, rsStop.Lon);

                        report.WriteEntry(ReportGroup.NoStopMatchInRange, "No OSM stops at all in range of RS stop \"" + rsStop.Name + "\" (closest " + farawayDistance.ToString("F0") + " m) - https://www.openstreetmap.org/#map=19/" + rsStop.Lat.ToString("F5") + "/" + rsStop.Lon.ToString("F5") + "");
                    }
                }
            }

            // todo: other way - OSM stop but no RS stop
            
            // Parse routes

            foreach (RigasSatiksmeRoute rsRoute in rsNetwork.Routes.Routes)
            {
                List<OsmElement> matchingOsmRoutes = osmRoutes.Elements.Where(e => MatchesRoute((OsmRelation)e, rsRoute)).ToList();

                //report.WriteLine(rsRoute.Name + " - x" + matchingOsmRoutes.Count + ": " + string.Join(", ", matchingOsmRoutes.Select(s => s.Id)));

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

                    report.WriteEntry(ReportGroup.NoRouteMatch, rsRoute.CleanType + " route #" + rsRoute.Number + " \"" + rsRoute.Name + "\" did not match any OSM route. RS end stops are " + string.Join(", ", endStops.Select(s => "\"" + s.Name + "\" (" + (fullyMatchedStops.ContainsKey(s) ? "https://www.openstreetmap.org/node/" + fullyMatchedStops[s].Id : "no matched OSM stop") + ")")) + ".");
                }
                else
                {
                    // TODO: same number of services?
                    // TODO: match services
                    // TODO: for each service - same number of stops, same order
                }
            }
            
                        
            void WriteListToReport(List<string> list, string header)
            {
                if (list.Count > 0)
                {
                    report.WriteRawLine(header);
                    foreach (string line in list)
                        report.WriteRawLine("* " + line);
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
        private static StopNameMatching StopNamesMatch(OsmNode osmStop, RigasSatiksmeStop rsStop)
        {
            string? stopName = osmStop.GetValue("name");

            if (stopName == null)
                return StopNameMatching.NoName;

            if (IsStopNameMatchGoodEnough(rsStop.Name, stopName))
                return StopNameMatching.Match;
            
            // Stops in real-life can have a different name to RS
            // For example OSM "Ulbrokas ciems" is signed so in real-life vs RS "Ulbroka"
            // After verifying these, one can keep `name=Ulbrokas ciems` but `alt_name=Ulbroka`, so we can match these
            
            string? stopAltName = osmStop.GetValue("alt_name");

            if (stopAltName != null)
                if (IsStopNameMatchGoodEnough(rsStop.Name, stopAltName))
                    return StopNameMatching.Match;
            
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

        private enum StopNameMatching
        {
            Match,
            Mismatch,
            NoName
        }

        private enum ReportGroup
        {
            NoStopMatchButHaveClose,
            MatchedOsmStopIsTooFar,
            NoStopMatchInRange,
            NoStopMatchAndAllFar,
            NoRouteMatch
        }
    }
}