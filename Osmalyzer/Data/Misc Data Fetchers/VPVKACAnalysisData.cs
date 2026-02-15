using System.Web;

namespace Osmalyzer;

[UsedImplicitly]
public class VPVKACAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "VPVKAC";

    public override string ReportWebLink => @"https://www.pakalpojumucentri.lv/vpvkac";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "vpvkac";


    public List<VPVKACOffice> Offices { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.pakalpojumucentri.lv/vpvkac", 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".html")
        );
    }

    protected override void DoPrepare()
    {
        Offices = [ ];

        string text = File.ReadAllText(Path.Combine(CacheBasePath, DataFileIdentifier + @".html"));
        
        /*
                                                                    <tr>
                                        <td>
                                            <span style="color: #999999;">95)</span>

                                            <a href="https://www.pakalpojumucentri.lv/vpvkac/grobinas-novads"><strong>
                                                        Dienvidkurzemes novada Grobiņas pilsētas VPVKAC
                                                    </strong></a>
                                                <span style="color: #999999;"><br />Novadu nozīmes attīstības centrs</span>

                                            
                                                <br /><br />
                                            <a href="https://www.pakalpojumucentri.lv/vpvkac/grobinas-novads">Lielā iela 54<br /> Grobiņas pilsēta<br /> Dienvidkurzemes nov.<br /> LV-3430</a>
                                        </td>
                                        <td class="text-left">Pirmdiena: 8:30 - 12:00; 12:20 - 17:00<br />
Otrdiena: 8:30 - 12:00; 12:30 - 19:00<br />
Trešdiena: 8:30 - 12:00; 12:20 - 17:00<br />
Ceturtdiena: 8:30 - 12:00; 12:20 - 17:00<br />
Piektdiena: 8:30 - 14:00</td>
                                        <td class="text-right">grobina@pakalpojumucentri.lv<br /><i class="fa fa-phone" aria-hidden="true"></i>&nbsp;&nbsp;66954818</td>
                                                                                                                    </tr>
                            
        */            

        MatchCollection rowMatches = Regex.Matches(text, @"<tr>(.*?)<\/tr>", RegexOptions.Singleline);

        if (rowMatches.Count == 0) throw new Exception();

        foreach (Match rowMatch in rowMatches)
        {
            string rowText = rowMatch.Groups[1].ToString();
            
            Match nameMatch = Regex.Match(rowText, @"<strong>([^<]+?)<\/strong>");
            if (!nameMatch.Success)
                continue;
            
            if (rowText.Contains("Tiks atvērts"))
                continue; // this is a future office

            string name = nameMatch.Groups[1].ToString().Trim();
            // e.g. "Dienvidkurzemes novada Grobiņas pilsētas VPVKAC"

            name = HttpUtility.HtmlDecode(name); // e.g. "Aug&scaron;daugavas novada Vi&scaron;ķu pagasta VPVKAC"

            // Special case for missing "pilsētas" in name - all others have it (or "pagasta")
            if (name == "Jelgavas novada Jelgavas VPVKAC")
                name = "Jelgavas novada Jelgavas pilsētas VPVKAC";

            string shortName = GetShortName(name);
            string disambiguatedName = GetDisambiguatedName(name);

            MatchCollection addressMatches = Regex.Matches(rowText, @"<a href=""[^""]+"">(.*?)</a>", RegexOptions.Singleline);
            // This will match both <a> - name and address
            if (addressMatches.Count != 2) throw new Exception();

            string rawAddress = addressMatches[1].Groups[1].ToString().Trim();
            if (rawAddress == "") 
                continue; // address not (yet) specified, ignoring entry

            CleanAddress(
                rawAddress, 
                out string cleanedAddress, 
                out string originalAddress
            );
            
            Match openingHoursMatch = Regex.Match(rowText, @"<td class=""text-left"">(.+?)<\/td>", RegexOptions.Singleline);
            if (!openingHoursMatch.Success) throw new Exception();
            
            string openingHours = openingHoursMatch.Groups[1].ToString().Trim();

            openingHours = OpeningHoursToOsmSyntax(openingHours);
            
            Match emailMatch = Regex.Match(rowText, @"<td class=""text-right"">([^<]+?)<br");
            if (!emailMatch.Success) throw new Exception();
            
            string email = emailMatch.Groups[1].ToString().Trim();
            
            email = EmailToOsmSyntax(email);
            
            Match phoneMatch = Regex.Match(rowText, @"</i>([^<]+?)</td>");
            if (!phoneMatch.Success) throw new Exception();
            
            string phone = phoneMatch.Groups[1].ToString().Trim();
            
            phone = PhonesToOsmSyntax(phone);
            
            Offices.Add(
                new VPVKACOffice(
                    name,
                    shortName,
                    disambiguatedName,
                    cleanedAddress,
                    email,
                    phone,
                    openingHours,
                    originalAddress != cleanedAddress ? originalAddress : null
                )
            );
        }
        
        if (Offices.Count == 0) throw new Exception();
        
        // Mark ambiguous offices that share the same short parsed name, i.e. we will need to use the disambiguated name instead
        foreach (IGrouping<string, VPVKACOffice> group in Offices.GroupBy(o => o.ShortName))
            if (group.Count() > 1)
                foreach (VPVKACOffice office in group)
                    office.MarkAmbiguous();
    }


    [Pure]
    private static string GetShortName(string officeName)
    {
        // "Cēsu novada Vecpiebalgas pagasta VPVKAC" -> "Vecpiebalgas VPVKAC"
        // "Aizkraukles novada Jaunjelgavas pilsētas VPVKAC" -> "Jaunjelgavas VPVKAC"
        
        string result = Regex.Replace(
            officeName,
            @"^(?:.+?) novada (.+?) (?:pilsētas) VPVKAC",
            @"$1 VPVKAC"
        );

        result = Regex.Replace(
            result,
            @"^(?:.+?) novada (.+?) (?:pagasta) VPVKAC",
            @"$1 VPVKAC"
        );

        return result;
    }

    [Pure]
    private static string GetDisambiguatedName(string officeName)
    {
        // "Cēsu novada Vecpiebalgas pagasta VPVKAC" -> "Vecpiebalgas pagasta VPVKAC"
        // "Aizkraukles novada Jaunjelgavas pilsētas VPVKAC" -> "Jaunjelgavas pilsētas VPVKAC"
        
        Match m = Regex.Match(officeName, @"^(?:.+?) novada (.+? (?:pilsētas|pagasta)) VPVKAC$");
        
        if (m.Success)
            return m.Groups[1].Value + " VPVKAC";
        
        return officeName;
    }


    [Pure]
    private static string PhonesToOsmSyntax(string phone)
    {
        phone = phone.Replace("&nbsp;", " ");
        
        string[] parts = phone.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].Replace(" ", ""); // remove spaces
            
            if (!Regex.IsMatch(parts[i], @"^\d+")) throw new Exception();
        }
        
        return string.Join(";", parts);
    }

    [Pure]
    private static string EmailToOsmSyntax(string email)
    {
        if (!Regex.IsMatch(email, @"^[a-zA-Z\.\@\d]+")) throw new Exception();
        
        if (!email.EndsWith(".lv")) throw new Exception(); // they all seem to be @pakalpojumucentri.lv

        return email;
    }

    [Pure]
    private static string OpeningHoursToOsmSyntax(string raw)
    {
        string[] rawParts = raw.Split("<br />", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (rawParts[0].Contains("Ziemas"))
        {
            if (rawParts[1].Contains("31.maijs")) {
                // Ziemas darba laiks<br />
                // (1.oktobris - 31.maijs)<br />
                // Pirmdiena: Slēgts<br />
                // ...
                // Sestdiena: 9:00 - 15:00<br />
                // <br />
                // Vasaras darba laiks <br />
                // (1.jūnijs - 30.septembris) <br />
                // Pirmdiena: 8:00 - 12:00; 12:30 - 17:00<br />
                // ...
                // Piektdiena: 9:00 - 15:00<br />
                // Sestdiena: Slēgts

                if (rawParts[1] != "(1.oktobris - 31.maijs)") throw new Exception();
                
                int summerIndex = rawParts.ToList().FindIndex(part => part.Contains("Vasaras"));

                if (rawParts[summerIndex + 1] != "(1.jūnijs - 30.septembris)") throw new Exception();

                string[] winterParts = rawParts[2..summerIndex].ToArray();
                string[] summerParts = rawParts[(summerIndex + 2)..].ToArray();

                return "Jun 1-Sep 30 " + Clean(summerParts) + "; Oct 1-May 31 " + Clean(winterParts);
            }
            else if (rawParts[1].Contains("30.aprīlis")) {
                // Ziemas darba laiks<br />
                // (1.oktobris - 30.aprīlis)<br />
                // Pirmdiena: Slēgts<br />
                // ...
                // Sestdiena: 9:00 - 15:00<br />
                // <br />
                // Vasaras darba laiks <br />
                // (1.maijs - 30.septembris) <br />
                // Pirmdiena: 8:00 - 12:00; 12:30 - 17:00<br />
                // ...
                // Piektdiena: 9:00 - 15:00<br />
                // Sestdiena: Slēgts

                if (rawParts[1] != "(1.oktobris - 30.aprīlis)") throw new Exception();
                
                int summerIndex = rawParts.ToList().FindIndex(part => part.Contains("Vasaras"));

                // Both 1.maijs and 1.maija are present
                if (!Regex.IsMatch(rawParts[summerIndex + 1], @"(1\.maij. - 30\.septembris)")) 
                    throw new Exception();

                string[] winterParts = rawParts[2..summerIndex].ToArray();
                string[] summerParts = rawParts[(summerIndex + 2)..].ToArray();

                return "May 1-Sep 30 " + Clean(summerParts) + "; Oct 1-Apr 30 " + Clean(winterParts);
            }
            else throw new Exception();
        }
        else
        {
            return Clean(rawParts);
        }

        
        string Clean(string[] parts)
        {
            List<string> cleaned = [ ];
            string? extraLast = null;

            foreach (string partRaw in parts)
            {
                // e.g. "Pirmdiena: 08:00 - 12:00; 12:30 - 18:00"
                // e.g. "Otrdiena: 8:00 - 13:00 ; 15:00-17:30"
                // e.g. "Ceturtdiena: 8:00 – 17.00"
                // e.g. "Otrdiena: 8:00 – 17:00"
                // e.g. "Ceturtdiena: 8:00 -12:15; 13:00 - 17:00"
                // e.g. "Piektdiena 8:00 - 12:00; 12:30 - 15:30"
                // e.g. "Sestdiena: 10:00 - 14:00 (no septembra līdz maijam)"
                // e.g. "Piektdiena: Slēgts"
                // e.g. "Piektdiena: slēgts"
                // e.g. "Otrdiena: Slēgts (atrodas Rugāju pagasta pārvaldē)"
                // e.g. "Otrdiena: Slēgts (apkalpošana ārpus telpām)"
                // e.g. "Katra mēneša otrā trešdiena - metodiskā diena"

                // Ignore case, it is inconsistent and doesn't matter for meaning
                string part = partRaw.ToLowerInvariant(); 
                
                // Skip closed days - OSM doesn't list closed, but open
                if (part.Contains("slēgts"))
                    continue;
                
                // Skip closed/by appointment days, not parsing this
                // "Apkalpošana ārpus bibliotēkas"
                if (part.Contains("ārpus"))
                    continue;
                // todo: we can theoretically parse as "by appointment"

                // Monthly off day "metodiskā diena"
                // Free text stuff like "Katra mēneša pēdējā piektdiena" or "Katra mēneša otrā trešdiena - metodiskā diena"
                
                (string period, string osm)[] periods =
                [
                    ("pirmā", "1"),
                    ("otrā", "2"),
                    ("trešā", "3"),
                    ("ceturktā", "4"),
                    ("pēdējā", "-1"),
                    ("priekšpēdējā", "-2")
                ];

                (string day, string osm)[] weekdays =
                [
                    ("pirmdiena", "Mo"),
                    ("otrdiena", "Tu"),
                    ("trešdiena", "We"),
                    ("ceturtdiena", "Th"),
                    ("piektdiena", "Fr"),
                    ("sestdiena", "Sa"),
                    ("svētdiena", "Su")
                ];

                bool handledMonthly = false;

                if (part.Contains("metodiskā", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach ((string periodText, string periodOsm) in periods)
                    {
                        foreach ((string dayText, string dayOsm) in weekdays)
                        {
                            if (part.Contains($"{periodText} {dayText}", StringComparison.InvariantCultureIgnoreCase))
                            {
                                extraLast = $"{dayOsm}[{periodOsm}] off"; // e.g. "We[2] off" or "Fr[-1] off"
                                handledMonthly = true;
                                break;
                            }
                        }
                        if (handledMonthly)
                            break;
                    }
                }

                if (handledMonthly)
                    continue;

                // Special case with applicable month suffix to day
                // (not seen other ranges, so not doing generic for now)
                bool sepToMay = false;
                string sepToMayPrefix = " (no septembra līdz maijam)";
                if (part.EndsWith(sepToMayPrefix))
                {
                    part = part[..^sepToMayPrefix.Length].Trim();
                    sepToMay = true;
                }

                // Replace tabs with spaces, since sometimes tabs are used
                part = part.Replace("\t", " ");

                // Replace en-dash with dash, since en-dash and dash are both used
                part = part.Replace("–", "-");

                // Replace multiple spaces with a single space
                part = Regex.Replace(part, @"\s{2,}", " ");

                // Remove spaces around dashes
                part = Regex.Replace(part, @"\s*-\s*", "-");

                // Enforce one space after day separator
                part = Regex.Replace(part, @"(?<=[a]):\s*", ": ");

                // Enforce no space before time separator and one after; semicolon to comma
                part = Regex.Replace(part, @"\s*;\s*", ", ");

                // Drop redundant time separator in the end
                part = Regex.Replace(part, @", $", "");

                // Replace LV words (and colon suffix) with OSM weekday names
                part = part
                        .Replace("pirmdiena:", "Mo").Replace("pirmdiena", "Mo")
                        .Replace("otrdiena:", "Tu").Replace("otrdiena", "Tu")
                        .Replace("trešdiena:", "We").Replace("trešdiena", "We")
                        .Replace("ceturtdiena:", "Th").Replace("ceturtdiena", "Th")
                        .Replace("piektdiena:", "Fr").Replace("piektdiena", "Fr")
                        .Replace("sestdiena:", "Sa").Replace("sestdiena", "Sa")
                        .Replace("svētdiena:", "Su").Replace("svētdiena", "Su");

                // Replace dot in hour separator with a colon
                part = part.Replace(".", ":");
                
                // Enforce two-digit hours
                part = Regex.Replace(part, @" (\d):", @" 0$1:");
                
                // Empty times probably imply closed
                if (Regex.IsMatch(part, @"^(Mo|Tu|We|Th|Fr|Sa|Su)(-|\s)$"))
                    continue;
                
                // At this point, we should have a valid OSM opening hours syntax, so check it
                if (!Regex.IsMatch(part, @"^(Mo|Tu|We|Th|Fr|Sa|Su) \d\d:\d\d-\d\d:\d\d(, \d\d:\d\d-\d\d:\d\d)?$"))
                    throw new Exception(); // todo: better than this? report entry as unparsed?

                if (sepToMay)
                    part = "Sep-May " + part;
                
                // At this point, part has been transformed into OSM opening hours syntax (or we bailed or exceptioned)
                cleaned.Add(part);
            }

            if (extraLast != null)
                cleaned.Add(extraLast);

            // Merge sequential days with same times into day ranges instead
            List<string> merged = OsmOpeningHoursHelper.MergeSequentialWeekdaysWithSameTimes(cleaned);
            
            return string.Join("; ", merged);
        }
    }


    [Pure]
    private static void CleanAddress(
        string raw,
        out string cleaned,
        out string original)
    {
        // e.g. "Lielā iela 54<br /> Grobiņas pilsēta<br /> Dienvidkurzemes nov.<br /> LV-3430"
        // we want "Lielā iela 54, Grobiņas, Dienvidkurzemes nov., LV-3430"
        
        string decoded = HttpUtility.HtmlDecode(raw);

        // First pass: non-broken, unambiguous normalization (not counted as errors)
        string normalized = decoded;
        normalized = normalized.Replace("<br />", ", ");
        normalized = normalized.Replace('“', '"').Replace('”', '"');
        normalized = normalized.Replace("  ", " "); // double spaces
        normalized = normalized.Replace("\t", " ");
        normalized = Regex.Replace(normalized, "\\s{2,}", " ");
        normalized = normalized.Replace("ielā", "iela");
        normalized = normalized.Replace("pag.", "pagasts");
        normalized = normalized.Replace("nov.", "novads");

        // Apply systematic city wording normalization (not considered a typo)
        // todo: let parser handle this instead?
        if (normalized.Contains("pilsēta"))
            normalized = normalized
               .Replace("s pilsēta", "")
               .Replace("u pilsēta", "i")
               .Replace("Pļaviņu pilsēta", "Pļaviņas")
               .Replace("Cēsu pilsēta", "Cēsis");

        string fixedAddress = normalized;

        // Hard-coded case - unknown meaning of "10"; use locality name only
        if (fixedAddress.Contains("\"Tērces-10\"")) fixedAddress = fixedAddress.Replace("\"Tērces-10\"", "\"Tērces\"");

        // Hard-coded case - address is "Tautas nams Misā" but common name is "Misas tautas nams"
        if (fixedAddress.Contains("Tautas nams Misā")) fixedAddress = fixedAddress.Replace("Tautas nams Misā", "Misas tautas nams");

        // Unique typos
        if (fixedAddress.Contains("Somersetas")) fixedAddress = fixedAddress.Replace("Somersetas", "Somersētas");
        if (fixedAddress.Contains("Skola iela")) fixedAddress = fixedAddress.Replace("Skola iela", "Skolas iela");
        if (fixedAddress.Contains("Brinģenes")) fixedAddress = fixedAddress.Replace("Brinģenes", "Briģenes");
        if (fixedAddress.Contains("Liela")) fixedAddress = fixedAddress.Replace("Liela", "Lielā");

        // Cleanup spacing again in case replacements introduced duplicates
        fixedAddress = Regex.Replace(fixedAddress, "\\s{2,}", " ");

        cleaned = fixedAddress.Trim();
        original = normalized.Trim();
    }
}