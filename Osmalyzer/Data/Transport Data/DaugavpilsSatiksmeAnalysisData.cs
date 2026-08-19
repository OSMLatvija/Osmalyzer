namespace Osmalyzer;

[UsedImplicitly]
public class DaugavpilsSatiksmeAnalysisData : GTFSAnalysisData
{
    public override string Name => "Daugavpils satiksme";

    public override string ReportWebLink => @"https://satiksme.daugavpils.lv/";

    public override bool DataDateHasDayGranularity => true;

    protected override string DataFileIdentifier => "daugavpils-satiksme";

    protected override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".zip");

    public override string ExtractionFolder => "GTFS-DauSat";

    protected override string DataURL => @"https://satiksme.daugavpils.lv/saraksti/GTFS.zip";

    
    protected override void CleanUpGtfsData()
    {
        // We don't have any known global issues.
    }
}

