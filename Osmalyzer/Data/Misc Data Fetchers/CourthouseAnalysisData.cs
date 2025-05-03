namespace Osmalyzer;

[UsedImplicitly]
public class CourthouseAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Courthouses";

    public override string ReportWebLink => @"https://www.tiesas.lv/tiesas/saraksts";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "courthouses";


    public IEnumerable<CourthouseData> Courthouses => _courthouses;

    
    private List<CourthouseData> _courthouses = null!; // only null until prepared
    
    private readonly (string, string)[] _pages = {
        ("riga", "https://www.tiesas.lv/tiesas/saraksts/rigas-tiesu-apgabals-658"),
        ("kurz", "https://www.tiesas.lv/tiesas/saraksts/kurzemes-tiesu-apgabals-660"),
        ("latg", "https://www.tiesas.lv/tiesas/saraksts/latgales-tiesu-apgabals-654"),
        ("vidz", "https://www.tiesas.lv/tiesas/saraksts/vidzemes-tiesu-apgabals-662"),
        ("zemg", "https://www.tiesas.lv/tiesas/saraksts/zemgales-tiesu-apgabals-664"),
        ("admin", "https://www.tiesas.lv/tiesas/saraksts/administrativas-tiesas-3380/"),
        ("augst", "https://www.tiesas.lv/tiesas/saraksts/augstaka-tiesa-3386/")
    };

    protected override void Download()
    {
        foreach ((string id, string page) in _pages)
        {
            WebsiteDownloadHelper.Download(
                page, 
                Path.Combine(CacheBasePath, DataFileIdentifier + @"-" + id + @".html")
            );
        }
    }

    protected override void DoPrepare()
    {
        _courthouses = new List<CourthouseData>();

        foreach ((string id, string _) in _pages)
        {
            string source = File.ReadAllText(Path.Combine(CacheBasePath, DataFileIdentifier + @"-" + id + @".html"));

            MatchCollection matches = Regex.Matches(source, @"latlng = new google.maps.LatLng\(([^,]+), ([^\)]+)\);\s*marker = new google.maps.Marker\(\{position: latlng, map: map, title:""([^""]+)""\+"", ""\+""([^""]+)"" }\);", RegexOptions.Singleline);
            // latlng = new google.maps.LatLng(56.95338, 24.11555);
            // marker = new google.maps.Marker({position: latlng, map: map, title:"Rīgas apgabaltiesa"+", "+"Brīvības bulvāris 34, Rīga, LV-1886" });   

            if (matches.Count == 0)
                throw new Exception("Did not match any items on webpage");

            foreach (Match match in matches)
            {
                double lat = double.Parse(match.Groups[1].ToString());
                double lon = double.Parse(match.Groups[2].ToString());

                string name = match.Groups[3].ToString().Trim();

                string address = match.Groups[4].ToString().Trim();

                _courthouses.Add(
                    new CourthouseData(
                        new OsmCoord(lat, lon),
                        name,
                        address
                    )
                );
            }
        }
    }
}