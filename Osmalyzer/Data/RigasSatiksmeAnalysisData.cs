using JetBrains.Annotations;

namespace Osmalyzer
{
    /// <summary>
    /// The data found on their website, seemingly whenever there are changes
    /// </summary>
    [UsedImplicitly]
    public class RigasSatiksmeAnalysisData : GTFSAnalysisData
    {
        public override string Name => "Rigas Satiksme";
        
        public override string DataDateFileName => @"cache/rigas-satiksme-direct.zip-date.txt";

        public override bool? DataDateHasDayGranularity => true; 


        protected override string DataFileName => @"cache/rigas-satiksme-direct.zip";

        public override string ExtractionFolder => "GTFS-RigSat";
        
        protected override string DataURL => @"https://saraksti.rigassatiksme.lv/riga/gtfs.zip";
    }
}