using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class StreetNameAnalyzer : Analyzer
    {
        public override string Name => "Street Names";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(RoadLawAnalysisData), typeof(KuldigaRoadsAnalysisData) };
        

        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();
           
            OsmMasterData osmMasterData = osmData.MasterData;

            OsmDataExtract osmNamedWays = osmMasterData.Filter(
                new IsWay(),
                new HasKey("name"),
                new HasAnyValue("highway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "living_street", "service", "track", "trunk_link", "primary_link", "secondary_link")
            );

            OsmDataExtract osmNamedRoutes = osmMasterData.Filter(
                new IsRelation(),
                new HasKey("name"),
                new HasKey("ref"), // otherwise it'll find all the local roads being grouped into route, we want a "full ref" 
                new HasValue("type", "route"),
                new HasValue("route", "road")
            );
            
            // Filter strictly to inside Latvia
            
            OsmRelation latviaRelation = (OsmRelation)osmData.MasterData.Find(
                new IsRelation(),
                new HasValue("type", "boundary"),
                new HasValue("admin_level", "2"),
                new HasValue("name", "Latvija")
            )!; // never expecting to not have this

            OsmPolygon latviaPolygon = latviaRelation.GetOuterWayPolygon();

            InsidePolygon insidePolygonFilter = new InsidePolygon(latviaPolygon, OsmPolygon.RelationInclusionCheck.Fuzzy); // somewhat expensive, so keep outside
            
            osmNamedWays = osmNamedWays.Filter(insidePolygonFilter);
            
            // Load known suffixes
            
            string knownSuffixFileName = @"data/street name suffixes.tsv";

            if (!File.Exists(knownSuffixFileName))
                knownSuffixFileName = @"../../../../" + knownSuffixFileName; // "exit" Osmalyzer\bin\Debug\net6.0\ folder and grab it from root data\
            
            string[] knownSuffixesRaw = File.ReadAllLines(knownSuffixFileName, Encoding.UTF8);

            List<KnownSuffix> knownSuffixes = knownSuffixesRaw.Select(ks => new KnownSuffix(ks)).ToList();
            
            // Load known names
            
            string knownNamesFileName = @"data/known street names.tsv";

            if (!File.Exists(knownNamesFileName))
                knownNamesFileName = @"../../../../" + knownNamesFileName; // "exit" Osmalyzer\bin\Debug\net6.0\ folder and grab it from root data\
            
            List<string> knownNames = File.ReadAllLines(knownNamesFileName, Encoding.UTF8).Select(l => l.Split('\t')[0]).ToList();

            // Get law road data

            RoadLawAnalysisData roadLawData = datas.OfType<RoadLawAnalysisData>().First();

            RoadLaw roadLaw = roadLawData.RoadLaw;
            
            // Get Kuldiga road names
            
            KuldigaRoadsAnalysisData kuldigaRoadsData = datas.OfType<KuldigaRoadsAnalysisData>().First();

            // Prepare report groups

            report.AddGroup(ReportGroup.UnknownSuffixes, "Unknown names", "These names are not necessarily wrong, just not recognized as common street names or classified by some other name source.");
            
            report.AddGroup(ReportGroup.KnownSuffixes, "Recognized street name suffixes", "These names have common suffixes for street names, so they are assumed to be \"real\" street and road names.");

            report.AddGroup(ReportGroup.KnownNames, "Recognized names", "These names were manually checked as having unique but valid street names in cadaster.");

            report.AddGroup(ReportGroup.RouteNames, "Named after regional routes", "The names of these roads match a regional road route. While technically incorrect (road name is not route name), these are commonly used and not invalid per se.");

            report.AddGroup(ReportGroup.LVMRoads, "LVM roads", "These roads are marked as operated by LVM and so they mostly have unique names.");
            
            report.AddGroup(ReportGroup.KuldigaRoads, "Kuldīga roads", "These roads are listed as local Kuldīga region roads.");

            // Parse

            OsmGroups osmWaysByName = osmNamedWays.GroupByValues("name", false);

            List<(string n, int c)> knownFullyMatchedNames = new List<(string n, int c)>();
            List<(string n, string r)> cleanlyMatchedOsmRouteNames = new List<(string n, string r)>();
            List<(string n, string r)> cleanlyMatchedLawRouteNames = new List<(string n, string r)>();
            List<(string n, int c)> lvmFullyMatchedNames = new List<(string n, int c)>();
            List<(string n, int c)> kuldigaMatchedNames = new List<(string n, int c)>();

            foreach (OsmGroup osmGroup in osmWaysByName.groups)
            {
                string wayName = osmGroup.Value;
                // todo: alt names? match any? must match all? how to
                
                // Match to suffixes
                
                KnownSuffix? knownSuffix = GetKnownSuffixForName(wayName, knownSuffixes);

                if (knownSuffix != null)
                {
                    knownSuffix.AddStatsFromFoundGroup(osmGroup);
                }
                else
                {
                    // Try to match known names

                    if (knownNames.Contains(wayName))
                    {
                        knownFullyMatchedNames.Add((wayName, osmGroup.Elements.Count));
                        continue;
                    }
                    
                    // Try to match to regional routes or law road list

                    RouteNameMatch routeNameMatch = IsWayNamedAfterMajorRouteName(wayName, osmNamedRoutes, roadLaw, out string? routeRef, out string? routeName);

                    switch (routeNameMatch)
                    {
                        case RouteNameMatch.YesOsm:
                            cleanlyMatchedOsmRouteNames.Add((wayName, routeRef!));
                            continue;
                        
                        case RouteNameMatch.PartialOsm:
                            report.AddEntry(
                                ReportGroup.RouteNames,
                                new IssueReportEntry(
                                    "Ways partially match regional route \"" + routeName + "\" for \"" + routeRef + "\" " +
                                    "as name \"" + wayName + "\" on " + osmGroup.Elements.Count + " road (segments) - " +
                                    ReportEntryFormattingHelper.ListElements(osmGroup.Elements),
                                    new SortEntryDesc(osmGroup.Elements.Count)
                                )
                            );
                            continue;
                        
                        case RouteNameMatch.YesLaw:
                            cleanlyMatchedLawRouteNames.Add((wayName, routeRef!));
                            continue;
                        
                        case RouteNameMatch.PartialLaw:
                            report.AddEntry(
                                ReportGroup.RouteNames,
                                new IssueReportEntry(
                                    "Ways don't match OSM regional route, but do partially match road law entry \"" + routeName + "\" for \"" + routeRef + "\" " +
                                    "as name \"" + wayName + "\" on " + osmGroup.Elements.Count + " road (segments) - " +
                                    ReportEntryFormattingHelper.ListElements(osmGroup.Elements),
                                    new SortEntryDesc(osmGroup.Elements.Count)
                                )
                            );
                            continue;
                    }

                    // Try to match to LVM roads
                
                    if (IsWayForLVM(osmGroup, out int lvmMatchCount))
                    {
                        if (lvmMatchCount < osmGroup.Elements.Count)
                        {
                            report.AddEntry(
                                ReportGroup.LVMRoads,
                                new IssueReportEntry(
                                    "Ways partially match LVM-operated roads for \"" + wayName + "\" " +
                                    "on " + lvmMatchCount + "/" + osmGroup.Elements.Count + " road (segments) - " +
                                    ReportEntryFormattingHelper.ListElements(osmGroup.Elements),
                                    new SortEntryDesc(osmGroup.Elements.Count)
                                )
                            );
                        }
                        else
                        {
                            lvmFullyMatchedNames.Add((wayName, lvmMatchCount));
                        }

                        continue;
                    }
                    
                    // Try to match Kuldiga names

                    KuldigaNameMatch kuldigaNameMatch = IsWayNamedAfterKuldigaRoad(wayName, kuldigaRoadsData.RoadNames);

                    switch (kuldigaNameMatch)
                    {
                        case KuldigaNameMatch.Yes:
                        case KuldigaNameMatch.Partial: // kuldiga names themselves are badly formatted, so reporting "partial" means nothing unless we format them into proper expected names
                            kuldigaMatchedNames.Add((wayName, osmGroup.Elements.Count));
                            continue;
                        
                        case KuldigaNameMatch.No:
                            break;
                        
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    
                    // Nothing matched anything, so unknown
                    
                    report.AddEntry(
                        ReportGroup.UnknownSuffixes,
                        new IssueReportEntry(
                            "Unknown name \"" + wayName + "\" on " + osmGroup.Elements.Count + " road (segments) - " +
                            ReportEntryFormattingHelper.ListElements(osmGroup.Elements),
                            new SortEntryDesc(osmGroup.Elements.Count),
                            osmGroup.GetAverageElementCoord()
                        )
                    );
                }
            }
            
            // Known suffixes

            foreach (KnownSuffix knownSuffix in knownSuffixes)
            {
                report.AddEntry(
                    ReportGroup.KnownSuffixes,
                    new GenericReportEntry(
                        "Recognized suffix \"" + knownSuffix.Suffix + "\" " +
                        "found in " + knownSuffix.FoundVariantCount + 
                        " variants on " + knownSuffix.FoundTotalCount + " road (segments)",
                        new SortEntryDesc(knownSuffix.FoundVariantCount)
                    )
                );
            }
            
            // Known names

            if (knownFullyMatchedNames.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.KnownNames,
                    new GenericReportEntry(
                        "These " + knownFullyMatchedNames.Count + " names are known as unique and valid: " +
                        string.Join(", ", knownFullyMatchedNames.Select(n => "\"" + n.n + "\" x " + n.c + ""))
                    )
                );
            }

            // Matched names to something
            
            if (cleanlyMatchedOsmRouteNames.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.RouteNames,
                    new GenericReportEntry(
                        "These " + cleanlyMatchedOsmRouteNames.Count + " names fully matched regional route roads: " +
                        string.Join(", ", cleanlyMatchedOsmRouteNames.Select(n => "\"" + n.n + "\" for \"" + n.r + "\""))
                    )
                );
            }

            if (cleanlyMatchedLawRouteNames.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.RouteNames,
                    new GenericReportEntry(
                        "These " + cleanlyMatchedLawRouteNames.Count + " names fully matched entries in road law, but not any OSM regional route roads: " +
                        string.Join(", ", cleanlyMatchedLawRouteNames.Select(n => "\"" + n.n + "\" for \"" + n.r + "\""))
                    )
                );
            }

            if (lvmFullyMatchedNames.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.LVMRoads,
                    new GenericReportEntry(
                        "These " + lvmFullyMatchedNames.Count + " names fully matched for LVM-operated roads: " +
                        string.Join(", ", lvmFullyMatchedNames.Select(n => "\"" + n.n + "\" x " + n.c + ""))
                    )
                );
            }

            if (kuldigaMatchedNames.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.KuldigaRoads,
                    new GenericReportEntry(
                        "These " + kuldigaMatchedNames.Count + " names fully matched to Kuldīga local roads: " +
                        string.Join(", ", kuldigaMatchedNames.Select(n => "\"" + n.n + "\" x " + n.c + ""))
                    )
                );
            }


            [Pure]
            static KnownSuffix? GetKnownSuffixForName(string name, List<KnownSuffix> suffixes)
            {
                foreach (KnownSuffix knownSuffix in suffixes)
                {
                    if (name.Length > knownSuffix.Suffix.Length && // can't be all suffix like "iela" or "taka"
                        name.ToLower().EndsWith(knownSuffix.Suffix)) // space not required
                    {
                        return knownSuffix;
                    }
                }

                return null;
            }

            [Pure]
            static RouteNameMatch IsWayNamedAfterMajorRouteName(string wayName, OsmDataExtract namedRoutes, RoadLaw roadLaw, out string? routeRef, out string? routeName)
            {
                // Match agains an OSM road route
                
                OsmElement? foundRoute = namedRoutes.Elements.FirstOrDefault(e => IsNameMatch(e.GetValue("name")!, wayName, out bool _));

                if (foundRoute != null)
                {
                    routeName = foundRoute.GetValue("name")!;
                    routeRef = foundRoute.GetValue("ref"); 

                    // Check again, but get extra info
                    IsNameMatch(routeName, wayName, out bool cleanMatch);
                    
                    return cleanMatch ? RouteNameMatch.YesOsm : RouteNameMatch.PartialOsm;
                }

                // Match against the law route list
                
                ActiveRoad? foundLawRoad = roadLaw.roads.OfType<ActiveRoad>().FirstOrDefault(r => IsNameMatch(r.Name, wayName, out bool _));

                if (foundLawRoad != null)
                {
                    routeName = foundLawRoad.Name;
                    routeRef = foundLawRoad.Code; 

                    // Check again, but get extra info
                    IsNameMatch(routeName, wayName, out bool cleanMatch);
                    
                    return cleanMatch ? RouteNameMatch.YesLaw : RouteNameMatch.PartialLaw;
                }

                // Couldn't match
                
                routeName = null;
                routeRef = null;
                return RouteNameMatch.No;
                

                [Pure]
                static bool IsNameMatch(string routeName, string roadName, out bool cleanMatch)
                {
                    if (routeName == roadName)
                    {
                        cleanMatch = true;
                        return true;
                    }

                    cleanMatch = false;
                    
                    if (CleanName(routeName) == CleanName(roadName))
                        return true;

                    return false;


                    [Pure]
                    static string CleanName(string name)
                    {
                        // Remove braces
                        // "Krāslava–Preiļi–Madona (Madonas apvedceļš)" == "Krāslava — Preiļi — Madona" 
                        name = Regex.Replace(name, @"\([^\)]+\)", @"");
                        // have to do this before, because these happen near dashes with spaces in all sorts of combos

                        // In case above created extra space in the middle
                        name = name.Replace("  ", " ");

                        name = name
                               .Replace("—", "-") // mdash
                               .Replace("–", "-") // ndash
                               .Replace(" - ", "-")
                               .Replace("- ", "-")
                               .Replace(" -", "-")
                            ;
                        
                        return name.Trim();
                    }
                }
            }

            [Pure]
            static bool IsWayForLVM(OsmGroup osmGroup, out int count)
            {
                count = osmGroup.Elements.Count(e => e.GetValue("operator") == "Latvijas valsts meži");
                return count >= 1;
            }
            
            [Pure]
            static KuldigaNameMatch IsWayNamedAfterKuldigaRoad(string wayName, List<string> kuldigaRoadNames)
            {
                string? foundName = kuldigaRoadNames.FirstOrDefault(n => IsNameMatch(n, wayName, out bool _));

                if (foundName != null)
                {
                    // Check again, but get extra info
                    IsNameMatch(wayName, wayName, out bool cleanMatch);
                    
                    return cleanMatch ? KuldigaNameMatch.Yes : KuldigaNameMatch.Partial;
                }

                return KuldigaNameMatch.No;
                
                
                [Pure]
                static bool IsNameMatch(string routeName, string roadName, out bool cleanMatch)
                {
                    if (routeName == roadName)
                    {
                        cleanMatch = true;
                        return true;
                    }

                    cleanMatch = false;
                    
                    if (CleanName(routeName) == CleanName(roadName))
                        return true;

                    return false;


                    [Pure]
                    static string CleanName(string name)
                    {
                        name = name
                               .Replace("—", "-") // mdash
                               .Replace("–", "-") // ndash
                               .Replace(" - ", "-")
                               .Replace("- ", "-")
                               .Replace(" -", "-")
                            ;
                        
                        return name.Trim();
                    }
                }
            }
        }


        private class KnownSuffix
        {
            public string Suffix { get; }
            
            public int FoundTotalCount { get; private set; }
            
            public int FoundVariantCount { get; private set; }

            
            public KnownSuffix(string rawLine)
            {
                string[] split = rawLine.Split('\t');

                Suffix = split[0];
            }

            
            public void AddStatsFromFoundGroup(OsmGroup group)
            {
                FoundTotalCount += group.Elements.Count;
                FoundVariantCount++;
            }
        }

        private enum RouteNameMatch
        {
            YesOsm,
            PartialOsm,
            No,
            YesLaw,
            PartialLaw
        }

        private enum KuldigaNameMatch
        {
            Yes,
            Partial,
            No
        }

        private enum ReportGroup
        {
            UnknownSuffixes,
            KnownSuffixes,
            RouteNames,
            LVMRoads,
            KnownNames,
            KuldigaRoads
        }
    }
}