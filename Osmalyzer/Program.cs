using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Osmalyzer
{
    public static class Program
    {
        private const string osmDataFileName = @"latvia-latest.osm.pbf"; // from https://download.geofabrik.de/europe/latvia.html
        private const string osmDate = @"2023-05-29T20:21:24Z"; // todo: read this from the file
        
        
        public static void Main(string[] args)
        {
            //ParseLVCRoads();

            //ParseCommonNames();
            
            //ParseHighwaySpeedConditionals();
            
            //ParseTrolleyWires();

            ParseRigasSatiksme();
        }

        
        private static void ParseRigasSatiksme()
        {
            // Load RS stop data

            RigasSatiksmeData rsData = new RigasSatiksmeData("RS");

            Console.OutputEncoding = Encoding.Unicode;
            foreach (RigasSatiksmeRoute route in rsData.Routes.Routes)
                Console.WriteLine(route.Id + " - " + route.Name + " x" + route.Trips.Count() + " trips");

            //foreach (RigasSatiksmeTrip trip in rsData.Trips.Trips)
            //    Console.WriteLine(trip.Id + " for " + trip.Route.Name + " - x" + trip.Points.Count());

            return;
            
            // Start report file
            
            const string reportFileName = @"Rigas Satiksme report.txt";
            const string rsDataDate = "2023-04-25"; // TODO: is it specified somewhere? 

            using StreamWriter reportFile = File.CreateText(reportFileName);
            
            // Load OSM data

            List<OsmBlob> blobs = OsmBlob.CreateMultiple(
                osmDataFileName,
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
                    }
                }
            );
            
            OsmBlob osmStops = blobs[0];

            // Parse

            const double maxSearchDistance = 150.0; // we search this far for potential stops
            const double acceptDistance = 50.0; // but only this far counts as a good match

            List<OsmNode> matchedOsmStops = new List<OsmNode>();
            // so that we don't match the same stop multiple times

            List<string> matchedOsmIsTooFar = new List<string>();
            List<string> noMatchButHaveClose = new List<string>();
            List<string> noMatchAndAllFar = new List<string>();
            List<string> noMatchInRange = new List<string>();
            
            foreach (RigasSatiksmeStop rsStop in rsData.Stops.Stops)
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
                    }
                    else
                    {
                        string osmStopName = matchedStop.GetValue("name")!; // already matched, can't not have name

                        matchedOsmIsTooFar.Add("RS stop \"" + rsStop.Name + "\"" + " matches OSM stop \"" + osmStopName + "\" but is far away " + stopDistance.ToString("F0") + " m - https://www.openstreetmap.org/node/" + matchedStop.Id + " , expecting around https://www.openstreetmap.org/#map=19/" + rsStop.Lat.ToString("F5") + "/" + rsStop.Lon.ToString("F5"));
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
                            noMatchButHaveClose.Add("RS stop \"" + rsStop.Name + "\" has no matching OSM stop nearby but is closest to OSM stop \"" + osmStopName + "\" - https://www.openstreetmap.org/node/" + closestStop.Id);
                        }
                        else if (stopInAcceptRange && osmStopName == null)
                        {
                            noMatchButHaveClose.Add("RS stop \"" + rsStop.Name + "\" has no matching OSM stop nearby but is closest to unnamed OSM stop - https://www.openstreetmap.org/node/" + closestStop.Id);
                        }
                        else if (!stopInAcceptRange)
                        {
                            noMatchAndAllFar.Add("RS stop \"" + rsStop.Name + "\" has no matching OSM stop in range and all other stops are far away (closest " + (osmStopName != null ? "\"" + osmStopName + "\"" : "unnamed") + " at " + stopDistance.ToString("F0") + " m) -- https://www.openstreetmap.org/#map=19/" + rsStop.Lat.ToString("F5") + "/" + rsStop.Lon.ToString("F5"));
                        }
                    }
                    else // no stop at all within distance
                    {
                        OsmNode farawayStop = osmStops.GetClosestNodeTo(rsStop.Lat, rsStop.Lon)!;
                        double farawayDistance = OsmGeoTools.DistanceBetween(farawayStop.Lat, farawayStop.Lon, rsStop.Lat, rsStop.Lon);

                        noMatchInRange.Add("No OSM stops at all in range of RS stop \"" + rsStop.Name + "\" (closest " + farawayDistance.ToString("F0") + " m) - https://www.openstreetmap.org/#map=19/" + rsStop.Lat.ToString("F5") + "/" + rsStop.Lon.ToString("F5") + "");
                    }
                }
            }

            WriteListToReport(noMatchButHaveClose, "These RS stops don't have a matching OSM stop in range (" + maxSearchDistance + " m), but have a stop nearby (<" + acceptDistance + " m):");
            WriteListToReport(matchedOsmIsTooFar, "These RS stops have a matching OSM stop in range (" + maxSearchDistance + " m), but it is far away (>" + acceptDistance + " m)");
            WriteListToReport(noMatchInRange, "These RS stops don't have any already-unmatched OSM stops in range (" + maxSearchDistance + " m)");
            WriteListToReport(noMatchAndAllFar, "These RS stops have no matching OSM stop in range (" + maxSearchDistance + " m), and even all unmatched stops are far away (>" + acceptDistance + " m)");
            
            void WriteListToReport(List<string> list, string header)
            {
                reportFile.WriteLine(header);
                foreach (string line in list)
                    reportFile.WriteLine("* " + line);
                reportFile.WriteLine();
            }

            // todo: other way - OSM stop but no RS stop
            
            // Finish report file

            reportFile.WriteLine("OSM data as of " + osmDate + ". RS data as of " + rsDataDate + ". Provided as is; mistakes possible.");

            reportFile.Close();

            Process.Start(reportFileName);
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

        private static void ParseTrolleyWires()
        {
            // Start report file
            
            const string reportFileName = @"Trolley wire problem report.txt";
            
            using StreamWriter reportFile = File.CreateText(reportFileName);

            // Load OSM data

            List<OsmBlob> blobs = OsmBlob.CreateMultiple(
                osmDataFileName,
                new List<OsmFilter[]>()
                {
                    new OsmFilter[]
                    {
                        new IsRelation(),
                        new HasValue("type", "route"),
                        new HasValue("route", "trolleybus")
                    },
                    new OsmFilter[]  
                    {
                        new IsWay(),
                        new HasAnyValue("highway", new List<string>() { "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "service" })
                    }
                }
            );

            OsmBlob routes = blobs[0];
            OsmBlob roads = blobs[1];

            // Process

            foreach (OsmElement route in routes.Elements)
            {
                OsmRelation relation = (OsmRelation)route;

                string routeName = relation.GetValue("name")!;

                bool foundIssue = false;
                
                foreach (OsmRelationMember member in relation.Members)
                {
                    OsmElement? roadSegment = roads.Elements.FirstOrDefault(r => r.Id == member.Id);

                    if (roadSegment != null)
                    {
                        string? trolley_wire = roadSegment.GetValue("trolley_wire");
                        string? trolley_wire_forward = roadSegment.GetValue("trolley_wire:forward");
                        string? trolley_wire_backward = roadSegment.GetValue("trolley_wire:backward");

                        if (trolley_wire != null && (trolley_wire_forward != null || trolley_wire_backward != null))
                        {
                            CheckFirstMentionOfRouteIssue();
                            reportFile.WriteLine("Conflicting `trolley_wire:xxx` subvalue(s) with main `trolley_wire` value on https://www.openstreetmap.org/way/" + roadSegment.Id);
                        }
                        else if (trolley_wire != null)
                        {
                            if (trolley_wire != "yes" && trolley_wire != "no")
                            {
                                CheckFirstMentionOfRouteIssue();
                                reportFile.WriteLine("`trolley_wire` unknown value \"" + trolley_wire + "\" on https://www.openstreetmap.org/way/" + roadSegment.Id);
                            }
                        }
                        else if (trolley_wire_forward != null || trolley_wire_backward != null)
                        {
                            if (trolley_wire_forward != null && trolley_wire_forward != "yes" && trolley_wire_forward != "no")
                            {
                                CheckFirstMentionOfRouteIssue();
                                reportFile.WriteLine("`trolley_wire:forward` unknown value \"" + trolley_wire_forward + "\" on https://www.openstreetmap.org/way/" + roadSegment.Id);
                            }

                            if (trolley_wire_backward != null && trolley_wire_backward != "yes" && trolley_wire_backward != "no")
                            {
                                CheckFirstMentionOfRouteIssue();
                                reportFile.WriteLine("`trolley_wire:backward` unknown value \"" + trolley_wire_backward + "\" on https://www.openstreetmap.org/way/" + roadSegment.Id);
                            }
                        }
                        else
                        {
                            CheckFirstMentionOfRouteIssue();
                            reportFile.WriteLine("`trolley_wire` missing on https://www.openstreetmap.org/way/" + roadSegment.Id);
                        }


                        void CheckFirstMentionOfRouteIssue()
                        {
                            if (!foundIssue)
                            {
                                foundIssue = true;
                                reportFile.WriteLine("Route " + route.Id + " \"" + routeName + "\"");
                            }
                        }
                    }
                }
            }

            // Finish report file
                
            reportFile.WriteLine("Data as of " + osmDate + ". Provided as is; mistakes possible.");

            reportFile.Close();

            Process.Start(reportFileName);
            
            // TODO: trolley_wire=no, but no route - pointless? not that it hurts anything
        }

        private static void ParseHighwaySpeedConditionals()
        {
            // Load OSM data

            OsmBlob speedLimitedRoads = new OsmBlob(
                osmDataFileName,
                new IsWay(),
                new HasAnyValue("highway", new List<string>() { "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "service" }),
                new HasTag("maxspeed"),
                new HasTag("maxspeed:conditional")
            );
            
            // Start report file
            
            const string reportFileName = @"Max speed conditional report.txt";
            
            using StreamWriter reportFile = File.CreateText(reportFileName);

            reportFile.WriteLine("Ways with maxspeed and maxspeed:conditional: " + speedLimitedRoads.Elements.Count);

            // Process
            
            List<(int regular, int conditional)> limits = new List<(int regular, int conditional)>(); 
                
            foreach (OsmElement way in speedLimitedRoads.Elements)
            {
                string maxspeedStr = way.GetValue("maxspeed")!;

                if (int.TryParse(maxspeedStr, out int maxspeed))
                {
                    string maxspeedConditionalStr = way.GetValue("maxspeed:conditional")!;

                    Match match = Regex.Match(maxspeedConditionalStr, @"([0-9]+)\s*@\s*\(May 1\s*-\s*Oct 1\)");

                    if (match.Success)
                    {
                        int maxspeedConditional = int.Parse(match.Groups[1].ToString());
                        
                        if (!limits.Any(l => l.regular == maxspeed && l.conditional == maxspeedConditional))
                            limits.Add((maxspeed, maxspeedConditional));
                        
                        if (maxspeed == maxspeedConditional)
                            reportFile.WriteLine("Same limits for " + maxspeed + ": " + maxspeedConditionalStr + " https://www.openstreetmap.org/way/" + way.Id);
                    }
                    else
                    {
                        if (!Regex.IsMatch(maxspeedConditionalStr, @"\d+ @ \((\w\w-\w\w )?\d\d:\d\d-\d\d:\d\d\)")) // "30 @ (Mo-Fr 07:00-19:00)" / "90 @ (22:00-07:00)"
                        {
                            reportFile.WriteLine("Conditional not recognized: " + maxspeedConditionalStr + " https://www.openstreetmap.org/way/" + way.Id);
                        }
                    }
                }
                else
                {
                    reportFile.WriteLine("Max speed not recognized as seasonal: " + maxspeedStr);
                }
            }

            limits.Sort();
            
            reportFile.WriteLine("Combos found:");

            foreach ((int regular, int conditional) in limits)
            {
                reportFile.WriteLine("Conditional limit " + conditional + " for regular limit " + regular);
            }
                
            
            // Finish report file
                
            reportFile.WriteLine("Data as of " + osmDate + ". Provided as is; mistakes possible.");

            reportFile.Close();

            Process.Start(reportFileName);
        }

        private static void ParseCommonNames()
        {
            const int titleCountThreshold = 10;

            List<string> titleTags = new List<string>() { "brand", "name", "operator" };
            // Note that the first found is picked, so if there's no brand but is a "name", then "operator" will be ignored and "name" picked

            // Start report file

            const string reportFileName = @"Common name report.txt";
            
            using StreamWriter reportFile = File.CreateText(reportFileName);

            reportFile.WriteLine("These are the most common POI titles with at least " + titleCountThreshold + " occurences grouped by type (recognized by NSI):");
            
            reportFile.WriteLine("title(s)" + "\t" + "count" + "\t" + "counts" + "\t" + "tag" + "\t" + "value(s)");

            // Load OSM data

            OsmBlob titledElements = new OsmBlob(
                osmDataFileName,
                new IsNodeOrWay(),
                new HasAnyTag(titleTags)
            );

            string nsiTagsFileName = @"NSI tags.tsv"; // from https://nsi.guide/?t=brands

            if (!File.Exists(nsiTagsFileName))
                nsiTagsFileName = @"..\..\..\data\" + nsiTagsFileName; // "exit" Osmalyzer\bin\Debug folder and grab it from root data\
            
            string[] nsiRawTags = File.ReadAllLines(nsiTagsFileName);

            List<(string, List<string>)> nsiTags = nsiRawTags.Select(t =>
            {
                int i = t.IndexOf('\t'); 
                return (t.Substring(0, i), t.Substring(i + 1).Split(';').ToList());
            }).ToList();
            // todo: retrieve automatically from NSI repo or wherever they keep these
            // todo: would need to manually specify exceptions/grouping if parsing
            // todo: this can only group different values for the same key, not different keys

            List<(int count, string line)> reportEntries = new List<(int, string)>();
            
            foreach ((string nsiTag, List<string> nsiValues) in nsiTags)
            {
                OsmBlob matchingElements = titledElements.Filter(
                    new HasAnyValue(nsiTag, nsiValues)
                );

                OsmGroups titleGroupsSeparate = matchingElements.GroupByValues(titleTags, false);

                OsmMultiValueGroups titleGroupsSimilar = titleGroupsSeparate.CombineBySimilarValues(
                    (s1, s2) => string.Equals(
                        CleanName(s1), 
                        CleanName(s2), 
                        StringComparison.InvariantCulture)
                );

                string CleanName(string s)
                {
                    return s
                           .Trim()
                           .ToLower()
                           .Replace("!", "") // e.g. Top! -> Top
                           .Replace("ā", "a")
                           .Replace("č", "c")
                           .Replace("ē", "e")
                           .Replace("ģ", "g")
                           .Replace("ī", "i")
                           .Replace("ķ", "k")
                           .Replace("ļ", "l")
                           .Replace("ņ", "n")
                           .Replace("ō", "o")
                           .Replace("š", "s")
                           .Replace("ū", "u")
                           .Replace("ž", "z");
                }

                foreach (OsmMultiValueGroup group in titleGroupsSimilar.groups)
                {
                    if (group.Elements.Count >= titleCountThreshold)
                    {
                        string reportLine =
                            string.Join(", ", group.Values.Select(v => "\"" + v + "\"")) +
                            "\t" +
                            group.Elements.Count +
                            "\t" +
                            (group.Values.Count > 1 ? string.Join("+", group.ElementCounts.Select(c => c.ToString())) : "") +
                            "\t" +
                            nsiTag +
                            "\t" +
                            string.Join("; ", group.GetUniqueKeyValues(nsiTag, true)) + // just because we grouped NSI POII types, doesn't mean data has instances for each
                            "\t";

                        reportEntries.Add((group.Elements.Count, reportLine));
                    }
                }
            }

            // Each NSI tag pair has a separate name multi-value group, so we need to re-sort if we want to order by count indepenedent of POI type
            reportEntries.Sort((e1, e2) => e2.count.CompareTo(e1.count));

            foreach ((int _, string line) in reportEntries)
                reportFile.WriteLine(line);

            reportFile.WriteLine(
                "POI \"title\" here means the first found value from tags " + string.Join(", ", titleTags.Select(t => "\"" + t + "\"")) + ". " +
                "Title values are case-insensitive, leading/trailing whitespace ignored, Latvian diacritics ignored, character '!' ignored. " +
                "Title counts will repeat if the same element is tagged with multiple NSI POI types.");

            // Finish report file
            
            reportFile.WriteLine("Data as of " + osmDate + ". Provided as is; mistakes possible.");

            reportFile.Close();

            Process.Start(reportFileName);
        }

        private static void ParseLVCRoads()
        {
            const string reportFileName = @"LVC road report.txt";

            using StreamWriter reportFile = File.CreateText(reportFileName);

            // Load law road data

            const string roadLawTextFileName = @"noteikumi.txt";
            // Pielikums MK 25.10.2022. noteikumu Nr. 671 redakcijā
            // todo: put this in report
            // todo: read this from the source

            RoadLaw roadLaw = new RoadLaw(roadLawTextFileName);

            // Load OSM data

            OsmBlob blob = new OsmBlob(osmDataFileName);
            // todo: filter earlier
            // todo: but we also need routes -- need "multi-filter" and multiple blobs result

            OsmBlob reffedRoads = blob.Filter(
                new IsWay(),
                new HasTag("highway"),
                new HasTag("ref"),
                new DoesntHaveTag("aeroway"), // some old aeroways are also tagged as highways
                new DoesntHaveTag("abandoned:aeroway"), // some old aeroways are also tagged as highways
                new DoesntHaveTag("disused:aeroway"), // some old aeroways are also tagged as highways
                new DoesntHaveTag("railway") // there's a few "railway=platform" and "railway=rail" with "highway=footway"
            );

            OsmBlob recognizedReffedRoads = reffedRoads.Filter(
                new SplitValuesCheck("ref", IsValidRef)
            );

            // using StreamWriter rawOutFile = File.CreateText(@"raw road refs.txt");
            // foreach (OsmElement element in recognizedReffedRoads.Elements.OrderBy(e => e.Element.Tags.GetValue("ref")))
            //     rawOutFile.WriteLine(element.Element.Tags.GetValue("ref"));
            // rawOutFile.Close();

            OsmGroups roadsByRef = recognizedReffedRoads.GroupByValues("ref", true);

            // Road on map but not in law

            List<string> mappedRoadsNotFoundInLaw = new List<string>();
            List<string> mappedRoadsNotFoundInLawFormatted = new List<string>();
            bool anyStricken = false;
            bool anyHistoric = false;

            foreach (OsmGroup osmGroup in roadsByRef.groups)
            {
                //Console.WriteLine(osmGroup.Value + " x " + osmGroup.Elements.Count);

                bool foundInLaw = roadLaw.roads.OfType<ActiveRoad>().Any(r => r.Code == osmGroup.Value);

                if (!foundInLaw)
                {
                    mappedRoadsNotFoundInLaw.Add(osmGroup.Value);

                    mappedRoadsNotFoundInLawFormatted.Add(osmGroup.Value);

                    bool foundAsStricken = roadLaw.roads.OfType<StrickenRoad>().Any(r => r.Code == osmGroup.Value);
                    if (foundAsStricken)
                    {
                        mappedRoadsNotFoundInLawFormatted[mappedRoadsNotFoundInLawFormatted.Count - 1] += "†";
                        anyStricken = true;
                    }

                    bool foundAsHistoric = roadLaw.roads.OfType<HistoricRoad>().Any(r => r.Code == osmGroup.Value);
                    if (foundAsHistoric)
                    {
                        mappedRoadsNotFoundInLawFormatted[mappedRoadsNotFoundInLawFormatted.Count - 1] += "‡";
                        anyHistoric = true;
                    }
                }
            }

            if (mappedRoadsNotFoundInLawFormatted.Count > 0)
            {
                reportFile.WriteLine(
                    (mappedRoadsNotFoundInLawFormatted.Count > 1 ? "Roads" : "Road") + " " +
                    string.Join(", ", mappedRoadsNotFoundInLawFormatted.OrderBy(v => v)) +
                    " " + (mappedRoadsNotFoundInLawFormatted.Count > 1 ? "are" : "is") + " on the map, but not in the law." +
                    (anyStricken ? " † Marked as stricken." : "") +
                    (anyHistoric ? " ‡ Formerly stricken." : "")
                );
            }
            else
            {
                reportFile.WriteLine("All roads on the map are present in the law.");
            }

            // Road in law but not on map

            List<string> lawedRoadsNotFoundOnMap = new List<string>();

            foreach (ActiveRoad road in roadLaw.roads.OfType<ActiveRoad>())
            {
                bool foundInOsm = roadsByRef.groups.Any(g => g.Value == road.Code);

                if (!foundInOsm)
                    lawedRoadsNotFoundOnMap.Add(road.Code);
            }

            if (lawedRoadsNotFoundOnMap.Count > 0)
            {
                reportFile.WriteLine(
                    (lawedRoadsNotFoundOnMap.Count > 1 ? "Roads" : "Road") + " " +
                    string.Join(", ", lawedRoadsNotFoundOnMap) +
                    " " + (lawedRoadsNotFoundOnMap.Count > 1 ? "are" : "is") + " in the law, but not on the map.");
            }
            else
            {
                reportFile.WriteLine("All roads in the law are present on the map.");
            }

            // Check shared segments

            List<(string, List<string>)> unsharedSegments = new List<(string, List<string>)>();

            foreach (KeyValuePair<string, List<string>> entry in roadLaw.SharedSegments)
            {
                List<OsmElement> matchingRoads = recognizedReffedRoads.Elements.Where(e => TagUtils.SplitValue(e.GetValue("ref")!).Contains(entry.Key)).ToList();

                if (matchingRoads.Count > 0)
                {
                    List<string> sharingsNotFound = new List<string>();

                    foreach (string shared in entry.Value)
                    {
                        bool sharingFound = false;

                        foreach (OsmElement matchingRoad in matchingRoads)
                        {
                            List<string> refs = TagUtils.SplitValue(matchingRoad.GetValue("ref")!);

                            if (refs.Contains(shared))
                            {
                                sharingFound = true;
                                break;
                            }
                        }

                        if (!sharingFound)
                            sharingsNotFound.Add(shared);
                    }

                    if (sharingsNotFound.Count > 0)
                    {
                        //reportFile.WriteLine("Road " + entry.Key + " is supposed to share segments with " + string.Join(", ", sharingsNotFound) + " but currently doesn't.");

                        unsharedSegments.Add((entry.Key, sharingsNotFound));
                    }
                }
            }

            if (unsharedSegments.Count > 0)
            {
                reportFile.WriteLine(
                    (unsharedSegments.Count > 1 ? "These roads do" : "This road does") + " not have expected overlapping segments as in the law: " +
                    string.Join("; ", unsharedSegments.OrderBy(s => s.Item1).Select(s => s.Item1 + " with " + string.Join(", ", s.Item2.OrderBy(i => i)))) +
                    "."
                );
            }
            else
            {
                reportFile.WriteLine(
                    "All roads have expected shared segments as in the law."
                );
            }

            List<(string, string)> uniqueRefPairs = new List<(string, string)>();

            foreach (OsmElement reffedRoad in reffedRoads.Elements)
            {
                List<string> refs = TagUtils.SplitValue(reffedRoad.GetValue("ref")!);

                if (refs.Count > 1)
                {
                    for (int a = 0; a < refs.Count - 1; a++)
                    {
                        for (int b = a + 1; b < refs.Count; b++)
                        {
                            string refA = refs[a];
                            string refB = refs[b];

                            bool found = uniqueRefPairs.Any(r =>
                                                                (r.Item1 == refA && r.Item2 == refB) ||
                                                                (r.Item1 == refB && r.Item2 == refA));

                            if (!found)
                                uniqueRefPairs.Add((refA, refB));
                        }
                    }
                }
            }

            List<string> sharedRefsNotInLaw = new List<string>();

            for (int i = 0; i < uniqueRefPairs.Count; i++)
            {
                string refA = uniqueRefPairs[i].Item1;
                string refB = uniqueRefPairs[i].Item2;

                bool found = roadLaw.SharedSegments.Any(ss =>
                                                            ss.Key == refA && ss.Value.Contains(refB) ||
                                                            ss.Key == refB && ss.Value.Contains(refA));

                if (!found)
                    sharedRefsNotInLaw.Add(refA + " + " + refB);
            }

            if (sharedRefsNotInLaw.Count > 0)
            {
                reportFile.WriteLine(
                    (sharedRefsNotInLaw.Count > 1 ? "These roads have" : "This road has") + " shared segments that are not in the law: " +
                    string.Join("; ", sharedRefsNotInLaw.OrderBy(s => s)) +
                    "."
                );
            }
            else
            {
                reportFile.WriteLine("There are no roads with shared refs that are not in the law.");
            }

            // todo: roads sharing segments not defined in law
            
            // todo: unconnected segments, i.e. gaps
            
            // todo: roads with missing name
            // todo: roads with mismatched name to law (but not valid street name)
            
            // todo: routes with missing name
            // todo: routes with mismatched name

            //

            OsmBlob routeRelations = blob.Filter(
                new IsRelation(),
                new HasValue("type", "route"),
                new HasValue("route", "road"),
                new HasTag("ref"),
                new SplitValuesCheck("ref", IsValidRef)
            );

            List<string> missingRelations = new List<string>();

            List<(string, int)> relationsWithSameRef = new List<(string, int)>();

            foreach (OsmGroup refGroup in roadsByRef.groups)
            {
                string code = refGroup.Value;

                List<OsmElement> elements = routeRelations.Elements.Where(e => e.GetValue("ref")! == code).ToList();

                if (elements.Count == 0)
                {
                    missingRelations.Add(code);
                }
                else if (elements.Count > 1)
                {
                    relationsWithSameRef.Add((code, elements.Count));
                }
                else
                {
                    // todo: compare ways
                }
            }

            List<string> extraRelations = new List<string>();

            foreach (OsmElement routeElement in routeRelations.Elements)
            {
                string code = routeElement.GetValue("ref")!;

                bool haveRoad = roadsByRef.groups.Any(g => g.Value == code);

                if (!haveRoad)
                    extraRelations.Add(code);
            }

            if (missingRelations.Count > 0)
            {
                reportFile.WriteLine(
                    (missingRelations.Count > 1 ? "These route relations are missing" : "This route relation is missing") + ": " +
                    string.Join(", ", missingRelations.OrderBy(c => c)) +
                    "."
                );
            }
            else
            {
                reportFile.WriteLine("There are route relations for all mapped road codes.");
            }

            if (extraRelations.Count > 0)
            {
                reportFile.WriteLine(
                    (extraRelations.Count > 1 ? "These route relations don't" : "This route relation doesn't") + " have a road with such code: " +
                    string.Join(", ", extraRelations.OrderBy(c => c)) +
                    "."
                );
            }
            else
            {
                reportFile.WriteLine("There are no route relations with codes that no road uses.");
            }

            if (relationsWithSameRef.Count > 0)
            {
                reportFile.WriteLine(
                    "These route relations have the same code: " +
                    string.Join(", ", relationsWithSameRef.OrderBy(c => c.Item1).Select(c => c.Item1 + " x " + c.Item2)) +
                    "."
                );
            }

            // Uncrecognized ref

            OsmBlob unrecognizedReffedRoads = reffedRoads.Subtract(
                recognizedReffedRoads
            );

            int excludedCount = unrecognizedReffedRoads.GroupByValues("ref", true).groups.Count; // real value below

            unrecognizedReffedRoads = unrecognizedReffedRoads.Filter(
                new SplitValuesCheck("ref", s => !IsExcludedRef(s))
            );

            OsmGroups unrecognizedRoadsByRef = unrecognizedReffedRoads.GroupByValues("ref", true);

            excludedCount -= unrecognizedRoadsByRef.groups.Count;

            if (unrecognizedRoadsByRef.groups.Count > 0)
            {
                reportFile.WriteLine(
                    (unrecognizedRoadsByRef.groups.Count > 1 ? "These road refs" : "This road ref") + " " +
                    string.Join(", ", unrecognizedRoadsByRef.groups.Select(g => g.Value).OrderBy(v => v)) +
                    " " + (unrecognizedRoadsByRef.groups.Count > 1 ? "are" : "is") + " not recognized." +
                    (excludedCount > 0 ? " " + excludedCount + " are ignored/excluded." : "")
                );
            }
            else
            {
                reportFile.WriteLine(
                    "All road refs are recognized" +
                    (excludedCount > 0 ? " and " + excludedCount + " are ignored/excluded" : "") +
                    "."
                );
            }
            
            
            // todo: missing route segments - basically relation doesn't match roads - this is going to have a TON of hits

            // Done

            reportFile.WriteLine("Data as of " + osmDate + ". Provided as is; mistakes possible.");

            reportFile.Close();

            Process.Start(reportFileName);
        }


        private static bool IsValidRef(string value)
        {
            Match match = Regex.Match(value, @"^([AVP])([1-9][0-9]{0,3})$");

            if (!match.Success)
                return false;

            string letter = match.Groups[1].ToString();
            int number = int.Parse(match.Groups[2].ToString());

            switch (letter)
            {
                case "A": return number <= 30; // max is A15 as of writing this
                case "P": return number <= 300; // max is P136 as of writing this
                case "V": return number <= 3000; // max is V1489 as of writing this
                default:  throw new InvalidOperationException();
            }
        }

        private static bool IsExcludedRef(string value)
        {
            // C class
            // C9 C-9 C27 C-122 ...
            if (Regex.IsMatch(value, @"^C-?[1-9][0-9]{0,2}$"))
                return true;

            // Limbazi https://www.limbazunovads.lv/lv/media/10414/download
            // B3.-01 A3.-03 ...
            if (Regex.IsMatch(value, @"^[AB][0-9]\.-[0-9]{2}$"))
                return true;

            // Kuldiga https://kuldiga.lv/pasvaldiba/publiskie-dokumenti/autocelu-klases
            // 6278B003 6282D011 6296C008 ...
            if (Regex.IsMatch(value, @"^62[0-9]{2}[ABCD][0-9]{3}$"))
                return true;

            // Limbazi https://www.limbazunovads.lv/lv/media/10387/download
            // C1-29 C1-30 C1-46 C1-46 - these are all of them
            // File also has A and B
            if (Regex.IsMatch(value, @"^[ABC]1-[0-9]{2}$"))
                return true;

            // Non-Latvia
            // 58К-150
            // К is cyrillic
            if (Regex.IsMatch(value, @"^58К-[0-9]{3}$"))
                return true;

            // Non-Latvia
            // Н2100 Р20 М-9
            // H, Р, М are cyrillic
            if (Regex.IsMatch(value, @"^[РНМ]-?[0-9]+$"))
                return true;

            return false;
        }
    }
}