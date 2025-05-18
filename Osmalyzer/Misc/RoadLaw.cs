using System.Web;
using HtmlAgilityPack;

namespace Osmalyzer;

public class RoadLaw
{
    public readonly List<Road> roads;

    public readonly Dictionary<string, List<string>> sharedSegments;


    public RoadLaw(string dataFileName)
    {
        HtmlDocument doc = new HtmlDocument();
        doc.Load(dataFileName, Encoding.UTF8);

        HtmlNodeCollection? rows = doc.DocumentNode.SelectNodes(".//tr[contains(@class,'tv_html')]");
        if (rows == null) throw new Exception();
        if (rows.Count == 0) throw new Exception();

        roads = [ ];

        sharedSegments = new Dictionary<string, List<string>>();

        foreach (HtmlNode row in rows)
        {
            if (row.InnerText == "&nbsp;")
                continue; // empty/spacer row
            
            HtmlNodeCollection? cells = row.SelectNodes(".//td");
            if (cells == null) throw new Exception();
            if (cells.Count == 0) throw new Exception();

            string firstCellInnerText = cells[0].InnerText.Trim();
            
            if (firstCellInnerText.Contains("Indekss") || // header, first row
                firstCellInnerText.Contains("kopgarums") || // header, second row (of merged stuff)
                firstCellInnerText.Contains("posmi ārpus pilsētām") || // header, third row (of merged stuff)
                firstCellInnerText == "1") // column index row
                continue;

            List<HtmlNode> cellList = cells.ToList();

            const int columnCountVMain = 11; // V
            const int columnCountVSubsequent = columnCountVMain - 1; // -1 as road name gets merged down across rows
            const int columnCountAPMain = 14; // A and P
            const int columnCountAPSubsequent = columnCountAPMain - 1; // road name gets merged down across rows
            
            bool subsequent = cells.Count is columnCountAPSubsequent or columnCountVSubsequent;
            // i.e., the first column of the row is merged with the "primary" row where the road code is, which is shared for all these subsequent rows
            
            if (!subsequent)
            {
                // First row for road
            
                const string codePattern = @"^[AVP][1-9][0-9]{0,3}$";

                if (!Regex.IsMatch(firstCellInnerText, codePattern)) // only rows we expect are for road entries
                    throw new Exception();

                string code = firstCellInnerText;

                string name = cellList[1].InnerText.Trim();

                double length = GetLengthFromNode(cellList[2]);

                //Console.WriteLine(code + " - " + length);

                roads.Add(new Road(code, name, length));

                GatherNotes(code, cellList[10], sharedSegments);
            }
            else
            {
                // Subsequent row for road - distances and notes

                GatherNotes(roads.Last().Code, cellList[10 - 1], sharedSegments);
            }
        }
        
        if (roads.Count == 0)
            throw new Exception("Failed to find any roads");

        // foreach (KeyValuePair<string,List<string>> segment in sharedSegments)
        //     Console.WriteLine(segment.Key + " shared with " + string.Join(", ", segment.Value));
    }

        
    private static void GatherNotes(string code, HtmlNode node, Dictionary<string, List<string>> sharedSegments)
    {
        string? notes = GetNotesFromNode(node);

        if (notes == null)
            return;
        
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
                         trimmedEntry == "Tilts") // e.g. "Kurzemes iela, Tilts, Neretas iela"
                {
                    //string streetName = trimmedEntry;
                }
                else if (Regex.IsMatch(trimmedEntry, doubleNameMatchPattern))
                {
                    //Match match = Regex.Match(trimmedEntry, doubleNameMatchPattern);
                    //string streetName = match.Groups[1].ToString();
                    //string altStreetName = match.Groups[2].ToString();
                }
                else if (trimmedEntry.Contains("īpašnieks")) // e.g. "posma 77,4.–77,9. km īpašnieks – akciju sabiedrība "Latvenergo""
                {
                    // don't care
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        return;


        void AppendSharedCode(string c)
        {
            if (!sharedSegments.ContainsKey(code))
                sharedSegments.Add(code, [ c ]);
            else if (!sharedSegments[code].Contains(c))
                sharedSegments[code].Add(c);
        }
    }

    private static double GetLengthFromNode(HtmlNode cell)
    {
        string lengthString;

        HtmlNode? paraNode = cell.SelectSingleNode(".//p");

        if (paraNode != null)
            lengthString = paraNode.InnerText;
        else
            lengthString = cell.InnerText;

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


public class Road
{
    public string Code { get; }
    public string Name { get; }
    public double Length { get; }

    
    public Road(string code, string name, double length)
    {
        Code = code;
        Name = name;
        Length = length;
    }
}