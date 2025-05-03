using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class OmnivaParcelLockerAnalysisData : ParcelLockerAnalysisData
{
    public override string Name => "Omniva Parcel Lockers";

    public override string ReportWebLink => @"https://www.omniva.lv/privats/adreses";

    protected override string DataFileIdentifier => "parcel-lockers-omniva";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");


    public override IEnumerable<ParcelLocker> ParcelLockers => _parcelLockers;
    
    public override IEnumerable<ParcelPickupPoint>? PickupPoints => null; // we don't have any pickup points


    private List<ParcelLocker> _parcelLockers = null!; // only null until prepared
    public override PickupPointAmenity? PickupPointLocation => null; // we don't have any pickup points
    public override string? PickupPointLocationName => null; // we don't have any pickup points


    protected override void Download()
    {
        // Map at https://www.omniva.lv/privats/adreses
        // List at https://www.omniva.lv/locations.json

        WebsiteDownloadHelper.Download(
            "https://www.omniva.lv/locations.json", 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        _parcelLockers = new List<ParcelLocker>();

        string source = File.ReadAllText(DataFileName);
        // Expecting item to be:
        //{
        // "ZIP": "9595",
        // "NAME": "Aglonas TOP pakomāts",
        // "TYPE": "0",
        // "A0_NAME": "LV",
        // "A1_NAME": "Preiļu novads",
        // "A2_NAME": "Aglonas pagasts",
        // "A3_NAME": "Aglona",
        // "A4_NAME": "",
        // "A5_NAME": "Somersētas iela",
        // "A6_NAME": "",
        // "A7_NAME": "33",
        // "A8_NAME": "",
        // "X_COORDINATE": "27.007230",
        // "Y_COORDINATE": "56.131607",
        // "SERVICE_HOURS": "",
        // "TEMP_SERVICE_HOURS": "",
        // "TEMP_SERVICE_HOURS_UNTIL": "",
        // "TEMP_SERVICE_HOURS_2": "",
        // "TEMP_SERVICE_HOURS_2_UNTIL": "",
        // "comment_est": "",
        // "comment_eng": "",
        // "comment_rus": "",
        // "comment_lav": "",
        // "comment_lit": "",
        // "MODIFIED": "2024-03-26T14:51:43.103+02:00"
        //}

        dynamic[] content = JsonConvert.DeserializeObject<dynamic[]>(source)!;

        foreach (dynamic item in content)
        {
            string name = item.NAME;
            string country = item.A0_NAME;
            string address = item.A5_NAME + item.A6_NAME + ", " + item.A7_NAME + item.A8_NAME;
            double lat = double.Parse(item.Y_COORDINATE.ToString());
            double lon = double.Parse(item.X_COORDINATE.ToString());

            if (country == "LV")
            {
                _parcelLockers.Add(
                    new ParcelLocker(
                        "Omniva",
                        null,
                        name,
                        address,
                        new OsmCoord(lat, lon)
                    )
                );
            }
        }
    }
}