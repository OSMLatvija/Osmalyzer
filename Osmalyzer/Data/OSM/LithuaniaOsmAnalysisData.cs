namespace Osmalyzer;

[UsedImplicitly]
public class LithuaniaOsmAnalysisData : OsmAnalysisData
{
    protected override string DataFileIdentifier => "osm-lithuania";

    protected override string CountryName => "Lithuania";

    protected override string DownloadUrlSubpage => "europe/lithuania.html";
    
    protected override string DownloadUrlFile => "europe/lithuania-latest.osm.pbf";
}