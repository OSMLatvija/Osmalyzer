namespace Osmalyzer;

[UsedImplicitly]
public class MegoShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Mego Shops";

    public override string ReportWebLink => @"https://mego.lv/kontakti";


    protected override string DataFileIdentifier => "shops-mego";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://mego.lv/kontakti", 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        string source = File.ReadAllText(DataFileName);
        
        MatchCollection matches = Regex.Matches(
            source, 
            @"\{""x"":""([^""]+)"",""y"":""([^""]+)"",""city_id"":\d+,""shop_id"":\d+,""address"":""([^""]+)"",""info"":""([^""]+)""\}"
        );
        
        // {"x":"56.9321559","y":"24.2017965","city_id":1,"shop_id":1,"address":"A. Saharova iela 2","info":"\u003cdiv class='map__item'\u003e\n            \u003cspan class='map__item-title'\u003e\n              \u003ci class='fa fa-recycle' title=Taromāts\u003e\u003c/i\u003e\n              \u003cspan\u003eA. Saharova iela 2\u003c/span\u003e\n            \u003c/span\u003e\n            \u003cspan class='map__item-contacts\u003e\u003c/span\u003e\n            \u003cspan class='map__item-content\u003e\u003cp\u003eDarba dienās\u0026nbsp;07:00\u0026ndash;23:00\u003cbr /\u003e\r\nSest. 07:00\u0026ndash;23:00\u003cbr /\u003e\r\nSv. 07:00\u0026ndash;23:00\u003c/p\u003e\r\n\u003c/span\u003e\n          \u003c/div\u003e"}

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");
        
        _shops = new List<ShopData>();
                
        foreach (Match match in matches)
        {
            double lat = double.Parse(match.Groups[1].ToString());
            double lon = double.Parse(match.Groups[2].ToString());
            string address = Regex.Unescape(match.Groups[3].ToString());
            //string info = Regex.Unescape(match.Groups[4].ToString()); // todo: has some extra details

            _shops.Add(
                new ShopData(
                    "Mego", 
                    address, 
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
}