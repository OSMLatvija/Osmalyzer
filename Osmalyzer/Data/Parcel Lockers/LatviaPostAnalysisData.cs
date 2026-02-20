using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpKml.Dom;

namespace Osmalyzer;

[UsedImplicitly]
public class LatviaPostAnalysisData : AnalysisData, IParcelLockerListProvider, IUndatedAnalysisData
{
    public override string Name => "Latvijas Pasts";

    public override bool NeedsPreparation => true;

    public override string ReportWebLink => @"https://mans.pasts.lv/postal-network";

    protected override string DataFileIdentifier => "latvia-post";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");

    public List<LatviaPostItem> LatviaPostItems { get; private set; } = null!; // only null until prepared

    public IEnumerable<ParcelLocker> ParcelLockers => LatviaPostItems
                                                      .Where(i => i.ItemType == LatviaPostItemType.ParcelLocker)
                                                      .Select(i => i.AsParcelLocker());

    IEnumerable<ParcelPickupPoint>? IParcelLockerListProvider.PickupPoints => null;

    PickupPointAmenity? IParcelLockerListProvider.PickupPointLocation => null;

    string? IParcelLockerListProvider.PickupPointLocationName => null;

    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://mans.pasts.lv/api/public/addresses/service_location?type[]=1&type[]=2&type[]=6&country[]=LV&search=&itemsPerPage=10000&page=1", 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        LatviaPostItems = [ ];

        string source = File.ReadAllText(DataFileName);

        // {
        //   "@id": "/api/addresses/service_location/3122",
        //   "@type": "ServiceLocation",
        //   "id": "3122",
        //   "type": 2,
        //   "countryCode": "LV",
        //   "postCode": "LV-1055",
        //   "readableAddress": "Cementa iela 12, Rīga, LV-1055",
        //   "label": "Iļģuciema pasta nodaļa",
        //   "latitude": 56.969091258159,
        //   "longitude": 24.061597193063,
        //   "workingHours": {
        //     "@type": "WorkingHours",
        //     "monday": "09:00-18:00",
        //     "tuesday": "09:00-18:00",
        //     "wednesday": "09:00-18:00",
        //     "thursday": "09:00-18:00",
        //     "friday": "09:00-18:00",
        //     "saturday": "-",
        //     "sunday": "-"
        //   },
        //   "workingHoursCombined": [],
        //   "outside": false,
        //   "status": 1,
        //   "info": "",
        //   "locationPostCode": "LV-1055",
        //   "officeFcd": "LV1055"
        // }
        dynamic content;
        
        try
        {
            content = JsonConvert.DeserializeObject(source)!;
        }
        catch (JsonException)
        {
            Console.WriteLine("JSON exception!");
            Console.WriteLine("We were trying to parse: " + (source.Length <= 200 ? source : source[..200] + " [" + (source.Length - 200) + "]..."));
            throw;
        }
        JArray items = content["hydra:member"];
        foreach(dynamic item in items)
        {
            LatviaPostItems.Add(
                new LatviaPostItem(
                    EntryTypeToItemType((int)item.type),
                    (string)item.label,
                    (string)item.readableAddress,
                    (string)item.locationPostCode,
                    new OsmCoord((double)item.latitude, (double)item.longitude)
                )
            );
        }
    }

    
    [Pure]
    private static LatviaPostItemType EntryTypeToItemType(int type)
    {
        switch (type)
        {
            case 2: return LatviaPostItemType.PostBox;
            case 1: return LatviaPostItemType.Office;
            case 6: return LatviaPostItemType.ParcelLocker;
            case 7: return LatviaPostItemType.Unisend;

            default: throw new NotImplementedException();
        }
    }
}