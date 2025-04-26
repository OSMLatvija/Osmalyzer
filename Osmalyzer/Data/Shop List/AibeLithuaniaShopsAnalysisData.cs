using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Osmalyzer;

[UsedImplicitly]
[DisabledAnalyzer]
public class AibeLithuaniaShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Aibė (Lithuania) Shops";

    public override string ReportWebLink => @"https://aibe.lt/parduotuves/";


    protected override string DataFileIdentifier => "shops-aibe-lithuania";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://aibe.lt/parduotuves/", 
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
        
        // [56.0260816,21.0833377,"Pu\u0161yno g. 6, \u0160ilgali\u0173 k.","P-1967 Us\u0117nai, UAB","","","","","","","Taurag\u0117s apskritis","taurages-apskritis","shop","false","false","0","Parduotuv\u0117s","","","taurages-apskritis","","","","","","","silgaliu-k"]

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
                        "Aibė",
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