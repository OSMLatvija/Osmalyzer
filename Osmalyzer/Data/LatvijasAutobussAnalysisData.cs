using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class LatvijasAutobussAnalysisData : GTFSAnalysisData
    {
        public override string Name => "Latvijas Sabiedriskais Autobuss";

        public override bool DataDateHasDayGranularity => true;

        
        protected override string DataFileIdentifier => "latvijas-autobuss";


        public override string ExtractionFolder => "GTFS-LatAut";
        
        protected override string DataFileName => cacheBasePath + DataFileIdentifier + @".zip";

        protected override string DataURL => @"http://www.marsruti.lv/LSA/lsa/gtfs.zip";
        // todo: where could I get a direct link from them? 
    }
}