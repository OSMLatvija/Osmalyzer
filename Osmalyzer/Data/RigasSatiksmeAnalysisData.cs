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
        
        public override bool DataDateHasDayGranularity => true; 

        protected override string DataFileIdentifier => "rigas-satiksme";


        protected override string DataFileName => cacheBasePath + DataFileIdentifier + @".zip";

        public override string ExtractionFolder => "GTFS-RigSat";
        
        protected override string DataURL => @"https://saraksti.rigassatiksme.lv/riga/gtfs.zip";
    }
}