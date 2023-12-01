using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class VeskoShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Vesko Shops";

    public override string ReportWebLink => @"https://veskoveikals.lv/";


    protected override string DataFileIdentifier => "shops-vesko";


    public override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://veskoveikals.lv/", 
            DataFileName
        );
    }

    
    public override void Prepare()
    {
        string source = File.ReadAllText(DataFileName);
        
        MatchCollection matches = Regex.Matches(
            source, 
            @"\{""x"":""([^""]+)"",""y"":""([^""]+)"",""city_id"":\d+,""type"":""([^""]+)"",""info"":""([^""]+)""\}"
        );
        
        // {"x":"56.409534","y":"24.201303","city_id":5,"type":"shop","info":"\u003cdiv class='point-content'\u003eBauska, Salātu 21a\u003c/br\u003e\n        \u003cspan\u003e\u003c/span\u003e\u003c/div\u003e"}

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");
        
        _shops = new List<ShopData>();

        foreach (Match match in matches)
        {
            double lat = double.Parse(match.Groups[1].ToString());
            
            double lon = double.Parse(match.Groups[2].ToString());
            
            ShopType shopType = ShopTypeFromRaw(match.Groups[3].ToString().Trim());

            string address = Regex.Unescape(match.Groups[4].ToString().Trim());
            address = Regex.Match(address, @"<div[^>]+?>([^<]+?)<").Groups[1].ToString();
            // <div class='point-content'>Carnikava, Tulpju 1a</br>
            // <span></span></div>

            _shops.Add(
                new ShopData(
                    "Vesko" + (shopType == ShopType.MiniShop ? " mini" : ""),
                    address,
                    new OsmCoord(lat, lon)
                )
            );
        }
    }

    
    private ShopType ShopTypeFromRaw(string rawType)
    {
        return rawType switch
        {
            "shop"    => ShopType.Shop,
            "mini_shop" => ShopType.MiniShop,

            _ => throw new ArgumentOutOfRangeException(nameof(rawType), rawType, null)
        };
    }

    
    private enum ShopType
    {
        Shop,
        MiniShop
    }
}