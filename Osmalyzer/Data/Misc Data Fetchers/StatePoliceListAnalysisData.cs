using System.Net;
using System.Threading;

namespace Osmalyzer;

[UsedImplicitly]
public class StatePoliceListAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "State Police offices";

    public override string ReportWebLink => @"https://www.vp.gov.lv/lv/filiales";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "state-police";


    public List<StatePoliceData> Offices { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        Console.WriteLine("-> Downloading main page \"" + ReportWebLink + "\"...");

        string mainPage = WebsiteBrowsingHelper.Read(
            ReportWebLink,
            true
        );

        // Each branch entry has a link like:
        // <a href="/lv/filiale/rigas-pardaugavas-parvalde" rel="bookmark">
        //   <h3>Rīgas Pārdaugavas pārvalde</h3>
        // </a>
        MatchCollection matches = Regex.Matches(
            mainPage,
            @"<a href=""(/lv/filiale/[^""]+)"" rel=""bookmark"">\s*<h3>[^<]+</h3>",
            RegexOptions.Singleline
        );

        if (matches.Count == 0)
            throw new Exception("Did not match any branch subpage links on state police list page");

        // Deduplicate, because the same link appears twice per entry (left-side name + right-side arrow)
        List<string> uniqueSubpages = matches
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        for (int i = 0; i < uniqueSubpages.Count; i++)
        {
            string subpageUrl = "https://www.vp.gov.lv" + uniqueSubpages[i];

            Thread.Sleep(1000);

            Console.WriteLine("-> Downloading subpage #" + (i + 1) + "/" + uniqueSubpages.Count + ": \"" + subpageUrl + "\"...");

            WebsiteBrowsingHelper.DownloadPage(
                subpageUrl,
                Path.Combine(CacheBasePath, DataFileIdentifier + "-" + (i + 1) + ".html"),
                true,
                "branch_contacts__branch-address" // ensure the branch content is loaded
            );
        }
    }

    protected override void DoPrepare()
    {
        Offices = [];

        int index = 1;

        do
        {
            string path = Path.Combine(CacheBasePath, DataFileIdentifier + "-" + index + ".html");

            if (!File.Exists(path))
                break;

            string content = File.ReadAllText(path);

            // Some pages are disambiguation pages that list sub-offices ("Nodaļas" section)
            // rather than being actual office entries -- skip these, as the actual offices are linked individually
            if (IsDisambiguationOnlyPage(content))
            {
                index++;
                continue;
            }

            string name = ParseName(content);
            OsmCoord coord = ParseCoord(content);
            string? address = ParseAddress(content);
            string? phone = ParsePhone(content);
            string? email = ParseEmail(content);
            string? openingHours = ParseOpeningHours(content);

            Offices.Add(
                new StatePoliceData(
                    name,
                    coord,
                    address,
                    phone,
                    email,
                    openingHours
                )
            );

            index++;
        } while (true);
    }


    [Pure]
    private static bool IsDisambiguationOnlyPage(string content)
    {
        // Some pages list sub-offices ("Nodaļas" section) without being actual entries themselves --
        // they have no address/coords of their own. Skip these, as the actual offices are linked individually.
        // Pages that have both "Nodaļas" and their own address are real offices (e.g. regional HQ) and should not be skipped.
        return content.Contains(@"<h4>Nodaļas</h4>") &&
               !content.Contains(@"branch_contacts__branch-address");
    }


    private static string ParseName(string content)
    {
        // <h1 class="display-4">Valsts policijas ... iecirknis ...</h1>
        Match nameMatch = Regex.Match(
            content,
            @"<h1 class=""display-4"">([^<]+)</h1>",
            RegexOptions.Singleline
        );

        if (!nameMatch.Success)
            throw new Exception("Did not match name on state police branch page");

        return WebUtility.HtmlDecode(nameMatch.Groups[1].Value.Trim());
    }

    private static OsmCoord ParseCoord(string content)
    {
        // <div class="branch_contacts__branch-address"><a href="https://www.google.com/maps/search/?api=1&amp;query=56.914910632600794,24.120078843113923" data-latitude="507311.1725455095" data-longitude="307920.65337404417" class="geo-location-url has-generated-url" target="_blank" aria-label="Adrese: Mūkusalas iela 101, Rīga, LV-1004">Mūkusalas iela 101, Rīga, LV-1004</a></div>
        
        Match coordMatch = Regex.Match(
            content,
            @"class=""branch_contacts__branch-address"">.+?data-latitude=""(\d+)""[^>]+data-longitude=""(\d+)""",
            RegexOptions.Singleline
        );

        if (!coordMatch.Success)
            throw new Exception("Did not match coordinates on state police branch page");

        double northing = double.Parse(coordMatch.Groups[1].Value);
        double easting = double.Parse(coordMatch.Groups[2].Value);

        (double lat, double lon) = CoordConversion.LKS92ToWGS84(northing, easting);

        return new OsmCoord(lat, lon);
    }

    private static string? ParseAddress(string content)
    {
        // <div class="branch_contacts__branch-address"><a ... >Zemgales iela 26a, Olaine, LV - 2114</a></div>
        Match addressMatch = Regex.Match(
            content,
            @"class=""branch_contacts__branch-address"">(?:<a[^>]+>)?([^<]+)(?:</a>)?</div>",
            RegexOptions.Singleline
        );

        if (!addressMatch.Success)
            return null;

        return WebUtility.HtmlDecode(addressMatch.Groups[1].Value.Trim());
    }

    private static string? ParsePhone(string content)
    {
        // Phone in branch_contacts__branch__new-phone (not the 112 in branch_contacts__new-short-branch-numbe):
        // <div class="branch_contacts__branch__new-phone">
        //   <div><div class="field__item field-phone">
        //     <a href="tel:+371 NNNNN" ...>+371 NNNNN</a>
        //   </div></div>
        // </div>
        Match phoneMatch = Regex.Match(
            content,
            @"class=""branch_contacts__branch__new-phone"".*?<a href=""tel:([^""]+)""",
            RegexOptions.Singleline
        );

        if (!phoneMatch.Success)
            return null;

        return phoneMatch.Groups[1].Value.Trim();
    }

    private static string? ParseEmail(string content)
    {
        // Limit parsing to branch contact section, not institution footer
        Match contactSectionMatch = Regex.Match(
            content,
            @"<div class=""row branch-contacts-row"">(.*?)</div><!-- /\.node -->",
            RegexOptions.Singleline
        );

        string searchBlock = contactSectionMatch.Success ? contactSectionMatch.Groups[1].Value : content;

        // Non-obfuscated: <a href="mailto:pasts@vp.gov.lv" ...>pasts@vp.gov.lv</a>
        Match emailMatch = Regex.Match(
            searchBlock,
            @"href=""mailto:([^""]+)""",
            RegexOptions.Singleline
        );

        if (emailMatch.Success)
            return emailMatch.Groups[1].Value.Trim();

        // Obfuscated: <span class="spamspan"><span class="u">USER</span> [at] <span class="d">DOMAIN</span></span>
        emailMatch = Regex.Match(
            searchBlock,
            @"<span class=""u"">([^<]+)</span>\s*\[at\]\s*<span class=""d"">([^<]+)</span>",
            RegexOptions.Singleline
        );

        if (emailMatch.Success)
            return emailMatch.Groups[1].Value.Trim() + "@" + emailMatch.Groups[2].Value.Trim();

        return null;
    }

    private static string? ParseOpeningHours(string content)
    {
        // <ul class="work-time-list">
        //   <li data-day="1"><span>Pirmdiena</span><span class="work-time ">8.00 - 16.30</span></li>
        //   ...
        // </ul>
        int hoursStart = content.IndexOf(@"<ul class=""work-time-list"">", StringComparison.Ordinal);

        if (hoursStart == -1)
            return null;

        int hoursEnd = content.IndexOf("</ul>", hoursStart, StringComparison.Ordinal);

        if (hoursEnd == -1)
            return null;

        string hoursPortion = content[hoursStart..hoursEnd];

        MatchCollection dayMatches = Regex.Matches(
            hoursPortion,
            @"<span>([^<]+)</span>\s*<span[^>]+>([^<]+)</span>",
            RegexOptions.Singleline
        );

        if (dayMatches.Count == 0)
            return null;

        List<string> dayHours = [];

        foreach (Match dayMatch in dayMatches)
        {
            string day = dayMatch.Groups[1].Value.Trim().ToLowerInvariant();
            string hours = dayMatch.Groups[2].Value.Trim().ToLowerInvariant();

            if (hours == "slēgts")
                continue;

            string cleanDay = TextDayToOsmDay(day);
            string cleanHours = CleanHours(hours);

            dayHours.Add(cleanDay + " " + cleanHours);
        }

        if (dayHours.Count == 0)
            return null;

        List<string> mergedDayHours = OsmOpeningHoursHelper.MergeSequentialWeekdaysWithSameTimes(dayHours);

        return string.Join(";", mergedDayHours);


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
            // "8.00 - 16.30" or "8.00-16.30" or "8.00–16.30" -> "08:00-16:30"
            Match hoursMatch = Regex.Match(hours, @"^(\d{1,2})[.](\d{2})\s*[–\-]\s*(\d{1,2})[.](\d{2})$");

            if (!hoursMatch.Success)
                throw new Exception("Did not match hours: " + hours);

            int openHour = int.Parse(hoursMatch.Groups[1].Value);
            int openMinute = int.Parse(hoursMatch.Groups[2].Value);
            int closeHour = int.Parse(hoursMatch.Groups[3].Value);
            int closeMinute = int.Parse(hoursMatch.Groups[4].Value);

            return $"{openHour:D2}:{openMinute:D2}-{closeHour:D2}:{closeMinute:D2}";
        }
    }
}
