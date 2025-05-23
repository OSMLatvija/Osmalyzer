﻿namespace Osmalyzer;

[UsedImplicitly]
public class OsmPolyAnalysisData : AnalysisData, IDatedAnalysisData
{
    public override string Name => "OSM Poly";

    public override string ReportWebLink => @"https://download.geofabrik.de/europe/latvia.html";

    public override bool NeedsPreparation => false;


    public bool DataDateHasDayGranularity => true;

    protected override string DataFileIdentifier => "osm-latvia-poly";


    public DateTime RetrieveDataDate()
    {
        string result = WebsiteDownloadHelper.Read("https://download.geofabrik.de/europe/latvia.html", true);
                
        Match match = Regex.Match(result, @"contains all OSM data up to ([^\.]+)\.");
        string newestDateString = match.Groups[1].ToString(); // will be something like "2023-06-12T20:21:53Z"
                
        return DateTime.Parse(newestDateString);
    }

    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://download.geofabrik.de/europe/latvia.poly",
            Path.Combine(CacheBasePath, DataFileIdentifier + @".poly")
        );
    }

    protected override void DoPrepare()
    {
        throw new InvalidOperationException();
    }
}