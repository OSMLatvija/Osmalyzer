namespace Osmalyzer
{
    public abstract class ShopListAnalysisData : AnalysisData
    {
        public abstract string ShopListUrl { get; }

        public override string? DataDateFileName => null;

        public override bool? DataDateHasDayGranularity => null;


        public override void Retrieve()
        {
            WebsiteDownloadHelper.Download(
                ShopListUrl, 
                DataFileName
            );
        }
    }
}