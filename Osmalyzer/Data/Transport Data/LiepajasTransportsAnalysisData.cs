using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsAnalysisData : GTFSAnalysisData
{
    public override string Name => "Liepajas Sabiedriskais Transports";

    public override string ReportWebLink => @"https://www.marsruti.lv/liepaja/";


    public override bool DataDateHasDayGranularity => true; 

    protected override string DataFileIdentifier => "liepajas-transports";

        
    protected override string DataFileName => cacheBasePath + DataFileIdentifier + @".zip";

    public override string ExtractionFolder => "GTFS-LiepTra";
        
    protected override string DataURL => @"http://www.marsruti.lv/liepaja/liepaja/gtfs.zip";
    // todo: where could I get a direct link from them? 
}