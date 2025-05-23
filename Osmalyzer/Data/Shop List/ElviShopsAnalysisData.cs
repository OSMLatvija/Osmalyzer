using HtmlAgilityPack;

namespace Osmalyzer;

[UsedImplicitly]
public class ElviShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Elvi Shops";

    public override string ReportWebLink => @"https://elvi.lv/elvi-veikali/";


    protected override string DataFileIdentifier => "shops-elvi";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://elvi.lv/elvi-veikali/", 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        // value: "Kursīši, Saldus nov., Bērzu iela 1-18, LV-3890, ELVI veikals",
        // data: [
        //         {
        //             id: 1643,
        //             link: "https://elvi.lv/veikali/kursisi-elvi-veikals/",
        //             lat: 56.512548,
        //             lng: 22.405646                                    }
        //   ]
        // },       
                
        string source = File.ReadAllText(DataFileName);
        
        MatchCollection matches = Regex.Matches(
            source, 
            @"value: ""([^""]+)"",\s*data: \[\s*\{\s*id:\s\d+,\s*link:\s\""[^""]+\"",\s*lat: (\d{2}.\d{1,15}),\s*lng:(\s\d{2}.\d{1,15})\s*"
        );
        
        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");
                
        _shops = new List<ShopData>();
                
        foreach (Match match in matches)
        {
            double lat = double.Parse(match.Groups[2].ToString());
            double lon = double.Parse(match.Groups[3].ToString());
            string address = HtmlEntity.DeEntitize(match.Groups[1].ToString()).Trim();
                    
            address = Regex.Replace(address, @", ELVI veikals$", "");
            address = Regex.Replace(address, @", LV-\d{4}$", "");

            _shops.Add(
                new ShopData(
                    "Elvi", 
                    address,
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
}