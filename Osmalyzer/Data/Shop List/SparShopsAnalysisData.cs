using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class SparShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Spar Shops";

    public override string ReportWebLink => @"https://spar.lv/veikali/";


    protected override string DataFileIdentifier => "shops-spar";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://spar.lv/wp-admin/admin-ajax.php?action=asl_load_stores&nonce=a995d933db&load_all=1&layout=1", 
            DataFileName
        );
    }
    
    protected override void DoPrepare()
    {
        _shops = new List<ShopData>();
        
        string source = File.ReadAllText(DataFileName);
        
        // {
        // 	"0": {
        // 		"id": "40",
        // 		"title": "Pārtikas veikals SPAR",
        // 		"description": "",
        // 		"street": "Mālpils iela 2A, LV-1013",
        // 		"city": "Rīga",
        // 		"state": "",
        // 		"postal_code": "",
        // 		"country": "Latvia",
        // 		"lat": "56.9661039",
        // 		"lng": "24.1262653",
        // 		"phone": "+37120209461",
        // 		"fax": "",
        // 		"email": "",
        // 		"website": "",
        // 		"logo_id": "0",
        // 		"path": null,
        // 		"marker_id": "153",
        // 		"description_2": "",
        // 		"open_hours": "{\"mon\":[\"08:00 - 22:00\"],\"tue\":[\"08:00 - 22:00\"],\"wed\":[\"08:00 - 22:00\"],\"thu\":[\"08:00 - 22:00\"],\"fri\":[\"08:00 - 22:00\"],\"sat\":[\"08:00 - 22:00\"],\"sun\":[\"09:00 - 21:00\"]}",
        // 		"ordr": "0",
        // 		"slug": "spar-veikals-r-ga-3",
        // 		"brand": "",
        // 		"special": "",
        // 		"categories": "22",
        // 		"days_str": "P., Ot., Tr., Ce., Pk., S., Sv."
        // 	}
        // }        

        dynamic[] content = JsonConvert.DeserializeObject<dynamic[]>(source)!;

        foreach (dynamic item in content)
        {
            string name = item.title;

            string address = item.street + ", " + item.city;

            OsmCoord coord = new OsmCoord(
                (double)item.lat, 
                (double)item.lng
            );
                
            _shops.Add(
                new ShopData(
                    name,
                    address,
                    coord
                )
            );
        }
    }
}