namespace Osmalyzer;

[UsedImplicitly]
[DisabledData("Website is asking captcha to access map page")]
public class LatviaPostAnalysisData : AnalysisData, IParcelLockerListProvider
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

    public IEnumerable<ParcelPickupPoint> PickupPoints => LatviaPostItems
                                                          .Where(i => i.ItemType == LatviaPostItemType.CircleK)
                                                          .Select(i => i.AsPickupPointLocker());

    public PickupPointAmenity? PickupPointLocation => PickupPointAmenity.GasStation;
    public string PickupPointLocationName => "Circle K";


    protected override void Download()
    {
        WebsiteBrowsingHelper.DownloadPage( // page comes with no content and lots of JS to actually load everything
            ReportWebLink, 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        LatviaPostItems = [ ];

        string source = File.ReadAllText(DataFileName);

        throw new NotImplementedException();
        
        // LatviaPostItems.Add(
        //     new LatviaPostItem(
        //         itemType,
        //         name,
        //         address,
        //         code,
        //         TryExtractCodeValue(code, itemType),
        //         new OsmCoord(lat, lon)
        //     )
        // );
    }
}