using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class LatviaPostAnalysisData : AnalysisData, IParcelLockerListProvider
{
    public override string Name => "Latvijas Pasts";

    public override bool NeedsPreparation => true;

    public override string ReportWebLink => @"https://www.pasts.lv/lv/kategorija/pasta_nodalas/";

    protected override string DataFileIdentifier => "latvia-post";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");

    public List<LatviaPostItem> LatviaPostItems { get; private set; } = null!; // only null until prepared

    public IEnumerable<ParcelLocker> ParcelLockers => LatviaPostItems
                                                      .Where(i => i.ItemType == LatviaPostItemType.ParcelLocker)
                                                      .Select(i => i.AsParcelLocker());

    public IEnumerable<ParcelPickupPoint> PickupPoints => LatviaPostItems
                                                          .Where(i => i.ItemType == LatviaPostItemType.CircleK)
                                                          .Select(i => i.AsPickupPointLocker());

    public PickupPointAmenity? PickupPointLocation => PickupPointAmenity.GasStation;
    public string PickupPointLocationName => "Circle K";


    protected override void Download()
    {
        // list at https://pasts.lv/lv/kategorija/pasta_nodalas/
        // direct query to get json data at https://pasts.lv/ajax/module:post_office/
        // but this isn't reliable and can be blocked as "attack", then ip-filtered, even just accessing it a few times
        // accessing this without actually going through the main page seems to trigger their protection stuff

        WebsiteBrowsingHelper.DownloadPage( // direct download can fail both locally and on remote
            "https://pasts.lv/ajax/module:post_office/", 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        LatviaPostItems = new List<LatviaPostItem>();

        string source = File.ReadAllText(DataFileName);

        // Due to headless browsing, it could/will be wrapped in boilerplate HTML that we need to strip
        source = WebsiteBrowsingHelper.TryUnwrapJsonFromBoilerplateHtml(source);

        dynamic content;
        
        try
        {
            content = JsonConvert.DeserializeObject<dynamic>(source)!;
        }
        catch (JsonException)
        {
            Console.WriteLine("JSON exception!");
            Console.WriteLine("We were trying to parse: " + (source.Length <= 200 ? source : source[..200] + " [" + (source.Length - 200) + "]..."));
            throw;
        }
        
        
        string serialisedArray = content.all;

        // Format of data: {"all":"<serialised json array of POIs>","count":1287}

        dynamic[] jsonItems = JsonConvert.DeserializeObject<dynamic[]>(serialisedArray)!;

        foreach (dynamic item in jsonItems)
        {
            // Expecting item to be:
            // {
            //     "tmpLat":57.077432765182,
            //     "tmpLong":24.323845390354,
            //     "tmpName":"\\u0100da\\u017eu pasta noda\\u013ca",
            //     "tmpNameFull":"\\u0100da\\u017eu pasta noda\\u013ca LV-2164",
            //     "tmpAddress":"Gaujas iela 11, \\u0100da\\u017ei, \\u0100da\\u017eu nov., LV-2164",
            //     "tmpCategory":1,
            //     "tmpDistrict":"\\u0100da\\u017eu nov.",
            //     "tmpPhone":"67008001",
            //     "tmpService":"LV-2164",
            //     "tmpImage":null,
            //     "tmpWork":"P 8:00-18:00<br \\/>O 8:00-18:00<br \\/>T 8:00-19:00<br \\/>C 8:00-18:00<br \\/>P 8:00-18:00<br \\/>S -<br \\/>Sv -<br \\/>",
            //     "tmpPayment":"1",
            //     "tmpFilterOut":false,
            //     "tmpMarker":null,
            //     "tmpInfo":null
            // },
            
            string? name = item.tmpName;
            string? address = item.tmpAddress;
            string? code = item.tmpService;
            double lat = item.tmpLat;
            double lon = item.tmpLong;
            int type = item.tmpCategory;

            LatviaPostItemType itemType = ParseTypeOfItem(type);

            // Post box names are exactly the same as addresses, which makes them pointless
            if (name == address) name = null;

            if (name != null)
            {
                name = name.Trim();
                
                // Add space after period for name, e.g. for "Daugavpils 18.novembra ielas pasta nodaļa"
                // (this used to be many like "Rīgas 5.pasta nodaļa", but they got names changed)
                name = Regex.Replace(name, @"(\d+)\.(?! )", @"$1. ");

                if (name.EndsWith(" -")) // some weird formatting in data
                    name = name[..^2];
            }

            address = address.Replace("pag.", "pagasts");
            address = address.Replace("nov.", "novads");
            
            if (string.IsNullOrWhiteSpace(code)) code = null;
            
            // Service location addresses are just post codes like "LV-3283" but there is no code, so convert them to code (which will be extracted)
            if (itemType == LatviaPostItemType.ServiceOnRequest)
            {
                if (code == null)
                {
                    if (Regex.IsMatch(address, @"^LV-\d{4}$"))
                    {
                        code = address;
                        address = null;
                    }
                }
            }

            LatviaPostItems.Add(
                new LatviaPostItem(
                    itemType,
                    name,
                    address,
                    code,
                    TryExtractCodeValue(code, itemType),
                    new OsmCoord(lat, lon)
                )
            );
        }
    }


    [Pure]
    private static LatviaPostItemType ParseTypeOfItem(int itemType)
    {
        return itemType switch
        {
            1 => LatviaPostItemType.Office,
            2 => LatviaPostItemType.CircleK,
            4 => LatviaPostItemType.PostBox,
            5 => LatviaPostItemType.ParcelLocker,
            6 => LatviaPostItemType.ServiceOnRequest,
            
            _ => throw new ArgumentException($"Value {itemType} is unexpected")
        };
    }

    [Pure]
    private static int? TryExtractCodeValue(string? code, LatviaPostItemType itemType)
    {
        if (code == null) 
            return null;

        switch (itemType)
        {
            case LatviaPostItemType.PostBox:
            {
                // For post boxes, it's a code written out as "Vēstuļu kastītes numurs: 129"

                string prefix = "Vēstuļu kastītes numurs: ";
                
                if (!code.StartsWith(prefix))
                    return null;
                
                if (int.TryParse(code[prefix.Length..], out int codeValue))
                    return codeValue;

                return null;
            }

            case LatviaPostItemType.Office:
            {
                // For post offices, it's a post code in form "LV-4724"
                
                if (code.Length != 7)
                    return null;

                if (int.TryParse(code[3..], out int codeValue))
                    return codeValue;

                return null;
            }

            case LatviaPostItemType.ParcelLocker:
            {
                // For parcel lockers, it's a code in form "LV6941" or "LVP231"
                // The former variant looks like a post code, but I think that's just a coincidence and data being weird

                if (code.Length < 3)
                    return null;
                
                if (code.StartsWith("LV"))
                    if (int.TryParse(code[2..], out int codeValue))
                        return codeValue;
                
                if (code.StartsWith("LVP"))
                    if (int.TryParse(code[3..], out int codeValue))
                        return codeValue;

                return null;
            }

            case LatviaPostItemType.CircleK:
            {
                // For Circle K locations, it's a code in form "LV1761"
                // It looks like a post code, but I think that's just a coincidence and data being weird
                
                if (code.Length < 3)
                    return null;
                
                if (code.StartsWith("LV"))
                    if (int.TryParse(code[2..], out int codeValue)) // we enforce it being a number
                        return codeValue;

                return null;
            }

            case LatviaPostItemType.ServiceOnRequest:
            {
                // For service locations, it's a post code in form "LV-4724" (that was originally given as address)
                
                if (code.Length != 7)
                    return null;

                if (int.TryParse(code[3..], out int codeValue))
                    return codeValue;

                return null;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
        }
    }
}