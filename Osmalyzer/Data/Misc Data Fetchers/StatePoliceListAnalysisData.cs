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


    private const string hqPageUrl = @"https://www.vp.gov.lv/lv/iestades-kontakti";

    private const string hqFileId = "state-police-hq";


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

        Thread.Sleep(1000);

        Console.WriteLine("-> Downloading HQ page \"" + hqPageUrl + "\"...");

        WebsiteBrowsingHelper.DownloadPage(
            hqPageUrl,
            Path.Combine(CacheBasePath, hqFileId + ".html"),
            true,
            "institution_contacts__institution-address" // ensure the contact content is loaded
        );
    }

    protected override void DoPrepare()
    {
        Offices = [];

        // Parse the main headquarters page

        string hqPath = Path.Combine(CacheBasePath, hqFileId + ".html");

        if (!File.Exists(hqPath))
            throw new Exception("HQ page file not found at expected path: " + hqPath);
        
        string hqContent = File.ReadAllText(hqPath);

        Offices.Add(
            ParseOffice(
                hqContent,
                "Valsts policija",
                null,
                hqPageUrl,
                "institution_contacts__institution-address",
                "institution_contacts__institution-main-new-phone",
                "row institution-contacts-row"
            )
        );

        // Parse branch pages

        int index = 1;

        do
        {
            string path = Path.Combine(CacheBasePath, DataFileIdentifier + "-" + index + ".html");

            if (!File.Exists(path))
            {
                if (index == 1)
                    throw new Exception("No branch page files at all found at expected path: " + path);
                
                break;
            }

            string content = File.ReadAllText(path);

            // Some pages are disambiguation pages that list sub-offices ("Nodaļas" section)
            // rather than being actual office entries -- skip these, as the actual offices are linked individually
            if (IsDisambiguationOnlyPage(content))
            {
                index++;
                continue;
            }

            string name = ParseName(content);
            name = CleanName(name);
            string abbreviatedName = AbbreviateName(name);
            string website = ParseWebsite(content);

            Offices.Add(
                ParseOffice(
                    content,
                    name,
                    abbreviatedName,
                    website,
                    "branch_contacts__branch-address",
                    "branch_contacts__branch__new-phone",
                    "row branch-contacts-row"
                )
            );

            index++;
        } while (true);
    }


    [Pure]
    private static StatePoliceData ParseOffice(string content, string name, string? abbreviatedName, string website, string addressDivClass, string phoneDivClass, string contactRowClass)
    {
        OsmCoord coord = ParseCoord(content, addressDivClass);
        string? address = ParseAddress(content, addressDivClass);
        address = CleanAddress(address);
        string? phone = ParsePhone(content, phoneDivClass);
        string? email = ParseEmail(content, contactRowClass);
        string? openingHours = ParseOpeningHours(content);

        return new StatePoliceData(
            name,
            abbreviatedName,
            coord,
            website,
            address,
            phone,
            email,
            openingHours
        );
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


    [Pure]
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

    [Pure]
    private string CleanName(string name)
    {
        // "Rīgas Pārdaugavas pārvalde" -> "Valsts policijas Rīgas reģiona pārvaldes Rīgas Pārdaugavas pārvalde"
        // "Rīgas Ziemeļu pārvalde" -> "Valsts policijas Rīgas reģiona pārvaldes Rīgas Pārdaugavas pārvalde"
        // "Rīgas Austrumu pārvalde" -> "Valsts policijas Rīgas reģiona pārvaldes Rīgas Pārdaugavas pārvalde"
        
        if (name.StartsWith("Rīgas "))
            name = "Valsts policijas Rīgas reģiona pārvaldes " + name;

        return name;
    }

    [Pure]
    private string AbbreviateName(string name)
    {
        // "Valsts policijas Latgales reģiona pārvaldes Dienvidlatgales iecirknis Daugavpilī"
        // -> "VP LRP Dienvidlatgales iecirknis Daugavpilī"
        
        name = name.Replace("Valsts policijas", "VP");
        
        name = name.Replace("Rīgas reģiona pārvaldes", "RRP");
        name = name.Replace("Vidzemes reģiona pārvaldes", "VRP");
        name = name.Replace("Latgales reģiona pārvaldes", "LRP");
        name = name.Replace("Zemgales reģiona pārvaldes", "ZRP");
        name = name.Replace("Kurzemes reģiona pārvaldes", "KRP");
        
        return name;
    }

    [Pure]
    private static OsmCoord ParseCoord(string content, string addressDivClass)
    {
        // Branch pages:
        // <div class="branch_contacts__branch-address"><a href="..." data-latitude="507311.1725455095" data-longitude="307920.65337404417" ...>Mūkusalas iela 101, Rīga, LV-1004</a></div>

        // HQ page:
        // <div class="institution_contacts__institution-address"><a href="/lv" data-latitude="509623" data-longitude="315463" ...>Čiekurkalna 1.līnija 1, k- 4 , Rīga, LV - 1026</a></div>

        // Note that branch pages also always have institution_contacts__institution-address in the footer --
        // so cannot match just "data-latitude", need to target the right div class
        Match coordMatch = Regex.Match(
            content,
            @"class=""" + Regex.Escape(addressDivClass) + @""">.*?data-latitude=""([^""]+)""\s+data-longitude=""([^""]+)""",
            RegexOptions.Singleline
        );

        if (!coordMatch.Success)
            throw new Exception("Did not match coordinates on state police page (class=\"" + addressDivClass + "\")");

        double northing = double.Parse(coordMatch.Groups[1].Value);
        double easting = double.Parse(coordMatch.Groups[2].Value);

        (double lat, double lon) = CoordConversion.LKS92ToWGS84(northing, easting);

        return new OsmCoord(lat, lon);
    }

    [Pure]
    private static string ParseWebsite(string content)
    {
        // <link rel="canonical" href="https://www.vp.gov.lv/lv/filiale/valsts-policijas-vidzemes-regiona-parvaldes-ziemelvidzemes-iecirknis" />
        Match websiteMatch = Regex.Match(
            content,
            @"<link rel=""canonical"" href=""([^""]+)""",
            RegexOptions.Singleline
        );

        if (!websiteMatch.Success)
            throw new Exception("Did not match canonical URL on state police branch page");

        return websiteMatch.Groups[1].Value.Trim();
    }

    [Pure]
    private static string? ParseAddress(string content, string addressDivClass)
    {
        // Branch pages:
        // <div class="branch_contacts__branch-address"><a ... >Zemgales iela 26a, Olaine, LV - 2114</a></div>

        // HQ page:
        // <div class="institution_contacts__institution-address"><a ... >Čiekurkalna 1.līnija 1, k- 4 , Rīga, LV - 1026</a></div>
        Match addressMatch = Regex.Match(
            content,
            @"class=""" + Regex.Escape(addressDivClass) + @""">(?:<a[^>]+>)?([^<]+)(?:</a>)?</div>",
            RegexOptions.Singleline
        );

        if (!addressMatch.Success)
            return null;

        return WebUtility.HtmlDecode(addressMatch.Groups[1].Value.Trim());
    }

    [Pure]
    private static string? CleanAddress(string? address)
    {
        if (address == null)
            return null;
        
        // "Zemgales iela 26a, Olaine, LV - 2114" -> "Zemgales iela 26a, Olaine, LV-2114"
        address = address.Replace("LV - ", "LV-");
        
        return address;
    }

    [Pure]
    private static string? ParsePhone(string content, string phoneDivClass)
    {
        // Branch pages:
        // <div class="branch_contacts__branch__new-phone">
        //   <div><div class="field__item field-phone">
        //     <a href="tel:+371 NNNNN" ...>+371 NNNNN</a>
        //   </div></div>
        // </div>

        // HQ page:
        // <div class="institution_contacts__institution-main-new-phone">
        //   <div><div class="field__item field-phone">
        //     <a href="tel:+371 67075333" ...>+371 67075333</a>
        //   </div></div>
        // </div>
        Match phoneMatch = Regex.Match(
            content,
            @"class=""" + Regex.Escape(phoneDivClass) + @""".*?<a href=""tel:([^""]+)""",
            RegexOptions.Singleline
        );

        if (!phoneMatch.Success)
            return null;

        return phoneMatch.Groups[1].Value.Trim();
    }

    [Pure]
    private static string? ParseEmail(string content, string contactRowClass)
    {
        // Limit parsing to the contact section, not the institution footer shared by all pages
        // Branch pages use "row branch-contacts-row"; HQ page uses "row institution-contacts-row"
        Match contactSectionMatch = Regex.Match(
            content,
            @"<div class=""" + Regex.Escape(contactRowClass) + @""">(.*?)</div><!-- /\.node -->",
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

    [Pure]
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
