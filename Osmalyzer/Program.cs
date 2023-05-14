using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Osmalyzer
{
    public static class Program
    {
        private const string osmDataFileName = @"latvia-latest.osm.pbf"; // from https://download.geofabrik.de/europe/latvia.html
        private const string osmDate = @"2023-05-13T20:22:00Z"; // todo: read this from the file
        
        
        public static void Main(string[] args)
        {
            //ParseLVCRoads();

            ParseCommonNames();
        }

        
        private static void ParseCommonNames()
        {
            const int nameCountThreshold = 10;

            const string reportFileName = @"Common name report.txt";
            
            using StreamWriter reportFile = File.CreateText(reportFileName);

            reportFile.WriteLine("These are the most POI names with at least " + nameCountThreshold + " occurences grouped by type (recognized by NSI):");
            
            reportFile.WriteLine("name" + "\t" + "count" + "\t" + "counts" + "\t" + "tag" + "\t" + "value");

            // Load OSM data

            OsmBlob namedElements = new OsmBlob(
                osmDataFileName,
                new IsNodeOrWay(),
                new HasTag("name")
            );

            const string nsiTagsFileName = @"NSI tags.txt"; // from https://nsi.guide/?t=brands

            string[] nsiRawTags = File.ReadAllLines(nsiTagsFileName);

            List<(string, string)> nsiTags = nsiRawTags.Select(t =>
            {
                int i = t.IndexOf('\t'); 
                return (t.Substring(0, i), t.Substring(i + 1));
            }).ToList();
            // todo: retrieve automatically from NSI repo or wherever they keep these

            List<(int count, string line)> reportEntries = new List<(int, string)>();
            
            foreach ((string nsiTag, string nsiValue) in nsiTags)
            {
                OsmBlob matchingElements = namedElements.Filter(
                    new HasValue(nsiTag, nsiValue)
                );

                OsmGroups nameGroupsSeparate = matchingElements.GroupByValues("name", false);

                OsmMultiValueGroups nameGroupsSimilar = nameGroupsSeparate.CombineBySimilarValues(
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

                foreach (OsmMultiValueGroup group in nameGroupsSimilar.groups)
                {
                    if (group.Elements.Count >= nameCountThreshold)
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
                            nsiValue;
                        
                        reportEntries.Add((group.Elements.Count, reportLine));
                    }
                }
            }

            // Each NSI tag pair has a separate name multi-value group, so we need to re-sort if we want to order by count indepenedent of POI type
            reportEntries.Sort((e1, e2) => e2.count.CompareTo(e1.count));

            foreach ((int _, string line) in reportEntries)
                reportFile.WriteLine(line);

            reportFile.WriteLine("Name values are case-insensitive, leading/trailing whitespae ignored, Latvian diacritics ignored, character '!' ignored.");
            
            reportFile.WriteLine("Name counts will repeat if the same element is tagged with multiple NSI POI types.");
            // todo: thsi may produce duplicates if the same elements has multiple NSI-compatible tag-value pairs - we might want to filter those out? or report them?
            
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
                List<OsmElement> matchingRoads = recognizedReffedRoads.Elements.Where(e => TagUtils.SplitValue(e.Element.Tags.GetValue("ref")).Contains(entry.Key)).ToList();

                if (matchingRoads.Count > 0)
                {
                    List<string> sharingsNotFound = new List<string>();

                    foreach (string shared in entry.Value)
                    {
                        bool sharingFound = false;

                        foreach (OsmElement matchingRoad in matchingRoads)
                        {
                            List<string> refs = TagUtils.SplitValue(matchingRoad.Element.Tags.GetValue("ref"));

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
                List<string> refs = TagUtils.SplitValue(reffedRoad.Element.Tags.GetValue("ref"));

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

                List<OsmElement> elements = routeRelations.Elements.Where(e => e.Element.Tags.GetValue("ref") == code).ToList();

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
                string code = routeElement.Element.Tags.GetValue("ref");

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