using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class VenipakParcelLockerAnalysisData : ParcelLockerAnalysisData
{
    public override string Name => "Venipak Parcel Lockers";

    public override string ReportWebLink => @"https://venipak.com/lv/produkti-un-pakalpojumi/pickup-sutijumu-punkti/";

    protected override string DataFileIdentifier => "parcel-lockers-venipak";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");


    public override IEnumerable<ParcelLocker> ParcelLockers => _parcelLockers;


    private List<ParcelLocker> _parcelLockers = null!; // only null until prepared


    protected override void Download()
    {
        // list at https://venipak.com/lv/produkti-un-pakalpojumi/pickup-sutijumu-punkti/
        // query to get json data at https://go.venipak.lt/ws/get_pickup_points

        WebsiteBrowsingHelper.DownloadPage( // regular download fails on GitHub with SSL errors 
            "https://go.venipak.lt/ws/get_pickup_points", 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        _parcelLockers = new List<ParcelLocker>();

        string source = File.ReadAllText(DataFileName);
        
        // On remote it could be wrapped like this:
        // We were trying to parse: <html><head><meta name="color-scheme" content="light dark"><meta charset="utf-8"></head><body><pre>[{"id":3348,"name":"Venipak locker, Coop Venipak pakiautomaat","code":"12142751","address":"P\u00e4rn [1635718]...

        if (source.StartsWith("<"))
        {
            // Grab content between "<pre>", which seems what it is wrapped in
            source = source[
                (source.IndexOf("<pre>", StringComparison.Ordinal) + "<pre>".Length)
                ..
                source.LastIndexOf("</pre>", StringComparison.Ordinal)
            ];
        }

        dynamic[] content;
        
        try
        {
            content = JsonConvert.DeserializeObject<dynamic[]>(source)!;
        }
        catch (JsonException)
        {
            Console.WriteLine("JSON exception!");
            Console.WriteLine("We were trying to parse: " + (source.Length <= 200 ? source : source[..200] + " [" + (source.Length - 200) + "]..."));
            throw;
        }

        foreach (dynamic item in content)
        {
            // Expecting item to be:
            //{
            //    "id": 2342,
            //    "address": "Rīgas gatve 5",
            //    "city": "Ādaži",
            //    "zip": "2164",
            //    "country": "LV",
            //    "display_name": "Ādažu TC Apelsīns DROGAS Venipak Pickup punkts",
            //    "lat": "57.0712143",
            //    "lng": "24.3200930",
            //    "type": 1,
            //     ...
            //}
            
            string id = item.id;
            string name = item.display_name;
            string country = item.country;
            string address = item.address + ", " + item.city;
            double lat = double.Parse(item.lat.ToString());
            double lon = double.Parse(item.lng.ToString());
            int type = item.type;

            if (type == 3 && country == "LV")
            {
                _parcelLockers.Add(
                    new ParcelLocker(
                        "Venipak",
                        id,
                        name,
                        address,
                        new OsmCoord(lat, lon)
                    )
                );
            }
        }
    }
}