using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class LatvijasSabiedriskaisAutobussAnalysisData : GTFSAnalysisData
    {
        public override string Name => "Latvijas Sabiedriskais Autobuss";

        public override bool DataDateHasDayGranularity => true;

        
        protected override string DataFileIdentifier => "latvijas-autobuss";


        public override string ExtractionFolder => "GTFS-LSA";
        
        protected override string DataFileName => cacheBasePath + DataFileIdentifier + @".zip";

        protected override string DataURL => @"https://www.atd.lv/sites/default/files/GTFS/gtfs-latvia-lv.zip";
    }
}