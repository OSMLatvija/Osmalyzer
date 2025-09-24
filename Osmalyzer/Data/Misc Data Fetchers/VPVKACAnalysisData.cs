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

            MatchCollection addressMatches = Regex.Matches(rowText, @"<a href=""[^""]+"">(.*?)</a>", RegexOptions.Singleline);
            // This will match both <a> - name and address
            if (addressMatches.Count != 2) throw new Exception();

            string rawAddress = addressMatches[1].Groups[1].ToString().Trim();
            if (rawAddress == "") 
                continue; // address not (yet) specified, ignoring entry
                
            VPVKACOffice.VPVKACAddress? address = CleanAddress(rawAddress);
            if (address == null) throw new Exception();
            
            address = AdjustAddress(address);

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
            
            Offices.Add(new VPVKACOffice(name, address, email, phone, openingHours));
        }
        
        if (Offices.Count == 0) throw new Exception();
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
        else
        {
            return Clean(rawParts);
        }

        
        string Clean(string[] parts)
        {
            List<string> cleaned = [ ];
            string? extraLast = null;

            foreach (string part in parts)
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

                // Skip closed days
                if (part.ToLower().Contains("slēgts"))
                    continue;
                
                // Skip closed/by appointment days, not parsing this
                // "Apkalpošana ārpus bibliotēkas"
                if (part.ToLower().Contains("ārpus"))
                    continue;

                // Parse a very specific case
                // "Katra mēneša otrā trešdiena - metodiskā diena"
                if (part.ToLower().Contains("katra mēneša otrā trešdiena - metodiskā diena"))
                {
                    extraLast = "We[2] off";
                    continue;
                }

                // Parse a very specific case
                // "*katra mēneša pēdējā trešdiena – metodiskā diena, bibliotēka lasītājiem slēgta."
                if (part.ToLower().Contains("katra mēneša pēdējā trešdiena – metodiskā diena"))
                {
                    extraLast = "We[-1] off";
                    continue;
                }

                string clean = part;
                
                // Special case with applicable month suffix to day
                bool sepToMay = false;
                string sepToMayPrefix = " (no septembra līdz maijam)";
                if (clean.EndsWith(sepToMayPrefix))
                {
                    clean = part[..^sepToMayPrefix.Length].Trim();
                    sepToMay = true;
                }

                // Replace tabs with spaces, since sometimes tabs are used
                clean = clean.Replace("\t", " ");

                // Replace en-dash with dash, since en-dash and dash are both used
                clean = clean.Replace("–", "-");

                // Replace multiple spaces with a single space
                clean = Regex.Replace(clean, @"\s{2,}", " ");

                // Remove spaces around dashes
                clean = Regex.Replace(clean, @"\s*-\s*", "-");

                // Enforce one space after day separator
                clean = Regex.Replace(clean, @"(?<=[a]):\s*", ": ");

                // Enforce no space before time separator and one after; semicolon to comma
                clean = Regex.Replace(clean, @"\s*;\s*", ", ");

                // Replace LV words (and colon suffix) with OSM weekday names
                clean = clean
                        .Replace("Pirmdiena:", "Mo").Replace("Pirmdiena", "Mo")
                        .Replace("Otrdiena:", "Tu").Replace("Otrdiena", "Tu")
                        .Replace("Trešdiena:", "We").Replace("Trešdiena", "We")
                        .Replace("Ceturtdiena:", "Th").Replace("Ceturtdiena", "Th")
                        .Replace("Piektdiena:", "Fr").Replace("Piektdiena", "Fr")
                        .Replace("Sestdiena:", "Sa").Replace("Sestdiena", "Sa")
                        .Replace("Svētdiena:", "Su").Replace("Svētdiena", "Su");

                // Replace dot in hour separator with a colon
                clean = clean.Replace(".", ":");
                
                // Enforce two-digit hours
                clean = Regex.Replace(clean, @" (\d):", @" 0$1:");
                
                // Empty times probably imply closed
                if (Regex.IsMatch(clean, @"^(Mo|Tu|We|Th|Fr|Sa|Su)-$"))
                    continue;
                
                // At this point, we should have a valid OSM opening hours syntax, so check it
                if (!Regex.IsMatch(clean, @"^(Mo|Tu|We|Th|Fr|Sa|Su) \d\d:\d\d-\d\d:\d\d(, \d\d:\d\d-\d\d:\d\d)?$"))
                    throw new Exception();

                if (sepToMay)
                    clean = "Sep-May " + clean;
                
                cleaned.Add(clean);
            }

            if (extraLast != null)
                cleaned.Add(extraLast);

            // Merge sequential days with same times into day ranges instead
            
            for (int i = 1; i < cleaned.Count; i++)
            {
                string previous = cleaned[i - 1];
                string current = cleaned[i];
                
                // Skip special case month prefixes - they are their own line
                // e.g. "Sep-May Mo 08:00-12:00"
                if (current[3] == '-')
                    continue;
                
                if (TimeMatches(previous, current))
                {
                    // Replace previous with the merged version
                    cleaned[i - 1] = MergeDays(previous, current);

                    cleaned.RemoveAt(i);
                    i--; // Adjust index since we removed an item
                }

                
                bool TimeMatches(string a, string b)
                {
                    // e.g. "Mo 08:00-12:00" => "08:00-12:00"
                    // or "Mo-Tu 08:00-12:00" => "08:00-12:00"
                    string aTime = a[(a.IndexOf(' ') + 1) ..];
                    
                    // e.g. "Tu 08:00-12:00" => "08:00-12:00"
                    string bTime = b[3..];
                    
                    return aTime == bTime;
                }

                string MergeDays(string a, string b)
                {
                    // e.g. "Tu 08:00-12:00" => "08:00-12:00"
                    string time = b[3..];
                    
                    // e.g. "Mo 08:00-12:00" => "Mo"
                    // or "Mo-Tu 08:00-12:00" => "Mo"
                    string aStartDay = a[..2];
                    
                    // e.g. "Tu 08:00-12:00" => "Tu"
                    string bDay = b[..2];
                    
                    return aStartDay + "-" + bDay + " " + time;
                }
            }
            
            return string.Join("; ", cleaned);
        }
    }


    [Pure]
    private static VPVKACOffice.VPVKACAddress? CleanAddress(string raw)
    {
        // e.g. "Lielā iela 54<br /> Grobiņas pilsēta<br /> Dienvidkurzemes nov.<br /> LV-3430"
        
        string[] parts = raw.Split("<br />", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 3)
        {
            // Brīvības gatve 455
            // Rīga
            // LV-1024 
            
            string name = parts[0]; // e.g. "Brīvības gatve 455"
            string location = parts[1]; // e.g. "Rīga"
            string postalCode = parts[2]; // e.g. "LV-1024"

            if (Regex.IsMatch(postalCode, @"^LV-\d{4}$") == false) return null;

            name = TryCleanName(name);
            location = TryCleanCityName(location);
            
            return new VPVKACOffice.VPVKACAddress(
                name,
                location,
                null, // pagasts
                null, // novads
                postalCode
            );
        }
        else if (parts.Length == 4)
        {
            // Atmodas iela 22
            // Aizputes pilsēta
            // Dienvidkurzemes nov.
            // LV-3456   
            
            string name = parts[0]; // e.g. "Atmodas iela 22"
            string location = parts[1]; // e.g. "Aizputes pilsēta"
            string novads = parts[2]; // e.g. "Dienvidkurzemes nov."
            string postalCode = parts[3]; // e.g. "LV-3456"

            if (!novads.Contains("nov.")) return null;
            if (Regex.IsMatch(postalCode, @"^LV-\d{4}$") == false) return null;

            name = TryCleanName(name);
            location = TryCleanCityName(location);
            novads = TryCleanNovads(novads);
            
            return new VPVKACOffice.VPVKACAddress(
                name,
                location,
                null, // pagasts
                novads,
                postalCode
            );
        }
        else if (parts.Length == 5)
        {
            if (parts[2].Contains("pilsēta")) // special case
            {     
                // Gaismas iela 19
                // k.9-1
                // Ķekavas pilsēta
                // Ķekavas nov.
                // LV-2123  
                
                string name = parts[0]; // e.g. "Gaismas iela 19"
                // Ignoring e.g. "k.9-1"
                string location = parts[2]; // e.g. "Ķekavas pilsēta"
                string novads = parts[3]; // e.g. "Ķekavas nov."
                string postalCode = parts[4]; // e.g. "LV-2123"

                if (!novads.Contains("nov.")) return null;
                if (!Regex.IsMatch(postalCode, @"^LV-\d{4}$")) return null;

                name = TryCleanName(name);
                novads = TryCleanNovads(novads);

                return new VPVKACOffice.VPVKACAddress(
                    name,
                    location,
                    null,
                    novads,
                    postalCode
                );
            }
            else
            {       
                // Alauksta iela 4
                // Vecpiebalga
                // Vecpiebalgas pag.
                // Cēsu nov.
                // LV-4122   
                
                string name = parts[0]; // e.g. "Alauksta iela 4"
                string location = parts[1]; // e.g. "Vecpiebalga"
                string pagasts = parts[2]; // e.g. "Vecpiebalgas pag."
                string novads = parts[3]; // e.g. "Cēsu nov."
                string postalCode = parts[4]; // e.g. "LV-4122"

                if (!pagasts.Contains("pag.")) return null;
                if (!novads.Contains("nov.")) return null;
                if (!Regex.IsMatch(postalCode, @"^LV-\d{4}$")) return null;

                name = TryCleanName(name);
                location = TryCleanCityName(location);
                pagasts = TryCleanPagasts(pagasts);
                novads = TryCleanNovads(novads);

                return new VPVKACOffice.VPVKACAddress(
                    name,
                    location,
                    pagasts,
                    novads,
                    postalCode
                );
            }
        }
        else 
        {
            return null;
        }
    }

    [Pure]
    private static VPVKACOffice.VPVKACAddress AdjustAddress(VPVKACOffice.VPVKACAddress address)
    {
        // No idea what the "10" is here, but "Tērces" seems to match well
        if (address.Name == "\"Tērces-10\"")
            return address with { Name = "\"Tērces\"" };

        return address;
    }


    [Pure]
    private static string TryCleanName(string name)
    {
        name = name.Replace('“', '"').Replace('”', '"'); // e.g. "“Silavas”", just use normal quotes that address matcher understands

        name = name.Replace("  ", " "); // e.g. "Raiņa  iela"
        
        name = name.Replace("ielā", "iela"); // e.g. "Skolas ielā 17" or "Bērzu ielā"
        
        name = name.Replace("Somersetas", "Somersētas"); // unique typo
        name = name.Replace("Skola iela", "Skolas iela"); // unique typo
        name = name.Replace("Brinģenes", "Briģenes"); // unique typo
        name = name.Replace("Liela", "Lielā"); // unique typo
        
        name = name.Replace("Skolas iela 4-40", "Skolas iela 4"); // 40 is the flat number in an apartment building
        name = name.Replace("Pils iela 5-1", "Pils iela 5"); // 1 must be some in-building designation
        
        return name;
    }

    [Pure]
    private static string TryCleanCityName(string location)
    {
        if (location.Contains("pilsēta"))
            location = location
                       .Replace("s pilsēta", "") // generic e.g. "Grobiņas pilsēta" -> "Grobiņa", many matches like this
                       .Replace("u pilsēta", "i") // generic e.g. "Ādažu pilsēta" -> "Ādaži", multiple matches like this
                       .Replace("Pļaviņu pilsēta", "Pļaviņas") // special case "Pļaviņu pilsēta" -> "Pļaviņas"
                       .Replace("Cēsu pilsēta", "Cēsis"); // special case "Cēsu pilsēta" -> "Cēsis"

        return location;
    }

    [Pure]
    private static string TryCleanPagasts(string pagasts)
    {
        return pagasts.Replace("pag.", "pagasts").Trim();
    }

    [Pure]
    private static string TryCleanNovads(string novads)
    {
        return novads.Replace("nov.", "novads").Trim();
    }
}