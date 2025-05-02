using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Osmalyzer;

[UsedImplicitly]
public class LuluRestaurantAnalysisData : RestaurantListAnalysisData
{
    public override string Name => "Lulu Restaurants";

    public override string ReportWebLink => @"https://www.lulu.lv/picerijas";


    protected override string DataFileIdentifier => "restaurants-lulu";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<RestaurantData> Restaurant => _restaurants;


    private List<RestaurantData> _restaurants = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.lulu.lv/picerijas",
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        string source = File.ReadAllText(DataFileName);

        MatchCollection matches = Regex.Matches(
            source,
            @"https:\/\/maps\.google\.com\?q=(\d\d\.\d+),(\d\d\.\d+)"
        );

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");

        _restaurants = new List<RestaurantData>();

        foreach (Match match in matches)
        {
            double lat = double.Parse(Regex.Unescape(match.Groups[1].ToString()));
            double lon = double.Parse(Regex.Unescape(match.Groups[2].ToString()));

            string display = "";


            _restaurants.Add(
                new RestaurantData(
                    "Lulu",
                    !string.IsNullOrWhiteSpace(display) ? display : null,
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
}