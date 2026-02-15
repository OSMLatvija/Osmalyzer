using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class CaffeineRestaurantAnalysisData : RestaurantListAnalysisData
{
    public override string Name => "Caffeine Restaurants";

    public override string ReportWebLink => @"https://caffeine.lv/kafejnicas/";


    protected override string DataFileIdentifier => "restaurants-caffeine";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<RestaurantData> Restaurant => _restaurants;


    private List<RestaurantData> _restaurants = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://caffeine.lv/kafejnicas/",
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        string source = File.ReadAllText(DataFileName);

        Match dataMatch = Regex.Match(
            source,
            @"""places"":(\[.+?\])\,"
        );

        /* "places":[{"id":"1","title":"Aud\u0113ju iela 15","address":"Aud\u0113ju iela 15, Central District, Riga, Latvia","source":"manual","location":{"icon":"http:\/\/caffeine.lv\/wp-content\/uploads\/2021\/05\/caffeine-logo-30x30-1.png","lat":"56.9472","lng":"24.1127504","city":"R\u012bga","country":"Latvia","onclick_action":"marker","open_new_tab":"yes","postal_code":"1050","draggable":false,"infowindow_default_open":false,"infowindow_disable":true,"zoom":5,"extra_fields":{"listorder":0}}}, */

        string jsonString = dataMatch.Groups[1].ToString();

        using (JsonDocument doc = JsonDocument.Parse(jsonString))
        {
            JsonElement root = doc.RootElement;

            _restaurants = new List<RestaurantData>();

            foreach (JsonElement place in root.EnumerateArray())
            {
                JsonElement loc = place.GetProperty("location");

                string name = "Caffeine";
                string? address = place.GetProperty("address").GetString();
                double lat = double.Parse(loc.GetProperty("lat").GetString()!);
                double lon = double.Parse(loc.GetProperty("lng").GetString()!);

                _restaurants.Add(
                    new RestaurantData(
                        name,
                        !string.IsNullOrWhiteSpace(address) ? address : null,
                        new OsmCoord(lat, lon)
                    )
                );
            }
        }

    }
}