namespace Osmalyzer;

public abstract class ShopListAnalysisData : AnalysisData
{
    public abstract string DataFileName { get; }

    public abstract string ShopListUrl { get; }


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            ShopListUrl, 
            DataFileName
        );
    }
}