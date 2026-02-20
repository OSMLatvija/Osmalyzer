namespace Osmalyzer;

[UsedImplicitly]
public class MaximaShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Maxima Shops";

    public override string ReportWebLink => @"https://www.maxima.lv/veikalu-kedes";


    protected override string DataFileIdentifier => "shops-maxima";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared

    
    protected override void Download()
    {
        // list at https://www.maxima.lv/veikalu-kedes
        // json query at https://www.maxima.lv/ajax/shopsnetwork/map/getCities
        
        // Default POST query with all items:
        // {
        //     "cityId": "0",
        //     "shopType": "",
        //     "mapId": "1",
        //     "shopId": "",
        //     "language": "lv_lv",
        //     "certificate": ""
        // }

        WebsiteDownloadHelper.DownloadPostJson(
            @"https://www.maxima.lv/ajax/shopsnetwork/map/getCities",
            [ ("cityId", "0"), ("shopType", ""), ("mapId", "1"), ("shopId", ""), ("language", "lv_lv"), ("certificate", "") ],
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        string source = File.ReadAllText(DataFileName);
        
        MatchCollection matches = Regex.Matches(
            source, 
            @"\{([^\}]+)\}"
        );
        
        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");
        
        _shops = new List<ShopData>();
                
        foreach (Match match in matches)
        {
            string raw = match.Groups[1].ToString();

            // {
            //     "lat": "56.958497",
            //     "lng": "24.125626",
            //     "info": "\r\n\r\n\u003cdiv class\u003d\"shop\" style\u003d\"background-image:url(\u0027/images/front/icons/shop1.png\u0027)\"\u003e\r\n    \u003cb\u003eAdrese\u003c/b\u003e: Brivibas iela 78, Riga\u003cbr\u003e\r\n    \u003cb\u003eTalrunis\u003c/b\u003e: 8 000 2020\u003cbr\u003e\r\n    \u003c!--        \u003cb\u003eFax\u003c/b\u003e: 67843045\u003cbr\u003e\r\n    --\u003e\r\n    \u003cb\u003eDarba laiks\u003c/b\u003e: 8.00-22.00    \u003cp class\u003d\u0027info\u0027\u003e\u003cbr\u003e\u003cbr\u003e\u003cb\u003e\u003cp style\u003d\"color: #ff1900\"\u003e\tVeikals darbojas sarkanaja režima. Pieejams visiem pircejiem\t\u003c/p\u003e\u003c/b\u003e\u003c/p\u003e\r\n\u003c/div\u003e\r\n\r\n\u003c!--\u003cdiv class\u003d\"serv\"\u003e\u003cp\u003eParduotuveje rasite\u003c/p\u003e\u003cimg src\u003d\"assets/img/map/services/swed.png\" width\u003d\"0\" height\u003d\"0\"\u003e\u003cimg\r\n        src\u003d\"assets/img/map/services/dnblogo2012.jpg\" width\u003d\"0\" height\u003d\"0\"\u003e\u003cimg\r\n        src\u003d\"assets/img/map/services/vilniaus-bankas-logo2.jpg\" width\u003d\"0\" height\u003d\"0\"\u003e\u003cimg\r\n        src\u003d\"assets/img/map/services/vaistine.png\" width\u003d\"0\" height\u003d\"0\"\u003e\u003cimg\r\n        src\u003d\"assets/img/map/services/keturiu-banku_1.jpg\" width\u003d\"0\" height\u003d\"0\"\u003e\u003c/div\u003e--\u003e\r\n",
            //     "tag": "abcf790c307391b4214df700051470d2.png",
            //     "address": "Brivibas iela 78, Riga",
            //     "time": "8.00-22.00",
            //     "id": "2"
            // },
            
            string address = Regex.Unescape(Regex.Match(raw, @"""address"":""([^""]+)""").Groups[1].ToString());
            double lat = double.Parse(Regex.Unescape(Regex.Match(raw, @"""lat"":""([^""]+)""").Groups[1].ToString()));
            double lon = double.Parse(Regex.Unescape(Regex.Match(raw, @"""lng"":""([^""]+)""").Groups[1].ToString()));

            _shops.Add(
                new ShopData(
                    "Maxima", // todo: X XX XXX variant?
                    address, 
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
}