using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class UnisendParcelLockerAnalysisData : ParcelLockerAnalysisData
{
    public override string Name => "Unisend Parcel Lockers";

    public override string ReportWebLink => @"https://my.unisend.lv/map";

    protected override string DataFileIdentifier => "parcel-lockers-unisend";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");


    public override IEnumerable<ParcelLocker> ParcelLockers => _parcelLockers;
    
    public override IEnumerable<ParcelPickupPoint>? PickupPoints => null; // we don't have any pickup points
    public override PickupPointAmenity? PickupPointLocation => null; // we don't have any pickup points
    public override string? PickupPointLocationName => null; // we don't have any pickup points


    private List<ParcelLocker> _parcelLockers = null!; // only null until prepared


    protected override void Download()
    {
        // list at https://my.unisend.lv/map
        // query to get json data at https://api-esavitarna.post.lt/terminal/list?size=9999
        // Doesn't give answer unless Origin is specified

        WebsiteDownloadHelper.Download(
            "https://api-esavitarna.post.lt/terminal/list?size=9999",
            DataFileName,
            new Dictionary<string, string>
            {
                { "Origin", "https://my.unisend.lv" }
            }
        );
    }

    protected override void DoPrepare()
    {
        _parcelLockers = new List<ParcelLocker>();

        string source = File.ReadAllText(DataFileName);

        // Data structure: {"content":[  item, item, ...  ], ...}

        // Expecting item to be:
        // {
        //     "active": true,
        //     "id": "8560",
        //     "countryCode": "LV",
        //     "name": "ELVI",
        //     "provider": "udrop",
        //     "city": "Kuldīga",
        //     "address": "Gravas iela 1",
        //     "postalCode": "LV-3301",
        //     "latitude": "56.973548",
        //     "longitude": "21.957985",
        //     "workingHours": "00:24 h",
        //     "servicingHours": "I - V 10:00, VI 10:00",
        //     "comment": "Paštomatas kairėje įvažiavimo į stovi parkavimo aikštelę pusėje.",
        //     "multilingualComment": "The parcel locker is located on the parking, on the left side from main entrance.",
        //     "boxes": [
        //         "XLarge",
        //         "Large",
        //         "Medium",
        //         "Small",
        //         "XSmall"
        //     ],
        //     "created": "2024-04-24 08:30:07",
        //     "updated": "2024-05-31 20:30:02"
        //}

        dynamic content = JsonConvert.DeserializeObject<dynamic>(source)!;

        foreach (dynamic item in content.content)
        {
            string id = item.id;
            string name = item.name;
            string address = item.address;
            double lat = double.Parse(item.latitude.ToString());
            double lon = double.Parse(item.longitude.ToString());
            string country = item.countryCode;
            string provider = item.provider;

            if (country == "LV" && provider != "lv_pasts")
            {
                _parcelLockers.Add(
                    new ParcelLocker(
                        provider,
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