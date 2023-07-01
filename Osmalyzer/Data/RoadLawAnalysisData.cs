using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class RoadLawAnalysisData : AnalysisData
    {
        public override string Name => "Road Law";

        public bool? DataDateHasDayGranularity => null;

        protected override string DataFileIdentifier => "road-law";


        protected override void Download()
        {
            WebsiteDownloadHelper.Download(
                "https://likumi.lv/ta/id/198589-noteikumi-par-valsts-autocelu-un-valsts-autocelu-maroadLawruta-ietverto-pasvaldibam-piederoso-autocelu-posmu-sarakstiem", 
                cacheBasePath + DataFileIdentifier + @".html"
            );
        }
    }
}