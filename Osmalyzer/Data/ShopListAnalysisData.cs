namespace Osmalyzer
{
    public abstract class ShopListAnalysisData : AnalysisData
    {
        public override string? DataDateFileName => null;

        public override bool? DataDateHasDayGranularity => null;

        
        public abstract string DataFileName { get; }

        public abstract string ShopListUrl { get; }


        public override void OldRetrieve()
        {
            WebsiteDownloadHelper.Download(
                ShopListUrl, 
                DataFileName
            );
        }
    }
}