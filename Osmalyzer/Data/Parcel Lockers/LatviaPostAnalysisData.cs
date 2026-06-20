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
                                                      .Where(i => i.ItemType == LatviaPostItemType.ParcelLocker &&
                                                                  !i.Unisend) // don't want unisends, they double-map with Unisend data and also aren't tagged on OSM as LP lockers
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
        
        // todo: figure out if post code can be included as ref or something
        
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

            string? openingHours = ParseOpeningHours(item.workingHours);
            
            bool indoors = !(bool)item.outside;
            
            LatviaPostItems.Add(
                new LatviaPostItem(
                    EntryTypeToItemType((int)item.type),
                    label,
                    (string)item.readableAddress,
                    (string)item.locationPostCode,
                    new OsmCoord((double)item.latitude, (double)item.longitude),
                    unisend,
                    openingHours,
                    indoors
                )
            );
        }
    }

    
    [Pure]
    private static string? ParseOpeningHours(dynamic workingHours)
    {
        if (workingHours == null)
            return null; // hours not provided
            
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
        
        List<string> days = [ ];
        
        string monday = ParseTimes(workingHours.monday);
        if (monday != null) days.Add("Mo " + monday);
        
        string tuesday = ParseTimes(workingHours.tuesday);
        if (tuesday != null) days.Add("Tu " + tuesday);
        
        string wednesday = ParseTimes(workingHours.wednesday);
        if (wednesday != null) days.Add("We " + wednesday);
        
        string thursday = ParseTimes(workingHours.thursday);
        if (thursday != null) days.Add("Th " + thursday);
        
        string friday = ParseTimes(workingHours.friday);
        if (friday != null) days.Add("Fr " + friday);
        
        string saturday = ParseTimes(workingHours.saturday);
        if (saturday != null) days.Add("Sa " + saturday);
        
        string sunday = ParseTimes(workingHours.sunday);
        if (sunday != null) days.Add("Su " + sunday);
        
        if (days.Count == 0)
            return null; // no hours provided (e.g. all days are "-")
        
        List<string> merged = OsmOpeningHoursHelper.MergeSequentialWeekdaysWithSameTimes(days);

        return string.Join("; ", merged);
        
        // Note that "PH off" is not implicit - some work on some public holidays with possible shortened times 

        
        static string? ParseTimes(dynamic? times)
        {
            if (times == null)
                return null; // hours not provided
            
            if (times == "-")
                return "Off"; // explicitly closed
            
            // 09:00-18:00
            // 8:00-11:00
            // " 08:00-22:00" - space
            Match match = Regex.Match(times.ToString().Trim(), @"^(\d{1,2})[:\.](\d{2})-(\d{1,2})[:\.](\d{2})$");
            if (!match.Success)
                throw new Exception("Unexpected time format: " + times);
            
            return match.Groups[1].Value.PadLeft(2, '0') + ":" + match.Groups[2].Value + "-" +
                   match.Groups[3].Value.PadLeft(2, '0') + ":" + match.Groups[4].Value;
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