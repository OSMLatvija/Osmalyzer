using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Osmalyzer;

public abstract class PublicTransportAnalyzer<T> : Analyzer
    where T : GTFSAnalysisData, new()
{
    public override string Description => "This checks the public transport route issues for " + Name;

    public override AnalyzerGroup Group => AnalyzerGroups.PublicTransport;
    
    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(LatviaOsmAnalysisData), typeof(T) };


    /// <summary> Very short label for report texts </summary>
    protected abstract string Label { get; }


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load stop data

        GTFSAnalysisData gtfsData = datas.OfType<T>().First();

        GTFSNetwork gtfsNetwork = new GTFSNetwork(Path.GetFullPath(gtfsData.ExtractionFolder));
            
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        List<OsmDataExtract> osmDataExtracts = osmMasterData.Filter(
            new List<OsmFilter[]>()
            {
                new OsmFilter[]
                {
                    new IsNode(),
                    new OrMatch(
                        new HasValue("highway", "bus_stop"),
                        new HasValue("railway", "tram_stop"),
                        new HasValue("disused:railway", "tram_stop") // still valid, if not in active use - Rigas Satiksme data seems to have these too
                    )
                },
                new OsmFilter[]
                {
                    new IsRelation(),
                    new HasValue("type", "route"),
                    new OrMatch(
                        new HasAnyValue("route", "tram", "bus", "trolleybus"),
                        new HasAnyValue("disused:route", "tram", "bus", "trolleybus")
                    )
                }
            }
        );
            
        OsmDataExtract osmStops = osmDataExtracts[0];
        OsmDataExtract osmRoutes = osmDataExtracts[1];

        // Parse routes

        // GTFS groups by route and service
        // Websites (like RS) group by stop pattern/variant
        // We want to group by such variant to match how users and OSM would expect them
        
        List<RouteVariant> skippedVariants = new List<RouteVariant>();

        const int minTripCountToInclude = 5;

        foreach (GTFSRoute gtfsRoute in gtfsNetwork.Routes.Routes)
        {
            foreach (RouteVariant variant in ExtractVariants(gtfsRoute).OrderByDescending(mt => mt.TripCount))
            {
                if (variant.TripCount < minTripCountToInclude) // todo: optional, e.g. JAP lists them but RS doesn't
                {
                    // Not enough to report as a "full" route, presumably first/final depo routes 
                    skippedVariants.Add(variant);
                    continue;
                }
                
                string header = gtfsRoute.CleanType + " #" + gtfsRoute.Number + ": " + variant.FirstStop.Name + " => " + variant.LastStop.Name;
                // e.g. "Bus #2: Mangaļsala => Abrenes iela"

                report.AddGroup(variant, header);

                
                report.AddEntry(variant, new GenericReportEntry("This route has " + variant.TripCount + " trips with the unqiue sequence/pattern of " + variant.StopCount + " stops: " + string.Join(", ", variant.Stops.Select(s => s.Name))));
            }
        }

        if (skippedVariants.Count > 0)
        {
            report.AddGroup(GroupDesignation.SkippedVariants, "Skipped route variants");

            report.AddEntry(GroupDesignation.SkippedVariants, new GenericReportEntry("These routes have less than " + minTripCountToInclude + " trips with their unqiue sequence/pattern of stops, so they are skipped from full analysis."));

            foreach (RouteVariant variant in skippedVariants)
            {
                report.AddEntry(GroupDesignation.SkippedVariants, new GenericReportEntry("The route " + variant.Route.CleanType + " #" + variant.Route.Number + ": " + variant.FirstStop.Name + " => " + variant.LastStop.Name + " has " + variant.TripCount + " trips with the sequence of " + variant.StopCount + " stops: " + string.Join(", ", variant.Stops.Select(s => s.Name))));
            }
        }
    }

    [Pure]
    private static IEnumerable<RouteVariant> ExtractVariants(GTFSRoute route)
    {
        List<RouteVariant> variants = new List<RouteVariant>();

        foreach (GTFSTrip trip in route.AllTrips)
        {
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
        
        public IEnumerable<GTFSTrip> Trips => _trips;

        public IEnumerable<GTFSStop> Stops => _stops;

        public GTFSStop FirstStop => _stops[0];
        
        public GTFSStop LastStop => _stops[^1];
        
        public int StopCount => _stops.Count;
        
        public int TripCount => _trips.Count;


        private readonly List<GTFSTrip> _trips;
        
        private readonly List<GTFSStop> _stops;


        public RouteVariant(GTFSRoute route, GTFSTrip trip, IEnumerable<GTFSStop> stops)
        {
            Route = route;
            _trips = new List<GTFSTrip> { trip };
            _stops = stops.ToList();
        }

        
        public void AddTrip(GTFSTrip trip)
        {
            _trips.Add(trip);
        }
    }

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
    // [Pure]
    // private static bool IsStopNameMatchGoodEnough(string ptStopName, string osmStopName)
    // {
    //     // Stops never differ by capitalization, so just lower them and avoid weird capitalization in additon to everything else
    //     // Rezekne "18.Novembra iela" vs OSM "18. novembra iela"
    //     ptStopName = ptStopName.ToLower();
    //     osmStopName = osmStopName.ToLower();
    //         
    //     // Quick check first, may be we don't need to do anything
    //     if (ptStopName == osmStopName)
    //         return true;
    //
    //     // Rezeknes almost all stops have "uc" and "nc" suffixes like "Brīvības iela nc" and "Brīvības iela uc" - probably route direction?
    //     ptStopName = Regex.Replace(ptStopName, @" uc$", @"");
    //     ptStopName = Regex.Replace(ptStopName, @" nc$", @"");
    //         
    //     // Trim parenthesis from OSM
    //     // Jurmalas OSM stops have a lot of parenthesis, like JS "Majoru stacija" vs OSM "Majoru stacija (Majori)"
    //     osmStopName = Regex.Replace(osmStopName, @" \([^\(\)]+\)$", @"");
    //     // todo: return if the match was poor quality this way and the name should be checked
    //     // todo: what if GTFS data DOES have the parenthesis?
    //
    //     // Trim brackets from GTFS
    //     // A couple Jurmalas GTFS stops have brackets, like JS "Promenādes iela [Promenādes iela]" and "Promenādes iela [Rīgas iela]" vs OSM "Promenādes iela" and "Promenādes iela"
    //     ptStopName = Regex.Replace(ptStopName, @" \[[^\[\]]+\]$", @"");
    //         
    //     // Both OSM and RS stops are inconsistent about spacing around characters
    //     // "2.trolejbusu parks" or "Jaunciema 2.šķērslīnija" (also all the abbreviated "P.Lejiņa iela" although this won't match)
    //     // "Upesgrīvas iela/ Spice"
    //     // OSM "TEC-2 pārvalde" vs RS "TEC- 2 pārvalde" or OSM "Preču-2" vs RS "Preču - 2"
    //     if (Regex.Replace(Regex.Replace(osmStopName, @"([\./-])(?! )", @"$1 "), @"(?<! )([\./-])", @" $1") == 
    //         Regex.Replace( Regex.Replace(ptStopName, @"([\./-])(?! )", @"$1 "), @"(?<! )([\./-])", @" $1"))
    //         return true;
    //         
    //     // Sometimes proper quotes are inconsistent between the two
    //     // OSM "Arēna "Rīga"" vs RS "Arēna Rīga" or OSM ""Bērnu pasaule"" vs RS "Bērnu pasaule"
    //     // or opposite OSM "Dzintars" vs RS ""Dzintars""
    //     if (osmStopName.Replace("\"", "") == ptStopName.Replace("\"", ""))
    //         return true;
    //         
    //     // RS likes to abbreviate names for stops while OSM spells them out
    //     // OSM "Eduarda Smiļģa iela" vs RS "E.Smiļģa iela"
    //     // Because there are so many like this, I will consider them correct for now, even if they aren't technically accurate 
    //     if (ptStopName.Contains('.') && !osmStopName.Contains('.'))
    //     {
    //         string[] ptSplit = ptStopName.Split('.');
    //         if (ptSplit.Length == 2)
    //         {
    //             string ptPrefix = ptSplit[0].TrimEnd(); // "E"
    //             string ptSuffiix = ptSplit[1].TrimStart(); // "Smiļģa iela"
    //
    //             if (osmStopName.StartsWith(ptPrefix) && osmStopName.EndsWith(ptSuffiix)) // not a perfect check, but good enough
    //                 return true;
    //         }
    //     }
    //         
    //     // RS also has some double names for some reason when OSM and real-life has just one "part"
    //     // RS "Botāniskais dārzs/Rīgas Stradiņa universitāte" vs OSM "Botāniskais dārzs
    //     if (ptStopName.Contains('/'))
    //     {
    //         string[] ptSplit = ptStopName.Split('/');
    //         if (ptSplit.Length == 2)
    //         {
    //             string ptFirst = ptSplit[0].TrimEnd(); // "Botāniskais dārzs"
    //             string ptSecond = ptSplit[1].TrimStart(); // "Rīgas Stradiņa universitāte"
    //
    //             if (osmStopName == ptFirst || osmStopName == ptSecond)
    //                 return true;
    //         }
    //     }
    //         
    //     // Couldn't match anything
    //     return false;
    // }
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