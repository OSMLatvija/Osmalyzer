using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class LVCRoadAnalyzer : Analyzer
    {
        public override string Name => "LVC Roads";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(RoadLawAnalysisData) };


        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load law road data

            RoadLawAnalysisData lawData = datas.OfType<RoadLawAnalysisData>().First();

            RoadLaw roadLaw = new RoadLaw(lawData.DataFileName);

            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            List<OsmDataExtract> osmDataExtracts = osmData.MasterData.Filter(
                new List<OsmFilter[]>()
                {
                    new OsmFilter[]
                    {
                        new IsWay(),
                        new HasTag("highway"),
                        new HasTag("ref"),
                        new DoesntHaveTag("aeroway"), // some old aeroways are also tagged as highways
                        new DoesntHaveTag("abandoned:aeroway"), // some old aeroways are also tagged as highways
                        new DoesntHaveTag("disused:aeroway"), // some old aeroways are also tagged as highways
                        new DoesntHaveTag("railway") // there's a few "railway=platform" and "railway=rail" with "highway=footway"
                    },
                    new OsmFilter[]
                    {
                        new SplitValuesCheck("ref", IsValidRef)
                    },
                    new OsmFilter[]
                    {
                        new IsRelation(),
                        new HasValue("type", "route"),
                        new HasValue("route", "road"),
                        new HasTag("ref"),
                        new SplitValuesCheck("ref", IsValidRef)
                    }
                }
            );

            OsmDataExtract reffedRoads = osmDataExtracts[0];
            OsmDataExtract recognizedReffedRoads = osmDataExtracts[1];
            OsmDataExtract routeRelations = osmDataExtracts[2];

            // Filter strictly to inside Latvia
            
            OsmRelation latviaRelation = (OsmRelation)osmData.MasterData.Find(
                new IsRelation(),
                new HasValue("type", "boundary"),
                new HasValue("admin_level", "2"),
                new HasValue("name", "Latvija")
            )!; // never expecting to not have this

            OsmPolygon latviaPolygon = latviaRelation.GetOuterWayPolygon();

            latviaPolygon.SaveToFile("latvia-real.poly");
            
            InsidePolygon insidePolygonFilter = new InsidePolygon(latviaPolygon, OsmPolygon.RelationInclusionCheck.Fuzzy); // somewhat expensive, so keep outside
            
            reffedRoads = reffedRoads.Filter(insidePolygonFilter);
            recognizedReffedRoads = recognizedReffedRoads.Filter(insidePolygonFilter);
            routeRelations = routeRelations.Filter(insidePolygonFilter);

            // Parse
            
            OsmGroups roadsByRef = recognizedReffedRoads.GroupByValues("ref", true);

            // Road on map but not in law

            //TODO:List<string> mappedRoadsNotFoundInLaw = new List<string>();
            List<string> mappedRoadsNotFoundInLawFormatted = new List<string>();
            bool anyStricken = false;
            bool anyHistoric = false;

            foreach (OsmGroup osmGroup in roadsByRef.groups)
            {
                //Console.WriteLine(osmGroup.Value + " x " + osmGroup.Elements.Count);

                bool foundInLaw = roadLaw.roads.OfType<ActiveRoad>().Any(r => r.Code == osmGroup.Value);

                if (!foundInLaw)
                {
                    //TODO:mappedRoadsNotFoundInLaw.Add(osmGroup.Value);

                    mappedRoadsNotFoundInLawFormatted.Add(osmGroup.Value);

                    bool foundAsStricken = roadLaw.roads.OfType<StrickenRoad>().Any(r => r.Code == osmGroup.Value);
                    if (foundAsStricken)
                    {
                        mappedRoadsNotFoundInLawFormatted[^1] += "†";
                        anyStricken = true;
                    }

                    bool foundAsHistoric = roadLaw.roads.OfType<HistoricRoad>().Any(r => r.Code == osmGroup.Value);
                    if (foundAsHistoric)
                    {
                        mappedRoadsNotFoundInLawFormatted[^1] += "‡";
                        anyHistoric = true;
                    }
                }
            }

            report.AddGroup(ReportGroup.MappedRoadsNotFoundInLaw, "These roads are on the map, but not in the law:");
            
            if (mappedRoadsNotFoundInLawFormatted.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.MappedRoadsNotFoundInLaw,
                    new Report.MainReportEntry(
                        (mappedRoadsNotFoundInLawFormatted.Count > 1 ? "Roads" : "Road") + " " +
                        string.Join(", ", mappedRoadsNotFoundInLawFormatted.OrderBy(v => v)) +
                        " " + (mappedRoadsNotFoundInLawFormatted.Count > 1 ? "are" : "is") + " on the map, but not in the law." +
                        (anyStricken ? " † Marked as stricken." : "") +
                        (anyHistoric ? " ‡ Formerly stricken." : "")
                    )
                );
            }
            else
            {
                report.AddEntry(
                    ReportGroup.MappedRoadsNotFoundInLaw, 
                    new Report.MainReportEntry("All roads on the map are present in the law.")
                );
            }

            // Road in law but not on map

            List<string> lawedRoadsNotFoundOnMap = new List<string>();

            foreach (ActiveRoad road in roadLaw.roads.OfType<ActiveRoad>())
            {
                bool foundInOsm = roadsByRef.groups.Any(g => g.Value == road.Code);

                if (!foundInOsm)
                    lawedRoadsNotFoundOnMap.Add(road.Code);
            }

            report.AddGroup(ReportGroup.LawedRoadsNotFoundOnMap, "These roads are in the law, but not on the map:");

            if (lawedRoadsNotFoundOnMap.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.LawedRoadsNotFoundOnMap,
                        new Report.MainReportEntry(
                        (lawedRoadsNotFoundOnMap.Count > 1 ? "Roads" : "Road") + " " +
                        string.Join(", ", lawedRoadsNotFoundOnMap) +
                        " " + (lawedRoadsNotFoundOnMap.Count > 1 ? "are" : "is") + " in the law, but not on the map."
                    )
                );
            }
            else
            {
                report.AddEntry(
                    ReportGroup.LawedRoadsNotFoundOnMap, 
                    new Report.MainReportEntry("All roads in the law are present on the map.")
                );
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
                        //report.WriteLine("Road " + entry.Key + " is supposed to share segments with " + string.Join(", ", sharingsNotFound) + " but currently doesn't.");

                        unsharedSegments.Add((entry.Key, sharingsNotFound));
                    }
                }
            }

            report.AddGroup(ReportGroup.UnsharedSegments, "These roads do not have expected overlapping segments as in the law");

            if (unsharedSegments.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.UnsharedSegments,
                    new Report.MainReportEntry(
                        (unsharedSegments.Count > 1 ? "These roads do" : "This road does") + " not have expected overlapping segments as in the law: " +
                        string.Join("; ", unsharedSegments.OrderBy(s => s.Item1).Select(s => s.Item1 + " with " + string.Join(", ", s.Item2.OrderBy(i => i)))) +
                        "."
                    )
                );
            }
            else
            {
                report.AddEntry(
                    ReportGroup.UnsharedSegments, 
                    new Report.MainReportEntry("All roads have expected shared segments as in the law.")
                );
            }

            List<(string, string, List<OsmElement>)> uniqueRefPairs = new List<(string, string, List<OsmElement>)>();

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

                            (string, string, List<OsmElement>) existing = uniqueRefPairs
                                .FirstOrDefault(r =>
                                                    (r.Item1 == refA && r.Item2 == refB) ||
                                                    (r.Item1 == refB && r.Item2 == refA));

                            if (existing.Item3 != null)
                                existing.Item3.Add(reffedRoad);
                            else
                                uniqueRefPairs.Add((refA, refB, new List<OsmElement>() { reffedRoad }));
                        }
                    }
                }
            }

            report.AddGroup(ReportGroup.SharedRefsNotInLaw, "These roads have shared ref segments that are not in the law:");
            
            //todo: for empty report.AddEntry(ReportGroup.SharedRefsNotInLaw, "There are no roads with shared refs that are not in the law.");

            for (int i = 0; i < uniqueRefPairs.Count; i++)
            {
                string refA = uniqueRefPairs[i].Item1;
                string refB = uniqueRefPairs[i].Item2;

                bool found = roadLaw.SharedSegments.Any(ss =>
                                                            ss.Key == refA && ss.Value.Contains(refB) ||
                                                            ss.Key == refB && ss.Value.Contains(refA));

                if (!found)
                {
                    List<OsmElement> roads = uniqueRefPairs[i].Item3;

                    report.AddEntry(
                        ReportGroup.SharedRefsNotInLaw,
                        new Report.MainReportEntry(
                            $"These segments share refs \"{refA}\" and \"{refB}\", but are not in the law: " +
                            (roads.Count > 5 ?
                                $" on {roads.Count} road (segments)" :
                                "on these road (segments): " + string.Join(", ", roads.Select(e => "https://www.openstreetmap.org/way/" + e.Id))) +
                            "."
                        )
                    );
                }
            }
            
            // todo: unconnected segments, i.e. gaps
            
            // todo: roads with missing name
            // todo: roads with mismatched name to law (but not valid street name)
            
            // todo: routes with missing name
            // todo: routes with mismatched name

            //

            List<string> missingRelations = new List<string>();

            List<List<OsmElement>> relationsWithSameRef = new List<List<OsmElement>>();

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
                    relationsWithSameRef.Add(elements);
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

            report.AddGroup(ReportGroup.MissingRelations, "These route relations are missing:");

            if (missingRelations.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.MissingRelations,
                    new Report.MainReportEntry(
                        (missingRelations.Count > 1 ? "These route relations are missing" : "This route relation is missing") + ": " +
                        string.Join(", ", missingRelations.OrderBy(c => c)) +
                        "."
                    )
                );
            }
            else
            {
                report.AddEntry(ReportGroup.MissingRelations, new Report.MainReportEntry("There are route relations for all mapped road codes."));
            }

            report.AddGroup(ReportGroup.ExtraRelations, "These route relations don't have a road with such code:");

            if (extraRelations.Count > 0)
            {
                report.AddEntry(
                    ReportGroup.ExtraRelations,
                    new Report.MainReportEntry(
                        (extraRelations.Count > 1 ? "These route relations don't" : "This route relation doesn't") + " have a road with such code: " +
                        string.Join(", ", extraRelations.OrderBy(c => c)) +
                        "."
                    )
                );
            }
            else
            {
                report.AddEntry(ReportGroup.ExtraRelations, new Report.MainReportEntry("There are no route relations with codes that no road uses."));
            }

            report.AddGroup(ReportGroup.RelationsWithSameRef, "These route relations have the same code:");
            report.AddEntry(ReportGroup.RelationsWithSameRef, new Report.PlaceholderReportEntry("There are no route relations that use the same ref (that is, all route refs are unique)."));

            if (relationsWithSameRef.Count > 0)
            {
                foreach (List<OsmElement> sameRefRoutes in relationsWithSameRef)
                {
                    string routeRef = sameRefRoutes.First().GetValue("ref")!;

                    report.AddEntry(
                        ReportGroup.RelationsWithSameRef,
                        new Report.MainReportEntry(
                            "These " + sameRefRoutes.Count + " route relations have the same code " + routeRef + ": " +
                            string.Join("; ", sameRefRoutes.Select(
                                            r => (r.HasKey("name") ? "\"" + r.GetValue("name") + "\"" : "unnamed") + " https://www.openstreetmap.org/relation/" + r.Id + "")
                            ) + "."
                        )
                    );
                }
            }

            // Uncrecognized ref

            OsmDataExtract unrecognizedReffedRoads = reffedRoads.Filter(
                new SplitValuesCheck("ref", s => !IsValidRef(s))
            );

            int excludedCount = unrecognizedReffedRoads.GroupByValues("ref", true).groups.Count; // real value below

            unrecognizedReffedRoads = unrecognizedReffedRoads.Filter(
                new SplitValuesCheck("ref", s => !IsExcludedRef(s))
            );

            OsmGroups unrecognizedRoadsByRef = unrecognizedReffedRoads.GroupByValues("ref", true);

            excludedCount -= unrecognizedRoadsByRef.groups.Count;

            report.AddGroup(ReportGroup.UnrecognizedRoadsByRef, "These road refs are not recognized:");

            if (unrecognizedRoadsByRef.groups.Count > 0)
            {
                foreach (OsmGroup osmGroup in unrecognizedRoadsByRef.groups)
                {
                    report.AddEntry(
                        ReportGroup.UnrecognizedRoadsByRef,
                        new Report.MainReportEntry(
                            "Road ref " +
                            "\"" + osmGroup.Value + "\" " +
                            "not recognized " +
                            (osmGroup.Elements.Count > 5 ?
                                " on " + osmGroup.Elements.Count + " road (segments)" :
                                "on these road (segments): " + string.Join(", ", osmGroup.Elements.Select(e => "https://www.openstreetmap.org/way/" + e.Id))
                            )
                        )
                    );
                }

                if (excludedCount > 0)
                {
                    report.AddEntry(
                        ReportGroup.UnrecognizedRoadsByRef,
                        new Report.MainReportEntry(excludedCount + " refs are ignored/excluded.")
                    );
                }
            }
            else
            {
                report.AddEntry(
                    ReportGroup.UnrecognizedRoadsByRef, 
                    new Report.MainReportEntry(
                        "All road refs are recognized" +
                        (excludedCount > 0 ? " and " + excludedCount + " are ignored/excluded" : "") +
                        "."
                    )
                );
            }
            
            
            // todo: missing route segments - basically relation doesn't match roads - this is going to have a TON of hits
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

            return false;
        }

        private enum ReportGroup
        {
            MappedRoadsNotFoundInLaw,
            LawedRoadsNotFoundOnMap,
            UnsharedSegments,
            SharedRefsNotInLaw,
            MissingRelations,
            ExtraRelations,
            RelationsWithSameRef,
            UnrecognizedRoadsByRef
        }
    }
}