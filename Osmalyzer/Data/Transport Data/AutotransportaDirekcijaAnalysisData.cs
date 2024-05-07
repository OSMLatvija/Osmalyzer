using System.IO;

namespace Osmalyzer;

[UsedImplicitly]
public class AutotransportaDirekcijaAnalysisData : GTFSAnalysisData
{
    public override string Name => "Autotransporta Direkcija";

    public override string ReportWebLink => @"https://www.atd.lv/lv/sabiedrisko-transportl%C4%ABdzek%C4%BCu-kust%C4%ABba";


    public override bool DataDateHasDayGranularity => true;

        
    protected override string DataFileIdentifier => "autotransporta-direkcija";


    public override string ExtractionFolder => "GTFS-ATD";
        
    protected override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".zip");

    protected override string DataURL => @"https://www.atd.lv/sites/default/files/GTFS/gtfs-latvia-lv.zip";
}