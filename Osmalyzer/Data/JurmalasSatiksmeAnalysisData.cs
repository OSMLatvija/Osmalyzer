using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class JurmalasSatiksmeAnalysisData : GTFSAnalysisData
    {
        public override string Name => "Jurmalas Autobusu Satiksme";

        public override string DataDateFileName => @"cache/jurmalas-satiksme.zip-date.txt";

        public override bool? DataDateHasDayGranularity => true;


        protected override string DataFileName => @"cache/jurmalas-satiksme.zip";

        public override string ExtractionFolder => "GTFS-JurSat";
        
        protected override string DataURL => @"http://www.marsruti.lv/jurmala/jurmala/gtfs.zip";
        // todo: where could I get a direct link from them? 
    }
}