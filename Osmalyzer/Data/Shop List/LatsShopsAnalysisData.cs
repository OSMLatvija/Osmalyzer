using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class LatsShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "LaTS Shops";

    public override string ReportWebLink => @"https://www.latts.lv/lats-veikali";

        
    protected override string DataFileIdentifier => "shops-lats";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.latts.lv/lats-veikali", 
            DataFileName
        );
    }

    public override void Prepare()
    {
        // markers.push({
        //     coordinates: {lat: 56.9069266, lng: 24.1982285},
        //     image: '/img/map-marker.png',
        //     title: 'Veikals',
        //     info: '<div id="contentMap">' +
        //         '<h3>Veikals:</h3>' +
        //         '<div id="bodyContentMap">' +
        //                                         '<p>Plostu iela 29, Rīga, LV-1057</p>' +
        //                                         '<p>Tel.nr.: 67251697</p>' +
        //                                         '<p>Šodien atvērts 9:00-19:00</p>' +
        //                     '</div></div>'
        // });          

        string source = File.ReadAllText(DataFileName);
        
        MatchCollection matches = Regex.Matches(
            source, 
            @"markers\.push\({\s*coordinates: {lat: (\d{2}.\d{1,8}), lng: (\d{2}.\d{1,8})},[^}]*?<p>([^<]+)</p>"
        );
        
        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");
                
        _shops = new List<ShopData>();
                
        foreach (Match match in matches)
        {
            double lat = double.Parse(match.Groups[1].ToString());
            double lon = double.Parse(match.Groups[2].ToString());
            string address = HtmlEntity.DeEntitize(match.Groups[3].ToString()).Trim();
                    
            address = Regex.Replace(address, @", LV-\d{4}$", "");
                    
            _shops.Add(
                new ShopData(
                    "LaTS",
                    address, 
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
}