using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class RezeknesSatiksmeAnalysisData : GTFSAnalysisData
    {
        public override string Name => "Rezeknes Satiksme";
        
        public override string DataDateFileName => @"cache/rezeknes-satiksme.zip-date.txt";

        public override bool? DataDateHasDayGranularity => true; 


        protected override string DataFileName => @"cache/rezeknes-satiksme.zip";

        public override string ExtractionFolder => "GTFS-RezSat";
        
        protected override string DataURL => @"http://www.marsruti.lv/rezekne/rezekne/gtfs.zip";
        // todo: where could I get a direct link from them? 
    }
}