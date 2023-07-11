using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class AutotransportaDirekcijaAnalysisData : GTFSAnalysisData
    {
        public override string Name => "Autotransporta Direkcija";

        public override bool DataDateHasDayGranularity => true;

        
        protected override string DataFileIdentifier => "autotransporta-direkcija";


        public override string ExtractionFolder => "GTFS-ATD";
        
        protected override string DataFileName => cacheBasePath + DataFileIdentifier + @".zip";

        protected override string DataURL => @"https://www.atd.lv/sites/default/files/GTFS/gtfs-latvia-lv.zip";
    }
}