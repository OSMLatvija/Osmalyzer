using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class LiepajasTransportsAnalysisData : GTFSAnalysisData
    {
        public override string Name => "Liepajas Sabiedriskais Transports";
        
        public override string DataFileName => @"cache/liepajas-transports.zip";

        public override string DataDateFileName => @"cache/liepajas-transports.zip-date.txt";

        public override bool? DataDateHasDayGranularity => true; // only day given on data page (file itself is month only)


        public override string ExtractionFolder => "GTFS-LT";
        
        protected override string DataURL => @"http://www.marsruti.lv/liepaja/liepaja/gtfs.zip";
        // todo: where could I get a direct link from them? 
    }
}