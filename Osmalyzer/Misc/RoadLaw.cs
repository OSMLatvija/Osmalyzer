﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace Osmalyzer;

public class RoadLaw
{
    public List<Road> roads;

    public Dictionary<string, List<string>> SharedSegments { get; }


    public RoadLaw(string dataFileName)
    {
        const string codePattern = @"^[AVP][1-9][0-9]{0,3}$";

        HtmlDocument doc = new HtmlDocument();
        doc.Load(dataFileName, Encoding.UTF8);

        HtmlNodeCollection rows = doc.DocumentNode.SelectNodes(".//tr[contains(@class,'tv_html')]");
        if (rows.Count == 0) throw new Exception();

        roads = new List<Road>();

        Dictionary<string, List<string>> sharedSegments = new Dictionary<string, List<string>>();

        foreach (HtmlNode row in rows)
        {
            HtmlNodeCollection cells = row.SelectNodes(".//td");
            if (cells.Count == 0) throw new Exception();

            if (cells[0].InnerText.Contains("Autoceļa maršruta indekss"))
                continue; // header, first row

            if (cells[0].InnerText.Contains("maršruta kopgarums"))
                continue; // header, second row (of merged stuff)

            if (cells[0].InnerText.Contains("posmi ārpus pilsētām"))
                continue; // header, third row (of merged stuff)

            if (cells[0].InnerText == "no")
                continue; // header, fourth row (of merged stuff)

            if (cells[0].InnerText == "1")
                continue; // column index row
            
            switch (cells.Count)
            {
                case 10  // P and V secondary when merged
                    or 11 // P and V first or secondary when unmerged
                    or 12 // A secondary merged 
                    or 13: // A first
                {
                    // Regular

                    List<HtmlNode> cellList = cells.ToList();

                    int offset = 0;

                    bool subsequent =
                        cells.Count == 12 || // A merged
                        cells.Count == 10 || // P or V merged
                        HttpUtility.HtmlDecode(cellList[0].InnerText).Trim() == ""; // P or V unmerged empty

                    if (!subsequent)
                    {
                        // First row for road

                        string? code = GetCodeFromNode(cellList[0]);

                        if (code == null) throw new Exception();
                        
                        if (!Regex.IsMatch(code, codePattern)) throw new Exception();

                        string name = cellList[1].InnerText.Trim();

                        double length = GetLengthFromNode(cellList[2 + offset]);

                        //Console.WriteLine(code + " - " + length);

                        roads.Add(new ActiveRoad(code, name, length));

                        GatherNotes(code, cellList[10 + offset], sharedSegments);
                    }
                    else
                    {
                        // Subsequent row for road - distances and notes

                        if (cells.Count is 12 or 10) // secondary row of A, which is merged (as opposed to P and V which aren't)
                            GatherNotes(roads.Last().Code, cellList[10 - 1], sharedSegments);
                        else // P or V unmerged
                            GatherNotes(roads.Last().Code, cellList[10], sharedSegments);
                    }

                    break;
                }

                // There are no more "Svītrots" entries in the latest law revision
                // case 2:
                // {
                //     // Removed
                //
                //     List<HtmlNode> cellList = cells.ToList();
                //
                //     if (!cellList[1].SelectSingleNode(".//span").InnerText.Contains("Svītrots")) throw new Exception();
                //
                //     string code = cellList[0].SelectSingleNode(".//p").InnerText.Trim();
                //
                //     if (!Regex.IsMatch(code, codePattern)) throw new Exception();
                //
                //     roads.Add(new StrickenRoad(code));
                //
                //     break;
                // }

                default:
                    throw new Exception();
            }
        }

        SharedSegments = sharedSegments;

        // foreach (KeyValuePair<string,List<string>> segment in sharedSegments)
        //     Console.WriteLine(segment.Key + " shared with " + string.Join(", ", segment.Value));

        // Since old laws no longer change, we can just hard-code all the road numbers that were removed across the years and revisions
        // And, yes, it's only "V" roads that have changed

        // "First" (as far as OSM is concerned) list 245
        // https://likumi.lv/ta/id/77328-grozijumi-ministru-kabineta-1999-gada-6-julija-noteikumos-nr-245-noteikumi-par-valsts-autocelu-sarakstiem-

        // Stricken in changes
        // https://likumi.lv/ta/id/84045-grozijumi-ministru-kabineta-1999-gada-6-julija-noteikumos-nr-245-noteikumi-par-valsts-autocelu-sarakstiem-
        roads.Add(new HistoricRoad("V1014"));
        roads.Add(new HistoricRoad("V1438"));
        roads.Add(new HistoricRoad("V1465"));
        roads.Add(new HistoricRoad("V1466"));
        roads.Add(new HistoricRoad("V1467"));
        roads.Add(new HistoricRoad("V1469"));
        roads.Add(new HistoricRoad("V1470"));
        roads.Add(new HistoricRoad("V1471"));

        // Stricken in changes
        // https://likumi.lv/ta/id/93754-grozijumi-ministru-kabineta-1999-gada-6-julija-noteikumos-nr-245-noteikumi-par-valsts-autocelu-sarakstiem-
        roads.Add(new HistoricRoad("V263"));
        roads.Add(new HistoricRoad("V639"));
        roads.Add(new HistoricRoad("V897"));
        roads.Add(new HistoricRoad("V1483"));

        // Stricken in changes
        // https://likumi.lv/ta/id/110186-grozijumi-ministru-kabineta-1999-gada-6-julija-noteikumos-nr-245-noteikumi-par-valsts-autocelu-sarakstiem-
        roads.Add(new HistoricRoad("V29"));
        roads.Add(new HistoricRoad("V720"));
        roads.Add(new HistoricRoad("V721"));
        roads.Add(new HistoricRoad("V1189"));
        roads.Add(new HistoricRoad("V1228"));

        // Stricken in changes
        // https://likumi.lv/ta/id/141192-grozijumi-ministru-kabineta-1999-gada-6-julija-noteikumos-nr-245-noteikumi-par-valsts-autocelu-sarakstiem-
        roads.Add(new HistoricRoad("V77"));

        // New list 245 -> 809
        // https://likumi.lv/ta/id/167195-valsts-autocelu-un-valsts-autocelu-marsruta-ietverto-pasvaldibam-piederoso-autocelu-posmu-saraksts

        roads.Add(new HistoricRoad("V897"));
        roads.Add(new HistoricRoad("V77"));
        roads.Add(new HistoricRoad("V721"));
        roads.Add(new HistoricRoad("V720"));
        roads.Add(new HistoricRoad("V639"));
        roads.Add(new HistoricRoad("V367"));
        roads.Add(new HistoricRoad("V328"));
        roads.Add(new HistoricRoad("V29"));
        roads.Add(new HistoricRoad("V263"));
        roads.Add(new HistoricRoad("V1483"));
        roads.Add(new HistoricRoad("V1471"));
        roads.Add(new HistoricRoad("V1470"));
        roads.Add(new HistoricRoad("V1469"));
        roads.Add(new HistoricRoad("V1467"));
        roads.Add(new HistoricRoad("V1466"));
        roads.Add(new HistoricRoad("V1465"));
        roads.Add(new HistoricRoad("V1438"));
        roads.Add(new HistoricRoad("V1387"));
        roads.Add(new HistoricRoad("V1370"));
        roads.Add(new HistoricRoad("V136"));
        roads.Add(new HistoricRoad("V1267"));
        roads.Add(new HistoricRoad("V1228"));
        roads.Add(new HistoricRoad("V1189"));
        roads.Add(new HistoricRoad("V1133"));
        roads.Add(new HistoricRoad("V1063"));
        roads.Add(new HistoricRoad("V1014"));

        // New list 809 -> 1104
        // https://likumi.lv/ta/id/198589-noteikumi-par-valsts-autocelu-un-valsts-autocelu-marsruta-ietverto-pasvaldibam-piederoso-autocelu-posmu-sarakstiem

        roads.Add(new HistoricRoad("V901"));
        roads.Add(new HistoricRoad("V704"));
        roads.Add(new HistoricRoad("V53"));
        roads.Add(new HistoricRoad("V44"));
        roads.Add(new HistoricRoad("V41"));
        roads.Add(new HistoricRoad("V40"));
        roads.Add(new HistoricRoad("V37"));
        roads.Add(new HistoricRoad("V1320"));
        roads.Add(new HistoricRoad("V1227"));
        roads.Add(new HistoricRoad("V1187"));
        roads.Add(new HistoricRoad("V1170"));
        roads.Add(new HistoricRoad("V1132"));
        roads.Add(new HistoricRoad("V1043"));
        roads.Add(new HistoricRoad("V1042"));
        roads.Add(new HistoricRoad("V1041"));
        roads.Add(new HistoricRoad("V1040"));

        // And now we are on the current version
    }

        
    private static void GatherNotes(string code, HtmlNode node, Dictionary<string, List<string>> sharedSegments)
    {
        string? notes = GetNotesFromNode(node);

        if (notes != null)
        {
            const string matchingString = @"[Ss]akrītošais posms ar ";
            const string doubleNameMatchPattern = @"^([^(]+) \(([^)]+)\)$";
            const string segmentTwoMatchPattern = "^" + matchingString + @"([APV]\d+) un ([APV]\d+)$";
            const string segmentThreeMatchPattern = "^" + matchingString + @"([APV]\d+), ([APV]\d+) un ([APV]\d+)$";
           
            string sanitizedNotes = Regex.Replace(notes, @"(\d+),(\d+)", @"$1.$2");
            sanitizedNotes = sanitizedNotes.TrimEnd(',');

            if (Regex.IsMatch(sanitizedNotes, segmentThreeMatchPattern))
            {
                Match match = Regex.Match(sanitizedNotes, segmentThreeMatchPattern);

                string code1 = match.Groups[1].ToString();
                string code2 = match.Groups[2].ToString();
                string code3 = match.Groups[3].ToString();

                AppendSharedCode(code1);
                AppendSharedCode(code2);
                AppendSharedCode(code3);
            }
            else if (Regex.IsMatch(sanitizedNotes, segmentTwoMatchPattern))
            {
                Match match = Regex.Match(sanitizedNotes, segmentTwoMatchPattern);

                string code1 = match.Groups[1].ToString();
                string code2 = match.Groups[2].ToString();

                AppendSharedCode(code1);
                AppendSharedCode(code2);
            }
            else
            {
                string[] noteEntries = sanitizedNotes.Split(',');

                foreach (string noteEntry in noteEntries)
                {
                    string trimmedEntry = noteEntry.Trim();

                    if (Regex.IsMatch(trimmedEntry, segmentTwoMatchPattern))
                    {
                        Match match = Regex.Match(trimmedEntry, segmentTwoMatchPattern);

                        string code1 = match.Groups[1].ToString();
                        string code2 = match.Groups[2].ToString();

                        AppendSharedCode(code1);
                        AppendSharedCode(code2);
                    }
                    else if (Regex.IsMatch(trimmedEntry, @"^" + matchingString)) // starts with
                    {
                        Match match = Regex.Match(trimmedEntry, @"^" + matchingString);

                        string singleCode = trimmedEntry.Substring(match.Length);

                        AppendSharedCode(singleCode);
                    }
                    else if (FuzzyAddressMatcher.EndsWithStreetNameSuffix(trimmedEntry) ||
                             trimmedEntry == "Tilts") // hmmm
                    {
                        //string streetName = trimmedEntry;
                    }
                    else if (Regex.IsMatch(trimmedEntry, doubleNameMatchPattern))
                    {
                        //Match match = Regex.Match(trimmedEntry, doubleNameMatchPattern);
                        //string streetName = match.Groups[1].ToString();
                        //string altStreetName = match.Groups[2].ToString();
                    }
                    else if (trimmedEntry.Contains("īpašnieks"))
                    {
                        // don't care
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }


            void AppendSharedCode(string c)
            {
                if (!sharedSegments.ContainsKey(code))
                    sharedSegments.Add(code, new List<string>() { c });
                else if (!sharedSegments[code].Contains(c))
                    sharedSegments[code].Add(c);
            }
        }
    }

    private static string? GetCodeFromNode(HtmlNode cell)
    {
        string? code;

        HtmlNode paraNode = cell.SelectSingleNode(".//p");

        if (paraNode != null)
        {
            code = paraNode.InnerText;
        }
        else
        {
            code = cell.InnerText;
        }

        code = HttpUtility.HtmlDecode(code).Trim(); // can have stuff like &nbsp;

        if (code == string.Empty)
            code = null;

        return code;
    }

    private static double GetLengthFromNode(HtmlNode cell)
    {
        string lengthString;

        HtmlNode paraNode = cell.SelectSingleNode(".//p");

        if (paraNode != null)
        {
            lengthString = paraNode.InnerText;
        }
        else
        {
            lengthString = cell.InnerText;
        }

        lengthString = HttpUtility.HtmlDecode(lengthString).Trim(); // can have stuff like &nbsp;

        lengthString = lengthString.Replace(",", ".");

        return double.Parse(lengthString);
    }

    private static string? GetNotesFromNode(HtmlNode cell)
    {
        HtmlNode paraNode = cell.SelectSingleNode(".//p");

        string? notes;
        
        if (paraNode != null)
            notes = paraNode.InnerText;
        else
            notes = cell.InnerText;

        notes = HttpUtility.HtmlDecode(notes).Trim(); // can have stuff like &nbsp;

        notes = notes.Replace('\u00a0', ' '); // replace literal NBSP with space
        
        if (notes == string.Empty)
            notes = null;

        return notes;
    }
}

public abstract class Road
{
    public string Code { get; }


    protected Road(string code)
    {
        Code = code;
    }
}

public class ActiveRoad : Road
{
    public string Name { get; }

    public double Length { get; }


    public ActiveRoad(string code, string name, double length)
        : base(code)
    {
        Name = name;
        Length = length;
    }
}

public abstract class FormerRoad : Road
{
    protected FormerRoad(string code)
        : base(code)
    {
    }
}

public class StrickenRoad : FormerRoad
{
    public StrickenRoad(string code)
        : base(code)
    {
    }
}

public class HistoricRoad : FormerRoad
{
    public HistoricRoad(string code)
        : base(code)
    {
    }
}