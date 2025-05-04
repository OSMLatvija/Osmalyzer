namespace Osmalyzer;

[UsedImplicitly]
public class JelgavasAutobusuParksAnalysisData : GTFSAnalysisData
{
    public override string Name => "Jelgavas Autobusu Parks";

    public override string ReportWebLink => @"https://www.jap.lv/?page_id=11";

        
    public override bool DataDateHasDayGranularity => true; 

    protected override string DataFileIdentifier => "jelgavas-autobusu-parks";


    protected override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".zip");

    public override string ExtractionFolder => "GTFS-JAP";
        
    protected override string DataURL => @"https://data.lvnap.lv/jap/gtfs-lv-jap.zip";
    // todo: where could I get a direct link from them? 
}