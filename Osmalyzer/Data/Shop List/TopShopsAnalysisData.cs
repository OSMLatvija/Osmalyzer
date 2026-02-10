using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class TopShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Top! Shops";

    public override string ReportWebLink => @"https://www.toppartika.lv/veikali/";


    protected override string DataFileIdentifier => "shops-top";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");


    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.DownloadPostAsJson(
            "https://etop.lv/v1/Stores/GetMap",
            [],
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        _shops = [ ];

        string source = File.ReadAllText(DataFileName);

// {"id":"b8cfefa0-04ad-470f-873d-b20d00fd07d1","externalId":"86",
// "imageUrl":"https://eveikalstop.azureedge.net/cms/1807e531-8607-4453-9e3c-b22800e12213.jpg?v=638671883812405920","name":"\"mini top!”",
// "type":4,"location":null,"address":"Rīgas iela 13A, Pļaviņas, Aizkraukles nov., LV-5120","phoneStore":"26184482","phoneBakery":null,
// "email":"minitop.plavinas@madara89.lv","workingTime":{"monday":"08.00-22.00","tuesday":"08.00-22.00","wednesday":"08.00-22.00","thursday":"08.00-22.00","friday":"08.00-22.00","saturday":"08.00-22.00","sunday":"08.00-22.00"},
// "hasTaromat":true,"taromatName":null,"hasTextileContainer":false,"hasChargingStation":false,
// "services":{"hasOmniva":false,"hasDpd":false,"hasVenipak":true,"hasLatvianPost":false,"hasSmartpost":true,"hasSebBank":false,"hasSwedbank":false,"hasCitadele":false,"hasLuminor":false,"hasBank":false,"hasPickupPoint":true},
// "marker":{"title":null,"longitude":25.7021382,"latitude":56.612054757950624},"hasDonationBoxChildrenFund":true,"hasDonationBoxOther":false,"donationBoxOtherName":null,"hasBatteryHandover":false,"hasLocalTopBakery":false,"hasCafe":false,"isVisible":true},
      

        dynamic content = JsonConvert.DeserializeObject(source)!;

        foreach (dynamic match in content)
        {
            string address = match.address;
            
            OsmCoord coord = new OsmCoord(
                Convert.ToDouble(match.marker.latitude),
                Convert.ToDouble(match.marker.longitude)
            );

            _shops.Add(
                new ShopData(
                    "Top!",
                    address,
                    coord
                )
            );
        }
    }
}