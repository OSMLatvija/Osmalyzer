using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class RoadLawAnalysisData : AnalysisData
    {
        public override string Name => "Road Law";
        
        public override string DataFileName => @"cache/road-law.html";

        public override string? DataDateFileName => null;

        public override bool? DataDateHasDayGranularity => null;


        public override void Retrieve()
        {
            WebsiteDownloadHelper.Download(
                "https://likumi.lv/ta/id/198589-noteikumi-par-valsts-autocelu-un-valsts-autocelu-maroadLawruta-ietverto-pasvaldibam-piederoso-autocelu-posmu-sarakstiem", 
                DataFileName
            );
        }

        public override void Prepare()
        {
            // Don't need to prepare anything
        }
    }
}