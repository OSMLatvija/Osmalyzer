namespace Osmalyzer;

[UsedImplicitly]
public class LuminorPointAnalysisData : BankPointAnalysisData
{
    public override string Name => "Luminor Points";

    public override string ReportWebLink => @"https://www.luminor.lv/lv/musu-tikls";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "luminor-points";


    protected override void Download()
    {
        WebsiteBrowsingHelper.DownloadPage(
            "https://www.luminor.lv/lv/musu-tikls", 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".html"),
            true,
            null,
            new WaitForElementOfClass("contact-map") // loads JS garbage first that loads the rest of the page
        );
    }

    protected override void DoPrepare()
    {
        string data = File.ReadAllText(Path.Combine(CacheBasePath, DataFileIdentifier + @".html"));

        Points = new List<BankPoint>();
        
        MatchCollection matches = Regex.Matches(data, @"""\d+"":{""title"":""([^""]+)"",""geolocation"":{""lat"":""([^""]+)"",""lng"":""([^""]+)""},""address"":""([^""]+)"",""pin_icon"":""([^""]+)"",");

        // "13488":{"title":"TC B\u0112RNU PASAULE","geolocation":{"lat":"56.95712","lng":"24.133350000000064"},"address":"Mat\u012bsa iela 25","pin_icon":"cash-in-out","object_types":[71],"town":{"county":169,"town":159}},

        if (matches.Count == 0)
            throw new Exception("Did not find items on webpage");
        
        foreach (Match match in matches)
        {
            BankPointType type = RawTypeToPointType(
                match.Groups[5].ToString(),
                out bool? deposit
            );
            
            OsmCoord coord = new OsmCoord(
                double.Parse(match.Groups[2].ToString()),
                double.Parse(match.Groups[3].ToString())
            );

            string name = Regex.Unescape(match.Groups[1].ToString());

            string address = Regex.Unescape(match.Groups[4].ToString());

            BankPoint point = type switch
            {
                BankPointType.Branch => new BankBranchPoint("Luminor", name, address, coord),
                BankPointType.Atm    => new BankAtmPoint("Luminor", name, address, coord, deposit),

                _ => throw new ArgumentOutOfRangeException()
            };
            
            Points.Add(point);
        }
    }
    
    
    [Pure]
    private static BankPointType RawTypeToPointType(string rawType, out bool? deposit)
    {
        switch (rawType)
        {
            case "branch":
            case "consultation":
            case "partner":
                deposit = null;
                return BankPointType.Branch;
            
            case "cash-in-out":
                deposit = true;
                return BankPointType.Atm;
            
            case "cash-out":
                deposit = false;
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