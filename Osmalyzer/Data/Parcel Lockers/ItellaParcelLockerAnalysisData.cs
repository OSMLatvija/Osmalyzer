using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class ItellaParcelLockerAnalysisData : ParcelLockerAnalysisData
{
    public override string Name => "Itella Parcel Lockers";

    public override string ReportWebLink => @"https://itella.lv/en/private-customer/parcel-locker-locations/";

    protected override string DataFileIdentifier => "parcel-lockers-itella";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");


    public override IEnumerable<ParcelLocker> ParcelLockers => _parcelLockers;

    public override IEnumerable<ParcelPickupPoint>? PickupPoints => null; // TODO: !!!!!!!!!!!!!!!!!


    private List<ParcelLocker> _parcelLockers = null!; // only null until prepared
    public override PickupPointAmenity? PickupPointLocation => PickupPointAmenity.Kiosk;
    public override string PickupPointLocationName => "Narvessen";


    protected override void Download()
    {
        // list at https://itella.lv/en/private-customer/parcel-locker-locations/
        // query to get json data at https://itella.lv/wp-admin/admin-ajax.php

        WebsiteDownloadHelper.DownloadPost(
            "https://itella.lv/wp-admin/admin-ajax.php",
            new[] {("action","ld_get_baltics_lockers")},
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        _parcelLockers = new List<ParcelLocker>();

        string source = File.ReadAllText(DataFileName);

        // Data structure: {"success": true,"data": [  item, item, ...  ]}

        // Expecting item to be:
        // {
        //     "ID": "2770",
        //     "place_id": "01008543",
        //     "name": "Smartpost kiosks Barona centrs",
        //     "city": "Rīga",
        //     "address": "Krišjāņa Barona iela 46, Rīga",
        //     "country": "lv",
        //     "postalcode": "1011",
        //     "availability": "P-Sv 08:00 - 22:00",
        //     "description": "Smartpost terminālis atrodas veikalā Barona centrs 0. stāvā, blakus taromatam",
        //     "latitude": "56.95390000",
        //     "longitude": "24.12770000",
        //     "pupcode": "010113203",
        //     "type": "ipb",
        //     "open": ""
        // },

        // types:
        //   ipb  - parcel locker
        //   pudo - pick-up point in store

        dynamic content = JsonConvert.DeserializeObject<dynamic>(source)!;

        foreach (dynamic item in content.data)
        {
            string id = item.place_id;
            string name = item.name;
            string address = item.address;
            double lat = double.Parse(item.latitude.ToString());
            double lon = double.Parse(item.longitude.ToString());
            string type = item.type;

            if (type == "ipb")
            {
                _parcelLockers.Add(
                    new ParcelLocker(
                        "Itella",
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