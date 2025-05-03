namespace Osmalyzer;

[UsedImplicitly]
public class LuluRestaurantAnalysisData : RestaurantListAnalysisData
{
    public override string Name => "Lulu Restaurants";

    public override string ReportWebLink => @"https://www.lulu.lv/picerijas";


    protected override string DataFileIdentifier => "restaurants-lulu";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<RestaurantData> Restaurant => _restaurants;


    private List<RestaurantData> _restaurants = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.lulu.lv/picerijas",
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        string source = File.ReadAllText(DataFileName);

        MatchCollection matches = Regex.Matches(
            source,
            @">([^>&]+?)&nbsp;\s*<a href=""https:\/\/maps\.google\.com\?q=(\d\d\.\d+),(\d\d\.\d+)"""
        );
        /* <div class="text-small pizza-house-address">
                                Rīga, Kurzemes prospekts 21                                &nbsp;
                                <a href="https://maps.google.com?q=56.96326065,24.03173637" rel="nofollow" target="_blank">Karte</a>
                            </div> */

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");

        _restaurants = new List<RestaurantData>();

        foreach (Match match in matches)
        {
            string address = match.Groups[1].ToString().Trim();
            double lat = double.Parse(match.Groups[2].ToString());
            double lon = double.Parse(match.Groups[3].ToString());

            _restaurants.Add(
                new RestaurantData(
                    "Lulu",
                    !string.IsNullOrWhiteSpace(address) ? address : null,
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
}