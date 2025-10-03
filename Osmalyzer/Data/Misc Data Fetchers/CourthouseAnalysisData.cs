using System.Diagnostics;
using System.Globalization;

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

        // "<a href="/lv/filiale/vidzemes-rajona-tiesa-gulbene"   data-uuid="7640734c-7ef9-4e59-bff5-0abecd6b9236" class="nav-link">Vidzemes rajona tiesa (Gulbenē)</a>"
        MatchCollection matches = Regex.Matches(mainPage, @"<a href=""(\/lv\/filiale\/[^""]+)""\s+data-uuid=""[""]+""\s+class=""nav-link"">[^<]+<\/a>", RegexOptions.Singleline);
        
        if (matches.Count == 0)
            throw new Exception("Did not match any subpages on main page");

        for (int i = 0; i < matches.Count; i++)
        {
            string subpageUrl = "https://www.tiesas.lv/" + matches[i].Groups[1].ToString().Trim();
            
            Console.WriteLine("-> Downloading subpage #" + (i + 1) + "/" + matches.Count + ": \"" + subpageUrl + "\"...");

            WebsiteBrowsingHelper.DownloadPage(
                subpageUrl,
                Path.Combine(CacheBasePath, DataFileIdentifier + "-" + (i + 1) + ".html")
            );
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
            
            // Address with coords
            // "<a href="https://www.google.com/maps/search/?api=1&amp;query=56.957050000001004,24.109149999999993" data-latitude="506638.26574253" data-longitude="312610.24397132" class="geo-location-url has-generated-url" target="_blank">Antonijas iela 6, Rīga, LV - 1010</a>"
            // "<a href="/lv" data-latitude="506638.26574253" data-longitude="312610.24397132" class="geo-location-url" target="_blank">Antonijas iela 6, Rīga, LV - 1010</a>"
            
            Match match = Regex.Match(content, @"<a href=""[^""]+""\s+data-latitude=""([^""]+)""\s+data-longitude=""([^""]+)""\s+class=""[^""]+""\s+target=""_blank"">([^<]+)</a>", RegexOptions.Singleline);

            if (!match.Success)
                throw new Exception("Did not match address and coordinates");
            
            double latRaw = double.Parse(match.Groups[1].ToString().Trim(), CultureInfo.InvariantCulture);
            double lonRaw = double.Parse(match.Groups[2].ToString().Trim(), CultureInfo.InvariantCulture);
            (double lat, double lon) = CoordConversion.LKS92ToWGS84(latRaw, lonRaw);
            // TODO: I don't know what is wrong with these pages, but they all point to the same wrong invalid coord and this has something to do with browsing/loading pages and not running some sort of script they have for coords
            
            string address = match.Groups[3].ToString().Trim();
            
            Debug.WriteLine("Parsed courthouse: " + name + " @ " + lat + "," + lon);
            
            // Phone number(s)
            // todo: "<a href="tel:+371 67613390" aria-label="Tālruņa numurs: +371 67613390">+371 67613390</a>"
            
            // E-mail
            // todo: "<div class="field-email"><span class="spamspan"><span class="u">rigas.pilseta</span> [at] <span class="d">tiesas.lv</span></span></div>"
            
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
            
            _courthouses.Add(
                new CourthouseData(
                    new OsmCoord(lat, lon),
                    name,
                    address
                )
            );
            
            index++;
        } while (true);
    }
}