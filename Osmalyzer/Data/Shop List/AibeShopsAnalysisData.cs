using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class AibeShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Aibe Shops";

    public override string ReportWebLink => @"https://aibe.lv/veikali/";


    protected override string DataFileIdentifier => "shops-aibe";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://aibe.lv/veikali/", 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        string source = File.ReadAllText(DataFileName);
        
        MatchCollection matches = Regex.Matches(
            source, 
            @"\[([\d\.]+),([\d\.]+),""([^""]+)"",""([^""]+)"",[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,""([^""]+)"",[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+\]"
        );
        
        // [56.95491,24.19849,"Dzelzavas iela 76, R\u012bga","V-980, Ankami SIA","08:00","22:00","08:00","22:00","08:00","22:00","R\u012bga","riga","partner","false","false","0","Partneris","29619824","","riga","","","","","","","riga"]

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");
        
        _shops = new List<ShopData>();

        foreach (Match match in matches)
        {
            double lat = double.Parse(match.Groups[1].ToString());
            
            double lon = double.Parse(match.Groups[2].ToString());
            
            string address = Regex.Unescape(match.Groups[3].ToString().Trim());
            
            string? company = Regex.Unescape(match.Groups[4].ToString().Trim());
            if (company == "")
            {
                company = null;
            }
            else
            {
                company = Regex.Replace(company, @"V-\d+,\s*", ""); // prefix for something unknown "V-960, Falko-2 SIA"
                if (company.ToLower().Contains("aibe")) company = null; // i.e. self - "Aibe pārtika SIA";
            }

            ShopType shopType = ShopTypeFromRaw(match.Groups[5].ToString().Trim());

            if (shopType is ShopType.Shop or ShopType.Partner) // todo: gas and bar? these have different tagging in osm
            {
                _shops.Add(
                    new ShopData(
                        "Aibe",
                        (company != null ? company + ", " : "") + address,
                         new OsmCoord(lat, lon)
                    )
                );
            }
        }
    }

    
    private ShopType ShopTypeFromRaw(string rawType)
    {
        return rawType switch
        {
            "shop"    => ShopType.Shop,
            "Veikals" => ShopType.Shop, // looks like a manual input error
            "partner" => ShopType.Partner,
            "bar"     => ShopType.Bar,
            "gas"     => ShopType.Gas,

            _ => throw new ArgumentOutOfRangeException(nameof(rawType), rawType, null)
        };
    }

    
    private enum ShopType
    {
        Shop,
        Partner,
        Bar,
        Gas
    }
}