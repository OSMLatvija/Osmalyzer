namespace Osmalyzer;

[UsedImplicitly]
public class MegoShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Mego Shops";

    public override string ReportWebLink => @"https://mego.lv/kontakti/";


    protected override string DataFileIdentifier => "shops-mego";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://mego.lv/wp-admin/admin-ajax.php?action=store_locations", 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        string source = File.ReadAllText(DataFileName);
        
        MatchCollection matches = Regex.Matches(source, @"""location"":\{([^{}]+)\}");
        
        // {"id":555,"title":"A. \u010caka iela 55","location":{"address":"Aleksandra \u010caka iela 55, Centra rajons, R\u012bga, Latvija","lat":56.9547986,"lng":24.1331645,"zoom":14,"place_id":"ChIJ-y8DEjPO7kYRvRN4D7jjBuE","name":"Aleksandra \u010caka iela 55","street_number":"55","street_name":"Aleksandra \u010caka iela","city":"R\u012bga","post_code":"1011","country":"Latvija","country_short":"LV"},"locationIcon":"https:\/\/mego.lv\/wp-content\/themes\/mego_theme\/dist\/img\/map-marker-default.svg"}

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");
        
        _shops = new List<ShopData>();
                
        foreach (Match match in matches)
        {
            string location = match.Groups[1].Value;
            double lat = double.Parse(Regex.Match(location, @"""lat"":([\d\.]+)").Groups[1].Value);
            double lon = double.Parse(Regex.Match(location, @"""lng"":([\d\.]+)").Groups[1].Value);
            string address = Regex.Unescape(Regex.Match(location, @"""address"":""([^""]+)""").Groups[1].Value);
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