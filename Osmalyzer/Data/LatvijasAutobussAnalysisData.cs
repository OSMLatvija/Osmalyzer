using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class LatvijasAutobussAnalysisData : GTFSAnalysisData
    {
        public override string Name => "Latvijas Sabiedriskais Autobuss";

        public override string DataDateFileName => @"cache/latvijas-autobuss.zip-date.txt";

        public override bool? DataDateHasDayGranularity => true;


        public override string ExtractionFolder => "GTFS-LatAut";
        
        protected override string DataFileName => @"cache/latvijas-autobuss.zip";

        protected override string DataURL => @"http://www.marsruti.lv/LSA/lsa/gtfs.zip";
        // todo: where could I get a direct link from them? 
    }
}