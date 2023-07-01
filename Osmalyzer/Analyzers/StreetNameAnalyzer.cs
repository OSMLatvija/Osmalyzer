using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class StreetNameAnalyzer : Analyzer
    {
        public override string Name => "Street Names";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };
        

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

            // Parse

            report.AddGroup(ReportGroup.UnknownSuffixes, "Unknown names");
            report.AddEntry(ReportGroup.UnknownSuffixes, new DescriptionReportEntry("These names are not necessarily wrong, just not recognized as common street names or classified by some other name source."));
            
            report.AddGroup(ReportGroup.KnownSuffixes, "Recognized street name suffixes");
            
            report.AddGroup(ReportGroup.RouteNames, "Named after regional routes");
            
            report.AddGroup(ReportGroup.LVMRoads, "LVM roads");

            OsmGroups osmWaysByName = osmNamedWays.GroupByValues("name", false);

            List<(string n, string r)> cleanlyMatchedRouteNames = new List<(string n, string r)>();
            List<(string n, int c)> lvmFullyMatchedNames = new List<(string n, int c)>();

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
                    // Try to match to regional routes

                    RouteNameMatch routeNameMatch = IsWayNamedAfterMajorRouteName(wayName, osmNamedRoutes, out string? routeRef, out string? routeName);

                    if (routeNameMatch == RouteNameMatch.Yes)
                    {
                        cleanlyMatchedRouteNames.Add((wayName, routeRef!));
                        continue;
                    }
                    else if (routeNameMatch == RouteNameMatch.Partial)
                    {
                        report.AddEntry(
                            ReportGroup.RouteNames,
                            new IssueReportEntry(
                                "Ways partially match regional route \"" + routeName + "\" for \"" + routeRef + "\" as name \"" + wayName + "\" on " + osmGroup.Elements.Count + " road (segments)",
                                new SortEntryDesc(osmGroup.Elements.Count)
                            )
                        );
                    }

                    // Try to match to LVM roads
                
                    if (IsWayForLVM(osmGroup, out int lvmMatchCount))
                    {
                        if (lvmMatchCount < osmGroup.Elements.Count)
                        {
                            report.AddEntry(
                                ReportGroup.LVMRoads,
                                new IssueReportEntry(
                                    "Ways partially match LVM roads for \"" + wayName + "\" on " + lvmMatchCount + "/" + osmGroup.Elements.Count + " road (segments)",
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
                    
                    // Nothing matched anything, so unknown
                    
                    report.AddEntry(
                        ReportGroup.UnknownSuffixes,
                        new IssueReportEntry(
                            "Unknown name \"" + wayName + "\" on " + osmGroup.Elements.Count + " road (segments)",
                            new SortEntryDesc(osmGroup.Elements.Count)
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

            if (cleanlyMatchedRouteNames.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.RouteNames,
                    new GenericReportEntry(
                        "These " + cleanlyMatchedRouteNames.Count + " names fully matched regional route roads: " +
                        string.Join(", ", cleanlyMatchedRouteNames.Select(n => "\"" + n.n + "\" for \"" + n.r + "\""))
                    )
                );
            }

            if (lvmFullyMatchedNames.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.LVMRoads,
                    new GenericReportEntry(
                        "These " + lvmFullyMatchedNames.Count + " names fully matched for LVM roads: " +
                        string.Join(", ", lvmFullyMatchedNames.Select(n => "\"" + n.n + "\" x " + n.c + ""))
                    )
                );
            }


            [Pure]
            static KnownSuffix? GetKnownSuffixForName(string name, List<KnownSuffix> suffixes)
            {
                foreach (KnownSuffix knownSuffix in suffixes)
                {
                    if (name.ToLower().EndsWith(knownSuffix.Suffix)) // space not required
                    {
                        return knownSuffix;
                    }
                }

                return null;
            }

            [Pure]
            static RouteNameMatch IsWayNamedAfterMajorRouteName(string wayName, OsmDataExtract namedRoutes, out string? routeRef, out string? routeName)
            {
                OsmElement? foundRoute = namedRoutes.Elements.FirstOrDefault(e => IsNameMatch(e.GetValue("name")!, wayName, out bool _));

                if (foundRoute != null)
                {
                    routeName = foundRoute.GetValue("name")!;
                    routeRef = foundRoute.GetValue("ref"); 

                    // Check again, but get extra info
                    IsNameMatch(routeName, wayName, out bool cleanMatch);
                    
                    return cleanMatch ? RouteNameMatch.Yes : RouteNameMatch.Partial;
                }

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
                        return name
                            .Replace("—", "-") // mdash
                            .Replace("–", "-") // ndash
                            .Replace(" - ", "-")
                            .Replace("- ", "-")
                            .Replace(" -", "-")
                            ;
                    }
                }
            }

            [Pure]
            static bool IsWayForLVM(OsmGroup osmGroup, out int count)
            {
                count = osmGroup.Elements.Count(e => e.GetValue("operator") == "Latvijas valsts meži");
                return count > 1;
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
            Yes,
            Partial,
            No
        }

        private enum ReportGroup
        {
            UnknownSuffixes,
            KnownSuffixes,
            RouteNames,
            LVMRoads
        }
    }
}