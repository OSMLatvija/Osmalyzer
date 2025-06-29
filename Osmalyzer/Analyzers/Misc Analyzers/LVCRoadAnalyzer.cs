namespace Osmalyzer;

[UsedImplicitly]
public class LVCRoadAnalyzer : Analyzer
{
    public override string Name => "LVC Roads";

    public override string Description => "This report checks LVC route roads for issues.";

    public override AnalyzerGroup Group => AnalyzerGroup.Roads;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(RoadLawAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Get law road data

        RoadLawAnalysisData roadLawData = datas.OfType<RoadLawAnalysisData>().First();

        RoadLaw roadLaw = roadLawData.RoadLaw;

        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        List<OsmDataExtract> osmDataExtracts = osmData.MasterData.Filter(
            [
                [
                    new IsWay(),
                    new HasKey("highway"),
                    new HasKey("ref"),
                    new DoesntHaveKey("aeroway"), // some old aeroways are also tagged as highways
                    new DoesntHaveKey("abandoned:aeroway"), // some old aeroways are also tagged as highways
                    new DoesntHaveKey("disused:aeroway"), // some old aeroways are also tagged as highways
                    new DoesntHaveKey("railway") // there's a few "railway=platform" and "railway=rail" with "highway=footway"
                ],

                [
                    new IsRelation(),
                    new HasValue("type", "route"),
                    new HasValue("route", "road"),
                    new HasKey("ref"),
                    new SplitValuesCheck("ref", IsValidRef)
                ]
            ]
        );

        OsmDataExtract reffedRoads = osmDataExtracts[0];
        OsmDataExtract routeRelations = osmDataExtracts[1];

        OsmDataExtract recognizedReffedRoads = reffedRoads.Filter(
            new SplitValuesCheck("ref", IsValidRef)
        );

        // Filter strictly to inside Latvia
            
        InsidePolygon insidePolygonFilter = new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy); // somewhat expensive, so keep outside

        reffedRoads = reffedRoads.Filter(insidePolygonFilter);
        recognizedReffedRoads = recognizedReffedRoads.Filter(insidePolygonFilter);
        routeRelations = routeRelations.Filter(insidePolygonFilter);

        // Parse
            
        OsmGroups roadsByRef = recognizedReffedRoads.GroupByValues("ref", true);

        // Road on map but not in law

        report.AddGroup(
            ReportGroup.MappedRoadsNotFoundInLaw, 
            "Roads on the map, but not in the law", 
            "There shouldn't be any roads with LVC route codes on the map if they aren't in the law. It is possible the law hasn't yet been updated for newly-assigned codes.", 
            "All roads on the map are present in the law."
        );

        foreach (OsmGroup osmGroup in roadsByRef.groups)
        {
            bool foundInLaw = roadLaw.roads.Any(r => r.Code == osmGroup.Value);

            if (!foundInLaw)
            {
                report.AddEntry(
                    ReportGroup.MappedRoadsNotFoundInLaw,
                    new IssueReportEntry(
                        "The OSM road `" + osmGroup.Value + "` is on the map, but not in the law" + 
                        " - " + ReportEntryFormattingHelper.ListElements(osmGroup.Elements)
                    )
                );
            }
        }

        // Road in law but not on map

        List<string> lawedRoadsNotFoundOnMap = [ ];

        foreach (Road road in roadLaw.roads.OfType<Road>())
        {
            bool foundInOsm = roadsByRef.groups.Any(g => g.Value == road.Code);

            if (!foundInOsm)
                lawedRoadsNotFoundOnMap.Add(road.Code);
        }

        report.AddGroup(ReportGroup.LawedRoadsNotFoundOnMap, "Roads in the law, but not on the map", null, "All roads in the law are present on the map.");

        if (lawedRoadsNotFoundOnMap.Count > 0)
        {
            // TODO: INDIVIDUAL
                
            report.AddEntry(
                ReportGroup.LawedRoadsNotFoundOnMap,
                new IssueReportEntry(
                    (lawedRoadsNotFoundOnMap.Count > 1 ? "Roads" : "Road") + " " +
                    string.Join(", ", lawedRoadsNotFoundOnMap) +
                    " " + (lawedRoadsNotFoundOnMap.Count > 1 ? "are" : "is") + " in the law, but not on the map."
                )
            );
        }

        // Check shared segments

        List<(string, List<string>)> unsharedSegments = [ ];

