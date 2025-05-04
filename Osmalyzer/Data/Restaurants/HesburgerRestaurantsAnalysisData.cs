using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class HesburgerRestaurantAnalysisData : RestaurantListAnalysisData
{
    public override string Name => "Hesburger Restaurants";

    public override string ReportWebLink => @"https://www.hesburger.lv/restor--ni?country=lv";


    protected override string DataFileIdentifier => "restaurants-hesburger";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<RestaurantData> Restaurant => _restaurants;


    private List<RestaurantData> _restaurants = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.hesburger.lv/restor--ni?country=lv",
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        string source = File.ReadAllText(DataFileName);

        Match dataMatch = Regex.Match(
            source,
            @"var DATA=(\[.+\]);"
        );

        /* var DATA=[{"nimi":"Hesburger \u0100da\u017ei T\/c Apels\u012bns","kuntaNimi":"\u0100da\u017ei","latitude":"57.073210000000000","longitude":"24.318838000000000","url":"https:\/\/www.hesburger.lv\/restor--ni?tid=486","osoite":"T\/C Apels\u012bns, R\u012bgas gatve 5, LV-2164 \u0100da\u017ei, Latvija,  ","phone":"+37126434536","phone_extra":"","email":"adazi@hesburger.lv","aukioloajat":"<ul class='unstyled'><li>Katru dienu 9-22<\/li><\/ul>","poikkeusajat":"","poikkeustopic":"Uzman\u012bbu! \u012apa\u0161s darba laiks:","aukioloaikaLisatieto":" Auto kase atv\u0113rta no 08:00","tutustujaarvostele":"Apmekl\u0113 un nov\u0113rt\u0113","katsoravintola":"Skat\u012bt restor\u0101nu","arviota":"v\u0113rt\u0113jums","aukioloajattopic":"Darba laiks","palveluttopic":"Pakalpojumi","palvelut":{"konseptit":"","services":"Drive-in<br>WiFi<br>WC inval\u012bdiem<br>Bolt Food<br>","array":[1,6,7,36]},"tid":"486"}, */

        // TODO: check if phone, email & services match OSM

        string jsonString = dataMatch.Groups[1].ToString();

        using (JsonDocument doc = JsonDocument.Parse(jsonString))
        {
            JsonElement root = doc.RootElement;

            _restaurants = new List<RestaurantData>();

            foreach (JsonElement loc in root.EnumerateArray())
            {
                string name = loc.GetProperty("nimi").GetString()!;
                string? address = loc.GetProperty("osoite").GetString();
                double lat = double.Parse(loc.GetProperty("latitude").GetString()!);
                double lon = double.Parse(loc.GetProperty("longitude").GetString()!);

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