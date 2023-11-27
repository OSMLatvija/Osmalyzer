using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class ElviShopsAnalysisData : SimplePageShopListAnalysisData
{
    public override string Name => "Elvi Shops";

    public override string ReportWebLink => @"https://elvi.lv/elvi-veikali/";


    protected override string DataFileIdentifier => "shops-elvi";


    public override string DataFileName => cacheBasePath + DataFileIdentifier + @".html";

    public override string ShopListUrl => "https://elvi.lv/elvi-veikali/";

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    public override void Prepare()
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
                
        _shops = new List<ShopData>();
                
        foreach (Match match in matches)
        {
            double lat = double.Parse(match.Groups[2].ToString());
            double lon = double.Parse(match.Groups[3].ToString());
            string address = HtmlEntity.DeEntitize(match.Groups[1].ToString()).Trim();
                    
            address = Regex.Replace(address, @", ELVI veikals$", "");
            address = Regex.Replace(address, @", LV-\d{4}$", "");

            _shops.Add(new ShopData(address, new OsmCoord(lat, lon)));
        }
    }
}