using System.Threading;

namespace Osmalyzer;

[UsedImplicitly]
public class CourthouseAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Courthouses";

    public override string ReportWebLink => @"https://www.tiesas.lv/lv/tiesu-saraksts-pec-apgabala";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "courthouses";


    public IEnumerable<CourthouseData> Courthouses => _courthouses;

    
    private List<CourthouseData> _courthouses = null!; // only null until prepared

    
    protected override void Download()
    {
        Console.WriteLine("-> Downloading main page \"" + ReportWebLink + "\"...");
        
        string mainPage = WebsiteBrowsingHelper.Read(
            ReportWebLink,
            true
        );

        // "<a href="/lv/filiale/vidzemes-rajona-tiesa-gulbene" rel="bookmark" aria-label="Atvērt Vidzemes rajona tiesa (Gulbenē)"></a>"
        MatchCollection matches = Regex.Matches(mainPage, @"<a href=""(/lv/filiale/[^""]+)"" rel=""bookmark"" aria-label=""Atvērt ([^""]+)""></a>", RegexOptions.Singleline);
        
        if (matches.Count == 0)
            throw new Exception("Did not match any subpages on main page");

        for (int i = 0; i < matches.Count; i++)
        {
            string subpageUrl = "https://www.tiesas.lv/" + matches[i].Groups[1].ToString().Trim();

            Thread.Sleep(1000);
            
            Console.WriteLine("-> Downloading subpage #" + (i + 1) + "/" + matches.Count + ": \"" + subpageUrl + "\"...");
           
            WebsiteBrowsingHelper.DownloadPage(
                subpageUrl,
                Path.Combine(CacheBasePath, DataFileIdentifier + "-" + (i + 1) + ".html"),
                true,
                "copyright-wrap" // found at footer as div class
            );
            // I had page not fully download
        }
    }

    protected override void DoPrepare()
    {
        _courthouses = [ ];

        int index = 1;
        
        do
        {
            string path = Path.Combine(CacheBasePath, DataFileIdentifier + "-" + index + ".html");
            
            if (!File.Exists(path))
                break;

            string content = File.ReadAllText(path);
            
            // Name
            // "<h1 class="display-4">Rīgas pilsētas tiesa (Daugavgrīvas iela 58)</h1>"
            
            Match nameMatch = Regex.Match(content, @"<h1 class=""display-4"">([^<]+)</h1>", RegexOptions.Singleline);
            
            if (!nameMatch.Success)
                throw new Exception("Did not match name");
            
            string name = nameMatch.Groups[1].ToString().Trim();
            
            // Names can come with parentheses for a location hint
            // e.g. "Rīgas rajona tiesa (Siguldā)"
            string? locationHint;
            
            Match locationHintMatch = Regex.Match(name, @"^(.*) \(([^)]+)\)$", RegexOptions.Singleline);
            if (locationHintMatch.Success)
            {
                name = locationHintMatch.Groups[1].ToString().Trim();
                locationHint = locationHintMatch.Groups[2].ToString().Trim();
            }
            else
            {
                locationHint = null;
            }
            
            // Address (with bad coords)
            // "<div class="branch_contacts__branch-address"><a href="https://www.google.com/maps/search/?api=1&amp;query=57.51297000000112,24.717880000000058" data-latitude="543007.42647012" data-longitude="374716.96658417" class="geo-location-url has-generated-url" target="_blank">Cēsu iela 18, Limbaži, LV-4001</a></div>"
            // or
            // "<div class="branch_contacts__branch-address">Brīvības bulvāris 34, Rīga, LV-1886</div>"
            Match match = Regex.Match(content, @"<div class=""branch_contacts__branch-address"">([^<]+)</div>", RegexOptions.Singleline);
            // must match from "branch_contacts__branch-address" because it has multiple links to other places too with the same <a> syntax
            
            if (!match.Success) // try with <a>
                match = Regex.Match(content, @"<div class=""branch_contacts__branch-address""><a [^>]+>([^<]+)</a>", RegexOptions.Singleline);
            
            if (!match.Success)
                throw new Exception("Did not match address and coordinates");
            
            // I don't know what is wrong with these pages, but they all point to the same wrong invalid coord
            // and this has something to do with browsing/loading pages and not running some sort of script they have for coords
            // So not parsing coords directly for now
            // double latRaw = double.Parse(match.Groups[1].ToString().Trim(), CultureInfo.InvariantCulture);
            // double lonRaw = double.Parse(match.Groups[2].ToString().Trim(), CultureInfo.InvariantCulture);
            // (double lat, double lon) = CoordConversion.LKS92ToWGS84(latRaw, lonRaw);
            //Debug.WriteLine("Parsed courthouse: " + name + " @ " + lat + "," + lon);
            
            string address = match.Groups[1].ToString().Trim();
            
            // Grab the portions with phone and email
            
            // Start at "<h4>Kontakti</h4>" (not "<h4 id="footer-contacts">Kontakti</h4>")
            Match contactTopMatch = Regex.Match(content, @"<h4>\s*Kontakti\s*</h4>", RegexOptions.Singleline);
            
            if (!contactTopMatch.Success)
                throw new Exception("Did not find contact section start");

            int contactMatchStart = contactTopMatch.Index;
                
            // End somewhere at next "`... <a data-external-link="TRUE" href="//latvija.gov.lv/KDV/Mailbox/">Latvija.gov.lv,</a> ...), not sure how else to detect other than to parse full html hierarchy
            Match contactBottomMatch = Regex.Match(content[contactMatchStart..], @"KDV/Mailbox/", RegexOptions.Singleline);
            
            if (!contactBottomMatch.Success)
                throw new Exception("Did not find contact section end match");
            
            int contactMatchEnd = contactBottomMatch.Index + contactMatchStart; // since we matched on substring

            string contactPortion = content[contactMatchStart .. contactMatchEnd];
            
            // Phone number(s)

            // Find "<a href="tel:+371 67610337" aria-label="Tālruņa numurs: +371 67610337">+371 67610337</a>"
            MatchCollection phoneMatches = Regex.Matches(contactPortion, @"""tel:([^""]+)""", RegexOptions.Singleline);
            
            if (phoneMatches.Count == 0)
                throw new Exception("Did not match any phone numbers");
            
            List<string> phones = [ ];
            foreach (Match phoneMatch in phoneMatches)
            {
                string cleaned = phoneMatch.Groups[1].ToString().Trim();
                if (!phones.Contains(cleaned))
                    phones.Add(cleaned);
            }

            // E-mail
            // Either unobfuscated:
            // "<div class="field-email"><a href="mailto:riga.apgabals@tiesas.lv" class="spamspan">riga.apgabals@tiesas.lv</a></div>"
            // or obfuscated:
            // "<span class="spamspan"><span class="u">riga.apgabals</span> [at] <span class="d">tiesas.lv</span></span>"
            // "<span class="spamspan"><span class="u">vidzeme</span> [at] <span class="d">tiesas.lv</span></span>"
            
            Match emailMatch = Regex.Match(contactPortion, @"<a href=""mailto:([^@]+)@([^""]+)""", RegexOptions.Singleline);
            
            if (!emailMatch.Success) // try obfuscated
                emailMatch = Regex.Match(content, @"<span class=""spamspan"">\s*<span[^>]+>([^<]+)</span>\s*\[at\]\s*<span[^>]+>\s*([^<]+)\s*</span>\s*</span>", RegexOptions.Singleline);
            
            if (!emailMatch.Success)
                throw new Exception("Did not match email address");
            
            string email = emailMatch.Groups[1].ToString().Trim() + "@" + emailMatch.Groups[2].ToString().Trim();
            
            // Opening hours
            // todo:
            // <div class="institution_working_time__working-time-data"><ul class="work-time-list">
            //   <li data-day="1">
            //     <span>Pirmdiena</span>
            //         <span class="work-time ">
            //               8.30–18.00
            //           </span>
            //   </li>
            //   ..
            //   <li data-day="7">
            //     <span>Svētdiena</span>
            //         <span class="work-time closed">
            //               Slēgts
            //           </span>
            //   </li>
            // </ul>
            // </div>
            
            // Find "<ul class="work-time-list">"
            int hoursMatchStart = content.IndexOf(@"<ul class=""work-time-list"">", StringComparison.Ordinal);
            
            // End at "</ul>"
            int hoursMatchEnd = content.IndexOf("</ul>", hoursMatchStart, StringComparison.Ordinal);
            
            if (hoursMatchStart == -1 || hoursMatchEnd == -1 || hoursMatchEnd <= hoursMatchStart)
                throw new Exception("Did not find opening hours section");
            
            string hoursPortion = content[hoursMatchStart .. hoursMatchEnd];
            
            // Match all the day spans
            
            MatchCollection dayMatches = Regex.Matches(hoursPortion, @"<span>([^<]+)</span>\s*<span[^>]+>([^<]+)</span>", RegexOptions.Singleline);

            if (dayMatches.Count == 0)
                throw new Exception("Did not match any opening hours");

            List<string> dayHours = [ ];
            
            foreach (Match dayMatch in dayMatches)
            {
                string day = dayMatch.Groups[1].ToString().Trim().ToLowerInvariant();
                string hours = dayMatch.Groups[2].ToString().Trim().ToLowerInvariant();

                if (hours == "slēgts")
                    continue;
                
                string cleanDay = TextDayToOsmDay(day);
                string cleanHours = CleanHours(hours);
                
                dayHours.Add(cleanDay + " " + cleanHours);
                    
                continue;

                [Pure]
                static string TextDayToOsmDay(string day) =>
                    day switch
                    {
                        "pirmdiena"   => "Mo",
                        "otrdiena"    => "Tu",
                        "trešdiena"   => "We",
                        "ceturtdiena" => "Th",
                        "piektdiena"  => "Fr",
                        "sestdiena"   => "Sa",
                        "svētdiena"   => "Su",
                        _             => throw new Exception("Unknown day: " + day)
                    };

                [Pure]
                static string CleanHours(string hours)
                {
                    // "8.30–17.00" -> "08:30-17:00"
                    Match hoursMatch = Regex.Match(hours, @"^(\d{1,2})[.](\d{2})[–-](\d{1,2})[.](\d{2})$");
                    
                    if (!hoursMatch.Success)
                        throw new Exception("Did not match hours: " + hours);
                    
                    int openHour = int.Parse(hoursMatch.Groups[1].ToString());
                    int openMinute = int.Parse(hoursMatch.Groups[2].ToString());
                    int closeHour = int.Parse(hoursMatch.Groups[3].ToString());
                    int closeMinute = int.Parse(hoursMatch.Groups[4].ToString());
                    
                    return $"{openHour:D2}:{openMinute:D2}-{closeHour:D2}:{closeMinute:D2}";
                }
            }
            
            if (dayMatches.Count == 0)
                throw new Exception("Did not match any opening hours");
            
            // Merge sequential days with same times into day ranges instead
            List<string> mergedDayHours = OsmOpeningHoursHelper.MergeSequentialWeekdaysWithSameTimes(dayHours);
            
            string openingHours = string.Join(";", mergedDayHours);

            // Done parsing what we can

            _courthouses.Add(
                new CourthouseData(
                    name,
                    address,
                    locationHint,
                    phones,
                    email,
                    openingHours
                )
            );
            
            index++;
        } while (true);
    }
}