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


    public override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://aibe.lv/veikali/", 
            DataFileName
        );
    }

    public override void Prepare()
    {
        string source = File.ReadAllText(DataFileName);
        
        MatchCollection matches = Regex.Matches(
            source, 
            @"\[([\d\.]+),([\d\.]+),""([^""]+)"",[^\]]+\]," // todo: can capture more, but it's all cryptic data
        );
        
        // [56.95491,24.19849,"Dzelzavas iela 76, R\u012bga","V-980, Ankami SIA","08:00","22:00","08:00","22:00","08:00","22:00","R\u012bga","riga","partner","false","false","0","Partneris","29619824","","riga","","","","","","","riga"]

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");
        
        _shops = new List<ShopData>();
                
        foreach (Match match in matches)
        {
            double lat = double.Parse(match.Groups[1].ToString());
            double lon = double.Parse(match.Groups[2].ToString());
            string address = Regex.Unescape(match.Groups[3].ToString());

            _shops.Add(
                new ShopData(
                    "Aibe", 
                    address, 
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
}