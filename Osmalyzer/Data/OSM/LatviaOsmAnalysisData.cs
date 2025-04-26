namespace Osmalyzer;

[UsedImplicitly]
public class LatviaOsmAnalysisData : OsmAnalysisData
{
    protected override string DataFileIdentifier => "osm-latvia";

    protected override string DownloadUrlSubpage => "europe/latvia.html";
    
    protected override string DownloadUrlFile => "europe/latvia-latest.osm.pbf";
}