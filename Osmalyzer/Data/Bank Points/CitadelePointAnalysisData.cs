using System.Web;

namespace Osmalyzer;

[UsedImplicitly]
public class CitadelePointAnalysisData : BankPointAnalysisData
{
    public override string Name => "Citadele Points";

    public override string ReportWebLink => @"https://www.citadele.lv/lv/contacts";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "citadele-points";


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.citadele.lv/lv/map/", 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".html")
        );
    }

    protected override void DoPrepare()
    {
        string data = File.ReadAllText(Path.Combine(CacheBasePath, DataFileIdentifier + @".html"));

        // <li class="location-details" data-id="110" data-latitude="56.915" data-longitude="24.113819" data-options="citadele_atm" data-type="atm" id="place110">
        // <div class="info">
        // <h3 class="title">
        // <a href="#place110">
        // Pie CSDD
        // </a>
        // </h3>
        // <ul class="opening-hours">
        // <li class="24h">diennakts</li>
        // </ul>
        // <p class="address">
        // Bauskas iela 86, Rīga
        // </p>
        // </div>
        // <div class="actions">
        // <p class="directions">
        // <a href="https://www.google.com/maps?daddr=56.915,24.113819">
        // <span class="text">Kā nokļūt </span>
        // </a>
        // </p>
        // </div>
        // </li>

        Points = new List<BankPoint>();
        
        MatchCollection matches = Regex.Matches(data, @"<li class=""location-details"".*?\n<\/li>", RegexOptions.Singleline);

        if (matches.Count == 0)
            throw new Exception("Did not find items on webpage");
        
        foreach (Match match in matches)
        {
            string matchText = match.ToString();
            
            BankPointType type = RawTypeToPointType(
                Regex.Match(matchText, @"data-type=""([^""]+)""").Groups[1].ToString().Trim(), // data-type="atm"
                Regex.Match(matchText, @"data-options=""([^""]+)""").Groups[1].ToString().Trim(), // data-options="cash_deposit_atm citadele_atm"
                out bool? deposit
            );
            
            OsmCoord coord = new OsmCoord(
                double.Parse(Regex.Match(matchText, @"data-latitude=""([^""]+)""").Groups[1].ToString().Trim()), // data-latitude="54.910316"
                double.Parse(Regex.Match(matchText, @"data-longitude=""([^""]+)""").Groups[1].ToString().Trim()) // data-longitude="23.851042"
            );

            string name = HttpUtility.HtmlDecode(Regex.Match(matchText, @"<a href=""#place[^""]+"">([^<]+)<\/a>", RegexOptions.Singleline).Groups[1].ToString().Trim());
            // <a href="#place309">
            // Veikals  ERMITAŽAS
            // </a>            
            
            string? address = HttpUtility.HtmlDecode(Regex.Match(matchText, @"<p class=""address"">([^<]+)<\/p>", RegexOptions.Singleline).Groups[1].ToString().Trim());
            if (address == "")
                address = null;
            // <p class="address">
            // Republikas laukums 2a, Rīga
            // </p>            

            BankPoint point = type switch
            {
                BankPointType.Branch => new BankBranchPoint("Citadele", name, address, coord),
                BankPointType.Atm    => new BankAtmPoint("Citadele", name, address, coord, deposit),

                _ => throw new ArgumentOutOfRangeException()
            };
            
            Points.Add(point);
        }
    }
    
    
    [Pure]
    private static BankPointType RawTypeToPointType(string rawType, string rawExtras, out bool? deposit)
    {
        switch (rawType)
        {
            case "branch":
                deposit = null;
                return BankPointType.Branch;
            
            case "atm":
                deposit = rawExtras.Contains("cash_deposit_atm");
                return BankPointType.Atm;
            
            default:
                throw new NotImplementedException();
        }
    }

    
    private enum BankPointType
    {
        Atm,
        Branch
    }
}