        foreach (KeyValuePair<string, List<string>> entry in roadLaw.sharedSegments)
        {
            List<OsmElement> matchingRoads = recognizedReffedRoads.Elements.Where(e => TagUtils.SplitValue(e.GetValue("ref")!).Contains(entry.Key)).ToList();

            if (matchingRoads.Count > 0)
            {
                List<string> sharingsNotFound = [ ];

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

        report.AddGroup(ReportGroup.UnsharedSegments, "Roads without expected overlapping segments as in the law", null, "All roads have expected shared segments as in the law.");

        if (unsharedSegments.Count > 0)
        {
            // TODO: INDIVIDUAL

            report.AddEntry(
                ReportGroup.UnsharedSegments,
                new IssueReportEntry(
                    (unsharedSegments.Count > 1 ? "These roads do" : "This road does") + " not have expected overlapping segments as in the law: " +
                    string.Join("; ", unsharedSegments.OrderBy(s => s.Item1).Select(s => s.Item1 + " with " + string.Join(", ", s.Item2.OrderBy(i => i)))) +
                    "."
                )
            );
        }

        List<(string, string, List<OsmElement>)> uniqueRefPairs = [ ];

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
                            uniqueRefPairs.Add((refA, refB, [ reffedRoad ]));
                    }
                }
            }
        }

        report.AddGroup(ReportGroup.SharedRefsNotInLaw, "Roads with shared ref segments not in the law", "The law has a list of routes that overlap, but this list is not accurate, especially for minor connections like viaducts. Roundabouts are ignored.", "There are no roads with shared refs that are not in the law.");

        List<(string, string)> roundaboutOnlyShared = [ ];
            
        for (int i = 0; i < uniqueRefPairs.Count; i++)
        {
            string refA = uniqueRefPairs[i].Item1;
            string refB = uniqueRefPairs[i].Item2;

            bool found = roadLaw.sharedSegments.Any(ss =>
                                                        ss.Key == refA && ss.Value.Contains(refB) ||
                                                        ss.Key == refB && ss.Value.Contains(refA));

            if (!found)
            {
                List<OsmElement> roads = uniqueRefPairs[i].Item3;

                OsmCoord coord = OsmGeoTools.GetAverageCoord(roads);

                string text = $"Segments share refs \"{refA}\" and \"{refB}\", but are not in the law on {roads.Count} road (segments) - " +
                              ReportEntryFormattingHelper.ListElements(roads);

                bool onlyRoundabout = roads.All(r => r.HasValue("junction", "roundabout"));

                if (!onlyRoundabout)
                {
                    report.AddEntry(
                        ReportGroup.SharedRefsNotInLaw,
                        new IssueReportEntry(
                            text,
                            coord,
                            MapPointStyle.Problem
                        )
                    );
                }
                else
                {
                    roundaboutOnlyShared.Add((refA, refB));
                }
            }
        }

        if (roundaboutOnlyShared.Count > 0)
        {
            report.AddEntry(
                ReportGroup.SharedRefsNotInLaw,
                new GenericReportEntry("These segments share refs that are not in the law, but are ignored because they are roundabouts, which the law doesn't list as shared - " +
                                       string.Join(", ", roundaboutOnlyShared.Select(s => "`" + s.Item1 + "` + `" + s.Item2 + "`")))
            );
        }
            
        // todo: unconnected segments, i.e. gaps
            
        // todo: roads with missing name
        // todo: roads with mismatched name to law (but not valid street name)
            
        // todo: routes with missing name
        // todo: routes with mismatched name

        //

        List<string> missingRelations = [ ];

        List<List<OsmElement>> relationsWithSameRef = [ ];

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

        report.AddGroup(ReportGroup.ExtraRelations, "Route relations without a road matching code", null, "There are no route relations with codes that no road uses.");

        foreach (OsmElement routeElement in routeRelations.Elements)
        {
            string code = routeElement.GetValue("ref")!;

            bool haveRoad = roadsByRef.groups.Any(g => g.Value == code);

            if (!haveRoad)
            {
                report.AddEntry(
                    ReportGroup.ExtraRelations,
                    new IssueReportEntry(
                        "The route relation `" + code + "` doesn't have a any road segment with such code - " + routeElement.OsmViewUrl,
                        routeElement.GetAverageCoord(),
                        MapPointStyle.Problem
                    )
                );
            }
                
            // todo: ROUTE MEMEBRS TO ALL HAVE SEGMENTS WITH REF and no other road to have ref without route parent
        }

        report.AddGroup(ReportGroup.MissingRelations, "Missing route relations", null, "There are route relations for all mapped road codes.");

        if (missingRelations.Count > 0)
        {
            report.AddEntry(
                ReportGroup.MissingRelations,
                new IssueReportEntry(
                    (missingRelations.Count > 1 ? "These route relations are missing" : "This route relation is missing") + ": " +
                    string.Join(", ", missingRelations.OrderBy(c => c)) +
                    "."
                )
            );
        }

        report.AddGroup(ReportGroup.RelationsWithSameRef, "Route relations with the same code", null, "There are no route relations that use the same ref (that is, all route refs are unique).");

        if (relationsWithSameRef.Count > 0)
        {
            foreach (List<OsmElement> sameRefRoutes in relationsWithSameRef)
            {
                string routeRef = sameRefRoutes.First().GetValue("ref")!;

                report.AddEntry(
                    ReportGroup.RelationsWithSameRef,
                    new IssueReportEntry(
                        "These " + sameRefRoutes.Count + " route relations have the same code " + routeRef + ": " +
                        string.Join("; ", sameRefRoutes.Select(
                                        r => (r.HasKey("name") ? "\"" + r.GetValue("name") + "\"" : "unnamed") + " " + r.OsmViewUrl + "")
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

        report.AddGroup(ReportGroup.UnrecognizedRoadsByRef, "Unrecognized road refs", null, "All road refs are recognized" + (excludedCount > 0 ? " and " + excludedCount + " are ignored/excluded" : "") + ".");

        if (unrecognizedRoadsByRef.groups.Count > 0)
        {
            foreach (OsmGroup osmGroup in unrecognizedRoadsByRef.groups)
            {
                OsmCoord coord = osmGroup.GetAverageElementCoord();

                report.AddEntry(
                    ReportGroup.UnrecognizedRoadsByRef,
                    new IssueReportEntry(
                        "Road ref " +
                        "\"" + osmGroup.Value + "\" " +
                        "not recognized on " + osmGroup.Elements.Count + " road (segments) - " +
                        ReportEntryFormattingHelper.ListElements(osmGroup.Elements),
                        coord,
                        MapPointStyle.Problem
                    )
                );
            }

            if (excludedCount > 0)
            {
                report.AddEntry(
                    ReportGroup.UnrecognizedRoadsByRef,
                    new GenericReportEntry(excludedCount + " refs are ignored/excluded as coming from other sources.")
                );
            }
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