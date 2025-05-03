using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class DPDParcelLockerAnalysisData : ParcelLockerAnalysisData
{
    public override string Name => "DPD Parcel Lockers";

    public override string ReportWebLink => @"https://dpdbaltics.com/PickupParcelShopData.json";

    protected override string DataFileIdentifier => "parcel-lockers-dpd";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");


    public override IEnumerable<ParcelLocker> ParcelLockers => _parcelLockers;

    public override IEnumerable<ParcelPickupPoint>? PickupPoints => null; // TODO: !!!!!!!!!!!!!!!!!
    public override PickupPointAmenity? PickupPointLocation => PickupPointAmenity.GasStation;
    public override string PickupPointLocationName => "Circle K";


    private List<ParcelLocker> _parcelLockers = null!; // only null until prepared


    protected override void Download()
    {
        // list at https://dpdbaltics.com/PickupParcelShopData.json as JSON

        WebsiteDownloadHelper.Download(
            ReportWebLink,
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        _parcelLockers = new List<ParcelLocker>();

        string source = File.ReadAllText(DataFileName);

        // Expecting item to be:
        // {
        //     "parcelShopId": "LV90003",
        //     "legacyShopId": "390003",
        //     "parcelShopType": "PickupStation",
        //     "companyName": "Paku Skapis TN Kurzeme",
        //     "companyShortName": "Paku Skapis TN Kurzeme",
        //     "street": "Lielā iela 13",
        //     "houseNo": "",
        //     "addressLine2": "",
        //     "countryCode": "LV",
        //     "zipCode": "3401",
        //     "city": "Liepāja",
        //     "longitude": "21.01122",
        //     "latitude": "56.50774",
        //     "openingHours": [
        //       {
        //         "weekday": "Monday",
        //         "openMorning": "0001",
        //         "closeMorning": "1200",
        //         "openAfternoon": "1200",
        //         "closeAfternoon": "2359"
        //       },
        //       {
        //         "weekday": "Tuesday",
        //         "openMorning": "0001",
        //         "closeMorning": "1200",
        //         "openAfternoon": "1200",
        //         "closeAfternoon": "2359"
        //       },
        //       {
        //         "weekday": "Wednesday",
        //         "openMorning": "0001",
        //         "closeMorning": "1200",
        //         "openAfternoon": "1200",
        //         "closeAfternoon": "2359"
        //       },
        //       {
        //         "weekday": "Thursday",
        //         "openMorning": "0001",
        //         "closeMorning": "1200",
        //         "openAfternoon": "1200",
        //         "closeAfternoon": "2359"
        //       },
        //       {
        //         "weekday": "Friday",
        //         "openMorning": "0001",
        //         "closeMorning": "1200",
        //         "openAfternoon": "1200",
        //         "closeAfternoon": "2359"
        //       },
        //       {
        //         "weekday": "Saturday",
        //         "openMorning": "0001",
        //         "closeMorning": "1200",
        //         "openAfternoon": "1200",
        //         "closeAfternoon": "2359"
        //       },
        //       {
        //         "weekday": "Sunday",
        //         "openMorning": "0001",
        //         "closeMorning": "1200",
        //         "openAfternoon": "1200",
        //         "closeAfternoon": "2359"
        //       }
        //     ]
        //   },

        dynamic content = JsonConvert.DeserializeObject<dynamic>(source)!;

        foreach (dynamic item in content)
        {
            string country = item.countryCode; // "LV", "EE" or "LT"
            
            if (country != "LV")
                continue;

            string id = item.parcelShopId;
            
            string type = item.parcelShopType; // all are "PickupStation

            if (type != "PickupStation")
                throw new NotImplementedException("Unknown DPD parcel locker type: " + type);
            
            string shop = item.companyName; // seems to be the name of the shop where the locker is attached
            // e.g. "Paku Skapis Vesko Carnikava" or few as "PS Barona Centrs (NOSLOGOTS)"
            // There is also `companyShortName`, but it's exactly the same as `companyName` for all entries

            string street = item.street; // includes house number, actual "houseNo" field is never used
            string city = item.city;
            string addressLine2 = item.addressLine2; // very few have this, seems like some sort of qualifier for city, but some are city itself
            string zipCode = item.zipCode; // the digit part of the postal code
            
            double lat = double.Parse(item.latitude.ToString());
            double lon = double.Parse(item.longitude.ToString());
            
            // Opening hours are all the same - 24/7, no real point in parsing
            

            // Make address

            string address = 
                street +
                (addressLine2 != "" && addressLine2 != city ? ", " + addressLine2 : "") +
                ", " + city +
                ", LV-" + zipCode;
            
            // Strip useless prefix/suffix from name
            
            string[] strippablePrefixes = { "Paku Skapis ", "PS " }; 
            
            foreach (string prefix in strippablePrefixes)
            {
                if (shop.StartsWith(prefix))
                {
                    shop = shop[prefix.Length..];
                    break;
                }
            }

            string strippableSuffix = " (NOSLOGOTS)"; // as in "busy" in Latvian, presumably (close to) full
            if (shop.EndsWith(strippableSuffix))
                shop = shop[..^strippableSuffix.Length];
            
            
            _parcelLockers.Add(
                new ParcelLocker(
                    "DPD",
                    id,
                    shop,
                    address,
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
}