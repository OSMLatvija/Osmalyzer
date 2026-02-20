using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            "https://mans.pasts.lv/api/public/addresses/service_location?type[]=1&type[]=2&type[]=6&type[]=9&country[]=LV&search=&itemsPerPage=10000&page=1", 
            DataFileName,
            // headers
            new Dictionary<string, string>() {
                { "Accept", "application/json" } // this skips the (useless) hydra search API headers
            }
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
        
        foreach (dynamic item in content)
        {
            if ((string)item.countryCode != "LV")
                continue; // just in case - since not supplying type[] params actually returns LT and EE for some reason

            string label = (string)item.label;

            bool unisend = label.Contains("Unisend", StringComparison.InvariantCultureIgnoreCase);
            // e.g. "Rīga Biķernieku iela Rimi" vs "Unisend 8009 Rimi"

            bool clientCenter = label.Contains("Klientu centrs", StringComparison.InvariantCultureIgnoreCase);
            // e.g e.g. "Juglas pasta nodaļa" vs Klientu centrs Kauguri"

            LatviaPostItems.Add(
                new LatviaPostItem(
                    EntryTypeToItemType((int)item.type),
                    label,
                    (string)item.readableAddress,
                    (string)item.locationPostCode,
                    new OsmCoord((double)item.latitude, (double)item.longitude),
                    unisend,
                    clientCenter
                )
            );
        }
    }

    
    [Pure]
    private static LatviaPostItemType EntryTypeToItemType(int type)
    {
        switch (type)
        {
            case 1: // e.g. "Juglas pasta nodaļa" or Klientu centrs Kauguri"
            case 2: // e.g. "Iļģuciema pasta nodaļa" or "Klientu centrs Ziepniekkalns"
                return LatviaPostItemType.Office;
            // I have no idea what the difference between 1 and 2 is - both have offices and client centers
            // and none of the other data fields suggest any clear difference
            
            case 6: // e.g. "Rīga Biķernieku iela Rimi" or "Jelgava TC Valdeka" or "Unisend 8009 Rimi"
                return LatviaPostItemType.ParcelLocker;
            
            // 7 appear to be Unisend LT and EE parcel lockers
            // e.g. "Visalaukio g. 1, Vilnius, 08428" or "Ehitajate tee 107, Tallinn, 13511"
            
            case 9: // e.g. "Jelgava vēstuļu kastīte Nr.3007" or "Rīga vēstuļu kastīte Nr.5"
                return LatviaPostItemType.PostBox;

            default: throw new NotImplementedException();
        }
    }
}