namespace Osmalyzer;

[UsedImplicitly]
public abstract class OsmAnalysisData : AnalysisData, IDatedAnalysisData
{
    public override string Name => "OSM (" + CountryName + ")";

    public override string ReportWebLink => @"https://download.geofabrik.de/" + DownloadUrlSubpage;

    public override bool NeedsPreparation => true;


    public bool DataDateHasDayGranularity => true;


    public OsmMasterData MasterData { get; private set; } = null!; // only null during initialization


    protected abstract string CountryName { get; }

    protected abstract string DownloadUrlSubpage { get; }
    
    protected abstract string DownloadUrlFile { get; }


    public DateTime RetrieveDataDate()
    {
        string result = WebsiteDownloadHelper.Read("https://download.geofabrik.de/" + DownloadUrlSubpage, true);
                
        Match match = Regex.Match(result, @"contains all OSM data up to ([^\.]+)\.");
        string newestDateString = match.Groups[1].ToString(); // will be something like "2023-06-12T20:21:53Z"
            
        return DateTime.Parse(newestDateString);
    }

    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://download.geofabrik.de/" + DownloadUrlFile, 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".osm.pbf")
        );
    }

    protected override void DoPrepare()
    {
        MasterData = new OsmMasterData(Path.Combine(CacheBasePath, DataFileIdentifier + @".osm.pbf"));
    }
}