namespace Osmalyzer;

[UsedImplicitly]
public class RezeknesSatiksmeAnalysisData : GTFSAnalysisData
{
    public override string Name => "Rezeknes Satiksme";

    public override string ReportWebLink => @"https://www.marsruti.lv/rezekne/";

        
    public override bool DataDateHasDayGranularity => true; 

    protected override string DataFileIdentifier => "rezeknes-satiksme";


    protected override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".zip");

    public override string ExtractionFolder => "GTFS-RezSat";
        
    protected override string DataURL => @"https://marsruti.lv/rezekne/rezekne/gtfs.zip";
    // todo: where could I get a direct link from them? 
    
    
    protected override void CleanUpGtfsData()
    {
        Network.CleanStopNames(CleanRouteStopName);

        [Pure]
        static string CleanRouteStopName(string ptStopName)
        {
            // Rezeknes almost all stops have "uc" and "nc" like suffixes like "Brīvības iela nc" and "Brīvības iela uc" - probably route direction "no centra"/"uz centru"?
            ptStopName = Regex.Replace(ptStopName, @" (uc|nc|mv)$", @"");

            // todo: move more here from IsStopNameMatchGoodEnough
        
            return ptStopName;
        }
    }
}